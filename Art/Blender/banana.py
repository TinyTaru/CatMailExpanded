"""Banana plant in a terracotta pot and the festoon poles for the yard (imported by yard.py)."""
import bpy, bmesh, math, random
import cme_export as C
from mathutils import Vector

TOP = 0.03


def leaf_mesh(name, base, yaw_deg, length, width, rise, droop, curl, material, tear=0.0):
    """A banana leaf in Unity space: a long blade arching out from `base` along `yaw_deg`,
    folded along the midrib, with a slight curl and optional tears along one edge."""
    yaw = math.radians(yaw_deg)
    fwd = Vector((math.sin(yaw), 0, math.cos(yaw)))
    side = Vector((math.cos(yaw), 0, -math.sin(yaw)))
    up = Vector((0, 1, 0))
    n = 14
    rows = []
    for i in range(n + 1):
        t = i / n
        centre = Vector(base) + fwd * (length * t) + up * (rise * math.sin(math.pi * t * 0.75) - droop * t * t)
        w = width * (math.sin(math.pi * min(1.0, t * 1.05)) ** 0.6) * (1.0 - 0.15 * t)
        if t < 0.06:
            w = width * 0.12
        fold = 0.18 * w  # the midrib sits lower than the edges: a soft V
        twist = curl * t
        s = side * math.cos(twist) + up * math.sin(twist)
        ripple = 0.0
        if tear and 0.25 < t < 0.9 and i % 3 == 0:
            ripple = -tear * w
        left = centre - s * (w + ripple) + up * fold
        right = centre + s * w + up * fold
        rows.append((left, centre, right))
    me = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    bm = bmesh.new()
    vrows = [[bm.verts.new(C.U(*p)) for p in r] for r in rows]
    for i in range(n):
        for j in range(2):
            bm.faces.new((vrows[i][j], vrows[i][j + 1], vrows[i + 1][j + 1], vrows[i + 1][j]))
    bm.normal_update()
    bm.to_mesh(me)
    bm.free()
    me.uv_layers.new(name='UVMap')
    me.materials.append(material)
    sol = obj.modifiers.new('solid', 'SOLIDIFY')
    sol.thickness = 0.012
    sol.offset = 0
    return obj


def tube(name, pts, radii, material, sides=8, tip_mat=None, tip_from=None):
    """A tube along Unity-space points with per-point radius; optional second material on the last segments.
    Ring frames are parallel-transported along the path so consecutive rings never twist, and each face's
    winding is set explicitly so its normal points away from the tube's axis."""
    me = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    me.materials.append(material)
    if tip_mat:
        me.materials.append(tip_mat)
    # work in Blender space so the winding test matches the final geometry
    P = [Vector(C.U(*p)) for p in pts]
    tangents = []
    for i in range(len(P)):
        d = P[min(i + 1, len(P) - 1)] - P[max(i - 1, 0)]
        tangents.append(d.normalized())
    frames = []
    n0 = tangents[0].orthogonal().normalized()
    for i, t in enumerate(tangents):
        if i > 0:
            # parallel transport: remove the component along the new tangent
            n0 = (n0 - t * n0.dot(t))
            if n0.length < 1e-6:
                n0 = t.orthogonal()
            n0.normalize()
        frames.append((n0.copy(), t.cross(n0).normalized()))
    bm = bmesh.new()
    rings = []
    for i, p in enumerate(P):
        a_, b_ = frames[i]
        ring = []
        for k in range(sides):
            ang = k / sides * math.tau
            ring.append(bm.verts.new(p + (a_ * math.cos(ang) + b_ * math.sin(ang)) * radii[i]))
        rings.append(ring)

    def add_face(verts, outward, mat_index):
        f = bm.faces.new(verts)
        f.normal_update()
        if f.normal.dot(outward) < 0:
            f.normal_flip()
        f.material_index = mat_index
        return f

    for i in range(len(rings) - 1):
        mi = 1 if (tip_mat and tip_from is not None and i >= tip_from) else 0
        for k in range(sides):
            quad = (rings[i][k], rings[i][(k + 1) % sides], rings[i + 1][(k + 1) % sides], rings[i + 1][k])
            centre = sum((v.co for v in quad), Vector()) / 4
            axis = (P[i] + P[i + 1]) / 2
            add_face(quad, centre - axis, mi)
    add_face(rings[0], -tangents[0], 0)
    add_face(rings[-1], tangents[-1], 1 if tip_mat else 0)
    bm.to_mesh(me)
    bm.free()
    me.uv_layers.new(name='UVMap')
    return obj


def taper_top(obj, k=0.55):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    for v in bm.verts:
        if v.co.z > 0:
            v.co.x *= k
            v.co.y *= k
    bm.to_mesh(obj.data)
    bm.free()


def build_banana(M, out_dir, bx=-12.8, bz=-3.4):
    random.seed(21)
    P = C.painterly_mat
    pot = P('pot', (0.80, 0.48, 0.34), 0.08, 6, (0.74, 0.42, 0.30))
    potdark = P('potdark', (0.55, 0.30, 0.22), 0.06, 6)
    soil = P('soil', (0.30, 0.22, 0.16), 0.12, 9, (0.36, 0.27, 0.19))
    stem = P('stem', (0.47, 0.58, 0.32), 0.10, 6, (0.56, 0.52, 0.30))
    leafA = P('leafA', (0.40, 0.62, 0.34), 0.10, 5, (0.46, 0.68, 0.36))
    leafB = P('leafB', (0.50, 0.70, 0.38), 0.10, 5, (0.42, 0.62, 0.33))
    leafOld = P('leafOld', (0.62, 0.62, 0.36), 0.10, 5, (0.58, 0.52, 0.32))
    banana = P('banana', (0.97, 0.84, 0.36), 0.05, 8, (0.92, 0.80, 0.30))
    tip = P('tip', (0.35, 0.27, 0.18), 0.04, 8)
    bud = P('bud', (0.52, 0.24, 0.32), 0.06, 7)
    parts = []

    # pot: open-topped terracotta with a rolled rim and a band; soil sits just below the rim
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=0.30, radius2=0.40, depth=0.58, location=C.U(bx, TOP + 0.29, bz))
    p = bpy.context.active_object
    bm = bmesh.new()
    bm.from_mesh(p.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.normal.z > 0.9], context='FACES')
    bm.to_mesh(p.data)
    bm.free()
    s = p.modifiers.new('s', 'SOLIDIFY')
    s.thickness = 0.03
    C.assign(p, pot)
    parts.append(p)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.41, minor_radius=0.045, major_segments=32, minor_segments=10, location=C.U(bx, TOP + 0.59, bz))
    r = bpy.context.active_object
    C.assign(r, pot)
    parts.append(r)
    parts.append(C.ucyl('band', (bx, TOP + 0.47, bz), 0.385, 0.05, potdark, 24, 0.01))
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=10, radius=0.37, location=C.U(bx, TOP + 0.47, bz))
    sl = bpy.context.active_object
    sl.scale = (1, 1, 0.14)
    C.assign(sl, soil)
    parts.append(sl)
    for k in range(6):
        a = random.uniform(0, 6.28)
        d = random.uniform(0.12, 0.3)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=random.uniform(0.02, 0.035),
                                              location=C.U(bx + math.cos(a) * d, TOP + 0.545, bz + math.sin(a) * d))
        pb = bpy.context.active_object
        C.assign(pb, potdark)
        parts.append(pb)

    # pseudostem with leaf-sheath rings
    bpy.ops.mesh.primitive_cone_add(vertices=14, radius1=0.085, radius2=0.055, depth=0.95, location=C.U(bx, TOP + 1.0, bz))
    st = bpy.context.active_object
    C.assign(st, stem)
    parts.append(st)
    for k, y in enumerate((0.7, 0.95, 1.2, 1.4)):
        rr = 0.085 - (y - 0.55) * 0.032
        bpy.ops.mesh.primitive_cone_add(vertices=14, radius1=rr + 0.012, radius2=rr, depth=0.07, location=C.U(bx, TOP + y, bz))
        ring = bpy.context.active_object
        C.assign(ring, leafOld if k % 2 else stem)
        parts.append(ring)

    # leaves: arching blades in a golden-angle spiral, plus one old drooping leaf
    crown = Vector((bx, TOP + 1.47, bz))
    specs = []
    for k in range(8):
        young = k >= 6
        specs.append(dict(yaw=k * 137.5 + random.uniform(-12, 12),
                          length=random.uniform(0.85, 1.15) * (0.75 if young else 1.0),
                          width=random.uniform(0.17, 0.21),
                          rise=random.uniform(0.35, 0.5) * (1.6 if young else 1.0),
                          droop=random.uniform(0.55, 0.85) * (0.4 if young else 1.0),
                          curl=random.uniform(-0.35, 0.35),
                          mat=leafB if k % 2 else leafA,
                          tear=0.35 if k in (1, 4) else 0.0))
    specs.append(dict(yaw=200, length=0.8, width=0.17, rise=0.1, droop=1.0, curl=0.5, mat=leafOld, tear=0.4))
    for i, sp in enumerate(specs):
        yr = math.radians(sp['yaw'])
        base = crown + Vector((math.sin(yr) * 0.04, -0.03 * (i % 3), math.cos(yr) * 0.04))
        parts.append(leaf_mesh('leaf%d' % i, base, sp['yaw'], sp['length'], sp['width'], sp['rise'], sp['droop'],
                               sp['curl'], sp['mat'], sp['tear']))

    # banana bunch: a stalk droops out of the crown; two hands of curved fingers point upward off it,
    # and the purple bud hangs at the stalk's end. Everything is generated along shared curves so
    # nothing floats loose.
    sd = math.radians(55)
    out = Vector((math.sin(sd), 0, math.cos(sd)))
    up = Vector((0, 1, 0))
    def stalk_pt(t):
        # from the crown, out and then down
        return crown + out * (0.48 * math.sin(t * math.pi * 0.5)) - up * (0.62 * t * t) + up * (0.08 * t)
    pts = [stalk_pt(i / 10) for i in range(11)]
    parts.append(tube('stalk', pts, [0.035 - 0.008 * (i / 10) for i in range(11)], stem))
    # Hands of bananas wrap around the hanging stalk. Each finger leaves the stalk heading outward and a
    # little down, then bends upward in the classic banana arc (a quarter circle), so the tips point up
    # and outward rather than straight up. Fingers are slightly rotated and varied so no two line up.
    rng = random.Random(5)
    for hand, (ts, n, spread) in enumerate(((0.42, 8, 300), (0.62, 7, 280), (0.80, 5, 240))):
        base = stalk_pt(ts)
        # stalk direction at this point (hands sit perpendicular to it)
        tang = (stalk_pt(min(1, ts + 0.02)) - stalk_pt(max(0, ts - 0.02))).normalized()
        ref = Vector((math.cos(sd), 0, -math.sin(sd)))
        e1 = (ref - tang * ref.dot(tang)).normalized()
        e2 = tang.cross(e1).normalized()
        length = 0.20 - hand * 0.025
        for j in range(n):
            a = math.radians(-spread / 2 + spread * j / max(1, n - 1) + hand * 20 + rng.uniform(-6, 6))
            radial = (e1 * math.cos(a) + e2 * math.sin(a)).normalized()
            # curl the outward direction towards "up" along a quarter circle of radius R
            R = length * (0.62 + rng.uniform(-0.05, 0.05))
            side = radial.cross(up)
            side = side.normalized() if side.length > 1e-4 else Vector((1, 0, 0))
            twist = rng.uniform(-0.12, 0.12)
            fp = []
            for i in range(10):
                t = i / 9
                ang = t * (math.pi / 2) * 0.95
                local = radial * (math.sin(ang) * R) + up * ((1 - math.cos(ang)) * R) - up * 0.025 * (1 - t)
                fp.append(base + radial * 0.03 + local + side * (twist * t * t))
            radii = [0.024 + 0.016 * math.sin(math.pi * min(1.0, t * 1.05)) for t in (i / 9 for i in range(10))]
            radii[0] = 0.014
            radii[-1] = 0.009
            parts.append(tube('banana', fp, radii, banana, tip_mat=tip, tip_from=8))
    end_pt = stalk_pt(1.0)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.07, location=C.U(*(end_pt - up * 0.08)))
    bd = bpy.context.active_object
    bd.scale = (1, 1, 1.5)
    C.assign(bd, bud)
    parts.append(bd)
    return C.finish(parts, 'yard_banana', out_dir, 1024)


def build_poles(posts, out_dir):
    """Tall wooden festoon poles with a cream cap and a hook for the garland."""
    P = C.painterly_mat
    wood = P('pole', (0.45, 0.34, 0.25), 0.08, 4, (0.40, 0.30, 0.22))
    cap = P('cap', (0.90, 0.86, 0.78), 0.05, 5)
    metal = P('metal', (0.30, 0.34, 0.36), 0.04, 6)
    parts = []
    for (x, z, h) in posts:
        parts.append(C.ubox('pole', (x, TOP + h / 2, z), (0.12, h, 0.12), wood, 0.02))
        parts.append(C.ubox('foot', (x, TOP + 0.07, z), (0.28, 0.14, 0.28), wood, 0.03))
        parts.append(C.ubox('cap', (x, TOP + h + 0.03, z), (0.18, 0.06, 0.18), cap, 0.02))
        parts.append(C.ucyl('hook', (x, TOP + h - 0.12, z + 0.08), 0.015, 0.1, metal, 6))
    return C.finish(parts, 'yard_poles', out_dir, 512)
