"""Builds the planetarium's textures into Assets/Build.

- sticker_otherworldly: the Fibo-painted sticker (Art/Textures/planetarium_raw/sticker_a.png) cut out of its
  white background, like Tools/finish_stickers.py does for the other stickers.
- scan_otherworldly: the scanner glyph (a ringed planet), yellow on black, repeated in a 3x3 grid.
- tex_planet_sky: the Fibo-painted nebula (sky_b.png) made seamless left to right for the dome.
- tex_planet_<name>: equirectangular planet surfaces with the sunlight baked in. Planets always turn their
  local +Z towards the orrery's sun, so light comes from longitude 0 (u = 0.5).
- tex_planet_ring, tex_planet_sun, tex_planet_rug, tex_planet_sign, tex_planet_brass, tex_planet_tube.

    python3 -I Tools/make_planetarium_textures.py
"""
import math, os, sys
from collections import deque
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
RAW = os.path.join(ROOT, 'Art', 'Textures', 'planetarium_raw')
OUT = os.path.join(ROOT, 'Assets', 'Build')
rng = np.random.default_rng(7)


def save(img, name):
    img.save(os.path.join(OUT, name + '.png'))
    print('wrote', name)


# ------------------------------------------------------------------ sticker
def sticker():
    o = np.asarray(Image.open(os.path.join(RAW, 'sticker_a.png')).convert('RGB')).astype(int)
    h, w, _ = o.shape
    white = (o.min(axis=2) > 232) & (o.max(axis=2) - o.min(axis=2) < 16)
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
    alpha = np.asarray(Image.fromarray(((~bg) * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(1.2)))
    img = Image.fromarray(np.dstack([o.astype(np.uint8), alpha]), 'RGBA')
    img = img.crop(img.getbbox())
    s = max(img.size)
    pad = int(s * 0.04)
    canvas = Image.new('RGBA', (s + 2 * pad, s + 2 * pad), (0, 0, 0, 0))
    canvas.paste(img, ((s + 2 * pad - img.size[0]) // 2, (s + 2 * pad - img.size[1]) // 2), img)
    canvas = canvas.resize((512, 512), Image.LANCZOS)
    arr = np.asarray(canvas).astype(float)
    a = arr[..., 3:4] / 255
    rgb = arr[..., :3]
    blur = np.asarray(Image.fromarray(np.uint8(rgb * a)).filter(ImageFilter.GaussianBlur(6))).astype(float)
    wsum = np.asarray(Image.fromarray(np.uint8(a[..., 0] * 255)).filter(ImageFilter.GaussianBlur(6))).astype(float)[..., None] / 255
    fill = np.where(wsum > 0.01, blur / np.maximum(wsum, 1e-3), 230)
    rgb = np.where(a > 0.02, rgb, fill)
    out = Image.fromarray(np.uint8(np.clip(np.dstack([rgb, arr[..., 3]]), 0, 255)), 'RGBA')
    save(out, 'sticker_otherworldly')
    return out


# ------------------------------------------------------------------ scanner glyph
def glyph():
    cell = 170
    g = Image.new('L', (cell * 4, cell * 4), 0)   # drawn at 4x, downsampled for smooth edges
    d = ImageDraw.Draw(g)
    S = 4
    cx, cy, r = 78 * S // 1, 92 * S // 1, 40 * S
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=255)
    # ring: an ellipse band, cut where it passes behind the planet
    ring = Image.new('L', g.size, 0)
    rd = ImageDraw.Draw(ring)
    rw, rh, t = 74 * S, 22 * S, 9 * S
    rd.ellipse([cx - rw, cy - rh, cx + rw, cy + rh], fill=255)
    rd.ellipse([cx - rw + t, cy - rh + t * 0.7, cx + rw - t, cy + rh - t * 0.7], fill=0)
    ring = ring.rotate(-18, center=(cx, cy))
    behind = Image.new('L', g.size, 0)
    ImageDraw.Draw(behind).rectangle([0, 0, g.size[0], cy - 4 * S], fill=255)
    ring_arr = np.asarray(ring).astype(int)
    gap = np.asarray(Image.new('L', g.size, 0)).astype(int)
    gm = Image.new('L', g.size, 0)
    ImageDraw.Draw(gm).ellipse([cx - r - 7 * S, cy - r - 7 * S, cx + r + 7 * S, cy + r + 7 * S], fill=255)
    gap = np.asarray(gm).astype(int)
    hid = (np.asarray(behind) > 0) & (gap > 0)
    planet = np.asarray(g).astype(int)
    planet = np.where((ring_arr > 0) & ~hid, 0, planet)                        # outline gap in front
    ring_front = Image.fromarray(np.uint8(ring_arr)).filter(ImageFilter.MinFilter(1))
    out = np.maximum(planet, np.where(hid, 0, np.asarray(ring_front)))
    # carve a thin dark gap between ring and planet where the ring passes in front
    edge = Image.fromarray(np.uint8((ring_arr > 0) * 255)).filter(ImageFilter.MaxFilter(13))
    out = np.where((np.asarray(edge) > 0) & (ring_arr == 0) & (planet > 0) & ~hid, 0, out)
    img = Image.fromarray(np.uint8(out))
    d = ImageDraw.Draw(img)
    for (sx, sy, sr) in ((140, 34, 14), (150, 140, 9), (24, 26, 8)):
        sx, sy, sr = sx * S, sy * S, sr * S
        d.polygon([(sx, sy - sr), (sx + sr * 0.28, sy - sr * 0.28), (sx + sr, sy), (sx + sr * 0.28, sy + sr * 0.28),
                   (sx, sy + sr), (sx - sr * 0.28, sy + sr * 0.28), (sx - sr, sy), (sx - sr * 0.28, sy - sr * 0.28)], fill=255)
    img = img.resize((cell, cell), Image.LANCZOS)
    a = np.asarray(img).astype(float) / 255
    tile = np.zeros((cell, cell, 4))
    tile[..., 0] = a * 255
    tile[..., 1] = a * 255
    tile[..., 3] = 255
    sheet = np.zeros((512, 512, 4), np.uint8)
    sheet[..., 3] = 255
    for i in range(3):
        for j in range(3):
            sheet[j * cell + 1:j * cell + 1 + cell, i * cell + 1:i * cell + 1 + cell] = tile.astype(np.uint8)
    save(Image.fromarray(sheet, 'RGBA'), 'scan_otherworldly')


# ------------------------------------------------------------------ sky
def sky():
    img = np.asarray(Image.open(os.path.join(RAW, 'sky_b.png')).convert('RGB')).astype(float)
    h, w, _ = img.shape
    band = w // 6
    out = img[:, :w - band].copy()
    for i in range(band):
        t = i / band
        t = t * t * (3 - 2 * t)
        out[:, i] = img[:, i] * t + img[:, w - band + i] * (1 - t)
    # the top rows converge at the dome's apex: fade them to the sky's average there so no streaks show
    top = out[: h // 10].reshape(-1, 3).mean(axis=0)
    for y in range(h // 6):
        k = (1 - y / (h // 6)) ** 2
        out[y] = out[y] * (1 - k) + top * k
    save(Image.fromarray(np.uint8(np.clip(out, 0, 255))).resize((2048, 1024), Image.LANCZOS), 'tex_planet_sky')


# ------------------------------------------------------------------ planets
W, H = 512, 256
lon = (np.arange(W) + 0.5) / W * 2 * np.pi - np.pi
lat = np.pi / 2 - (np.arange(H) + 0.5) / H * np.pi      # row 0 = north pole (image top = v 1)
LON, LAT = np.meshgrid(lon, lat)


def periodic_noise(scale, seed, octaves=3):
    r = np.random.default_rng(seed)
    total = np.zeros((H, W))
    amp = 1.0
    for o in range(octaves):
        f = np.fft.fft2(r.standard_normal((H, W)))
        ky = np.fft.fftfreq(H)[:, None] * H
        kx = np.fft.fftfreq(W)[None, :] * W / 2
        k = np.sqrt(kx ** 2 + ky ** 2)
        cut = scale * (2 ** o)
        f *= np.exp(-(k / cut) ** 2)
        n = np.real(np.fft.ifft2(f))
        total += amp * n / (n.std() + 1e-9)
        amp *= 0.5
    return total / total.std()


def lerp(a, b, t):
    t = np.clip(t, 0, 1)[..., None]
    return np.array(a) * (1 - t) + np.array(b) * t


def shade(surface, ambient=0.2, rim=(0.55, 0.45, 0.95)):
    n_dot_l = np.cos(LAT) * np.cos(LON)
    s = np.clip((n_dot_l + 0.12) / 0.55, 0, 1)
    s = s * s * (3 - 2 * s)
    lit = surface * (ambient + (1 - ambient) * s)[..., None]
    night = np.array([0.10, 0.09, 0.22]) * 255 * (1 - s)[..., None] * 0.6
    # thin atmosphere rim on the lit limb
    limb = np.clip(1 - np.abs(np.cos(LAT) * np.sin(LON)), 0, 1)
    glow = (np.clip(np.cos(LAT) * np.cos(LON), 0, 1) ** 0.5) * (1 - limb) ** 6
    return np.clip(lit + night + np.array(rim) * 255 * glow[..., None] * 0.35, 0, 255)


def bands(colors, freq, turb, seed):
    n = periodic_noise(6, seed)
    t = (np.sin(LAT * freq + n * turb) + 1) / 2
    k = np.clip(t * (len(colors) - 1), 0, len(colors) - 1.001)
    i = k.astype(int)
    f = k - i
    cs = np.array(colors, float)
    return cs[i] * (1 - f[..., None]) + cs[i + 1] * f[..., None]


def craters(base, seed, count, dark=0.75):
    r = np.random.default_rng(seed)
    out = base.copy()
    for _ in range(count):
        clon, clat = r.uniform(-np.pi, np.pi), r.uniform(-1.2, 1.2)
        rad = r.uniform(0.05, 0.22)
        d = np.arccos(np.clip(np.sin(LAT) * np.sin(clat) + np.cos(LAT) * np.cos(clat) * np.cos(LON - clon), -1, 1))
        inner = np.clip(1 - d / rad, 0, 1)
        rimk = np.exp(-((d - rad) / (rad * 0.18)) ** 2)
        out = out * (1 - (inner > 0)[..., None] * (1 - dark) * np.clip(inner * 3, 0, 1)[..., None]) + rimk[..., None] * 18
    return out


def planet(name, surface, ambient=0.2, rim=(0.55, 0.45, 0.95)):
    img = shade(surface, ambient, rim)
    save(Image.fromarray(np.uint8(img)).filter(ImageFilter.SMOOTH), 'tex_planet_' + name)


def planets():
    planet('violet', bands([(122, 92, 200), (172, 140, 226), (226, 206, 246), (150, 110, 214), (104, 76, 176)], 7, 0.3, 1))
    amber = bands([(236, 186, 120), (250, 226, 180), (206, 128, 76), (240, 204, 150), (186, 104, 70)], 9, 0.22, 2)
    spot = np.exp(-(((LON - 0.6) / 0.32) ** 2 + ((LAT + 0.35) / 0.13) ** 2))
    amber = lerp(amber, (196, 84, 62), spot * 0.9)
    planet('amber', amber, rim=(1.0, 0.8, 0.6))
    n = periodic_noise(5, 3)
    land = n > 0.55
    ocean = lerp((40, 92, 190), (70, 150, 220), (n + 2) / 4)
    blue = np.where(land[..., None], lerp((84, 170, 110), (186, 196, 120), (n - 0.55)), ocean)
    ice = np.abs(LAT) > 1.25
    blue = np.where(ice[..., None], np.array([236, 244, 255.0]), blue)
    clouds = np.clip(periodic_noise(9, 4) - 0.9, 0, 1)
    blue = lerp(blue, (245, 248, 255), clouds * 0.9)
    planet('blue', blue, rim=(0.5, 0.75, 1.0))
    sw = periodic_noise(4, 5)
    pink = lerp((246, 150, 196), (255, 214, 232), (np.sin(LAT * 5 + LON * 2 + sw * 2) + 1) / 2)
    planet('pink', pink, rim=(1.0, 0.6, 0.9))
    red = lerp((176, 78, 56), (222, 128, 84), (periodic_noise(7, 6) + 2) / 4)
    planet('red', craters(red, 7, 18), rim=(1.0, 0.6, 0.45))
    cy = lerp((176, 228, 240), (236, 252, 255), (periodic_noise(6, 8) + 2) / 4)
    cracks = np.abs(periodic_noise(10, 9)) < 0.08
    cy = np.where(cracks[..., None], np.array([120.0, 186, 214]), cy)
    planet('ice', cy, rim=(0.6, 0.9, 1.0))
    moon = lerp((150, 150, 166), (204, 204, 214), (periodic_noise(8, 10) + 2) / 4)
    planet('moon', craters(moon, 11, 26, 0.7), ambient=0.16, rim=(0.6, 0.6, 0.8))
    # the sun is its own light: no terminator, warm granulated surface
    g = periodic_noise(14, 12)
    sun = lerp((255, 170, 60), (255, 236, 150), (g + 2) / 4)
    save(Image.fromarray(np.uint8(np.clip(sun, 0, 255))), 'tex_planet_sun')


def ring():
    w = 512
    x = np.linspace(0, 1, w)
    n = np.random.default_rng(13).standard_normal(w)
    n = np.convolve(n, np.ones(9) / 9, 'same')
    t = (np.sin(x * 40) * 0.5 + 0.5) * 0.4 + n * 0.25 + 0.5
    col = lerp((150, 120, 220), (236, 222, 255), t)
    gap = np.exp(-((x - 0.62) / 0.025) ** 2)
    col = col * (1 - 0.7 * gap)[..., None]
    img = np.repeat(col[None], 16, axis=0)
    save(Image.fromarray(np.uint8(np.clip(img, 0, 255))), 'tex_planet_ring')


def star(d, cx, cy, r, fill):
    pts = []
    for k in range(8):
        a = k * math.pi / 4
        rr = r if k % 2 == 0 else r * 0.3
        pts.append((cx + rr * math.sin(a), cy - rr * math.cos(a)))
    d.polygon(pts, fill=fill)


def rug():
    S = 1024
    img = Image.new('RGB', (S, S), (40, 36, 92))
    d = ImageDraw.Draw(img)
    c = S / 2
    d.ellipse([8, 8, S - 8, S - 8], fill=(214, 172, 86))
    d.ellipse([30, 30, S - 30, S - 30], fill=(36, 32, 86))
    d.ellipse([60, 60, S - 60, S - 60], outline=(214, 172, 86), width=6)
    r = np.random.default_rng(14)
    for _ in range(110):
        a, rad = r.uniform(0, 2 * math.pi), r.uniform(80, 440)
        star(d, c + rad * math.cos(a), c + rad * math.sin(a), r.uniform(4, 14), (236, 206, 120))
    # constellation lines
    pts = [(c + 260 * math.cos(a), c + 260 * math.sin(a)) for a in np.linspace(0.4, 2.2, 5)]
    d.line(pts, fill=(170, 150, 220), width=4)
    for p in pts: star(d, p[0], p[1], 16, (250, 230, 160))
    # crescent moon in the middle
    d.ellipse([c - 120, c - 120, c + 120, c + 120], fill=(240, 206, 110))
    d.ellipse([c - 70, c - 150, c + 150, c + 70], fill=(36, 32, 86))
    noise = np.asarray(img).astype(float) * (1 + np.random.default_rng(15).normal(0, 0.035, (S, S, 1)))
    save(Image.fromarray(np.uint8(np.clip(noise, 0, 255))).filter(ImageFilter.SMOOTH), 'tex_planet_rug')


def sign(stk):
    S = 512
    img = Image.new('RGB', (S, S), (214, 172, 86))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([14, 14, S - 14, S - 14], 60, fill=(36, 32, 86))
    d.rounded_rectangle([34, 34, S - 34, S - 34], 46, outline=(214, 172, 86), width=5)
    r = np.random.default_rng(16)
    for _ in range(26):
        star(d, r.uniform(60, S - 60), r.uniform(60, S - 60), r.uniform(3, 8), (236, 206, 120))
    s = stk.resize((380, 380), Image.LANCZOS)
    img.paste(s, ((S - 380) // 2, (S - 380) // 2), s)
    save(img, 'tex_planet_sign')


def flat(name, c1, c2, size=64):
    t = (periodic_noise(3, hash(name) % 1000)[:size, :size] + 2) / 4
    img = lerp(c1, c2, t)
    save(Image.fromarray(np.uint8(np.clip(img, 0, 255))), name)


def front_path():
    """Flagstones set in sand, tiling along the path (v) with sandy edges across it (u)."""
    W2, H2 = 256, 256
    r = np.random.default_rng(31)
    pts = []
    for _ in range(26):
        pts.append((r.uniform(0.18, 0.82) * W2, r.uniform(0, 1) * H2))
    ys, xs = np.mgrid[0:H2, 0:W2].astype(float)
    d1 = np.full((H2, W2), 1e9); d2 = np.full((H2, W2), 1e9); idx = np.zeros((H2, W2), int)
    for k, (px, py) in enumerate(pts):
        for oy in (-H2, 0, H2):                      # wrap along v so it tiles
            d = np.hypot(xs - px, ys - (py + oy)) * (1 + 0.15 * np.sin(k * 7.1))
            closer = d < d1
            d2 = np.where(closer, d1, np.minimum(d2, d))
            idx = np.where(closer, k, idx)
            d1 = np.where(closer, d, d1)
    gap = np.clip((d2 - d1) / 6.0, 0, 1)
    tone = np.array([r.uniform(0.82, 1.08) for _ in pts])[idx]
    stone = np.array([176, 166, 150.0]) * tone[..., None]
    sand = np.array([214, 194, 156.0])
    edge = np.clip((np.abs(xs / W2 - 0.5) - 0.36) / 0.1, 0, 1)
    k = np.clip(gap * 1.4, 0, 1) * (1 - edge)
    img = sand * (1 - k[..., None]) + stone * k[..., None]
    img *= (1 + r.normal(0, 0.04, (H2, W2, 1)))
    save(Image.fromarray(np.uint8(np.clip(img, 0, 255))).filter(ImageFilter.SMOOTH), 'tex_front_path')


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    stk = sticker()
    glyph()
    sky()
    planets()
    ring()
    rug()
    sign(stk)
    flat('tex_planet_brass', (176, 128, 60), (226, 180, 96))
    flat('tex_planet_tube', (196, 120, 255), (226, 170, 255))
    front_path()
