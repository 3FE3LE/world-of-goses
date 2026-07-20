# Characters — source files

> Pixelorama sources for citizens, heroes, expedition members, and NPCs.

This folder will hold the **editable** Pixelorama sources (`.pxo` or `.pxm`) for every character shown in the city. PNGs in `art/exports/characters/` are derived from these sources and must not be edited by hand.

## Placeholder slice

The previous `worker_placeholder.png` has been removed. The current slice renders no character art at the `VisibleWorkerSlot` until a real `worker.pxo` source lands; the slot gracefully renders without a sprite instead of crashing the loader. The three building PNGs (`home_idle.png`, `quarry_idle.png`, `farm_idle.png`) live in `art/exports/buildings/` and have their own notes there.

## Required sources for the next iteration

| Subject | File name    | Canvas  | Frames (current)                                       |
| ------- | ------------ | ------- | ------------------------------------------------------ |
| Worker  | `worker.pxo` | 64 × 96 | `idle`, `walk`, `mine`, `carry`, `hurt` (4 each)       |

When the worker is exported:

- Frame naming follows `art/exports/characters/worker_<state>_<frame>.png`.
- Sprite-sheet export uses `worker_<state>_sheet.png` with one row per frame at the canvas size.
- The default orientation is **side-facing**, looking to the right.
- The first frame of every state must be a complete silhouette so it can drop into the existing `VisibleWorkerSlot` canvas (defined in `PresentationConstants.DetailedCitizenWidth` / `Height`) without re-anchoring.

## Replacement workflow

1. Open `worker.pxo` in Pixelorama.
2. Edit frames.
3. **File → Export As…** PNG (or sprite-sheet PNG) into `art/exports/characters/`.
4. Re-import into `game/assets/characters/` through the Godot editor (or update `.import` metadata if you hand-edit it).
5. The scene that hosts `VisibleWorkerSlot` sets `WorkerSpritePath` via the inspector to point at the new sprite sheet. `SpriteFrames_Worker_<State>` resources are referenced by name — no scene re-wiring should be needed when the sprite sheet lands.