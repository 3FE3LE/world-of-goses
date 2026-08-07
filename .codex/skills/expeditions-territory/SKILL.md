---
name: expeditions-territory
description: >
  Own expeditions, encounters, retreat, return, parcels, territory, and the
  territorial state machine. Required whenever a task touches Expedition,
  ExpeditionPhase, ExpeditionStatus, CityParcel, ParcelGrid, or any feature
  that claims to be an expedition but is really a one-way timer that yields
  resources. Also load when a change affects what the city can discover,
  unlock, or build.
license: World of Goses project license
compatibility: Documentation-only; references files under game/scripts/Domain/.
metadata:
  domain: expeditions
  layer: domain
  audience: gameplay-integrator, citizens-rpg, city-simulation
---

# Expeditions and territory

## Purpose

Make expeditions a configured, automated, two-way extension of city life:
outbound, objective, return. They are not a minigame and not a timer that
converts resources. Make territory a state machine whose transitions are
caused by expeditions, not an arbitrary unlock list.

## When to use

- Modifying `Expedition`, `ExpeditionPhase`, `ExpeditionStatus`,
  `ExpeditionEncounterOutcome`, or `ExpeditionRequest`.
- Adding team selection, retreat posture, supply reservation, or loadout.
- Changing parcel state, parcel placement, or territorial unlock rules.
- Reviewing whether a feature accidentally rewards a one-way trip or an
  arbitrary resource conversion.

## Required documentation

- `docs/world-of-goses-design-bible/05_EXPEDITIONS.md` — the canonical
  chapter.
- `docs/world-of-goses-design-bible/03_CITY_TERRITORY_AND_GROWTH.md` — for
  parcel and territory.
- `docs/ai/CROSS_DOMAIN_INVARIANTS.md` → "Expeditions".

## Conditional documentation

- `docs/world-of-goses-design-bible/04_CITIZENS_PROFESSIONS_AND_HEROES.md`
  — for expedition eligibility and personal consequences.
- `docs/world-of-goses-design-bible/02_CORE_GAMEPLAY_PILLARS.md` pillar 2.
- `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §3, §8.4 and §13. The
  old G3/G4/G6/G7 gap IDs pointed at `docs/FIRST_PLAYABLE_LOOP_AUDIT.md`,
  discarded 2026-07-31.

## Core invariants

- Real citizens depart. Only citizens incorporated as heroes participate.
  *(bible/05)*
- The expedition includes the outbound leg and the return. It does not end
  on reaching the objective; it must return or trigger emergency return.
  *(bible/05)*
- The player prepares teams, supplies, formation, and priorities. The
  player does not manually control movement. *(bible/05)*
- An expedition must affect the city, its citizens, or the territory.
- Rewards cannot be limited to a timed conversion of resources.
- Survivors return without equipment and with their wounds. The city must
  treat them. *(bible/05, bible/02 pillar 6)*
- Encounters are deterministic and reproducible from persisted state.
- Retreat is a configured posture, not a generic failure state.

## Expected workflow

1. Read the relevant expedition code in `game/scripts/Domain/`.
2. Identify every phase the change touches: configuration, dispatch,
   outbound, encounter, objective, retreat, return, resolution.
3. For team and supply selection, route the change through the
   authoritative commitment model owned by `citizens-rpg` and the resource
   ledger owned by `city-simulation`.
4. For an encounter, define the deterministic seed source and prove it
   survives a save/load cycle.
5. For a parcel transition, name the precondition, the trigger, and the
   consequence in the city ledger or parcel state.
6. Persistence round-trip is mandatory; offline progression must apply the
   same rules. Coordinate with `technical-foundation`.
7. Add tests covering dispatch → travel → encounter → objective → return.

## Files commonly involved

- Domain: `Expedition.cs`, `ExpeditionId.cs`, `ExpeditionStatus.cs`,
  `ExpeditionPhase.cs`, `ExpeditionEncounterOutcome.cs`,
  `ExpeditionRequest.cs`, `CityParcel.cs`, `ParcelGrid.cs`,
  `ParcelId.cs`, `ParcelPlacement.cs`, `PassageClass.cs`,
  `CityResourceLedger.cs`, `NaturalResourcePatch*.cs`,
  `WorldEvent.cs`, `WorldEventLog.cs`.
- Presentation: `ExpeditionPanel.cs`, `MigrantPanel.cs`,
  `OfflineReportPanel.cs`.
- Tests: `tests/WorldofGoses.Tests/Expedition*Tests.cs`,
  `Parcel*Tests.cs`, `WorldEvent*Tests.cs`,
  `NaturalResourcePatchTests.cs`, `WorldPersistence*Tests.cs`,
  `OfflineProgressionTests.cs`.

## Tests to run

- `dotnet test --filter "FullyQualifiedName~Expedition"`
- `dotnet test --filter "FullyQualifiedName~Parcel"`
- `dotnet test --filter "FullyQualifiedName~WorldEvent"`
- `dotnet test --filter "FullyQualifiedName~OfflineProgression"`
- `dotnet test --filter "FullyQualifiedName~Persistence"`

## Cross-domain consultation rules

This skill has **mandatory** consultations:

- `citizens-rpg` when the change has personal effects (eligibility, team,
  injuries, return).
- `city-simulation` when the change has costs, rewards, or unlocks.
- `technical-foundation` when persistence, simulation, or offline
  progression is touched.

Additional:

- `narrative-lore` when a chronicle entry, dialogue, or lore is needed.
- `lineages-and-cultures` when a per-lineage encounter modifier is
  considered. Affinities may influence but must not be automatic
  multipliers.

## Things not to do

- Do not turn an expedition into a one-way timer that yields resources.
- Do not simulate the encounter second by second. Use discrete events.
- Do not let the player manually control movement. Configuration only.
- Do not bypass the resource ledger for supplies.
- Do not bypass the commitment model for team selection.
- Do not convert territorial unlocks into level-gates.

## Definition of done

- The change preserves the outbound → objective → return leg.
- Personal consequences persist and arrive with the survivors.
- Territory transitions have a named cause and a named consequence.
- Persistence and offline behavior share the same code path as live play.
- Tests cover the full phase chain.
- `citizens-rpg`, `city-simulation`, and `technical-foundation` reviewed.