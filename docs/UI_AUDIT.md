# UI Audit — Current state

**Last aligned:** 2026-08-07

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
| HUD | CanvasLayer-independent status/navigation surfaces; immediate time/alerts and global actions, without resource counters. Navigation is a shrink-wrapped vertical `NavigationRail` at the top-left, icon-only with tooltips, not the full-width strip it was until 2026-08-07. |
| Shelter resources | Collapsible icon-and-quantity inventory in Shelter detail; reservation detail remains available by tooltip. |
| Founding cargo | Construction shows the founder's 6-unit load expanded before Cache, then the site's 12-unit Cache; no hidden pre-camp warehouse. |
| Modals | `ModalHost` owns scrim, focus restoration, ESC and outside-click close. |
| Reusable controls | `StandardButtons`, `PanelHeader`, `AssignmentRow`, `SafeAreaMarginContainer` and shared theme variations. |
| Compact HUD foundation | An isolated second scale: six `Hud*` text variations (14–20 px), twelve `Hud*` chrome variations all drawing one 1 px Kenney frame at different fills, and the primitives `HudSectionHeader`, `HudMetricRow`, `HudResourceRow`, `HudProgressBar`, `HudBadge`, `CollapsiblePanelHeader`. Foundation only — no HUD surface consumes it yet. |
| Citizens | Selectable roster; selection does not activate camera follow; debug builds expose semantic routine context. |
| Policies | Read-only workday/production/off-duty/construction surface with bounded scroll. |
| Chronicle | Bounded scroll, compaction and causal blockers; routine resource gains are excluded from its presentation. |
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
| Pending | A complete normal-UI run of the current loop, plus relaunch boundaries | Not signed. |
