#!/bin/bash
G=/Volumes/NVMe/git/CatMailExpanded/Tools/macos/game.sh
$G stop >/dev/null; $G start; sleep 3; $G cmd "click Singleplayer" >/dev/null; sleep 2; $G cmd "click Content0" >/dev/null
for i in $(seq 1 60); do sleep 1; $G log 'Progress:|Yard built' 5 | grep -q "$(date +%H:%M)\|Progress" && break; done
sleep 4; $G log "Cat_Mail|ERROR" 12
