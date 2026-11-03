"""Helpers for bringing Synty POLYGON FBX models into the Cat Mail Expanded Blender pipeline.

Synty models are flat-coloured low poly meshes whose UVs point at swatches of one colour atlas
(e.g. PolygonNature_01.png). We import the FBX, bake its transforms, give every face the atlas
material and return a single object whose origin sits at the base centre.
"""
import bpy, os, math
from mathutils import Vector

PACKS = r'C:/git/Mods/CatMailExpanded/AssetPacks/_extracted'
NATURE = PACKS + '/Nature/FBX'
NATURE_ATLAS = PACKS + '/Nature/Textures/PolygonNature_01.png'
ADV = PACKS + '/Adventure/Synty/PolygonAdventure/Models'
ADV_ATLAS = PACKS + '/Adventure/Synty/PolygonAdventure/Textures/PolyAdventure_01.png'
GEN = PACKS + '/Generic/Models'
GEN_TEX = PACKS + '/Generic/Textures'

_mats = {}


def atlas_mat(png):
    """One material per atlas. Closest filtering keeps swatch edges crisp in previews."""
    key = os.path.basename(png)
    m = _mats.get(key)
    try:
        if m and m.name in bpy.data.materials:
            return m
    except ReferenceError:
        pass
    m = bpy.data.materials.new('atlas_' + os.path.splitext(key)[0])
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes.get('Principled BSDF')
    bsdf.inputs['Roughness'].default_value = 0.9
    tex = nt.nodes.new('ShaderNodeTexImage')
    tex.image = bpy.data.images.load(png, check_existing=True)
    tex.interpolation = 'Closest'
    nt.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
    _mats[key] = m
    return m


def load(fbx, atlas, name=None):
    """Imports one FBX and returns a single joined mesh object (transforms applied, base at z=0)."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=fbx)
    new = [o for o in bpy.data.objects if o not in before]
    meshes = [o for o in new if o.type == 'MESH' and 'LOD' not in o.name.upper().replace('_LOD0', '')
              and 'COLLI' not in o.name.upper()]
    for o in new:
        if o not in meshes:
            bpy.data.objects.remove(o, do_unlink=True)
    if not meshes:
        raise RuntimeError('no mesh in ' + fbx)
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes:
        o.parent = None
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = name or os.path.splitext(os.path.basename(fbx))[0]
    # some source packs are authored in centimetres without an FBX unit scale
    bpy.context.view_layer.update()
    if max(obj.dimensions) > 25:
        obj.data.transform(__import__('mathutils').Matrix.Scale(0.01, 4))
        obj.data.update()
        bpy.context.view_layer.update()
    m = atlas_mat(atlas)
    obj.data.materials.clear()
    obj.data.materials.append(m)
    for p in obj.data.polygons:
        p.material_index = 0
    # origin at base centre
    bb = [Vector(c) for c in obj.bound_box]
    base = Vector(((min(v.x for v in bb) + max(v.x for v in bb)) / 2,
                   (min(v.y for v in bb) + max(v.y for v in bb)) / 2,
                   min(v.z for v in bb)))
    obj.data.transform(__import__('mathutils').Matrix.Translation(-base))
    obj.location = (0, 0, 0)
    return obj


def size(obj):
    return obj.dimensions.copy()


def contact_sheet(objs, out_png, cols=6, res=(1800, 1000), labels=True, gap=1.3):
    """Lays objects out in a grid and renders a front-ish orthographic view for review."""
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE_NEXT'
    scene.view_settings.view_transform = 'Standard'
    scene.render.resolution_x, scene.render.resolution_y = res
    x = 0.0
    row_y = 0.0
    row_h = 0.0
    rows = []
    cur = []
    for i, o in enumerate(objs):
        w = max(o.dimensions.x, 0.5)
        if i % cols == 0 and i:
            rows.append((cur, row_h))
            cur, row_h = [], 0
        cur.append(o)
        row_h = max(row_h, o.dimensions.z)
    rows.append((cur, row_h))
    z = 0.0
    allpts = []
    for objs_row, h in rows:
        x = 0.0
        for o in objs_row:
            w = max(o.dimensions.x, 1.2)
            o.location = (x + w / 2, 0, z)
            x += w * gap + 0.3
            if labels:
                bpy.ops.object.text_add(location=(o.location.x - w / 2, -1, z - 0.5))
                t = bpy.context.active_object
                t.data.body = o.name.replace('SM_', '').replace('Env_', '').replace('Plant_', '')[:18]
                t.data.size = max(0.22, w * 0.12)
                t.rotation_euler = (math.radians(90), 0, 0)
                t.data.materials.append(bpy.data.materials.get('lbl') or _lbl())
        z -= h + 1.4
    bpy.context.view_layer.update()
    allpts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    mn = Vector((min(p.x for p in allpts), 0, min(p.z for p in allpts) - 1))
    mx = Vector((max(p.x for p in allpts), 0, max(p.z for p in allpts)))
    w = scene.world or bpy.data.worlds.new('w')
    scene.world = w
    w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[0].default_value = (0.75, 0.78, 0.86, 1)
    bpy.ops.object.light_add(type='SUN', rotation=(math.radians(55), math.radians(5), math.radians(25)))
    bpy.context.active_object.data.energy = 3.5
    bpy.ops.object.camera_add(location=((mn.x + mx.x) / 2, -60, (mn.z + mx.z) / 2), rotation=(math.radians(90), 0, 0))
    cam = bpy.context.active_object
    cam.data.type = 'ORTHO'
    aspect = res[0] / res[1]
    cam.data.ortho_scale = max(mx.x - mn.x, (mx.z - mn.z) * aspect) * 1.05
    scene.camera = cam
    scene.render.filepath = out_png
    bpy.ops.render.render(write_still=True)


def _lbl():
    m = bpy.data.materials.new('lbl')
    m.use_nodes = True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.05, 0.05, 0.08, 1)
    return m
