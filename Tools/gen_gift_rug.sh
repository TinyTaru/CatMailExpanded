#!/bin/bash
# Paints the gift room rug with Qwen-Image-Edit 2511 (+ the 4-step Lightning LoRA, local, through mflux)
# from the flat layout Tools/gift_rug_draft.py draws -> Assets/Build/gift_rug.png.
cd "$(dirname "$0")/.."
RAW=Art/Textures/gift_raw
mkdir -p "$RAW"
python3 Tools/gift_rug_draft.py "$RAW/rug_draft.png"
LORA=$(ls ~/.cache/huggingface/hub/models--lightx2v--Qwen-Image-Edit-2511-Lightning/snapshots/*/Qwen-Image-Edit-2511-Lightning-4steps-V1.0-bf16.safetensors | head -1)
~/.local/bin/mflux-generate-qwen-edit --model mlx-community/qwen-image-edit-2511-8bit --base-model qwen-image-edit \
  --lora-paths "$LORA" --lora-scales 1.0 --steps 4 --guidance 1.0 --seed ${SEED:-41} \
  --image-paths "$RAW/rug_draft.png" --width 768 --height 1152 \
  --prompt "Turn this flat layout into a cozy hand-tufted wool rug seen perfectly straight from above, filling the whole frame edge to edge. Keep the same layout and colours: purple outer edge, thin gold line, teal border with little pink gift boxes, cream field, a pink oval medallion with a purple gift box tied with a gold ribbon in the middle, teal stars in the corners. Soft wool texture with visible tufts, gentle hand-made irregularity, evenly lit, no shadows, no floor, no perspective, no text." \
  --output "$RAW/rug.png" && python3 -c "from PIL import Image; Image.open('$RAW/rug.png').convert('RGB').resize((768,1152)).save('Assets/Build/gift_rug.png')" && echo RUGDONE
