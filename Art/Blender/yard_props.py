"""Synty props for the yard (Gang Warfare pack): potted palm and watering can.

    blender -b --factory-startup --python yard_props.py -- <out_dir>
"""
import bpy, sys, os, shutil
sys.path.insert(0, os.path.dirname(__file__))
import cme_export as C
import synty_lib as S

OUT = sys.argv[sys.argv.index('--') + 1]
GANG = S.PACKS + '/Gang/Synty/PolygonGangWarfare'
ATLAS = GANG + '/Textures/Alts/PolygonGangWarfare_01_A.png'

for src, name in (('SM_Prop_Plant_Palm_01', 'syn_palm'), ('SM_Prop_WateringCan_01', 'syn_wateringcan')):
    C.reset_scene()
    o = S.load(f'{GANG}/Models/{src}.fbx', ATLAS, name)
    print('SIZE', name, tuple(round(v, 2) for v in o.dimensions))
    C.export_cmemesh(o, os.path.join(OUT, name + '.cmemesh'), smooth_angle=25)
shutil.copy(ATLAS, os.path.join(OUT, 'tex_synty_gang.png'))
