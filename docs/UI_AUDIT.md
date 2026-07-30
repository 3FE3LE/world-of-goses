# UI Audit — Current state

**Last aligned:** 2026-07-29

**Baseline:** clean build, 553/553 tests, successful headless boot.

**Active signature:** VS-5 at 1280×720 and 1920×1080.

This audit complements `CURRENT_STATUS.md` § *Presentation and input* and the
reproducible matrix in `VISUAL_REGRESSION.md`.

## 1. Current architecture

| Area | Current state |
| --- | --- |
| Macro world | `MacroStreetLiveView` is the only runtime macro representation. |
| HUD | CanvasLayer-independent status/navigation surfaces; immediate time/resources/alerts only. |
| Modals | `ModalHost` owns scrim, focus restoration, ESC and outside-click close. |
| Reusable controls | `StandardButtons`, `PanelHeader`, `AssignmentRow`, `SafeAreaMarginContainer` and shared theme variations. |
| Citizens | Selectable roster; selection does not activate camera follow; debug builds expose semantic routine context. |
| Policies | Read-only workday/production/off-duty/construction surface with bounded scroll. |
| Chronicle | Bounded scroll, compaction and causal blockers; macro-only visibility. |
| Save feedback | Temporary confirmation; no permanent `Saved` navigation chip. |
| Camera | Free default, explicit follow, WASD/arrows camera-only. |
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

## 3. VS-5 human checklist

### Containment and reading order

- [ ] 1280×720: no clipped panel, footer, close button, HUD chip or Chronicle.
- [ ] 1920×1080: no excessive expansion, overlap or unreadable empty space.
- [ ] Policies and Citizens keep actions visible while long bodies scroll.
- [ ] Expedition phase/outcome, wound/treatment and territory copy remain
      legible without exposing raw ticks.

### Input and focus

- [ ] Every modal closes through its visible close path and ESC.
- [ ] Keyboard/gamepad focus reaches every critical action in the VS-5 run.
- [ ] Disabled actions expose a text reason, not color alone.
- [ ] Scrolling any panel never zooms the city behind it, including at the
      first and last scroll row.
- [ ] Selecting a citizen never starts follow; explicit follow tracks the
      selected citizen; manual camera input releases follow coherently.
- [ ] WASD/arrows never move the founder directly.

### Citizen and city representation

- [ ] Founder and recruited citizens travel visibly through the same route
      system and do not teleport into Farm/Quarry/Town Hall.
- [ ] Citizens disappear only after logical building entry and reconstruct at a
      context-appropriate anchor after load.
- [ ] Storage/food/schedule blockers produce a coherent wait/rest/leisure state,
      not a frozen citizen at an entrance.
- [ ] Multiple citizens never duplicate a carrier when switching macro/detail.
- [ ] Gather routes remain visible and pass through valid gaps between trees.

### Complete-loop feedback

- [ ] Prospect arrival and housing restriction are understandable.
- [ ] Daily Food pressure is visible before it becomes a soft lock.
- [ ] Expedition team, supplies and retreat posture are discoverable.
- [ ] Encounter, return, wound and territory changes identify subject and cause.
- [ ] Treatment communicates Food cost, duration and completion.
- [ ] Save confirmation appears briefly and disappears.

## 4. Known presentation debt

- Forest/natural-resource, Town Hall and expedition art remain provisional.
- Some resource/system icons do not yet communicate their meaning strongly.
- Large-event feedback needs final human tuning.
- Toast/tutorial/Chronicle exclusion is not centralized; implement a shared host
  only if VS-5 reproduces an actual collision.
- Expedition UI should consume a dedicated presentation snapshot before adding
  another planning/outcome dimension.
- No final audio feedback or bus tree is wired.

## 5. Sign-off rule

A UI flow is not complete because it compiles or appears in a headless scene.
Closure requires the relevant matrix plus a real pointer/keyboard/gamepad path
to its domain effect. Record any failed state in `TO_DO.md` under VS-5 or reopen
the owning gap in `FIRST_PLAYABLE_LOOP_AUDIT.md`.

## 6. Human signature history

| Date | Scope | Result |
| --- | --- | --- |
| 2026-07-29 | Policies and Citizens, 1280×720 / 1920×1080 | Contained; scroll surfaces and actions visible. |
| Pending | Complete VS-5 normal-UI run and relaunch boundaries | Not signed. |
