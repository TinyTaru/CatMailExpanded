"""Seamless scalloped-shingle texture for the post office roof (512 px = 1.6 m of roof).
Rows run horizontally; image up = up the slope. Each row's rounded edges overlap the row below."""
import random, sys
from PIL import Image, ImageDraw, ImageFilter

out = sys.argv[1]
S = 512
ROWS, PER_ROW = 8, 8
RH, SW = S // ROWS, S // PER_ROW
rng = random.Random(7)
base = (112, 124, 170)          # soft slate blue-lavender, matches the yard eaves and blue stripes

# draw on a canvas three tiles tall, lowest row first so each upper row overlaps the one below,
# with colours that repeat every tile, then keep the middle tile: seamless both ways
img = Image.new('RGB', (S, 3 * S), base)
d = ImageDraw.Draw(img)
colours = {}


def tint(r, k):
    key = (r % ROWS, k % PER_ROW)
    if key not in colours:
        j = rng.randint(-10, 10)
        lift = 14 if rng.random() < 0.12 else 0
        colours[key] = tuple(max(0, min(255, c + j + lift)) for c in base)
    return colours[key]


for r in range(3 * ROWS + 2):
    y_bottom = 3 * S - r * RH + RH // 2          # scallop tips hang half a row below the row line
    off = (SW // 2) if r % 2 else 0
    for k in range(-1, PER_ROW + 1):
        col = tint(r, k)
        for wrap in (-S, 0, S):
            xa, ya = k * SW + off + wrap, y_bottom
            d.ellipse([xa + 2, ya - RH * 0.95 + 5, xa + SW - 2, ya + 5], fill=tuple(int(c * 0.72) for c in base))
            d.rectangle([xa + 1, ya - RH * 1.6, xa + SW - 1, ya - RH * 0.45], fill=col)
            d.ellipse([xa + 1, ya - RH * 0.95, xa + SW - 1, ya], fill=col)
            d.arc([xa + 4, ya - RH * 0.9, xa + SW - 4, ya - 4], 20, 160, fill=tuple(min(255, c + 22) for c in col), width=2)
img = img.crop((0, S, S, 2 * S))

# painterly grain
noise = Image.effect_noise((S, S), 18).convert('L')
img = Image.blend(img, Image.merge('RGB', (noise, noise, noise)), 0.06)
img = img.filter(ImageFilter.SMOOTH)
img.save(out)
print('wrote', out)
