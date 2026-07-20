# Product Direction and Alignment Guide

> A living decision guide for checking whether ongoing development still
> serves the intended game. It complements `GAME_VISION.md`; it does not
> replace the vision or freeze implementation details that have not yet been
> validated. Revisit this guide whenever the roadmap, a major system, or the
> core interaction model changes.

---

## 1. North star

The player configures policies and priorities for one persistent city. The
world executes only what the player has authorized, produces understandable
causal consequences, and presents each return as an opportunity to make new
decisions.

The desired fantasy is not direct control over every action. It is shaping a
society that remains legible and trustworthy while operating without constant
player attention.

## 2. Core loop to validate

The project should progressively prove this complete loop:

1. The player observes needs, resources, people, and constraints.
2. The player authorizes an order, policy, assignment, or priority.
3. Time advances with the game open or closed.
4. The city executes the authorization without inventing sovereign decisions.
5. Resources, experience, risks, and opportunities change for explicit causes.
6. The game explains what changed, why, and what now needs attention.
7. The player chooses among multiple valid responses.

A feature that cannot yet support the whole loop may still be useful, but its
place in this loop must be stated before implementation.

## 3. Recommended validation sequence

This sequence is directional rather than contractual. Evidence from play and
implementation may change the order.

### 3.1 Persistent production authorization

Replace repeated manual advancement with a minimal policy per building, such
as enabled state, desired stock range, and priority. The player configures the
intent; the city performs the repeated work.

### 3.2 A small interdependent economy

Use Quarry and Farm to prove at least one understandable dependency between
resources, labor, storage, or construction. Prefer one meaningful trade-off
over many independent counters or a prematurely generic production framework.

### 3.3 Causal simulation and return report

Advance the whole world through relevant domain events rather than treating
each elapsed second as the important unit. Reports should identify the time,
subject, result, and cause of meaningful changes. They should distinguish
healthy operation, stoppages, and decisions requiring player attention.

### 3.4 A consequential city decision

Introduce one construction, policy, or institution whose availability follows
from real conditions such as people, knowledge, materials, and authorization.
It should create a lasting difference between otherwise similar cities without
using an arbitrary overall level.

### 3.5 Selective citizen depth

Add a citizen attribute only when it changes an actual decision. Fatigue,
aptitude, knowledge, health, relationships, and culture should not arrive as a
batch of passive fields. Each attachment must participate in behavior,
causality, presentation, and persistence when introduced.

### 3.6 One complete minimal expedition

Prefer a small end-to-end expedition over a broad expedition skeleton: an
objective, members, supplies, one configurable policy, deterministic causal
resolution, a return, and a visible consequence for the city. Expand combat,
equipment, routes, diplomacy, and exploration only after that seam is proven.

## 4. UI/UX is a core system constraint

The intended simulation can become deep. Its interface must therefore receive
the same design effort as the domain model. Complexity may live in the system;
confusion, repetitive work, and hidden causality must not live in the player's
interaction with it.

The governing rule is:

> Simplify operation and comprehension, not the underlying systemic depth.

Good UI/UX should let a player engage at several levels without creating a
separate "simple mode" that removes meaningful rules:

- **Overview first.** Show what is healthy, blocked, changing, or awaiting a
  decision before exposing every parameter.
- **Progressive disclosure.** Move from city summary to system, building,
  citizen, and causal detail without losing context.
- **Explain every consequence.** Values and states should expose their source,
  dependencies, recent changes, and stopping conditions.
- **Policies over repetitive clicks.** Repeated intent belongs in persistent
  configuration, sensible defaults, templates, ranges, priorities, and batch
  operations—not in click-heavy maintenance.
- **Safe experimentation.** Preview costs and expected effects, clearly mark
  irreversible decisions, and make reversible configuration easy to revise.
- **Attention management.** Rank information by urgency and player authority.
  Do not use alerts for events that require no decision.
- **Preserve alternatives.** Convenience features must not silently choose the
  single "correct" strategy or conceal meaningful trade-offs.
- **Consistent navigation.** Screen hierarchy, back behavior, selection state,
  keyboard/gamepad focus, and mouse interaction must remain predictable.
- **Responsive presentation.** Use anchors and containers, support relevant
  aspect ratios, and avoid layouts that only work at one window size.
- **Event-driven updates.** Presentation reacts to domain events; it does not
  create state or poll every frame without a concrete need.

UI acceptance is part of feature acceptance. A system is not complete merely
because its domain logic works: the player must be able to discover, configure,
understand, and revisit it with low interaction cost.

## 5. Technical direction

- Keep the simulation deterministic and independent of Godot.
- Use the same domain rules for live advancement and offline catch-up.
- Simulate the whole city, not only the currently selected building.
- Treat commands as player authorization and events as resulting facts.
- Preserve causal information long enough to produce useful reports and
  history.
- Keep persistence versioned and validate snapshots before mutating live state.
- Add abstractions only when a current vertical slice needs them.
- Keep Godot responsible for representation, navigation, animation, audio, and
  interaction feedback—not business rules.

Parameters such as real-time scale, offline limits, and final progression pace
remain provisional until play validates them. Temporary implementation values
must not silently become product rules.

## 6. Warning signs of drift

Reconsider a proposed change when it:

- Adds types, DTOs, screens, or frameworks without enabling a player decision.
- Adds several independent counters without a causal relationship between them.
- Requires frequent clicking to restate an intent the player already expressed.
- Hides complexity by removing meaningful constraints or choices.
- Produces consequences the interface cannot explain.
- Advances only the visible or selected system while the rest of the city waits.
- Introduces arbitrary levels, random rewards, or unexplained failure.
- Builds a large future-facing skeleton instead of a small complete loop.
- Expands content faster than the interface can organize and communicate it.

## 7. Alignment review checklist

Use these questions when reviewing the project or planning a slice:

1. What new decision can the player make?
2. What persistent authorization can the city execute without repeated input?
3. What causal consequence can occur while the player is absent?
4. How will the player understand what changed and why?
5. Does the feature create more than one valid response or development path?
6. Does UI/UX reduce interaction cost without deleting systemic depth?
7. Can the player reach details from an overview and return without losing
   context?
8. Do live and offline advancement use the same domain behavior?
9. Does the change preserve the single-city, no-meta-progression constraint?
10. Is this the smallest complete slice that can validate the idea?

If the answers are weak, refine the slice before expanding its implementation.

## 8. Current recommended next proof

The current proof is the founding-hero vertical slice: a new world begins empty, the player completes a complete identity profile, and the game persists one hero without inventing buildings or secondary citizens. The hero profile is visible and the old production fixture is no longer the game's established data.

The next proof should give the player one consequential first-building decision. That decision should follow from the hero's profile, available knowledge, materials, authorisation, and the city's actual conditions. It must not turn a lineage affinity into an automatic production bonus or use an arbitrary overall level.

### 8.1 Lineage and profile contract

The eight lineage definitions and the citizen-profile vocabulary are canonical in `LINEAGES_AND_PROFESSIONAL_AFFINITIES.md`. The current implementation stores and presents them as validated identity data. It intentionally does not interpret them as production percentages, permanent skill ceilings, or profession locks.

When skill mechanics are introduced, they must demonstrate that lineage mostly affects early learning context while experience, education, tools, health, motivation, opportunities, and institutions can overturn the initial tendency. A professional affinity chosen by an individual may contradict their lineage and remains valid.

The exact first-building costs, time scale, policies, and later skill formulas remain open. They should be chosen through implementation and play rather than treated as commitments in this guide.
