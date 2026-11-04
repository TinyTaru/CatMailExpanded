"""The mirror room west of the extended pile corridor (see MirrorRoom.cs), and the exterior of the whole new wing
(siding and a striped foundation skirt for the corridor's new stretch and the gift wrapping room, see PileWing.cs).
All coordinates are Unity world space (see cme_export.U). Usage:
  blender -b --factory-startup --python mirror.py -- [room|exterior|all]
Outputs mirror_room (+ tex_mirror_room, baked), wing_exterior (+ tex_wing_exterior, baked) and mirror_lights.txt
(chandelier and sconce candle positions, lit at runtime).
"""
import bpy, bmesh, sys, os, math, random
sys.path.insert(0, os.path.dirname(__file__))
import importlib, cme_export as C
importlib.reload(C)
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Build')
which = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else ['all']
PREVIEW = os.environ.get('CME_PREVIEW')

# interior (keep in sync with MirrorRoom.cs)
IX0, IX1 = -1.6, 4.25
IZ0, IZ1 = 27.35, 36.4
FY = 0.04
WH = 3.8
TW = 0.2
OX0, OZ0, OZ1 = IX0 - TW, IZ0 - TW, IZ1 + TW
EX = 4.43                      # corridor west wall plane
EXI = EX - 0.04
DZ0, DZ1, DTOP = 34.04, 36.01, 3.65   # doorway (outer edges of the door frame)
ROOF = WH + 0.3
CX, CZ = (IX0 + IX1) / 2, (IZ0 + IZ1) / 2
# mirrors: glass rectangles (the glass itself is drawn at runtime from a camera)
MW = dict(x=IX0 + 0.06, z=31.875, w=3.0, y0=0.55, y1=2.95)      # west wall, facing +x
MN = dict(z=IZ1 - 0.06, x=1.325, w=2.6, y0=0.55, y1=2.95)       # north wall, facing -z

# the new wing's outer walls (PileWing.cs)
CX0, CX1, END = 4.43, 7.69, 38.40
RX1, RZ0, RZ1 = 12.31, 25.82, 33.66


def mats():
    P = C.painterly_mat
    return dict(
        parquet=P('parquet', (0.36, 0.22, 0.16), 0.10, 4, (0.32, 0.19, 0.14)),
        parquet2=P('parquet2', (0.42, 0.27, 0.19), 0.10, 4, (0.38, 0.24, 0.17)),
        under=P('under', (0.16, 0.12, 0.12), 0.05, 4),
        wains=P('wains', (0.30, 0.17, 0.24), 0.07, 5, (0.27, 0.15, 0.22)),
        panel=P('panel', (0.36, 0.21, 0.29), 0.07, 5),
        wall=P('wall', (0.42, 0.25, 0.46), 0.09, 7, (0.36, 0.21, 0.42)),
        ceil=P('ceil', (0.26, 0.17, 0.30), 0.06, 4),
        gold=P('gold', (0.88, 0.70, 0.36), 0.07, 7, (0.80, 0.60, 0.30)),
        gold2=P('gold2', (0.74, 0.56, 0.26), 0.07, 7),
        velvet=P('velvet', (0.50, 0.12, 0.22), 0.10, 5, (0.42, 0.09, 0.18)),
        velvet2=P('velvet2', (0.40, 0.09, 0.18), 0.10, 5),
        rug=P('rug', (0.30, 0.20, 0.42), 0.07, 5, (0.26, 0.17, 0.38)),
        rugborder=P('rugborder', (0.86, 0.68, 0.38), 0.06, 6),
        silver=P('silver', (0.80, 0.84, 0.90), 0.05, 6, (0.70, 0.76, 0.86)),
        crystal=P('crystal', (0.86, 0.90, 0.98), 0.04, 6),
        wood=P('wood', (0.36, 0.22, 0.16), 0.08, 5),
        black=P('black', (0.16, 0.14, 0.18), 0.05, 5),
        cream=P('cream', (0.94, 0.90, 0.82), 0.05, 6),
        siding=P('siding', (0.80, 0.80, 0.87), 0.06, 5, (0.76, 0.77, 0.85)),
        siding2=P('siding2', (0.74, 0.75, 0.83), 0.06, 5),
        trimw=P('trimw', (0.94, 0.92, 0.88), 0.04, 6),
        teal=P('teal', (0.45, 0.66, 0.68), 0.06, 6),
        stone=P('stone', (0.56, 0.55, 0.58), 0.10, 6, (0.50, 0.50, 0.54)),
    )


def rot_about(obj, pivot, x_deg=0.0, y_deg=0.0):
    from mathutils import Matrix, Vector
    m = Matrix.Rotation(math.radians(-y_deg), 4, 'Z') @ Matrix.Rotation(math.radians(x_deg), 4, 'X')
    p = Vector(C.U(*pivot))
    obj.matrix_basis = Matrix.Translation(p) @ m @ Matrix.Translation(-p) @ obj.matrix_basis


def sphere(name, center, r, material, segs=12, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, segments=segs, ring_count=rings, location=C.U(*center))
    s = bpy.context.active_object
    s.name = name
    C.assign(s, material)
    return s


def octa(name, center, r, h, material):
    """A crystal drop: two cones base to base."""
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=r, radius2=0, depth=h * 0.35, location=C.U(center[0], center[1] + h * 0.175, center[2]))
    a = bpy.context.active_object
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=r, radius2=0, depth=h * 0.65, location=C.U(center[0], center[1] - h * 0.325, center[2]), rotation=(math.pi, 0, 0))
    b = bpy.context.active_object
    for o in (a, b):
        C.assign(o, material)
    return [a, b]


# ---------------------------------------------------------------- the room
def build_room(M):
    random.seed(17)
    parts = []
    B = C.ubox
    lights = []

    # --- herringbone-ish parquet: short boards in alternating directions, in strips along x
    strip = 0.42
    z = OZ0
    k = 0
    while z < OZ1 - 0.01:
        zb = min(z + strip, OZ1)
        x = OX0
        j = 0
        while x < EX - 0.01:
            xb = min(x + 0.62, EX)
            m = M['parquet'] if (j + k) % 2 == 0 else M['parquet2']
            parts.append(B('pq', ((x + xb) / 2, FY - 0.03, (z + zb) / 2), (xb - x - 0.01, 0.06, zb - z - 0.01), m, 0.008, 2))
            x = xb
            j += 1
        z = zb
        k += 1
    parts.append(B('underlay', ((OX0 + EX) / 2, FY - 0.35, (OZ0 + OZ1) / 2), (EX - OX0, 0.6, OZ1 - OZ0), M['under'], 0.0))

    # --- walls: dark panelled wainscot to 1.05, plum wallpaper above, gold rails and crown
    def wall(axis, fixed, a, b, inward, gap=None):
        th = 0.04
        mid = (a + b) / 2
        L = b - a

        def box(n, off, y, size_along, h, depth, m, bev, c=None):
            c = mid if c is None else c
            if axis == 'x':
                parts.append(B(n, (fixed + inward * off, y, c), (depth, h, size_along), m, bev))
            else:
                parts.append(B(n, (c, y, fixed + inward * off), (size_along, h, depth), m, bev))
        box('wains', th / 2, FY + 0.525, L, 1.05, th, M['wains'], 0.01)
        box('wall', -0.02, FY + (WH - FY) / 2, L, WH - FY, 0.04, M['wall'], 0.0)
        # the gold rail stops at a mirror's frame (it would stand in front of the glass)
        for (ra, rb) in (((a, gap[0]), (gap[1], b)) if gap else ((a, b),)):
            box('rail', 0.055, FY + 1.06, rb - ra, 0.06, 0.06, M['gold'], 0.012, (ra + rb) / 2)
        box('crown', 0.06, WH - 0.07, L, 0.12, 0.1, M['gold'], 0.02)
        box('skirt', 0.05, FY + 0.07, L, 0.14, 0.06, M['wood'], 0.01)
        # raised panels in the wainscot
        n = max(1, int(L / 0.75))
        pw = L / n
        for i in range(n):
            c = a + (i + 0.5) * pw
            if axis == 'x':
                parts.append(B('panel', (fixed + inward * 0.05, FY + 0.56, c), (0.02, 0.62, pw - 0.18), M['panel'], 0.012))
            else:
                parts.append(B('panel', (c, FY + 0.56, fixed + inward * 0.05), (pw - 0.18, 0.62, 0.02), M['panel'], 0.012))

    wall('x', IX0, IZ0, IZ1, +1, (MW['z'] - MW['w'] / 2 - 0.16, MW['z'] + MW['w'] / 2 + 0.16))
    wall('z', IZ0, IX0, IX1, +1)
    wall('z', IZ1, IX0, IX1, -1, (MN['x'] - MN['w'] / 2 - 0.16, MN['x'] + MN['w'] / 2 + 0.16))
    wall('x', IX1, IZ0, DZ0, -1)
    wall('x', IX1, DZ1, IZ1, -1)
    parts.append(B('wall_over_door', (IX1 - 0.02, (DTOP + WH) / 2, (DZ0 + DZ1) / 2), (0.04, WH - DTOP, DZ1 - DZ0), M['wall'], 0.0))
    parts.append(B('crown_door', (IX1 - 0.06, WH - 0.07, (DZ0 + DZ1) / 2), (0.1, 0.12, DZ1 - DZ0), M['gold'], 0.02))
    # solid wall body between the room and the corridor (kept clear of both wall faces)
    ea, eb = IX1 + 0.05, EX - 0.04
    for (za, zb) in ((OZ0, DZ0), (DZ1, OZ1)):
        parts.append(B('ewall', ((ea + eb) / 2, (FY + ROOF) / 2 - 0.2, (za + zb) / 2), (eb - ea, ROOF - FY + 0.4, zb - za), M['siding2'], 0.0))
    parts.append(B('ewall_top', ((ea + eb) / 2, (DTOP + ROOF) / 2, (DZ0 + DZ1) / 2), (eb - ea, ROOF - DTOP, DZ1 - DZ0), M['siding2'], 0.0))

    # --- coffered ceiling
    parts.append(B('ceiling', ((OX0 + EX) / 2, WH + 0.04, (OZ0 + OZ1) / 2), (EX - OX0, 0.08, OZ1 - OZ0), M['ceil'], 0.0))
    nx, nz = 4, 6
    for i in range(1, nx):
        x = IX0 + (IX1 - IX0) * i / nx
        parts.append(B('beam', (x, WH - 0.07, CZ), (0.12, 0.14, IZ1 - IZ0), M['gold2'], 0.015))
    for i in range(1, nz):
        z = IZ0 + (IZ1 - IZ0) * i / nz
        parts.append(B('beam', (CX, WH - 0.07, z), (IX1 - IX0, 0.14, 0.12), M['gold2'], 0.015))
    # ceiling rose for the chandelier
    parts.append(C.ucyl('rose', (CX, WH - 0.02, CZ), 0.3, 0.06, M['gold'], 24, 0.01))

    # --- mirror frames (gilded, with a shell crest and corner rosettes)
    def frame_x(m):
        x, zc, w, y0, y1 = m['x'], m['z'], m['w'], m['y0'], m['y1']
        fw, fd = 0.16, 0.09
        za, zb = zc - w / 2, zc + w / 2
        parts.append(B('fr_b', (x + fd / 2 - 0.02, y0 - fw / 2, zc), (fd, fw, w + 2 * fw), M['gold'], 0.03))
        parts.append(B('fr_t', (x + fd / 2 - 0.02, y1 + fw / 2, zc), (fd, fw, w + 2 * fw), M['gold'], 0.03))
        parts.append(B('fr_l', (x + fd / 2 - 0.02, (y0 + y1) / 2, za - fw / 2), (fd, y1 - y0, fw), M['gold'], 0.03))
        parts.append(B('fr_r', (x + fd / 2 - 0.02, (y0 + y1) / 2, zb + fw / 2), (fd, y1 - y0, fw), M['gold'], 0.03))
        parts.append(B('fr_in', (x + 0.005, (y0 + y1) / 2, zc), (0.02, y1 - y0 + 0.06, w + 0.06), M['gold2'], 0.005))
        for (yy, zz) in ((y0 - fw / 2, za - fw / 2), (y0 - fw / 2, zb + fw / 2), (y1 + fw / 2, za - fw / 2), (y1 + fw / 2, zb + fw / 2)):
            parts.append(sphere('ros', (x + 0.08, yy, zz), 0.09, M['gold']))
        crest(lambda dy, dz: (x + 0.06, y1 + fw + dy, zc + dz), 'x')

    def frame_z(m):
        z, xc, w, y0, y1 = m['z'], m['x'], m['w'], m['y0'], m['y1']
        fw, fd = 0.16, 0.09
        xa, xb = xc - w / 2, xc + w / 2
        parts.append(B('fr_b', (xc, y0 - fw / 2, z - fd / 2 + 0.02), (w + 2 * fw, fw, fd), M['gold'], 0.03))
        parts.append(B('fr_t', (xc, y1 + fw / 2, z - fd / 2 + 0.02), (w + 2 * fw, fw, fd), M['gold'], 0.03))
        parts.append(B('fr_l', (xa - fw / 2, (y0 + y1) / 2, z - fd / 2 + 0.02), (fw, y1 - y0, fd), M['gold'], 0.03))
        parts.append(B('fr_r', (xb + fw / 2, (y0 + y1) / 2, z - fd / 2 + 0.02), (fw, y1 - y0, fd), M['gold'], 0.03))
        parts.append(B('fr_in', (xc, (y0 + y1) / 2, z - 0.005), (w + 0.06, y1 - y0 + 0.06, 0.02), M['gold2'], 0.005))
        for (yy, xx) in ((y0 - fw / 2, xa - fw / 2), (y0 - fw / 2, xb + fw / 2), (y1 + fw / 2, xa - fw / 2), (y1 + fw / 2, xb + fw / 2)):
            parts.append(sphere('ros', (xx, yy, z - 0.08), 0.09, M['gold']))
        crest(lambda dy, dx: (xc + dx, y1 + fw + dy, z - 0.06), 'z')

    def crest(at, axis):
        # a fan of rounded ribs like a shell, with a pearl
        for i in range(7):
            a = math.radians(-60 + 20 * i)
            L = 0.32 if i % 2 == 0 else 0.26
            p = at(L * 0.5 * math.cos(a), L * 0.5 * math.sin(a))
            rib = B('crest', p, (0.05, L, 0.05) if axis == 'x' else (0.05, L, 0.05), M['gold'], 0.02)
            if axis == 'x':
                C.urot(rib, x_deg=-math.degrees(a))
            else:
                C.urot(rib, z_deg=math.degrees(a))
            parts.append(rib)
        parts.append(sphere('pearl', at(0.02, 0), 0.07, M['cream']))

    frame_x(MW)
    frame_z(MN)

    # --- velvet curtains either side of the big mirror, with a valance
    def drape(x, zc, side):
        for i in range(5):
            r = 0.07 + 0.02 * (i % 2)
            z = zc + side * i * 0.11
            o = C.ucyl('drape', (x + 0.12, (FY + 3.35) / 2, z), r, 3.35 - FY, M['velvet'] if i % 2 == 0 else M['velvet2'], 10)
            parts.append(o)
        parts.append(C.ucyl('tie', (x + 0.16, 1.15, zc + side * 0.22), 0.27, 0.07, M['gold'], 14))
    drape(IX0, MW['z'] - MW['w'] / 2 - 0.42, -1)
    drape(IX0, MW['z'] + MW['w'] / 2 + 0.42, +1)
    parts.append(B('valance', (IX0 + 0.14, 3.42, MW['z']), (0.16, 0.3, MW['w'] + 1.9), M['velvet'], 0.04))
    parts.append(B('valance_trim', (IX0 + 0.2, 3.26, MW['z']), (0.06, 0.05, MW['w'] + 1.9), M['gold'], 0.01))

    # --- chandelier: chain, a gilded ring with arms and candle cups, crystal drops
    top = WH - 0.05
    cy = 2.85
    parts.append(C.ucyl('chain', (CX, (top + cy + 0.3) / 2, CZ), 0.025, top - cy - 0.3, M['gold'], 8))
    bpy.ops.mesh.primitive_torus_add(major_radius=0.55, minor_radius=0.035, major_segments=40, minor_segments=8, location=C.U(CX, cy, CZ))
    t = bpy.context.active_object
    C.assign(t, M['gold'])
    parts.append(t)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.3, minor_radius=0.03, major_segments=32, minor_segments=8, location=C.U(CX, cy + 0.25, CZ))
    t = bpy.context.active_object
    C.assign(t, M['gold'])
    parts.append(t)
    parts.append(sphere('hub', (CX, cy + 0.12, CZ), 0.14, M['gold']))
    parts += octa('drop', (CX, cy - 0.3, CZ), 0.09, 0.4, M['crystal'])
    for i in range(8):
        a = 2 * math.pi * i / 8
        px, pz = CX + 0.55 * math.cos(a), CZ + 0.55 * math.sin(a)
        parts.append(C.ucyl('cup', (px, cy + 0.06, pz), 0.06, 0.06, M['gold'], 12))
        parts.append(C.ucyl('candle', (px, cy + 0.17, pz), 0.025, 0.16, M['cream'], 10))
        lights.append((px, cy + 0.29, pz))
        parts += octa('drop', (px, cy - 0.14, pz), 0.04, 0.16, M['crystal'])
        # an arm from the hub
        arm = C.ubox('arm', ((CX + px) / 2, cy + 0.05, (CZ + pz) / 2), (0.04, 0.04, 0.55), M['gold'], 0.01)
        C.urot(arm, y_deg=90 - math.degrees(a))
        parts.append(arm)
    # wall sconces on the east wall and the north wall's sides
    for (x, z, n) in ((IX1, 29.75, (-1, 0)), (IX1, 32.1, (-1, 0)), (MN['x'] - MN['w'] / 2 - 0.65, IZ1, (0, -1)), (MN['x'] + MN['w'] / 2 + 0.65, IZ1, (0, -1))):
        bx, bz = x + n[0] * 0.04, z + n[1] * 0.04
        parts.append(B('sc_plate', (bx, 2.05, bz), (0.05 if n[0] else 0.16, 0.24, 0.16 if n[0] else 0.05), M['gold'], 0.015))
        cx_, cz_ = x + n[0] * 0.16, z + n[1] * 0.16
        arm = B('sc_arm', ((bx + cx_) / 2, 2.0, (bz + cz_) / 2), (abs(cx_ - bx) + 0.04, 0.04, abs(cz_ - bz) + 0.04), M['gold'], 0.01)
        parts.append(arm)
        parts.append(C.ucyl('sc_cup', (cx_, 2.04, cz_), 0.05, 0.05, M['gold'], 12))
        parts.append(C.ucyl('sc_candle', (cx_, 2.14, cz_), 0.022, 0.14, M['cream'], 10))
        lights.append((cx_, 2.25, cz_))

    # --- big oval rug in the middle
    def oval(name, rx, rz, y, m, segs=48):
        verts = [(CX, y, CZ)] + [(CX + rx * math.cos(2 * math.pi * i / segs), y, CZ + rz * math.sin(2 * math.pi * i / segs)) for i in range(segs)]
        faces = [(0, 1 + (i + 1) % segs, 1 + i) for i in range(segs)]
        me = bpy.data.meshes.new(name)
        me.from_pydata([C.U(*v) for v in verts], [], faces)
        me.update()
        o = bpy.data.objects.new(name, me)
        bpy.context.collection.objects.link(o)
        me.materials.append(m)
        if me.polygons[0].normal.z < 0:
            bm = bmesh.new(); bm.from_mesh(me)
            bmesh.ops.reverse_faces(bm, faces=bm.faces[:])
            bm.to_mesh(me); bm.free()
        o.modifiers.new('s', 'SOLIDIFY').thickness = 0.012
        return o
    parts.append(oval('rug_border', 1.95, 2.9, FY + 0.012, M['rugborder']))
    parts.append(oval('rug', 1.8, 2.75, FY + 0.02, M['rug']))
    parts.append(oval('rug_star', 0.5, 0.75, FY + 0.028, M['rugborder'], 6))

    # --- dressing table with a round vanity mirror and a stool, south-west corner, in the 1.04 m gap between the
    # west wall's south shelf unit (x <= -0.85) and the south wall's cubby unit (x >= 0.19) (MirrorRoom.cs)
    tx, tz = -0.33, IZ0 + 0.35
    parts.append(B('dt_top', (tx, 0.78, tz), (0.84, 0.06, 0.48), M['wood'], 0.015))
    for (dx, dz) in ((-0.38, -0.18), (0.38, -0.18), (-0.38, 0.18), (0.38, 0.18)):
        parts.append(B('dt_leg', (tx + dx, 0.41, tz + dz), (0.05, 0.74, 0.05), M['wood'], 0.01))
    parts.append(B('dt_drawer', (tx, 0.68, tz + 0.21), (0.68, 0.14, 0.06), M['wood'], 0.01))
    parts.append(sphere('dt_knob', (tx, 0.68, tz + 0.25), 0.025, M['gold']))
    rim = C.ucyl('vanity_rim', (tx, 1.32, tz - 0.12), 0.34, 0.05, M['gold'], 32)
    C.urot(rim, x_deg=90)
    parts.append(rim)
    glass = C.ucyl('vanity_glass', (tx, 1.32, tz - 0.09), 0.29, 0.02, M['silver'], 32)
    C.urot(glass, x_deg=90)
    parts.append(glass)
    parts.append(B('vanity_post', (tx, 0.98, tz - 0.12), (0.05, 0.38, 0.05), M['gold'], 0.01))
    parts.append(C.ucyl('stool', (tx, 0.45, tz + 0.62), 0.22, 0.12, M['velvet'], 20, 0.03))
    for i in range(3):
        a = 2 * math.pi * i / 3
        parts.append(B('stool_leg', (tx + 0.14 * math.cos(a), 0.2, tz + 0.62 + 0.14 * math.sin(a)), (0.04, 0.4, 0.04), M['wood'], 0.01))
    # perfume bottles and a powder box
    for (dx, h, m) in ((-0.3, 0.14, M['crystal']), (-0.21, 0.1, M['velvet']), (0.28, 0.08, M['gold'])):
        parts.append(C.ucyl('bottle', (tx + dx, 0.81 + h / 2, tz + 0.05), 0.04, h, m, 10))

    # --- coat stand with a top hat, in the gap between the west wall's north shelf unit (MirrorRoom.cs, it reaches
    # x -0.88) and the north mirror's frame, far enough out from the wall that the sconce never seems to sit on the hat
    sx, sz = -0.5, IZ1 - 0.85
    parts.append(C.ucyl('stand_pole', (sx, 0.95, sz), 0.03, 1.8, M['wood'], 10))
    parts.append(C.ucyl('stand_base', (sx, 0.06, sz), 0.24, 0.06, M['wood'], 16))
    for i in range(4):
        a = 2 * math.pi * i / 4
        hk = C.ubox('hook', (sx + 0.1 * math.cos(a), 1.72, sz + 0.1 * math.sin(a)), (0.03, 0.03, 0.2), M['gold'], 0.005)
        C.urot(hk, x_deg=35, y_deg=90 - math.degrees(a))
        parts.append(hk)
    parts.append(C.ucyl('hat_brim', (sx, 1.86, sz), 0.2, 0.025, M['black'], 20))
    parts.append(C.ucyl('hat_crown', (sx, 1.99, sz), 0.12, 0.24, M['black'], 20))
    parts.append(C.ucyl('hat_band', (sx, 1.91, sz), 0.123, 0.04, M['velvet'], 20))

    obj, tex = C.finish(parts, 'mirror_room', OUT, 2048)
    with open(os.path.join(OUT, 'mirror_lights.txt'), 'w') as f:
        for p in lights:
            f.write('%.3f %.3f %.3f\n' % p)
    if PREVIEW:
        C.render_preview(obj, PREVIEW, tex, res=1200, angle=(55, 0, 210))


# ---------------------------------------------------------------- the wing's exterior
def build_exterior(M):
    """Lap siding on every outward wall of the new wing, a white cornice, and a striped skirt down to a stone
    plinth (the wing reaches the shore, where the ground falls away)."""
    parts = []
    B = C.ubox
    GAP = 0.05          # siding stands this far out from the wall's own outer face
    BOT = -1.6          # plinth bottom (below the waterline)
    TOPS = 4.2          # siding up to the ceiling slab's top

    # (axis, fixed coordinate of the siding, from, to, outward sign)
    runs = [
        # mirror room
        ('x', OX0 - GAP, OZ0, OZ1 + GAP, -1),
        ('z', OZ1 + GAP, OX0 - GAP, EX - 0.2, +1),
        # corridor: west wall beyond the mirror room, end wall, east wall beyond the returns room
        ('x', EX - 0.2 - GAP, OZ1 + GAP, END + 0.05 + GAP, -1),
        # (the end wall keeps its opening, where the bottomless pile pours in: siding on either side and above)
        ('z', END + 0.05 + GAP, EX - 0.2 - GAP, 4.77, +1),
        ('z', END + 0.05 + GAP, 7.45, CX1 + 0.2 + GAP, +1),
        ('z', END + 0.05 + GAP, 4.77, 7.45, +1, 3.28),
        ('x', CX1 + 0.2 + GAP, RZ1 + 0.02 + GAP, END + 0.05 + GAP, +1),
        # returns room
        ('z', RZ1 + 0.02 + GAP, CX1 + 0.2 + GAP, RX1 + GAP, +1),
        ('x', RX1 + GAP, RZ0 + 0.1, RZ1 + 0.02 + GAP, +1),
    ]
    k = 0
    for run in runs:
        axis, f, a, b, out = run[:5]
        y = run[5] if len(run) > 5 else 0.1
        L = b - a
        mid = (a + b) / 2
        row = 0
        while y < TOPS - 0.05:
            m = M['siding'] if row % 2 == 0 else M['siding2']
            h = min(0.215, TOPS - y + 0.015)
            if axis == 'x':
                parts.append(B('sd', (f + out * 0.025, y + h / 2, mid), (0.05, h, L), m, 0.01))
            else:
                parts.append(B('sd', (mid, y + h / 2, f + out * 0.025), (L, h, 0.05), m, 0.01))
            y += 0.2
            row += 1
        # cornice
        if axis == 'x':
            parts.append(B('cornice', (f + out * 0.06, TOPS, mid), (0.14, 0.14, L + 0.14), M['trimw'], 0.02))
        else:
            parts.append(B('cornice', (mid, TOPS, f + out * 0.06), (L + 0.14, 0.14, 0.14), M['trimw'], 0.02))
        # striped skirt and stone plinth
        p = a
        while p < b - 0.01:
            w = min(0.3, b - p)
            m = M['teal'] if k % 2 == 0 else M['trimw']
            if axis == 'x':
                parts.append(B('sk', (f + out * 0.03, -0.15, p + w / 2), (0.06, 0.5, w), m, 0.004))
            else:
                parts.append(B('sk', (p + w / 2, -0.15, f + out * 0.03), (w, 0.5, 0.06), m, 0.004))
            p += w
            k += 1
        if axis == 'x':
            parts.append(B('plinth', (f + out * 0.04, (BOT - 0.4) / 2, mid), (0.1, -0.4 - BOT, L + 0.1), M['stone'], 0.01))
        else:
            parts.append(B('plinth', (mid, (BOT - 0.4) / 2, f + out * 0.04), (L + 0.1, -0.4 - BOT, 0.1), M['stone'], 0.01))
    # corner boards where two runs meet
    for (x, z) in ((OX0 - GAP, OZ0), (OX0 - GAP, OZ1 + GAP), (EX - 0.2 - GAP, OZ1 + GAP), (EX - 0.2 - GAP, END + 0.05 + GAP),
                   (CX1 + 0.2 + GAP, END + 0.05 + GAP), (CX1 + 0.2 + GAP, RZ1 + 0.02 + GAP), (RX1 + GAP, RZ1 + 0.02 + GAP)):
        parts.append(B('corner', (x, (TOPS + 0.1) / 2, z), (0.14, TOPS - 0.1, 0.14), M['trimw'], 0.02))
    # the end wall's opening (where the bottomless pile pours in) gets a white frame on the outside
    oz = END + 0.05 + GAP + 0.05
    for xx in (4.85, 7.37):
        parts.append(B('openframe', (xx, 1.6, oz), (0.16, 3.2, 0.1), M['trimw'], 0.02))
    parts.append(B('openframe', (6.11, 3.2, oz), (2.68, 0.16, 0.1), M['trimw'], 0.02))
    obj, tex = C.finish(parts, 'wing_exterior', OUT, 2048)
    if PREVIEW:
        C.render_preview(obj, PREVIEW, tex, res=1200, angle=(55, 0, 150))


builders = dict(room=build_room, exterior=build_exterior)
for name, fn in builders.items():
    if 'all' in which or name in which:
        C.reset_scene()
        fn(mats())
