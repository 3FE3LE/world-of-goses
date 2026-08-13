# Product Direction and Alignment Guide

> A living decision guide for **how to validate, ship, and review** the
> prototype. The fantasy, principles, pillars, and lineages are owned
> by the design bible; this file owns the loop, the sequence, the UI
> rules, the drift checklist, and the alignment questions the team
> asks before cutting a slice.

## What lives in the bible

- Fantasy, single-city rule, absence rule, 14 design principles,
  originality boundary, naming discipline:
  [`docs/world-of-goses-design-bible/01_GAME_VISION.md`](world-of-goses-design-bible/01_GAME_VISION.md).
- Nine gameplay pillars (city dev, expeditions, citizens, causal
  production, territory, health, environment, delegation, organic
  difficulty):
  [`docs/world-of-goses-design-bible/02_CORE_GAMEPLAY_PILLARS.md`](world-of-goses-design-bible/02_CORE_GAMEPLAY_PILLARS.md).
- Eight lineages (idea, nature, culture, affinities, tensions,
  architecture):
  [`docs/world-of-goses-design-bible/06_LINEAGES.md`](world-of-goses-design-bible/06_LINEAGES.md).
- Open design questions (cosmology, time scale, combat elements,
  etc.):
  [`docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE.md`](world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE.md)
  § *Preguntas abiertas*.

This file does not restate those. It defines **how** a slice is
validated, sequenced, reviewed, and shipped.

---

## 1. North star

The player configures policies and priorities for one persistent city.
The world executes only what the player has authorized, produces
understandable causal consequences, and presents each return as an
opportunity to make new decisions.

The desired fantasy is not direct control over every action. It is
shaping a society that remains legible and trustworthy while operating
without constant player attention.

The full fantasy statement is in the bible (§01).

---

## 2. Core loop to validate

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

## 3. Recommended validation sequence

This sequence is directional rather than contractual. Evidence from
play and implementation may change the order.

### 3.1 Persistent production authorization

Replace repeated manual advancement with a minimal policy per
building, such as enabled state, desired stock range, and priority.
The player configures the intent; the city performs the repeated work.

### 3.2 A small interdependent economy

Use Quarry and Farm to prove at least one understandable dependency
between resources, labor, storage, or construction. Prefer one
meaningful trade-off over many independent counters or a prematurely
generic production framework.

### 3.3 Causal simulation and return report

Advance the whole world through relevant domain events rather than
treating each elapsed second as the important unit. Reports should
identify the time, subject, result, and cause of meaningful changes.
They should distinguish healthy operation, stoppages, and decisions
requiring player attention.

### 3.4 A consequential city decision

Introduce one construction, policy, or institution whose availability
follows from real conditions such as people, knowledge, materials, and
authorization. It should create a lasting difference between otherwise
similar cities without using an arbitrary overall level.

### 3.5 Selective citizen depth

Add a citizen attribute only when it changes an actual decision.
Fatigue, aptitude, knowledge, health, relationships, and culture
should not arrive as a batch of passive fields. Each attachment must
participate in behavior, causality, presentation, and persistence when
introduced.

### 3.6 One complete minimal expedition

Prefer a small end-to-end expedition over a broad expedition skeleton:
an objective, members, supplies, one configurable policy, deterministic
causal resolution, a return, and a visible consequence for the city.
`DEC-0020` makes the first combat encounter part of proving that seam,
not a later reward for closing it. Broader combat, equipment, routes,
diplomacy, and exploration still wait until the end-to-end Founder Spirit
Trail is proven.

The first encounter occurs over the same expedition depth-band stage
(see `docs/ARCHITECTURE.md` §10 "Spatial grammar"): travel and combat
share the projection, only the camera policy changes. The former
"lateral battlefield" stage exists only as a transient prototype and
must not be reintroduced.

---

## 4. UI/UX is a core system constraint

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
[`docs/world-of-goses-design-bible/08_VISUAL_UI_AND_ASSET_GUIDELINES.md`](world-of-goses-design-bible/08_VISUAL_UI_AND_ASSET_GUIDELINES.md).

---

## 5. Technical direction (process rules)

These rules govern **how** code is added; the stack itself lives in
the bible and in `docs/ARCHITECTURE.md`.

- Keep the simulation deterministic and independent of Godot. The
  boundary rule and folder layout are in
  [`docs/ARCHITECTURE.md`](ARCHITECTURE.md) § 2–5.
- Use the same domain rules for live advancement and offline catch-up.
  The persistence approach is in
  [`docs/ARCHITECTURE.md`](ARCHITECTURE.md) § 8.
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

## 6. Warning signs of drift

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

## 7. Alignment review checklist

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

## 8. Current recommended next proof

The active proof is **EG-5V — Founder Spirit Trail visual vertical**. From a
clean slot, the player must move through astral onboarding, the authored first
night and `SpiritDeparted`, dispatch the Founder, see the first automatic
encounter within roughly five minutes of gameplay, continue to the
objective and return to the city. The encounter occurs over the same expedition
depth-band stage as travel (per `docs/ARCHITECTURE.md` §10 "Spatial grammar").

The proof keeps one unpausable world clock: city, travel and combat advance in
parallel; switching to `ExpeditionLiveView` preserves the current 1x / 2x / 4x
speed. It is intentionally narrow. Traits, Chains, carriage, `SPACE`, advanced
formation and functional Active Skills 2–4 remain deferred.

After EG-5V, resume **EG-5C** (plots 2–3 and Farm consolidation), then close
**EG-6** calibration/signature. `DEC-0020` supersedes the earlier rule that no
combat depth could open before EG-5/EG-6; it does not approve broader combat.

The **current** slice status, next proof, and verification commands are
in
[`docs/CURRENT_STATUS.md`](CURRENT_STATUS.md).
The validation cross-check against the bible is in
[`docs/VALIDATION.md`](VALIDATION.md).

When skill mechanics are introduced, they must demonstrate that
lineage mostly affects early learning context while experience,
education, tools, health, motivation, opportunities, and institutions
can overturn the initial tendency. A professional affinity chosen by
an individual may contradict their lineage and remains valid. The
canonical lineage contract is in the bible
([§04](world-of-goses-design-bible/04_CITIZENS_PROFESSIONS_AND_HEROES.md)
and
[§06](world-of-goses-design-bible/06_LINEAGES.md)); the implementation
guardrail ("don't turn lineage into a production bonus") is in
[`docs/VALIDATION.md`](VALIDATION.md).

The exact first-building costs, time scale, policies, and later skill
formulas remain open. They should be chosen through implementation
and play rather than treated as commitments in this guide.
