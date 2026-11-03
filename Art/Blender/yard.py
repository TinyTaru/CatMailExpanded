"""Outdoor yard west of the storage corridor's service door + door, signs and poster.
All coordinates are Unity world space (see cme_export.U). Usage:
  blender -b --factory-startup --python yard.py -- [deck|pergola|planters|door|sign|poster|all]...
"""
import bpy, sys, os, math, random
sys.path.insert(0, os.path.dirname(__file__))
import importlib, cme_export as C
importlib.reload(C)
import banana as B
importlib.reload(B)
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Build')
# festoon poles (x, z, height) under the garland's high hanging points (see Yard.cs)
POLES = [(-12.33, -4.0, 3.30), (-8.99, -4.0, 3.30), (-5.76, -4.0, 3.30)]
which = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else ['all']
PREVIEW = os.environ.get('CME_PREVIEW')

X0, X1 = -13.5, -2.45      # deck extent (x)
Z0, Z1 = -9.0, 2.62        # deck extent (z)
TOP = 0.03                 # deck surface height (matches the indoor floor)


def mats():
    P = C.painterly_mat
    return dict(
        plank=P('plank', (0.56, 0.40, 0.28), 0.10, 4, (0.50, 0.36, 0.25)),
        plank2=P('plank2', (0.50, 0.35, 0.24), 0.10, 4, (0.56, 0.41, 0.29)),
        beam=P('beam', (0.47, 0.36, 0.27), 0.08, 4),
        post=P('post', (0.42, 0.33, 0.26), 0.08, 4),
        rail=P('rail', (0.90, 0.86, 0.78), 0.05, 5),
        stripeA=P('stripeA', (0.44, 0.52, 0.76), 0.06, 6),
        stripeB=P('stripeB', (0.94, 0.90, 0.80), 0.05, 6),
        bulb=P('bulb', (1.0, 0.93, 0.72), 0.03, 6),
        wire=P('wire', (0.22, 0.22, 0.25), 0.02, 6),
        soil=P('soil', (0.33, 0.24, 0.18), 0.10, 8),
        leaf=P('leaf', (0.42, 0.60, 0.36), 0.10, 7, (0.36, 0.55, 0.33)),
        leaf2=P('leaf2', (0.52, 0.68, 0.40), 0.10, 7),
        berry=P('berry', (0.86, 0.30, 0.30), 0.06, 8),
        flowerw=P('flowerw', (0.97, 0.95, 0.90), 0.03, 8),
        banana=P('banana', (0.97, 0.84, 0.36), 0.05, 8),
        pot=P('pot', (0.80, 0.48, 0.34), 0.08, 6),
        metal=P('metal', (0.30, 0.38, 0.36), 0.06, 6),
        glass=P('glass', (1.0, 0.90, 0.62), 0.03, 6),
    )


# ---------------------------------------------------------------- deck
def build_deck(M):
    random.seed(4)
    parts = []
    n = int((X1 - X0) / 0.2)
    pw = (X1 - X0) / n
    ZE = 2.90   # planks run on under the dock wall (which starts at z 2.86) so the deck meets the building
    for i in range(n):
        x = X0 + (i + 0.5) * pw
        split = Z0 + (Z1 - Z0) * random.uniform(0.3, 0.7)
        for (za, zb) in ((Z0, split), (split, ZE)):
            m = M['plank'] if random.random() < 0.6 else M['plank2']
            parts.append(C.ubox('plank', (x, TOP - 0.035, (za + zb) / 2), (pw - 0.014, 0.07, zb - za - 0.014), m, 0.012, 2))
    under = C.painterly_mat('under', (0.24, 0.18, 0.14), 0.05, 4)
    parts.append(C.ubox('underlay', ((X0 + X1) / 2, TOP - 0.085, (Z0 + ZE) / 2), (X1 - X0, 0.03, ZE - Z0), under, 0.0))
    parts.append(C.ubox('fasciaW', (X0 - 0.04, TOP - 0.12, (Z0 + Z1) / 2), (0.08, 0.22, Z1 - Z0 + 0.16), M['beam'], 0.01))
    parts.append(C.ubox('fasciaS', ((X0 + X1) / 2, TOP - 0.12, Z0 - 0.04), (X1 - X0 + 0.08, 0.22, 0.08), M['beam'], 0.01))
    for k in range(7):
        z = Z0 + 0.3 + k * 1.85
        parts.append(C.ubox('joist', ((X0 + X1) / 2, TOP - 0.2, z), (X1 - X0, 0.16, 0.14), M['beam'], 0.01))
    for x in (X0 + 0.15, X0 + 3.8, X0 + 7.4, X1 - 0.3):
        for z in (Z0 + 0.15, Z0 + 3.9, Z0 + 7.7, Z1 - 0.3):
            parts.append(C.ucyl('stilt', (x, -0.75, z), 0.13, 1.5, M['post'], 12, 0.01))

    def rail_run(fixed, a, b, along_x):
        length = abs(b - a)
        count = max(2, int(length / 1.7) + 1)
        for k in range(count):
            t = a + (b - a) * k / (count - 1)
            pos = (t, TOP + 0.5, fixed) if along_x else (fixed, TOP + 0.5, t)
            parts.append(C.ubox('rpost', pos, (0.11, 1.0, 0.11), M['rail'], 0.015))
            parts.append(C.ubox('rcap', (pos[0], TOP + 1.025, pos[2]), (0.15, 0.05, 0.15), M['rail'], 0.012))
        mid = (a + b) / 2
        for h, th in ((0.45, 0.06), (0.95, 0.09)):
            if along_x:
                parts.append(C.ubox('rail', (mid, TOP + h, fixed), (length, th, 0.07), M['rail'], 0.012))
            else:
                parts.append(C.ubox('rail', (fixed, TOP + h, mid), (0.07, th, length), M['rail'], 0.012))

    rail_run(X0 + 0.06, Z0 + 0.06, Z1 - 0.1, False)     # west
    rail_run(Z0 + 0.06, X0 + 0.06, X1 - 0.06, True)     # south
    rail_run(X1 - 0.06, Z0 + 0.06, -7.2, False)         # east, south of the building
    # threshold boards under the service door, closing the gap to the corridor floor (which starts at x -2.24)
    for k, z in enumerate((0.84 + 0.07 + i * 0.145 for i in range(14))):
        parts.append(C.ubox('threshold', (-2.335, TOP - 0.03, z), (0.29, 0.06, 0.135), M['plank'] if k % 2 else M['plank2'], 0.008))
    C.finish(parts, 'yard_deck', OUT, 2048)


# ---------------------------------------------------------------- pergola against the dock wall
PX0, PX1 = -12.9, -5.2
PZF, PZB = 0.15, 2.55
HF, HB = 2.65, 3.15


def build_pergola(M):
    parts = []
    for x in (PX0, (PX0 + PX1) / 2, PX1):
        parts.append(C.ubox('ppost', (x, TOP + HF / 2, PZF), (0.16, HF, 0.16), M['post'], 0.02))
        parts.append(C.ubox('pfoot', (x, TOP + 0.06, PZF), (0.26, 0.12, 0.26), M['beam'], 0.02))
    parts.append(C.ubox('pbeam', ((PX0 + PX1) / 2, TOP + HF + 0.08, PZF), (PX1 - PX0 + 0.5, 0.18, 0.16), M['beam'], 0.02))
    parts.append(C.ubox('wallbeam', ((PX0 + PX1) / 2, TOP + HB + 0.05, PZB - 0.03), (PX1 - PX0 + 0.5, 0.16, 0.12), M['beam'], 0.02))
    run = PZB - PZF
    slope = math.degrees(math.atan2(HB - HF, run))
    span = run / math.cos(math.radians(slope))
    midz = (PZF + PZB) / 2
    for k in range(6):
        x = PX0 - 0.15 + (PX1 - PX0 + 0.3) * k / 5
        r = C.ubox('rafter', (x, TOP + (HF + HB) / 2 + 0.2, midz), (0.09, 0.12, span + 0.3), M['beam'], 0.01)
        parts.append(C.urot(r, x_deg=-slope))
    stripes = 14
    width = (PX1 - PX0 + 0.6) / stripes
    for k in range(stripes):
        x = PX0 - 0.3 + width * (k + 0.5)
        m = M['stripeA'] if k % 2 == 0 else M['stripeB']
        s = C.ubox('awning', (x, TOP + (HF + HB) / 2 + 0.29, midz - 0.12), (width + 0.002, 0.03, span + 0.55), m, 0.0)
        parts.append(C.urot(s, x_deg=-slope))
        # scalloped valance
        parts.append(C.ubox('flap', (x, TOP + HF + 0.3, PZF - 0.36), (width + 0.002, 0.22, 0.036), m, 0.0))
        bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=width / 2, depth=0.026, location=C.U(x, TOP + HF + 0.19, PZF - 0.36),
                                            rotation=(math.radians(90), 0, 0))
        f = bpy.context.active_object
        C.assign(f, m)
        parts.append(f)
    C.finish(parts, 'yard_pergola', OUT, 2048)


# ---------------------------------------------------------------- planters, banana plant, lamp post
def build_planters(M):
    import strawberry as SB
    importlib.reload(SB)
    SM = SB.materials()
    rng = random.Random(9)
    parts, plants = [], []
    for cx in (-11.4, -8.2):
        box, greens = SB.planter(cx, Z0 + 0.55, SM, rng, top=TOP, front_z=1)
        parts += box
        plants += greens
    box, greens = window_box(SB, SM, rng)
    parts += box
    plants += greens
    # lamp post near the south-west corner
    lx, lz = X0 + 0.45, Z0 + 1.6
    parts.append(C.ubox('lampbase', (lx, TOP + 0.08, lz), (0.3, 0.16, 0.3), M['metal'], 0.03))
    parts.append(C.ucyl('lamppost', (lx, TOP + 1.2, lz), 0.05, 2.3, M['metal'], 12, 0.01))
    parts.append(C.ubox('lamparm', (lx + 0.22, TOP + 2.3, lz), (0.5, 0.05, 0.05), M['metal'], 0.01))
    parts.append(C.ubox('lantern', (lx + 0.42, TOP + 2.08, lz), (0.22, 0.3, 0.22), M['glass'], 0.03))
    parts.append(C.ubox('lantop', (lx + 0.42, TOP + 2.27, lz), (0.28, 0.06, 0.28), M['metal'], 0.02))
    parts.append(C.ubox('lanbot', (lx + 0.42, TOP + 1.92, lz), (0.26, 0.04, 0.26), M['metal'], 0.01))
    # the plants are thousands of tiny parts: export them palette-coloured; bake only box, soil and lamp
    SB.palette_export(plants, 'yard_strawberries', OUT, random.Random(4))
    for o in [o for o in bpy.data.objects if o.name.startswith('yard_strawberries')]:
        bpy.data.objects.remove(o, do_unlink=True)
    C.finish(parts, 'yard_planters', OUT, 2048)
    if PREVIEW:
        C.reset_scene()
        objs = [C.import_cmemesh(os.path.join(OUT, nm + '.cmemesh'), os.path.join(OUT, 'tex_' + nm + '.png'))
                for nm in ('yard_planters', 'yard_strawberries')]
        C.render_preview(objs[0], PREVIEW, None, res=1400, angle=(65, 0, 250), frame=(C.U(-2.85, 1.45, -3.9), 0.55))


def window_box(SB, SM, rng):
    """A small version of the strawberry planter hanging under the facade window, facing west (-x).
    Built at the origin facing +z, then scaled, turned and moved under the sill."""
    from mathutils import Matrix
    k = 0.6
    box, greens = SB.planter(0.0, 0.0, SM, rng, length=0.95 / k, width=0.24 / k, height=0.2 / k, top=0.0,
                             front_z=1, n_plants=5)
    WX, wy, wz = -2.53, 2.2, -3.9          # facade plane and window centre (see build_facade)
    base = (WX - 0.2, wy - 0.88, wz)       # box bottom centre
    # Unity yaw -90 (front +z -> -x) is +90 about Blender Z
    M = Matrix.Translation(C.U(*base)) @ Matrix.Rotation(math.radians(90), 4, 'Z') @ Matrix.Scale(k, 4)
    bpy.context.view_layer.update()
    for o in box + greens:
        o.matrix_world = M @ o.matrix_world
    # two wall brackets under the box
    for dz in (-0.3, 0.3):
        box.append(C.ubox('bracket_arm', (WX - 0.13, base[1] - 0.03, wz + dz), (0.22, 0.05, 0.05), SM['post'], 0.008))
        box.append(C.ubox('bracket_leg', (WX - 0.03, base[1] - 0.12, wz + dz), (0.05, 0.2, 0.05), SM['post'], 0.008))
    return box, greens


# ---------------------------------------------------------------- service door: frame + hinged leaf
DX = -2.25
DZ0, DZ1 = 0.84, 2.85


def build_door(M):
    P = C.painterly_mat
    frame_m = P('frame', (0.45, 0.40, 0.36), 0.06, 5)
    leaf_m = P('leafdoor', (0.58, 0.54, 0.49), 0.07, 5, (0.55, 0.51, 0.47))
    trim_m = P('trim', (0.50, 0.46, 0.42), 0.05, 5)
    handle_m = P('handle', (0.76, 0.80, 0.87), 0.03, 6)
    win_m = P('win', (0.62, 0.66, 0.88), 0.06, 5, (0.72, 0.70, 0.90))
    T = 0.46
    mid = (DZ0 + DZ1) / 2
    parts = [
        C.ubox('jambS', (DX, 1.38, DZ0 + 0.09), (T, 2.76, 0.18), frame_m, 0.02),
        C.ubox('jambN', (DX, 1.38, DZ1 - 0.09), (T, 2.76, 0.18), frame_m, 0.02),
        C.ubox('head', (DX, 2.82, mid), (T, 0.14, DZ1 - DZ0), frame_m, 0.02),
        C.ubox('top', (DX, 3.58, mid), (T, 0.14, DZ1 - DZ0), frame_m, 0.02),
        C.ubox('tjS', (DX, 3.2, DZ0 + 0.09), (T, 0.62, 0.18), frame_m, 0.02),
        C.ubox('tjN', (DX, 3.2, DZ1 - 0.09), (T, 0.62, 0.18), frame_m, 0.02),
        C.ubox('glass', (DX, 3.2, mid), (0.03, 0.62, DZ1 - DZ0 - 0.3), win_m, 0.0),
    ]
    for k in range(2):
        z = DZ0 + 0.18 + (DZ1 - DZ0 - 0.36) * (k + 1) / 3
        parts.append(C.ubox('mull', (DX, 3.2, z), (0.12, 0.62, 0.06), frame_m, 0.01))
    C.finish(parts, 'door_frame', OUT, 1024)
    # leaf modelled with its hinge (south edge, bottom) at the origin; it spans +z
    C.reset_scene()
    leaf_m = P('leafdoor', (0.58, 0.54, 0.49), 0.07, 5, (0.55, 0.51, 0.47))
    trim_m = P('trim', (0.50, 0.46, 0.42), 0.05, 5)
    handle_m = P('handle', (0.76, 0.80, 0.87), 0.03, 6)
    LW, LH, LT = DZ1 - DZ0 - 0.36, 2.7, 0.07
    parts = [C.ubox('leaf', (0, LH / 2 + 0.02, LW / 2), (LT, LH, LW), leaf_m, 0.015)]
    for (y, h) in ((0.75, 0.95), (2.0, 1.05)):
        for side in (-1, 1):
            parts.append(C.ubox('panel', (side * (LT / 2 + 0.005), y, LW / 2), (0.012, h, LW - 0.36), trim_m, 0.008))
    for side in (-1, 1):
        parts.append(C.ubox('hplate', (side * (LT / 2 + 0.012), 1.25, LW - 0.16), (0.015, 0.28, 0.08), handle_m, 0.008))
        parts.append(C.ubox('lever', (side * (LT / 2 + 0.05), 1.32, LW - 0.22), (0.03, 0.035, 0.18), handle_m, 0.01))
    C.finish(parts, 'door_leaf', OUT, 1024)


# ---------------------------------------------------------------- sign board on posts; face is its own textured quad
def build_sign(M):
    W, H = 1.6, 0.9
    parts = []
    for s in (-1, 1):
        # posts stand in front of the board, over its plain side margin and the art's painted edge
        parts.append(C.ubox('spost', (-0.045, 0.925, s * (W / 2 - 0.005)), (0.09, 1.85, 0.09), M['post'], 0.015))
    parts.append(C.ubox('sback', (0.03, 1.3, 0), (0.05, H + 0.08, W + 0.08), M['beam'], 0.015))
    C.finish(parts, 'sign_frame', OUT, 512)
    C.reset_scene()
    # faces -x (towards a viewer standing at -x); viewer's right is -z
    q = C.uquad('signface', (0.0, 1.3, 0), (0, 0, -W), (0, H, 0), (-1, 0, 0))
    C.export_cmemesh(q, os.path.join(OUT, 'sign_face.cmemesh'))


# ---------------------------------------------------------------- framed poster; face is its own textured quad
def build_poster(M):
    W, H = 0.72, 0.96
    fm = C.painterly_mat('pframe', (0.55, 0.40, 0.28), 0.06, 5)
    parts = [
        C.ubox('pf_l', (0.015, 0, -W / 2), (0.03, H + 0.06, 0.05), fm, 0.008),
        C.ubox('pf_r', (0.015, 0, W / 2), (0.03, H + 0.06, 0.05), fm, 0.008),
        C.ubox('pf_t', (0.015, H / 2, 0), (0.03, 0.05, W + 0.06), fm, 0.008),
        C.ubox('pf_b', (0.015, -H / 2, 0), (0.03, 0.05, W + 0.06), fm, 0.008),
        C.ubox('pf_back', (0.025, 0, 0), (0.01, H, W), fm, 0.0),
    ]
    C.finish(parts, 'poster_frame', OUT, 512)
    C.reset_scene()
    q = C.uquad('posterface', (0.0, 0, 0), (0, 0, -W), (0, H, 0), (-1, 0, 0))
    C.export_cmemesh(q, os.path.join(OUT, 'poster_face.cmemesh'))


# ---------------------------------------------------------------- building facade facing the yard
def build_facade(M):
    P = C.painterly_mat
    siding = P('siding', (0.80, 0.80, 0.87), 0.06, 5, (0.76, 0.77, 0.85))
    siding2 = P('siding2', (0.74, 0.75, 0.83), 0.06, 5)
    trimw = P('trimw', (0.94, 0.92, 0.88), 0.04, 6)
    teal = P('teal', (0.45, 0.66, 0.68), 0.06, 6)
    roof = P('roof', (0.36, 0.40, 0.55), 0.08, 6, (0.32, 0.36, 0.50))
    glass = P('wglass', (0.62, 0.66, 0.88), 0.06, 5, (0.72, 0.70, 0.90))
    parts = []
    WX = -2.53                     # west face plane (just outside the building)
    WZ0, WZ1 = -7.22, 0.80         # up to the service door frame
    SZ = -7.2                      # south face plane
    SX0, SX1 = -2.56, 7.75
    TOPY = 4.55
    # lap siding boards
    k = 0
    y = 0.38
    while y < TOPY - 0.05:
        m = siding if k % 2 == 0 else siding2
        parts.append(C.ubox('sw', (WX, y + 0.1, (WZ0 + WZ1) / 2), (0.05, 0.215, WZ1 - WZ0), m, 0.01))
        parts.append(C.ubox('ss', ((SX0 + SX1) / 2, y + 0.1, SZ), (SX1 - SX0, 0.215, 0.05), m, 0.01))
        y += 0.2
        k += 1
    # striped skirt at the base (like the dock's foundation trim)
    n = 0
    z = WZ0
    while z < WZ1 - 0.01:
        w = min(0.3, WZ1 - z)
        parts.append(C.ubox('skw', (WX - 0.01, 0.2, z + w / 2), (0.06, 0.36, w), teal if n % 2 == 0 else trimw, 0.004))
        z += w
        n += 1
    x = SX0
    while x < SX1 - 0.01:
        w = min(0.3, SX1 - x)
        parts.append(C.ubox('sks', (x + w / 2, 0.2, SZ - 0.01), (w, 0.36, 0.06), teal if n % 2 == 0 else trimw, 0.004))
        x += w
        n += 1
    # corner + top trims
    parts.append(C.ubox('corner', (WX - 0.03, TOPY / 2, SZ - 0.03), (0.16, TOPY, 0.16), trimw, 0.02))
    parts.append(C.ubox('trimtopW', (WX - 0.02, TOPY, (WZ0 + WZ1) / 2), (0.1, 0.12, WZ1 - WZ0 + 0.1), trimw, 0.015))
    parts.append(C.ubox('trimtopS', ((SX0 + SX1) / 2, TOPY, SZ - 0.02), (SX1 - SX0 + 0.1, 0.12, 0.1), trimw, 0.015))
    # (the roof over this corner, with its overhang and fascia, comes from roof.py)
    # a window on the west face to break up the wall
    wz, wy = -3.9, 2.2
    parts.append(C.ubox('wframe', (WX - 0.03, wy, wz), (0.08, 1.2, 1.0), trimw, 0.02))
    parts.append(C.ubox('wglass', (WX - 0.06, wy, wz), (0.03, 1.0, 0.8), glass, 0.0))
    parts.append(C.ubox('wmullv', (WX - 0.08, wy, wz), (0.04, 1.0, 0.05), trimw, 0.005))
    parts.append(C.ubox('wmullh', (WX - 0.08, wy, wz), (0.04, 0.05, 0.8), trimw, 0.005))
    parts.append(C.ubox('wsill', (WX - 0.1, wy - 0.63, wz), (0.2, 0.06, 1.15), trimw, 0.015))
    # (the window box below this window is built with the strawberry planters)
    C.finish(parts, 'yard_facade', OUT, 2048)


builders = dict(banana=lambda M: B.build_banana(M, OUT), poles=lambda M: B.build_poles(POLES, OUT), facade=build_facade, deck=build_deck, pergola=build_pergola, planters=build_planters, door=build_door, sign=build_sign, poster=build_poster)
for name, fn in builders.items():
    if 'all' in which or name in which:
        C.reset_scene()
        fn(mats())
