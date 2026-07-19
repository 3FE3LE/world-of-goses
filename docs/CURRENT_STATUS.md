# Current Project Status

> Practical handoff for the next development session. Read this after
> `GAME_VISION.md` and `PRODUCT_DIRECTION.md` to understand what is implemented,
> what has been verified, and where work should resume. Update this file when a
> vertical slice materially changes the playable state or next priority.

---

## 1. Last verified baseline

- Godot `.NET` 4.7.1, C# on `.NET 8.0`.
- `dotnet build` succeeds with 0 errors and 0 warnings.
- xUnit suite: **189 / 189 passing**.
- Godot headless loads the main scene and the primary save slot without scene,
  signal, or C# errors.
- Latest completed slice covers three changes stacked on top of the persistent
  production policies slice: Stamina-gated production, Day/Night cycle +
  passive Upkeep + WellFed buff, and citizen Mobilisation with a Home building.

## 2. Playable slice

The main scene presents one city with three buildings:

- Quarry produces stone through mining competency.
- Farm produces food through farming competency.
- Home is the resting location — workers visually move here at night and
  return to their assigned production building at sunrise.
- Citizens can be assigned and removed from Quarry and Farm.
- Visible worker slots render based on each citizen's current physical
  location, not the static assignment, so the day/night cycle is visible
  without opening the detail view.
- The city status strip reports time-of-day, upkeep rate, per-building
  stock and staffing with the active stop cause, and the live split of
  citizens at work versus at home.

## 3. Stamina-gated production

Each citizen carries `CurrentStamina` (clamped 0–`MaxStamina`, default 100).
Every world tick, assigned workers on a producing building pay the
building's per-tick stamina cost, then eat food from the Farm-kind
buildings in deterministic order to refill. If every contributor to a
building runs out of stamina, the building sets
`ProductionStopCause.WorkersExhausted` and produces nothing that tick.
The Quarry and Farm are both stamina consumers in this slice. The
underlying numbers (`MaxStamina`, costs, regen) live in `StaminaRules.cs`
and are provisional tuning values, not product rules.

## 4. Day, Night, and Upkeep

The world clock ticks at 1 Hz (a prototype parameter). One in-game day
lasts 3600 ticks (one real hour). The day portion is the first
`DayTicks = 2400` (16 in-game hours) and the night portion is the
remaining `NightTicks = 1200` (8 in-game hours). `GameClock.IsDaytime`
and `GameClock.DayFraction` derive position from the world tick.

Day-time behaviour: assigned workers pay stamina, eat food, produce.
Night-time behaviour: no stamina cost, no production, food may still be
eaten, passive stamina regen keeps workers topped up. At sunrise the
day/night status changes, the night→day transition fires
`MobiliseForDay`, and the night→day transition fires `MobiliseForNight`.

Passive city upkeep consumes one stone per five citizens per tick
(`Upkeep.StonePerTick`, rounded up, floor of one) from the Quarry-kind
buildings in deterministic insertion order. The drain runs 24/7 and
is the placeholder for future building-driven demand.

## 5. WellFed buff

Eating resets each citizen's `WellFedRemainingTicks` to
`StaminaRules.WellFedBuffDuration` (100 ticks). The buff decrements
by one each world tick. While the buff is positive, stamina regen gains
`StaminaRules.WellFedRegenBonus` (one extra per tick). The buff exists
so food scarcity degrades regen gradually instead of cutting it off
abruptly, and so a future food-quality slice can scale the bonus
without restructuring the formula. The buff is a citizen-level
property, so per-citizen variation is already isolated in
`Citizen.WellFedRemainingTicks`.

## 6. Mobilisation and the Home building

`Citizen.CurrentAssignment` is the worker's job (static). A new
`Citizen.CurrentLocation` is the worker's physical location right now
(`AtWork` or `AtHome`). At sunset every citizen moves to
`AtHome`; at sunrise, assigned citizens return to `AtWork` while the
unassigned stay home. The transition fires once per world tick from
`AdvanceWorldTick` when `IsDaytime` changes between the previous and
the current tick.

A new `BuildingKind.Home` was added to the seed. The Home has
worker capacity equal to the seeded population (5), no production,
and no upkeep consumption. Its slots render every citizen whose
`CurrentLocation == AtHome`. `CityWorld.GetCurrentlyVisibleOccupants`
encapsulates the per-building occupant logic so production buildings
show only workers with `AtWork` while Home shows everyone with
`AtHome`. `CityWorld.Restore` runs `MobiliseForDay` or `MobiliseForNight`
based on `IsDaytime(_tick)` so the visible state matches the loaded
clock from the first frame after a save load.

## 7. Persistence

- One primary local JSON slot is auto-loaded and auto-saved.
- DTOs remain separate from domain entities.
- Snapshots include schema version, last-seen UTC timestamp, citizens,
  buildings, assignments, competencies, roles, stock, production
  policies, stamina (`StaminaCurrent`/`StaminaMax`), and the WellFed buff
  counter (`WellFedRemainingTicks`).
- Writes use a temporary file and preserve the previous snapshot as `.bak`.
- Validation runs before restore and rejects structurally inconsistent worlds.
- Invalid or empty snapshots retain the seeded Quarry/Farm/Home world and
  are replaced with a valid snapshot so the same recovery does not recur.
- Older snapshots without stamina fields default each citizen to full
  stamina; older snapshots without the buff field default to no buff.

## 8. Controller-level event for live UI

`CityWorldController` exposes a `WorldTickAdvanced(int tick)` signal
that fires after every `AdvanceWorldTick`. `CityMacroView` subscribes
and refreshes the city status strip every tick so the time-of-day and
the at-work / at-home split stay in sync with the simulation even
when no production change fires `BuildingStateChanged`.

## 9. Known limitations

- Quarry and Farm produce independent resources; cross-resource
  consumption exists only through the WellFed buff mechanic on stamina.
- Upkeep is a single city-wide stone drain; there is no building-driven
  demand yet (Smithy, depot maintenance, etc.).
- Offline reporting is aggregate. It does not yet provide per-building
  causal events or a chronological return report.
- The day/night split is provisional: day starts at tick 0 of each
  in-game day. Moving the sunrise to hour 8 is a one-line offset change.
- Citizens cannot yet be assigned explicitly to a Home; mobilisation
  is automatic and follows the world clock.
- The Home has no production and no special effects (no rest-quality
  multiplier on regen, no healing). A future slice will add
  capacity-aware housing and per-Home bonuses.
- Placeholder art is still in use.
- No construction, institutions, development dimensions, expeditions,
  health, relationships, lineage, or environmental alignment are
  playable yet.
- Visual interaction is manually/headless verified; there is no
  automated Godot UI test harness.

## 10. Recommended next slice

The next slice should give the player a meaningful choice about what the
city consumes and produces, beyond the placeholder stone upkeep. Two
open seams are ready for that:

1. Replace the abstract upkeep with a building-driven demand (e.g.
   a Smithy that consumes stone to produce tools, which boost
   production). The seam is `Upkeep.StonePerTick` plus the per-building
   cost table in `StaminaRules.cs`.
2. Move day/night so sunrise starts at hour 8 instead of hour 0.
   One constant change plus an offset in `GameClock.IsDaytime`.

Before choosing, compare the proposal with the alignment checklist in
`PRODUCT_DIRECTION.md` (UI/UX must reduce interaction cost through
overview, progressive disclosure, and persistent policies without
removing the systemic trade-off).

## 11. Verification commands

From `C:\dev\world-of-goses`:

```powershell
# Build the C# project (must be run from game/ for Godot's project layout)
cd game
dotnet build

# Domain and persistence tests
cd ../tests/WorldofGoses.Tests
dotnet test

# Godot headless (machine-specific path, not a repo requirement)
C:\Tools\Godot\Godot_v4.7.1-stable_mono_win64.exe --headless --quit-after 3 res://scenes/CityPrototype.tscn
```

There is no linter or CI configured yet. Do not invent commands. Do not
install global tools.
