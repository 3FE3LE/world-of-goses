# Asset inventory

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
| `Jacquard_24/`, `Jacquard_12/` | 2 files each | Display titles and the HUD brand; integrated 2026-08-15. |
| `Jacquarda_Bastarda_9/` | 2 files | The founder's name only; integrated 2026-08-15. |
| `Jersey_15/`, `Jersey_10/` | 2 files each | Headings, buttons and reading text; already integrated. |
| `Micro_5/` | 2 files | Compact HUD rows, figures and badge counts; integrated 2026-08-15. |

Three font packages were **deleted on 2026-08-15**, not merely unwired:
`Geist_Pixel/` (3 files, 3,663,243 bytes), replaced by Jacquard;
`Pixelify_Sans/` (7 files, 289,469 bytes), replaced by Jersey 10 and Micro 5;
and `Zilla_Slab/` (10 files, 2,478,924 bytes), evaluated as a Pixelify
replacement and never adopted — it is a screen slab serif, incompatible with
the project's `antialiasing=0` grid contract. The two retired faces are in
git history; all three are OFL and re-downloadable from Google Fonts. Keeping
dead sources under `art/` only invites a future session to re-import them.
| `Kenney/` | 38 files, 36 images | Existing CC0 pixel UI kit; already used by the global theme. |
| `Pixelarticons/` | 879 files, 877 SVG/images | MIT 24×24 icon catalog; selected icons are promoted individually. |

Five of the six shipping faces come from one OFL collection, Soft Type by Sarah
Cadigan-Fried, which is also where Jersey 10 always came from. Two neighbours
were evaluated on 2026-08-15 and not imported:

- **Handjet** (Rosetta / David Březina, OFL) — the only pixel face on Google
  Fonts with a real weight axis, and rejected for it anyway: its native em is
  34 px, so at the project's 14-18 px reading sizes it renders under half a
  pixel per element and the glyphs collapse under `antialiasing=0`.
- **Yarndings 12 / 20** (same collection, OFL) — pixel dingbats that could
  complement the Pixelarticons in `Tokens.HudGlyphCell`. Deliberately out of
  scope of the typography pass; worth its own issue, not a drive-by import.

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

Two more followed on 2026-08-07, for a real missing state rather than a wish:

- `users.svg` — the citizen roster.
- `camera.svg` — the camera follow/free toggle.

The former navigation rail drew the hero, the roster and the camera toggle with the
same `user.svg`. That was survivable while every button carried a text label; it
was not survivable when the rail collapsed to icons, because three unrelated
actions became one glyph repeated three times. The icon-only `PrimaryNavDock`
keeps those distinct glyphs and adds `backpack.svg` for Expeditions plus
`clipboard-note.svg` for Policies. These are generic navigation, which
`art/world-of-goses-iconography-guideline.md` assigns to Pixelarticons rather than
to Kenney or to project-owned iconography, so promoting them is the tier model
working as intended. `game/assets/ui/icons/24/` now holds 33 SVGs.
All promoted SVGs use a white source fill so Godot's HUD icon-color overrides
can tint them; `currentColor` imports as black and cannot be brightened by a
multiplicative theme tint.

The CC0 Kenney Roguelike RPG transparent atlas and its license were selectively
promoted into `game/assets/terrain/kenney/roguelike-rpg/`. The atlas currently
supplies provisional ground tiles and scattered trees to the macro parcel scene.
The CC0 `tile_0111.png` tool/axe cursor and its license were selectively promoted
from `kenney_cursor-pixel-pack.zip` into `game/assets/ui/cursors/kenney-pixel/`
for the tree-resource hover state.
On 2026-08-06 four CC0 tiles were selectively promoted from
`kenney_ui-pack-pixel-adventure` into
`game/assets/ui/kenney-pixel-adventure/9-slice/`, with the pack's `LICENSE.txt`
alongside them: `Tiles/Large tiles/Thick outline/tile_0002` → `slate_raised`,
`tile_0003` → `slate_raised_dark`, `tile_0015` → `slate_inset`, `tile_0016` →
`slate_inset_dark` (all 32×32, `texture_margin = 8`). They fill the real
missing state this file asked to identify before promoting anything: the theme
had **no non-yellow surface**, so `ButtonText` and the lineage fallback both
resolved to `kenney/9-slice/yellow.tres`. Hover and disabled reuse those same
PNGs through `modulate_color`, so no asset was promoted for a brightness
change. `yellow.png`, `yellow_pressed.png`, their `.tres`/`.import`, and the
already-dead `ancient_tan.png` were deleted once nothing referenced them.

Two more cursor glyphs were promoted the same day from
`kenney_cursor-pixel-pack` into `game/assets/ui/cursors/kenney-pixel/`:
`tile_0026` → `pointer.png` and `tile_0154` → `hand_point.png`. That closes
this file's standing "later cursor-state pass" item without replacing the
cursor wholesale: the arrow and the pointing hand are now distinct glyphs
rather than one SVG re-tinted twice, and `CursorController` still bakes the
lineage accent per pixel. 3 of 220 tiles are imported.

The roguelike atlas needed no new promotion — its ground, bush, sprout, rubble
and berry-bush tiles were already in the imported sheet and simply were not
being used. Their coordinates now live in `game/scripts/Ui/TerrainAtlas.cs`.

On 2026-08-07 three more CC0 tiles were promoted from the same pack into
`game/assets/ui/kenney-pixel-adventure/9-slice/`, and the whole of
`game/assets/ui/kenney/` was deleted: `Tiles/Small tiles/Thick outline/tile_0071`
→ `red`, `tile_0070` → `red_outlined`, `tile_0075` → `green`. They replace
`kenney/9-slice/{red,red_pressed,green}`, which came from the older
`art/Kenney/` kit at 16×16 upscaled 3× to 48×48 — so `ButtonWarning` carried a
3× thicker border than the `ButtonText`/`ButtonPrimary` slate beside it. Both
button families are now native, at the same absolute border weight. `red_pressed`
reuses `red.png` through `modulate_color`, per the reuse rule above. Once the
theme stopped pointing at the old folder nothing referenced it at all, so
`ancient_brown`, `ancient_grey`, `grey`, `grey_pressed` and `green_pressed` went
with it rather than being pruned one by one.

Two structural facts about this pack, established by measuring every tile with
`tools/New-KenneyContactSheet.ps1` (which upscales tiles nearest-neighbour and
labels each with its index — the pack ships no semantic filenames, only
`tile_NNNN.png`, so a tile can only be identified by looking at it):

1. **The Large tiles are 9-slice material; the Small tiles are not.**
   `slate_raised_dark.png` is 1020/1024 opaque and fills its canvas, so
   `texture_margin = 8` is correct. The Small tiles are ~10×10 sprites centred in
   a 16×16 canvas with 3 px of transparent padding, so `texture_margin = 4`
   slices through the border and leaves the inner highlight ring inside the
   tiled centre — which renders as a repeating dot grid. The correct value is
   `texture_margin = 6`, which lands the centre on the uniform 4×4 interior.
   Verified at 1280×720 and 1920×1080.
2. **The pack has no dark tile.** Its darkest opaque tile centre is luminance
   114; `StyleBoxFlat_panel` is 17 and `StyleBoxFlat_panel_elevated` is 11. The
   slate tiles carry only 4–6 distinct tones, so darkening one by `modulate_color`
   to reach the project's panel value compresses its tonal range to ~7/255 —
   indistinguishable from a flat fill. **This pack can supply buttons, chips,
   slots and small widgets, but it cannot supply this game's dark panel
   surfaces** without either changing the palette or baking a composite asset.
   That is why `OverlayPanel`, `Panel`, `PanelCard`, `ScrollContainer` and
   `StatusStrip` remain `StyleBoxFlat`.

Also deferred for a structural reason rather than a taste one: the vertical
scrollbar track and grabber (`tile_0117`+`tile_0140`, `tile_0118`+`tile_0141`),
the banner ribbon (`tile_0043-0045`+`tile_0056-0058`) and the framed progress bar
are **multi-tile composites**, not single 9-slices. Using them needs a scripted
`art/source` → `art/exports` compositing step, since §10 of `ART_PIPELINE.md`
forbids hand-edited exported PNGs.

### Composited panel chrome

Finding 2 above says the pack cannot supply a dark panel. The way around it is
that the pack *also* ships frame tiles whose centre is fully transparent
(`tile_0008`, `tile_0009`, `tile_0019`, `tile_0032` in the Large set).
`tools/New-CompositeStylebox.ps1` takes one of those, floods the enclosed
interior with a project fill colour, and remaps the frame's own tones onto a
project border ramp — so the output keeps the authored pixel frame *and* the
project palette, which neither the raw tile nor a `modulate_color` could do.

| Output | Frame tile | Fill | Border ramp | Consumers |
| --- | --- | --- | --- | --- |
| `game/assets/ui/composites/panel_card.png` | `tile_0008` (3 tones) | `14,17,23,246` | tan, around the old `Color(0.43,0.37,0.25)` | `Panel`, `PanelCard` |
| `game/assets/ui/composites/panel_elevated.png` | `tile_0009` (4 tones) | `9,11,16,251` | gold, around the old `Color(0.83,0.66,0.3)` | `OverlayPanel` |

Both fills and both border hues are the values the previous `StyleBoxFlat`
carried, so **the palette did not move** — only the border gained depth (a 3–4
tone bevel instead of one flat line) and the corners gained the pack's ornament.
`content_margin` is unchanged at 14/12 and 18/16, so no layout metric moved
either.

Two consequences worth knowing before extending this:

- **`OverlayPanel` lost its drop shadow.** `StyleBoxTexture` has no `shadow_size`,
  and the old flat box had an 8 px soft one. For a project whose invariants
  require pure pixel art with no antialiasing, a blurred shadow was arguably off
  style already, but it is a real change and not a pure gain.
- **The composite does not reach panels that override the theme.** 17 of the
  repository's 24 `AddThemeStyleboxOverride` calls apply
  `LineageThemeRegistry.GetStyleBox("panel")` directly to a `PanelContainer`, which
  wins over the theme variation. `ResourceInventoryPanel` in the shelter view is
  the visible example. Unifying lineage theming with the theme instead of
  overriding it is the architectural work; this pass only fixes the asset.

Each generated PNG ships a `.recipe.json` beside it recording the frame tile,
fill, tone mapping and interior pixel count, so the asset is reproducible from
the repository alone. That mirrors the existing generated lineage panels under
`game/assets/ui/lineages/<lineage>/panel/`, which also keep a recipe next to the
PNG it produced. The margins are `8` for `panel_card` (6 px frame plus slack) and
`10` for `panel_elevated`, which samples the pack's clean recessed edge rather
than repeating its corner nub along the whole side. Both are even multiples that
land on whole pixels at the 1.5× that 1920×1080 implies.

### The compact HUD's chrome, 2026-08-08

The HUD profile needed a **1 px** border — measured off
`art/references/Proposal 06 — minimalist workstation.png`, which draws 1 px and
nothing thicker. Every tile in the pack was measured again for frame thickness,
which settled a question this file previously answered only for tone count:

| Set | Tile | Tones | Frame | Note |
| --- | --- | ---: | ---: | --- |
| Large/Thick | `tile_0008`, `tile_0009` | 3, 4 | 6 | in use → `panel_card`, `panel_elevated` |
| Large/Thick | `tile_0019`, `tile_0032` | 6, 3 | 6 | free, too heavy |
| Small/Thin | `tile_0018`, `tile_0062` | 4, 3 | 4 | free |
| Small/Thin | `tile_0019`, `tile_0039` | 4, 6 | 3 | thinnest **rectangular** frame |
| **Small/Thin** | **`tile_0069`** | **2** | **1** | 10×10 rounded outline, 1 px stroke |
| Small/Thin | `tile_0092` | 2 | 1 | the same outline, full-height |

`tile_0069` is the pack's only 1 px artefact. Its corners carry a 1 px chamfer
and the reference's are square; that one pixel is the whole cost of using an
authored frame instead of a drawn rectangle, and it was judged worth paying.

Seven composites are generated from that single tile by
`tools/New-CompositeStylebox.ps1`, each with its `.recipe.json`:

| Output | Fill | Ramp | Consumers |
| --- | --- | --- | --- |
| `hud_surface.png` | `9,12,19,250` | cool grey | `HudSurface`, `HudDock` |
| `hud_inset.png` | `7,10,17,250` | dim grey | `HudInset`, `HudHeaderSurface`, `HudCollapsibleHeader`, `HudProgress` background |
| `hud_card.png` | `19,20,29,250` | mid grey | `HudCard` |
| `hud_button.png` | `19,20,29,250` | light grey | `HudButton` |
| `hud_button_selected.png` | `26,23,18,250` | amber | `HudButtonSelected` |
| `hud_button_danger.png` | `32,18,18,250` | red | `HudButtonDanger` |
| `hud_badge.png` | `200,135,58,255` | dark amber | `HudBadge` |

All seven are `region_rect = Rect2(3, 3, 10, 10)` with `texture_margin = 6`'s
small-tile logic reduced to `2`: the region crops the source's 3 px transparent
padding, and a 2 px slice puts the corner chamfer in the corner cell. Hover,
pressed and disabled reuse those same PNGs through `modulate_color`, per the
standing rule that a brightness change is not grounds for a new asset — as does
`hud_dock.tres`, which is `hud_surface.png` at dock padding.

The final macro composition promoted no additional asset. `CityStatusPanel`,
`CitySummaryPanel`, `ExpeditionRail`, `PrimaryNavDock`, `ActionDock`,
`SimulationControls` and transient `ContextInspector` all resolve through the
same seven `hud_*.png` composites above. In particular, the contextual dock no
longer reaches the large-screen slate `OverlayPanel`, and the inspector uses
`HudCard`; this is reuse of the selected one-pixel family, not a new frame.

**Two tiles were composited and then withdrawn.** `hud_card` and `hud_dock` were
first baked from `tile_0019` (3 px) and `tile_0018` (4 px) and shown beside the
1 px frame in `HudComponentShowcase.tscn`. At 1920×1080 `tile_0019`'s corner
studs read as artefacts and `tile_0018` doubled its own edge, so both were
rebuilt on `tile_0069` and the heavier PNGs deleted rather than left in
`game/assets/` unused. That is the promotion checklist below working: the
showcase is step 2's "concrete UI state that needs it", and it is allowed to say
no.

No complete ZIP was extracted into `game/assets/`. 7 of 504 tiles are imported
directly, plus 9 composited from hollow frames — 2 for the screens and 7 for the
HUD, all seven of the latter from one tile.

## Promotion checklist

Before any downloaded asset enters `game/assets/`:

1. Confirm license and attribution requirements.
2. Identify the concrete scene or UI state that needs it.
3. Copy only the selected source asset, not an entire pack.
4. Record provenance and replacement status.
5. Verify nearest filtering, integer scale, canvas size, and visual hierarchy.
6. Add it to the applicable visual-regression state.
