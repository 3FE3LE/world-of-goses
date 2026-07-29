---
name: technical-foundation
description: >
  Own the technical architecture, the Godot/.NET boundary, C# and Godot
  conventions, persistence, schema versioning, migrations, determinism,
  offline progression, performance, and tests. Required whenever a task
  touches persistence, schema version, the domain/presentation boundary,
  simulation determinism, or any file under game/scripts/Domain/. Also load
  when the change affects build, CI, or test infrastructure.
license: World of Goses project license
compatibility: Documentation-only; references the design bible, ARCHITECTURE.md, and C# / Godot 4.7 conventions.
metadata:
  domain: technical
  layer: cross-cutting
  audience: every agent
---

# Technical foundation

## Purpose

Keep the architecture honest: domain independent of Godot, persistence
versioned, offline progression equivalent to live play, simulation
deterministic, and every invariant covered by a test.

## When to use

- Adding or modifying anything under `game/scripts/Domain/`.
- Adding or modifying a persistence DTO or migration.
- Changing how offline catch-up works.
- Changing the domain/presentation boundary or the layer split.
- Touching build, CI, or test infrastructure.

## Required documentation

- `docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md`.
- `docs/ARCHITECTURE.md`.
- `docs/REPOSITORY_CONVENTIONS.md` (C# and Godot conventions).
- `docs/ai/CROSS_DOMAIN_INVARIANTS.md` → "Architecture".

## Conditional documentation

- `docs/PRODUCT_DIRECTION.md` — engineering rules.
- `docs/PERFORMANCE_BUDGETS.md` — when the change affects frame time or
  allocations.
- `docs/VISUAL_REGRESSION.md` — when the change affects the visual
  regression matrix.
- `docs/VALIDATION.md` — for the cross-check against the design bible.

## Core invariants

- The domain does not depend on Godot. *(bible/10)* — verified by
  `DomainBoundaryTests`.
- Scenes do not decide rules. Input handling is presentation;
  decision-making is domain.
- No per-citizen `_Process`. No per-citizen node. *(bible/10)*
- Offline progression must use the same domain rules as live advancement,
  not simulate second by second.
- Saves are versioned (`WorldSave.CurrentVersion`); migrations are
  explicit; snapshots are validated before mutating live state.
- Commands are player authorization; events are the resulting facts.
  Causal information is preserved long enough for reports.
- No mutable global state, no premature event bus, no pathfinding for
  invisible population. *(bible/10)*
- Pixel-perfect: integer scale, nearest filter, integer positions.

## Expected workflow

1. Read the relevant chapter in the design bible and the architecture doc.
2. Read `docs/REPOSITORY_CONVENTIONS.md` for naming and style rules.
3. For any persistence change, run the migration plan by the persistence
   review: name the new DTO fields, the schema version bump, the
   migration code, the round-trip test.
4. For any domain change, run `DomainBoundaryTests` in your head: does the
   new file import `Godot`? does it reference `res://`? does it touch a
   node? If yes, redesign.
5. For any offline change, prove equivalence by feeding the same state
   through live advancement and through `OfflineProgression.Apply`.
6. Add tests:
   - unit tests for new domain logic,
   - persistence round-trip tests for new fields,
   - regression tests for migrations,
   - `DomainBoundaryTests` if the layer split changed.

## Files commonly involved

- Domain: everything under `game/scripts/Domain/`.
- Persistence: `game/scripts/Domain/Persistence/`, including
  `WorldSave.cs` (`CurrentVersion`), `WorldPersistence.cs`,
  `IncompatibleSaveVersionException.cs`, all `*Save.cs` DTOs.
- Time: `game/scripts/Domain/OfflineProgression.cs`,
  `OfflineProgressionReport.cs`, `WorldTimeAdvance.cs`, `GameClock.cs`.
- Boundary tests: `tests/WorldofGoses.Tests/DomainBoundaryTests.cs`.
- Project: `game/World of Goses.csproj`, `game/World of Goses.sln`,
  `game/project.godot`, `tests/WorldofGoses.Tests/WorldofGoses.Tests.csproj`.

## Tests to run

- `cd tests/WorldofGoses.Tests; dotnet test`
- `cd game; dotnet build`

For any persistence change, additionally:

- `dotnet test --filter "FullyQualifiedName~Persistence"`
- `dotnet test --filter "FullyQualifiedName~WorldEventPersistence"`

## Cross-domain consultation rules

- `citizens-rpg` whenever personal state changes.
- `city-simulation` whenever city state changes.
- `expeditions-territory` whenever expedition state changes.
- `narrative-lore` whenever event retention or chronicle changes.
- `presentation-experience` whenever snapshots change.

## Things not to do

- Do not introduce `using Godot` under `game/scripts/Domain/`.
- Do not reference `res://` paths from the domain.
- Do not introduce mutable global state.
- Do not introduce a premature event bus or mediator pattern without a
  concrete current need.
- Do not simulate offline progression second by second.
- Do not store derived state that can be computed from persisted state.
- Do not add a NuGet dependency without a concrete current need.
- Do not promote a provisional name to a permanent identifier.

## Definition of done

- `dotnet build` from `game/` is clean.
- `dotnet test` from `tests/WorldofGoses.Tests/` is green.
- `DomainBoundaryTests` is green.
- For a persistence change, a real saved file was loaded successfully, or a
  fixture covering the migration is committed.
- For an offline change, equivalence with live advancement is demonstrated
  by a test or by a deterministic scenario.
- For a layer-split change, the boundary test list is updated and green.
- The change is covered by tests before it is reviewed.