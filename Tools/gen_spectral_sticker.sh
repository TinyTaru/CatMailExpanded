#!/bin/bash
# Paints the spectral parcel sticker with FLUX.2 [klein] 4B edit (Apache 2.0, local, through mflux), using the
# otherworldly sticker as the style reference. Tools/make_expansion_textures.py cuts out spectral_5.png.
cd "$(dirname "$0")/../Art/Textures/spectral_raw"
for s in 5 12 29; do
  ~/.local/bin/mflux-generate-flux2-edit --model flux2-klein-4b --image-paths ref_sticker.png --steps 4 --guidance 1.0 --seed $s \
    --width 768 --height 768 --output spectral_$s.png \
    --prompt "Redraw this sticker in exactly the same cute hand-drawn sticker style, with the same cream sticker border and plain white background: replace the planet, ring and moons with a cute translucent ghost: a small smiling cardboard parcel that is half see-through, pale blue-white, with a wavy ghost tail at the bottom instead of a base, little floating sparkles and a small oval hand mirror next to it. Soft pastel cyan and lavender colours."
done
