import bpy, sys, os, math, bmesh
sys.path.insert(0, os.path.dirname(__file__))
import importlib, cme_export as C
importlib.reload(C)
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
C.reset_scene()

body_m = C.painterly_mat('bin', (0.42, 0.62, 0.58), 0.10, 5, (0.38, 0.58, 0.6))   # soft teal metal
rim_m = C.painterly_mat('rim', (0.86, 0.84, 0.78), 0.05, 6)
lid_m = C.painterly_mat('lid', (0.42, 0.62, 0.58), 0.08, 5)
dark_m = C.painterly_mat('dark', (0.2, 0.22, 0.24), 0.05, 6)
label_m = C.painterly_mat('label', (0.95, 0.88, 0.66), 0.05, 6)
fish_m = C.painterly_mat('fish', (0.85, 0.42, 0.36), 0.06, 6)

R, H = 0.27, 0.62
parts = []
# tapered body via cone with open top
bpy.ops.mesh.primitive_cone_add(vertices=32, radius1=R * 0.86, radius2=R, depth=H, location=(0, 0, H / 2 + 0.02))
body = bpy.context.active_object
bm = bmesh.new(); bm.from_mesh(body.data)
top = [f for f in bm.faces if f.normal.z > 0.9]
bmesh.ops.delete(bm, geom=top, context='FACES')
bm.to_mesh(body.data); bm.free()
s = body.modifiers.new('solid', 'SOLIDIFY'); s.thickness = 0.012
b = body.modifiers.new('bevel', 'BEVEL'); b.width = 0.004; b.segments = 2; b.limit_method = 'ANGLE'
C.assign(body, body_m); parts.append(body)
# horizontal ridges
for z in (0.17, 0.47):
    r = R * 0.86 + (R - R * 0.86) * (z / H)
    t = bpy.ops.mesh.primitive_torus_add(major_radius=r + 0.004, minor_radius=0.011, major_segments=40, minor_segments=8, location=(0, 0, z + 0.02))
    o = bpy.context.active_object; C.assign(o, rim_m); parts.append(o)
# rim
bpy.ops.mesh.primitive_torus_add(major_radius=R + 0.005, minor_radius=0.018, major_segments=40, minor_segments=10, location=(0, 0, H + 0.02))
o = bpy.context.active_object; C.assign(o, rim_m); parts.append(o)
# base ring
bpy.ops.mesh.primitive_torus_add(major_radius=R * 0.86, minor_radius=0.016, major_segments=40, minor_segments=8, location=(0, 0, 0.03))
o = bpy.context.active_object; C.assign(o, dark_m); parts.append(o)
# inner dark bottom
inner = C.cylinder('inner', R * 0.83, 0.01, 32, (0, 0, 0.05)); C.assign(inner, dark_m); parts.append(inner)
# swing lid: domed, hinged open slightly at back
bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=12, radius=R + 0.02, location=(0, 0, 0))
lid = bpy.context.active_object
bm = bmesh.new(); bm.from_mesh(lid.data)
bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -0.001], context='VERTS')
bm.to_mesh(lid.data); bm.free()
lid.scale = (1, 1, 0.38)
s = lid.modifiers.new('solid', 'SOLIDIFY'); s.thickness = 0.014
C.assign(lid, lid_m)
lid.location = (0, 0.0, H + 0.035)
parts.append(lid)
knob = C.cylinder('knob', 0.03, 0.04, 16, (0, 0, H + 0.035 + (R + 0.02) * 0.38 + 0.01), (0, 0, 0), 0.008)
C.assign(knob, rim_m); parts.append(knob)
# hinge at the back rim: flip the lid up so the bin clearly reads as open
from mathutils import Matrix
hinge = Matrix.Translation((0, R + 0.01, H + 0.035))
rot = hinge @ Matrix.Rotation(math.radians(-62), 4, 'X') @ hinge.inverted()
for o in (lid, knob):
    o.matrix_basis = rot @ o.matrix_basis
# pedal
ped = C.rounded_box('pedal', (0.14, 0.09, 0.025), 0.008, 2, (0, -R * 0.9 - 0.03, 0.04)); C.assign(ped, dark_m); parts.append(ped)
for p in parts:
    C.apply_all(p)
obj = C.join(parts, 'TrashCan')
tex = os.path.join(ROOT, 'Assets', 'Build', 'tex_trashcan.png')
C.bake_textures(obj, tex, 1024)
C.export_cmemesh(obj, os.path.join(ROOT, 'Assets', 'Build', 'trashcan.cmemesh'))
C.render_preview(obj, os.path.join(ROOT, 'Art', 'Previews', 'trashcan.png'), tex, angle=(72, 0, -25))
