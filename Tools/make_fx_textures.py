"""Stylised cartoon cloud puffs (white, alpha = coverage) for the scent fog, 2x2 sheet."""
import numpy as np
from PIL import Image, ImageFilter
import os, sys
out = sys.argv[1]
rng = np.random.default_rng(11)
S = 256

def noise(shape, scale):
    n = rng.random((scale, scale))
    return np.asarray(Image.fromarray((n * 255).astype(np.uint8)).resize(shape, Image.BICUBIC), dtype=float) / 255

sheet = np.zeros((S * 2, S * 2, 4))
for f in range(4):
    yy, xx = (np.mgrid[0:S, 0:S] / (S - 1) * 2 - 1)
    cover = np.zeros((S, S))
    shade = np.zeros((S, S))
    blobs = [(0, 0.05, 0.42)] + [(rng.uniform(-0.42, 0.42), rng.uniform(-0.3, 0.2), rng.uniform(0.22, 0.34)) for _ in range(5)]
    for cx, cy, r in blobs:
        d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / r
        m = d < 1
        cover = np.maximum(cover, m.astype(float))
        # per-blob shading: lighter toward its top-left
        sh = np.clip(1 - ((xx - cx + 0.35 * r) ** 2 + (yy - cy + 0.45 * r) ** 2) / (r * r * 1.6), 0, 1)
        shade = np.where(m, np.maximum(shade, sh), shade)
    # wobble the silhouette with noise for a hand-painted edge
    wob = noise((S, S), 9) * 0.5 + noise((S, S), 22) * 0.5
    edge = Image.fromarray((cover * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(6))
    cover = np.asarray(edge, dtype=float) / 255
    cover = np.clip((cover - 0.5 + (wob - 0.5) * 0.25) * 6 + 0.5, 0, 1)
    cover = np.asarray(Image.fromarray((cover * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.5)), dtype=float) / 255
    # alpha slightly denser in the middle, softer at rims
    alpha = cover * (0.7 + 0.3 * np.asarray(edge, dtype=float) / 255)
    bright = 0.78 + 0.22 * np.clip(shade + (noise((S, S), 14) - 0.5) * 0.12, 0, 1)
    rgb = np.stack([bright] * 3, -1)
    y, x = divmod(f, 2)
    sheet[y * S:(y + 1) * S, x * S:(x + 1) * S] = np.dstack([rgb, alpha])
Image.fromarray((sheet * 255).astype(np.uint8)).save(os.path.join(out, 'fx_cloud.png'))
prev = Image.new('RGBA', (S * 2, S * 2), (90, 80, 110, 255))
img = Image.open(os.path.join(out, 'fx_cloud.png'))
prev.paste(img, (0, 0), img)
prev.save(os.path.join(out, '..', '..', 'Art', 'Previews', 'fx_cloud_preview.png'))
print('ok')
