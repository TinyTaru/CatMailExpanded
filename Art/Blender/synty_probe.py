import bpy, sys, os
args = sys.argv[sys.argv.index('--') + 1:]
root = args[0]
for name in args[1:]:
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete()
    bpy.ops.import_scene.fbx(filepath=os.path.join(root, name + '.fbx'))
    objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs)
    mats = sorted({s.material.name for o in objs for s in o.material_slots if s.material})
    dims = [tuple(round(v, 2) for v in o.dimensions) for o in objs]
    texs = set()
    for o in objs:
        for s in o.material_slots:
            if s.material and s.material.use_nodes:
                for n in s.material.node_tree.nodes:
                    if n.type == 'TEX_IMAGE' and n.image: texs.add(os.path.basename(n.image.filepath))
    print(f'PROBE {name}: objs={len(objs)} tris={tris} mats={mats} dims={dims[:2]} scale={[tuple(round(s,3) for s in o.scale) for o in objs][:1]} tex={texs}')
