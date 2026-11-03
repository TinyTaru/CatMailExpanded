"""Flat layout for the gift room rug (Art/Textures/gift_raw/rug_draft.png); Tools/gen_gift_rug.sh paints it."""
import os
import sys
from PIL import Image, ImageDraw

W, H = 768, 1152
img = Image.new('RGB', (W, H), (236, 222, 196))
d = ImageDraw.Draw(img)
PURPLE, TEAL, PINK, GOLD, CREAM = (92, 52, 120), (46, 128, 132), (222, 120, 150), (220, 176, 70), (236, 222, 196)
d.rectangle([0, 0, W, H], fill=PURPLE)
d.rectangle([34, 34, W - 34, H - 34], fill=GOLD)
d.rectangle([46, 46, W - 46, H - 46], fill=TEAL)
d.rectangle([92, 92, W - 92, H - 92], fill=CREAM)
# small gift boxes along the teal border
for y in range(130, H - 100, 110):
    for x in (69,  W - 69):
        d.rectangle([x - 14, y - 14, x + 14, y + 14], fill=PINK)
        d.rectangle([x - 3, y - 14, x + 3, y + 14], fill=GOLD)
# centre medallion: a gift box with its ribbon cross
cx, cy = W // 2, H // 2
d.ellipse([cx - 230, cy - 300, cx + 230, cy + 300], fill=PINK)
d.ellipse([cx - 200, cy - 270, cx + 200, cy + 270], fill=CREAM)
d.rectangle([cx - 110, cy - 110, cx + 110, cy + 110], fill=PURPLE)
d.rectangle([cx - 18, cy - 110, cx + 18, cy + 110], fill=GOLD)
d.rectangle([cx - 110, cy - 18, cx + 110, cy + 18], fill=GOLD)
# corner stars
for (x, y) in ((170, 170), (W - 170, 170), (170, H - 170), (W - 170, H - 170)):
    d.regular_polygon((x, y, 40), 5, fill=TEAL)
img.save(sys.argv[1] if len(sys.argv) > 1 else 'Art/Textures/gift_raw/rug_draft.png')
