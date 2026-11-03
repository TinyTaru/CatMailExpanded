"""Coastline terrain + forest layout around the post office.

    blender -b --factory-startup --python terrain.py -- <out_dir> [preview.png]

Writes (all in Unity world space):
  terrain.cmemesh            faceted low-poly ground: seabed -> beach -> grass -> forest hills
  tex_terrain.png            colour ramp the terrain UVs index into (u = ground type)
  forest_layout.txt          "proto x y z yaw scale" per placed plant/tree
  syn_<proto>.cmemesh        Synty Adventure models (base at origin), textured by tex_synty_adventure.png

Layout facts (measured in game, Unity x/z):
  sea level -0.39; old water plane x -116..-14, z -47..83; old seabed at y -1
  sky sphere (day) centre (-42.3, 9.8), radius ~78; night sphere centre x -34.7
  building footprint (floors at y 0): see BUILDING below; yard pier x -13.6..-2.4, z -9..2.6
  boats reach the dock along the west wall (x < -14, z 3..20) - keep that water open
"""
import bpy, bmesh, sys, os, math, random
from mathutils import Vector, noise

sys.path.insert(0, os.path.dirname(__file__))
import cme_export as C
import synty_lib as S

args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
OUT = args[0] if args else os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Build')
PREVIEW = args[1] if len(args) > 1 else None

WL = -0.39
SKY = (-42.27, 9.78)
SKY_R = 77.5
YARD_C = (-8.0, -3.5)

# land outline (Unity x, z), clockwise; land is inside. The sea is open to the west.
LAND = [
    (70, 115), (70, -95), (-100, -95), (-100, -68), (-76, -55), (-55, -44), (-38, -33),
    (-25, -23), (-15, -17.5), (-7, -14.6), (-2.6, -12.6), (-1.6, -10.0),
    (-1.6, 2.6), (-12.6, 2.6), (-12.6, 22.8), (-15.5, 26.5), (-23, 33), (-33, 44),
    (-46, 58), (-58, 74), (-68, 115),
]
# building blocks (x0, z0, x1, z1); ground under them stays just above the sea so floors hide it
BUILDING = [
    (-14.2, -7.9, 4.6, 22.2),
    (4.4, -7.9, 12.6, 30.4),
    (12.2, -1.2, 18.0, 18.4),
    (17.6, -1.2, 24.0, 5.2),
]
YARD = (-13.8, -9.2, -2.3, 2.7)
# WaterDock plane: centre (-7.78, 10), 12.63 x 6.71; boats enter from the west along z 10
SLIP = (-16.0, 6.4, -1.2, 13.6)


def smoothstep(a, b, x):
    t = min(1.0, max(0.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def fbm(x, z, octaves=4, seed=0.0):
    v, amp, f, tot = 0.0, 1.0, 1.0, 0.0
    for _ in range(octaves):
        v += amp * noise.noise(Vector((x * f + seed, z * f - seed * 0.7, seed * 1.3)))
        tot += amp
        amp *= 0.5
        f *= 2.03
    return v / tot


def seg_dist(px, pz, ax, az, bx, bz):
    dx, dz = bx - ax, bz - az
    t = max(0.0, min(1.0, ((px - ax) * dx + (pz - az) * dz) / (dx * dx + dz * dz)))
    ex, ez = ax + dx * t - px, az + dz * t - pz
    return math.sqrt(ex * ex + ez * ez)


def inside(px, pz, poly):
    c = False
    n = len(poly)
    for i in range(n):
        ax, az = poly[i]
        bx, bz = poly[(i + 1) % n]
        if (az > pz) != (bz > pz) and px < (bx - ax) * (pz - az) / (bz - az) + ax:
            c = not c
    return c


def land_sdf(x, z):
    d = min(seg_dist(x, z, *LAND[i], *LAND[(i + 1) % len(LAND)]) for i in range(len(LAND)))
    return d if inside(x, z, LAND) else -d


def rect_dist(x, z, r):
    x0, z0, x1, z1 = r
    dx = max(x0 - x, 0, x - x1)
    dz = max(z0 - z, 0, z - z1)
    return math.sqrt(dx * dx + dz * dz)


def building_dist(x, z):
    return min(rect_dist(x, z, r) for r in BUILDING)


def sky_dist(x, z):
    return math.hypot(x - SKY[0], z - SKY[1])


def shore_d(x, z):
    """Signed distance to the shoreline (+ inland) with a wobbly coast away from the yard."""
    d0 = land_sdf(x, z)
    near = smoothstep(9, 24, math.hypot(x - YARD_C[0], z - YARD_C[1]))
    near = min(near, smoothstep(3, 10, building_dist(x, z)))
    return d0 + fbm(x / 20, z / 20, 3, 4.2) * 3.2 * (0.25 + 0.75 * near)


def height(x, z):
    d = shore_d(x, z)
    if d < 0:
        # seabed falls away smoothly so the water's depth colouring fades from shallow to deep
        h = WL - 0.12 - 2.4 * (1 - math.exp(d / 9.0))
        h += 0.12 * fbm(x / 6, z / 6, 2, 9.1) * smoothstep(-2, -10, d)
    else:
        beach = WL - 0.12 + 0.13 * min(d, 6.0) + 0.05 * max(0.0, d - 6.0)
        inland = smoothstep(5, 32, d) * (1.4 + 5.0 * (0.5 + 0.5 * fbm(x / 28, z / 28, 3, 1.7)))
        rim = smoothstep(46, 74, sky_dist(x, z)) * (13 + 9 * (0.5 + 0.5 * fbm(x / 22, z / 22, 3, 5.5)))
        bumps = 0.22 * fbm(x / 3.5, z / 3.5, 2, 2.2) * smoothstep(3, 9, d)
        # keep a gentle clearing in front of the customer entrance (east side, door near x 24, z 2)
        door = smoothstep(9, 26, math.hypot(x - 24.5, z - 2.0))
        h = beach + (inland + rim * smoothstep(2, 14, d)) * door + bumps
    # keep the ground just under the floors in and around the building
    bd = building_dist(x, z)
    if bd < 7.0:
        k = smoothstep(0.8, 7.0, bd)
        h = (1 - k) * min(h, -0.3) + k * h
    # the covered boat slip inside the dock (its own water plane) must stay open water
    sd = rect_dist(x, z, SLIP)
    if sd < 2.0:
        k = smoothstep(0.6, 2.0, sd)
        h = (1 - k) * min(h, -1.3) + k * h
    return h


def ground_type(x, z, h, ny, d):
    """u coordinate into tex_terrain (see RAMP)."""
    if ny < 0.52 and h > WL + 0.6:
        return 0.95
    if ny < 0.7 and h > WL + 0.6:
        return 0.87
    if h < WL - 0.06:
        return 0.02 + 0.17 * smoothstep(WL - 2.5, WL - 0.06, h)
    if d < 6.5 and h < WL + 0.55:
        return 0.27 + 0.12 * smoothstep(WL - 0.06, WL + 0.4, h)
    if d < 9:
        return 0.47
    forest = 0.5 + 0.5 * fbm(x / 14, z / 14, 2, 3.3)
    return 0.55 + 0.27 * smoothstep(0.3, 0.75, forest) + 0.04 * smoothstep(4, 14, h)


# colour ramp (sRGB) - soft painterly greens and sands that sit with the game's pastel palette
RAMP = [
    (0.00, (0.27, 0.40, 0.41)), (0.19, (0.55, 0.62, 0.53)),
    (0.24, (0.70, 0.66, 0.52)), (0.30, (0.80, 0.72, 0.55)), (0.40, (0.93, 0.85, 0.66)),
    (0.45, (0.80, 0.80, 0.55)), (0.50, (0.66, 0.76, 0.45)),
    (0.60, (0.56, 0.70, 0.39)), (0.72, (0.47, 0.62, 0.34)), (0.86, (0.39, 0.53, 0.30)),
    (0.88, (0.46, 0.55, 0.36)), (0.90, (0.54, 0.59, 0.48)), (1.00, (0.64, 0.68, 0.58)),
]


def ramp_png(path, w=512, hgt=4):
    img = bpy.data.images.new('tex_terrain', w, hgt, alpha=True)
    px = []
    for j in range(hgt):
        for i in range(w):
            t = (i + 0.5) / w
            for k in range(len(RAMP) - 1):
                if RAMP[k][0] <= t <= RAMP[k + 1][0]:
                    a, ca = RAMP[k]
                    b, cb = RAMP[k + 1]
                    f = (t - a) / (b - a)
                    break
            else:
                f, ca, cb = 0, RAMP[-1][1], RAMP[-1][1]
            c = [ca[n] + (cb[n] - ca[n]) * f for n in range(3)]
            # the image buffer is linear; convert so the saved PNG holds the sRGB values above
            px += [((v / 12.92) if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4) for v in c] + [1.0]
    img.pixels = px
    img.filepath_raw = path
    img.file_format = 'PNG'
    img.save()


# ---------------------------------------------------------------- terrain mesh
STEP = 0.625
X0, X1, Z0, Z1 = -128.0, 38.0, -72.0, 90.0
NX = int((X1 - X0) / STEP) + 1
NZ = int((Z1 - Z0) / STEP) + 1


def gx(i): return X0 + i * STEP
def gz(j): return Z0 + j * STEP


def build_terrain(path):
    """Writes the terrain as a height grid + per-triangle ground type; the mod builds the faceted mesh.
    Format 'CMET': u32 version, u32 nx, u32 nz, f32 x0, f32 z0, f32 step, f32 heights[nz*nx],
    then per cell (j-major): u8 keep, u8 type tri0, u8 type tri1. Diagonal of cell (i,j):
    (i+j) even -> (0,0)-(1,1), odd -> (1,0)-(0,1)."""
    import struct
    H = [[height(gx(i), gz(j)) for i in range(NX)] for j in range(NZ)]
    rng = random.Random(3)
    cells = bytearray()
    kept = 0
    for j in range(NZ - 1):
        for i in range(NX - 1):
            cx, cz = gx(i) + STEP / 2, gz(j) + STEP / 2
            if sky_dist(cx, cz) > SKY_R + 1.0:
                cells += bytes(3)
                continue
            kept += 1
            if (i + j) % 2 == 0:
                tris = [((i, j), (i + 1, j), (i + 1, j + 1)), ((i, j), (i + 1, j + 1), (i, j + 1))]
            else:
                tris = [((i, j), (i + 1, j), (i, j + 1)), ((i + 1, j), (i + 1, j + 1), (i, j + 1))]
            dd = shore_d(cx, cz)
            types = []
            for tri in tris:
                p = [Vector((gx(a), H[b][a], gz(b))) for a, b in tri]
                n = (p[1] - p[0]).cross(p[2] - p[0]).normalized()
                ny = abs(n.y)
                hx = sum(q.y for q in p) / 3
                tx = sum(q.x for q in p) / 3
                tz = sum(q.z for q in p) / 3
                u = ground_type(tx, tz, hx, ny, dd) + rng.uniform(-0.012, 0.012)
                types.append(max(1, min(254, int(u * 255 + 0.5))))
            cells += bytes([1] + types)
    with open(path, 'wb') as f:
        f.write(b'CMET')
        f.write(struct.pack('<IIIfff', 1, NX, NZ, X0, Z0, STEP))
        for row in H:
            f.write(struct.pack('<%df' % NX, *row))
        f.write(bytes(cells))
    print('terrain grid', NX, NZ, 'cells kept', kept)
    return H


def surface_y(H, x, z):
    """Exact height of the triangulated terrain at (x, z)."""
    fi, fj = (x - X0) / STEP, (z - Z0) / STEP
    i, j = int(math.floor(fi)), int(math.floor(fj))
    i = max(0, min(NX - 2, i))
    j = max(0, min(NZ - 2, j))
    u, w = fi - i, fj - j
    h00, h10, h01, h11 = H[j][i], H[j][i + 1], H[j + 1][i], H[j + 1][i + 1]
    if (i + j) % 2 == 0:  # diagonal (0,0)-(1,1)
        return h00 + (h10 - h00) * u + (h11 - h10) * w if u >= w else h00 + (h11 - h01) * u + (h01 - h00) * w
    # diagonal (1,0)-(0,1)
    if u + w <= 1:
        return h00 + (h10 - h00) * u + (h01 - h00) * w
    return h11 + (h01 - h11) * (1 - u) + (h10 - h11) * (1 - w)


# ---------------------------------------------------------------- forest layout
BROAD = ['Tree_01', 'Tree_02', 'Tree_03', 'Tree_04', 'Tree_06', 'Tree_07', 'Tree_09', 'Tree_012', 'Tree_015', 'Tree_016']
PINE = ['TreePine_01', 'TreePine_02', 'TreePine_03']
BIRCH = ['TreeBirch_01', 'TreeBirch_03']
SLIM = ['Tree_010', 'Tree_011']
UNDER = ['Bush_01', 'Bush_02', 'Bush_03', 'Bush_04', 'Plant_04', 'Plant_05', 'Plant_03']
FLOWERS = ['Flower_03', 'Flower_05', 'Plant_01', 'Plant_02', 'Grass_01']
SHORE = ['Reeds_01']
ODD = ['TreeStump_01', 'TreeLog_01', 'Mushroom_01']


def place_forest(H):
    rng = random.Random(11)
    out = []
    pts = []  # (x, z, r) for spacing
    cell = 4.0
    grid = {}

    def ok(x, z, r):
        ci, cj = int(x // cell), int(z // cell)
        for a in range(ci - 2, ci + 3):
            for b in range(cj - 2, cj + 3):
                for (px, pz, pr) in grid.get((a, b), ()):
                    if (px - x) ** 2 + (pz - z) ** 2 < ((r + pr) * 0.5) ** 2:
                        return False
        return True

    def add(x, z, r, proto, scale, sink):
        grid.setdefault((int(x // cell), int(z // cell)), []).append((x, z, r))
        y = surface_y(H, x, z) - sink * scale
        out.append((proto, x, y, z, rng.uniform(0, 360), scale))

    def slope(x, z):
        e = 0.8
        dx = surface_y(H, x + e, z) - surface_y(H, x - e, z)
        dz = surface_y(H, x, z + e) - surface_y(H, x, z - e)
        return math.hypot(dx, dz) / (2 * e)

    # trees
    for _ in range(90000):
        x, z = rng.uniform(X0, X1), rng.uniform(Z0, Z1)
        sd = sky_dist(x, z)
        if sd > SKY_R - 2.5:
            continue
        d = shore_d(x, z)
        if d < 6.0 or building_dist(x, z) < 4.0:
            continue
        h = surface_y(H, x, z)
        if h < WL + 0.35 or slope(x, z) > 1.3:
            continue
        dy = math.hypot(x - YARD_C[0], z - YARD_C[1])
        density = 0.5 + 0.5 * fbm(x / 16, z / 16, 2, 7.7)
        if rng.random() > 0.45 + 0.7 * smoothstep(0.3, 0.6, density) * smoothstep(6, 14, d):
            continue
        r = 2.7 + 1.3 * smoothstep(25, 70, dy)
        if not ok(x, z, r):
            continue
        elev = smoothstep(4, 14, h)
        roll = rng.random()
        if roll < 0.15 + 0.55 * elev:
            proto = rng.choice(PINE)
        elif roll < 0.25 + 0.5 * elev and d < 22:
            proto = rng.choice(BIRCH)
        elif roll < 0.32 + 0.5 * elev:
            proto = rng.choice(SLIM)
        else:
            proto = rng.choice(BROAD)
        add(x, z, r, proto, rng.uniform(0.85, 1.3), 0.25)
    trees = len(out)

    # undergrowth: bushes along the forest edge and under the trees, nearer the yard where it is seen
    for _ in range(60000):
        x, z = rng.uniform(-90, 36), rng.uniform(-70, 85)
        d = shore_d(x, z)
        if d < 4.0 or building_dist(x, z) < 2.5 or sky_dist(x, z) > SKY_R - 3:
            continue
        dy = math.hypot(x - YARD_C[0], z - YARD_C[1])
        if rng.random() > 1.0 - smoothstep(20, 70, dy) * 0.85:
            continue
        if surface_y(H, x, z) < WL + 0.3 or slope(x, z) > 1.0:
            continue
        r = 1.5
        if not ok(x, z, r):
            continue
        if d < 9 and rng.random() < 0.55:
            add(x, z, 0.9, rng.choice(FLOWERS), rng.uniform(0.9, 1.4), 0.03)
        elif rng.random() < 0.06:
            add(x, z, 1.4, rng.choice(ODD), rng.uniform(0.8, 1.15), 0.08)
        else:
            add(x, z, r, rng.choice(UNDER), rng.uniform(0.75, 1.25), 0.1)

    # reeds where the beach meets the water, kept away from the pier
    for _ in range(6000):
        x, z = rng.uniform(-80, 36), rng.uniform(-70, 70)
        d = shore_d(x, z)
        if not (-0.8 < d < 1.2) or building_dist(x, z) < 3 or math.hypot(x - YARD_C[0], z - YARD_C[1]) < 9:
            continue
        if rng.random() > 0.35 or not ok(x, z, 1.6):
            continue
        add(x, z, 1.6, SHORE[0], rng.uniform(0.8, 1.2), 0.05)
    print('placed', trees, 'trees,', len(out) - trees, 'small plants')
    return out


def export_protos(protos):
    for p in sorted(protos):
        C.reset_scene()
        o = S.load(f'{S.ADV}/SM_Env_{p}.fbx', S.ADV_ATLAS, 'syn_' + p)
        C.export_cmemesh(o, os.path.join(OUT, 'syn_' + p.lower() + '.cmemesh'), smooth_angle=25)


def main():
    os.makedirs(OUT, exist_ok=True)
    C.reset_scene()
    H = build_terrain(os.path.join(OUT, 'terrain.cmeterr'))
    ramp_png(os.path.join(OUT, 'tex_terrain.png'))
    layout = place_forest(H)
    with open(os.path.join(OUT, 'forest_layout.txt'), 'w') as f:
        for proto, x, y, z, yaw, s in layout:
            f.write(f'{proto.lower()} {x:.2f} {y:.3f} {z:.2f} {yaw:.0f} {s:.2f}\n')
    export_protos({p for p, *_ in layout})
    import shutil
    shutil.copy(S.ADV_ATLAS, os.path.join(OUT, 'tex_synty_adventure.png'))
    if PREVIEW:
        preview(layout)


def preview(layout):
    """Top-down map of terrain types + tree dots for a quick sanity check."""
    import struct, zlib
    W, Hh = int((X1 - X0) * 4), int((Z1 - Z0) * 4)
    rows = []
    marks = {}
    for proto, x, y, z, *_ in layout:
        marks[(int((x - X0) * 4), int((Z1 - z) * 4))] = (40, 70, 30) if 'tree' in proto.lower() else (160, 60, 120)
    for py in range(Hh):
        row = bytearray([0])
        for pxx in range(W):
            x, z = X0 + pxx / 4, Z1 - py / 4
            if (pxx, py) in marks:
                row += bytes(marks[(pxx, py)])
                continue
            h = height(x, z)
            if sky_dist(x, z) > SKY_R:
                c = (200, 200, 200)
            elif h < WL:
                k = max(0.0, min(1.0, (h - WL + 2.6) / 2.6))
                c = (int(40 + 60 * k), int(90 + 80 * k), int(140 + 60 * k))
            else:
                t = ground_type(x, z, h, 1.0, shore_d(x, z))
                c = (230, 210, 160) if t < 0.45 else (int(110 - 4 * h), int(160 - 3 * h), int(90 - 2 * h))
            if building_dist(x, z) == 0:
                c = (120, 120, 130)
            row += bytes(max(0, min(255, v)) for v in c)
        rows.append(bytes(row))
    raw = b''.join(rows)

    def chunk(t, d):
        return struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)
    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', W, Hh, 8, 2, 0, 0, 0)) + \
        chunk(b'IDAT', zlib.compress(raw, 6)) + chunk(b'IEND', b'')
    open(PREVIEW, 'wb').write(png)


main()
