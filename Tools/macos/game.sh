#!/bin/bash
# macOS counterpart of Tools/game.sh: drive the game through the CME dev command file (DevMode = true).
G="${CME_GAME_DIR:-/Applications/CatMailCo LAN}"
U="$G/UserData/CME"
L="$G/MelonLoader/Latest.log"
case "$1" in
  start)
    if pgrep -x CatMailCo >/dev/null && [ "$2" != "--force" ]; then echo "game already running - stop it first or pass --force"; exit 1; fi
    pkill -x CatMailCo; sleep 1; rm -f "$U/out.txt"
    (cd "$G" && nohup "./Launch CatMailCo (Mods).command" >/dev/null 2>&1 &)
    for i in $(seq 1 120); do sleep 1; grep -q 'Scene loaded: sc_MainMenu' "$L" 2>/dev/null && break; done
    sleep 2; echo started;;
  stop) pkill -x CatMailCo; sleep 2; echo stopped;;
  cmd) shift; mkdir -p "$U"; rm -f "$U/out.txt"; printf '%s\n' "$@" > "$U/cmd.txt"
    for i in $(seq 1 100); do sleep 0.3; grep -q '#done' "$U/out.txt" 2>/dev/null && break; done
    sleep ${WAIT:-0}; cat "$U/out.txt";;
  log) grep -E "${2:-Cat_Mail_Expanded|ERROR}" "$L" | tail -${3:-30};;
esac
