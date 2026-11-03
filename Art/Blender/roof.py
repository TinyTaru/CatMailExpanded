"""Exterior roof for the post office, replacing the grey light-blocker boxes when seen from outside.

    blender -b --factory-startup --python roof.py -- [preview.png]

Every roof surface lies on or above the bottom of the light-blocker box it covers. The game keeps the
rooms below those boxes (they are what shade the interiors), so nothing built here can be seen from
inside. Overhangs are only added on edges that face the outdoors, never over a neighbouring block.

Outputs (Unity world space):
  roof_shingles.cmemesh   roof planes, UVs tile tex_roof_shingles.png (1.6 m per repeat)
  roof_trim.cmemesh       fascia, soffits, ridge/hip caps, gable cladding; palette tex_roof_trim.png
"""
import bpy, bmesh, sys, os, math
from mathutils import Vector
sys.path.insert(0, os.path.dirname(__file__))
import cme_export as C

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Build')
args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
PREVIEW = args[0] if args else None

TILE = 1.6          # metres of roof per texture repeat
OVER = 0.4          # eave overhang on outdoor edges

# light-blocker boxes (Decompiled/Reference/light_blockers.txt): x0, z0, x1, z1, bottom y
# The dock pair (tilted) forms a shallow gable over x -13.97..1.28, z 2.03..17.56, bottom peak 6.07.
BLOCKS = {
    'dock':   dict(rect=(-13.97, 2.03, 1.28, 17.56), base=4.74, kind='gable', pitch=25),
    # raised to clear the yard facade (top 4.61); walls below are lower, so 'fill' clads the gap down to 3.95
    'south':  dict(rect=(-2.41, -7.13, 7.67, 3.87), base=4.80, kind='hip', pitch=30, fill=3.95),
    'centre': dict(rect=(1.21, 1.22, 11.29, 15.66), base=4.22, kind='hip', pitch=30),
    'east':   dict(rect=(7.57, -0.69, 17.65, 10.31), base=4.22, kind='hip', pitch=30),
    'north':  dict(rect=(4.52, 15.66, 12.40, 30.10), base=4.22, kind='hip', pitch=30),
    'porch':  dict(rect=(7.16, -2.66, 13.38, -0.02), base=4.22, kind='hip', pitch=30),
    # the customer room has no light blocker; its solid wood ceiling tops out at 4.49, so start just above it.
    # (The hot room is left open on purpose: its skylights should keep showing the sky.)
    'customer':  dict(rect=(17.65, -0.78, 20.06, 13.35), base=4.52, kind='hip', pitch=30, fill=3.95),
    'customer2': dict(rect=(13.28, 10.31, 17.65, 13.35), base=4.52, kind='hip', pitch=30, fill=3.95),
}
# walls the yard facade already finishes (its own trim sits at the wall top): no overhang there
NO_OVERHANG = set()   # the south roof starts above the facade top, so its overhang clears the facade

# boat entrance frame on the dock's west gable (measured in game)
ARCH_Z, ARCH_HW, ARCH_CORNER, ARCH_TOP = 10.0, 3.24, 5.12, 5.73

PALETTE = {
    'fascia': (0.95, 0.93, 0.89),
    'soffit': (0.86, 0.85, 0.90),
    'cap':    (0.33, 0.37, 0.55),
    'gable':  (0.80, 0.80, 0.87),
    'gable2': (0.74, 0.75, 0.83),
    'vent':   (0.36, 0.38, 0.50),
}


def overlaps(a, b):
    return a[0] < b[2] and b[0] < a[2] and a[1] < b[3] and b[1] < a[3]


def edge_overhangs(name):
    """W, E, S, N overhang for a block: OVER unless the strip outside that edge touches another block."""
    x0, z0, x1, z1 = BLOCKS[name]['rect']
    strips = {'W': (x0 - OVER, z0, x0 - 0.01, z1), 'E': (x1 + 0.01, z0, x1 + OVER, z1),
              'S': (x0, z0 - OVER, x1, z0 - 0.01), 'N': (x0, z1 + 0.01, x1, z1 + OVER)}
    res = {}
    for e, strip in strips.items():
        # an edge keeps its overhang unless neighbouring blocks cover most of it (where a neighbour only
        # clips one end, the two roofs simply meet there)
        along = 0 if e in ('W', 'E') else 1
        length = (strip[3] - strip[1]) if along == 0 else (strip[2] - strip[0])
        covered = 0.0
        for n, b in BLOCKS.items():
            if n == name or not overlaps(strip, b['rect']):
                continue
            r = b['rect']
            if along == 0:
                covered += min(strip[3], r[3]) - max(strip[1], r[1])
            else:
                covered += min(strip[2], r[2]) - max(strip[0], r[0])
        blocked = (name, e) in NO_OVERHANG or covered > 0.5 * length
        res[e] = 0.0 if blocked else OVER
    return res


def clip(poly, f):
    """Sutherland-Hodgman: keep the part of a convex xz polygon where f(x, z) <= 0 (f linear)."""
    out = []
    n = len(poly)
    for i in range(n):
        p, q = poly[i], poly[(i + 1) % n]
        fp, fq = f(*p), f(*q)
        if fp <= 0:
            out.append(p)
        if (fp <= 0) != (fq <= 0):
            t = fp / (fp - fq)
            out.append((p[0] + (q[0] - p[0]) * t, p[1] + (q[1] - p[1]) * t))
    return out


def roof_planes(name):
    """Planes as (edge, height function, inward direction, eave point) for a block's roof."""
    b = BLOCKS[name]
    x0, z0, x1, z1 = b['rect']
    o = edge_overhangs(name)
    t = math.tan(math.radians(b['pitch']))
    base = b['base']
    X0, X1, Z0, Z1 = x0 - o['W'], x1 + o['E'], z0 - o['S'], z1 + o['N']
    # each plane rises inward from its wall line at `base`; the overhang continues the same plane outward
    planes = {
        'S': (lambda x, z: base + (z - z0) * t, (0, 1)),
        'N': (lambda x, z: base + (z1 - z) * t, (0, -1)),
        'W': (lambda x, z: base + (x - x0) * t, (1, 0)),
        'E': (lambda x, z: base + (x1 - x) * t, (-1, 0)),
    }
    if b['kind'] == 'gable':
        planes = {k: v for k, v in planes.items() if k in ('S', 'N')}
    return (X0, Z0, X1, Z1), planes, o


shingle_faces = []   # (list of Unity (x,y,z), list of uv)
trim_faces = []      # (list of Unity (x,y,z), palette key)


def uv_for(name, pts, inward):
    ix, iz = inward
    ux, uz = -iz, ix
    slope = math.sqrt(1 + math.tan(math.radians(BLOCKS[name]['pitch'])) ** 2)
    return [((x * ux + z * uz) / TILE, (x * ix + z * iz) * slope / TILE) for x, _, z in pts]


INWARD = {'S': (0, 1), 'N': (0, -1), 'W': (1, 0), 'E': (-1, 0)}
STRIPS = {}   # name -> list of (edge, outer start (x,z), outer end (x,z), inner start, inner end)


def eave_spans(name, edge):
    """Open stretches of a wall line that get an overhang. A gable roof's eaves stop short of a
    neighbouring hip roof's overhang, so two eaves never cross."""
    grow = OVER if BLOCKS[name]['kind'] == 'gable' else 0.0
    return exposed_spans(name, edge, grow)


def add_roof(name):
    (X0, Z0, X1, Z1), planes, o = roof_planes(name)
    b = BLOCKS[name]
    x0, z0, x1, z1 = b['rect']
    gable = b['kind'] == 'gable'
    # core: the roof over the walls (a gable also runs out over its rake overhangs)
    cx0, cx1 = (X0, X1) if gable else (x0, x1)
    rect = [(cx0, z0), (cx1, z0), (cx1, z1), (cx0, z1)]
    for k, (h, inward) in planes.items():
        poly = rect
        for j, (hj, _) in planes.items():
            if j == k:
                continue
            poly = clip(poly, lambda x, z, h=h, hj=hj: h(x, z) - hj(x, z) - 1e-6)
            if len(poly) < 3:
                break
        if len(poly) < 3:
            continue
        pts = [(x, h(x, z), z) for x, z in poly]
        shingle_faces.append((pts, uv_for(name, pts, inward)))
    # overhang strips, continuing each plane past its wall line
    strips = []
    edges = ('S', 'N') if gable else ('S', 'N', 'W', 'E')
    spans = {e: eave_spans(name, e) for e in ('S', 'N', 'W', 'E')}

    def open_at(edge, u):
        return any(s0 - 0.02 <= u <= s1 + 0.02 for s0, s1 in spans[edge])

    for e in edges:
        h, inward = planes[e]
        for s0, s1 in spans[e]:
            if e in ('S', 'N'):
                wz = z0 if e == 'S' else z1
                oz = wz - OVER if e == 'S' else wz + OVER
                if gable:
                    # eaves run on to the rake overhang at an open gable end
                    a0 = X0 if abs(s0 - x0) < 0.02 else s0
                    a1 = X1 if abs(s1 - x1) < 0.02 else s1
                    i0, i1, o0, o1 = (a0, wz), (a1, wz), (a0, oz), (a1, oz)
                else:
                    d0 = OVER if abs(s0 - x0) < 0.02 and open_at('W', wz) else 0.0
                    d1 = OVER if abs(s1 - x1) < 0.02 and open_at('E', wz) else 0.0
                    i0, i1, o0, o1 = (s0, wz), (s1, wz), (s0 - d0, oz), (s1 + d1, oz)
            else:
                wx = x0 if e == 'W' else x1
                ox = wx - OVER if e == 'W' else wx + OVER
                d0 = OVER if abs(s0 - z0) < 0.02 and open_at('S', wx) else 0.0
                d1 = OVER if abs(s1 - z1) < 0.02 and open_at('N', wx) else 0.0
                i0, i1, o0, o1 = (wx, s0), (wx, s1), (ox, s0 - d0), (ox, s1 + d1)
            poly = [i0, i1, o1, o0]
            pts = [(x, h(x, z), z) for x, z in poly]
            shingle_faces.append((pts, uv_for(name, pts, inward)))
            strips.append((e, o0, o1, i0, i1))
    STRIPS[name] = strips
    return (X0, Z0, X1, Z1), planes, o


def quad(pts, key):
    trim_faces.append((pts, key))


def beam(p, q, w, h, key, up=(0, 1, 0)):
    """A rectangular bar along p->q (Unity coords), its top face `h` above the line."""
    p, q = Vector(p), Vector(q)
    d = (q - p).normalized()
    u = Vector(up)
    u = (u - d * u.dot(d)).normalized()
    s = d.cross(u).normalized() * (w / 2)
    lo, hi = -0.02, h
    c = [p + s + u * lo, p - s + u * lo, p - s + u * hi, p + s + u * hi,
         q + s + u * lo, q - s + u * lo, q - s + u * hi, q + s + u * hi]
    for f in ((0, 1, 2, 3), (4, 7, 6, 5), (0, 3, 7, 4), (1, 5, 6, 2), (3, 2, 6, 7), (0, 4, 5, 1)):
        quad([tuple(c[i]) for i in f], key)


def exposed_spans(name, edge, grow=0.0):
    """Parts of a block's wall line that face the outdoors (no neighbouring block just outside them).
    `grow` widens each neighbour along the wall line (to make room for its overhang)."""
    x0, z0, x1, z1 = BLOCKS[name]['rect']
    if edge in ('W', 'E'):
        a, b = z0, z1
        strip = (x0 - 0.3, x0 - 0.01) if edge == 'W' else (x1 + 0.01, x1 + 0.3)
        cover = [(r[1], r[3]) for n, bl in BLOCKS.items() if n != name for r in [bl['rect']]
                 if r[0] < strip[1] and strip[0] < r[2]]
    else:
        a, b = x0, x1
        strip = (z0 - 0.3, z0 - 0.01) if edge == 'S' else (z1 + 0.01, z1 + 0.3)
        cover = [(r[0], r[2]) for n, bl in BLOCKS.items() if n != name for r in [bl['rect']]
                 if r[1] < strip[1] and strip[0] < r[3]]
    spans = [(a, b)]
    cover = [(c0 - grow, c1 + grow) for c0, c1 in cover]
    for c0, c1 in cover:
        nxt = []
        for s0, s1 in spans:
            if c1 <= s0 or c0 >= s1:
                nxt.append((s0, s1))
                continue
            if c0 > s0:
                nxt.append((s0, c0))
            if c1 < s1:
                nxt.append((c1, s1))
        spans = nxt
    return [(s0, s1) for s0, s1 in spans if s1 - s0 > 0.05]


def add_fill(name, o):
    """Lap-board cladding from the walls' top up to a raised roof, on outward-facing wall lines only,
    plus a fascia board where that edge has no overhang (so the roof never shows a bare edge)."""
    b = BLOCKS[name]
    if 'fill' not in b:
        return
    x0, z0, x1, z1 = b['rect']
    lo, hi = b['fill'], b['base']
    for e in ('W', 'E', 'S', 'N'):
        for s0, s1 in exposed_spans(name, e):
            off = 0.02
            if e == 'W':
                P = lambda u, y: (x0 - off, y, u)
            elif e == 'E':
                P = lambda u, y: (x1 + off, y, u)
            elif e == 'S':
                P = lambda u, y: (u, y, z0 - off)
            else:
                P = lambda u, y: (u, y, z1 + off)
            y, k = lo, 0
            while y < hi - 0.005:
                y2 = min(hi, y + 0.2)
                quad([P(s0, y), P(s1, y), P(s1, y2), P(s0, y2)], 'gable' if k % 2 == 0 else 'gable2')
                y, k = y2, k + 1


def add_trim(name, rectx, planes, o):
    b = BLOCKS[name]
    X0, Z0, X1, Z1 = rectx
    x0, z0, x1, z1 = b['rect']
    t = math.tan(math.radians(b['pitch']))
    base = b['base']
    # fascia along each overhang strip's outer edge, soffit under the strip
    for e, o0, o1, i0, i1 in STRIPS.get(name, []):
        ye = base - OVER * t
        beam((o0[0], ye - 0.08, o0[1]), (o1[0], ye - 0.08, o1[1]), 0.05, 0.1, 'fascia')
        quad([(o0[0], ye - 0.01, o0[1]), (i0[0], ye - 0.01, i0[1]), (i1[0], ye - 0.01, i1[1]), (o1[0], ye - 0.01, o1[1])], 'soffit')
    if b['kind'] == 'gable':
        hS, hN = planes['S'][0], planes['N'][0]
        # ridge where the two slopes meet
        zr = (z0 + z1) / 2
        lo_, hi_ = Z0, Z1
        for _ in range(40):
            zr = (lo_ + hi_) / 2
            if hS(0, zr) < hN(0, zr):
                lo_ = zr
            else:
                hi_ = zr
        yr = hS(0, zr)
        beam((X0, yr, zr), (X1, yr, zr), 0.16, 0.07, 'cap')
        # gable ends (west faces the sea, east is mostly behind the centre roof), with rake boards
        for gx, side, overhang in ((x0 - 0.07, -1, o['W']), (x1 + 0.03, 1, o['E'])):
            ys, yn = base, base
            tri = [(gx, ys, z0), (gx, yr, zr), (gx, yn, z1)] if side < 0 else [(gx, yn, z1), (gx, yr, zr), (gx, ys, z0)]
            # lap boards: horizontal bands clipped to the triangle; on the west gable the boat entrance
            # arch (outer frame z 6.78..13.22, corners at 5.12, apex 5.72) stays uncovered
            def zs(yy):
                f = (yy - base) / (yr - base)
                return z0 + (zr - z0) * f, z1 - (z1 - zr) * f

            def arch_half(yy):
                if side > 0 or yy >= ARCH_TOP:
                    return 0.0
                return ARCH_HW if yy <= ARCH_CORNER else ARCH_HW * (ARCH_TOP - yy) / (ARCH_TOP - ARCH_CORNER)

            band = 0.24
            ys = [base + band * i for i in range(int((yr - base) / band) + 1)]
            if side < 0:
                ys += [ARCH_CORNER, ARCH_TOP]
            ys = sorted(set(round(v, 4) for v in ys if base <= v < yr)) + [yr]
            for y, y2 in zip(ys, ys[1:]):
                k = int((y - base) / band + 1e-6)
                key = 'gable' if k % 2 == 0 else 'gable2'
                a0, a1 = zs(y)
                b0, b1 = zs(y2)
                h, h2 = arch_half(y), arch_half(y2)
                if h <= 0 and h2 <= 0:
                    pieces = [((a0, b0), (a1, b1))]
                else:
                    pieces = [((a0, b0), (ARCH_Z - h, ARCH_Z - h2)), ((ARCH_Z + h, ARCH_Z + h2), (a1, b1))]
                for (l0, l1), (r0, r1) in pieces:
                    pts = [(gx, y, l0), (gx, y2, l1), (gx, y2, r1), (gx, y, r0)]
                    if side > 0:
                        pts.reverse()
                    quad(pts, key)
            # round vent in the west gable
            if side < 0:
                cy, cz, r = base + (yr - base) * 0.42, zr, 0.38
                ring = [(gx - 0.02, cy + math.sin(a) * r, cz + math.cos(a) * r) for a in (i / 12 * math.tau for i in range(12))]
                for i in range(12):
                    pa, pb = ring[i], ring[(i + 1) % 12]
                    quad([(gx - 0.02, cy, cz), pa, pb, (gx - 0.02, cy, cz)], 'vent')
                for i in range(12):
                    pa, pb = ring[i], ring[(i + 1) % 12]
                    beam(pa, pb, 0.05, 0.05, 'fascia', up=(-1, 0, 0))
            # rake boards along the sloped edges (out to the gable overhang)
            gx2 = gx - overhang if side < 0 else gx + overhang
            for (za, ya) in ((z0 - o['S'], base - o['S'] * t), (z1 + o['N'], base - o['N'] * t)):
                beam((gx2 + side * 0.02, ya - 0.05, za), (gx2 + side * 0.02, yr - 0.05, zr), 0.06, 0.16, 'fascia', up=(0, 1, 0))
    else:
        # ridge and hip caps: the hip roof's peak lines
        hx = (x0 + x1) / 2
        hz = (z0 + z1) / 2
        half = min(x1 - x0, z1 - z0) / 2
        yr = base + half * t
        if (x1 - x0) >= (z1 - z0):
            ra, rb = (x0 + half, yr, hz), (x1 - half, yr, hz)
        else:
            ra, rb = (hx, yr, z0 + half), (hx, yr, z1 - half)
        if Vector(ra) != Vector(rb):
            beam(ra, rb, 0.16, 0.07, 'cap')
        # hips from each ridge end down to the wall corners (extended to the eave corners)
        corners = []
        for (wx, ex, wz, ez) in ((x0, 'W', z0, 'S'), (x1, 'E', z0, 'S'), (x1, 'E', z1, 'N'), (x0, 'W', z1, 'N')):
            both = any(st[0] == ex and min(st[1][1], st[2][1]) - 0.02 <= wz <= max(st[1][1], st[2][1]) + 0.02
                       for st in STRIPS.get(name, [])) and                    any(st[0] == ez and min(st[1][0], st[2][0]) - 0.02 <= wx <= max(st[1][0], st[2][0]) + 0.02
                       for st in STRIPS.get(name, []))
            dx = OVER if both else 0.0
            corners.append((wx - dx if ex == 'W' else wx + dx, wz - dx if ez == 'S' else wz + dx))
        for cx, cz in corners:
            # height of the roof at the eave corner (min of the planes)
            y = min(h(cx, cz) for h, _ in planes.values())
            end = ra if Vector((cx - ra[0], cz - ra[2])).length <= Vector((cx - rb[0], cz - rb[2])).length else rb
            beam((cx, y, cz), end, 0.12, 0.06, 'cap')


def build():
    C.reset_scene()
    for name in BLOCKS:
        rectx, planes, o = add_roof(name)
        add_trim(name, rectx, planes, o)
        add_fill(name, o)
    # every roof plane gets a plain underside a little below it, so overhangs never look paper thin
    # and you can't see the sky through them from below
    for pts, _ in shingle_faces:
        trim_faces.append(([(x, y - 0.03, z) for x, y, z in pts], 'soffit'))
    # ---- shingles mesh
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new('UVMap')
    for pts, uvs in shingle_faces:
        vs = [bm.verts.new(C.U(*p)) for p in pts]
        try:
            f = bm.faces.new(vs)
        except ValueError:
            continue
        f.normal_update()
        if f.normal.z < 0:   # face up/out
            f.normal_flip()
            for loop, uv in zip(f.loops, list(reversed(uvs))):
                loop[uvl].uv = uv
        else:
            for loop, uv in zip(f.loops, uvs):
                loop[uvl].uv = uv
    me = bpy.data.meshes.new('roof_shingles')
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new('roof_shingles', me)
    bpy.context.collection.objects.link(obj)
    me.materials.append(bpy.data.materials.new('shingles'))
    C.export_cmemesh(obj, os.path.join(OUT, 'roof_shingles.cmemesh'), smooth_angle=1)
    # ---- trim mesh (palette)
    keys = list(PALETTE)
    SW = 16
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new('UVMap')
    for pts, key in trim_faces:
        uniq = []
        for p in pts:
            if not uniq or (Vector(p) - Vector(uniq[-1])).length > 1e-6:
                uniq.append(p)
        if len(uniq) > 2 and (Vector(uniq[0]) - Vector(uniq[-1])).length < 1e-6:
            uniq.pop()
        if len(uniq) < 3:
            continue
        u = (keys.index(key) * SW + SW / 2) / (SW * len(keys))
        # trim pieces are thin and seen from every side: emit both windings
        for order in (uniq, list(reversed(uniq))):
            vs = [bm.verts.new(C.U(*p)) for p in order]
            try:
                f = bm.faces.new(vs)
            except ValueError:
                continue
            for loop in f.loops:
                loop[uvl].uv = (u, 0.5)
    me = bpy.data.meshes.new('roof_trim')
    bm.to_mesh(me)
    bm.free()
    obj2 = bpy.data.objects.new('roof_trim', me)
    bpy.context.collection.objects.link(obj2)
    me.materials.append(bpy.data.materials.new('trim'))
    C.export_cmemesh(obj2, os.path.join(OUT, 'roof_trim.cmemesh'), smooth_angle=1)
    write_palette(os.path.join(OUT, 'tex_roof_trim.png'), [PALETTE[k] for k in keys], SW)
    print('roof faces', len(shingle_faces), 'trim faces', len(trim_faces))
    if PREVIEW:
        C.reset_scene()
        a = C.import_cmemesh(os.path.join(OUT, 'roof_shingles.cmemesh'), os.path.join(OUT, 'tex_roof_shingles.png'))
        C.import_cmemesh(os.path.join(OUT, 'roof_trim.cmemesh'), os.path.join(OUT, 'tex_roof_trim.png'))
        C.render_preview(a, PREVIEW, None, res=1400, angle=(55, 0, 215))


def write_palette(path, cols, SW):
    import struct, zlib
    W, H = SW * len(cols), SW
    row = bytes(1) + b''.join(bytes(int(c * 255 + 0.5) for c in col) * SW for col in cols)

    def chunk(t, d):
        return struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)
    png = bytes([137, 80, 78, 71, 13, 10, 26, 10]) + chunk(b'IHDR', struct.pack('>IIBBBBB', W, H, 8, 2, 0, 0, 0)) + \
        chunk(b'IDAT', zlib.compress(row * H, 9)) + chunk(b'IEND', b'')
    open(path, 'wb').write(png)


build()
