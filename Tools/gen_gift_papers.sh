#!/bin/bash
# Paints the gift wrapping papers with FLUX.2 [klein] 4B (Apache 2.0, local, through mflux).
# One flat pattern per paper -> Art/Textures/gift_raw/<id>.png; Tools/make_gift_textures.py makes them tile.
cd "$(dirname "$0")/.."
OUT=Art/Textures/gift_raw
mkdir -p "$OUT"
seed=300
while IFS="|" read -r id subject; do
  [ -z "$id" ] && continue
  seed=$((seed + 11))
  [ -f "$OUT/$id.png" ] && continue
  ~/.local/bin/mflux-generate-flux2 --model flux2-klein-4b --steps 4 --width 768 --height 768 --seed $seed \
    --prompt "Gift wrapping paper, flat repeating pattern filling the entire frame edge to edge: $subject. Seen straight on, evenly lit, cozy hand-painted video game texture, no text, no border, no objects other than the pattern." \
    --output "$OUT/$id.png" >/dev/null 2>&1 && echo "painted $id"
done <<'LIST'
stars|deep midnight blue paper scattered with small golden stars and little crescent moons
candy|bold diagonal red and white candy cane stripes
dots|soft pink paper with neat white polka dots
flowers|cream paper covered in small pastel spring flowers and green leaves
fish|teal paper with rows of cute little orange fish
plaid|green and red tartan plaid
paws|plain brown kraft paper with small white cat paw prints
gold|shiny gold foil paper with a subtle embossed diamond lattice
LIST
echo ALLDONE
