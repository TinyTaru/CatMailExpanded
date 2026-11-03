"""Renders a contact sheet of Synty models.  blender -b --python synty_sheet.py -- out.png pack:Name pack:Name ..."""
import bpy, sys, os
sys.path.insert(0, os.path.dirname(__file__))
import synty_lib as S

args = sys.argv[sys.argv.index('--') + 1:]
out = args[0]
cols = 7
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()
objs = []
for spec in args[1:]:
    if spec.startswith('cols='):
        cols = int(spec[5:]); continue
    pack, name = spec.split(':')
    try:
        if pack == 'n':
            o = S.load(f'{S.NATURE}/{name}.fbx', S.NATURE_ATLAS)
        elif pack == 'a':
            o = S.load(f'{S.ADV}/{name}.fbx', S.ADV_ATLAS)
        elif pack == 'g':
            o = S.load(f'{S.PACKS}/Gang/Synty/PolygonGangWarfare/Models/{name}.fbx',
                       f'{S.PACKS}/Gang/Synty/PolygonGangWarfare/Textures/Alts/PolygonGangWarfare_01_A.png')
        else:
            raise ValueError(pack)
        objs.append(o)
        print('SIZE', name, tuple(round(v, 2) for v in o.dimensions), len(o.data.polygons))
    except Exception as e:
        print('FAIL', name, e)
S.contact_sheet(objs, out, cols=cols)
