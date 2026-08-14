---
name: vertical-slice-validation
description: >
  Cross-cutting validation skill. Use to check that a change advances the
  game the repository is actually building, stays inside the scope its
  GitHub issue claims, and does not drift toward a generic city builder.
  Required for any task that risks moving the game off-identity or that
  closes a scoped increment.
license: World of Goses project license
compatibility: Documentation-only; references the product canon under docs/world/ and docs/systems/.
metadata:
  domain: validation
  layer: cross-cutting
  audience: gameplay-integrator, quality-guardian
---

# Slice validation

## Purpose

Keep a change honest about its own scope. Every task either advances a system,
stabilises one, or is out of scope — and none of the three may widen the game
without an explicit decision.

## When to use

- A task claims to close or advance a GitHub issue.
- A task risks turning the game into a generic city builder.
- A reviewer (`quality-guardian`) needs to check acceptance.

**Scope lives in the issue, not in a document.** `gh issue view <n>` is the
statement of what the work is for. Canon under `docs/` says what the game is;
it never carries a queue.

## Required documentation

- `docs/world/vision-and-pillars.md` — fantasy, nine pillars, guardrails.
- `docs/engineering/design-review.md` — the review checklist and drift signs.
- `docs/ai/CROSS_DOMAIN_INVARIANTS.md` — the hard constraints.

## Conditional documentation

- `docs/systems/expeditions.md` — expedition or combat scope.
- `docs/systems/city-and-territory.md` — city, territory or construction scope.
- `docs/engineering/performance.md` — when frame time is part of acceptance.
- `docs/engineering/visual-regression.md` — when visual state is part of
  acceptance.

## Core invariants

Not new rules — the lens through which any change is judged.

- The city is the long-term protagonist.
- One game, one city. No meta-progression, no restart bonus.
- Citizens are the source of decisions, risk, and history.
- A feature must enable a decision or communicate a consequence.
- A mechanic is not implemented only because it is technically possible.
- Not a traditional city builder with anonymous inhabitants, and not a
  classic colony simulator.
- The domain does not depend on Godot.
- A building does not produce merely by existing.
- An expedition includes the return leg.
- Lineages are not classes or professions.

## Expected workflow

1. Read the issue. Name what it claims to change.
2. Classify the task: advancing a system, stabilising one, or out of scope.
3. If advancing, name the system document whose invariants the change touches
   and check them one by one.
4. If stabilising, name the regression or defect it prevents.
5. If out of scope, say so in the handoff and leave the work to its issue.
6. Apply acceptance: persistence round-trip and live/offline equivalence
   whenever state was added.
7. Update the system document **only** if an invariant or contract moved.

## Files commonly involved

- The system document the change touches under `docs/systems/`.
- `tests/WorldofGoses.Tests/FirstRunRegressionTests.cs`.

## Tests to run

- `dotnet test --filter "FullyQualifiedName~FirstRunRegression"`
- `dotnet test` (full suite) before any claim that a system advanced.
- For an expedition change:
  `dotnet test --filter "FullyQualifiedName~Expedition"`
- For a recovery change:
  `dotnet test --filter "FullyQualifiedName~Stamina"`

## Cross-domain consultation rules

This skill owns no domain. It consults:

- Always: `core-game-vision`.
- Always: the skill of the domain the change touches.
- For review: `quality-guardian`.

## Things not to do

- Do not widen scope without a documented decision.
- Do not record progress, status or next steps in a document.
- Do not implement several systems in one task; advance one and keep tests
  green between tasks.
- Do not claim a system advanced without `dotnet test` and
  `FirstRunRegressionTests` green.
- Do not introduce generic city-builder assumptions (level-gated unlocks,
  anonymous inhabitants, flat-rate production).

## Definition of done

- The issue's scope is met and named.
- `dotnet build` is clean.
- `dotnet test` is green, including `FirstRunRegressionTests`.
- Canon was updated only where a contract moved.
- `quality-guardian` has reviewed.
