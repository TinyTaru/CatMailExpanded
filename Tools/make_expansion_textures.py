"""Builds the textures for the mirror room / spectral parcels and the gift wrapping room.

    python3 -I Tools/make_expansion_textures.py

Inputs: Art/Textures/spectral_raw/spectral_5.png (FLUX.2 [klein] edit of the otherworldly sticker) and
Art/Textures/gift_raw/<id>.png (FLUX.2 [klein] wrapping paper patterns, Tools/gen_gift_papers.sh).
Outputs go to Assets/Build:
  sticker_spectral.png   the spectral sticker, cut out of its white background
  scan_spectral.png      scanner glyph (3x3 repeat, yellow on black like the game's glyph atlas)
  gift_paper_<id>.png    the wrapping papers, made to tile (512x512)
  gift_logo.png          a wrapped present (unlock card, corridor plaque)
  gift_tag.png           the sender's gift tag card (RGBA)
  gift_sign.png          the gift room's wall sign
  gift_poster.png        how-to-wrap poster
  scan_gift.png          scanner glyph for gifts
"""
import math
import os
import random
from collections import deque

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
OUT = os.path.join(ROOT, 'Assets', 'Build')
RAW = os.path.join(ROOT, 'Art', 'Textures')
ROUNDED = '/System/Library/Fonts/Supplemental/Arial Rounded Bold.ttf'

TEAL = (58, 150, 160)
ORANGE = (240, 150, 60)
INK = (52, 46, 60)
CREAM = (247, 240, 226)
KRAFT = (196, 160, 116)


def font(size):
    return ImageFont.truetype(ROUNDED, size)


def cutout(src, out, size=512):
    """White-background sticker to RGBA: flood-fill the outside white, soften the edge, crop, pad."""
    o = np.asarray(Image.open(src).convert('RGB')).astype(int)
    h, w, _ = o.shape
    white = (o.min(axis=2) > 232) & (o.max(axis=2) - o.min(axis=2) < 16)
    bg = np.zeros((h, w), bool)
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if white[y, x] and not bg[y, x]:
                bg[y, x] = True
                q.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if white[y, x] and not bg[y, x]:
                bg[y, x] = True
                q.append((y, x))
    while q:
        y, x = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and white[ny, nx] and not bg[ny, nx]:
                bg[ny, nx] = True
                q.append((ny, nx))
    alpha = Image.fromarray(((~bg) * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(1.0))
    img = Image.fromarray(np.dstack([o.astype(np.uint8), np.asarray(alpha)]), 'RGBA')
    img = img.crop(img.getbbox())
    s = max(img.size)
    pad = int(s * 0.04)
    canvas = Image.new('RGBA', (s + 2 * pad, s + 2 * pad), (255, 255, 255, 0))
    canvas.paste(img, ((s + 2 * pad - img.size[0]) // 2, (s + 2 * pad - img.size[1]) // 2), img)
    canvas = canvas.resize((size, size), Image.LANCZOS)
    # bleed colour into the transparent pixels so mipmaps don't get dark fringes
    arr = np.asarray(canvas).astype(float)
    a = arr[..., 3:4] / 255
    rgb = arr[..., :3]
    blur = np.asarray(Image.fromarray(np.uint8(rgb * a)).filter(ImageFilter.GaussianBlur(6))).astype(float)
    wsum = np.asarray(Image.fromarray(np.uint8(a[..., 0] * 255)).filter(ImageFilter.GaussianBlur(6))).astype(float)[..., None] / 255
    fill = np.where(wsum > 0.01, blur / np.maximum(wsum, 1e-3), 235)
    rgb = np.where(a > 0.02, rgb, fill)
    Image.fromarray(np.uint8(np.dstack([rgb, arr[..., 3]])), 'RGBA').save(out)


def ghost_glyph(d, cx, cy, s, col):
    """A little ghost: round head, wavy hem, two eye holes."""
    r = s * 0.5
    d.ellipse([cx - r, cy - r * 1.1, cx + r, cy + r * 0.9], fill=col)
    d.rectangle([cx - r, cy - r * 0.1, cx + r, cy + r * 1.05], fill=col)
    for i in range(3):
        x0 = cx - r + i * (2 * r / 3)
        d.ellipse([x0, cy + r * 0.8, x0 + 2 * r / 3, cy + r * 1.35], fill=col if i % 2 == 0 else (0, 0, 0))
    d.ellipse([cx - r * 0.45, cy - r * 0.35, cx - r * 0.15, cy + r * 0.05], fill=(0, 0, 0))
    d.ellipse([cx + r * 0.15, cy - r * 0.35, cx + r * 0.45, cy + r * 0.05], fill=(0, 0, 0))


def star(d, cx, cy, r, col):
    pts = []
    for i in range(8):
        a = math.pi / 4 * i
        rr = r if i % 2 == 0 else r * 0.32
        pts.append((cx + rr * math.sin(a), cy - rr * math.cos(a)))
    d.polygon(pts, fill=col)


def scan_glyph(out):
    img = Image.new('RGBA', (512, 512), (0, 0, 0, 255))
    d = ImageDraw.Draw(img)
    yellow = (255, 238, 0, 255)
    cell = 512 / 3
    for j in range(3):
        for i in range(3):
            cx, cy = (i + 0.5) * cell, (j + 0.5) * cell
            ghost_glyph(d, cx - 6, cy - 4, cell * 0.5, yellow)
            star(d, cx + cell * 0.32, cy - cell * 0.3, cell * 0.08, yellow)
            star(d, cx + cell * 0.36, cy + cell * 0.18, cell * 0.05, yellow)
    img.save(out)



PAPERS = ['stars', 'candy', 'dots', 'flowers', 'fish', 'plaid', 'paws', 'gold']
RIBBON = (205, 62, 72)
GOLD = (222, 176, 70)


def period(a, axis, lo=300, hi=740, strip=10):
    hi = min(hi, a.shape[axis] - strip)
    """The crop size along an axis at which the pattern repeats best (for regular patterns)."""
    n = a.shape[axis]
    best, bw = 1e9, None
    for w in range(lo, min(hi, n - strip)):
        if axis == 1:
            e = np.abs(a[:, :strip] - a[:, w:w + strip]).mean()
        else:
            e = np.abs(a[:strip] - a[w:w + strip]).mean()
        if e < best:
            best, bw = e, w
    return bw, best


def tileable(src, out, size=512, band=40):
    """Crops one repeat of the pattern where it has one, then blends the edges with a copy shifted by half a
    tile so opposite edges meet."""
    a = np.asarray(Image.open(src).convert('RGB')).astype(float)
    # flatten uneven lighting (foil sheen, vignettes) so the tiles match in brightness
    low = np.asarray(Image.fromarray(np.uint8(a)).filter(ImageFilter.GaussianBlur(90))).astype(float)
    a = np.clip(a - low + low.mean(axis=(0, 1)), 0, 255)
    w, ew = period(a, 1)
    h, eh = period(a, 0, lo=int(w * 0.8), hi=int(w * 1.25) + 1)
    if ew < 30 and eh < 30:
        a = a[:h, :w]
        band = 16
    a = np.asarray(Image.fromarray(np.uint8(a)).resize((size, size), Image.LANCZOS)).astype(float)
    b = np.roll(np.roll(a, size // 2, axis=0), size // 2, axis=1)
    d = np.minimum(np.arange(size), size - 1 - np.arange(size)).astype(float)
    w1 = np.clip(d / band, 0, 1)
    w1 = w1 * w1 * (3 - 2 * w1)
    m = np.minimum(w1[:, None], w1[None, :])[..., None]
    print(os.path.basename(src), 'repeat', w, h, round(ew, 1), round(eh, 1))
    Image.fromarray(np.uint8(a * m + b * (1 - m))).save(out)


def present(d, cx, cy, s, box, ribbon, outline=None, width=0):
    """A wrapped present seen from the front: box, lid, ribbon cross and a bow."""
    w, h = s, s * 0.78
    lid = s * 0.2
    x0, y0 = cx - w / 2, cy - h / 2 + lid * 0.6
    d.rectangle([x0 + w * 0.05, y0, x0 + w * 0.95, cy + h / 2], fill=box, outline=outline, width=width)
    d.rectangle([x0, y0 - lid, x0 + w, y0 + lid * 0.15], fill=box, outline=outline, width=width)
    d.rectangle([cx - s * 0.07, y0 - lid, cx + s * 0.07, cy + h / 2], fill=ribbon)
    for sx in (-1, 1):
        d.ellipse([cx + sx * s * 0.04 - (s * 0.28 if sx < 0 else 0), y0 - lid - s * 0.2,
                   cx + sx * s * 0.04 + (s * 0.28 if sx > 0 else 0), y0 - lid + s * 0.02], fill=ribbon, outline=outline, width=width)
    d.ellipse([cx - s * 0.07, y0 - lid - s * 0.1, cx + s * 0.07, y0 - lid + s * 0.03], fill=ribbon, outline=outline, width=width)


def gift_logo(out):
    W = 512
    img = Image.new('RGBA', (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([16, 16, W - 16, W - 16], fill=CREAM + (255,), outline=TEAL + (255,), width=18)
    present(d, W / 2, W / 2 + 30, 250, TEAL + (255,), RIBBON + (255,), INK + (255,), 6)
    img.save(out)


def scan_gift(out):
    img = Image.new('RGBA', (512, 512), (0, 0, 0, 255))
    d = ImageDraw.Draw(img)
    yellow = (255, 238, 0, 255)
    cell = 512 / 3
    for j in range(3):
        for i in range(3):
            cx, cy = (i + 0.5) * cell, (j + 0.5) * cell + 10
            s = cell * 0.5
            d.rectangle([cx - s / 2, cy - s * 0.3, cx + s / 2, cy + s * 0.45], fill=yellow)
            d.rectangle([cx - s * 0.58, cy - s * 0.45, cx + s * 0.58, cy - s * 0.22], fill=yellow)
            d.rectangle([cx - s * 0.06, cy - s * 0.45, cx + s * 0.06, cy + s * 0.45], fill=(0, 0, 0, 255))
            d.line([(cx - s * 0.58, cy - s * 0.2), (cx + s * 0.58, cy - s * 0.2)], fill=(0, 0, 0, 255), width=4)
            for sx in (-1, 1):
                x0, x1 = sorted((cx + sx * s * 0.02, cx + sx * s * 0.5))
                d.ellipse([x0, cy - s * 0.95, x1, cy - s * 0.47], outline=yellow, width=9)


def gift_tag(out):
    """A cream card with a scalloped border and a punched hole on the left, string looping out."""
    W, H = 512, 320
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([6, 6, W - 6, H - 6], radius=34, fill=(250, 244, 230, 255), outline=(196, 160, 116, 255), width=6)
    d.rounded_rectangle([26, 26, W - 26, H - 26], radius=22, outline=RIBBON + (255,), width=4)
    for x in range(40, W - 30, 26):
        d.ellipse([x - 4, 14, x + 4, 22], fill=GOLD + (255,))
        d.ellipse([x - 4, H - 22, x + 4, H - 14], fill=GOLD + (255,))
    d.ellipse([16, H / 2 - 13, 42, H / 2 + 13], fill=(0, 0, 0, 0), outline=(196, 160, 116, 255), width=4)
    img.save(out)


def gift_sign(out):
    W, H = 1024, 448
    img = Image.new('RGBA', (W, H), (250, 244, 230, 255))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([10, 10, W - 10, H - 10], radius=40, fill=(250, 244, 230), outline=TEAL, width=22)
    d.text((W / 2, 150), 'GIFT WRAPPING', font=font(98), fill=INK, anchor='mm')
    d.rectangle([22, 236, W - 22, 262], fill=RIBBON)
    cols = [(238, 170, 180), (150, 205, 210), (246, 214, 120), (180, 160, 220), (160, 210, 150)]
    for i, c in enumerate(cols):
        present(d, 170 + i * 171, 345, 105, c, RIBBON, INK, 4)
    img.convert('RGB').save(out)


def gift_poster(out):
    W, H = 768, 1024
    img = Image.new('RGB', (W, H), (250, 244, 230))
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W, 150], fill=TEAL)
    d.text((W / 2, 78), 'HOW TO WRAP A GIFT', font=font(54), fill=(255, 255, 255), anchor='mm')
    papers = [Image.open(os.path.join(OUT, 'gift_paper_' + p + '.png')).resize((70, 70)) for p in PAPERS]
    rows = [
        ('tag', 'Read the gift tag riddle.'),
        ('bench', 'Put the gift on the bench.'),
        ('paper', 'Click the right paper roll.'),
        ('ribbon', 'Click the right ribbon spool.'),
    ]
    y = 280
    for kind, text in rows:
        if kind == 'tag':
            d.rounded_rectangle([50, y - 34, 150, y + 34], radius=10, fill=(250, 244, 230), outline=(196, 160, 116), width=5)
            d.line([(70, y - 8), (130, y - 8)], fill=INK, width=4)
            d.line([(70, y + 10), (118, y + 10)], fill=INK, width=4)
        elif kind == 'bench':
            present(d, 100, y, 80, (238, 170, 180), RIBBON, INK, 3)
            d.rectangle([40, y + 34, 160, y + 44], fill=(150, 110, 75))
        elif kind == 'paper':
            img.paste(papers[0], (40, y - 35))
            img.paste(papers[4], (95, y - 35))
        elif kind == 'ribbon':
            for i, c in enumerate((RIBBON, GOLD, (90, 130, 210))):
                d.ellipse([40 + i * 40, y - 20, 80 + i * 40, y + 20], fill=c, outline=INK, width=3)
        else:
            d.rectangle([45, y - 40, 155, y + 40], outline=(150, 110, 75), width=8)
            d.line([(45, y), (155, y)], fill=(150, 110, 75), width=8)
            present(d, 100, y - 16, 34, (150, 205, 210), RIBBON)
        d.text((185, y), text, font=font(36), fill=INK, anchor='lm')
        y += 165
    d.text((W / 2, H - 70), 'Right paper and ribbon = happy customers!', font=font(30), fill=TEAL, anchor='mm')
    img.save(out)


def main():
    cutout(os.path.join(RAW, 'spectral_raw', 'spectral_5.png'), os.path.join(OUT, 'sticker_spectral.png'))
    scan_glyph(os.path.join(OUT, 'scan_spectral.png'))
    for p in PAPERS:
        tileable(os.path.join(RAW, 'gift_raw', p + '.png'), os.path.join(OUT, 'gift_paper_' + p + '.png'))
    gift_logo(os.path.join(OUT, 'gift_logo.png'))
    scan_gift(os.path.join(OUT, 'scan_gift.png'))
    gift_tag(os.path.join(OUT, 'gift_tag.png'))
    gift_sign(os.path.join(OUT, 'gift_sign.png'))
    gift_poster(os.path.join(OUT, 'gift_poster.png'))


if __name__ == '__main__':
    main()
