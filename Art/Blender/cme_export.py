"""Shared helpers for Cat Mail Expanded Blender model scripts.

Binary mesh format (.cmemesh), little endian:
  magic 'CMEM' | version u32 (1) | submesh count u32 | vertex count u32
  per vertex: pos 3f, normal 3f, uv 2f, color 4f (Unity space: left handed, Y up)
  per submesh: name len u32 + utf8 name, index count u32, indices u32[]
Coordinates are converted from Blender (Z up, right handed) to Unity (Y up, left handed)
by (x, y, z) -> (-x, z, -y)  [the same as Unity's FBX import with "bake axis conversion"].
"""
import bpy, bmesh, struct, os, math
from mathutils import Vector


def reset_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.images):
        for b in list(block):
            if b.users == 0:
                block.remove(b)


def apply_all(obj):
    bpy.context.view_layer.objects.active = obj
    for o in bpy.context.selected_objects:
        o.select_set(False)
    obj.select_set(True)
    for m in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)


def join(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = name
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(40))
    return o


def export_cmemesh(obj, path, smooth_angle=35.0):
    """Export evaluated mesh of obj (world transform applied) as .cmemesh.
    Material slots become submeshes. Vertex colors (first color attribute) exported, default white."""
    depsgraph = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(depsgraph)
    me = ev.to_mesh()
    me.transform(obj.matrix_world)
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bm.to_mesh(me)
    bm.free()
    # split normals: use auto smooth like behaviour
    try:
        me.set_sharp_from_angle(angle=math.radians(smooth_angle))
    except Exception:
        pass
    uv = me.uv_layers.active.data if me.uv_layers.active else None
    col_attr = me.color_attributes.active_color if me.color_attributes else None
    corner_normals = [cn.vector.copy() for cn in me.corner_normals] if hasattr(me, 'corner_normals') else None

    verts = []
    vmap = {}
    sub = {}
    for poly in me.polygons:
        idxs = []
        for li in poly.loop_indices:
            loop = me.loops[li]
            v = me.vertices[loop.vertex_index].co
            n = corner_normals[li] if corner_normals else loop.normal
            t = (uv[li].uv[0], uv[li].uv[1]) if uv else (0.0, 0.0)
            if col_attr is not None:
                if col_attr.domain == 'CORNER':
                    c = tuple(col_attr.data[li].color)
                else:
                    c = tuple(col_attr.data[loop.vertex_index].color)
            else:
                c = (1.0, 1.0, 1.0, 1.0)
            key = (round(v.x, 5), round(v.y, 5), round(v.z, 5), round(n.x, 3), round(n.y, 3), round(n.z, 3),
                   round(t[0], 5), round(t[1], 5), tuple(round(x, 3) for x in c))
            i = vmap.get(key)
            if i is None:
                i = len(verts)
                vmap[key] = i
                # Blender -> Unity axis conversion
                verts.append(((-v.x, v.z, -v.y), (-n.x, n.z, -n.y), t, c))
            idxs.append(i)
        # Unity treats clockwise triangles as front faces: reverse Blender's counter-clockwise order.
        sub.setdefault(poly.material_index, []).extend([idxs[0], idxs[2], idxs[1]])
    names = []
    for mi in sorted(sub.keys()):
        mat = obj.material_slots[mi].material if mi < len(obj.material_slots) and obj.material_slots[mi].material else None
        names.append((mat.name if mat else 'default', sub[mi]))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'wb') as f:
        f.write(b'CMEM')
        f.write(struct.pack('<III', 1, len(names), len(verts)))
        for p, n, t, c in verts:
            f.write(struct.pack('<12f', *p, *n, *t, *c))
        for nm, ids in names:
            b = nm.encode('utf-8')
            f.write(struct.pack('<I', len(b)))
            f.write(b)
            f.write(struct.pack('<I', len(ids)))
            f.write(struct.pack('<%dI' % len(ids), *ids))
    ev.to_mesh_clear()
    print('exported', path, len(verts), 'verts', [(n, len(i) // 3) for n, i in names])


def mat(name, color, rough=0.8):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = rough
    return m


def rounded_box(name, size, bevel=0.02, segments=2, loc=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)  # keep the pivot at the box centre (urot rotates about it)
    if bevel > 0:
        b = o.modifiers.new('bevel', 'BEVEL')
        b.width = bevel
        b.segments = segments
        b.limit_method = 'ANGLE'
    return o


def cylinder(name, r, depth, verts=24, loc=(0, 0, 0), rot=(0, 0, 0), bevel=0.0):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=depth, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    if bevel > 0:
        b = o.modifiers.new('bevel', 'BEVEL')
        b.width = bevel
        b.segments = 2
        b.limit_method = 'ANGLE'
    return o


def assign(obj, material):
    obj.data.materials.clear()
    obj.data.materials.append(material)


# ---------------------------------------------------------------------------
# Painterly material + texture baking
# ---------------------------------------------------------------------------
def painterly_mat(name, color, variation=0.08, scale=6.0, color2=None):
    """Base colour with soft blotchy variation, imitating the game's hand painted textures.
    Colours are given in sRGB (as picked from screenshots) and converted to linear here."""
    color = tuple(c ** 2.2 for c in color)
    if color2 is not None:
        color2 = tuple(c ** 2.2 for c in color2)
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    bsdf = nt.nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.inputs['Roughness'].default_value = 0.9
    tc = nt.nodes.new('ShaderNodeTexCoord')
    noise = nt.nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = scale
    noise.inputs['Detail'].default_value = 1.5
    noise.inputs['Roughness'].default_value = 0.4
    vor = nt.nodes.new('ShaderNodeTexVoronoi')
    vor.inputs['Scale'].default_value = scale * 2.2
    nt.links.new(tc.outputs['Object'], noise.inputs['Vector'])
    nt.links.new(tc.outputs['Object'], vor.inputs['Vector'])
    mix_n = nt.nodes.new('ShaderNodeMix')
    mix_n.data_type = 'FLOAT'
    mix_n.inputs[0].default_value = 0.5
    nt.links.new(noise.outputs['Fac'], mix_n.inputs[2])
    nt.links.new(vor.outputs['Distance'], mix_n.inputs[3])
    ramp = nt.nodes.new('ShaderNodeMapRange')
    ramp.inputs['From Min'].default_value = 0.25
    ramp.inputs['From Max'].default_value = 0.75
    ramp.inputs['To Min'].default_value = 1 - variation
    ramp.inputs['To Max'].default_value = 1 + variation
    nt.links.new(mix_n.outputs[0], ramp.inputs['Value'])
    base = nt.nodes.new('ShaderNodeRGB')
    base.outputs[0].default_value = (*color, 1)
    mul = nt.nodes.new('ShaderNodeMix')
    mul.data_type = 'RGBA'
    mul.blend_type = 'MULTIPLY'
    mul.inputs[0].default_value = 1.0
    nt.links.new(base.outputs[0], mul.inputs[6])
    comb = nt.nodes.new('ShaderNodeCombineColor')
    for i in range(3):
        nt.links.new(ramp.outputs[0], comb.inputs[i])
    nt.links.new(comb.outputs[0], mul.inputs[7])
    final = mul
    if color2 is not None:
        # hue drift toward a second colour in blotches
        c2 = nt.nodes.new('ShaderNodeRGB')
        c2.outputs[0].default_value = (*color2, 1)
        drift = nt.nodes.new('ShaderNodeMix')
        drift.data_type = 'RGBA'
        nt.links.new(noise.outputs['Fac'], drift.inputs[0])
        nt.links.new(mul.outputs[2], drift.inputs[6])
        nt.links.new(c2.outputs[0], drift.inputs[7])
        final = drift
    nt.links.new(final.outputs[2], bsdf.inputs['Base Color'])
    nt.links.new(bsdf.outputs[0], out.inputs[0])
    return m


def image_mat(name, image_path):
    """Material using an image texture (e.g. AI painted decal) on the object's UVs."""
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes.get('Principled BSDF')
    tex = nt.nodes.new('ShaderNodeTexImage')
    tex.image = bpy.data.images.load(image_path)
    nt.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
    return m


def _boost_small_parts(obj, ref_area=0.6, max_boost=5.0):
    """Scales the UV islands of each small loose part up about its centre (packing then shrinks everything to fit),
    so tiny parts keep enough texels to show their colour."""
    import bmesh
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    uvl = bm.loops.layers.uv['bake']
    bm.faces.ensure_lookup_table()
    seen = set()
    for f0 in bm.faces:
        if f0.index in seen:
            continue
        stack, part = [f0], []
        seen.add(f0.index)
        while stack:
            f = stack.pop()
            part.append(f)
            for v in f.verts:
                for g in v.link_faces:
                    if g.index not in seen:
                        seen.add(g.index)
                        stack.append(g)
        area = sum(f.calc_area() for f in part)
        s = min(max_boost, max(1.0, math.sqrt(ref_area / max(area, 1e-6))))
        if s <= 1.001:
            continue
        loops = [l for f in part for l in f.loops]
        cu = sum(l[uvl].uv.x for l in loops) / len(loops)
        cv = sum(l[uvl].uv.y for l in loops) / len(loops)
        for l in loops:
            uv = l[uvl].uv
            l[uvl].uv = ((uv.x - cu) * s + cu, (uv.y - cv) * s + cv)
    bm.to_mesh(me)
    bm.free()


def _push_pull(rgb, w):
    """Fills pixels with weight 0 from a pyramid of their weighted surroundings (size must be a power of two)."""
    import numpy as np
    h, wd = w.shape
    if h <= 1 or wd <= 1:
        tot = max(w.sum(), 1e-6)
        return np.broadcast_to((rgb * w[..., None]).sum((0, 1)) / tot, rgb.shape).copy()
    pre = rgb * w[..., None]
    cs = pre.reshape(h // 2, 2, wd // 2, 2, 3).sum((1, 3))
    ws = w.reshape(h // 2, 2, wd // 2, 2).sum((1, 3))
    low = _push_pull(np.where(ws[..., None] > 0, cs / np.maximum(ws, 1e-6)[..., None], 0), np.minimum(ws, 1.0))
    up = np.repeat(np.repeat(low, 2, 0), 2, 1)
    return np.where(w[..., None] > 0, rgb, up)


def bake_textures(obj, out_png, size=1024, ao_strength=0.55, margin=8, top_light=0.12):
    """Bakes the object's materials (diffuse colour) * ambient occlusion into one texture.
    Replaces UVs with a fresh smart projection, keeps material slots for submesh names but
    all submeshes then share the baked texture."""
    import numpy as np
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 64
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    # new UV layer for bake
    me = obj.data
    if 'bake' not in me.uv_layers:
        me.uv_layers.new(name='bake')
    me.uv_layers.active = me.uv_layers['bake']
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.012)
    bpy.ops.object.mode_set(mode='OBJECT')
    # small parts (candles, rosettes, thin trims) would get islands under a pixel wide next to whole walls, and the
    # bake writes nothing into those: give each loose part a minimum share, then pack the islands tightly
    _boost_small_parts(obj)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.select_all(action='SELECT')
    bpy.ops.uv.pack_islands(rotate=True, margin=0.003, shape_method='AABB')
    bpy.ops.object.mode_set(mode='OBJECT')

    def make_img(nm):
        img = bpy.data.images.new(nm, size, size, alpha=True, float_buffer=False)
        img.generated_color = (0, 0, 0, 0)   # alpha marks the pixels the bake wrote
        for slot in obj.material_slots:
            nt = slot.material.node_tree
            n = nt.nodes.new('ShaderNodeTexImage')
            n.image = img
            n.name = 'BAKE_TARGET'
            nt.nodes.active = n
        return img

    def clear_targets():
        for slot in obj.material_slots:
            nt = slot.material.node_tree
            for n in [n for n in nt.nodes if n.name.startswith('BAKE_TARGET')]:
                nt.nodes.remove(n)

    col = make_img('bake_col')
    scene.render.bake.use_pass_direct = False
    scene.render.bake.use_pass_indirect = False
    scene.render.bake.use_pass_color = True
    scene.render.bake.margin = margin
    bpy.ops.object.bake(type='DIFFUSE')
    clear_targets()
    ao = make_img('bake_ao')
    scene.world = scene.world or bpy.data.worlds.new('w')
    bpy.ops.object.bake(type='AO')
    clear_targets()
    c = np.array(col.pixels[:]).reshape(size, size, 4)
    a = np.array(ao.pixels[:]).reshape(size, size, 4)[..., :1]
    shade = 1 - ao_strength * (1 - a)
    c[..., :3] = np.clip(c[..., :3] * shade, 0, 1)
    # fill every pixel the bake left empty with its surroundings, so no sampling or mip level ever reaches black
    valid = c[..., 3] > 0.5
    print('bake coverage %.2f' % valid.mean())
    c[..., :3] = _push_pull(c[..., :3], valid.astype(float))
    c[..., 3] = 1
    out = bpy.data.images.new('bake_out', size, size, alpha=False)
    out.pixels = c.flatten()
    out.filepath_raw = out_png
    out.file_format = 'PNG'
    out.save()
    # drop original UVs so the exported mesh uses the bake UVs
    for uvl in [u for u in me.uv_layers if u.name != 'bake']:
        me.uv_layers.remove(uvl)
    print('baked', out_png)


def render_preview(obj, out_png, texture_png=None, res=640, angle=(62, 0, 38), frame=None):
    """Quick EEVEE render of the object (optionally with the baked texture applied) for review."""
    scene = bpy.context.scene
    try:
        scene.render.engine = 'BLENDER_EEVEE_NEXT'
    except TypeError:
        scene.render.engine = 'BLENDER_EEVEE'
    scene.view_settings.view_transform = 'Standard'
    scene.render.resolution_x = res
    scene.render.resolution_y = res
    scene.render.film_transparent = False
    if texture_png:
        m = bpy.data.materials.new('preview')
        m.use_nodes = True
        nt = m.node_tree
        t = nt.nodes.new('ShaderNodeTexImage')
        t.image = bpy.data.images.load(texture_png, check_existing=False)
        nt.links.new(t.outputs['Color'], nt.nodes['Principled BSDF'].inputs['Base Color'])
        nt.nodes['Principled BSDF'].inputs['Roughness'].default_value = 0.9
        for i in range(len(obj.material_slots)):
            obj.material_slots[i].material = m
    w = scene.world or bpy.data.worlds.new('w')
    scene.world = w
    w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[0].default_value = (0.78, 0.76, 0.85, 1)
    w.node_tree.nodes['Background'].inputs[1].default_value = 1.0
    bpy.ops.object.light_add(type='SUN', rotation=(math.radians(50), math.radians(10), math.radians(30)))
    bpy.context.active_object.data.energy = 3.0
    # frame object
    bb = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    center = sum(bb, Vector()) / 8
    radius = max((v - center).length for v in bb)
    if frame:
        center, radius = Vector(frame[0]), frame[1]
    bpy.ops.object.camera_add()
    cam = bpy.context.active_object
    cam.data.lens = 50
    rx, ry, rz = (math.radians(a) for a in angle)
    cam.rotation_euler = (rx, ry, rz)
    d = radius / math.tan(cam.data.angle / 2) * 1.15
    direction = cam.rotation_euler.to_matrix() @ Vector((0, 0, 1))
    cam.location = center + direction * d
    scene.camera = cam
    scene.render.filepath = out_png
    bpy.ops.render.render(write_still=True)


# ---------------------------------------------------------------------------
# Unity-space authoring helpers: positions/sizes given in Unity coordinates
# (x right, y up, z forward). Blender = (-ux, -uz, uy).
# ---------------------------------------------------------------------------
def U(x, y, z):
    return (-x, -z, y)


def ubox(name, center, size, material, bevel=0.012, segments=2):
    sx, sy, sz = size
    o = rounded_box(name, (sx, sz, sy), bevel, segments, U(*center))
    assign(o, material)
    return o


def ucyl(name, center, radius, height, material, verts=12, bevel=0.0):
    o = cylinder(name, radius, height, verts, U(*center), (0, 0, 0), bevel)
    assign(o, material)
    return o


def urot(obj, x_deg=0.0, y_deg=0.0, z_deg=0.0):
    """Rotate by Unity euler angles (applied Z, X, Y like Unity) about the object's origin."""
    from mathutils import Matrix
    # The axis conversion is a mirror, so rotation sense flips:
    # Unity +a about X == Blender +a about X; about Y == -a about Z; about Z == +a about Y.
    # Unity applies Z, then X, then Y.
    m = (Matrix.Rotation(math.radians(-y_deg), 4, 'Z') @
         Matrix.Rotation(math.radians(x_deg), 4, 'X') @
         Matrix.Rotation(math.radians(z_deg), 4, 'Y'))
    loc = obj.location.copy()
    obj.matrix_basis = Matrix.Translation(loc) @ m @ Matrix.Translation(-loc) @ obj.matrix_basis
    return obj


def uquad(name, center, right, up, normal, material=None):
    """Single quad in Unity space with UV (0,0) bottom-left .. (1,1) top-right as seen from `normal`."""
    import bmesh as _bm
    c = Vector(U(*center)); r = Vector(U(*right)); u = Vector(U(*up)); n = Vector(U(*normal))
    me = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    bm = _bm.new()
    vs = [bm.verts.new(c - r * 0.5 - u * 0.5), bm.verts.new(c + r * 0.5 - u * 0.5),
          bm.verts.new(c + r * 0.5 + u * 0.5), bm.verts.new(c - r * 0.5 + u * 0.5)]
    f = bm.faces.new(vs)
    bm.normal_update()
    uvl = bm.loops.layers.uv.new('UVMap')
    uvs = [(0, 0), (1, 0), (1, 1), (0, 1)]
    for loop, uv in zip(f.loops, uvs):
        loop[uvl].uv = uv
    if f.normal.dot(n) < 0:
        # Mirror sends the image backwards when the face is flipped, so swap u as well
        f.normal_flip()
        for loop in f.loops:
            loop[uvl].uv = (loop[uvl].uv[0], loop[uvl].uv[1])
    bm.to_mesh(me)
    bm.free()
    if material:
        me.materials.append(material)
    return obj


def finish(parts, name, out_dir, tex_size=2048, bake=True):
    for p in parts:
        apply_all(p)
    obj = join(parts, name)
    tex = os.path.join(out_dir, 'tex_' + name + '.png')
    if bake:
        bake_textures(obj, tex, tex_size)
    export_cmemesh(obj, os.path.join(out_dir, name + '.cmemesh'))
    return obj, tex


def import_cmemesh(path, tex):
    """Loads an exported .cmemesh back into Blender (undoing the Unity axis conversion), textured."""
    d = open(path, 'rb').read()
    sub, vc = struct.unpack_from('<II', d, 8)
    off = 16
    V = [struct.unpack_from('<12f', d, off + i * 48) for i in range(vc)]
    off += vc * 48
    tris = []
    for _ in range(sub):
        nl = struct.unpack_from('<I', d, off)[0]
        off += 4 + nl
        ic = struct.unpack_from('<I', d, off)[0]
        off += 4
        idx = struct.unpack_from('<%dI' % ic, d, off)
        off += 4 * ic
        tris += [(idx[t], idx[t + 2], idx[t + 1]) for t in range(0, ic, 3)]
    name = os.path.splitext(os.path.basename(path))[0]
    me = bpy.data.meshes.new(name)
    me.from_pydata([(-v[0], -v[2], v[1]) for v in V], [], tris)
    uvl = me.uv_layers.new(name='uv')
    for poly in me.polygons:
        for li in poly.loop_indices:
            vi = me.loops[li].vertex_index
            uvl.data[li].uv = (V[vi][6], V[vi][7])
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.use_backface_culling = True
    t = m.node_tree.nodes.new('ShaderNodeTexImage')
    t.image = bpy.data.images.load(tex, check_existing=False)
    m.node_tree.links.new(t.outputs['Color'], m.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
    m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = 0.9
    me.materials.append(m)
    return obj
