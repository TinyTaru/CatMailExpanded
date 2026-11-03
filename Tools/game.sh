#!/bin/bash
# Helper to drive the game via the CME dev command file.
G="/c/Program Files (x86)/Steam/steamapps/common/CatMailCo"
U="$G/UserData/CME"
T="$(cd "$(dirname "$0")" && pwd -W)"
case "$1" in
  start) if tasklist | grep -qi CatMailCo.exe && [ "$2" != "--force" ]; then echo "game already running (maybe yours) - stop it first or pass --force"; exit 1; fi; taskkill //IM CatMailCo.exe //F >/dev/null 2>&1; sleep 1; rm -f "$U/out.txt"; powershell -NoProfile -ExecutionPolicy Bypass -File "$T/offscreen.ps1" backup >/dev/null; powershell -NoProfile -ExecutionPolicy Bypass -File "$T/offscreen.ps1" windowed >/dev/null; (cd "$G" && ./CatMailCo.exe >/dev/null 2>&1 &); powershell -NoProfile -ExecutionPolicy Bypass -File "$T/offscreen.ps1" hide; for i in $(seq 1 60); do sleep 1; grep -q 'Scene loaded: sc_MainMenu' "$G/MelonLoader/Latest.log" 2>/dev/null && break; done; sleep 2; powershell -NoProfile -ExecutionPolicy Bypass -File "$T/offscreen.ps1" hide >/dev/null; echo started;;
  stop) taskkill //IM CatMailCo.exe //F >/dev/null 2>&1; sleep 2; powershell -NoProfile -ExecutionPolicy Bypass -File "$T/offscreen.ps1" restore >/dev/null; echo stopped;;
  hide) powershell -NoProfile -ExecutionPolicy Bypass -File "$T/offscreen.ps1" hide;;
  cmd) shift; rm -f "$U/out.txt"; printf '%s\n' "$@" > "$U/cmd.txt"; for i in $(seq 1 100); do sleep 0.3; grep -q '#done' "$U/out.txt" 2>/dev/null && break; done; sleep ${WAIT:-0}; cat "$U/out.txt";;
  log) grep -E "${2:-Cat_Mail_Expanded|ERROR}" "$G/MelonLoader/Latest.log" | tail -${3:-30};;
esac
