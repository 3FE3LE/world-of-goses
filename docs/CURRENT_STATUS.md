# Current Project Status

> Practical handoff for the next development session. Read this after
> `GAME_VISION.md` and `PRODUCT_DIRECTION.md` to understand what is implemented,
> what has been verified, and where work should resume. Update this file when a
> vertical slice materially changes the playable state or next priority.

---

## 1. Last verified baseline

- Godot `.NET` 4.7.1, C# on `.NET 8.0`.
- `dotnet build` succeeds with 0 errors and 0 warnings.
- xUnit suite: 108 / 108 passing.
- Godot headless loads the main scene and the primary save slot without scene,
  signal, or C# errors.
- Latest completed direction-guide commit before this slice:
  `3034bef add product direction guide`.

## 2. Playable slice

The main scene presents one city with two selectable buildings:

- Quarry produces stone through mining competency.
- Farm produces food through farming competency.
- Both open the shared building-detail view.
- Citizens can be assigned and removed.
- Visible worker slots represent real citizen IDs and animate entry/exit.
- The city status strip reports both buildings and free citizens.
- Production rate responds deterministically to assigned workers and their
  relevant experience.

## 3. Production authorization and world time

Each building now owns a persistent production policy:

- Production can be authorized or paused independently.
- The player chooses a target stock from 0 through storage capacity.
- Production stops when paused, when no workers are assigned, or when the stock
  target is reached.
- The detail UI states the current stopping or authorization cause.
- Manual advancement remains available as a prototype inspection tool.

World advancement is shared:

- While the game is open, one world production tick runs each second.
- A world tick advances the clock once and processes every authorized building.
- Offline catch-up processes Quarry and Farm rather than only the first building.
- Live, manual, and offline paths respect each building's policy.
- Reaching a stock target stops further production and experience gain; no
  phantom experience or artificial production waste is recorded.

The one-second rate and seven-day offline cap are provisional tuning values,
not final product rules.

## 4. Persistence

- One primary local JSON slot is auto-loaded and auto-saved.
- DTOs remain separate from domain entities.
- Snapshots include schema version, last-seen UTC timestamp, citizens,
  buildings, assignments, competencies, roles, stock, and production policies.
- Writes use a temporary file and preserve the previous snapshot as `.bak`.
- Validation runs before restore and rejects structurally inconsistent worlds.
- Invalid or empty snapshots retain the seeded Quarry/Farm world and are
  replaced with a valid snapshot so the same recovery does not recur.
- Older snapshots without production-policy fields default each target to the
  building's storage capacity.

## 5. Known limitations

- Quarry and Farm produce independent resources; there is no cross-resource
  dependency or consumption yet.
- Offline reporting is aggregate. It does not yet provide per-building causal
  events or a chronological return report.
- The live timer and offline catch-up share policy semantics, but the offline
  calculation remains a batch calculator rather than a general event scheduler.
- Manual production is still visible even though automatic authorization now
  exists; its long-term role is undecided.
- Placeholder art is still in use.
- No construction, institutions, development dimensions, expeditions, health,
  relationships, lineage, or environmental alignment are playable yet.
- Visual interaction is manually/headless verified; there is no automated Godot
  UI test harness.

## 6. Recommended next slice

Introduce one understandable dependency between Quarry and Farm without
building a generic economy framework. The slice should:

1. Create a real trade-off between stone production, food production, labor,
   or a small approved construction.
2. Stop for an explicit domain reason when an input is unavailable.
3. Surface that reason in the city overview and building detail UI.
4. Behave consistently during live and offline advancement.
5. Produce the first per-building causal facts needed by a return report.

Before choosing exact costs or rules, compare the proposal with the alignment
checklist in `PRODUCT_DIRECTION.md`. UI/UX must reduce interaction cost through
overview, progressive disclosure, and persistent policies without removing the
systemic trade-off.

## 7. Verification commands

From `game/`:

```powershell
dotnet build
```

From `tests/WorldofGoses.Tests/`:

```powershell
dotnet test
```

Godot is installed locally at `C:\Tools\Godot\Godot_v4.7.1-stable_mono_win64.exe`
on the current development machine, but this machine-specific path must not be
treated as a repository requirement.
