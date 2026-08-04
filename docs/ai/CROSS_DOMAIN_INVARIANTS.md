# Cross-domain invariants

> Constraints that hold regardless of which agent is working. A change that
> violates one of these must be redesigned, not approved.
>
> Each invariant names its source. If an invariant here ever contradicts the
> design bible, the bible wins and this file must be corrected.

Source shorthand: `bible/NN` = `docs/world-of-goses-design-bible/NN_*.md`.

---

## Product

- The city is the long-term protagonist. *(bible/01)*
- One game represents one city. No meta-progression between cities, no bonus
  for restarting. *(bible/01)*
- Citizens are the source of decisions, risk, and history. Buildings are
  infrastructure, not the goal of the game.
- A feature must enable a decision or communicate a consequence. Data that
  does neither must not be added. *(bible/10, guard-rails)*
- Quantity of content is not a substitute for depth.
- Do not turn the game into a traditional city builder with anonymous
  inhabitants, and do not turn it into a classic colony simulator.
  *(bible/10, guard-rails)*
- A mechanic is not implemented only because it is technically possible.
  *(docs/README.md, authority hierarchy)*

## Citizens

- There is one principal entity, `Citizen`. *(bible/04)*
- Do not create separate entities or subclasses for hero, miner, medic,
  artisan, leader, or adventurer. These are assignments, competencies, ranks,
  memberships, recognitions, or history. *(bible/04)*
- Professions, roles, and heroism are accumulative state on the citizen.
  Changing profession does not erase the previous life. *(bible/04)*
- Everyone can develop every competency. *(bible/04)*
- A citizen cannot hold incompatible commitments simultaneously. Work,
  construction, expedition, rest, and recovery are mutually exclusive.
  *(docs/FIRST_PLAYABLE_LOOP_AUDIT.md, gap G0)*
- Personal consequences persist across sessions.
- There is no general instant healing. A wounded person requires beds, staff,
  medicine, time, and rehabilitation. *(bible/01 principle 8, bible/02 pillar 6)*
- The visual representation is not the persistent entity. `Citizen` is
  persistent data; `CitizenView` is a temporary visual representation. There
  must be no active Godot node per citizen. *(bible/04, bible/10)*
- The founder must not become an eternal bonus. They can die, retire, become
  myth, be questioned, or lose relevance. *(bible/07)*

## Lineages

- Lineages are not professions and not combat classes. *(bible/06)*
- They do not block professions, do not guarantee competence, and do not
  replace real experience. *(bible/04, bible/06)*
- Affinities accelerate learning and transfer; they do not grant exclusive
  ownership of a trade. *(bible/06)*
- An affinity must not be converted into an automatic production bonus.
  *(bible/04)*
- Every profession admits eight approaches, one per lineage. *(bible/06)*
- The environmental alignment axis is independent of lineage identity, and must
  not become binary morality. *(bible/02, bible/10)*

## Expeditions

- Real citizens depart. Only citizens incorporated as heroes participate.
  *(bible/05)*
- The expedition includes the outbound leg **and** the return. It does not end
  on reaching the objective; it must return or trigger emergency return.
  *(bible/05)*
- The player prepares teams but does not manually control movement. *(bible/05)*
- An expedition must affect the city, its citizens, or the territory.
- Rewards cannot be limited to a timed conversion of resources.
- Survivors return without equipment and with their wounds. The city must treat
  them. *(bible/05, bible/02 pillar 6)*

## City

- Production is causal. A building does not produce merely by existing.
  Production depends on accessible resource, workers, competence, tools,
  materials, energy, health, logistics, storage, policy, and risk.
  *(bible/02 pillar 4)*
- Blockers must be visible to the player as stop causes.
- Delegation executes the player's rules; it does not invent its own.
- Systemic pressure must be causal, not an arbitrary drain.
- Buildings are not unlocked by level alone. They require knowledge, plans,
  politics, materials, professionals, territory, infrastructure, and demand.
  *(bible/03)*
- Construction is collaborative, phased work. Its duration emerges from the
  assigned citizens, their skills, conditions, tools, and logistics — it is not
  a fixed countdown.
- Territorial reservation and physical obstruction are distinct for every
  obstacle. A resource unit occupies one explicit frontage cell rather than a
  building lot; different resource types may share a parcel and row. Buildings
  retain multi-column reservations. Navigation blocks only each authored solid
  footprint and respects its clearances. Trees receive no special exception.
  *(bible/03, bible/12)*
- Rudimentary ground resources are tool-free, but mature-tree Wood requires a
  persisted forestry capability. The first capability is a durable Primitive
  Axe stored after Shelter completion; repeated input cannot gather a depleted
  node twice, and storage rejection cannot drain it. *(early-game proposal §4)*
- Before Cache, rudimentary resources occupy the founder's six-unit carried
  load; unrelated inventory cannot fill it. Cache and Shelter supersede that
  owner with capacities 12 and 24, and presentation must expose the current
  owner rather than imply a hidden pre-camp warehouse. *(early-game proposal §4)*

## Architecture

- The domain does not depend on Godot nodes, sprites, animations, cameras,
  frame rate, input, or asset paths. *(bible/10)*
- Scenes do not decide rules. Input handling is presentation; decision-making
  is domain. *(bible/01 principle 13, docs/ARCHITECTURE.md)*
- Do not simulate each citizen per frame. No `_Process` per citizen. *(bible/10)*
- Do not simulate offline progression second by second. Prefer discrete,
  batched events. *(bible/10)*
- Live advancement and offline catch-up use the same domain rules.
  *(docs/PRODUCT_DIRECTION.md)*
- The simulation is deterministic and reproducible from persisted state.
- Saves are versioned; migrations are explicit; snapshots are validated before
  mutating live state. *(bible/10, docs/PRODUCT_DIRECTION.md)*
- Commands are player authorization; events are the resulting facts. Causal
  information is preserved long enough to produce useful reports.
  *(docs/PRODUCT_DIRECTION.md)*
- Invariants are covered by tests. `DomainBoundaryTests` enforces the Godot
  boundary and must keep passing.
- No mutable global state, no premature event bus, no pathfinding for invisible
  population. *(bible/10)*

## Presentation

- Pure 2D pixel art. Not 2.5D as the primary direction. *(bible/08)*
- Integer scale, nearest filtering, integer positions, no antialiasing on
  sprites and pixel-art UI, no fractional edge coordinates. *(bible/08, bible/10)*
- Logical resolution 1280 x 720. *(bible/08, bible/10)*
- Motion uses a discrete cadence grammar. Camera and world navigation use
  quantized steps, never smooth continuous 1:1 motion. *(bible/08)*
- Do not communicate a state by color alone.
- UI is functionally shared across lineages. Lineage themes may change palette,
  borders, corners, fills, shadows, patterns, selection, micro-animations, and
  icon treatment — never navigation, hierarchy, semantics, minimum sizes, or
  accessibility. *(bible/08)*
- Provisional assets do not define the final art direction. *(bible/10)*
- HUD lives on a `CanvasLayer` independent of the world `Camera2D`. *(bible/10)*

---

## Escalation

Stop and ask the user when you find:

- A contradiction between canonical documents.
- An unresolved product decision that changes the design.
- A risk of invalidating existing saves without a migration strategy.
- A need to remove or replace a central system.
