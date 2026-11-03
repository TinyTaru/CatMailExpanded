"""Planetarium wing west of the pile corridor, across from the break room (see Planetarium.cs).
All coordinates are Unity world space (see cme_export.U). Usage:
  blender -b --factory-startup --python planetarium.py -- [room|sky|all]
Outputs planet_room (+ tex_planet_room, baked) and planet_sky (UV mapped for tex_planet_sky, unlit in game).
"""
import bpy, bmesh, sys, os, math, random
sys.path.insert(0, os.path.dirname(__file__))
import importlib, cme_export as C
importlib.reload(C)
from mathutils import Vector
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Build')
which = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else ['all']
PREVIEW = os.environ.get('CME_PREVIEW')

# interior (keep in sync with Planetarium.cs)
IX0, IX1 = -1.6, 4.25
IZ0, IZ1 = 21.2, 26.9
FY = 0.04            # floor surface (matches the corridor floor)
WH = 3.8             # wall height (the door frame with its transom is 3.65 tall)
TW = 0.2             # outer wall thickness
CX, CZ = (IX0 + IX1) / 2, (IZ0 + IZ1) / 2
RD = 2.7             # dome radius (interior), dome base at WH
EX = 4.43            # corridor wall plane
EXI = EX - 0.04      # exterior pieces stop short of it, so their end faces never z-fight with the corridor wall
DZ0, DZ1, DTOP = 23.0, 25.01, 3.65   # doorway (outer edges of the door frame)
OX0, OZ0, OZ1 = IX0 - TW, IZ0 - TW, IZ1 + TW
ROOF = WH + 0.3


def mats():
    P = C.painterly_mat
    return dict(
        plank=P('plank', (0.30, 0.25, 0.34), 0.10, 4, (0.26, 0.22, 0.31)),
        plank2=P('plank2', (0.27, 0.22, 0.31), 0.10, 4, (0.31, 0.26, 0.35)),
        under=P('under', (0.16, 0.14, 0.18), 0.05, 4),
        wains=P('wains', (0.30, 0.22, 0.32), 0.07, 5, (0.27, 0.20, 0.30)),
        wall=P('wall', (0.20, 0.19, 0.42), 0.07, 4, (0.24, 0.20, 0.46)),
        ceil=P('ceil', (0.14, 0.13, 0.32), 0.06, 4),
        gold=P('gold', (0.86, 0.68, 0.36), 0.06, 7, (0.80, 0.60, 0.30)),
        siding=P('siding', (0.80, 0.80, 0.87), 0.06, 5, (0.76, 0.77, 0.85)),
        siding2=P('siding2', (0.74, 0.75, 0.83), 0.06, 5),
        trimw=P('trimw', (0.94, 0.92, 0.88), 0.04, 6),
        teal=P('teal', (0.45, 0.66, 0.68), 0.06, 6),
        roof=P('roof', (0.36, 0.40, 0.55), 0.08, 6, (0.32, 0.36, 0.50)),
        dome=P('dome', (0.42, 0.62, 0.66), 0.08, 5, (0.36, 0.56, 0.62)),
        rib=P('rib', (0.86, 0.68, 0.36), 0.06, 7),
        brass=P('brass', (0.78, 0.60, 0.30), 0.06, 8, (0.70, 0.52, 0.26)),
        navy=P('navy', (0.18, 0.18, 0.36), 0.05, 6),
        wood=P('wood', (0.42, 0.30, 0.22), 0.08, 5),
    )


def umesh(name, verts, faces, material):
    """Mesh from Unity-space vertices."""
    me = bpy.data.meshes.new(name)
    me.from_pydata([C.U(*v) for v in verts], [], faces)
    me.update()
    o = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(o)
    me.materials.append(material)
    return o


def ring_plate(name, cx, cz, r, x0, x1, z0, z1, y, material, up=True, segs=96):
    """Flat plate filling a rectangle except a circle (ceiling around the dome, roof around the outer dome)."""
    hx0, hx1, hz0, hz1 = x0 - cx, x1 - cx, z0 - cz, z1 - cz
    angs = sorted(set([2 * math.pi * i / segs for i in range(segs)] +
                      [math.atan2(z, x) % (2 * math.pi) for x in (hx0, hx1) for z in (hz0, hz1)]))
    verts, faces = [], []
    for a in angs:
        c, s = math.cos(a), math.sin(a)
        tx = (hx1 if c > 0 else hx0) / c if abs(c) > 1e-6 else 1e9
        tz = (hz1 if s > 0 else hz0) / s if abs(s) > 1e-6 else 1e9
        t = min(tx, tz)
        verts.append((cx + r * c, y, cz + r * s))
        verts.append((cx + t * c, y, cz + t * s))
    n = len(angs)
    for i in range(n):
        a, b = 2 * i, 2 * ((i + 1) % n)
        # Unity space is mirrored on the way to Blender; pick the order that makes the face point the right way
        faces.append((a, a + 1, b + 1, b) if not up else (a, b, b + 1, a + 1))
    o = umesh(name, verts, faces, material)
    me = o.data
    me.update()
    want = 1 if up else -1
    if me.polygons and (me.polygons[0].normal.z * want) < 0:
        bm = bmesh.new(); bm.from_mesh(me)
        bmesh.ops.reverse_faces(bm, faces=bm.faces[:])
        bm.to_mesh(me); bm.free()
    if up is not None:
        sol = o.modifiers.new('solid', 'SOLIDIFY')
        sol.thickness = 0.08
        sol.offset = -1 if up else 1
    return o


def hemisphere(name, cx, y0, cz, r, material, inward, ni=64, nj=20, uv=False):
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new('UVMap') if uv else None
    grid = []
    for j in range(nj + 1):
        el = (math.pi / 2) * j / nj
        row = []
        for i in range(ni + 1):
            az = 2 * math.pi * i / ni
            p = (cx + r * math.cos(el) * math.sin(az), y0 + r * math.sin(el), cz + r * math.cos(el) * math.cos(az))
            row.append(bm.verts.new(C.U(*p)))
        grid.append(row)
    centre = Vector(C.U(cx, y0, cz))
    for j in range(nj):
        for i in range(ni):
            vs = [grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]]
            if j == nj - 1:
                vs = [grid[j][i], grid[j][i + 1], grid[j + 1][i]]
                uvs = [(i / ni, j / nj), ((i + 1) / ni, j / nj), ((i + 0.5) / ni, 1.0)]
            else:
                uvs = [(i / ni, j / nj), ((i + 1) / ni, j / nj), ((i + 1) / ni, (j + 1) / nj), (i / ni, (j + 1) / nj)]
            try:
                f = bm.faces.new(vs)
            except ValueError:
                continue
            f.normal_update()
            out = (f.calc_center_median() - centre).dot(f.normal) > 0
            if out == inward:
                f.normal_flip()
                # flipping reverses the loop order; the uv list follows the loops below by vertex
            if uv:
                lookup = {v: uvc for v, uvc in zip(vs, uvs)}
                for loop in f.loops:
                    loop[uvl].uv = lookup[loop.vert]
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(o)
    me.materials.append(material)
    return o


def rot_about(obj, pivot, x_deg=0.0, y_deg=0.0):
    """Unity-euler rotation (X then Y) about a Unity-space pivot."""
    from mathutils import Matrix
    m = Matrix.Rotation(math.radians(-y_deg), 4, 'Z') @ Matrix.Rotation(math.radians(x_deg), 4, 'X')
    p = Vector(C.U(*pivot))
    obj.matrix_basis = Matrix.Translation(p) @ m @ Matrix.Translation(-p) @ obj.matrix_basis


# ---------------------------------------------------------------- the room
def build_room(M):
    random.seed(11)
    parts = []
    B = C.ubox
    # floor planks running along x, and a thick underlay that hides the hill under the room
    n = int((OZ1 - OZ0) / 0.2)
    pw = (OZ1 - OZ0) / n
    for i in range(n):
        z = OZ0 + (i + 0.5) * pw
        split = IX0 + (IX1 - IX0) * random.uniform(0.3, 0.7)
        for (xa, xb) in ((OX0, split), (split, EX)):
            m = M['plank'] if random.random() < 0.6 else M['plank2']
            parts.append(B('plank', ((xa + xb) / 2, FY - 0.03, z), (xb - xa - 0.012, 0.06, pw - 0.012), m, 0.01, 2))
    parts.append(B('underlay', ((OX0 + EX) / 2, FY - 0.35, (OZ0 + OZ1) / 2), (EX - OX0, 0.6, OZ1 - OZ0), M['under'], 0.0))

    # interior walls: wainscot to 1.0, painted indigo above, gold trims
    def wall_x(x, z0, z1, inward):  # wall face at x spanning z
        th = 0.04
        parts.append(B('wains', (x + inward * th / 2, FY + 0.5, (z0 + z1) / 2), (th, 1.0, z1 - z0), M['wains'], 0.01))
        parts.append(B('wall', (x - inward * 0.02, FY + (WH - FY) / 2, (z0 + z1) / 2), (0.04, WH - FY, z1 - z0), M['wall'], 0.0))
        parts.append(B('trim', (x + inward * 0.05, FY + 1.02, (z0 + z1) / 2), (0.05, 0.05, z1 - z0), M['gold'], 0.01))
        parts.append(B('crown', (x + inward * 0.05, WH - 0.06, (z0 + z1) / 2), (0.08, 0.1, z1 - z0), M['gold'], 0.015))
        parts.append(B('skirt', (x + inward * 0.05, FY + 0.06, (z0 + z1) / 2), (0.06, 0.12, z1 - z0), M['wood'], 0.01))

    def wall_z(z, x0, x1, inward):
        th = 0.04
        parts.append(B('wains', ((x0 + x1) / 2, FY + 0.5, z + inward * th / 2), (x1 - x0, 1.0, th), M['wains'], 0.01))
        parts.append(B('wall', ((x0 + x1) / 2, FY + (WH - FY) / 2, z - inward * 0.02), (x1 - x0, WH - FY, 0.04), M['wall'], 0.0))
        parts.append(B('trim', ((x0 + x1) / 2, FY + 1.02, z + inward * 0.05), (x1 - x0, 0.05, 0.05), M['gold'], 0.01))
        parts.append(B('crown', ((x0 + x1) / 2, WH - 0.06, z + inward * 0.05), (x1 - x0, 0.1, 0.08), M['gold'], 0.015))
        parts.append(B('skirt', ((x0 + x1) / 2, FY + 0.06, z + inward * 0.05), (x1 - x0, 0.12, 0.06), M['wood'], 0.01))

    wall_x(IX0, IZ0, IZ1, +1)
    wall_z(IZ0, IX0, IX1, +1)
    wall_z(IZ1, IX0, IX1, -1)
    # east wall around the doorway
    wall_x(IX1, IZ0, DZ0, -1)
    wall_x(IX1, DZ1, IZ1, -1)
    parts.append(B('wall_over_door', (IX1 - 0.02, (DTOP + WH) / 2, (DZ0 + DZ1) / 2), (0.04, WH - DTOP, DZ1 - DZ0), M['wall'], 0.0))
    parts.append(B('crown_door', (IX1 - 0.05, WH - 0.06, (DZ0 + DZ1) / 2), (0.08, 0.1, DZ1 - DZ0), M['gold'], 0.015))
    # solid east wall body between the room and the corridor (the door frame covers the opening's sides)
    # kept clear of both the interior wall face (IX1) and the corridor's own wall face (EX) so nothing z-fights
    ea, eb = IX1 + 0.05, EX - 0.04
    for (za, zb) in ((OZ0, DZ0), (DZ1, OZ1)):
        parts.append(B('ewall', ((ea + eb) / 2, (FY + ROOF) / 2 - 0.2, (za + zb) / 2), (eb - ea, ROOF - FY + 0.4, zb - za), M['siding2'], 0.0))
    parts.append(B('ewall_top', ((ea + eb) / 2, (DTOP + ROOF) / 2, (DZ0 + DZ1) / 2), (eb - ea, ROOF - DTOP, DZ1 - DZ0), M['siding2'], 0.0))

    # ceiling ring around the dome, gold rim
    parts.append(ring_plate('ceiling', CX, CZ, RD, IX0, IX1, IZ0, IZ1, WH, M['ceil'], up=False))
    bpy.ops.mesh.primitive_torus_add(major_radius=RD, minor_radius=0.06, major_segments=96, minor_segments=8, location=C.U(CX, WH - 0.02, CZ))
    t = bpy.context.active_object; C.assign(t, M['gold']); parts.append(t)
    # brass mount at the apex for the orrery's pole
    parts.append(C.ucyl('apexplate', (CX, WH + RD - 0.04, CZ), 0.22, 0.08, M['brass'], 24, 0.01))

    # ---------------- exterior: lap siding, striped skirt, trims, flat roof with a copper dome
    k = 0
    y = -0.45
    while y < ROOF - 0.1:
        m = M['siding'] if k % 2 == 0 else M['siding2']
        parts.append(B('sw', (OX0, y + 0.1, (OZ0 + OZ1) / 2), (0.05, 0.215, OZ1 - OZ0), m, 0.01))
        parts.append(B('ss', ((OX0 + EXI) / 2, y + 0.1, OZ0), (EXI - OX0, 0.215, 0.05), m, 0.01))
        parts.append(B('sn', ((OX0 + EXI) / 2, y + 0.1, OZ1), (EXI - OX0, 0.215, 0.05), m, 0.01))
        y += 0.2
        k += 1
    # wall cores between siding and interior
    parts.append(B('core_w', ((OX0 + IX0) / 2, (FY + ROOF) / 2, (OZ0 + OZ1) / 2), (IX0 - OX0 - 0.05, ROOF - FY, OZ1 - OZ0), M['siding2'], 0.0))
    parts.append(B('core_s', ((OX0 + EXI) / 2, (FY + ROOF) / 2, (OZ0 + IZ0) / 2), (EXI - OX0, ROOF - FY, IZ0 - OZ0 - 0.05), M['siding2'], 0.0))
    parts.append(B('core_n', ((OX0 + EXI) / 2, (FY + ROOF) / 2, (OZ1 + IZ1) / 2), (EXI - OX0, ROOF - FY, OZ1 - IZ1 - 0.05), M['siding2'], 0.0))
    for (x, z) in ((OX0, OZ0), (OX0, OZ1)):
        parts.append(B('corner', (x - 0.03, (ROOF - 0.45) / 2, z), (0.14, ROOF + 0.45, 0.14), M['trimw'], 0.02))
    parts.append(B('cornice_w', (OX0 - 0.03, ROOF, (OZ0 + OZ1) / 2), (0.14, 0.14, OZ1 - OZ0 + 0.14), M['trimw'], 0.02))
    parts.append(B('cornice_s', ((OX0 + EX) / 2, ROOF, OZ0 - 0.03), (EX - OX0 + 0.14, 0.14, 0.14), M['trimw'], 0.02))
    parts.append(B('cornice_n', ((OX0 + EX) / 2, ROOF, OZ1 + 0.03), (EX - OX0 + 0.14, 0.14, 0.14), M['trimw'], 0.02))
    # striped skirt at the base, like the yard facade
    nn = 0
    for (axis, fixed, a, b) in (('z', OX0 - 0.02, OZ0, OZ1), ('x', OZ0 - 0.02, OX0, EXI), ('x', OZ1 + 0.02, OX0, EXI)):
        p = a
        while p < b - 0.01:
            w = min(0.3, b - p)
            m = M['teal'] if nn % 2 == 0 else M['trimw']
            if axis == 'z':
                parts.append(B('sk', (fixed, -0.15, p + w / 2), (0.06, 0.5, w), m, 0.004))
            else:
                parts.append(B('sk', (p + w / 2, -0.15, fixed), (w, 0.5, 0.06), m, 0.004))
            p += w
            nn += 1
    # flat roof around the dome's drum
    RO = RD + 0.25
    parts.append(ring_plate('roof', CX, CZ, RO, OX0 - 0.1, EX + 0.05, OZ0 - 0.1, OZ1 + 0.1, ROOF + 0.06, M['roof'], up=True))
    # drum and dome
    # an open tube: a capped cylinder would close the dome off from inside
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=RO + 0.05, depth=0.4, end_fill_type='NOTHING', location=C.U(CX, ROOF + 0.2, CZ))
    drum = bpy.context.active_object
    drum.modifiers.new('solid', 'SOLIDIFY').thickness = 0.06
    C.assign(drum, M['trimw'])
    parts.append(drum)
    dome = hemisphere('dome', CX, ROOF + 0.4, CZ, RO, M['dome'], inward=False, ni=48, nj=14)
    parts.append(dome)
    for i in range(12):
        az = 2 * math.pi * i / 12
        # ribs: short boxes following the dome curve
        for j in range(7):
            el = (math.pi / 2) * (j + 0.5) / 7.5
            r = RO + 0.025
            p = (CX + r * math.cos(el) * math.sin(az), ROOF + 0.4 + r * math.sin(el), CZ + r * math.cos(el) * math.cos(az))
            rb = B('rib', p, (0.07, 0.07, RO * (math.pi / 2) / 7.2), M['rib'], 0.01)
            C.urot(rb, x_deg=90 - math.degrees(el), y_deg=math.degrees(az))
            parts.append(rb)
    parts.append(C.ucyl('finial', (CX, ROOF + 0.4 + RO + 0.15, CZ), 0.06, 0.35, M['gold'], 12, 0.01))
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.11, segments=16, ring_count=8, location=C.U(CX, ROOF + 0.4 + RO + 0.36, CZ))
    s = bpy.context.active_object; C.assign(s, M['gold']); parts.append(s)

    # ---------------- telescope on a tripod in the north-east corner, aimed at the dome
    tx, tz = 3.35, 26.05
    for i in range(3):
        a = math.radians(30 + 120 * i)
        leg = B('leg', (tx + 0.22 * math.sin(a), FY + 0.55, tz + 0.22 * math.cos(a)), (0.05, 1.15, 0.05), M['wood'], 0.01)
        C.urot(leg, x_deg=-17, y_deg=math.degrees(a))
        parts.append(leg)
    parts.append(C.ucyl('mount', (tx, FY + 1.13, tz), 0.07, 0.12, M['brass'], 16, 0.01))
    pivot = (tx, FY + 1.42, tz)
    tube = C.ucyl('tube', pivot, 0.085, 1.15, M['navy'], 20, 0.01)
    rot_about(tube, pivot, -52, 225)
    parts.append(tube)
    for (dy, rr, h) in ((0.0, 0.1, 0.08), (0.42, 0.1, 0.06), (-0.5, 0.06, 0.18)):
        band = C.ucyl('band', (tx, FY + 1.42 + dy, tz), rr, h, M['brass'], 20, 0.005)
        rot_about(band, pivot, -52, 225)
        parts.append(band)

    obj, tex = C.finish(parts, 'planet_room', OUT, 2048)
    if PREVIEW:
        C.render_preview(obj, PREVIEW, tex, res=1200, angle=(55, 0, 210))


def build_sky(M):
    sky = C.mat('skymat', (1, 1, 1))
    o = hemisphere('planet_sky', CX, WH - 0.04, CZ, RD - 0.03, sky, inward=True, ni=64, nj=24, uv=True)
    C.export_cmemesh(o, os.path.join(OUT, 'planet_sky.cmemesh'), smooth_angle=89)


builders = dict(room=build_room, sky=build_sky)
for name, fn in builders.items():
    if 'all' in which or name in which:
        C.reset_scene()
        fn(mats())
