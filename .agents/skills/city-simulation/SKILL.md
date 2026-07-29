---
name: city-simulation
description: >
  Own buildings, construction, recipes, production, consumption, storage,
  inventory, upkeep, the production policy triplet, and the systemic
  pressures of a single persistent city. Required whenever a task touches a
  building, a recipe, the city inventory, the ledger, a stop cause, or the
  construction pipeline. Also load when a feature might convert the city
  into a generic builder with anonymous inhabitants.
license: World of Goses project license
compatibility: Documentation-only; references files under game/scripts/Domain/.
metadata:
  domain: city
  layer: domain
  audience: gameplay-integrator, citizens-rpg, expeditions-territory
---

# City simulation

## Purpose

Make the city a causal system whose production and growth emerge from real
conditions, real citizens, and visible blockages — not a flat-rate
generator. Resist the drift toward a traditional city builder where
buildings are timers and people are multipliers.

## When to use

- Adding or modifying a building, recipe, production step, or stop cause.
- Changing construction phases, contributor assignment, or pause/resume.
- Changing consumption, storage, upkeep, or the resource ledger.
- Reviewing whether a feature turns the city into a generic builder.

## Required documentation

- `docs/world-of-goses-design-bible/02_CORE_GAMEPLAY_PILLARS.md` — pillars
  1, 4, 5, 6, 7, 8 (city, causal production, territory, health, environment,
  delegation, organic difficulty).
- `docs/world-of-goses-design-bible/03_CITY_TERRITORY_AND_GROWTH.md`.
- `docs/ai/CROSS_DOMAIN_INVARIANTS.md` → "City" and "Expeditions" (for
  city-side consequences).

## Conditional documentation

- `docs/world-of-goses-design-bible/04_CITIZENS_PROFESSIONS_AND_HEROES.md`
  — when production requires workers.
- `docs/world-of-goses-design-bible/05_EXPEDITIONS.md` — when an expedition
  changes the city's state.
- `docs/FIRST_PLAYABLE_LOOP_AUDIT.md` gaps G1, G2, G6 — current city gaps.

## Core invariants

- A building does not produce merely by existing. Every output depends on
  accessible resource, workers, competence, tools, materials, energy,
  health, logistics, storage, policy, and risk. *(bible/02 pillar 4)*
- Blockers must surface as visible stop causes.
- Buildings are not unlocked by level alone. They require knowledge, plans,
  politics, materials, professionals, territory, infrastructure, and
  demand. *(bible/03)*
- Construction is collaborative, phased work. Its duration emerges from
  assigned citizens and their conditions, tools, and logistics — not a
  fixed countdown.
- Delegation executes the player's rules; it does not invent its own.
- Systemic pressure is causal, not an arbitrary drain.
- The environmental alignment axis is independent of lineage identity, and
  must not become binary morality. *(bible/02, bible/10)*

## Expected workflow

1. Identify the building, recipe, or system touched.
2. Read the relevant code in `game/scripts/Domain/` (`Building.cs`,
   `BuildingProductionCalculator.cs`, `BuildingProductionSimulation.cs`,
   `BuildingKind.cs`, `Recipes.cs`, `CityInventory.cs`,
   `CityResourceLedger.cs`, `CityEconomyRules.cs`, `Upkeep.cs`,
   `Construction*`, `ParcelGrid.cs`, `CityParcel.cs`).
3. Read the corresponding tests.
4. Identify every input required for the change to be causal and check that
   each input is reachable in the current code.
5. Add or expose stop causes for every missing input.
6. If persistence changes, coordinate with `technical-foundation`.
7. If a UI exposes state, snapshot the state; do not let the panel query
   the world directly.

## Files commonly involved

- Domain: `Building*.cs`, `Recipes.cs`, `Production*.cs`, `Upkeep.cs`,
  `CityInventory.cs`, `CityResourceLedger.cs`, `CityEconomyRules.cs`,
  `Construction*.cs`, `NaturalResourcePatch*.cs`, `ParcelGrid.cs`,
  `CityParcel.cs`, `PassageClass.cs`, `StaminaRules.cs`,
  `TerrainWearGrid.cs`.
- Presentation: `ConstructionPanel.cs`, `ProductionPanel.cs`,
  `BuildingDetailView.cs`, `MacroBuildingView.cs`,
  `BuildingDetailSnapshot.cs`, `ConstructionSnapshot.cs`,
  `CityStatusSnapshot.cs`.
- Tests: `tests/WorldofGoses.Tests/Building*Tests.cs`,
  `ProductionPolicyRangeTests.cs`, `ResidentFoodRationTests.cs`,
  `RecipesTests.cs`, `UpkeepTests.cs`, `MobilizationTests.cs`,
  `RestoreMobilizationTests.cs`, `Construction*Tests.cs`.

## Tests to run

- `dotnet test --filter "FullyQualifiedName~Building"`
- `dotnet test --filter "FullyQualifiedName~Construction"`
- `dotnet test --filter "FullyQualifiedName~Production"`
- `dotnet test --filter "FullyQualifiedName~Upkeep"`
- `dotnet test --filter "FullyQualifiedName~City"`
- `dotnet test --filter "FullyQualifiedName~DomainBoundary"`

## Cross-domain consultation rules

- `citizens-rpg` whenever a worker, contributor, or recovery is in scope.
- `expeditions-territory` whenever a parcel, route, or expedition outcome
  changes city state.
- `lineages-and-cultures` whenever the change could be misread as a
  lineage bonus.
- `narrative-lore` whenever a chronicle entry or event is needed.
- `technical-foundation` whenever persistence, schema version, or the
  domain/presentation boundary changes.

## Things not to do

- Do not introduce flat-rate production per building.
- Do not hide stop causes. If a stop cause exists, the UI shows it.
- Do not introduce a free-recruitment default. Recruitment must be
  constrained. *(G2)*
- Do not let upkeep drain silently. `Upkeep.ApplyUpkeep` is currently a
  no-op for a reason: any change to upkeep must be causal and visible.
- Do not use "level" as the only unlock criterion.
- Do not convert lineage into a production multiplier.

## Definition of done

- Every blocker surfaces as a visible stop cause.
- Production remains causal — every recipe has all its inputs reachable
  from the current code.
- Construction duration still emerges from assigned citizens and
  conditions, not a fixed timer.
- If the change introduces a new building or recipe, the canonical lore
  layer in `narrative-lore` is consulted for any diegetic framing.
- Tests cover the causal chain and stop causes.
- If persistence changed, schema version and migration handled by
  `technical-foundation`.