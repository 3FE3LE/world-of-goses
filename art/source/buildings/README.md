# Buildings — source files

> Pixelorama sources for production buildings, residences, monuments, and decorations.

This folder will hold the **editable** Pixelorama sources (`.pxo` or `.pxm`) for every building shown in the city. PNGs in `art/exports/buildings/` are derived from these sources and must not be edited by hand.

## Placeholder slice

The current slice ships three placeholder PNGs supplied directly by the project owner to give the prototype its first visual identity. They live only in `art/exports/buildings/` (and their Godot import twins in `game/assets/buildings/`); they have **no editable `.pxo` source yet**.

| Subject | Source status | PNG in `art/exports/buildings/` | Canvas    | Maps to `BuildingKind`           |
| ------- | ------------- | ------------------------------- | --------- | ------------------------------- |
| Home    | Missing       | `home_idle.png`                 | 64 × 64   | `Home` (Basic Shelter)          |
| Quarry  | Missing       | `quarry_idle.png`               | 128 × 128 | `Quarry`                        |
| Farm    | Missing       | `farm_idle.png`                 | 128 × 128 | `Farm`                          |

The Home canvas (64 × 64) is intentionally smaller than the production buildings (128 × 128) because the macro view renders the founding dwelling at the macro citizen footprint, not at the full plot footprint. The two production buildings use the same canvas as `PresentationConstants.MacroPlotSize` (192 px in the original prototype, 128 px in this placeholder set) so they can replace the placeholders without re-anchoring scenes.

## Required sources for the next iteration

| Subject | File name    | Canvas       | Frames (current)                                                   |
| ------- | ------------ | ------------ | ------------------------------------------------------------------ |
| Home    | `home.pxo`   | 64 × 64      | `idle` (1)                                                         |
| Quarry  | `quarry.pxo` | 128 × 128    | `idle` (1), `active` (1) — planned                                 |
| Farm    | `farm.pxo`   | 128 × 128    | `idle` (1), `active` (1) — planned                                 |

When each subject is exported:

- Frame naming follows `art/exports/buildings/<subject>_<state>_<frame>.png` (or `<subject>_<state>_sheet.png` for sprite sheets). The current placeholders are the `<subject>_idle_<frame>.png` case where `<frame>` is omitted because there is only one frame.
- The exported PNG **replaces** the placeholder of the same name in `art/exports/buildings/`. No C# code needs to change when the file is swapped.
- The default orientation is **front-facing / three-quarter top-down** so the silhouette reads from the macro city view.
- The first frame of every state must be a complete silhouette so it can replace the placeholder without re-anchoring.

## Replacement workflow

1. Open `<subject>.pxo` in Pixelorama.
2. Edit frames at the canvas size listed above.
3. **File → Export As…** PNG (or sprite-sheet PNG) into `art/exports/buildings/`, keeping the same filename.
4. Re-import into `game/assets/buildings/` through the Godot editor (or update `.import` metadata if you hand-edit it).
5. The C# `BuildingArt` catalog references each PNG by name — no scene re-wiring should be needed when a placeholder PNG is replaced.

## Untouched `BuildingKind` values

`Smithy` and `PotionLab` have no art at any level yet. Their PNGs will be added to this folder and to `game/assets/buildings/` when a slice introduces them visually. Until then, code paths that try to render either kind must fall back to a generic tile rather than crashing.