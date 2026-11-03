# Cat Mail Expanded

A MelonLoader mod for **Cat Mail Co.** that adds two new parcel types, an outdoor yard and a speaker room.

## What it adds

### Scented parcels (Strawberry & Banana)
- Marked with a hand-painted strawberry or banana sticker (in the slot native constraint stamps use) and their own glyph on the scanner screen.
- Indoors, they slowly build up a thick pink (strawberry) or yellow (banana) fog cloud that makes it hard to see around them. Clouds dim at night.
- Store them **outside in the yard** (or load them on the boat) and the fog clears within a few seconds.
- Start arriving once the post office reaches **level 10** (configurable). The back door swings open at that moment.

### Restless parcels
- Marked with a "dancing box" sticker and their own scanner glyph.
- Anywhere without music they wiggle, then hop off shelves and knock nearby shelved parcels loose.
- Calm while in the **speaker room** with the music playing (and on the boat, customer counters and workstations).
- Only appear **after the game's ending** (the Old Postman scene and credits).

### Outdoor yard
- Built behind the storage corridor's service door (the one that never opened): a wooden pier deck over the water with railings, a striped awning, string lights, planters (strawberries!), a banana plant, a lamp post, a "Fresh Air Storage" sign and three storage shelves.
- The door stays shut until the yard unlocks, then swings open.

### Speaker room (the break room, after the ending)
- Two wooden speakers play the game's own music tracks (the regular background music ducks while you're inside) and gently bounce to the beat. A "Music Room" poster goes up.
- The empty tuna cans become carryable: throw them in the new trash can by the door. Each bookshelf/pantry compartment becomes parcel storage once its cans are gone ("Shelf cleared!").

### Customers
- Customers requesting scented/restless parcels describe them with new lines ("My package smells like strawberries…", "I ordered a box of jumping beans. Please tell me you played them some music."), including mood variants. All 13 game languages are included.

### Unlocks
- Both unlocks show the game's own notification and, when earned during play, the end-of-shift "unlocked" card.

## Installing
1. Install MelonLoader 0.7.x for the game (already installed here).
2. Copy `CatMailExpanded.dll` into the game's `Mods` folder.

## Settings (`UserData/MelonPreferences.cfg`, section `[CatMailExpanded]`)
| Setting | Default | Meaning |
|---|---|---|
| `YardUnlockLevel` | 10 | Level at which the yard opens and scented parcels start arriving (game max 15). |
| `RestlessMinInterval` / `RestlessMaxInterval` | 7 / 14 | Seconds between hops for an unhappy restless parcel. |
| `ForceUnlockAll` | false | Testing aid: unlock everything immediately. |
| `PrepareUninstall` | false | Turns all mod parcels back into normal ones and moves parcels off mod-only shelves on load. Save once, then remove the mod. |
| `DevMode` | false | Developer command file (`UserData/CME/cmd.txt`) used for automated testing. |

## Notes and limitations
- New parcel types are stored as new values of the game's storage-constraint field, so they save, load and sync like native types. **Remove the mod only after a save made with `PrepareUninstall = true`**; otherwise the vanilla game can't describe those parcels.
- Multiplayer: every player needs the mod. Restless hopping physics run on the host.
- The new types are excluded from the game's recap damage check; their consequences are the fog and hopping instead.

## Project layout
| Path | Contents |
|---|---|
| `src/CatMailExpanded` | Mod source (net6, Harmony patches). `Content/` holds gameplay, `Patches/` the hooks, `Dev/` the test command system. |
| `Art/Blender` | Blender scripts that model, bake (painterly colour + AO) and export every prop (`cme_export.py` holds the shared helpers and the `.cmemesh` exporter). |
| `Art/Textures`, `Art/Reference`, `Art/Previews` | Source images from Wando, style references cropped from the game, preview renders. |
| `Assets/Build` | Final meshes/textures, embedded into the DLL at build time. |
| `Tools` | `game.sh` (launch/drive the game for tests), sticker/FX/atlas generators, Cpp2IL, AssetRipper. |
| `Decompiled` | Reference copy of the game: interop stubs, clean C# signatures, ISIL method bodies, an AssetRipper Unity project, and research notes in `Decompiled/Reference`. |
| `Backups` | Copies of the original save files taken before testing. |

## Placement tools (DevMode)
Props are placed by the mod at runtime and automatically settled: `Core/Placement.cs` snaps them to the floor and
pushes them out of any wall/furniture collider they overlap (using their whole visual volume). For hand-tuning,
with `DevMode = true` and `Tools/game.sh`:
| Command | What it does |
|---|---|
| `objs` | Lists every mod object with position and yaw. |
| `move <name> x y z [yaw] [scale]` | Moves a mod object live and reports overlaps. |
| `clip <name>` | Reports what the object intersects (walls, beams, furniture). |
| `settle <name>` | Runs the automatic floor-snap + overlap resolution. |
| `copyvis <scene object> x y z` | Visual-only copy of any game prop, for inspection or reuse. |
| `Tools/orbit.sh <name> x y z [dist] [pitch] [fov]` | Parks the player and saves a 4-angle contact sheet to `Art/Previews/orbit_<name>.png`. |

## Building
```
cd src/CatMailExpanded
dotnet build -c Release
```
The build copies the DLL into the game's `Mods` folder (`-p:Deploy=false` to skip). Rebuild models with e.g.
`blender -b --factory-startup --python Art/Blender/yard.py -- all`.
