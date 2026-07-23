# Asset inventory and integration plan

Inventory date: 2026-07-23.

This document records what currently exists under `art/`, what may enter
`game/assets/`, and what remains reference material or tooling. Files under
`art/` are not imported into Godot merely because they are present.

## Inventory

### Existing source and exported game art

| Package/path | Contents | Current use |
| --- | ---: | --- |
| `exports/` | 3 PNG, 31,657 bytes | Building placeholders already promoted through the art pipeline. |
| `source/` | 2 documentation/source files | Current authored-source placeholders. |
| `Geist_Pixel/` | 3 files, 3,663,243 bytes | Screen and display typography; already integrated. |
| `Jersey_10/` | 2 files, 81,124 bytes | Headings and buttons; already integrated. |
| `Pixelify_Sans/` | 7 files, 289,469 bytes | Body and tooltip typography; already integrated. |
| `Kenney/` | 38 files, 36 images | Existing CC0 pixel UI kit; already used by the global theme. |
| `Pixelarticons/` | 879 files, 877 SVG/images | MIT 24×24 icon catalog; selected icons are promoted individually. |

### Newly downloaded archives

| Archive | Useful contents | License | Decision |
| --- | ---: | --- | --- |
| `kenney_cursor-pixel-pack.zip` | 223 PNG | CC0 | Candidate for a later cursor-state pass; do not bulk import. |
| `kenney_input-prompts-pixel.zip` | 819 PNG | CC0 | High-value for keyboard/gamepad prompts and future rebinding UI. |
| `kenney_minimap-pack.zip` | 164 PNG | CC0 | Defer until map/territory navigation exists. |
| `kenney_roguelike-rpg-pack.zip` | 5 PNG spritesheets/maps plus 2 TMX maps | CC0 | Best current candidate for an orthogonal top-down city-direction spike. Contains terrain, trees, props, roofs, walls, water, paths, and small buildings. |
| `kenney_ui-pack-pixel-adventure.zip` | 514 PNG | CC0 | Candidate component library; compare against the current Kenney theme before replacing anything. |
| `world-of-goses-lpc-lineages-godot4.zip` | 196 PNG, 16 scenes, 16 SpriteFrames resources | LPC package attribution applies | Superseded by the reproducible v2 package and current integrated character set. Keep as archive, do not import twice. |
| `world-of-goses-lpc-lineages-reproducible-v2.zip` | 196 PNG plus recipes, scenes, resources, checksums, and attribution | LPC package attribution applies | Source/rebuild archive for the character set already integrated into `game/assets/characters/lineages/`. |

### Tooling and conceptual generation

| Path | Contents | Decision |
| --- | ---: | --- |
| `world-of-goses-lpc-lineages-reproducible-v2/` | 1,385 files; 220 images; generator source and an embedded ignored `.venv` | Retain as reproducible source package. Never import its `.venv`, executables, or Python cache into Godot. |
| `world-of-goses-minimax-splash-generator/` | 1,310 files; generator, dependencies, and embedded ignored `.venv` | Tooling only. Generated splash images remain conceptual until reviewed and intentionally promoted. Never store or read an API key from the repository. |

The three `.pem` files found by extension are the public CA bundles bundled by
Python `certifi` inside ignored virtual environments. They are not game assets
and are not read or promoted.

## Assets promoted in this slice

Three MIT Pixelarticons were copied verbatim into
`game/assets/ui/icons/24/`:

- `menu.svg` — ESC menu heading.
- `reload.svg` — destructive reset confirmation.
- `trash.svg` — start-over action.

The CC0 Kenney Roguelike RPG transparent atlas and its license were selectively
promoted into `game/assets/terrain/kenney/roguelike-rpg/`. The atlas currently
supplies provisional ground tiles and scattered trees to the macro parcel scene.
The CC0 `tile_0111.png` tool/axe cursor and its license were selectively promoted
from `kenney_cursor-pixel-pack.zip` into `game/assets/ui/cursors/kenney-pixel/`
for the tree-resource hover state.
No complete ZIP was extracted into `game/assets/`.

## Integration plan

### Phase 1 — UI and input

1. Land the ESC menu with Resume, a reserved Settings entry, and confirmed reset.
2. Use `kenney_input-prompts-pixel` only when the first real keyboard/gamepad
   prompt or rebinding screen exists.
3. Compare `kenney_ui-pack-pixel-adventure` against the existing theme in a
   component showcase; promote only components that fill a real missing state.
4. Evaluate cursor states separately; do not replace the current cursor wholesale.

### Phase 2 — natural-resource and parcel proof

1. Replace `BuildingKind.Forest` with a parcel-owned natural-resource patch.
2. Use selected trees from `kenney_roguelike-rpg-pack` as provisional visual
   units: one visible tree can represent 40 wood.
3. Remove trees at reserve thresholds and remove the selectable patch at zero.
4. Add surface-stone patches using the same domain/presentation contract.
5. Keep these assets provisional and culture-neutral; they validate interaction,
   depletion, regeneration, and parcel layout rather than final art direction.

### Phase 3 — committed orthogonal macro view

The macro view uses an elevated orthogonal grid. Pseudo-isometric presentation
is no longer an active alternative. Continue the orthogonal direction through:

1. parcel ownership and locked/unlocked states;
2. cardinal paths and readable tile occupancy;
3. natural-resource patches whose visible units follow their reserves;
4. buildings aligned to parcel footprints;
5. citizen routes that remain legible above ground but below UI.

The current `OrthogonalParcelTerrain` now projects Forest reserves into
interactive tree units. Hover uses a resource cursor; left or right click opens
a contextual action menu; choosing Gather moves the macro hero representation
to the tree and invokes the existing domain gather operation on arrival. Parcel
state still needs a domain model before unlocking, placement, or regeneration
can become persistent gameplay.

## Promotion checklist

Before any downloaded asset enters `game/assets/`:

1. Confirm license and attribution requirements.
2. Identify the concrete scene or UI state that needs it.
3. Copy only the selected source asset, not an entire pack.
4. Record provenance and replacement status.
5. Verify nearest filtering, integer scale, canvas size, and visual hierarchy.
6. Add it to the applicable visual-regression state.
