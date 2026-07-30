# Citizen offline routine and reconstruction audit

> Stabilization record for the semantic-persistence, work-routine, visual
> reconstruction, save feedback, policy, and camera pass completed on
> 2026-07-29.

## Outcome

The existing schema already persisted citizen context rather than viewport
coordinates. Schema v19 therefore remains current and no save migration was
required. The correction makes that context observable through one domain
routine projection, reconstructs partially elapsed travel instead of replaying
it from its origin, prevents a standing order from creating a false route on
load, and keeps temporary production blocks distinct from worker capacity.

The simulation remains authoritative with no Godot node instantiated. Godot
owns only route planning, quantized locomotion, contextual anchors, ambient
wandering, animation, and camera observation.

## Root causes

1. `CitizenSave` already stored `CurrentLocation`, `TransitStartedAtTick`,
   `IsReturningHome`, the standing `WorkOrder`, and the current commitment, but
   `CityMacroSnapshot` did not expose the transit start to the macro view.
   Re-entering the view or loading a mid-route save therefore planned the full
   route again from its semantic origin.
2. The founder path treated a newly observed standing assignment as a command
   to start walking even when the restored domain state was `AtHome` because
   the workplace was full. A real headless load reproduced this as a false
   `[CitizenTravel] started` message. Route creation now requires
   `CitizenLocation.InTransit`.
3. `OfflineProgression.ApplyAll` returned `None` when elapsed time changed only
   the clock. The world advanced correctly, but diagnostics claimed no offline
   progression. Reports now use `WorldTimeAdvance.Result.TicksElapsed`.
4. Once full-stock workers returned home, the production simulator could
   replace `TargetReached` with `WorkersInTransit`. Stop-cause resolution now
   preserves storage-full as the causal block.
5. Work schedule, location, vital state, assignment, and production cause were
   individually available but no single projection explained the citizen's
   current activity, blocker, contextual anchor, or next transition.
6. The autosave interval was ten real seconds and periodic saves wrote even
   after no state change. The HUD retained the last save timestamp as a
   permanent chip.
7. The macro camera was free by default, but follow still targeted only the
   founder and input mixed named UI actions with raw key checks.

## Persistence model: before and after

### Before

Schema v19 persisted semantic citizen data:

- stable identity and profile;
- standing work order and mutually exclusive commitment;
- `AtHome`, `AtWork`, or `InTransit`;
- travel start tick and return direction;
- vital/recovery/wound state;
- logical resource IDs and logical resource slot index.

It did **not** persist global/local coordinates, route waypoints, sprite state,
animation state, or node references. The logical resource position index is a
stable resource-unit slot, not a viewport coordinate.

### After

The persisted shape is unchanged. `CitizenRoutineSnapshot` is a derived,
Godot-free query over the restored facts and current world tick. It exposes:

- activity (`Working`, `Resting`, `TravellingToWork`, `TravellingHome`,
  `WaitingForStorage`, `WaitingForResources`, `WorkplaceIdle`, `Leisure`,
  `OffDuty`, `Recovering`, `OnExpedition`, or `Unavailable`);
- contextual location and building/Shelter references;
- logical transit origin and destination when applicable;
- activity start, expected arrival, and next scheduled transition;
- typed block reason;
- underlying behavior, logical location, and standing work order.

Because this projection is derivable, persisting it would duplicate state and
create migration drift. No data was deleted or migrated and schema remains v19.
The city currently has one `PrimaryHome`; per-citizen housing assignment should
be added only with a real multi-housing decision and a versioned migration.

## Load and offline flow

```text
read JSON → migrate to current schema → validate → restore CityWorld
→ compute elapsed UTC time → WorldTimeAdvance.Advance
→ resolve travel/routine/production/recovery with domain rules
→ create immutable UI snapshots → instantiate/reconcile visual carriers
```

The controller loads and catches up before later scene siblings initialize.
Offline travel may complete after the existing abstract 30-tick duration; live
travel still requires the macro route to reach its final anchor. Production,
stamina, experience, recovery, and stock do not call sprites, animation,
pathfinding, rendering, or `_Process` during catch-up.

When catch-up ends during a transit, the view derives elapsed fraction from
`TransitStartedAtTick`, plans the current map route, and advances an ephemeral
route cursor to the corresponding point. That calculated pixel position is
never written to the citizen or save.

## Visual reconstruction and routines

`BuildingVisualAnchors` derives entrance, exit, work, waiting, and nearby
leisure anchors from the building's current placement. Only the building ID is
durable. A map layout change therefore produces new presentation positions
without invalidating the save.

- `AtWork` citizens are hidden in macro and may be shown by real `CitizenId` in
  building detail.
- `AtHome` resting citizens reconstruct at the Shelter.
- citizens in leisure or a temporary workplace wait use bounded,
  obstacle-aware ambient routes near the Shelter; this never mutates logical
  location and is immediately interrupted by a domain transit.
- citizens waiting for storage retain their work order. Worker capacity alone
  controls whether another assignment is accepted.
- arrival rejection and travel logs now include activity, context, blocker,
  start, expected arrival, and next transition.

The provisional workday is now explicitly centralized as 00:00–16:00. This is
the behavior the former `DayTicks = 2400` already implemented, not a new design
decision. The read-only Policies panel labels it provisional.

## UI, saving, and camera

- Policies is a main macro action and reports work hours, current workday
  state, production policy behavior, off-duty behavior, and the boundary
  between automatic progress and player-authorized construction.
- The status HUD remains limited to time/speed, headline resources, active
  project state, and a short-lived save confirmation.
- Autosave cadence is centralized at three real minutes. Periodic and
  pause/close saves skip when no simulation change is pending; explicit
  high-value commands may still force an immediate atomic save.
- Saving remains synchronous and atomic (`.tmp` plus `.bak`). Current snapshots
  are small; asynchronous I/O remains measured future work if profiling shows a
  visible stall.
- Camera input is registered under named camera-only actions. WASD and arrows
  never move a citizen. Selection only changes the observation target; follow
  is activated explicitly by the button or F, and manual pan releases it.
- The Citizens roster is present again in the macro navigation. Debug builds
  expose the selected citizen's derived activity/context/blocker/timing without
  storing presentation state.

## Files changed by this stabilization

Domain and application:

- `GameClock.cs`, `CitizenRoutine.cs`, `CityWorld.cs`,
  `BuildingProductionSimulation.cs`, `OfflineProgression.cs`;
- `CityWorldController.cs`, `SimulationPersistencePolicy.cs`,
  `CitizenDebugSnapshot.cs`;
- `CityMacroSnapshot.cs`, `CityPolicySnapshot.cs`.

Presentation:

- `MacroStreetLiveView.cs`, `BuildingVisualAnchors.cs`,
  `CameraInputActions.cs`;
- `PoliciesPanel.cs`, `CityStatusPanel.cs`, `MigrantPanel.cs`,
  `CityPrototype.cs`, `CityPrototype.tscn`;
- English/Spanish localization catalogs and POT template.

Tests and documentation:

- `CitizenRoutineTests.cs`, `MacroStreetLiveViewTests.cs`,
  `UiSnapshotTests.cs`, `WorldEventLogTests.cs`,
  `OnboardingDomainTests.cs`;
- this audit plus the architecture/current-status/visual-regression handoff.

## Verification

- `dotnet build`: 0 errors, 0 warnings.
- `dotnet test`: 553/553 passing.
- Godot 4.7.1 headless boot: schema v19 load, offline catch-up, and clean scene
  startup with no false work route for a storage-blocked founder.
- Policies visual fixture captured at 1280×720 and 1920×1080. The first capture
  exposed clipped content; the panel now uses a bounded scroll body and deferred
  responsive sizing.

## Remaining debt

- Assigned active worlds still step canonical simulation ticks during long
  catch-up. This is correct and node-free but not the final event-boundary
  batching described by the design bible. Replace it subsystem by subsystem
  only with snapshot and causal-event equivalence tests.
- The 00:00–16:00 schedule and 30-tick abstract journey are provisional tuning.
- Ambient leisure currently anchors near the shared Shelter. Districts, points
  of interest, and per-citizen homes require real domain decisions before new
  persisted references are justified.
- Resource icons remain provisional graphic debt; this pass did not replace
  existing assets.
- Synchronous save duration should be instrumented before introducing a worker
  thread or a second snapshot-copy architecture.
