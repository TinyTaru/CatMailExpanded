"""Wooden strawberry planters for the yard: board-and-post box, soil, and strawberry plants with
trifoliate serrated leaves, white flowers and berries spilling over the front edge.
All positions are Unity world space; meshes are built in Blender space via C.U().

The box and soil are baked like the other yard props. The plants are thousands of tiny parts, so they
are exported separately with a swatch palette (palette_export) instead of a baked texture."""
import bpy, bmesh, math, random
from mathutils import Vector, Matrix
import cme_export as C
import banana as B


def V(x, y, z):
    return Vector(C.U(x, y, z))


def frame_matrix(origin_u, fwd_u, up_u=(0, 1, 0)):
    """Blender matrix whose local +X points along fwd and local +Z along (roughly) up, at a Unity point."""
    x = V(*fwd_u).normalized()
    up = V(*up_u).normalized()
    y = up.cross(x)
    if y.length < 1e-5:
        y = Vector((0, 1, 0)).cross(x)
    y.normalize()
    z = x.cross(y).normalized()
    m = Matrix((x, y, z)).transposed().to_4x4()
    m.translation = V(*origin_u)
    return m


def mesh_obj(name, bm, material):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(o)
    me.materials.append(material)
    me.uv_layers.new(name='UVMap')
    return o


def leaflet(name, length, width, material, teeth=7, fold=0.25, droop=0.18):
    """Serrated oval leaflet along local +X, folded along its midrib and drooping at the tip, two-sided."""
    bm = bmesh.new()
    n = teeth * 2 + 1
    rib, left, right = [], [], []
    for i in range(n + 1):
        t = i / n
        w = width * 0.5 * (math.sin(math.pi * min(1.0, t * 1.05)) ** 0.7)
        if 0 < i < n:
            w *= 1.0 if i % 2 else 0.86      # saw-tooth edge
        x = length * t
        dz = -droop * length * t * t
        rib.append(bm.verts.new((x, 0, dz + 0.004 * math.sin(math.pi * t))))
        left.append(bm.verts.new((x, w, dz - fold * w)))
        right.append(bm.verts.new((x, -w, dz - fold * w)))
    for i in range(n):
        for side, flip in ((left, False), (right, True)):
            q = [rib[i], rib[i + 1], side[i + 1], side[i]]
            if flip:
                q.reverse()
            if (q[0].co - q[3].co).length < 1e-6 or (q[1].co - q[2].co).length < 1e-6:
                continue
            bm.faces.new(q)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bm.normal_update()
    for f in bm.faces:
        if f.normal.z < 0:
            f.normal_flip()
    o = mesh_obj(name, bm, material)
    mod = o.modifiers.new('thick', 'SOLIDIFY')
    mod.thickness = 0.0035
    mod.offset = 0
    return o


def place(obj, matrix):
    obj.matrix_world = matrix @ obj.matrix_world
    return obj


def berry(name, base_u, tip_dir_u, length, material, calyx_mat):
    """Tapered heart-shaped berry hanging from base towards tip_dir, with a green star calyx."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=1.0)
    o = bpy.context.active_object
    o.name = name
    me = o.data
    # sphere axis is z: z=+1 is the stem end; taper the tip and stretch to a strawberry profile
    for v in me.vertices:
        t = (v.co.z + 1) / 2              # 0 tip .. 1 shoulder
        r = (0.25 + 0.75 * math.sin(math.pi * 0.5 * t) ** 0.6) * (1.0 - 0.15 * t * t)
        v.co.x *= r * length * 0.42
        v.co.y *= r * length * 0.42
        v.co.z *= length * 0.5
    C.assign(o, material)
    # orient: local +Z (stem end) points back towards the stem, i.e. against tip_dir
    d = V(*tip_dir_u).normalized()
    rot = (-d).to_track_quat('Z', 'Y').to_matrix().to_4x4()
    centre = V(*base_u) + d * (length * 0.5)
    o.matrix_world = Matrix.Translation(centre) @ rot
    parts = [o]
    # calyx: five little sepals splayed over the shoulder
    for k in range(5):
        a = k / 5 * math.tau + 0.3
        s = leaflet(name + '_sepal', length * 0.42, length * 0.2, calyx_mat, teeth=1, fold=0.1, droop=0.5)
        local = Matrix.Rotation(a, 4, 'Z') @ Matrix.Rotation(math.radians(-25), 4, 'Y')
        s.matrix_world = Matrix.Translation(V(*base_u)) @ rot @ Matrix.Rotation(math.pi, 4, 'X') @ local
        parts.append(s)
    return parts


def flower(name, centre_u, petal_mat, eye_mat, size=0.035, face_u=(0, 1, 0)):
    parts = []
    m = frame_matrix(centre_u, (1, 0, 0), face_u)
    for k in range(5):
        a = k / 5 * math.tau
        bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=5, radius=1.0)
        p = bpy.context.active_object
        p.scale = (size * 0.55, size * 0.45, size * 0.08)
        p.location = (math.cos(a) * size * 0.55, math.sin(a) * size * 0.55, 0)
        p.rotation_euler = (0, 0, a)
        bpy.context.view_layer.update()
        p.matrix_world = m @ p.matrix_world
        C.assign(p, petal_mat)
        parts.append(p)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=5, radius=size * 0.28)
    e = bpy.context.active_object
    e.scale = (1, 1, 0.6)
    bpy.context.view_layer.update()
    e.matrix_world = m @ Matrix.Translation((0, 0, size * 0.12)) @ e.matrix_world
    C.assign(e, eye_mat)
    parts.append(e)
    return parts


def strawberry_plant(rng, crown, M, front_z, rim_y, outer_z, with_berries=True, with_flower=False):
    """crown: Unity (x, y, z) at soil level. front_z: +1 if the planter's open side faces +z."""
    cx, cy, cz = crown
    parts = []
    nleaves = rng.randint(7, 9)
    for i in range(nleaves):
        a = i / nleaves * math.tau + rng.uniform(-0.3, 0.3)
        out = Vector((math.cos(a), 0, math.sin(a)))
        reach = rng.uniform(0.07, 0.14)
        rise = rng.uniform(0.09, 0.16)
        # petiole: rises then arches outward
        pts = []
        for k in range(6):
            t = k / 5
            pts.append((cx + out.x * reach * t ** 1.4, cy + rise * math.sin(t * math.pi * 0.62), cz + out.z * reach * t ** 1.4))
        parts.append(B.tube('petiole', pts, [0.006] * 6, M['stem'], sides=5))
        tip = Vector(pts[-1])
        lean = rng.uniform(-0.15, 0.05)
        for j, off in enumerate((-0.75, 0.0, 0.75)):
            d = Vector((math.cos(a + off), lean, math.sin(a + off)))
            L = rng.uniform(0.075, 0.098) * (1.12 if j == 1 else 1.0)
            lf = leaflet('leaflet', L, L * 0.72, M['leaf'] if (i + j) % 3 else M['leaf2'], teeth=5)
            place(lf, frame_matrix(tuple(tip), tuple(d)))
            parts.append(lf)
    if with_flower:
        a = rng.uniform(0, math.tau)
        top = (cx + math.cos(a) * 0.05, cy + 0.17, cz + math.sin(a) * 0.05)
        parts.append(B.tube('fstalk', [(cx, cy, cz), (cx + math.cos(a) * 0.03, cy + 0.12, cz + math.sin(a) * 0.03), top],
                            [0.004] * 3, M['stem'], sides=4))
        parts += flower('flower', top, M['flowerw'], M['eye'], 0.03, (math.cos(a) * 0.4, 1, math.sin(a) * 0.4))
    if with_berries:
        # trusses arch out from the crown and droop, so the berries rest just above the soil around the
        # plant's edge (mostly towards the open front where they're seen), never leaving the box
        n = rng.randint(3, 4)
        front_limit = outer_z - front_z * 0.09
        back_limit = outer_z - front_z * 0.52
        for b in range(n):
            a = math.pi / 2 * front_z + rng.uniform(-1.3, 1.3)       # around the +z (front) direction
            r = rng.uniform(0.15, 0.22)
            sx = cx + math.cos(a) * r
            sz = cz + math.sin(a) * r
            sz = min(max(sz, min(front_limit, back_limit)), max(front_limit, back_limit))
            hang = cy + rng.uniform(0.045, 0.07)
            P0 = Vector((cx, cy + 0.02, cz))
            P1 = Vector((cx + (sx - cx) * 0.35, cy + rng.uniform(0.12, 0.16), cz + (sz - cz) * 0.35))
            P2 = Vector((cx + (sx - cx) * 0.9, cy + 0.11, cz + (sz - cz) * 0.9))
            P3 = Vector((sx, hang + 0.02, sz))
            over = sz
            pts = []
            for k in range(11):
                t = k / 10
                q = (1 - t) ** 3 * P0 + 3 * (1 - t) ** 2 * t * P1 + 3 * (1 - t) * t * t * P2 + t ** 3 * P3
                pts.append(tuple(q))
            parts.append(B.tube('truss', pts, [0.0055 - 0.0015 * k / 10 for k in range(11)], M['stem'], sides=6))
            ripe = rng.random() < 0.85
            out_dir = (math.cos(a) * 0.35, -1, math.sin(a) * 0.35)
            parts += berry('berry', (sx, hang + 0.02, over), out_dir, rng.uniform(0.045, 0.06),
                           M['berry'] if ripe else M['berryg'], M['calyx'])
    return parts


def planter(cx, cz, M, rng, length=2.2, width=0.6, height=0.48, top=0.03, front_z=1, n_plants=7):
    """Board-and-post planter box centred at (cx, cz), sitting on y = top.
    Returns (box_parts, plant_parts)."""
    parts = []
    L, W, H = length, width, height
    post = 0.075
    # hidden inner liner so the board gaps show shadow, not daylight
    parts.append(C.ubox('liner', (cx, top + H * 0.47, cz), (L - 0.1, H * 0.9, W - 0.1), M['liner'], 0.0))
    for sx in (-1, 1):
        for sz in (-1, 1):
            parts.append(C.ubox('post', (cx + sx * (L / 2 - post / 2), top + H / 2, cz + sz * (W / 2 - post / 2)),
                                (post, H, post), M['post'], 0.012, 2))
    boards = 3
    gap = 0.012
    bh = (H - 0.03 - gap * (boards - 1)) / boards
    for b in range(boards):
        y = top + 0.015 + bh / 2 + b * (bh + gap)
        mat = M['plank'] if b % 2 == 0 else M['plank2']
        for sz in (-1, 1):
            parts.append(C.ubox('board', (cx, y, cz + sz * (W / 2 - 0.022)), (L - post * 2 + 0.01, bh, 0.034), mat, 0.008, 2))
        for sx in (-1, 1):
            parts.append(C.ubox('endboard', (cx + sx * (L / 2 - 0.022), y, cz), (0.034, bh, W - post * 2 + 0.01), mat, 0.008, 2))
    # rim cap boards overhanging the box
    cap_t, cap_w = 0.026, 0.085
    yc = top + H + cap_t / 2
    for sz in (-1, 1):
        parts.append(C.ubox('cap', (cx, yc, cz + sz * (W / 2 - cap_w / 2 + 0.015)), (L + 0.03, cap_t, cap_w), M['cap'], 0.008, 2))
    for sx in (-1, 1):
        parts.append(C.ubox('capend', (cx + sx * (L / 2 - cap_w / 2 + 0.015), yc + 0.001, cz), (cap_w, cap_t, W - 0.1), M['cap'], 0.008, 2))
    # soil surface with a few lumps
    soil_y = top + H - 0.05
    parts.append(C.ubox('soil', (cx, soil_y - 0.02, cz), (L - 0.09, 0.04, W - 0.09), M['soil'], 0.01))
    for k in range(18):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1.0)
        s = bpy.context.active_object
        s.scale = (rng.uniform(0.05, 0.11), rng.uniform(0.04, 0.08), rng.uniform(0.015, 0.03))
        s.location = C.U(cx + rng.uniform(-L / 2 + 0.12, L / 2 - 0.12), soil_y - 0.005, cz + rng.uniform(-W / 2 + 0.1, W / 2 - 0.1))
        s.rotation_euler = (0, 0, rng.uniform(0, math.tau))
        C.assign(s, M['soil2'] if k % 2 else M['soil'])
        parts.append(s)
    # plants in two staggered rows
    plants = []
    n = n_plants
    outer_z = cz + front_z * (W / 2 + 0.015)
    for i in range(n):
        x = cx - L / 2 + 0.2 + i * (L - 0.4) / (n - 1)
        z = cz + (0.08 if i % 2 else -0.06) * front_z
        bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=5, radius=1.0)
        mound = bpy.context.active_object
        mound.scale = (0.07, 0.07, 0.025)
        mound.location = C.U(x, soil_y, z)
        C.assign(mound, M['soil2'])
        parts.append(mound)
        plants += strawberry_plant(rng, (x, soil_y + 0.01, z), M, front_z, top + H + cap_t, outer_z,
                                   with_berries=(i % 2 == 0) or rng.random() < 0.8, with_flower=(i % 2 == 1))
    return parts, plants


COLORS = {}


def materials():
    def P(name, color, var, scale, color2=None):
        COLORS[name] = color
        return C.painterly_mat(name, color, var, scale, color2)
    return dict(
        plank=P('sb_plank', (0.62, 0.45, 0.31), 0.10, 5, (0.55, 0.39, 0.27)),
        plank2=P('sb_plank2', (0.56, 0.40, 0.28), 0.10, 5, (0.62, 0.46, 0.32)),
        cap=P('sb_cap', (0.68, 0.52, 0.37), 0.08, 5),
        post=P('sb_post', (0.47, 0.34, 0.25), 0.08, 5),
        liner=P('sb_liner', (0.22, 0.16, 0.12), 0.05, 5),
        soil=P('sb_soil', (0.33, 0.24, 0.18), 0.12, 9, (0.28, 0.20, 0.15)),
        soil2=P('sb_soil2', (0.38, 0.28, 0.20), 0.12, 9),
        stem=P('sb_stem', (0.40, 0.58, 0.28), 0.06, 8),
        leaf=P('sb_leaf', (0.26, 0.50, 0.24), 0.10, 9, (0.30, 0.50, 0.28)),
        leaf2=P('sb_leaf2', (0.34, 0.58, 0.28), 0.10, 9),
        berry=P('sb_berry', (0.86, 0.13, 0.16), 0.08, 18, (0.95, 0.36, 0.30)),
        berryg=P('sb_berryg', (0.93, 0.86, 0.62), 0.06, 12),
        calyx=P('sb_calyx', (0.36, 0.62, 0.28), 0.05, 8),
        flowerw=P('sb_petal', (0.98, 0.96, 0.92), 0.03, 8),
        eye=P('sb_eye', (0.98, 0.80, 0.30), 0.05, 8),
    )


def palette_export(parts, name, out_dir, rng, tones=(1.0, 1.08, 0.92)):
    """Joins small parts and colours them from a swatch palette instead of a baked texture: every face
    samples the centre of a solid swatch of its material colour (one of a few tones, picked per face)."""
    import os, struct, zlib
    for p in parts:
        C.apply_all(p)
    obj = C.join(parts, name)
    me = obj.data
    mats = [sl.material.name for sl in obj.material_slots]
    SW = 16
    cols = []
    for mname in mats:
        base = COLORS.get(mname, (1, 0, 1))
        for t in tones:
            cols.append(tuple(min(1.0, c * t) for c in base))
    W, H = SW * len(cols), SW
    while len(me.uv_layers):
        me.uv_layers.remove(me.uv_layers[0])
    uvl = me.uv_layers.new(name='UVMap')
    for poly in me.polygons:
        k = poly.material_index * len(tones) + rng.randrange(len(tones))
        u = (k * SW + SW / 2) / W
        for li in poly.loop_indices:
            uvl.data[li].uv = (u, 0.5)
    row = bytes(1) + b''.join(bytes(int(c * 255 + 0.5) for c in col) * SW for col in cols)
    raw = row * H

    def chunk(t, d):
        return struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)
    sig = bytes([137, 80, 78, 71, 13, 10, 26, 10])
    png = sig + chunk(b'IHDR', struct.pack('>IIBBBBB', W, H, 8, 2, 0, 0, 0)) + \
        chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b'')
    tex = os.path.join(out_dir, 'tex_' + name + '.png')
    open(tex, 'wb').write(png)
    C.export_cmemesh(obj, os.path.join(out_dir, name + '.cmemesh'))
    return obj, tex
