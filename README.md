# Cat Mail Expanded

A MelonLoader mod for **Cat Mail Co.** that adds new parcel types, an outdoor yard, a planetarium, a speaker room, and a longer mail pile corridor with a mirror room and a gift wrapping room.

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

### Otherworldly parcels and the planetarium
- Marked with a "ringed-planet parcel" sticker and their own scanner glyph. They start arriving at post office **level 12** (configurable).
- They must be stored in the **planetarium**, or they get damaged exactly the way cold parcels do outside the cold room (the game's own damage check decides when).
- The planetarium is a domed wing off the pile corridor, across from the break room (the "winner" poster moved along the wall to make room). Its door is shut until it unlocks, then disappears like the game's other unlockable rooms.
- Inside: a painted nebula sky turning slowly overhead, an orrery of planets circling a little sun, black-light tubes, a telescope, an armillary sphere, a star rug, starry shelves and a floor pallet, each with a **working storage label**.
- It is lit like a black-light room: every parcel inside goes dark, and otherworldly parcels' white parts (tape, edges, name labels) and stickers fluoresce.

### The longer pile corridor
- The mail pile no longer turns bottomless at the break room. The corridor now carries on 11 m past its old end wall, and the pile keeps receding through **five more stages** (copies of the game's own stages, 90 parcels each) before the bottomless pile takes over at the new end.
- The new stretch is built from slices of the game's own corridor meshes (baked lighting included, `Content/PileWing.cs`), so it reads as the same hallway. The old end frame, panel and end wall move to the new end.
- Two new rooms open off it, with staggered doors: the **gift wrapping room** on the right (its door clears at the fifth new stage) and the **mirror room** further down on the left (its door clears when the pile turns bottomless).
- Saves that had already reached the bottomless pile carry on from the first new stage.

### Spectral parcels and the mirror room
- Marked with a ghost-parcel sticker and their own scanner glyph. They start arriving (pile, boat, customer drop-offs) once the mirror room opens.
- Anywhere but the mirror room they flicker and fade from sight: only their stamps, stickers and name label stay visible (aiming at one still shows its outline). No damage, just a harder find.
- The mirror room is plum and gold, with a chandelier, velvet curtains, a vanity, wooden shelves with working labels and a floor pallet. Its two gilded mirrors are real mirrors (each drawn by a camera at the viewer's reflection, only while visible), and, like a vampire, a spectral parcel never shows in them (ordinary parcels reflect as usual).

### Gift wrapping
- Once the gift wrapping room opens, 2–4 extra **gift customers** come to the counter each shift, on top of the shift's normal customers (they never count towards it). Each hands over a present and asks for its wrapping in a riddle ("Wrap it in whatever a cat dreams about at night. Tie it with a ribbon the colour of forget-me-nots."). The same riddle is written on the **gift tag** on top of the parcel.
- Eight papers (starry night, candy stripes, pink polka dots, flowers, fish, tartan, paw prints, gold foil, painted locally with FLUX.2 [klein], `Tools/gen_gift_papers.sh`) hang on the pegboard above the wrapping bench, and six ribbon spools (the game's red, yellow and blue plus green, purple and pink) on the dowel above them.
- Put the gift on the bench (or just hold it) and **click a paper roll**, then **a ribbon spool**. Re-click to change your mind. The ribbon is the game's own parcel ribbon; stamps, stickers and the name label stay on top of the paper.
- Shelve the gift. When the person it's for comes for it, they describe the wrapping they were promised, and react when you hand it over. Right paper and ribbon score the most, one of the two scores less, and an unwrapped gift costs points. The result is a **"Gifts delivered"** line on the shift summary and counts towards the post office's level.

### Customers
- Customers requesting scented/restless/otherworldly/spectral parcels describe them with new lines ("My package smells like strawberries…", "I ordered a box of jumping beans. Please tell me you played them some music."), including mood variants.
- Customers dropping off a parcel of a mod type usually mention what it is ("It smells of strawberries, so maybe keep it outside").
- 28 riddle-style descriptions join the game's own pools for native features ("My parcel is shy. It hides from the light, like a cat under the bed."), so finding parcels sometimes takes a little decoding.
- All 13 game languages are included.

### Outdoor yard
- Built behind the storage corridor's service door (the one that never opened): a wooden pier deck over the water with railings, a striped awning, string lights, planters (strawberries!), a banana plant, a lamp post, a "Fresh Air Storage" sign and three storage shelves.
- The door stays shut until the yard unlocks, then swings open.
- At the deck's south-east corner a closed garden gate looks out over a footbridge and a lamp-lit flagstone path to the front of the building (scenery only: the gate never opens).

### Speaker room (the break room, after the ending)
- Two wooden speakers play the game's own music tracks (the regular background music ducks while you're inside) and gently bounce to the beat. A "Music Room" poster goes up.
- The empty tuna cans become carryable: throw them in the new trash can by the door. Each bookshelf/pantry compartment becomes parcel storage once its cans are gone ("Shelf cleared!").

### Unlocks
- Every unlock shows the game's own notification and, when earned during play, the end-of-shift "unlocked" card.

## Installing
1. Install MelonLoader 0.7.x for the game (already installed here).
2. Copy `CatMailExpanded.dll` into the game's `Mods` folder.

### macOS (Apple Silicon, `CatMailCo LAN.app`)
MelonLoader 0.7.3's macOS build is x64-only and needs some workarounds, all in `Tools/macos`:
- The game lives in its own folder, `/Applications/CatMailCo LAN/CatMailCo LAN.app`. MelonLoader puts its files next to the `.app`, and its installer rejects a folder holding more than one `.app`, so it can't sit directly in `/Applications`.
- Next to the `.app`: the contents of `MelonLoader.macOS.x64.zip`, an x64 .NET 6 runtime in `dotnet-x64/` (set as `hostfxr_path_override` in `UserData/Loader.cfg`), `MelonPltFix.dylib` (built from `MelonPltFix.c`, see its header) and `Launch CatMailCo (Mods).command`.
- Start the modded game with `Launch CatMailCo (Mods).command`; it runs the game under Rosetta with MelonLoader injected. Opening the `.app` directly starts the unmodded game.
- `MelonPltFix.dylib` fixes three MelonLoader bugs on this build: its `dlsym` hook misses UnityPlayer (black screen forever), the managed module list treats the first injected library as the game, and GameAssembly's module size overshoots into unmapped memory (crash in Il2CppInterop's signature scan).
- Cpp2IL can't read the universal `GameAssembly.dylib` ("Stream does not support writing"). After a game update, regenerate the assemblies against an x86_64-only copy: `lipo -thin x86_64` the original to a new file and `mv` it into place, launch once, then put the universal file back the same way (a new file plus `mv`; overwriting the signed file in place makes Unity fail with "Failed to load il2cpp"). Finally set `GameAssemblyHash` in `MelonLoader/Dependencies/Il2CppAssemblyGenerator/Config.cfg` to its uppercase SHA-512.
- MelonLoader's own `NativeHook` API and ICall injector fail on macOS (ClrMD snapshot unsupported). Harmony patches on game methods are unaffected.

## Settings (`UserData/MelonPreferences.cfg`, section `[CatMailExpanded]`)
| Setting | Default | Meaning |
|---|---|---|
| `YardUnlockLevel` | 10 | Level at which the yard opens and scented parcels start arriving (game max 15). |
| `PlanetariumUnlockLevel` | 12 | Level at which the planetarium opens and otherworldly parcels start arriving. |
| `RestlessMinInterval` / `RestlessMaxInterval` | 7 / 14 | Seconds between hops for an unhappy restless parcel. |
| `ForceUnlockAll` | false | Testing aid: unlock everything immediately. |
| `PrepareUninstall` | false | Turns all mod parcels back into normal ones and moves parcels off mod-only shelves on load. Save once, then remove the mod. |
| `DevMode` | false | Developer command file (`UserData/CME/cmd.txt`) used for automated testing. |

## Notes and limitations
- New parcel types are stored as new values of the game's storage-constraint field, so they save, load and sync like native types. **Remove the mod only after a save made with `PrepareUninstall = true`**; otherwise the vanilla game can't describe those parcels.
- Multiplayer: every player needs the mod. Restless hopping physics run on the host. Gift customers, wrapping and gift scoring also run on the host (untested with clients).
- Every new type arrives through the game's own property roll: pile parcels, boat cargo and customer drop-offs all roll their storage constraint from the unlocked list, so mod types show up in all three once unlocked.
- Scented and restless parcels are excluded from the game's recap damage check (their consequences are the fog and hopping). Otherworldly parcels use it: outside the planetarium the check sees a native constraint no room around them protects.
- The doorway into the planetarium is cut out of the corridor's wall mesh at runtime (`Core/MeshCutter.cs` reads the GPU-only mesh back and clips the triangles).
- The wrapping papers and the spectral sticker were painted with FLUX.2 [klein] 4B (Apache 2.0) through mflux (`Tools/gen_gift_papers.sh`, `Tools/gen_spectral_sticker.sh`); `Tools/make_expansion_textures.py` makes the papers tile and draws the stickers, tag and signs. The gift room rug was painted with Qwen-Image-Edit 2511 and its 4-step Lightning LoRA from a flat layout (`Tools/gen_gift_rug.sh`).
- Saves from earlier builds with Catazon return items load fine: those items become ordinary parcels.
- The planetarium's sky and sticker art were painted with Bria Fibo 2.0 (local, `Tools/gen_planetarium_art.sh`); its open weights are CC BY-NC 4.0, so check Bria's licence before any commercial use of those two images.

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
`blender -b --factory-startup --python Art/Blender/yard.py -- all` (also `planetarium.py`, `front.py`, `mirror.py`, `gifts.py`, `roof.py`), and the planetarium textures with `python3 -I Tools/make_planetarium_textures.py`.
On macOS, `Tools/macos/game.sh` drives the game like `Tools/game.sh` does on Windows, and `Tools/macos/load_test_save.sh` restarts it into the first save slot.
