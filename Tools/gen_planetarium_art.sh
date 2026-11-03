#!/bin/bash
# Generates the planetarium's AI-painted source images with Bria Fibo 2.0 (local, BasePaint's venv).
cd "$(dirname "$0")/../Art/Textures/planetarium_raw"
PY=~/models/fibo2-venv/bin/python
RUN=/Volumes/NVMe/git/BasePaint/basepaint/local_ai/fibo_run.py
$PY $RUN --repo briaai/Fibo-2.0-edit-turbo --image ../../../Assets/Build/sticker_restless.png --prompt-file prompt_sticker.txt --out sticker_a.png --width 1024 --height 1024 --seed 11 --steps 8
$PY $RUN --repo briaai/Fibo-2.0-edit-turbo --image ../../../Assets/Build/sticker_restless.png --prompt-file prompt_sticker.txt --out sticker_b.png --width 1024 --height 1024 --seed 23 --steps 8
$PY $RUN --repo briaai/Fibo-2.0-edit-turbo --image seed_sky.png --prompt-file prompt_sky.txt --out sky_a.png --width 1536 --height 768 --seed 5 --steps 8
$PY $RUN --repo briaai/Fibo-2.0-edit-turbo --image seed_sky.png --prompt-file prompt_sky.txt --out sky_b.png --width 1536 --height 768 --seed 9 --steps 8
echo ALLDONE
