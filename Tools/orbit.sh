#!/bin/bash
# orbit.sh <name> x y z [dist] [pitch] [fov]  -> Art/Previews/orbit_<name>.png (2x2 sheet: views from yaw 0/90/180/270)
G="/c/Program Files (x86)/Steam/steamapps/common/CatMailCo"; U="$G/UserData/CME"
rm -f "$U/out.txt"; printf 'orbit %s\n' "$*" > "$U/cmd.txt"
for i in $(seq 1 120); do sleep 0.25; grep -q 'orbit done' "$U/out.txt" 2>/dev/null && break; done
/c/Users/Sabien/AppData/Local/Programs/Python/Python310/python.exe -I -c "
import sys
from PIL import Image, ImageDraw
n=sys.argv[1]; u=sys.argv[2]
ims=[Image.open(f'{u}/{n}_{i}.png').resize((800,450)) for i in range(4)]
out=Image.new('RGB',(1600,900))
for i,im in enumerate(ims):
    out.paste(im,((i%2)*800,(i//2)*450)); ImageDraw.Draw(out).text(((i%2)*800+8,(i//2)*450+6),f'yaw {i*90}',fill=(255,0,0))
out.save(f'C:/git/Mods/CatMailExpanded/Art/Previews/orbit_{n}.png')
print('sheet orbit_'+n)" "$1" "$U"
