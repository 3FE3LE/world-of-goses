---
name: citizens-rpg
description: >
  Own everything about Citizen — the single personal entity. Use for citizen
  state, identity, commitments, availability, competencies, roles, hero
  status, injuries, recovery, trajectories, recognition, relationships, and
  personal history. Required whenever a task touches Citizen or any class
  that defines personal state. Also load when a feature would create a
  parallel person type or push citizens toward interchangeable numbers.
license: World of Goses project license
compatibility: Documentation-only; references files under game/scripts/Domain/.
metadata:
  domain: citizens
  layer: domain
  audience: gameplay-integrator, city-simulation, expeditions-territory
---

# Citizens RPG

## Purpose

Keep the single `Citizen` entity the only personal actor in the world, so
that hero, miner, medic, adventurer, and artisan are accumulations on one
person instead of parallel person types. Make commitments exclusive and
visible. Make consequences persist.

## When to use

- Creating, modifying, deleting, or serializing any `Citizen` field.
- Adding or changing a commitment, assignment, or availability rule.
- Designing injuries, recovery, or any health model.
- Touching role, hero state, competency, aptitude, or profile code.
- Reviewing whether a feature accidentally treats citizens as anonymous
  population.

## Required documentation

- `docs/systems/citizens.md`
  — the canonical chapter.
- `docs/world/vision-and-pillars.md` — principles 6, 7, 8
  (no instant healing; people are not numbers).
- `docs/ai/CROSS_DOMAIN_INVARIANTS.md` → "Citizens".

## Conditional documentation

- `docs/systems/expeditions.md` — when a citizen can
  depart on an expedition.
- `docs/world/vision-and-pillars.md` pillar 6
  — when designing injury and recovery.
- `docs/world/lineages.md` — when lineage affects
  the change. Read `lineages-and-cultures` for cross-cutting rules.
- `docs/systems/kovari-cube.md` — when the
  founder profile, the cube profile, the elemental affinity, the
  equipment model or combat-derived stats are touched. Read
  `lineages-and-cultures` for the cube-as-cultural-system angle.

## Core invariants

- One entity, `Citizen`. No parallel person types. *(bible/04)*
- Professions, roles, and heroism are accumulative state on the citizen.
  *(bible/04)*
- A citizen cannot hold incompatible commitments simultaneously. Work,
  construction, expedition, rest, recovery are mutually exclusive. *(G0)*
- Persistent injuries are not depleted stamina. The separation between
  wounds and stamina is **Proposed, not Accepted** — see
  `docs/history/decisions.md` → DEC-0011. Do not assume a separate wound
  subsystem exists; read the current `StaminaRules` first.
- The visual representation is not the persistent entity. `Citizen` is data;
  `CitizenView` is a temporary visual. No per-citizen `_Process`, no
  per-citizen node. *(bible/04, bible/10)*
- The founder is a Citizen who can die, retire, become myth, be questioned,
  or lose relevance. *(bible/07)*

## Expected workflow

1. Identify the citizen field or behavior the change touches.
2. Read `Citizen.cs`, `CitizenCommitment.cs`, `CitizenAvailabilityReason.cs`,
   `CitizenVitalStatus.cs`, `Role.cs`, `CompetencyEntry.cs`,
   `CitizenAssignmentService.cs`.
3. Read the relevant tests in
   `tests/WorldofGoses.Tests/Citizen*Tests.cs`.
4. Decide whether the change requires a new commitment, a new competency, a
   new role, or a new field on `Citizen`.
5. If the change is a commitment or availability rule, prove exclusivity
   with tests that cover work, construction, expedition, rest, recovery.
6. If persistence changes, raise the schema version and provide a migration.
7. If presentation changes, snapshot the relevant state; do not query nodes.

## Files commonly involved

- Domain: `game/scripts/Domain/Citizen.cs`, `CitizenId.cs`,
  `CitizenProfile.cs`, `CitizenCommitment.cs`, `CitizenCommitmentKind.cs`,
  `CitizenAvailabilityReason.cs`, `CitizenVitalStatus.cs`,
  `CitizenAssignmentService.cs`, `CitizenNeedsRules.cs`,
  `CitizenRoutine.cs`, `CompetencyEntry.cs`, `CompetencyId.cs`, `Role.cs`,
  `RoleId.cs`.
- Presentation snapshots: `CitizenProfile.cs` (if used as snapshot),
  `CityStatusSnapshot.cs`, `BuildingDetailSnapshot.cs`,
  `CityMacroSnapshot.cs`.
- Tests: `tests/WorldofGoses.Tests/Citizen*.cs`,
  `CityWorldStaminaTests.cs`, `DomainBoundaryTests.cs`.

## Tests to run

- `dotnet test --filter "FullyQualifiedName~Citizen"`
- `dotnet test --filter "FullyQualifiedName~Stamina"`
- `dotnet test --filter "FullyQualifiedName~DomainBoundary"` — must stay
  green.
- For a new commitment or availability rule, add a regression test that
  fails when two commitments overlap.

## Cross-domain consultation rules

- `city-simulation` when the change affects housing, production, or labor
  supply.
- `expeditions-territory` when the change affects expedition eligibility,
  team selection, or return consequences.
- `lineages-and-cultures` whenever lineage is touched. Lineages do not
  block professions and must not become automatic multipliers.
- `narrative-lore` whenever a new dialogue, event, or chronicle entry is
  tied to the citizen.
- `technical-foundation` when persistence, schema version, or
  presentation/domain boundary changes.

## Things not to do

- Do not create a separate `Hero`, `Miner`, `Medic`, `Adventurer`, or
  `Artisan` entity. *(bible/04)*
- Do not treat citizens as anonymous population in any UI, formula, or
  panel.
- Do not let stamina deplete and call it "injury". Healing is not stamina
  recovery. *(bible/02 pillar 6)*
- Do not add per-citizen `_Process` or per-citizen node bookkeeping.
  *(bible/10)*
- Do not couple commitment state to the visual representation.

## Definition of done

- The change preserves the single `Citizen` entity and its accumulations.
- Commitments remain mutually exclusive and serialized correctly.
- If a wound model is introduced, the relationship to stamina is recorded
  in `docs/history/decisions.md` and cited in the handoff.
- Tests cover exclusivity and persistence round-trip.
- Documentation updated if a rule, decision, or current state changed.