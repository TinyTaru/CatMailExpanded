"""Props for the gift wrapping room (see GiftRoom.cs). Unity axes (x right, y up, z forward). Usage:
  blender -b --factory-startup --python gifts.py -- [bench|decor|all]
Outputs (with baked painterly textures):
  gift_bench   the wrapping bench against the east wall, around its own origin (floor level, centre of the top):
               a 1.0 x 2.0 m top at 0.85 (4x8 storage grid), a lower shelf with spare rolls and tape, and the
               pegboard behind it with the brackets the paper rolls rest on and the dowel the ribbon spools hang on
               (the rolls and spools themselves are made at runtime, so they can be clicked and use the real papers)
  gift_decor   world space: a floor mat inside the door, a round display table in the south-west corner and a paper
               bin with scraps (the big rug and the potted plant are made at runtime: the rug is a painted texture,
               the plant is the game's own)
"""
import bpy, sys, os, math, random
sys.path.insert(0, os.path.dirname(__file__))
import importlib, cme_export as C
importlib.reload(C)
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Build')
which = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else ['all']
PREVIEW = os.environ.get('CME_PREVIEW')

# keep in sync with GiftRoom.cs
TW, TL, TH = 1.0, 2.0, 0.85          # bench top (x, z) and height
BACK = 0.5                            # pegboard plane, local x (the wall side)
ROLL_X, ROLL_Y = 0.36, (1.42, 1.72)   # paper roll axis (local x, heights)
ROLL_Z = (-0.75, -0.25, 0.25, 0.75)
DOWEL_X, DOWEL_Y = 0.38, 2.1          # ribbon spool dowel


def mats():
    P = C.painterly_mat
    return dict(
        wood=P('wood', (0.66, 0.49, 0.34), 0.08, 5, (0.60, 0.44, 0.30)),
        wood2=P('wood2', (0.52, 0.37, 0.26), 0.08, 5),
        board=P('board', (0.86, 0.80, 0.70), 0.05, 7, (0.82, 0.76, 0.66)),
        hole=P('hole', (0.45, 0.40, 0.34), 0.03, 6),
        brass=P('brass', (0.80, 0.64, 0.30), 0.06, 6),
        red=P('red', (0.82, 0.30, 0.32), 0.05, 6),
        pink=P('pink', (0.93, 0.62, 0.70), 0.05, 6),
        teal=P('teal', (0.27, 0.60, 0.62), 0.05, 6),
        cream=P('cream', (0.95, 0.91, 0.82), 0.05, 6),
        tape=P('tape', (0.86, 0.80, 0.64), 0.05, 6),
        metal=P('metal', (0.58, 0.60, 0.65), 0.05, 6),
        mat=P('mat', (0.70, 0.30, 0.36), 0.06, 6, (0.64, 0.26, 0.32)),
        mat2=P('mat2', (0.93, 0.80, 0.52), 0.05, 6),
        wicker=P('wicker', (0.72, 0.56, 0.34), 0.12, 14, (0.6, 0.45, 0.27)),
        gold=P('gold', (0.92, 0.76, 0.35), 0.05, 6),
        purple=P('purple', (0.58, 0.42, 0.80), 0.05, 6),
        green=P('green', (0.38, 0.66, 0.40), 0.08, 6, (0.30, 0.56, 0.32)),
        terracotta=P('terracotta', (0.78, 0.45, 0.32), 0.06, 6),
        soil=P('soil', (0.30, 0.22, 0.16), 0.05, 6),
    )


def build_bench(M):
    parts = []
    B = C.ubox
    hx, hz = TW / 2, TL / 2
    parts.append(B('top', (0, TH - 0.03, 0), (TW, 0.06, TL), M['wood'], 0.012))
    parts.append(B('apron', (-hx + 0.02, TH - 0.1, 0), (0.03, 0.1, TL - 0.04), M['wood2'], 0.006))
    parts.append(B('shelf', (0, 0.2, 0), (TW - 0.1, 0.04, TL - 0.1), M['wood2'], 0.01))
    for (x, z) in ((-hx + 0.05, -hz + 0.05), (hx - 0.05, -hz + 0.05), (-hx + 0.05, hz - 0.05), (hx - 0.05, hz - 0.05)):
        parts.append(B('leg', (x, (TH - 0.06) / 2, z), (0.07, TH - 0.06, 0.07), M['wood2'], 0.01))
    # spare rolls, tape and scissors on the lower shelf
    for i, (z, m) in enumerate(((-0.6, M['red']), (-0.45, M['teal']), (-0.3, M['pink']))):
        r = C.ucyl('spare', (0.05 + i * 0.03, 0.28, z), 0.055, 0.8, m, 16)
        C.urot(r, z_deg=90)
        parts.append(r)
    t = C.ucyl('taperoll', (-0.1, 0.25, 0.45), 0.07, 0.05, M['tape'], 16)
    parts.append(t)
    parts.append(B('scissors_a', (0.15, 0.225, 0.62), (0.02, 0.012, 0.2), M['metal'], 0.003))
    parts.append(B('scissors_b', (0.19, 0.225, 0.62), (0.02, 0.012, 0.2), M['metal'], 0.003))
    parts.append(C.ucyl('scissors_h', (0.17, 0.225, 0.5), 0.045, 0.012, M['red'], 12))
    # pegboard on the wall behind, with a wooden frame
    y0, y1 = TH + 0.08, 2.35
    parts.append(B('board', (BACK + 0.02, (y0 + y1) / 2, 0), (0.025, y1 - y0, TL + 0.1), M['board'], 0.004))
    for (y, h) in ((y0, 0.05), (y1, 0.05)):
        parts.append(B('frame_h', (BACK + 0.0, y, 0), (0.05, h, TL + 0.16), M['wood'], 0.008))
    for z in (-hz - 0.06, hz + 0.06):
        parts.append(B('frame_v', (BACK + 0.0, (y0 + y1) / 2, z), (0.05, y1 - y0, 0.05), M['wood'], 0.008))
    for yy in range(14):
        for zz in range(26):
            y = y0 + 0.1 + yy * 0.1
            z = -hz + 0.04 + zz * 0.08
            if y > y1 - 0.06:
                continue
            parts.append(B('hole', (BACK + 0.006, y, z), (0.004, 0.018, 0.018), M['hole'], 0.0, 1))
    # roll brackets: a pair of brass hooks under each roll
    for y in ROLL_Y:
        for z in ROLL_Z:
            for dz in (-0.2, 0.2):
                arm = B('arm', ((BACK + ROLL_X) / 2 + 0.02, y - 0.07, z + dz), ((BACK - ROLL_X) + 0.02, 0.025, 0.025), M['brass'], 0.006)
                parts.append(arm)
                parts.append(B('lip', (ROLL_X - 0.075, y - 0.045, z + dz), (0.02, 0.06, 0.025), M['brass'], 0.006))
    # ribbon dowel on two brackets
    d = C.ucyl('dowel', (DOWEL_X, DOWEL_Y, 0), 0.016, TL + 0.04, M['wood2'], 12)
    C.urot(d, x_deg=90)
    parts.append(d)
    for z in (-hz + 0.0, hz - 0.0):
        parts.append(B('dowel_bracket', ((BACK + DOWEL_X) / 2, DOWEL_Y, z), (BACK - DOWEL_X + 0.03, 0.05, 0.03), M['wood'], 0.006))
    obj, tex = C.finish(parts, 'gift_bench', OUT, 1024)
    if PREVIEW:
        C.render_preview(obj, PREVIEW.replace('.png', '_bench.png'), tex, res=900, angle=(70, 0, 250))


def crumple(center, r, m):
    """A crumpled ball of paper: a low-poly sphere with jittered vertices."""
    import bmesh
    rnd = random.Random(int(center[0] * 100 + center[2] * 10))
    me = bpy.data.meshes.new('crumple')
    obj = bpy.data.objects.new('crumple', me)
    bpy.context.collection.objects.link(obj)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=1, radius=r)
    for v in bm.verts:
        v.co *= rnd.uniform(0.75, 1.15)
    bm.to_mesh(me)
    bm.free()
    obj.location = C.U(*center)
    me.materials.append(m)
    return obj


def build_decor(M):
    """World space (gift room: x 7.69..12.31, z 25.82..33.66, door on the west wall at z 30.97..32.57)."""
    parts = []
    B = C.ubox
    FY = 0.04
    # round floor mat inside the door
    parts.append(C.ucyl('mat', (8.45, FY + 0.006, 31.77), 0.72, 0.012, M['mat'], 40))
    parts.append(C.ucyl('mat_ring', (8.45, FY + 0.008, 31.77), 0.6, 0.012, M['mat2'], 40))
    parts.append(C.ucyl('mat_in', (8.45, FY + 0.01, 31.77), 0.5, 0.012, M['mat'], 40))
    # round display table in the south-west corner
    cx, cz = 8.35, 26.5
    parts.append(C.ucyl('disp_top', (cx, FY + 0.72, cz), 0.48, 0.04, M['wood'], 32, 0.01))
    parts.append(C.ucyl('disp_post', (cx, FY + 0.36, cz), 0.05, 0.7, M['wood2'], 12))
    parts.append(C.ucyl('disp_foot', (cx, FY + 0.02, cz), 0.3, 0.04, M['wood2'], 24, 0.01))
    parts.append(C.ucyl('disp_cloth', (cx, FY + 0.745, cz), 0.5, 0.01, M['cream'], 32))
    # paper bin with crumpled scraps by the north end of the bench
    nx, nz = 11.6, 30.95
    parts.append(C.ucyl('bin', (nx, FY + 0.2, nz), 0.17, 0.4, M['metal'], 18, 0.01))
    for (dx, dy, dz, rr, m) in ((0.03, 0.42, 0.02, 0.07, M['cream']), (-0.06, 0.41, -0.04, 0.06, M['pink']),
                               (0.05, 0.44, -0.07, 0.055, M['gold']), (-0.25, 0.05, 0.12, 0.05, M['teal']), (0.12, 0.05, 0.3, 0.045, M['cream'])):
        parts.append(crumple((nx + dx, FY + dy, nz + dz), rr, m))
    obj, tex = C.finish(parts, 'gift_decor', OUT, 1024)
    if PREVIEW:
        C.render_preview(obj, PREVIEW.replace('.png', '_decor.png'), tex, res=900, angle=(60, 0, 35))


builders = dict(bench=build_bench, decor=build_decor)
for name, fn in builders.items():
    if 'all' in which or name in which:
        C.reset_scene()
        fn(mats())
