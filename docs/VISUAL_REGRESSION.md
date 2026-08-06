# Visual regression matrix

This document is the reproducible visual-review contract for UI changes. A
successful headless boot is necessary, but it does not prove layout, focus,
occlusion, or readable content.

Each launched resolution writes to its own Godot log inside the capture
directory. This prevents a running editor or another matrix process from
contending for the default `user://logs` file before the window is exposed.

## Capture command

Run from the repository root with Godot 4.7.1 .NET:

```powershell
pwsh ./tools/Capture-VisualMatrix.ps1 `
  -GodotPath C:\Tools\Godot\Godot_v4.7.1-stable_mono_win64.exe `
  -OutputDirectory $env:TEMP\world-of-goses-visual `
  -StateName macro-current
```

Use normalized client coordinates to prepare an interactive state consistently
at every resolution. For example, the current status-bar pause control is:

```powershell
pwsh ./tools/Capture-VisualMatrix.ps1 `
  -GodotPath C:\Tools\Godot\Godot_v4.7.1-stable_mono_win64.exe `
  -OutputDirectory $env:TEMP\world-of-goses-visual `
  -StateName macro-paused `
  -NormalizedClicks '0.283,0.025'
```

Invoke the script directly when a fixture needs more than one click so
PowerShell preserves the string array:

```powershell
& ./tools/Capture-VisualMatrix.ps1 `
  -GodotPath C:\Tools\Godot\Godot_v4.7.1-stable_mono_win64.exe `
  -OutputDirectory $env:TEMP\world-of-goses-visual `
  -StateName construction-underway `
  -NormalizedClicks @('0.247,0.102', '0.57,0.75')
```

The Windows harness opens a real Godot window because Movie Maker always uses
the project's logical 1280×720 viewport and is not compatible with the headless
dummy renderer. It captures the window client at 1280×720 and 1920×1080,
rejects missing, empty, or incorrectly sized PNGs, and writes a JSON manifest. Captures are review artifacts and are
not committed by default. Use a distinct `StateName` for every prepared state.
The default scene is `CityPrototype.tscn`; `-ScenePath` may target a reusable
component scene. The harness sets `WOG_VISUAL_CAPTURE=1`: the controller still
loads the real slot as a fixture but suppresses every persistence write, including
periodic autosave and window-close save.

Typography has a dedicated in-engine capture because the desktop can
intermittently expose only Godot's 50×50 bootstrap window to the generic
window-handle harness. It also verifies that the title crop contains exactly
two colors (background and solid glyph), making grayscale fringe a terminating
failure:

```powershell
pwsh ./tools/Capture-TypographySpecimen.ps1 `
  -GodotPath C:\Tools\Godot\Godot_v4.7.1-stable_mono_win64.exe `
  -OutputDirectory $env:TEMP\world-of-goses-typography
```

States that cannot be reached reliably from an existing slot may use
`-VisualFixture offline-report`. Wound treatment uses
`-VisualFixture wound-recovery`; its action can be exercised at every
resolution with `-NormalizedClicks '0.5,0.48'`.

The `policies` fixture opens the read-only city Policies surface and validates
its bounded scroll body plus the macro action row at 1280×720 and 1920×1080.
The `migrant` fixture opens the Citizens roster used to choose the explicit
camera observation target. The `camera-depth-third-row` fixture places the
free camera on the final street of the first three-row parcel so near-plane
occlusion can be reviewed without relying on desktop keyboard focus. The
`cultivation-prepared` fixture creates a fresh Shelter plus one prepared plot;
`-NormalizedClicks @('R:0.14,0.658', 'L:0.179,0.636')` exercises the real
right-click menu and sow action at both official resolutions. All visual fixtures are ignored unless
`WOG_VISUAL_CAPTURE=1`; they do not alter the normal game flow. The selected
fixture is written into each manifest entry.

For manual extent comparison, `pwsh ./tools/Show-LongTerrariumProbe.ps1`
opens the current 8×9 windowed terrarium in a normal Godot window without
touching the live save. Pass `-Rows 16 -Columns 3` or `-Rows 21 -Columns 3` to
revisit the historical depth probes.

## Session capture

`tools/New-SessionSnapshot.ps1 -Mode Full` drives this same harness once per
session and commits the resulting `1280×720` frame to `docs/session-state/`.
That capture is **not** a matrix row and does not sign off anything: it uses
the live slot with no fixture and no clicks, so it records what the city
happened to look like rather than a prepared state. Its purpose is a scrubable
visual history, not acceptance. The matrix below still owns sign-off, and its
review artifacts still stay out of the repository.

## Required matrix

| State | Fixture/precondition | Automated capture | Human assertions |
| --- | --- | --- | --- |
| Macro, running | Loaded playable slot | Yes | Time/actions inside status bar with no resource counters; plots and Chronicle unobstructed |
| Macro street depth | `camera-depth-third-row` fixture | Yes | First street has crossed the near plane and is absent; second street remains as a large foreground obstruction; third street is the focal band; farther streets remain readable |
| Bounded terrarium window | `terrarium-8x9-window` fixture | Yes | Eight parcel rows by nine columns semantically; verify only thirteen construction streets render, the same perspective angle survives minimum zoom, the near/far edges approach the viewport bounds, and camera stepping reveals off-window rows |
| Orthogonal parcel terrain | Loaded playable slot | Yes | Eight parcel boundaries, integer-scaled ground, trees only, plots readable above terrain |
| Tree resource menu | `resource-menu` fixture | Yes | Menu anchored near tree, reserve copy, Gather/Close actions, no Forest cards |
| Tree gathering result | `resource-gather` fixture | Yes | Hero travel completes before +2 wood, reserve/tree count falls, icon + amount follows founder before Cache or appears above storage afterward, and no resource row reaches Chronicle |
| Shelter resource inventory | `shelter-resources` fixture | After opening detail | Pixel icons and quantities fit; capacity remains legible; chevron expands/collapses the section without disturbing the fixed back path |
| Founder cargo ownership | `eg2-founding-blueprint` then `eg2-founding-module-choice` | Yes | Construction shows 6-unit founder cargo before Cache; after Cache it relabels as 12-unit site storage; no resource counters return to the status bar |
| Cultivation prepared/action/sown | `cultivation-prepared` plus real right/left clicks | Yes | Prepared furrows, localized sow affordance, Food −1 and visibly distinct sown markers; HUD and Chronicle remain contained |
| Macro, paused | Pause selected | After preparing state | Pause action and selected multiplier are unambiguous |
| Construction, empty | No authorised project | After opening panel | Empty choices, long recipe text, close paths, focus |
| Construction, underway | Active project with contributors | After preparing slot | Progress, pause/cancel distinction, scrolling |
| Construction placement grid | `construction-placement-hover-invalid` fixture | Yes | Horizontal and vertical lines remain visible through blocked cells; hovered three-column blueprint is red with `[X]` blocker text before click; perspective remains shallow and pixel-stepped |
| Building detail | Selected Shelter/Farm/Quarry | After opening detail | Back path, worker slots, production controls, no clipping |
| Forest detail | Gatherable and depleted fixtures | After opening detail | Reserve/stock distinction and missing-input state |
| Hero profile | Existing founder | After opening profile | Long profile values wrap/scroll; Back to city remains visible |
| Offline report | Catch-up with maximum representative rows | After loading fixture | Bottom-right anchoring, compact rows, internal scrolling |
| ESC menu | `pause-menu` fixture | Yes | Scrim, title, Resume, disabled Settings placeholder, reset action, focus, and close paths |
| Reset confirmation | `pause-menu-reset` fixture | Yes | Consequence copy, destructive hierarchy, safe cancel path, and no clipping |
| Expedition idle | Dispatch button visible, no active expedition | After opening panel | Title, supply cost, return copy, Dispatch enabled, Cancel hidden |
| Expedition active | Active expedition in flight | After opening panel | Status text shows departure and return as world day/time, Cancel visible, Dispatch disabled, focus recoverable |
| Expedition returned | Active → Returned transition with one Stone deposited | After opening panel | Returned event visible in Chronicle, Expedition in City status, Dispatch re-enabled |
| Wound recovery | `wound-recovery` fixture | Yes | Wound severity/time and Shelter/Food action remain visible; click removes the action, debits Food, and starts countdown |
| World status indicators | `world-status-treatment` fixture plus a real pointer hover on a visible citizen | Yes | Hover bubble names the citizen and explains wound/treatment without permanent citizen clutter; full-storage badges remain persistent and legible |
| Citizen click summary | `citizen-click-summary` fixture plus a real left-click on the citizen's hit rect | Yes | `SelectionInfoPanel` shows the citizen's name and an at-a-glance activity line (same affordance trees and buildings already get); proves the click path runs end-to-end, not just the hover bubble |
| Pixel typography | `TypographySpecimen.tscn` | Yes | `W/w`, `O/o`, `M`, curves and diagonals use solid pixels only; no grayscale fringe or visibly unequal stroke caused by scaling |
| Forest depleted | `forest-depleted` fixture drains all natural-resource reserves | After loading fixture | Tree sprites disappear; parcel slots remain reserved; HUD/Chronicle/attention all stay inside the viewport |
| Migration | `migrant` fixture | Yes | Opaque reading surface, citizen count, Recruit/Close hierarchy, initial focus and no clipping |
| Astral opening | `astral-start` fixture | Yes | No lineage leakage, four narrative choices at their natural height, visible focus, fragments and readable fade-in. The footer sits inside the safe area and no scrollbar appears: the view has no `ScrollContainer`, so overflow would silently walk the footer off-screen rather than clip |
| Ground reveal | `astral-ground` fixture | Yes | Real board visible only through the configured 15% astral veil |
| Founder identity | `astral-identity` fixture | Yes | Only the resulting lineage/sprite is revealed. The naming beat carries the portrait, the name field and the body-presentation pair and nothing else: the result copy has moved to its own step. The pair is one compact control of roughly 304 px, never an expanded pair spanning the safe area, and the selected option carries a check glyph as well as the `ButtonPrimary` palette. Footer stays inside the safe area with no scrollbar |
| Founder card | `astral-founder-card` fixture | Yes | Shows exactly the seven fields the bible's final screen requires — name, body presentation, sprite (carried by the preceding step), lineage, elemental affinity, the three Cube axes, brief summary — and none of its seven forbidden ones; in particular **no weapon families**. Each axis prints both integers and the dominant pole stays identifiable in a desaturated copy of the capture. Footer contained, no scrollbar |
| Founder arrival | `founder-arrival` fixture | Yes | Fall targets the first free construction lot, impact placeholder remains aligned, title card is original and readable |
| Ambient day/night | `time-midnight`, `time-dawn`, `time-noon`, `time-dusk` fixtures | Yes | Each fixture pins the tint to one moment regardless of the save's clock. `time-noon` must be pixel-identical to an untinted capture (white multiplies to identity). `time-midnight` must be visibly darker and cooler while every terrain band, tree and building still reads apart — a night that flattens the map into one hue is the regression this row exists to catch. In all four, the status strip, navigation buttons and Chronicle keep their authored colours |
| First-night strip | `firstnight-strip` fixture: founder manifested, spirit just arrived | Yes | Non-modal band sits at the bottom of the screen with `OverlayLayers.Tutorial=50`, body text wraps to the available width, the `Continue` button stays inside the band, clicks on the band do not propagate to the macro view, and clicks above the band still reach the macro view without `MouseFilter.Stop` swallowing them |
| First-night spirit visual | `firstnight-spirit` fixture: founder manifested with the campfire completed | Yes | The 16-point ring sits beside the founder before `CampfireBuilt` and on the campfire afterwards; the triangular glyph stays inside the ring; the position reads as derived from the world, never as a hard pin to the screen |
| First-night embers | `firstnight-embers` fixture: city past the night with `SpiritDeparted` in the chronicle | Yes | A small orange translucent quadrilateral sits on the campfire's projected position; the spirit's inhabited ring is gone; the macro view's other terrain reads apart from the embers |
| First-night spirit trail button | `firstnight-spirit-trail-button` fixture: same as embers, with `ExpeditionPanel` open | Yes | The `SpiritButton` is visible and enabled; the other two resource objective buttons stay enabled; the panel summary line covers the trail; clicking the button arms `SpiritTrailSearch` and re-renders the summary |

Every applicable row must be checked at both official harness resolutions:
1280×720 is the logical baseline and 1920×1080 is the standard full-HD scale.
Changes to anchoring or safe-area behavior additionally require ultrawide, 4:3,
or vertical viewports only as targeted exploratory checks when the change puts
those shapes at risk.

## Review record

For each UI change, record:

- commit or change identifier;
- state names and manifest paths;
- compared resolutions;
- pass/fail for overflow, overlap, occlusion, focus, and close/back behavior;
- reviewer and date;
- any deliberately deferred state with its backlog ID.

Do not mark a visual acceptance criterion complete solely from `dotnet test` or
Godot headless boot. Automated capture proves reproducibility; a person still
signs composition and interaction until image-diff baselines are intentionally
introduced.

## Executed reviews

| Date | State | Resolutions | Result |
| --- | --- | --- | --- |
| 2026-07-22 | `macro-current` | 1024×576, 1280×720, 1600×900 | Captures and dimensions valid. HUD, actions, plots, and Chronicle stay inside the viewport. Failed citizen-label composition: the status icon obscures the first character of persisted name `zeventh` at all three sizes (M-16). |
| 2026-07-22 | `macro-m16-fixed` | 1024×576, 1280×720, 1600×900 | Pass. Persisted name `zeventh` is fully visible; the 16×16 contained icon and 6 px separation no longer overlap it. Harness also forces its Godot window to foreground before capture, preventing external-window contamination. |
| 2026-07-22 | `macro-paused` | 1024×576, 1280×720, 1600×900 | Pass. The main control changes to the play action, the retained speed control is visibly disabled, and the status bar remains legible without overlaps. An initial `0.21,0.025` click missed the control and was discarded; the reviewed fixture uses `0.283,0.025`. |
| 2026-07-22 | `construction-empty-pass2` | 1024×576, 1280×720, 1600×900 | Pass after fixes. The no-active-project choices fit without clipping, `View hero` retains its icon and label, the macro action changes immediately to `Close construction`, and Chronicle remains hidden behind the modal across live ticks. Intermediate captures exposed the blank packed-scene button and Chronicle refresh race and were rejected. |
| 2026-07-22 | `construction-underway-pass` | 1024×576, 1280×720, 1600×900 | Pass after fixes. The active-project HUD uses the concise project/city chips and remains inside both edges; the preview is aspect-contained and no longer overlaps instructions; header, scrolling body, Pause/Resume, Cancel, and View hero remain reachable. Invalid multi-click manifests and non-equivalent modal states were rejected. |
| 2026-07-23 | `shelter-detail-pass` | 1024×576, 1280×720, 1600×900 | Pass. Shelter art, resting citizen label, capacity summary, persistent status bar, and `Back to city` remain visible without overlap. An earlier black 1024×576 frame was rejected and recaptured after rebuilding. |
| 2026-07-23 | `farm-detail`, `quarry-detail` | 1024×576, 1280×720, 1600×900 | Pass for layout. Production, stock, policy controls, assignment sidebar, available citizen action, and `Back to city` remain inside the viewport. Review also found that the runtime still exposes `Reactive policy` although `CURRENT_STATUS.md` describes the future simplified panel; that product/code mismatch is not treated as a visual pass criterion. |
| 2026-07-23 | `forest-detail` | 1024×576, 1280×720, 1600×900 | Pass for a gatherable Forest. Stock/reserve, foraging rate, production controls, assignment sidebar, and back path remain visible. A deterministic depleted-detail fixture is still required because depleted macro plots intentionally disable entry. |
| 2026-07-23 | `hero-profile-fixed` | 1024×576, 1280×720, 1600×900 | Pass after fixing the profile scroll surface. The first capture exposed white copy on the global yellow `ScrollContainer` style and right-edge clipping at 1024×576. The profile now uses its dark reading surface, keeps a scrollbar gutter, wraps long copy, and preserves the fixed `Back to city` header. |
| 2026-07-23 | `tutorial`, `tutorial-long` | 1024×576, 1280×720, 1600×900 | Historical pass, **superseded on 2026-08-05**. The layout was sound but the copy was not: its three hand-written steps asked for `1 wood` from the Forest plots and described status chips that no longer exist, while the real first construction is the Campfire (3 Branches + 2 Small Stone). `TutorialOverlay` and both fixtures were removed rather than rewritten, because a hand-written step list drifts again on the next increment. The authored guidance layer is reserved for the first-night dialogue surface, which needs a fresh signature. |
| 2026-07-23 | `offline-report` | 1024×576, 1280×720, 1600×900 | Pass with a deterministic 80-event fixture. The first capture was overwritten by the live Chronicle refresh and was rejected. Capture mode now freezes the representative offline report; summary, decision rows, event list, scrollbar, header, and bottom-right anchoring remain inside the viewport. |
| 2026-07-23 | `pause-menu`, `pause-menu-reset` | 1024×576, 1280×720, 1600×900 | Pass after rejecting the first capture, which exposed blank packed-scene `IconButton` content. The corrected menu shows its icon/label hierarchy, pauses the simulation, provides ESC/X/scrim close paths, and separates permanent reset behind an explicit confirmation. Capture mode suppresses persistence writes; no live slot was reset. |
| 2026-07-23 | `orthogonal-terrain` | 1024×576, 1280×720, 1600×900 | Initial green capture rejected for excessive saturation and two atlas coordinates that rendered water tiles instead of trees. Brown-floor iteration was also rejected as too architectural. Final olive-ground capture uses only verified green/orange tree tiles; eight parcel boundaries, buildings, hero activity, macro actions, and Chronicle remain readable. |
| 2026-07-23 | `resource-menu`, `resource-macro` | 1024×576, 1280×720, 1600×900 | Pass. Forest placeholder cards are gone. Interactive trees derive from current reserves, the contextual menu stays inside the viewport, and the new Menu action fits beside View hero and Construction. Gather and Close retain visible labels/icons. |
| 2026-07-23 | `resource-gather` | 1024×576, 1280×720, 1600×900 | Historical pass in read-only capture mode. Its former status/Chronicle expectation was superseded on 2026-08-03 by physical-owner icon + amount feedback (following the founder before Cache); the fixture now requires a fresh visual signature. |
| 2026-07-23 | `tree-click`, `construction-scroll-fixed` | 1024×576, 1280×720, 1600×900 | Pass. A physical click on the upper-left tree opens Gather/Close, proving the center layout no longer intercepts resource input. The construction fixture reaches the bottom of the scroll body, exposes Assigned/Available and fixed footer actions, and shows the founding shelter with one contributor. |
| 2026-07-23 | `pixel-route-final`, `pixel-detail-retry` | 1024×576, 1280×720, 1600×900 | Pass. The in-flight gather route places the hero above-left of the Shelter instead of crossing its footprint. Shelter detail remains contained after removing its continuous fade and quantizing citizen carrier motion. The first detail attempt used a stale coordinate and was rejected. |
| 2026-07-23 | `stable-tree-unit` | 1024×576, 1280×720, 1600×900 | Pass after arrival. The selected upper-left tree is absent while later tree slots remain in place. The citizen marker stays at the depleted slot after the gather-triggered refresh. Citizen marker and name now share one moving container. Capture mode migrated the loaded v6 fixture in memory without writing it. |
| 2026-07-23 | `travel-refresh-guard`, `travel-refresh-arrival` | 1024×576, 1280×720, 1600×900 | Pass with Shelter and Farm complete and Quarry under construction. At 2 seconds the citizen remains mid-route despite project refresh events; at 6 seconds it has reached the upper-left resource slot. Active travel is no longer rebuilt by `CityMacroView.Refresh`. |
| 2026-07-31 | `eg2-founding-blueprint`, `eg2-founding-module-choice`, `eg2-founding-blocked-cargo` | 1280×720, 1920×1080 | Pass with deterministic fresh-city fixtures. The localized Campfire cost and enabled Founding Site CTA fit; after Campfire, Bedroll and Cache remain simultaneously visible without Canopy being chosen automatically; a worst-case 6/6 Wild Food load in `AwaitingModule` exposes the localized cargo-return recovery action beside those module choices. Header, scroll body and fixed footer remain contained at both official resolutions. Authored module sprites remain pending art integration; the current Home placeholder is intentional. |
| 2026-07-31 | `early-game-resources`, `early-game-resource-menu`, `early-game-resource-gather-signal` | 1280×720, 1920×1080 | Pass. Fresh fixture shows six mature trees and all 17 EG-A0 ground nodes. A real right-click on a Branches bundle opens the localized contextual action; confirming it routes the founder, removes that exact node and records `Branches +2` without changing Wood. Ground markers are intentional code-drawn placeholders pending authored sprites. |
| 2026-07-23 | `resource-menu-current`, `physical-gather` | 1024×576, 1280×720, 1600×900 | Pass through the real menu button rather than the direct fixture. A physical click on Gather closes the resource menu and advances the marker/name container by roughly 24 px within 350 ms (three 8 px cadence steps at 1024×576), confirming the contextual signal reaches `TravelHeroTo`. |
| 2026-07-24 | `expedition-idle`, `expedition-active`, `expedition-returned` | 1024×576, 1280×720, 1600×900 | Automated pass after rejecting the initial translucent panels. The dark modal surface keeps all copy readable; active state shows departure/return as world day and time and removes the leader from the city. The returned fixture initially froze the UI by replaying 14,400 ticks synchronously; its equivalent one-tick transition now completes the three-resolution matrix in 9.1 s and restores the leader. Human focus/close signature remains tracked by M-14. |
| 2026-07-24 | `migrant-panel` | 1024×576, 1280×720, 1600×900 | Automated layout pass. Current population, recruitment copy, Recruit, and Close remain legible and contained on the same opaque modal surface. Human keyboard/gamepad signature remains tracked by M-14. |
| 2026-07-25 | `forest-depleted` (headless) | n/a (fixture mode suppresses persistence) | The new `DrainAllForestsForVisualRegression` API in `CityWorldController` empties every natural resource patch through `WOG_VISUAL_CAPTURE` capture mode. The headless boot completes without C# or Godot errors. Windowed composition of the resulting empty macro view awaits an interactive desktop (50×50 client limitation). |
| 2026-07-29 | `wound-recovery`, `wound-treatment-started` | 1024×576, 1280×720, 1600×900 | Historical three-size pass. The wound row and Shelter/Food treatment action remained contained; a physical click consumed 1 Food and started recovery. Capture mode suppressed persistence. |
| 2026-07-29 | `wound-recovery`, `wound-treatment-started` | 1280×720, 1920×1080 | Pass under the new official two-resolution contract. The captured duration read “1 día de tratamiento restantes” instead of exposing ticks; a physical click consumed 1 Food, removed the action, and started recovery without overflow. A later locale-only grammar polish changed this to “tiempo de tratamiento: 1 día”; its recapture was blocked when the desktop harness reported a 50×50 client, so the final wording still needs a graphical signature. |
| 2026-07-29 | `typography-pixel-perfect` | 1280×720, 1920×1080 | Pass through an in-engine viewport capture after forced reimport. Geist Pixel, Jersey 10, and Pixelify Sans render `W/w`, `O/o`, `M`, curves, diagonals, accents, and numerals with solid pixels. The title crop contains exactly two colors (background and glyph) at both resolutions; the former grayscale fringe is absent. The window-handle harness still intermittently reports 50×50, so this fixture writes the viewport image directly when given its capture argument. |
| 2026-07-30 | `world-status`, `world-status-hover-paused`, `world-status-treatment` | 1280×720, 1920×1080 | Pass. Farm/Quarry full-storage badges remain visible without covering their buildings; a real pointer move exposed the contextual idle bubble at 1280×720; the deterministic treatment fixture exposed Tamara's moderate wound and one-day treatment at both official resolutions. The bubble remains transient and contained, while the building badges remain persistent. |
| 2026-07-30 | `citizen-click-summary` input-boundary fix | n/a (scene-tree fix) | `GameUiShell` and `ScreenContent` now declare `mouse_filter = 2` (Ignore) in `CityPrototype.tscn`. Without that, the fullscreen layout container swallowed every pointer event over the macro world — `GuiGetHoveredControl()` returned `GameUiShell`, `_UnhandledInput` never fired, the citizen bubble blinked open then was hidden one frame later by `ClearWorldStatusHover`, and left-clicks on citizens never reached `TryClick`. Building/tree clicks looked like they worked because the visual-regression fixtures call `TryClick` directly. `MacroInputBoundaryTests` now guards the property on both nodes. The `citizen-click-summary` row still needs a real left-click capture before it can be signed off — code review alone is insufficient (see `verify-clicks-with-real-clicks`). |
| 2026-07-31 | `camera-depth-third-row` | 1280×720, 1920×1080 | Pass. Manifest: `%TEMP%\wog-camera-depth-final\camera-depth-third-row-manifest.json`. The first street is absent after crossing the near plane, the second remains as a large foreground obstruction, the third is the focal band, and the remaining streets stay readable without overflow. This is a presentation stabilization, not a vertical-slice advance. |
| 2026-07-31 | `cultivation-prepared`, `cultivation-action`, `cultivation-sown` | 1280×720, 1920×1080 | Pass with a deterministic fresh-city fixture. The prepared plot reads as empty furrows beside the Shelter; a real right-click opens the localized sow affordance; a real left-click consumes one Food (7→6), closes the action and adds three non-color-only sow markers. HUD, selection summary and Chronicle remain contained. Manifests: `%TEMP%\wog-cultivation-valid`, `%TEMP%\wog-cultivation-menu`, `%TEMP%\wog-cultivation-sown`. Invalid pre-fixture captures that fell back to the loaded slot were rejected. |
| 2026-08-03 | `construction-placement-hover-invalid` | n/a (fixture and headless boot verified) | The fixture selects a domain-blocked three-column window before click so the complete grid and `[X]` feedback can be signed visually. Automated desktop capture was attempted but rejected because the Godot client reported 50×50 instead of 1280×720; no visual pass is claimed. |
| 2026-08-03 | `terrarium-8x9-window` | 1280×720 direct viewport | Diagnostic pass. The 8×9 semantic envelope keeps the original pseudo-perspective instead of stretching all rows into the viewport. At minimum zoom (`1.30`, uniform), lower pivot and focus street 2, thirteen street bands render from approximately `y=712` to the upper usable edge at `y=80`: two foreground, the focus and ten receding bands. The founding three-column band is recentered within the nine columns; remaining rows wait for discrete camera movement. Capture: `%TEMP%\wog-terrarium-8x9-window-final.png`. The final sample was 15.201 ms, so lateral culling/batching remains open. |
| 2026-08-06 | `astral-start`, `astral-ground`, `astral-identity`, `astral-founder-card` | 1280×720, 1920×1080 | Pass for containment after the onboarding layout rework. The identity step previously overflowed its 656 px budget by roughly 120 px and walked the footer off-screen; it now measures ~567 px with the footer inside the safe area at both resolutions. Choice buttons dropped from a forced 66 px to their natural 40 px, the `_narrative`/`_consequence` 92 px and 52 px floors are gone, empty rows hide instead of blanking, and two `ExpandFill` spacers absorb the slack so no row can push the footer out. The body-presentation pair is one 304 px control instead of two ~510 px expanded buttons, and its selected option carries a check glyph as well as the palette. The new `astral-founder-card` step renders the bible's seven fields with three `CubeAxisBar` rows and no weapon families. Manifests under `%TEMP%\wog-onboarding`. **Not signed:** the city status strip and macro action row remain visible through the deliberately translucent veil on the identity and card steps. That is pre-existing — `HeroAccessButton` already gates itself on `NeedsOnboarding()` but `CityStatusPanel` and `MacroActions` never did — and is out of this change's scope; it is the one composition defect still visible in these captures. |
