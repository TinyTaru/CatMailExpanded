"""Re-imports a .cmemesh into Blender (undoing the Unity axis conversion) and renders it with
back-face culling, to verify exported winding/normals the way Unity will draw them."""
import bpy, struct, sys, os, math, bmesh
sys.path.insert(0, os.path.dirname(__file__))
import cme_export as C
args = sys.argv[sys.argv.index('--') + 1:]
path, tex, out = args[0], args[1], args[2]
C.reset_scene()
d = open(path, 'rb').read()
sub, vc = struct.unpack_from('<II', d, 8)
off = 16
V = [struct.unpack_from('<12f', d, off + i * 48) for i in range(vc)]
off += vc * 48
tris = []
for s in range(sub):
    nl = struct.unpack_from('<I', d, off)[0]; off += 4 + nl
    ic = struct.unpack_from('<I', d, off)[0]; off += 4
    idx = struct.unpack_from('<%dI' % ic, d, off); off += 4 * ic
    tris += [(idx[t], idx[t + 2], idx[t + 1]) for t in range(0, ic, 3)]  # back to Blender's CCW order
me = bpy.data.meshes.new('imp')
me.from_pydata([(-v[0], -v[2], v[1]) for v in V], [], tris)
uvl = me.uv_layers.new(name='uv')
for poly in me.polygons:
    for li in poly.loop_indices:
        vi = me.loops[li].vertex_index
        uvl.data[li].uv = (V[vi][6], V[vi][7])
obj = bpy.data.objects.new('imp', me)
bpy.context.collection.objects.link(obj)
m = bpy.data.materials.new('m'); m.use_nodes = True; m.use_backface_culling = True
t = m.node_tree.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(tex)
m.node_tree.links.new(t.outputs['Color'], m.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
me.materials.append(m)
for ang in [(70, 0, 90), (55, 0, 200)]:
    C.render_preview(obj, out.replace('.png', '_%d.png' % ang[2]), None, angle=ang)
