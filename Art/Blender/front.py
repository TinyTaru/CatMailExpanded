"""The way to the front of the building from the yard's south-east corner (see FrontPath.cs): a closed garden
gate in the deck railing, a footbridge over the channel, a signpost, lamp posts, and a flagstone path draped
over the terrain along the building's south side. Unity world coordinates (see cme_export.U). Usage:
  blender -b --factory-startup --python front.py -- [gate|bridge|path|all]
"""
import bpy, bmesh, sys, os, math, random, struct
sys.path.insert(0, os.path.dirname(__file__))
import importlib, cme_export as C
importlib.reload(C)
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Build')
which = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else ['all']

TOP = 0.03                     # deck surface
GX = -2.51                     # the deck's east railing line
GZ0, GZ1 = -8.94, -7.22        # gate opening between the corner post and the building
BZ0, BZ1 = -8.8, -7.4          # footbridge width
BX0, BX1 = -2.45, -0.15        # footbridge length (over the channel)
LAMPS = [(4.4, -9.45), (11.2, -7.75)]
SIGN = (0.95, -9.25)
# path centre line (x, z): from the bridge's foot along the building's south side towards the front lawn
PATH = [(-0.15, -8.1), (0.9, -8.15), (3.5, -8.6), (6.5, -8.7), (9.0, -8.0), (11.5, -6.5), (13.6, -4.9), (15.8, -3.9)]
PATH_W = 1.15


# ---------------------------------------------------------------- terrain (same faceting as Outdoors.TerrainMesh)
def load_terrain():
    d = open(os.path.join(OUT, 'terrain.cmeterr'), 'rb').read()
    nx, nz = struct.unpack_from('<II', d, 8)
    x0, z0, step = struct.unpack_from('<fff', d, 16)
    h = struct.unpack_from('<%df' % (nx * nz), d, 28)
    return nx, nz, x0, z0, step, h


T = load_terrain()


def ground(x, z):
    nx, nz, x0, z0, step, h = T
    fx, fz = (x - x0) / step, (z - z0) / step
    i, j = int(math.floor(fx)), int(math.floor(fz))
    u, v = fx - i, fz - j
    H = lambda a, b: h[(j + b) * nx + (i + a)]
    h00, h10, h01, h11 = H(0, 0), H(1, 0), H(0, 1), H(1, 1)
    if ((i + j) & 1) == 0:
        if u >= v: return h00 + u * (h10 - h00) + v * (h11 - h10)
        return h00 + v * (h01 - h00) + u * (h11 - h01)
    if u + v <= 1: return h00 + u * (h10 - h00) + v * (h01 - h00)
    return h11 + (1 - u) * (h01 - h11) + (1 - v) * (h10 - h11)


def mats():
    P = C.painterly_mat
    return dict(
        rail=P('rail', (0.90, 0.86, 0.78), 0.05, 5),
        plank=P('plank', (0.56, 0.40, 0.28), 0.10, 4, (0.50, 0.36, 0.25)),
        plank2=P('plank2', (0.50, 0.35, 0.24), 0.10, 4, (0.56, 0.41, 0.29)),
        beam=P('beam', (0.47, 0.36, 0.27), 0.08, 4),
        post=P('post', (0.42, 0.33, 0.26), 0.08, 4),
        metal=P('metal', (0.30, 0.38, 0.36), 0.06, 6),
        glass=P('glass', (1.0, 0.90, 0.62), 0.03, 6),
        brass=P('brass', (0.80, 0.62, 0.32), 0.05, 7),
        signwood=P('signwood', (0.62, 0.46, 0.30), 0.08, 5),
        arrow=P('arrow', (0.95, 0.92, 0.84), 0.04, 6),
        teal=P('teal', (0.45, 0.66, 0.68), 0.06, 6),
    )


# ---------------------------------------------------------------- gate
def build_gate(M):
    parts = []
    B = C.ubox
    for z in (GZ0, GZ1):
        parts.append(B('gpost', (GX, TOP + 0.65, z), (0.14, 1.3, 0.14), M['rail'], 0.02))
        parts.append(B('gcap', (GX, TOP + 1.32, z), (0.19, 0.05, 0.19), M['rail'], 0.015))
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.065, segments=16, ring_count=8, location=C.U(GX, TOP + 1.41, z))
        s = bpy.context.active_object; C.assign(s, M['rail']); parts.append(s)
    # two leaves meeting in the middle, pickets with an arched top
    mid = (GZ0 + GZ1) / 2
    for (za, zb) in ((GZ0 + 0.1, mid - 0.015), (mid + 0.015, GZ1 - 0.1)):
        n = 7
        for k in range(n):
            z = za + (zb - za) * (k + 0.5) / n
            # arch rises towards the middle of the opening
            t = 1 - abs(z - mid) / (mid - GZ0)
            hgt = 0.9 + 0.22 * math.sin(t * math.pi / 2)
            parts.append(B('picket', (GX, TOP + 0.08 + hgt / 2, z), (0.045, hgt, 0.075), M['rail'], 0.01))
            pt = B('tip', (GX, TOP + 0.08 + hgt, z), (0.045, 0.07, 0.07), M['rail'], 0.008)
            C.urot(pt, x_deg=45)
            parts.append(pt)
        for y in (0.3, 0.82):
            parts.append(B('ledger', (GX - 0.035, TOP + y, (za + zb) / 2), (0.04, 0.08, zb - za), M['rail'], 0.01))
        brace = B('brace', (GX - 0.035, TOP + 0.56, (za + zb) / 2), (0.035, 0.07, math.hypot(zb - za, 0.52) - 0.05), M['rail'], 0.008)
        C.urot(brace, x_deg=math.degrees(math.atan2(0.52, zb - za)) * (1 if za < mid else -1))
        parts.append(brace)
        for y in (0.3, 0.82):   # hinge straps on the post side
            zh = za + 0.05 if za < mid else zb - 0.05
            parts.append(B('hinge', (GX - 0.06, TOP + y, zh), (0.015, 0.05, 0.16), M['metal'], 0.004))
    # latch where the leaves meet
    parts.append(B('latch', (GX - 0.07, TOP + 0.86, mid), (0.03, 0.05, 0.16), M['metal'], 0.006))
    parts.append(B('latchknob', (GX - 0.1, TOP + 0.86, mid + 0.05), (0.04, 0.04, 0.04), M['brass'], 0.01))
    C.finish(parts, 'front_gate', OUT, 1024)


# ---------------------------------------------------------------- footbridge, ramp, signpost, lamps
def build_bridge(M):
    random.seed(21)
    parts = []
    B = C.ubox
    n = int((BX1 - BX0) / 0.2)
    pw = (BX1 - BX0) / n
    for i in range(n):
        x = BX0 + (i + 0.5) * pw
        m = M['plank'] if random.random() < 0.6 else M['plank2']
        parts.append(B('bplank', (x, TOP - 0.035, (BZ0 + BZ1) / 2), (pw - 0.016, 0.07, BZ1 - BZ0 - random.uniform(0, 0.05)), m, 0.012))
    for z in (BZ0 + 0.06, BZ1 - 0.06):
        parts.append(B('stringer', ((BX0 + BX1) / 2, TOP - 0.13, z), (BX1 - BX0, 0.14, 0.1), M['beam'], 0.01))
        for x in (BX0 + 0.3, (BX0 + BX1) / 2, BX1 - 0.2):
            parts.append(C.ucyl('bstilt', (x, -0.7, z), 0.1, 1.4, M['post'], 12, 0.01))
    # ramp down to the sand
    gx = BX1 + 0.85
    gy = ground(gx, (BZ0 + BZ1) / 2) + 0.03
    ang = math.degrees(math.atan2(TOP - gy, gx - BX1))
    rl = math.hypot(gx - BX1, TOP - gy)
    for k in range(5):
        x = BX1 + (gx - BX1) * (k + 0.5) / 5
        y = TOP + (gy - TOP) * (k + 0.5) / 5 - 0.035
        p = B('rplank', (x, y, (BZ0 + BZ1) / 2), (rl / 5 - 0.016, 0.07, BZ1 - BZ0 - 0.04), M['plank2' if k % 2 else 'plank'], 0.012)
        C.urot(p, z_deg=-ang)
        parts.append(p)
    # railings on both sides of the bridge (same white as the deck's)
    for z in (BZ0 + 0.03, BZ1 - 0.03):
        for x in (BX0 + 0.12, (BX0 + BX1) / 2, BX1 - 0.06):
            parts.append(B('brpost', (x, TOP + 0.5, z), (0.1, 1.0, 0.1), M['rail'], 0.015))
            parts.append(B('brcap', (x, TOP + 1.02, z), (0.14, 0.05, 0.14), M['rail'], 0.012))
        for (y, th) in ((0.45, 0.06), (0.95, 0.08)):
            parts.append(B('brrail', ((BX0 + BX1) / 2, TOP + y, z), (BX1 - BX0 - 0.1, th, 0.07), M['rail'], 0.012))
    # signpost pointing along the path
    sx, sz = SIGN
    gy = ground(sx, sz)
    parts.append(B('spost', (sx, gy + 0.85, sz), (0.1, 1.9, 0.1), M['post'], 0.015))
    parts.append(B('scap', (sx, gy + 1.82, sz), (0.15, 0.05, 0.15), M['post'], 0.01))
    for (y, yaw, ln) in ((1.55, -8.0, 0.75), (1.2, 22.0, 0.6)):
        board = B('sboard', (sx + ln / 2 - 0.05, gy + y, sz - 0.06), (ln, 0.2, 0.04), M['signwood'], 0.012)
        tip = B('stip', (sx + ln - 0.05, gy + y, sz - 0.06), (0.15, 0.15, 0.04), M['signwood'], 0.01)
        C.urot(tip, z_deg=45)
        stripe = B('sstripe', (sx + ln / 2 - 0.05, gy + y, sz - 0.085), (ln * 0.7, 0.05, 0.012), M['arrow'], 0.004)
        for o in (board, tip, stripe):
            o.location = o.location  # keep
        for o in (board, tip, stripe):
            # boards turn about the post
            from mathutils import Matrix, Vector
            p = Vector(C.U(sx, gy + y, sz))
            o.matrix_basis = Matrix.Translation(p) @ Matrix.Rotation(math.radians(-yaw), 4, 'Z') @ Matrix.Translation(-p) @ o.matrix_basis
            parts.append(o)
    # lamp posts beside the path
    for (lx, lz) in LAMPS:
        gy = ground(lx, lz)
        parts.append(B('lampbase', (lx, gy + 0.1, lz), (0.3, 0.2, 0.3), M['metal'], 0.03))
        parts.append(C.ucyl('lamppost', (lx, gy + 1.25, lz), 0.05, 2.3, M['metal'], 12, 0.01))
        parts.append(B('lamparm', (lx, gy + 2.35, lz + 0.22), (0.05, 0.05, 0.5), M['metal'], 0.01))
        parts.append(B('lantern', (lx, gy + 2.13, lz + 0.42), (0.22, 0.3, 0.22), M['glass'], 0.03))
        parts.append(B('lantop', (lx, gy + 2.32, lz + 0.42), (0.28, 0.06, 0.28), M['metal'], 0.02))
        parts.append(B('lanbot', (lx, gy + 1.97, lz + 0.42), (0.26, 0.04, 0.26), M['metal'], 0.01))
    C.finish(parts, 'front_bridge', OUT, 2048)
    with open(os.path.join(OUT, 'front_lamps.txt'), 'w') as f:
        for (lx, lz) in LAMPS:
            f.write('%.3f %.3f %.3f\n' % (lx, ground(lx, lz) + 2.13, lz + 0.42))


# ---------------------------------------------------------------- flagstone path ribbon (UV tiles along its length)
def catmull(pts, steps):
    out = []
    P = [pts[0]] + pts + [pts[-1]]
    for k in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[k - 1], P[k], P[k + 1], P[k + 2]
        for s in range(steps):
            t = s / steps
            t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * ((2 * p1[i]) + (-p0[i] + p2[i]) * t + (2 * p0[i] - 5 * p1[i] + 4 * p2[i] - p3[i]) * t2 +
                                    (-p0[i] + 3 * p1[i] - 3 * p2[i] + p3[i]) * t3) for i in range(2)))
    out.append(pts[-1])
    return out


def build_path(M):
    pts = catmull(PATH, 14)
    across = 6
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new('UVMap')
    rows = []
    dist = 0.0
    for k, (x, z) in enumerate(pts):
        if k > 0:
            dist += math.hypot(x - pts[k - 1][0], z - pts[k - 1][1])
        a = pts[min(k + 1, len(pts) - 1)]
        b = pts[max(k - 1, 0)]
        dx, dz = a[0] - b[0], a[1] - b[1]
        L = math.hypot(dx, dz) or 1
        nx, nz = -dz / L, dx / L
        # the path narrows a little at its far end, as if it continued round the corner
        w = PATH_W * (1 - 0.35 * max(0, (k - len(pts) * 0.8) / (len(pts) * 0.2)))
        row = []
        for c in range(across + 1):
            s = c / across - 0.5
            px, pz = x + nx * s * w, z + nz * s * w
            py = ground(px, pz) + 0.035
            row.append((bm.verts.new(C.U(px, py, pz)), (c / across, dist / PATH_W)))
        rows.append(row)
    for k in range(len(rows) - 1):
        for c in range(across):
            quad = [rows[k][c], rows[k][c + 1], rows[k + 1][c + 1], rows[k + 1][c]]
            f = bm.faces.new([q[0] for q in quad])
            f.normal_update()
            if f.normal.z < 0:
                f.normal_flip()
            lookup = {q[0]: q[1] for q in quad}
            for loop in f.loops:
                loop[uvl].uv = lookup[loop.vert]
    me = bpy.data.meshes.new('front_path')
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new('front_path', me)
    bpy.context.collection.objects.link(o)
    me.materials.append(C.mat('pathmat', (1, 1, 1)))
    C.export_cmemesh(o, os.path.join(OUT, 'front_path.cmemesh'), smooth_angle=60)


builders = dict(gate=build_gate, bridge=build_bridge, path=build_path)
for name, fn in builders.items():
    if 'all' in which or name in which:
        C.reset_scene()
        fn(mats())
