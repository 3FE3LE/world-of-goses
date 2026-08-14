---
name: core-game-vision
description: >
  Load the project-level vision, the nine gameplay pillars, and the RPG-city-builder-idle
  identity. Load only when the change alters at least one of: player decisions,
  gameplay meaning, information availability, system purpose, fantasy, progression,
  risk/reward, or player agency. Do NOT load for spacing, layout, borders,
  typography, icon replacement, focus bug, scroll behavior, visual regression
  fixture, equivalent component swap, pure presentation refactor, test-only
  change, mechanical rename, or comment cleanup.
license: World of Goses project license
compatibility: Documentation-only; references files under docs/systems/.
metadata:
  domain: vision
  layer: cross-cutting
  audience: every agent
---

# Core game vision

## Purpose

Anchor every change to the fantasy the game is selling: one persistent city,
RPG-citizen-driven, with expeditions as configured, automated, and causal
extensions of city life. Without this anchor, local improvements drift toward
a generic city builder or a colony simulator.

## When to use

Load this skill only when the change alters **at least one** of:

- **Player decisions** — new choice the player can make, or removal of
  an existing one.
- **Gameplay meaning** — what an action or system *means* in the world
  (e.g. "this building now produces hope instead of grain").
- **Information availability** — what the player can know vs must
  discover.
- **System purpose** — the reason a system exists.
- **Fantasy** — the world the game sells.
- **Progression** — what advances, in what order, under what rules.
- **Risk / reward** — what can be lost, what can be gained.
- **Player agency** — the player's capacity to act on the city.

Load also when two or more domains disagree about any of the above.

## When NOT to use

Do **not** load this skill for:

- spacing, layout, border, padding, alignment
- font size, typography, icon replacement
- focus bug, scroll behavior, hover state, click target
- visual regression fixture, snapshot baseline update
- equivalent component swap with no semantic change
- pure presentation refactor
- test-only change, mechanical rename, comment cleanup, typo
- new file or scene with no decision, agency, or meaning attached
- reading existing state (see `docs/ai/DOMAIN_CONSULTATION.md`)

If the change is purely cosmetic, structural, or infrastructural
and does not alter any of the eight load-when items above, do not
load this skill.

## Required documentation

- `docs/world/vision-and-pillars.md` — fantasy statement,
  principles 1-14, naming discipline, originality boundary.
- `docs/world/vision-and-pillars.md` — the nine
  pillars and the "causal production" rule.
- `docs/README.md` — authority hierarchy (vision wins over prototype, domain
  wins over visuals, player experience wins over exhaustive-but-empty sim).

## Conditional documentation

- `docs/systems/city-and-territory.md` — if the
  change affects what the city contains or how it grows.
- `docs/systems/citizens.md` —
  if the change touches citizens, professions, or hero state.
- `docs/systems/expeditions.md` — if the change adds or
  modifies expeditions.
- `docs/engineering/design-review.md` — for the core loop and north star when
  sequencing work.

## Core invariants

The full list lives in `docs/ai/CROSS_DOMAIN_INVARIANTS.md`. The
non-negotiables for this skill are:

- The city is the long-term protagonist. *(bible/01)*
- One game, one city. No meta-progression, no restart bonus. *(bible/01)*
- Citizens are the source of decisions, risk, and history. *(bible/01,
  bible/02)*
- A feature must enable a decision or communicate a consequence. Data that
  does neither must not be added. *(bible/10, guard-rails)*
- A mechanic is not implemented only because it is technically possible.
  *(docs/README.md, authority hierarchy)*
- The 14 design principles in bible/01 are **restrictions on future
  decisions, not aspirations**. A system that violates one must be
  redesigned, not approved.

## Expected workflow

1. Read the fantasy statement in bible/01. Restate it in one sentence.
2. Read the nine pillars in bible/02. Identify which pillars the task
   touches.
3. Read the relevant principles in bible/01.
4. If the change risks a principle, write the alternative design that does
   not.
5. Carry the fantasy and pillars into every subsequent skill you load.

## Files commonly involved

- `docs/systems/` — every chapter.
- `docs/engineering/design-review.md` — process.
- `docs/ai/CONTEXT_MAP.md` — routing.
- `docs/ai/CROSS_DOMAIN_INVARIANTS.md` — hard constraints.

## Tests to run

This skill does not own tests, but any change informed by it must keep
`dotnet test` green in `tests/WorldofGoses.Tests/`.

## Cross-domain consultation rules

- Always loaded alongside the domain skill that owns the change.
- When in doubt, escalate to `gameplay-integrator`.
- When the change touches **persistence, simulation, or architecture**,
  additionally load `technical-foundation`.

## Things not to do

- Do not treat principles as aspirations to compromise on.
- Do not introduce a new "global bonus", meta-progression layer, or restart
  advantage. These are explicitly forbidden by bible/01.
- Do not promote provisional names, including the project name itself, to
  shipping terminology. *(bible/01)*
- Do not introduce systems whose only justification is "we have the
  technology".

## Definition of done

- The change can be defended in one sentence against the fantasy statement.
- The change does not violate any principle in bible/01.
- The change reinforces at least one of the nine pillars in bible/02.
- If a principle is at risk, the alternative design is recorded in the
  handoff.