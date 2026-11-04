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
  gift_plaque.png        the corridor plaque by the gift room's door
  scan_gift.png          scanner glyph for gifts
"""
import math
import os
import random
import sys
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


# ---------------------------------------------------------------- the gift room's wall art
# Drawn to match the game's "How to weigh a parcel" board (t_WorldsAndStampsMaps_BC): a pale frame, a cream title
# strip lettered in the game's own font (Grandstander), soft brushed colour bands, and flat icons made like
# stickers (a cream rim and a soft drop shadow).
GRAND = os.path.join(ROOT, 'Art', 'Fonts', 'Grandstander-clean.ttf')
P_INK = (74, 66, 68)
P_CREAM = (232, 221, 199)
P_FRAME = (226, 222, 214)
P_OLIVE = (162, 166, 96)
P_ORANGE = (238, 182, 96)
P_TEAL = (120, 168, 168)
P_PURPLE = (142, 120, 152)
P_PINK = (214, 136, 150)
P_SLATE = (138, 144, 170)
P_RIM = (236, 230, 218)
P_RED = (198, 72, 78)


def gfont(size):
    return ImageFont.truetype(GRAND, size)


def letter(d, xy, text, size, fill=P_INK, anchor='mm'):
    """Grandstander thickened a little, as the game sets its board titles."""
    d.text(xy, text, font=gfont(size), fill=fill, anchor=anchor, stroke_width=max(1, size // 30), stroke_fill=fill)


def brushed(w, h, col, seed, strength=1.0):
    """A flat colour with the soft, faceted brush texture of the game's painted boards."""
    rnd = random.Random(seed)
    img = Image.new('RGB', (w, h), col)
    d = ImageDraw.Draw(img, 'RGBA')
    for _ in range(int(w * h / 900) + 8):
        cx, cy = rnd.uniform(-0.1, 1.1) * w, rnd.uniform(-0.1, 1.1) * h
        r = rnd.uniform(0.08, 0.3) * max(w, h)
        a0 = rnd.uniform(0, math.tau)
        pts = [(cx + r * math.cos(a0 + k * math.tau / 3 + rnd.uniform(-0.4, 0.4)),
                cy + r * 0.6 * math.sin(a0 + k * math.tau / 3 + rnd.uniform(-0.4, 0.4))) for k in range(3)]
        dv = rnd.uniform(-14, 14) * strength
        tone = tuple(int(max(0, min(255, c + dv + rnd.uniform(-4, 4)))) for c in col)
        d.polygon(pts, fill=tone + (int(38 * strength),))
    return img.filter(ImageFilter.GaussianBlur(max(1.5, min(w, h) / 160)))


def sticker(base, layer, rim=10, shadow=(6, 8)):
    """Pastes an RGBA drawing onto base as a sticker: a cream rim around its shape and a soft shadow under it."""
    a = layer.split()[3]
    grown = a.filter(ImageFilter.MaxFilter(rim * 2 + 1)).filter(ImageFilter.GaussianBlur(1.2))
    sh = Image.new('RGBA', layer.size, (40, 30, 40, 0))
    sh.putalpha(grown.point(lambda v: int(v * 0.32)).filter(ImageFilter.GaussianBlur(5)))
    base.alpha_composite(sh, shadow)
    rimimg = Image.new('RGBA', layer.size, P_RIM + (0,))
    rimimg.putalpha(grown)
    base.alpha_composite(rimimg)
    base.alpha_composite(layer)


def icon_layer(size):
    return Image.new('RGBA', (size, size), (0, 0, 0, 0))


def draw_present(d, cx, cy, s, box, ribbon, line=P_INK):
    """A wrapped present: box, lid, ribbon band and a two-loop bow."""
    w, h, lid = s, s * 0.7, s * 0.22
    top = cy - h / 2 + lid * 0.5
    lw = max(2, int(s / 26))
    d.rounded_rectangle([cx - w * 0.44, top, cx + w * 0.44, cy + h / 2], radius=s * 0.05, fill=box, outline=line, width=lw)
    d.rounded_rectangle([cx - w / 2, top - lid, cx + w / 2, top + lid * 0.2], radius=s * 0.05, fill=box, outline=line, width=lw)
    d.rectangle([cx - s * 0.07, top - lid + lw, cx + s * 0.07, cy + h / 2 - lw], fill=ribbon)
    for sx in (-1, 1):
        x0, x1 = sorted((cx + sx * s * 0.03, cx + sx * s * 0.3))
        d.ellipse([x0, top - lid - s * 0.2, x1, top - lid + s * 0.02], fill=ribbon, outline=line, width=lw)
    d.ellipse([cx - s * 0.08, top - lid - s * 0.11, cx + s * 0.08, top - lid + s * 0.04], fill=ribbon, outline=line, width=lw)


def icon_present(size, box, ribbon):
    L = icon_layer(size)
    draw_present(ImageDraw.Draw(L), size / 2, size / 2 + size * 0.08, size * 0.74, box, ribbon)
    return L


def icon_tag(size):
    """A gift tag on a string, with a squiggle of riddle on it."""
    L = icon_layer(size)
    d = ImageDraw.Draw(L)
    lw = max(2, size // 40)
    c, s = size / 2, size * 0.36
    pts = [(c - s, c - s * 0.55), (c + s * 0.55, c - s * 0.55), (c + s, c), (c + s * 0.55, c + s * 0.55), (c - s, c + s * 0.55)]
    d.polygon(pts, fill=(246, 236, 214), outline=P_INK)
    d.line(pts + [pts[0]], fill=P_INK, width=lw, joint='curve')
    d.ellipse([c + s * 0.45, c - s * 0.12, c + s * 0.69, c + s * 0.12], fill=P_INK)
    d.arc([c + s * 0.5, c - s * 1.05, c + s * 1.3, c + s * 0.05], 180, 330, fill=P_RED, width=lw + 2)
    for k, ln in enumerate((0.95, 0.7, 0.85)):
        y = c - s * 0.28 + k * s * 0.28
        x0 = c - s * 0.78
        d.line([(x0 + i * s * 0.1, y + (s * 0.05 if i % 2 else -s * 0.05)) for i in range(int(ln * 11))], fill=P_INK, width=lw)
    return L


def icon_rolls(size, papers):
    """Three paper rolls standing in a row, each in a real wrapping paper."""
    L = icon_layer(size)
    lw = max(2, size // 45)
    rw, rh = size * 0.22, size * 0.62
    for k, p in enumerate(papers):
        x0 = size * 0.14 + k * size * 0.255
        y0 = size * 0.22 + (k % 2) * size * 0.06
        box = [int(x0), int(y0), int(x0 + rw), int(y0 + rh)]
        m = Image.new('L', (size, size), 0)
        ImageDraw.Draw(m).rounded_rectangle(box, radius=rw * 0.25, fill=255)
        tex = p.resize((size, size))
        L.paste(tex, (0, 0), m)
        d = ImageDraw.Draw(L)
        d.rounded_rectangle(box, radius=rw * 0.25, outline=P_INK, width=lw)
        d.ellipse([box[0] + rw * 0.2, box[1] - rw * 0.02, box[2] - rw * 0.2, box[1] + rw * 0.2], fill=(214, 186, 140), outline=P_INK, width=lw)
    return L


def icon_spools(size):
    """Ribbon spools with tails, red, yellow and blue."""
    L = icon_layer(size)
    d = ImageDraw.Draw(L)
    lw = max(2, size // 45)
    for k, col in enumerate((P_RED, (238, 196, 82), (98, 134, 206))):
        cx = size * (0.22 + k * 0.28)
        cy = size * 0.36
        r = size * 0.085
        d.line([(cx, cy), (cx + r * 0.6, cy + size * 0.36), (cx - r * 0.2, cy + size * 0.5)], fill=col, width=int(size * 0.045), joint='curve')
        for fy in (cy - r * 1.55, cy + r * 1.25):
            d.rounded_rectangle([cx - r * 1.3, fy, cx + r * 1.3, fy + r * 0.32], radius=r * 0.12, fill=(196, 150, 104), outline=P_INK, width=lw)
        d.rectangle([cx - r, cy - r * 1.23, cx + r, cy + r * 1.25], fill=col, outline=P_INK, width=lw)
    return L



def board(W, H, seed):
    """The poster's pale paper with its thin frame."""
    img = brushed(W, H, P_FRAME, seed, 0.5).convert('RGBA')
    return img


def band(img, box, col, seed):
    x0, y0, x1, y1 = [int(v) for v in box]
    img.paste(brushed(x1 - x0, y1 - y0, col, seed, 1.7), (x0, y0))


def gift_tag(out):
    """The sender's tag on a gift: a cream card, red inner border and gold dots, with a punched hole on the left.
    Opaque all over (the prop shader has no transparency, so cut-away corners would show black)."""
    W, H = 512, 320
    img = brushed(W, H, (246, 238, 220), 31, 0.4)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W - 1, H - 1], outline=(196, 160, 116), width=10)
    d.rounded_rectangle([26, 26, W - 26, H - 26], radius=22, outline=RIBBON, width=4)
    for x in range(40, W - 30, 26):
        d.ellipse([x - 4, 14, x + 4, 22], fill=GOLD)
        d.ellipse([x - 4, H - 22, x + 4, H - 14], fill=GOLD)
    d.ellipse([14, H / 2 - 15, 44, H / 2 + 15], fill=(150, 112, 78), outline=(196, 160, 116), width=4)
    img.save(out)


def gift_sign(out):
    """The sign over the pegboard: the title on a cream strip, and a row of colour bands each with a present."""
    W, H = 1024, 448
    img = board(W, H, 41)
    m = 18
    band(img, (m, m, W - m, 200), P_CREAM, 42)
    d = ImageDraw.Draw(img)
    letter(d, (W / 2, 112), 'Gift Wrapping', 104)
    cols = [P_OLIVE, P_ORANGE, P_TEAL, P_PURPLE, P_PINK]
    boxes = [(232, 236, 244), (120, 168, 168), (248, 214, 120), (238, 170, 180), (178, 158, 214)]
    ribbons = [P_RED, P_RED, (98, 134, 206), (142, 92, 170), (238, 196, 82)]
    gap = 10
    cw = (W - 2 * m - gap * 4) / 5
    for i, c in enumerate(cols):
        x0 = m + i * (cw + gap)
        band(img, (x0, 200 + gap, x0 + cw, H - m), c, 50 + i)
        sticker(img, _placed(icon_present(150, boxes[i], ribbons[i]), W, H, x0 + cw / 2, (200 + gap + H - m) / 2 + 6))
    img.convert('RGB').save(out)


def _placed(layer, W, H, cx, cy):
    """A full-size transparent layer with the icon centred at (cx, cy)."""
    full = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    full.paste(layer, (int(cx - layer.width / 2), int(cy - layer.height / 2)), layer)
    return full


def gift_poster(out):
    """How to wrap a gift, laid out like the game's weighing board: four bands, an icon on the left of each and the
    step on the right."""
    W, H = 768, 1024
    img = board(W, H, 61)
    m, gap = 22, 10
    title_h = 168
    band(img, (m, m, W - m, m + title_h), P_CREAM, 62)
    d = ImageDraw.Draw(img)
    letter(d, (W / 2, m + title_h / 2 + 4), 'How to wrap a gift', 66)
    papers = [Image.open(os.path.join(OUT, 'gift_paper_' + p + '.png')).convert('RGBA') for p in ('stars', 'candy', 'fish')]
    rows = [
        (P_OLIVE, icon_tag(170), ['Read the riddle', 'on the gift tag']),
        (P_ORANGE, icon_present(170, (238, 170, 180), P_RED), ['Put the gift', 'on the bench']),
        (P_TEAL, icon_rolls(170, papers), ['Click the', 'paper it wants']),
        (P_PURPLE, icon_spools(170), ['Click the', 'ribbon it wants']),
    ]
    top = m + title_h + gap
    rh = (H - m - top - 3 * gap) / 4
    lw = 250
    for i, (col, icon, text) in enumerate(rows):
        y0 = top + i * (rh + gap)
        band(img, (m, y0, m + lw, y0 + rh), col, 70 + i)
        band(img, (m + lw + gap, y0, W - m, y0 + rh), col, 80 + i)
        sticker(img, _placed(icon, W, H, m + lw / 2, y0 + rh / 2))
        d = ImageDraw.Draw(img)
        cx = (m + lw + gap + W - m) / 2
        letter(d, (cx, y0 + rh / 2 - 26), text[0], 40, fill=(58, 50, 54))
        letter(d, (cx, y0 + rh / 2 + 26), text[1], 40, fill=(58, 50, 54))
    img.convert('RGB').save(out)


def gift_plaque(out):
    """The corridor plaque beside the gift room's door: a present sticker on a teal band, in the board style."""
    W = 512
    img = board(W, W, 91)
    band(img, (22, 22, W - 22, W - 22), P_TEAL, 92)
    sticker(img, _placed(icon_present(330, (246, 232, 214), P_RED), W, W, W / 2, W / 2 + 6), rim=14, shadow=(8, 10))
    img.convert('RGB').save(out)


def main():
    only = sys.argv[1:]   # e.g. gift_sign gift_poster: rebuild just those
    if only:
        for name in only:
            globals()[name](os.path.join(OUT, name + '.png'))
        return
    cutout(os.path.join(RAW, 'spectral_raw', 'spectral_5.png'), os.path.join(OUT, 'sticker_spectral.png'))
    scan_glyph(os.path.join(OUT, 'scan_spectral.png'))
    for p in PAPERS:
        tileable(os.path.join(RAW, 'gift_raw', p + '.png'), os.path.join(OUT, 'gift_paper_' + p + '.png'))
    gift_logo(os.path.join(OUT, 'gift_logo.png'))
    scan_gift(os.path.join(OUT, 'scan_gift.png'))
    gift_tag(os.path.join(OUT, 'gift_tag.png'))
    gift_sign(os.path.join(OUT, 'gift_sign.png'))
    gift_poster(os.path.join(OUT, 'gift_poster.png'))
    gift_plaque(os.path.join(OUT, 'gift_plaque.png'))


if __name__ == '__main__':
    main()
