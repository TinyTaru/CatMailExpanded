import bpy, sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
import importlib, cme_export as C
importlib.reload(C)
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
C.reset_scene()

wood = C.painterly_mat('wood', (0.62, 0.42, 0.26), 0.10, 5, (0.55, 0.36, 0.22))
wood_dark = C.painterly_mat('wood_dark', (0.36, 0.24, 0.16), 0.08, 5)
grille = C.painterly_mat('grille', (0.22, 0.33, 0.44), 0.10, 9, (0.26, 0.38, 0.5))
cone = C.painterly_mat('cone', (0.16, 0.17, 0.2), 0.06, 8)
cap = C.painterly_mat('cap', (0.93, 0.78, 0.45), 0.06, 8)
trim = C.painterly_mat('trim', (0.94, 0.87, 0.74), 0.05, 6)

W, H, D = 0.42, 0.66, 0.34
parts = []
body = C.rounded_box('body', (W, D, H), 0.035, 3, (0, 0, H / 2 + 0.04)); C.assign(body, wood); parts.append(body)
# front panel recessed
panel = C.rounded_box('panel', (W - 0.06, 0.03, H - 0.07), 0.012, 2, (0, -D / 2 - 0.004, H / 2 + 0.04)); C.assign(panel, grille); parts.append(panel)
# woofer
for (z, r, cr) in [(0.27, 0.13, 0.045), (0.55, 0.065, 0.026)]:
    ring = C.cylinder('ring', r + 0.018, 0.03, 32, (0, -D / 2 - 0.02, z), (math.radians(90), 0, 0), 0.006); C.assign(ring, trim); parts.append(ring)
    cn = C.cylinder('cone', r, 0.03, 32, (0, -D / 2 - 0.03, z), (math.radians(90), 0, 0), 0.0)
    # dish the cone: scale centre verts inward
    me = cn.data
    for v in me.vertices:
        if abs(v.co.x) < 1e-4 and abs(v.co.y) < 1e-4:
            pass
    C.assign(cn, cone); parts.append(cn)
    dust = C.cylinder('cap', cr, 0.03, 24, (0, -D / 2 - 0.045, z), (math.radians(90), 0, 0), 0.008); C.assign(dust, cap); parts.append(dust)
# top trim strip and little knob
top = C.rounded_box('top', (W + 0.02, D + 0.02, 0.03), 0.01, 2, (0, 0, H + 0.04)); C.assign(top, wood_dark); parts.append(top)
base = C.rounded_box('base', (W + 0.02, D + 0.02, 0.03), 0.01, 2, (0, 0, 0.055)); C.assign(base, wood_dark); parts.append(base)
for x in (-1, 1):
    for y in (-1, 1):
        f = C.cylinder('foot', 0.025, 0.04, 12, (x * (W / 2 - 0.05), y * (D / 2 - 0.05), 0.02), (0, 0, 0), 0.006); C.assign(f, wood_dark); parts.append(f)
# small music note badge
badge = C.cylinder('badge', 0.028, 0.012, 20, (W / 2 - 0.07, -D / 2 - 0.022, H - 0.02), (math.radians(90), 0, 0), 0.004); C.assign(badge, cap); parts.append(badge)

for p in parts:
    C.apply_all(p)
obj = C.join(parts, 'Speaker')
tex = os.path.join(ROOT, 'Assets', 'Build', 'tex_speaker.png')
C.bake_textures(obj, tex, 1024)
C.export_cmemesh(obj, os.path.join(ROOT, 'Assets', 'Build', 'speaker.cmemesh'))
C.render_preview(obj, os.path.join(ROOT, 'Art', 'Previews', 'speaker.png'), tex, angle=(75, 0, -30))
