"""Combines Wando's soft cutout mask with a border flood-fill so interior details (music notes,
scent wisps) that the segmentation model dropped are kept. Crops, pads and resizes to 512px."""
import numpy as np, sys, os
from PIL import Image
from collections import deque
src_dir, out_dir = sys.argv[1], sys.argv[2]
pairs = [('CME Strawberry Sticker.png', 'CME Sticker Cutout T8 1.png', 'sticker_strawberry.png'),
         ('CME Banana Sticker.png', 'CME Sticker Cutout 2.png', 'sticker_banana.png'),
         ('CME Restless Sticker.png', 'CME Sticker Cutout T8 2.png', 'sticker_restless.png')]
for orig, cut, out in pairs:
    o = np.asarray(Image.open(os.path.join(src_dir, orig)).convert('RGB')).astype(int)
    c = np.asarray(Image.open(os.path.join(src_dir, cut)).convert('RGBA'))
    h, w, _ = o.shape
    white = (o.min(axis=2) > 236) & (o.max(axis=2) - o.min(axis=2) < 14)
    bg = np.zeros((h, w), bool)
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if white[y, x] and not bg[y, x]: bg[y, x] = True; q.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if white[y, x] and not bg[y, x]: bg[y, x] = True; q.append((y, x))
    while q:
        y, x = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and white[ny, nx] and not bg[ny, nx]:
                bg[ny, nx] = True; q.append((ny, nx))
    flood = (~bg).astype(np.uint8) * 255
    # soften flood edge a little and union with model alpha
    from PIL import ImageFilter
    flood_soft = np.asarray(Image.fromarray(flood).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(1.2)))
    alpha = np.maximum(c[..., 3], flood_soft)
    rgba = np.dstack([o.astype(np.uint8), alpha])
    img = Image.fromarray(rgba, 'RGBA')
    bb = img.getbbox()
    img = img.crop(bb)
    s = max(img.size)
    pad = int(s * 0.04)
    canvas = Image.new('RGBA', (s + 2 * pad, s + 2 * pad), (0, 0, 0, 0))
    canvas.paste(img, ((s + 2 * pad - img.size[0]) // 2, (s + 2 * pad - img.size[1]) // 2), img)
    canvas = canvas.resize((512, 512), Image.LANCZOS)
    # bleed colour into transparent pixels to avoid dark fringes when mipmapped
    arr = np.asarray(canvas).astype(float)
    a = arr[..., 3:4] / 255
    rgb = arr[..., :3]
    blur = np.asarray(Image.fromarray(np.uint8(rgb * a)).filter(ImageFilter.GaussianBlur(6))).astype(float)
    wsum = np.asarray(Image.fromarray(np.uint8(a[..., 0] * 255)).filter(ImageFilter.GaussianBlur(6))).astype(float)[..., None] / 255
    fill = np.where(wsum > 0.01, blur / np.maximum(wsum, 1e-3), 230)
    rgb = np.where(a > 0.02, rgb, fill)
    Image.fromarray(np.uint8(np.clip(np.dstack([rgb, arr[..., 3]]), 0, 255)), 'RGBA').save(os.path.join(out_dir, out))
    print('wrote', out)
