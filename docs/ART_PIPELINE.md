# Art Pipeline

> The end-to-end flow from a Pixelorama source file to an animated
> scene in Godot. This document defines the directory layout, the
> naming conventions, and the responsibilities of each tool in the
> pipeline.

This file owns the **mechanics** of the pipeline: where files live,
how they are named, what each tool does. The visual direction,
typography hierarchy, three visual scales, Sixteen Pixel Perfect
configuration, and per-lineage visual identity live in
[`docs/world-of-goses-design-bible/08_VISUAL_UI_AND_ASSET_GUIDELINES.md`](world-of-goses-design-bible/08_VISUAL_UI_AND_ASSET_GUIDELINES.md).
The typography sizes and Theme variations live in
[`art/world-of-goses-typography-guideline.md`](../art/world-of-goses-typography-guideline.md).
The iconography rules (Kenney UI vs Pixelarticons vs project-owned icons)
live in
[`art/world-of-goses-iconography-guideline.md`](../art/world-of-goses-iconography-guideline.md).

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

| Category     | What lives here                                       |
| ------------ | ----------------------------------------------------- |
| characters   | Citizens, heroes, expedition members, NPCs            |
| buildings    | Houses, production buildings, monuments, decorations  |
| terrain      | Tiles, ground, vegetation, water, climate features    |
| effects      | Particles, fire, smoke, dust, weather, magic-like FX  |
| audio        | Music, ambient sounds, SFX, voice (when added)        |
| ui           | Icons, frames, buttons, panels, fonts, cursors        |

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

## 4. Exports

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

### 5.1 Universal LPC lineage characters

The detailed lineage character set lives at
`game/assets/characters/lineages/<lineage>/<male|female>/`. It contains
16 Godot scenes backed by `SpriteFrames` resources and 128 × 128 cells
for `idle`, `walk`, and `slash` in four directions. Runtime selection is
centralised in `game/scripts/visual/CharacterVisualRegistry.cs`; animation
selection is owned by `LineageSpritePlayer.cs`.

These characters use Universal LPC body bases and require attribution.
Preserve `docs/LICENSING_AND_ATTRIBUTION.md`, `docs/licenses/`,
`docs/LINEAGE_DESIGN_MATRIX.md`, and `docs/MANIFEST.json` in distributions.

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

### 7.3 TileMapLayer

- Tilesets are imported once and referenced from `TileMapLayer` nodes.
- Tileset configuration (physics layers, occlusion, navigation) is
  version-controlled as part of the `.tres` or `.tscn` files.

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
