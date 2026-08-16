# Cross-domain invariants

> Constraints that hold regardless of which agent is working. A change that
> violates one of these must be redesigned, not approved.
>
> Each invariant names its source. If an invariant here ever contradicts the
> canonical document, the canonical document wins and this file must be corrected.

Source shorthand: a citation like *(systems/citizens)* means `docs/systems/citizens.md`.

---

## Product

- The city is the long-term protagonist. *(world/vision)*
- One game represents one city. No meta-progression between cities, no bonus
  for restarting. *(world/vision)*
- Citizens are the source of decisions, risk, and history. Buildings are
  infrastructure, not the goal of the game.
- A feature must enable a decision or communicate a consequence. Data that
  does neither must not be added. *(world/vision guardrails)*
- Quantity of content is not a substitute for depth.
- Do not turn the game into a traditional city builder with anonymous
  inhabitants, and do not turn it into a classic colony simulator.
  *(world/vision guardrails)*
- A mechanic is not implemented only because it is technically possible.
  *(docs/README.md, authority hierarchy)*

## Citizens

- There is one principal entity, `Citizen`. *(systems/citizens)*
- Do not create separate entities or subclasses for hero, miner, medic,
  artisan, leader, or adventurer. These are assignments, competencies, ranks,
  memberships, recognitions, or history. *(systems/citizens)*
- Professions, roles, and heroism are accumulative state on the citizen.
  Changing profession does not erase the previous life. *(systems/citizens)*
- Everyone can develop every competency. *(systems/citizens)*
- A citizen cannot hold incompatible commitments simultaneously. Work,
  construction, expedition, rest, and recovery are mutually exclusive.
  *(systems/citizens)*
- One truth has one owner. No two fields may independently answer the same
  question about a citizen — what they are doing is derived from commitment,
  location, transit metadata, wound, vital status, stamina and work order,
  never stored beside them. *(docs/engineering/state-authority.md)*
- Personal consequences persist across sessions.
- There is no general instant healing. A wounded person requires beds, staff,
  medicine, time, and rehabilitation. *(world/vision principle 8, world/vision pillar 6)*
- The domain never inflicts a durable consequence the city has no legal route
  to resolve. A wound is only inflicted on a city that can already treat it —
  a Basic Shelter and the treatment cost — because the wound itself blocks
  the gathering, construction and expedition work that would earn either.
  *(systems/expeditions, DEC-0011, GitHub #13)*
- The visual representation is not the persistent entity. `Citizen` is
  persistent data; `CitizenView` is a temporary visual representation. There
  must be no active Godot node per citizen. *(systems/citizens, engineering/architecture)*
- The founder must not become an eternal bonus. They can die, retire, become
  myth, be questioned, or lose relevance. *(systems/onboarding-and-founder)*

## Lineages

- Lineages are not professions and not combat classes. *(world/lineages)*
- They do not block professions, do not guarantee competence, and do not
  replace real experience. *(systems/citizens, world/lineages)*
- Affinities accelerate learning and transfer; they do not grant exclusive
  ownership of a trade. *(world/lineages)*
- An affinity must not be converted into an automatic production bonus.
  *(systems/citizens)*
- Every profession admits eight approaches, one per lineage. *(world/lineages)*
- The environmental alignment axis is independent of lineage identity, and must
  not become binary morality. *(world/vision, engineering/architecture)*

## Expeditions

- Real citizens depart. Only citizens incorporated as heroes participate.
  *(systems/expeditions)*
- The expedition includes the outbound leg **and** the return. It does not end
  on reaching the objective; it must return or trigger emergency return.
  *(systems/expeditions)*
- The player prepares teams but does not manually control movement. *(systems/expeditions)*
- The first Spirit Trail is Founder-only, reaches its first visual encounter
  within roughly the first five minutes of gameplay, continues to its objective,
  and includes the return. Its purpose is narrative trail-following, not a
  `1 Food → Wood` conversion. *(systems/expeditions, DEC-0022)*
- A combatant advances only to enter `AttackRange`; once able to attack, it does
  not kite backwards. Only a blow that applies `Knockdown` moves it; `Stability`
  reduces the displacement, `Impulse` may increase it and the physical share of
  the blow scales it. The lesser shove of a solid hit is a presentation-only hit
  reaction. *(systems/expeditions, systems/statistics-and-combat §2.3)*
- A physical expression is never guaranteed to land and never impossible to
  land: `ControlPower` is rolled against `ControlResistance` between a floor and
  a ceiling. No amount of any statistic makes a combatant immune to one.
  *(systems/statistics-and-combat §8.4)*
- A journey's duration is derived from the distance between its endpoints and
  the traveller's own `MovementSpeed`, never from a flat constant. Two journeys
  of equal length cost the same whatever the activity is for.
  *(systems/citizens, #58)*
- An aptitude changes how fast a citizen learns a competency and never how much
  they produce. Two citizens at the same competency level do the same work.
  *(systems/citizens, world/vision pillar on lineage)*
- A migrant's identity is a pure function of the founder, the arrival tick and
  the citizen id. Never free randomness — offline progression and save replay
  require the world to reproduce from what it stored — and never the citizen id
  alone, which is 2 for the first migrant of every city. *(systems/citizens)*
- No weapon family is strictly better than another. `PhysicalTransfer` and
  `ElementalResonance` are a trade within one sanctioned band, never a total to
  raise. *(systems/statistics-and-combat §4.3)*
- An expedition must affect the city, its citizens, or the territory.
- Rewards cannot be limited to a timed conversion of resources.
- Survivors return without equipment and with their wounds. The city must treat
  them. *(systems/expeditions, world/vision pillar 6)*

## City

- Production is causal. A building does not produce merely by existing.
  Production depends on accessible resource, workers, competence, tools,
  materials, energy, health, logistics, storage, policy, and risk.
  *(world/vision pillar 4)*
- Blockers must be visible to the player as stop causes.
- Delegation executes the player's rules; it does not invent its own.
- Systemic pressure must be causal, not an arbitrary drain.
- Buildings are not unlocked by level alone. They require knowledge, plans,
  politics, materials, professionals, territory, infrastructure, and demand.
  *(systems/city-and-territory)*
- Construction is collaborative, phased work. Its duration emerges from the
  assigned citizens, their skills, conditions, tools, and logistics — it is not
  a fixed countdown.
- Territorial reservation and physical obstruction are distinct for every
  obstacle. A resource unit occupies one explicit frontage cell rather than a
  building lot; different resource types may share a parcel and row. Buildings
  retain multi-column reservations. Navigation blocks only each authored solid
  footprint and respects its clearances. Trees receive no special exception.
  *(systems/city-and-territory, systems/frontage-and-corridors)*
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
  frame rate, input, or asset paths. *(engineering/architecture)*
- Scenes do not decide rules. Input handling is presentation; decision-making
  is domain. *(world/vision principle 13, docs/engineering/architecture.md)*
- Do not simulate each citizen per frame. No `_Process` per citizen. *(engineering/architecture)*
- Do not simulate offline progression second by second. Prefer discrete,
  batched events. *(engineering/architecture)*
- Live advancement and offline catch-up use the same domain rules.
  *(docs/engineering/design-review.md)*
- A citizen's journey ends when world time reaches its arrival tick, never
  because a view reported an arrival. There is one tick method; presentation
  paces its route to the domain's window and cannot delay, deny or hasten the
  outcome. *(DEC-0023, `CityWorld.CompleteDueTravel`)*
- There is one world clock. City, travel, and combat advance in parallel. The
  world cannot be paused; the only current global speeds are 1x / 2x / 4x, and
  changing to or from `ExpeditionLiveView` never changes speed.
  *(engineering/architecture §6)*
- The simulation is deterministic and reproducible from persisted state.
- Saves are versioned; migrations are explicit; snapshots are validated before
  mutating live state. *(engineering/architecture, docs/engineering/design-review.md)*
- Commands are player authorization; events are the resulting facts. Causal
  information is preserved long enough to produce useful reports.
  *(docs/engineering/design-review.md)*
- Invariants are covered by tests. `DomainBoundaryTests` enforces the Godot
  boundary and must keep passing.
- No mutable global state, no premature event bus, no pathfinding for invisible
  population. *(engineering/architecture)*

## First night (DEC-0014)

- The post-manifestation period runs from the founder's arrival at tick 0 to
  `FirstNightStage.Concluded` and is absorbing thereafter. Cities that already
  passed their opening (legacy saves or post-soft-reset) enter the period
  already concluded. *(docs/systems/first-night.md §17,
  `Domain/FirstNightState.cs`, `Domain/Persistence/WorldPersistence.cs`)*
- The night advances only on a closed dialogue node or a completed module
  (`FirstNightRules.WaitsForDialogue` / `WaitsForModule`). The clock never
  moves the night — a slow reader cannot lose the sequence.
- The Bedroll (or a consolidated `Home`) is the first mechanical meaning of
  "somewhere to sleep": `CityWorld.HasRestingPlace()` gates
  `TryCloseFirstNightDialogue` at the `OtherLightTold` → `Sleeping` boundary.
  Until then the founder cannot fall asleep.
- The fire spirit's visual position is derived from `FirstNightStage`, never
  persisted. The spirit's body of facts (its presence, its trail) lives in
  the chronicle; its location never does.
- Quantities in the spirit's dialogue come from `FoundingSiteRules.InputsFor`
  at runtime. No digit may appear in any `firstnight.*` msgid or msgstr —
  `FirstNightDialogueNoLiteralDigitsTests` enforces the invariant.
- Saving, loading, reloading or restarting cannot leave the sequence in an
  impossible state. `FirstNightState.CurrentDialogueNodeId` is the seam
  (not `DialogueRunner`'s async cursor): a save interrupted mid-line
  resumes on the same line. *(doc 19 invariant 13)*
- The route is strictly linear. Every node has empty `Choices` and `null`
  `Next`; the only advance path is `CityWorld.TryCloseFirstNightDialogue`.
- Variations per `LineageId` are textual reactions only. They never branch
  the route and never expose internal labels (no "primary affinity"
  surfaced to the player). *(doc 19 §13–14)*
- The three levels of post-dawn guidance stay separated: the first night is
  authored and finite; after dawn, derived directives are systemic and the
  Camino is read-only. No list of mission steps is built from the night,
  and no modal "tutorial replay" ships from it.

## Presentation

- Pure 2D pixel art. Not 2.5D as the primary direction. *(presentation/visual-language)*
- Integer scale, nearest filtering, integer positions, no antialiasing on
  sprites and pixel-art UI, no fractional edge coordinates. *(presentation/visual-language, engineering/architecture)*
- Logical resolution 1280 x 720. *(presentation/visual-language, engineering/architecture)*
- Motion uses a discrete cadence grammar. Camera and world navigation use
  quantized steps, never smooth continuous 1:1 motion. The single exception is
  expedition combat, which moves continuously from the first step of an
  encounter until travel resumes. *(presentation/visual-language)*
- Do not communicate a state by color alone.
- UI is functionally shared across lineages. Lineage themes may change palette,
  borders, corners, fills, shadows, patterns, selection, micro-animations, and
  icon treatment — never navigation, hierarchy, semantics, minimum sizes, or
  accessibility. *(presentation/visual-language)*
- Provisional assets do not define the final art direction. *(engineering/architecture)*
- HUD lives on a `CanvasLayer` independent of the world `Camera2D`. *(engineering/architecture)*

---

## Escalation

Stop and ask the user when you find:

- A contradiction between canonical documents.
- An unresolved product decision that changes the design.
- A risk of invalidating existing saves without a migration strategy.
- A need to remove or replace a central system.
