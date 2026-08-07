---
name: vertical-slice-validation
description: >
  Cross-cutting validation skill for the current vertical slice. Use to
  classify work into the active slice, identify what is still placeholder,
  apply the slice acceptance criteria, and avoid scope creep toward a
  generic city builder. Required for any task that names a vertical slice
  in its title or that risks moving the game off-slice.
license: World of Goses project license
compatibility: Documentation-only; references the design bible, PRODUCT_DIRECTION.md, and the audit / current-status docs.
metadata:
  domain: validation
  layer: cross-cutting
  audience: gameplay-integrator, quality-guardian
---

# Vertical slice validation

## Purpose

Keep the active vertical slice honest. Make sure each task either advances
the slice, stabilizes it, or is explicitly deferred — and never widens the
slice without a documented decision.

## When to use

- A task is filed against a named increment (EG-N) or an older vertical
  slice (VS-N).
- A task risks turning the slice into a generic city builder.
- A task closes one of the gaps the proposal diagnoses.
- A reviewer (`quality-guardian`) needs to check slice acceptance.

> **`docs/FIRST_PLAYABLE_LOOP_AUDIT.md` no longer exists.** It was discarded on
> 2026-07-31 together with the VS-5 audit and its seventeen criteria, and its
> G0–G7 gap table went with it. The acceptance criteria now live in
> `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` — §15 for the order of
> increments, §17 for the acceptance test. The VS-2, VS-3, VS-4 and VS-0 test
> suites still pass and are kept as safety regressions, but they are no longer
> acceptance criteria.

## Required documentation

- `docs/world-of-goses-design-bible/01_GAME_VISION.md`.
- `docs/world-of-goses-design-bible/02_CORE_GAMEPLAY_PILLARS.md`.
- `docs/PRODUCT_DIRECTION.md`.
- `docs/CURRENT_STATUS.md`.
- `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §15 and §17.
- `docs/VALIDATION.md`.
- `docs/ai/CURRENT_DEVELOPMENT_STATE.md`.

## Conditional documentation

- `docs/world-of-goses-design-bible/05_EXPEDITIONS.md` — for the current
  VS-2 (minimal expedition).
- `docs/world-of-goses-design-bible/03_CITY_TERRITORY_AND_GROWTH.md` — for
  VS-3 (consequences and territory).
- `docs/PERFORMANCE_BUDGETS.md` — when frame time is part of the
  acceptance criteria.
- `docs/VISUAL_REGRESSION.md` — when visual state is part of the
  acceptance criteria.

## Core invariants

This skill re-states the cross-cutting invariants. They are not new rules;
they are the lens through which any slice change is judged.

- The city is the long-term protagonist.
- One game, one city. No meta-progression, no restart bonus.
- Citizens are the source of decisions, risk, and history.
- A feature must enable a decision or communicate a consequence.
- A mechanic is not implemented only because it is technically possible.
- Do not convert the game into a traditional city builder with anonymous
  inhabitants, and do not convert it into a classic colony simulator.
- The domain does not depend on Godot.
- A building does not produce merely by existing.
- An expedition includes the return leg.
- Lineages are not classes or professions.

## Expected workflow

1. Read the active increment in `docs/CURRENT_STATUS.md` and its place in
   `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §15.
2. Classify the task against the slice: is it advancing, stabilizing, or
   out of scope?
3. If advancing, name the increment it serves and the gap from the
   proposal's §3 diagnosis it closes.
4. If stabilizing, name the regression or bug it prevents.
5. If out of scope, record the deferral in the handoff and link to the
   `OUT OF SCOPE` list in `docs/CURRENT_STATUS.md`.
6. Apply the slice acceptance criteria, including persistence round-trip
   and offline equivalence.
7. Update `docs/CURRENT_STATUS.md` and `docs/ai/CURRENT_DEVELOPMENT_STATE.md`
   when the slice advances.

## Files commonly involved

- `docs/CURRENT_STATUS.md`.
- `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`.
- `docs/VALIDATION.md`.
- `docs/ai/CURRENT_DEVELOPMENT_STATE.md`.
- `tests/WorldofGoses.Tests/FirstRunRegressionTests.cs`.

## Tests to run

- `dotnet test --filter "FullyQualifiedName~FirstRunRegression"`
- `dotnet test` (full suite) before any claim of "slice advances".
- For an expedition change:
  `dotnet test --filter "FullyQualifiedName~Expedition"`
- For a recovery change:
  `dotnet test --filter "FullyQualifiedName~Stamina"`

## Cross-domain consultation rules

This skill does not own a domain. It consults the relevant domain skill
and the reviewer.

- Always: `core-game-vision`.
- Always: the skill of the domain the slice change touches.
- For review: `quality-guardian`.

## Things not to do

- Do not widen the slice without a documented decision.
- Do not introduce systems that the audit already lists as deferred.
- Do not implement the second half of the first playable loop in a single
  task; advance one gap at a time and keep tests green between tasks.
- Do not claim a slice advances without `dotnet test` and the
  `FirstRunRegressionTests` green.
- Do not introduce generic city-builder assumptions (level-gated
  unlocks, anonymous inhabitants, flat-rate production).

## Definition of done

- The active slice is named and the gap is named.
- The acceptance criteria from the slice doc are met.
- `dotnet build` is clean.
- `dotnet test` is green, including `FirstRunRegressionTests`.
- `docs/CURRENT_STATUS.md` and `docs/ai/CURRENT_DEVELOPMENT_STATE.md` are
  updated.
- `quality-guardian` has reviewed.