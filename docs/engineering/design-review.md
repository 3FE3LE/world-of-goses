# Design review guide

> How a change is judged before it ships. Product canon (fantasy, pillars,
> lineages) lives under [`../world/`](../world/); system canon under
> [`../systems/`](../systems/); the code's boundaries in
> [`architecture.md`](architecture.md). This file owns the questions.

## North star

The player configures policies and priorities for one persistent city.
The world executes only what the player has authorized, produces
understandable causal consequences, and presents each return as an
opportunity to make new decisions.

The desired fantasy is not direct control over every action. It is
shaping a society that remains legible and trustworthy while operating
without constant player attention.

The full fantasy statement is in the bible (§01).

---

## The loop every feature serves

The project should progressively prove this complete loop:

1. The player observes needs, resources, people, and constraints.
2. The player authorizes an order, policy, assignment, or priority.
3. Time advances with the game open or closed.
4. The city executes the authorization without inventing sovereign
   decisions.
5. Resources, experience, risks, and opportunities change for explicit
   causes.
6. The game explains what changed, why, and what now needs attention.
7. The player chooses among multiple valid responses.

A feature that cannot yet support the whole loop may still be useful,
but its place in this loop must be stated before implementation.

---

## UI/UX is a core system constraint

The intended simulation can become deep. Its interface must therefore
receive the same design effort as the domain model. Complexity may
live in the system; confusion, repetitive work, and hidden causality
must not live in the player's interaction with it.

The governing rule is:

> Simplify operation and comprehension, not the underlying systemic depth.

Good UI/UX should let a player engage at several levels without
creating a separate "simple mode" that removes meaningful rules:

- **Overview first.** Show what is healthy, blocked, changing, or
  awaiting a decision before exposing every parameter.
- **Progressive disclosure.** Move from city summary to system,
  building, citizen, and causal detail without losing context.
- **Explain every consequence.** Values and states should expose their
  source, dependencies, recent changes, and stopping conditions.
- **Policies over repetitive clicks.** Repeated intent belongs in
  persistent configuration, sensible defaults, templates, ranges,
  priorities, and batch operations — not in click-heavy maintenance.
- **Safe experimentation.** Preview costs and expected effects, clearly
  mark irreversible decisions, and make reversible configuration easy
  to revise.
- **Attention management.** Rank information by urgency and player
  authority. Do not use alerts for events that require no decision.
- **Preserve alternatives.** Convenience features must not silently
  choose the single "correct" strategy or conceal meaningful trade-offs.
- **Consistent navigation.** Screen hierarchy, back behavior, selection
  state, keyboard/gamepad focus, and mouse interaction must remain
  predictable.
- **Responsive presentation.** Use anchors and containers, support
  relevant aspect ratios, and avoid layouts that only work at one
  window size.
- **Event-driven updates.** Presentation reacts to domain events; it
  does not create state or poll every frame without a concrete need.

UI acceptance is part of feature acceptance. A system is not complete
merely because its domain logic works: the player must be able to
discover, configure, understand, and revisit it with low interaction
cost.

The visual direction, three scales, typography hierarchy, and
per-lineage UI tokens live in the bible:
[`../presentation/visual-language.md`](../presentation/visual-language.md).

---

## Technical direction

These rules govern **how** code is added; the stack itself lives in
[`architecture.md`](architecture.md).

- Keep the simulation deterministic and independent of Godot. The
  boundary rule and folder layout are in
  [`architecture.md`](architecture.md) §0–§5.
- Use the same domain rules for live advancement and offline catch-up.
  The persistence approach is in
  [`architecture.md`](architecture.md) §8.
- Simulate the whole city, not only the currently selected building.
- Treat commands as player authorization and events as resulting facts.
- Preserve causal information long enough to produce useful reports
  and history.
- Keep persistence versioned and validate snapshots before mutating
  live state.
- Add abstractions only when a current vertical slice needs them.
- Keep Godot responsible for representation, navigation, animation,
  audio, and interaction feedback — not business rules.

Parameters such as real-time scale, offline limits, and final
progression pace remain provisional until play validates them.
Temporary implementation values must not silently become product rules.

---

## Warning signs of drift

Reconsider a proposed change when it:

- Adds types, DTOs, screens, or frameworks without enabling a player
  decision.
- Adds several independent counters without a causal relationship
  between them.
- Requires frequent clicking to restate an intent the player already
  expressed.
- Hides complexity by removing meaningful constraints or choices.
- Produces consequences the interface cannot explain.
- Advances only the visible or selected system while the rest of the
  city waits.
- Introduces arbitrary levels, random rewards, or unexplained failure.
- Builds a large future-facing skeleton instead of a small complete
  loop.
- Expands content faster than the interface can organize and
  communicate it.

---

## Review checklist

Use these questions when reviewing the project or planning a slice:

1. What new decision can the player make?
2. What persistent authorization can the city execute without repeated
   input?
3. What causal consequence can occur while the player is absent?
4. How will the player understand what changed and why?
5. Does the feature create more than one valid response or development
   path?
6. Does UI/UX reduce interaction cost without deleting systemic depth?
7. Can the player reach details from an overview and return without
   losing context?
8. Do live and offline advancement use the same domain behavior?
9. Does the change preserve the single-city, no-meta-progression
   constraint?
10. Is this the smallest complete slice that can validate the idea?

If the answers are weak, refine the slice before expanding its
implementation.

---

