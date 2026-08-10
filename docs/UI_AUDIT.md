# UI Audit — Current state

**Last aligned:** 2026-08-10

**Baseline:** measured, never restated here. See
[`session-state/STATE.txt`](session-state/STATE.txt).

**Official review sizes:** 1280×720 and 1920×1080.

> **This file records results.** The checklist a person walks *before* signing
> lives in [`VISUAL_REGRESSION.md`](VISUAL_REGRESSION.md) §&nbsp;*Human sign-off
> checklist*, together with the sign-off rule. It moved there on 2026-08-07: a
> reusable checklist and a dated record of what was signed are two documents,
> and keeping them in one produced twenty-six permanently unchecked boxes that
> read as a backlog nobody owned.
>
> The old framing pinned every box to the VS-5 audit, discarded on 2026-07-31
> along with `FIRST_PLAYABLE_LOOP_AUDIT.md`. That document no longer exists;
> references to it are gone rather than repaired.

This audit complements `CURRENT_STATUS.md` § *Presentation and input* and the
reproducible matrix in `VISUAL_REGRESSION.md`.

## 1. Current architecture

| Area | Current state |
| --- | --- |
| Macro world | `MacroStreetLiveView` is the only runtime macro representation. |
| HUD | CanvasLayer-independent authored sibling surfaces. The 40 px edge-to-edge top bar carries stable brand, lineage/day/time context, a ledger-backed icon-only resource ticker (priority-ordered, deterministic 5-chip cap with a `+N` overflow affordance whose tooltip lists hidden resources and their exact amounts, compact K/M formatter for large quantities, full amounts in tooltips), truthful population/capacity, temporary save feedback, and a right-edge utility cluster (`CameraButton`, `SpeedButton`, `MenuButton`). The 240 px `CitySummaryPanel` frames the left and the 236 px `ExpeditionRail` frames the right. The compact 520×60 labelled `PrimaryNavDock` owns bottom-centre primary navigation and yields that zone to the 480×72 contextual `ActionDock` during placement. `SimulationControls`, `PlayPauseButton` and the paused world state are gone; Speed cycles 1x/2x/4x in the top bar. Macro perspective activation owns the two macro-only side summaries; transient `ContextInspector` remains in its deterministic adjacent slot. |
| Shelter resources | Collapsible icon-and-quantity inventory in Shelter detail; reservation detail remains available by tooltip. |
| Founding cargo | Construction shows the founder's 6-unit load expanded before Cache, then the site's 12-unit Cache; no hidden pre-camp warehouse. |
| Modals | `ModalHost` owns scrim, focus restoration, ESC and outside-click close. |
| Reusable controls | `StandardButtons`, `PanelHeader`, `AssignmentRow`, `SafeAreaMarginContainer` and shared theme variations. The expedition-live seam has four PackedScenes: a real pixel-octagonal `OctagonalSkillSlot` with five states and eight invisible side anchors, `ExpeditionSquadSlot`, and four-child squad/skill strips. They remain presentation-only and do not change `ExpeditionRequest.MaxTeamSize`; `ExpeditionLiveView` is their first structural consumer. |
| Expedition live perspective | One authored `ExpeditionLiveView` instance lives under `GameUiShell/ScreenContent`. A real active expedition exposes `VER` in `ExpeditionRail`; selecting it hides Macro, both side summaries, primary/context/action docks and the rail without freeing them. `CityStatusPanel` remains global. The fixed reference-converged composition gives the battlefield the central 800×488 region, frames it with 228/224 px information columns, and reserves the lower edge for the 441 px four-slot squad, four enlarged octagonal Skills and stacked AUTO/RETIRADA controls. The view projects route, real members, health/stamina when available and real progress; missing encounter threat/enemy data is named as unavailable. AUTO/RETIRADA and the lateral stage are visual structure only, with no combat resolver or private clock. |
| Compact HUD foundation | An isolated second scale: six `Hud*` text variations (14–20 px), twelve `Hud*` chrome variations all drawing one 1 px Kenney frame at different fills, and the primitives `HudSectionHeader`, `HudMetricRow`, `HudResourceRow`, `HudProgressBar`, `HudBadge`, `CollapsiblePanelHeader`, plus the justified project-specific `ConstructionQueueItem` and `ExpeditionCompactCard`. Status, city and expedition HUD surfaces consume these roles without parallel resource, metric or frame systems. Every `ResourceType` has a project-owned procedural pixel silhouette in `ResourceIcon` and a single shared priority sequence in `ResourcePriority` that the top-bar ticker and the city summary both read. |
| Citizens | Selectable roster; selection does not activate camera follow; debug builds expose semantic routine context. |
| Policies | Read-only workday/production/off-duty/construction surface with bounded scroll. |
| Chronicle | One `ChroniclePanel` embedded in `ExpeditionRail`: compact mode shows four rows; expanded mode reuses the rail's bounded scroll for the 80 newest compacted events, offline summary and actionable blocker groups. `ChronicleEventProjection` remains the single filtering/compaction rule, so routine resource gains stay excluded. The former `OfflineReportPanel` surface is removed. |
| Connected menus | Construction, Expeditions, Policies, Citizens and Pause retain their existing controllers and modal routing while sharing `HudSurface`, compact `Hud*` typography and `HudButton*` state roles. Hero and building detail retain their full-screen ownership but reuse HUD typography, progress, cards and actions for their information surfaces. |
| Save feedback | Temporary confirmation; no permanent `Saved` navigation chip. |
| Camera | Free default, explicit follow, WASD/arrows camera-only; physical arrows are consumed before HUD focus dispatch in unobstructed macro mode, while gamepad D-pad retains explicit HUD navigation. Uniform zoom preserves one perspective while a thirteen-street render window moves through the territory. |
| World dialogue | Hover/status bubbles plus first-night balloon, spirit and embers use `OverlayLayers.WorldDialogue`: above the ambient tint, below persistent HUD/modals, and horizontally clamped to the central world corridor between the 240/236 px rails. |
| Wheel input | A hovered `ScrollContainer` owns the wheel at both scroll limits; map zoom cannot leak through. |
| Localization | Native EN/ES PO catalogs and hot locale changes. |

## 2. Current visual evidence

- Macro, construction, building detail, onboarding, profile, expedition,
  Citizens, Policies, Chronicle and selected regression fixtures have existing
  windowed captures.
- Policies and Citizens were revalidated at 1280×720 and 1920×1080 after their
  responsive scroll changes.
- The fixture harness validates client dimensions and samples actual Godot
  process-frame time.
- Headless boot validates scene/resource wiring, not final composition or input.
- The final Proposal-06 matrix has valid 1280×720 and 1920×1080 frames for
  default, selection, active construction, active expedition, placement and
  real-ESC restoration under `%TEMP%\wog-final-macro-hud`. The hero pointer
  fixture also confirms macro-only side/bottom surfaces are absent on the full
  profile view.
- `ExpeditionHudComponentShowcase` isolates the future live-view building
  blocks from gameplay and exposes default, keyboard-focus and gamepad-focus
  fixtures at both official sizes.

## 3. Known presentation debt

- Forest/natural-resource, Town Hall and expedition art remain provisional.
- Large-event feedback needs final human tuning.
- Toast/tutorial/Chronicle exclusion is not centralized; implement a shared host
  only if VS-5 reproduces an actual collision.
- No final audio feedback or bus tree is wired.

## 4. Human signature history

The only place a checklist box becomes true. An unlisted surface is unsigned,
whatever the code does.

| Date | Scope | Result |
| --- | --- | --- |
| 2026-07-29 | Policies and Citizens, 1280×720 / 1920×1080 | Contained; scroll surfaces and actions visible. |
| 2026-08-06 | Expedition team, finite Food/Wood objectives, supplies and retreat posture | Signed. Posture uses pixel-font `[X]`/`[ ]` buttons, verified by real pointer capture at both official sizes. |
| 2026-08-08 | Embedded Chronicle, 300×52 icon-only dock, Construction, Expeditions, Policies, Citizens and Pause | Signed at 1280×720 and 1920×1080. Chronicle begins on its offline summary/decisions and retains bounded scroll; promoted icons remain legible after tint normalization. |
| 2026-08-08 | World-dialogue layering, macro arrow isolation, Construction → Hero route | Signed at 1280×720 and 1920×1080. Dialogue remains legible inside the central corridor below both rails; a physical Right arrow moves only the camera while preserving HUD focus; a real View Hero click closes Construction before the profile appears. |
| 2026-08-09 | HUD visual convergence toward Proposal 06 — labelled `PrimaryNavDock` (520×60, five destinations: Héroe / Obra / Exp. / Norma / Gente), `SimulationControls` shrunk to time only (76×32), right-edge `UtilityCluster` inside `CityStatusPanel` carries the camera-mode toggle and menu/pause open | Captured at 1280×720 and 1920×1080. Pending human sign-off on the final docked dimensions. |
| 2026-08-10 | No-pause HUD correction — deleted `SimulationControls` and `PlayPauseButton`, moved Speed between Camera and Menu, expanded `ExpeditionRail` through the reclaimed lower-right space | Implemented and covered structurally on HEAD; a fresh human visual signature was not produced because the Full snapshot attached to a 50×50 client. |
| Pending | A complete normal-UI run of the current loop, plus relaunch boundaries | Not signed. |
