# UI Audit — Current state

**Last aligned:** 2026-08-08

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
| HUD | CanvasLayer-independent authored sibling surfaces. The 40 px edge-to-edge top bar carries stable brand, lineage/day/time context, a ledger-backed icon-only resource ticker, truthful population/capacity and temporary save feedback. The 240 px `CitySummaryPanel` frames the left and the 236 px `ExpeditionRail` frames the right. The compact 300×52 icon-only `PrimaryNavDock` owns bottom-centre primary navigation and yields that zone to the 480×72 contextual `ActionDock` during placement; bottom-right `SimulationControls` owns play/pause, speed and the camera-mode utility. Macro perspective activation owns the three macro-only summary/control surfaces; transient `ContextInspector` remains in its deterministic adjacent slot. |
| Shelter resources | Collapsible icon-and-quantity inventory in Shelter detail; reservation detail remains available by tooltip. |
| Founding cargo | Construction shows the founder's 6-unit load expanded before Cache, then the site's 12-unit Cache; no hidden pre-camp warehouse. |
| Modals | `ModalHost` owns scrim, focus restoration, ESC and outside-click close. |
| Reusable controls | `StandardButtons`, `PanelHeader`, `AssignmentRow`, `SafeAreaMarginContainer` and shared theme variations. |
| Compact HUD foundation | An isolated second scale: six `Hud*` text variations (14–20 px), twelve `Hud*` chrome variations all drawing one 1 px Kenney frame at different fills, and the primitives `HudSectionHeader`, `HudMetricRow`, `HudResourceRow`, `HudProgressBar`, `HudBadge`, `CollapsiblePanelHeader`, plus the justified project-specific `ConstructionQueueItem` and `ExpeditionCompactCard`. Status, city and expedition HUD surfaces consume these roles without parallel resource, metric or frame systems. |
| Citizens | Selectable roster; selection does not activate camera follow; debug builds expose semantic routine context. |
| Policies | Read-only workday/production/off-duty/construction surface with bounded scroll. |
| Chronicle | One `ChroniclePanel` embedded in `ExpeditionRail`: compact mode shows four rows; expanded mode reuses the rail's bounded scroll for the 80 newest compacted events, offline summary and actionable blocker groups. `ChronicleEventProjection` remains the single filtering/compaction rule, so routine resource gains stay excluded. The former `OfflineReportPanel` surface is removed. |
| Connected menus | Construction, Expeditions, Policies, Citizens and Pause retain their existing controllers and modal routing while sharing `HudSurface`, compact `Hud*` typography and `HudButton*` state roles. Hero and building detail retain their full-screen ownership but reuse HUD typography, progress, cards and actions for their information surfaces. |
| Save feedback | Temporary confirmation; no permanent `Saved` navigation chip. |
| Camera | Free default, explicit follow, WASD/arrows camera-only; uniform zoom preserves one perspective while a thirteen-street render window moves through the territory. |
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

## 3. Known presentation debt

- Forest/natural-resource, Town Hall and expedition art remain provisional.
- Some resource/system icons do not yet communicate their meaning strongly.
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
| Pending | A complete normal-UI run of the current loop, plus relaunch boundaries | Not signed. |
