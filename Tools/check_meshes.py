"""Fails if any exported world-space mesh has geometry outside its expected area (catches stray parts)."""
import struct, sys, glob, os
AREAS = {  # name: (xmin, xmax, ymin, ymax, zmin, zmax) in Unity world space
    'yard_': (-14.5, -1.5, -2.0, 5.5, -10.5, 3.5),
    'door_frame': (-3.0, -1.5, -0.1, 4.0, 0.5, 3.2),
    'yard_facade': (-14.5, 8.2, -0.5, 6.0, -8.0, 1.2),
}
bad = 0
for f in glob.glob('Assets/Build/*.cmemesh'):
    n = os.path.basename(f)[:-8]
    area = AREAS.get(n) or next((v for k, v in AREAS.items() if n.startswith(k)), None)
    if not area: continue
    d = open(f, 'rb').read()
    sub, vc = struct.unpack_from('<II', d, 8)
    V = [struct.unpack_from('<3f', d, 16 + i * 48) for i in range(vc)]
    out = [v for v in V if not (area[0] <= v[0] <= area[1] and area[2] <= v[1] <= area[3] and area[4] <= v[2] <= area[5])]
    print(f"{'BAD ' if out else 'ok  '}{n}: {len(out)}/{vc} verts outside" + (f" e.g. {out[0]}" if out else ''))
    bad += bool(out)
sys.exit(1 if bad else 0)
