# Art Pipeline

> The end-to-end flow from a Pixelorama source file to an animated
> scene in Godot. This document defines the directory layout, the
> naming conventions, and the responsibilities of each tool in the
> pipeline.

This file owns the **mechanics** of the pipeline: where files live,
how they are named, what each tool does. The visual direction,
typography hierarchy, three visual scales, Sixteen Pixel Perfect
configuration, and per-lineage visual identity live in
[`visual-language.md`](visual-language.md).
The typography sizes and Theme variations live in
[`art/world-of-goses-typography-guideline.md`](../../art/world-of-goses-typography-guideline.md).
The iconography rules (Kenney UI vs Pixelarticons vs project-owned icons)
live in
[`art/world-of-goses-iconography-guideline.md`](../../art/world-of-goses-iconography-guideline.md).

When this file and the bible disagree on a file path, naming pattern,
or import setting, this file wins. When they disagree on the look,
the typography, or what counts as identity vs chrome, the bible wins.

---

## 1. The pipeline

```
Pixelorama
  → editable source files  (art/source/<category>/)
  → export to PNG / sprite sheets
                          (art/exports/<category>/)
  → import into Godot     (game/assets/<category>/)
  → configure SpriteFrames, TileSets, scenes
  → C# logic selects states and animations
```

The conceptual rule:

> **Pixelorama defines how it looks.**
> **Godot defines how it is represented and animated.**
> **C# defines what is happening and why.**

## 2. Categories

| Category     | What lives here                                       | Backing |
| ------------ | ----------------------------------------------------- | --- |
| characters   | Citizens, heroes, expedition members, NPCs            | `Citizen`, LPC set |
| buildings    | Houses, production buildings, monuments, decorations  | `Building` |
| terrain      | Ground tilesets, vegetation, water, loose ground props | `TerrainAtlas` |
| environments | Side-view backdrops and parallax layers for the expedition stage | `ExpeditionStage` |
| creatures    | Bestiary adversaries and non-citizen combatants       | `CombatantView`; no domain type yet |
| items        | Equipment, materials, and the icons that stand for them | `PersonalEquipment` |
| emblems      | Lineage symbology: emblems, banners, flags, sigils    | `art/source/emblems/`, 8 lineages |
| effects      | Particles, fire, smoke, dust, weather, magic-like FX  | — |
| audio        | Music, ambient sounds, SFX, voice (when added)        | — |
| ui           | Icons, frames, buttons, panels, fonts, cursors        | `default_theme.tres` |

The **Backing** column names what in the code or the repository the category
serves today. A category with a dash is provisioned, not yet populated;
creating its folder is expected, not a surprise. A category **not on this list
does not exist** — `art/source/tilesets/` and a bare `art/source/emblems/`
predate the list and belong under `terrain/` and `emblems/` respectively.

Two boundaries that are easy to get wrong:

- **terrain vs environments.** Terrain is the macro city floor and what stands
  on it, drawn in perspective. Environments are the expedition's side-view
  backdrop, drawn as flat parallax layers. Different projection, different
  authoring, different category.
- **characters vs creatures.** A character is a person the simulation tracks —
  a `Citizen`, the hero, an NPC. A creature is an adversary with no personal
  state. If it can be assigned to a building, it is a character.

Lineage emblems get their own category rather than living in `ui/` because the
same symbol is wanted in more than one place — a hover card, a banner over a
parcel, a founder's sigil — and because there are eight lineages times however
many symbol types the world ends up with. Bundling that into `ui/icons/` would
mix a growing identity set into the chrome catalogue.

A category exists in three places:

- `art/source/<category>/`   — Pixelorama sources.
- `art/exports/<category>/`  — PNG / sprite sheets exported from Pixelorama.
- `game/assets/<category>/`   — Imported assets used by Godot.

The categories under `game/assets/` mirror the categories under
`art/source/`. A change to a category in one place is a change in all
three.

## 3. Source files

- **Format:** Pixelorama native (`.pxo` or `.pxm`).
- **One subject per file.** A character has a source file. A building
  has a source file. An effect has a source file.
- **Sprite sheets are exported, not authored.** If a sprite sheet
  exists, it lives in `art/exports/`, not in `art/source/`.
- **Reference material goes in `art/references/`**, not in `art/source/`.
  References are mood boards, inspiration, color scripts, and notes —
  not game art. They are not imported into Godot.

## 3.1 Escalera de tamaños

Everything is authored against the 32-unit grid defined in
[`visual-language.md`](visual-language.md). A source canvas is always a whole
number of tiles; there is no "roughly this big".

| Subject | Source canvas | Why |
| --- | ---: | --- |
| Ground tile | 32 × 32 | one tile |
| Loose ground prop (branch, stone, bush) | 32 × 32 | occupies one tile |
| Tree, two tiles tall | 32 × 64 | authored as **one** canvas, not canopy and trunk apart |
| Citizen — macro, building scene and expedition alike | 64 × 64 | **one canvas for every scale.** The macro needs four directions, the expedition one mirrored side view; the size never changes, so the lateral clips serve both |
| Bestiary creature | 64 × 64 or a whole multiple of it | shares the citizen's canvas so the shared reaction and effect clips register without adaptation |
| Standard-lot building | 96 × 96 for one storey; taller only when the subject is taller | width comes from the footprint (3 × 3 tiles, `LotUnitPx`). One storey checks out at 96: a ~56 px citizen against a ~96 px sprite is the real 55–65 % of a single storey seen from outside. **Height is a separate axis** — raise it, in whole tiles, for more storeys or when the near-row roof plane needs the room |
| Double-frontage building | 192 wide, same height rule | footprint 6 × 3 tiles |
| Lineage emblem | 32 × 32 and 64 × 64 | inline hover glyph and card/banner |
| UI icon | 32 × 32 | one tile, so an icon can sit in a world cell unchanged |

**No gutter, no margin, no separation.** A gutter exists to stop texture
bleeding, which needs linear filtering or mipmaps; this project renders with
`textures/canvas_textures/default_texture_filter = 0` and mipmaps off, so
nothing can bleed. A transparent gutter is worse than none: it does not stop
bleeding — it bleeds alpha instead of the neighbour — and it makes a multi-tile
subject impossible to draw as a single region, which is exactly what forced the
Kenney placeholder trees to be drawn as two separate calls. If a future change
ever needs a gutter, it must be **extruded** (edge pixels duplicated outward),
never transparent.

### One file per subject, states as frames

§3 says one subject per file and §6.2 names sources `<subject>_<state>.pxo`.
They agree for animation, where a state is a distinct clip
(`villager_walk_down.pxo`). For a subject whose states are a **progression** —
a building through its construction phases — use one file per subject and one
frame per phase (`sawmill.pxo` with frames `site`, `frame`, `built`), exported
as a sheet. Splitting a construction sequence across files makes it impossible
to keep the phases aligned on the same canvas.

## 4. Exports

Exporting is `tools/Export-Art.ps1`, not a manual save out of Pixelorama. It
derives every path from the convention above, so a subject cannot be written to
the wrong place:

```powershell
pwsh ./tools/Export-Art.ps1 -Category terrain -Subject eirune_ground
pwsh ./tools/Export-Art.ps1 -Category terrain      # every subject in it
pwsh ./tools/Export-Art.ps1 -All -Check            # report, write nothing
```

It composes the sheet, promotes it into `game/assets/`, updates the subject's
profile, and prints which cells are usable as a fill.

### 4.1 Tile ids are not stable, and the lockfile is why

**Pixelorama renumbers tiles on its own.** Its manual states that in Auto mode
"tiles that are no longer used anywhere in the tilemap get erased from the
tileset" — so removing the last use of a tile from the canvas shifts every id
after it, with no deliberate deletion. A profile naming tiles by id would
silently start pointing at different art, and nothing in the file would say so.

So a tile is identified by the hash of its pixels. Each export writes
`art/exports/<category>/<subject>.tiles.json` mapping id to hash; the next one
diffs against it and classifies every cell as unchanged, repainted, **moved**,
new or gone. A move is Pixelorama having renumbered, and the exporter rewrites
the profile's role ids to follow it. This is verified by simulating a shift and
watching `Fill` follow the pixels rather than the positions.

**Never hand-edit the exported sheet or the lockfile.** Re-export.

### 4.2 The exporter owns geometry; the artist owns roles

`TileSize`, `Separation` and `Columns` are written into the profile by the
exporter, because they are facts about the sheet it just composed and having
two sources for them is how they drift apart. Nobody edits those three by hand.

`Fill` and `Path` stay the artist's. The measurement decides which tiles
*can* be a fill — opaque, and matching their own opposite edge — and narrows
the candidates; which of them is the material and which the occasional patch is
an art decision that no pixel arithmetic can make.

### 4.3 Multi-tile subjects

A tileset holds one tile size, so a two-tile-tall tree is two tiles and nothing
in the file says they belong together. Do not pair them by id. Either give the
subject its own `.pxo`, or give it **its own tileset in the same file** with a
taller `tile_size` — Pixelorama supports several tilesets per project and
`-TilesetIndex` selects one. Then "one region" is the tile size, not an
assumption about adjacency.

### 4.4 Adding a category

Only the reader varies: a tileset yields cells of one size, a building yields
construction phases as canvas frames, a character yields animation frames.
Grid composition, hashing, the lockfile, promotion and the report are shared.
The two places to extend are marked `CATEGORY DISPATCH` in the script. An
unsupported category fails by name rather than exporting something plausible
and wrong.

### 4.5 Conventions

- **Format:** PNG (preferred) or sprite-sheet PNG.
- **Output location:** `art/exports/<category>/`.
- **Naming:** `<subject>_<state>_<frame>.png` for individual frames,
  `<subject>_<state>_sheet.png` for sheets. Use lowercase with
  underscores.
- **Do not edit exported PNGs by hand.** Always re-export from the
  Pixelorama source.

## 5. Imports

- **Final location:** `game/assets/<category>/`.
- **Configuration:** Godot import settings (filter, mipmaps, frames
  per row) are configured in the Godot editor. The settings chosen
  are part of the asset's behavior and must be reproducible.
- **Generated cache:** `.import/` and `.godot/` are ignored. The PNG
  (and any intentional `.import` file) is the committed source of
  truth.
- **Pixel fonts:** project TTF fonts commit their `.import` profile with
  antialiasing and subpixel positioning disabled, viewport-driven oversampling
  (`0.0`), and the project-wide Nearest canvas filter. Validate the profile
  with `tools/Test-PixelFontImports.ps1`; use the typography specimen for
  output-resolution review rather than judging the editor preview.

### 5.1 Universal LPC lineage characters

The detailed lineage character set lives at
`game/assets/characters/lineages/<lineage>/<male|female>/`. It contains
16 Godot scenes backed by `SpriteFrames` resources and 128 × 128 cells
for 14 animations (`idle`, `combat_idle`, `walk`, `run`, `jump`, `climb`,
`sit`, `hurt`, `slash`, `thrust`, `halfslash`, `backslash`, `shoot`,
`spellcast`) in four directions. Runtime selection is
centralised in `game/scripts/visual/CharacterVisualRegistry.cs`; animation
selection is owned by `LineageSpritePlayer.cs`. The combat poses (`slash`,
`thrust`, `halfslash`, `backslash`, `shoot`, `spellcast`) come straight
from the Universal LPC body sheets; the body recoloring and overlay
pipeline applies the lineage identity on top.

These characters use Universal LPC body bases and require attribution.
Preserve `docs/presentation/licensing-and-attribution.md`, `docs/presentation/licenses/`,
`docs/world/lineages.md`, and `docs/presentation/MANIFEST.json` in distributions.

### 5.2 Lineage splash generator

`art/world-of-goses-minimax-splash-generator/` contains an optional MiniMax
generation workflow for lineage splash concepts. It reads the integrated LPC
idle sheets as visual references but is not part of the Godot runtime or the
Pixelorama-to-engine asset path.

In this repository the Godot project root passed to the tool is `game/`, so its
current output is `game/art/generated/standardized_lineage_characters/`. That
directory, the generator's `.venv`, temporary files, and Godot-generated
`*.import` files are local outputs and must not be committed. A generated image
only becomes a game asset after manual art review and intentional promotion
through the normal `art/source` / `art/exports` / `game/assets` pipeline.

The generator reads `MINIMAX_API_KEY` from the process environment. Never store
the real value in repository files, prompts, manifests, shell history, or logs.

## 6. Naming conventions

### 6.1 Subjects and states

- Subject names are lowercase, with underscores between words
  (`villager`, `town_hall`, `campfire`).
- State names are lowercase, with underscores between words
  (`idle`, `walk_down`, `chop`, `carry_wood`).
- A subject + state is the basis for filenames and resource names.

### 6.2 Filenames

- Pixelorama source: `<subject>_<state>.pxo`
  (e.g. `villager_walk_down.pxo`).
- Individual frame export: `<subject>_<state>_<frame>.png`
  (e.g. `villager_walk_down_0.png`).
- Sprite-sheet export: `<subject>_<state>_sheet.png`.
- Godot resource name: `SpriteFrames_<Subject>_<State>` for animation
  resources; `TileSet_<Subject>` for tilesets; `Texture_<Subject>_<State>`
  for standalone textures.

### 6.3 Godot scenes and scripts

- Scene file: `<subject>_<state>.tscn` (e.g. `villager.tscn`).
- C# script: `<Subject>.cs` matching the class name in PascalCase
  (e.g. `Villager.cs`).
- The class name and the filename must match.

## 7. Animation, TileMaps, and scenes

### 7.1 AnimatedSprite2D

- Frame-based animation is configured in a `SpriteFrames` resource.
- The resource is referenced from the scene, not duplicated.
- C# code drives the current animation by name, not by frame number.

### 7.2 AnimationPlayer

- Use `AnimationPlayer` for procedural animation, transitions, and
  properties that are not pixel-art frames.
- Do not use `AnimationPlayer` as a substitute for `AnimatedSprite2D`
  when the visual is a frame-based pixel animation.

### 7.3 TileMapLayer — and where it does not apply

- Tilesets are imported once and referenced from `TileMapLayer` nodes.
- Tileset configuration (physics layers, occlusion, navigation) is
  version-controlled as part of the `.tres` or `.tscn` files.

**The macro city floor is the exception, and it is not negotiable.**
`MacroStreetRenderer.DrawTiledFloor` draws every ground tile as a
`DrawPixelStaircaseTrapezoid`: near and far edges have different widths,
derived from `ProjectedRowScreenY(depth)` and `HorizontalScale(depth)`. It is a
perspective floor with pixel-stepped edges. `TileMapLayer` draws axis-aligned
rectangles on a regular square, isometric, half-offset or hexagonal grid and
cannot express that. Any flat surface — a building interior, a future map
screen — should use `TileMapLayer`; the macro cannot, and rewriting it to fit
would throw away the pseudo-3D direction the macro exists for.

What the macro uses instead is an **atlas plus named regions**. The rule that
survives from this section is the one that matters: *the tileset configuration
is versioned as data, not as code.*

That data is `GroundAtlasProfile`, one `.tres` per lineage under
`game/assets/terrain/biomes/`. Each declares its own sheet, tile size, gutter,
column count, fill ids and path id. **Changing a biome's ground is editing its
profile** — no C# is touched, and the seven biomes still on the Kenney
placeholder sheet are unaffected by one that moves to an authored one, even
though the two are cut on different grids.

The sheet is an exported `Texture2D`, not a path string, so Godot tracks the
dependency by uid and renaming or moving the PNG keeps working.

Trees and loose ground props are **not** in the profile yet: they still come
from the shared Kenney sheet through `TerrainAtlas`. They are a separate
authoring job on a separate sheet, and inventing their contract before the art
exists would be guessing.

Two properties of that contract are load-bearing, both learned the hard way:

- **Column count is part of the identity of an atlas.** Ids are linear
  (`id % columns`, `id / columns`), so changing the column count silently
  renumbers every tile in the sheet.
- **A fill tile must tile with itself.** Most coloured bands in a sheet are
  autotile sets whose tiles carry a corner or edge of the neighbouring
  material. Repeating one shows the cut. This is measurable, not a matter of
  eye: for the eleven fills in use the discontinuity between a tile's opposite
  edges is 0–0.25, and for the three ids that were withdrawn after rendering
  them it is 18–61.

### 7.4 Particles and lighting

- Particles use Godot's `CPUParticles2D` or `GPUParticles2D`. They
  belong to the visual layer.
- Lighting is configured on `Light2D` nodes and on the world
  environment. C# code does not configure lighting per-frame.

## 8. Audio

- Music and sound effects are imported into `game/assets/audio/`.
- Audio is referenced by name from C# code. No magic strings.
- A future document will define the audio naming convention in detail.
  For now: `<category>_<subject>_<state>.wav` or `.ogg`
  (e.g. `sfx_chop_wood.ogg`).

## 9. Version control

- `art/source/` is committed. Pixelorama sources are the source of
  truth.
- `art/exports/` is committed. The exports are the canonical handoff
  to the engine.
- `art/references/` is committed. References are documentation, not
  game art.
- `game/assets/` is committed. Imported PNGs and audio are committed.
  Generated cache is not.
- `game/.godot/` and `game/bin/` and `game/obj/` are not committed.

## 10. Anti-patterns

- **No hand-edited exported PNGs.** Always re-export from the source.
- **No reference material in `art/source/`.** It goes in
  `art/references/`.
- **No sprite sheets in `art/source/`.** They are exports, not
  sources.
- **No mixed categories in one folder.** A folder is for one
  category.
- **No magic strings in C#.** Asset paths, scene names, group names,
  and animation names are constants.
- **No artwork-dependent domain logic.** The simulation must not
  depend on a particular sprite being present.
