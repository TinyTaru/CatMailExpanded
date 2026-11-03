#!/bin/bash
# Launches the macOS build of Cat Mail Co. with MelonLoader (and the mods in ./Mods).
# Lives in the game folder next to the .app, MelonLoader.Bootstrap.dylib and MelonPltFix.dylib.
#
# MelonLoader's macOS build is x64-only, so the game runs under Rosetta. The libraries are inserted by
# bare name (resolved from this folder) because MelonLoader only removes the bare
# "MelonLoader.Bootstrap.dylib" entry from DYLD_INSERT_LIBRARIES before launching child processes.
# The fallback path lets MelonLoader dlopen("GameAssembly.dylib") by bare name.
set -e
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$DIR"

shopt -s nullglob
APPS=("$DIR"/*.app)
shopt -u nullglob
if [ ${#APPS[@]} -ne 1 ]; then
    echo "Expected exactly one .app in $DIR, found ${#APPS[@]}" >&2
    exit 1
fi
APP="${APPS[0]}"
BINARY="$APP/Contents/MacOS/$(/usr/libexec/PlistBuddy -c "Print CFBundleExecutable" "$APP/Contents/Info.plist")"

exec arch -x86_64 \
    -e DYLD_INSERT_LIBRARIES="MelonLoader.Bootstrap.dylib:MelonPltFix.dylib" \
    -e DYLD_LIBRARY_PATH="$DIR" \
    -e DYLD_FALLBACK_LIBRARY_PATH="$APP/Contents/Frameworks:/usr/local/lib:/usr/lib" \
    "$BINARY" "$@"
