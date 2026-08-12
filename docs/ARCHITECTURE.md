# Architecture

> The initial architecture of World of Goses. This document describes the
> current project layout and the small set of boundaries that the rest of
> the code will be written against. It does not commit to systems that
> are not yet validated by a prototype.

The conceptual design bible lives at
[`docs/world-of-goses-design-bible/`](world-of-goses-design-bible/README.md);
[`world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE.md`](world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE.md)
holds the engine-neutral stack, scene map, and roadmap questions that
this implementation-aware file answers for the Godot + C# stack. When
the two disagree on a folder or a boundary, this file wins for the
code that exists today; the bible wins for *what the game must
eventually be*.

---

## 1. Goals of the initial architecture

The architecture has three goals, in order:

1. **Keep the simulation independent of the engine.** Domain types do
   not import `Godot.*`. Domain logic can be unit-tested without the
   engine running.
2. **Make one persistent city tractable.** The code is structured so
   that the two gameplay pillars — city development and expeditions —
   can grow without leaking into each other.
3. **Stay small.** No backend, no microservices, no premature patterns.

## 2. Layers

The project is conceptually separated into five layers:

```
┌───────────────────────────────────────────────────────────────┐
│  Domain and simulation   (C#, no Godot types)                 │
├───────────────────────────────────────────────────────────────┤
│  Godot representation    (scenes, nodes, animations, input)  │
├───────────────────────────────────────────────────────────────┤
│  Assets                  (PNG / sprite sheets / audio)        │
├───────────────────────────────────────────────────────────────┤
│  Local persistence       (versioned JSON snapshots)           │
├───────────────────────────────────────────────────────────────┤
│  Tests                   (xUnit domain/persistence suite)      │
└───────────────────────────────────────────────────────────────┘
```

The simulation does **not** depend directly on sprites, cameras, or
animations. The visual representation reacts to domain state.

## 3. Technology choices

- **Engine:** Godot `.NET` 4.7.x.
- **Language:** C# 12 on `.NET 8.0` (Android export: `net9.0`).
- **Editor:** Visual Studio Code.
- **Pixel art:** Pixelorama.
- **Persistence:** Local, versioned JSON snapshots with atomic replacement.
- **Backend:** Not implemented yet. None planned for the prototype.

The choices are stated in `README.md`. They will be revisited only when a
concrete need appears.

## 4. Project layout

```
world-of-goses/
├── .git/
├── AGENTS.md
├── README.md
├── .gitignore
├── docs/
│   ├── README.md                                  # consolidated doc index
│   ├── CURRENT_STATUS.md                          # current slice, next proof
│   ├── ARCHITECTURE.md                            # this file
│   ├── ART_PIPELINE.md                            # Pixelorama → PNG → Godot
│   ├── VALIDATION.md                              # cross-check vs bible
│   ├── PRODUCT_DIRECTION.md                       # process guide
│   ├── GAME_VISION.md                             # pointer → bible
│   ├── LINEAGES_AND_PROFESSIONAL_AFFINITIES.md    # pointer → bible
│   ├── DESIGN_INFLUENCES.md                       # pointer + audit trail
│   └── world-of-goses-design-bible/               # canonical design source
├── art/
│   ├── source/        # Pixelorama sources
│   │   ├── characters/
│   │   ├── buildings/
│   │   ├── terrain/
│   │   ├── effects/
│   │   └── ui/
│   ├── references/    # Mood boards, inspiration (not game art)
│   └── exports/       # PNG / sprite sheets exported from Pixelorama
├── game/
│   ├── project.godot
│   ├── World of Goses.csproj
│   ├── World of Goses.sln
│   ├── assets/        # Imported PNG / audio used by Godot
│   │   ├── characters/
│   │   ├── buildings/
│   │   ├── terrain/
│   │   ├── effects/
│   │   ├── audio/
│   │   └── ui/
│   ├── scenes/        # .tscn files
│   └── scripts/       # .cs files
└── tests/
    └── WorldofGoses.Tests/  # xUnit domain/persistence tests
```

`game/.godot/`, `game/bin/`, `game/obj/`, `.vscode/`, `*.tmp`,
`*.autosave`, `Thumbs.db`, `.DS_Store`, `*.exe`, `*.dll`, `*.pck`, and
similar artifacts are ignored by `.gitignore`.

## 5. The Godot `.NET` boundary

Godot 4.7 `.NET` uses C# source generators that emit `*.<Name>.cs`
partials. The agents and humans working on this project must:

- Never edit generated partials.
- Use `partial` only when required by the engine.
- Keep the domain free of `Godot.*` references.

The boundary in practice:

- **Domain layer:** pure C#. Holds entities, value objects, state
  machines, and rules. No nodes, no scenes, no signals.
- **Presentation layer:** Godot nodes. Reads domain state, drives
  animations, plays sounds, listens for input. Thin adapters.
- **Adapters:** small classes that translate between domain events and
  Godot signals, and between user input and domain commands.

`CityWorld` is the public aggregate facade, not the required home of every rule.
`CitizenAssignmentService` owns building/project assignment consistency,
citizen location transitions, and auto-release while operating only on
collections owned by the aggregate. `BuildingProductionSimulation` owns the
productive tick sequence: recipe gate, food/regeneration, stamina cost,
contributors, output, experience, and stop cause. Resource transfer and causal
log ownership stay in `CityWorld` through narrow delegates until H-23/H-22
provide their dedicated models.
`ConstructionSimulation` owns work-interval material drawdown and rollback,
stamina recovery/cost, contribution, project stop causes, blocking events, and
night rest. Authorisation and the final project-to-building transition remain
aggregate operations in `CityWorld`.
Presentation and controllers continue calling `CityWorld`; collaborators are
not service locators and are not exposed across the Godot boundary. Further
extraction requires a concrete slice; the aggregate still intentionally owns
resource topology, causal history, persistence restore, and orchestration.

Citizen responsibility is split between a durable player-authored
`Citizen.WorkOrder` and the mutually-exclusive current `Citizen.Commitment`.
Temporary expedition or vital-recovery execution can therefore suspend work
without deleting its target. On return, the scheduler re-evaluates the standing
order instead of resuming a stale action blindly. `CitizenVitalStatus` is
survival-only: it may pause for food/rest, but never chooses a profession or a
productive target. These additive fields remain compatible with older v14
snapshots; restore infers a missing standing order from `CurrentAssignment`.

Daytime assignment commits the citizen immediately but places their physical
location in `InTransit`. Production and construction simulations count only
assigned citizens whose location is `AtWork`. **World time is the only authority
that ends a journey.** `Citizen.TransitStartedAtTick` and `IsReturningHome` are
the durable facts; `Citizen.TravelArrivalTick` derives the deadline from them,
and `CityWorld.CompleteDueTravel` — reached from the single `AdvanceWorldTick`
that live play and offline catch-up share — is the only code that ends a trip.
A journey coming due outside labour hours reverses toward Home rather than
parking the citizen at a closed worksite, preserving the standing order. Neither
path stores pixel coordinates. Recovery exposes `WorkersRecovering` or
`WorkersBlockedNoFood`; once fed and above the resume threshold, the same
standing order is re-evaluated and travelled again.

Until A2 (`DEC-0023`) this worked the other way: live play could only end a
journey when `MacroStreetLiveView` reported its sprite had reached an anchor,
while offline catch-up ended the same journey on elapsed ticks. That was two
semantic authorities for one fact, and it meant an animation that never ran — a
hidden view, an unsolvable route — could hold a citizen in transit indefinitely
and keep their workplace on `WorkersInTransit` forever.

`CityEconomyRules` separates the one-second clock resolution from economic
events. Productive buildings resolve deterministic batches every ten ticks;
food is considered on a separate meal cadence, so animation frames and clock
ticks do not imply resource creation or consumption. Both live and offline
progression use the same absolute-tick predicates. The additive
`EconomicBalanceVersion` save field applies the first storage rebalance once to
older snapshots without rewriting explicitly tuned capacities in new saves.

`GameClock` / `CityWorld.CurrentTick` are the single world-time authority for
city, expedition travel and combat. A future `ExpeditionLiveView` consumes
that state; it does not own a timer. The controller exposes only global 1x,
2x and 4x cadence choices. There is no paused world state, and opening Menu or
switching presentation surfaces cannot change the selected speed.

Macro workplace routing targets the front approach band rather than the
occupied building centre. A carrier is visible while travelling, hidden on the
macro map while the citizen works inside, and mounted into the interior worker
slot when building detail is open. This remains presentation state; the domain
stores only `InTransit` or `AtWork`, never pixel coordinates.

`MacroStreetLiveView` draws journeys; it does not end them. The founding hero
and every other citizen use the same `StreetRoutePlanner`, obstacle topology and
quantized cadence; the founder keeps a dedicated carrier path only for
founder-specific actions such as gathering. `BuildingDetailView` renders only
citizens already at `AtWork`.

A drawn route is paced against the domain's own window rather than against the
render cadence: `PacedRouteSteps` spreads the route's steps across
`TransitStartedAtTick → TravelArrivalTick`, so the walk finishes on the tick the
domain has already chosen, 2x/4x accelerate it for free, and a dropped frame
merely catches up on the next one. Steps stay discrete — the pacing changes only
their timing, never the cadence grammar. The same `ReconstructRouteProgress` that
resumes a part-elapsed journey after a load is what advances a fresh one, so
restore and live play are one code path instead of two.

Routes with no domain journey behind them — gathering, ambient wandering — have
no arrival tick to be paced against and keep the plain cadence gait.
`ArchitectureBoundaryTests.Presentation_DoesNotConfirmCitizenArrival` keeps the
old authority from returning, with an allowlist that is empty by construction.

**A4 — macro view composition.** `MacroStreetLiveView` composes five
single-responsibility collaborators plus three pure-helper classes (A4):
`MacroStreetRenderer` (records + per-street draw + band-layer stack + hit-rect
publication), `MacroInteractionController` (selection + hover state + the one
`HitTest` that resolves a pointer position to a tree, a citizen or a building —
left and right click share it, so the two buttons cannot drift to different
targets for the same pixel),
`CitizenJourneyPresenter` (founder state + per-citizen journey dictionary +
`StreetNavigationServerPlanner`), `MacroCameraController` (zoom + free/follow
mode + lateral/depth anchor + pan + transition timing + building-entry push),
and `PlacementPresenter` (placement-mode state: the active flag, the chosen
kind, the projected lot and cell boxes, and the hovered / selected lot). The
placement clickable rects deliberately stay in `MacroHitRects`, the one
hit-rect bag the whole macro view shares, so the presenter takes them as an
argument instead of keeping a second copy. The pure helpers —
`MacroViewConstants`, `MacroProjectionHelpers`, `MacroObstacleGeometry`,
`MacroSelectionTextBuilder` — centralise numerics, projection maths, obstacle
geometry, and selection text. `MacroHitRects` and `MacroPlotLookup` are the
record-bag seams between the renderer and the interaction / journey paths. No
interfaces, no DI, no service locator. The view orchestrates the collaborators
but does not know their internals; the collaborators do not know about each
other. The presenter never ends a journey — `PacedRouteSteps` still paces
against the domain window, and `ArchitectureBoundaryTests.Presentation_DoesNotConfirmCitizenArrival`
keeps the old authority from returning.

What A4 has not done: the interaction *handlers* stay in the view on purpose.
`MacroInteractionController` answers "what is at this point" and owns the
selection and hover state, but `SelectTree`, `SelectBuildingPlot`,
`OpenGatherMenu`, `OpenCultivationMenu` and `UpdateWorldHover` still live in
the view, because moving them would mean handing the controller the context
inspector, both action menus, the terrain atlas and the controller facade —
trading one large class for a second one. The remaining follow-up is the
folder: `game/scripts/Prototypes/` is no longer a prototype and belongs under
`Presentation/Macro/`. Eight assertions in `HudCompositionTests` grep the view
by hardcoded path segments (`"game", "scripts", "Prototypes"`), so the move is
a rename plus those eight paths, and should be its own commit rather than a
rider on a behavioural change.

The macro camera is free by default. Selection changes information and action
context only; following the selected citizen requires the explicit camera toggle. WASD
and the arrow keys always pan the camera. In an unobstructed macro view their
physical key events are handled before GUI focus dispatch, so they cannot also
move focus across HUD buttons; gamepad D-pad remains available for focus.
Manual pan releases follow mode and
never changes the founder's physical street position.

`StreetDepthProjection` has one focus-relative perspective at every zoom level;
zoom is always a uniform node transform and never renormalizes or stretches the
terrain. Its visible window is bounded to thirteen construction streets: two
foreground streets, the focused street and ten receding streets. The fourth
position counting the focus crosses the near plane. `MacroStreetLiveView`
shifts this window as the camera advances through a larger semantic territory,
so off-window parcels are neither drawn nor folded onto the fixed horizon.

Citizen persistence remains semantic in schema v19: work order, commitment,
logical location, travel start and direction are authoritative; pixel position,
route cursor, sprite, animation, and node state are not stored. After restore and
offline advancement, `CitizenRoutineSnapshot` derives the current activity,
contextual building/Shelter, blocker, and next transition. The macro view derives
building anchors from the current placement and reconstructs an unfinished route
from semantic timing without writing that visual position back to the domain.

## 6. State, rules, and animation

The conceptual rule:

> Pixelorama defines how it looks.
> Godot defines how it is represented and animated.
> C# defines what is happening and why.

In practice:

- A Pixelorama source is the visual definition of a frame.
- A Godot `SpriteFrames` resource (or `TileSet`) is the runtime
  representation.
- A C# class decides which frame to display, when to transition, and
  what state the citizen / building / object is in.

The visual layer never invents state. It reads it.

## 7. Person entity: one citizen, many attachments

There is exactly one person entity in the domain: `Citizen`. The
prototype does **not** introduce specialised subclasses for hero,
miner, doctor, artisan, leader, adventurer, or any other role. Those
concepts are *attachments* composed onto a citizen:

- **Citizen profiles.** A citizen stores one immutable profile attachment containing lineage, personal aptitudes, professional affinities, elemental affinity, combat and weapon preferences, personality traits, political orientation, and spiritual posture. These are identity metadata in the current slice; they do not replace competencies or practical history.
- **Competencies.** A citizen accumulates experience in named
  competencies (e.g. mining). The current slice implements mining
  only; the model is open-ended and not bounded to a fixed number of
  competency slots.
- **Roles and recognitions.** A citizen may hold any number of roles
  at any time (e.g. founder, healer, expedition leader). Hero status
  is one of these roles — not a subclass.
- **Memberships and ranks.** The same composition mechanism carries
  institutional affiliations.
- **Availability.** Coarse state describing whether the citizen is
  free or currently assigned to a workplace. Future slices may extend
  this (injured, traveling, on leave) without changing the citizen
  model.

The vertical slice in `game/scripts/Domain/` exposes the minimum
needed by the current prototype (`CitizenId`, `LineageId`, profile option
IDs, `CompetencyId`, `RoleId`, `CompetencyEntry`, `Role`, `CitizenProfile`,
`Availability`, and `Citizen`). Professional history, education, health,
relationships, and expedition history remain future attachments.

A hero in this model is a citizen whose role list contains a `hero`
recognition. Any citizen is potentially eligible for expedition duty
through player choice and city systems — the domain does not enforce
statistical gates.

## 7a. Three visual scales

The prototype establishes three conceptual visual scales, even though
only the first two are implemented now.

| Scale              | Purpose                                                                 | Implementation          |
| ------------------ | ----------------------------------------------------------------------- | ----------------------- |
| Macro              | Communicate city-wide activity from a distance.                         | Implemented (this slice)|
| Building-detail    | Show workers inside a specific building; allow direct interaction.      | Implemented (this slice)|
| Expedition-detail  | Fully detailed side-facing sprites, frame-by-frame animation.           | Next approved vertical  |

Concretely:

- **Macro.** A city view uses one small marker per current citizen. Markers
  are not individually interactive yet, but their count is derived from the
  domain rather than from a fixed population fixture.
- **Building-detail.** When a building is selected, the view opens
  with one visible worker per *visual capacity* slot. Every visible
  worker corresponds to a real `CitizenId`. Workers that are assigned
  but exceed the visual capacity are reported as "working inside" by
  the domain; the presentation layer surfaces this number verbatim.
- **Expedition-detail.** The first lateral `ExpeditionLiveView` is implemented
  below `GameUiShell/ScreenContent`. It observes a domain `CombatSession` keyed
  by `ExpeditionId`; hiding the view never owns, stops or recreates combat.
  Each world tick advances one logical combat step and `ResolveToEnd` consumes
  that same incremental path for tests/debug. Schema v33 persists the logical
  step plus replayable AUTO/manual commands, then reconstructs health,
  cooldowns, RNG and `CombatLog` from the same seed and resolvers. Schema v34
  adds per-expedition combat-rules versioning: a v33 session replays its legacy
  balance, while a newly dispatched opening trail uses the non-persistent
  tutorial baseline. Later scenes
  will use fully detailed side-facing sprites authored as
  Pixelorama sprite sheets (`art/source/characters/...`) and driven by
  Godot `AnimatedSprite2D` with `AnimationPlayer` transitions.
  This observable path is currently restricted to the Founder-only
  `SpiritTrailSearch`; other expedition kinds keep the prior aggregate
  encounter resolver. Quiescent offline batching is disabled while its combat
  session is active so one world tick always means one combat step. The named
  `ExpeditionTiming` milestones drive the four-hour route: half-hour Encounter,
  post-combat Objective travel, physical objective arrival and visible Return.
  `SupplyRequirement.None` and `ExpeditionReward.Discovery` keep this narrative
  route outside the material reservation ledger.

Placeholder dimensions used by the prototype so that final art slots
in without re-anchoring:

```text
Base terrain unit:        64 × 64
Detailed citizen canvas:  approximately 64 × 96
Macro citizen:            approximately 6 × 6 (within the 4–8 range)
Building plot footprint:  192 × 192 (3 × 3 base units)
```

The numbers above live as constants in
`game/scripts/PresentationConstants.cs` so future art can rely on the
same anchors.

Logical placement does not use pixel rectangles. A parcel contributes nine
one-tile frontage columns to each of three construction rows. Standard
buildings reserve a sliding window of three contiguous columns with fixed
three-tile depth; the domain permits later growth up to six columns. Windows
may cross adjacent parcel boundaries only when every contributing parcel is
available. `BuildingReservation` owns this interval while
`ObstacleFootprintTemplate` describes the smaller solid geometry and authored
clearances in integer half-tiles for resources, constructions and
infrastructure. Protected `CorridorReservation` intervals cannot be consumed
by construction.

Schema v25 persists row, start column, total frontage, fixed depth and
directional expansion counts. The v24→v25 migration maps every former 3×3 lot
deterministically to three frontage columns while preserving the entity ID and
legacy anchor fields for diagnostics. The macro snapshot projects reservation
and solid footprint separately: placement draws the reserved area, while
`MacroStreetLiveView` sends only the clearance-derived solid interval to
navigation. Schema v26 persists one `NaturalResourceUnitPosition` per reserve
entry (`RowWithinParcel`, `FrontageColumnWithinParcel`). Fresh layouts use
`NaturalResourceLayoutPlanner` with the founder's stable seed; v25 saves are
deterministically reflowed around construction, corridors and the protected
founder arrival cell. Fresh positions are scattered across available cells
rather than authored as repeated compact rows. Patches of different resource
types may share a parcel.
Each live resource blocks one frontage cell for construction and uses the same
obstacle pipeline through `NaturalResourceFootprintCatalog`; trees have no
special collision rule. Godot translates this domain geometry to projected
pixels and anchors citizens on the street side; it does not decide placement
legality.

`ConstructionPlacementSnapshot` projects every visible frontage cell and every
candidate three-column window together with the domain-owned
`FrontageCellState`. `MacroStreetLiveView` uses that single read model for the
full two-axis grid, blocked-cell marks, hover feedback and final selection, so
hover cannot promise a placement that confirmation later rejects under a
different presentation-only rule.

The first agricultural authorization is a bounded exception to ordinary
building completion. `ConstructionKind.CultivationSite` still reserves a
normal persisted three-column frontage window, but completion replaces the
project with the Godot-free
`CultivationSite` domain entity rather than a productive `Building`. That
entity owns only `Prepared`/`Sown`/`Growing`/`Ready`/`Spent`, `PlantedTick` and
`ReadyAtTick`; crop visuals remain a projection. Sowing and harvesting are
explicit commands, while both live ticks and `WorldTimeAdvance` resolve the
same absolute readiness boundary. Growth never depends on a scene node or an
assigned citizen remaining attached.

### Screen composition shell

`CityPrototype.tscn` owns one typed `GameUiShell` (`VBoxContainer`) with two
non-overlapping regions: the persistent `CityStatusPanel` and an expanding
`ScreenContent`. Macro, building-detail, and hero-profile screens are siblings
inside `ScreenContent`; they no longer compensate for the status bar with
per-screen top offsets. `GameUiShell` validates both direct slots at startup,
enforces their order, and exposes typed references so the layout is a runtime
contract rather than a node-name convention. Full-screen onboarding and tutorial surfaces remain
outside the shell because they intentionally cover the normal screen flow.

Full views do not scroll as a default composition strategy. Scrolling belongs
to bounded subsections whose data can grow without a practical visual limit
(for example Chronicle entries or citizen assignment lists). The shell keeps
screen chrome stable; each screen must still fit its primary composition in the
available content region and delegate overflow only to the growing subsection.

`CityStatusPanel` renders a small global ticker from `CityStatusSnapshot`:
one resource icon plus its available amount, with stored and reserved totals
in the tooltip. The projection reads `CityResourceLedger`; it does not create
a second inventory or change physical ownership. `BuildingDetailSnapshot`
continues to project the fuller inventory read model consumed by the Shelter's
collapsible resource panel; presentation never queries scene nodes to infer
stock. `StockProduced` and `CropHarvested` remain persisted domain
events for metrics and causal history, while `ChronicleEventProjection` excludes
them from the player-facing Chronicle. Basic ground-resource gathering emits
a transient `ResourceGainPopup` at the current physical owner (founder,
Founding Cache, or Shelter); this feedback is presentation-only and cannot
mutate inventory. Before Cache, the popup samples the founder carrier's world
position at each quantized motion step; building-owned feedback remains fixed
to the projected storage anchor.

`CitySummaryPanel` consumes that same immutable `CityStatusSnapshot` as a
persistent, collapsible left-side read model. It shows the founding lineage,
truthful population/housing occupancy, ledger availability, and every exposed
construction project with work progress and its explicit stop cause. It does
not estimate production deltas or construction duration because no aggregate
rate currently exists. Collapse is presentation-only and never enters
`WorldSave`. The authored macro HUD slots remain non-overlapping: the
`CitySummaryPanel` begins at the left safe margin, with transient
`ContextInspector` immediately to its right and bottom-aligned. The fixed
bottom-centre `PrimaryNavDock` is mutually exclusive with contextual
`ActionDock`. `SimulationControls`, `PlayPauseButton` and the paused speed state
no longer exist. Camera, Speed and Menu share the right-edge utility cluster in
`CityStatusPanel`; Speed cycles 1x / 2x / 4x and Menu never freezes the world.
The macro-only summary siblings start hidden and are revealed/hidden by
`MacroStreetLiveView`'s existing
`ActivatePerspective`/`Deactivate` routing, so full profile/detail perspectives
cannot retain the city rails accidentally; no global HUD manager or runtime
surface construction is involved.

The persistent right-side `ExpeditionRail` consumes an immutable
`ExpeditionRailSnapshot` projected by `CityWorldController`. It exposes only
active domain expeditions, their real members, persisted phase, committed
supplies and authoritative tick interval; it introduces no queue or dispatch
rules. Its embedded `ChroniclePanel` delegates meaningful-event filtering and
compaction to `ChronicleEventProjection`, then uses the existing localized
`WorldEventTextFormatter`. Compact and expanded states share the same rail and
controller. Both section headers stay mounted on the rail's own column at all
times; their two bodies — the expedition scroll and the chronicle scroll —
share a single `AccordionHost`, which keeps exactly one of them visible. That
host is the rail's only vertically expanding child. The arrangement is
deliberate: two `ExpandFill` siblings previously divided one column between
claimants whose minimum sizes moved as their contents folded, and the loser was
squeezed to a near-zero rect while its children stayed `Visible` and undrawn —
measured at 2 px against a 25 px card. Because a Godot `Container` excludes
invisible children from its minimum size, one visible body means there is no
division to lose, and no ancestor needs `QueueSort`, `ResetSize` or
`UpdateMinimumSize` after a toggle. Expansion replaces the expedition summary
and adds offline catch-up summary, grouped blocker decisions and bounded
history. The accordion restores the expedition summary when Chronicle closes,
so neither surface can strand the other in an invisible state.
There is no adjacent or duplicate Chronicle surface.

The observable encounter owns a pure one-dimensional `CombatSpatialState` per
participant inside the deterministic `CombatSession`: `PositionX`, movement
speed, attack range, controlled body radius, facing, Stability and Impulse.
Actors only approach a target until its body envelopes are within
`AttackRange`; there is no reverse/preferred-range branch, so ranged actors do
not kite. Damage remains in `TechniqueResolver`; a resolved hit may displace its
target through centralized Impulse/Stability knockback. Session replay rebuilds
the same spatial state from seed, steps and commands, so schema v34 needs no
second serialized position stream. `ExpeditionStage` and `CombatantView` only
project snapshots/events and may interpolate visual pixels; Godot positions,
animations and missed frames never decide impact or mutate the domain.

Before a Cache exists, the four rudimentary resources are a six-unit founder
load rather than general city storage. `ConstructionSnapshot` projects that
load to the Construction surface; after Cache it projects aggregate 12-unit
site storage, and `BuildingDetailSnapshot` takes over after Shelter
consolidation. Legacy Food/Wood inventory is deliberately excluded from the
pre-Cache carrying headroom. The popup anchor follows the same derived owner,
so this presentation transition adds no persisted location field or schema
version.

## 7b. Citizen visual carrier: one citizen, one sprite

The domain guarantees a single `Citizen` instance per identity. The
visual layer must honour that guarantee — and the design bible confirms
it (see `world-of-goses-design-bible/04_CITIZENS_PROFESSIONS_AND_HEROES.md`)
when it states that no subclass duplication or per-context visual
variant is permitted. The presentation rule therefore is:

> **One citizen → one sprite instance, alive for the citizen's lifetime.**

Views (the macro hero sprite, the building-detail worker slot, the
future hero profile detail, the future expedition cast) **do not create
sprites**. They ask the central registry where the citizen's sprite
should be and animate it to that position. The sprite is a single
object that moves between contexts.

### Concretely

- `CitizenSpriteBank` (autoload) owns a
  `Dictionary<CitizenId, CitizenSpriteCarrier>`. The bank is the only
  place that creates and disposes sprites. Inactive carriers park under the
  bank; the active view mounts the canonical instance into its local visual
  host so normal clipping and scene order apply. Carrier initialization is
  internal to the assembly and stale visuals are hidden before deferred
  disposal, preserving the one-visible-instance invariant during replacement.
- `CitizenSpriteCarrier` (Node2D) wraps one `LineageSpritePlayer` and
  tracks its state — `Home`, `Entering`, `Working`, `Exiting`. It never
  has a duplicate; it is the canonical visual for one citizen.
- A view (slot, macro marker, profile panel) holds a **position** and tells
  the carrier "go to this position" — never "create a sprite here". If the
  active context changes, the single carrier moves or snaps to the new
  position; two contexts cannot render duplicate instances simultaneously.
- The carrier's animation is keyed by `WalkSpeedPxPerSec` (a constant
  derived from the sprite's own animation cadence, not an arbitrary
  duration). Entry and exit durations are `distance / speed`, so the
  visual speed stays consistent regardless of layout.

### Why this matters

- **No duplication.** A worker cannot be visible in two places at once.
  The "re-assign while exiting shows two sprites" class of bugs cannot
  exist by construction.
- **No allocation churn.** Re-entering a building does not instantiate
  a new sprite; it moves the existing one. The sprite's cached state
  (current animation, position) survives context changes.
- **One identity, one visual.** The hero profile, the macro
  hero marker, and the building-detail worker slot all reference the
  same carrier. Splash art, portraits, and stats panels become read
  models over the same `Citizen` instance.
- **Deterministic memory.** The bank caps sprite count at the number of
  citizens that have needed a detailed visual. `PruneExcept` removes stale
  carriers when the active world's citizen set shrinks, so tracked carriers
  remain a subset of current citizen identities. A lineage or gender mismatch
  for a reused id replaces the stale visual instead of retaining it.

### Out of scope for this rule

The rule applies to **ents with persistent identity** (citizens,
eventually named buildings, expedition parties). Anonymous decorations (dust
particles, weather, one-shot celebratory sprites, resource-item effects) do
not get a carrier. Pooling is introduced only when a repeated effect exists
and profiling shows allocation churn; the current prototype has no item-sprite
consumer or pool.

## 8. Local persistence

Local persistence is implemented through plain DTOs and
`WorldPersistence` under `game/scripts/Domain/Persistence/`:

- Domain entities remain free of serialization attributes and file-system
  operations.
- A versioned JSON snapshot stores world state and the last-seen UTC time.
- Writes use a temporary file and preserve the previous snapshot as `.bak`.
- Structural and cross-entity invariants are validated before restore.
- The Godot controller auto-loads the primary slot, auto-saves periodically
  and on window close after onboarding, and starts a new empty world when
  no valid v9 snapshot is available. The controller walks the sequential
  v2 → v3 → v4 → v5 → v6 → v7 → v8 → v9 migrations on raw JSON before `Validate`,
  so older saves upgrade non-fatally: v2 → v3 introduces the reactive
  policy triplet and IronStock; v3 → v4 introduces explicit gender identity
  (defaulting to Masculine when missing); v4 → v5 adds bounded event history;
  v5 → v6 adds durable reservations and explicit building input stock;
  v6 → v7 adds stable per-unit natural-resource reserves and semantic
  citizen resource visits; v7 → v8 introduces persistent `CityParcel` and
  `NaturalResourcePatch` state; v8 → v9 assigns persistent parcel placements
  to projects and buildings. The current chain continues through v28: v21 →
  v22 adds the phased Founding Site state, while v22 → v23 proportionally
  rescales legacy 16×40 founding forests into six finite mature trees with
  eight Wood each; v23 → v24 adds the Cultivation Site lifecycle and timing
  fields, initializing an empty list rather than inventing agricultural
  history in an older city; v24 → v25 converts fixed lots to dynamic frontage
  reservations and initializes an empty protected-corridor collection without
  inventing urban decisions; v25 → v26 persists compact per-unit resource
  positions; v26 → v27 adds finite resource opportunities plus each active
  sortie's opportunity link, exact return tiers and reserved cargo capacity;
  v27 → v28 adds the durable city/Shelter tool set, initially the Primitive
  Axe. A v27 migration initializes an empty set and never grants a tool.
  A resource visit persists exactly one semantic owner — a
  building id or a natural-patch id — together with its unit and logical
  terrain slot, so ground-resource gathering remains valid across save/load.
- The real-time autosave cadence is centralized at three minutes. Periodic and
  close checks skip unchanged worlds; explicit consequential commands
  may still force an immediate atomic save. Save feedback is temporary UI, not a
  persistent HUD status.
- Offline elapsed time is capped and applied as deterministic batched ticks;
  an empty hero-only world uses an equivalent idle fast-forward.

### Material reserves vs. produced-resource storage

`CityResourceLedger` is the common location-aware facade over these physical
stores. It does not copy stock into a global counter: entries retain their
building and storage kind. It provides atomic recipe consumption and runtime
reservations owned by a construction project or an expedition. Schema v7
persists reservations, typed owners, and IDs; restore validates commitments
against physical stock and resumes allocation after the largest retained ID.
Schema v13 reuses the same model for expeditions: the supply cost is held
during the active window and committed on a successful return (or released
on cancel/failure) without moving the goods. The validator rejects any
`Expedition`-owned reservation whose id is not present in
`WorldSave.Expeditions`.
Natural-resource patches persist each visible unit independently and attach it
to a stable parcel. Forest compatibility state still owns gathered Wood stock
while recipes transition to a parcel-independent city store. Citizen visits
are stored as exactly one building/patch domain ID plus a logical terrain slot
rather than viewport coordinates. The logical slot remains valid when a visible
unit is depleted or its presentation disappears.

`Building` carries two distinct counters so the operating-recipe drawdown
does not visually shrink the produced-resource amount:

- `Stock` is the building's produced-resource output (Stone, Food, etc.).
- `IronStock` remains persisted for schema compatibility and future tools/fuel
  work, but no current early-game construction or operating recipe consumes it.
- `WoodReserve` is the Forest-style remaining source, drained by the
  manual "Gather wood" action into the Forest's `Stock`, which the
  construction recipe gate then consumes.

`TryConsumeResource(type, amount)` can still route Iron to `IronStock`, Wood to
gathered Forest `Stock`, and everything else to produced `Stock`. Current recipe
drawdown uses Wood and Food; the Iron path is retained without making it a
bootstrap requirement.

### Causal event log

`WorldEvent` carries a typed subject kind, optional entity ID, captured display
label, and typed `CauseEventId`. `CityWorld.FindCauseEvent` compares building
identity rather than display names. The
`ChroniclePanel` renders a "Decisions needed" list grouped by subject
above the chronological rows so the player can scan what requires attention
without scrolling the full timeline.

`OfflineProgressionReport` carries both aggregate counters and causal events
generated during catch-up. Schema v5 persists at most 128 significant events.
Per-tick production/progress and day/night noise are excluded; repeated adjacent
steady states are compacted, and causes outside the retained subset are cleared.
Restore preserves retained IDs and resumes allocation after the largest ID.
Schema v13 introduces four persistent kinds: `ExpeditionDispatched`,
`ExpeditionReturned`, `ExpeditionFailed`, and `ExpeditionCancelled`. The
expedition's dispatch event id is captured on `Expedition.SetDispatchEventId`
and reused as the `CauseEventId` of the matching return or failure, so the
Chronicle surfaces a one-row chain per expedition. Schema v14 adds
`MigrantArrived` and retires the `BuildingKind.Forest` building entity;
wood lives in `NaturalResourcePatches` and `CityInventory`, so the migration
keeps existing reserves without losing the player's gathered stock.
Schema v15 persists 1-2 expedition members, v16 persists the phase and
deterministic encounter result, and v17 requires every member to hold the
accumulated Hero role. Schema v18 persists the retreat posture and retained
dispatch event id. Legacy v17 plans continue after a setback; new plans may
instead enter `Retreating`, keep their citizen commitments through the return
leg, commit supplies, and resolve as `Retreated` without an objective reward.
Quiescent offline advancement batches active expeditions only to their next
persisted phase boundary, then runs the same phase transition/resolution used
by live ticks. Their dispatch events are pinned inside the 128-event persisted
window until resolution, so encounter, retreat, return, and pre-travel
cancellation keep a causal parent across save/load.
Schema v19 persists a citizen wound independently from stamina, including its
severity, originating event, and remaining treatment ticks. It also replaces
the parcel unlock boolean as the authoritative runtime state with
`Locked → Reconnoitred → RouteSecured → Available` while retaining the legacy
boolean in the DTO for migration. Recovery commitments reference Basic Shelter;
active wound origins remain pinned until treatment completes. Live and offline
ticks share the same recovery and territory-resolution behavior. Quiescent
catch-up subtracts recovery in bounded batches up to the next treatment or
day/night boundary. The first successful reconnaissance emits the three legal
parcel transitions as one ordered causal chain so that its return immediately
opens the new construction lot; later content may put those transitions behind
separate route requirements.

### Recruitment

`CityWorld.TryRecruitMigrant(CitizenProfile, string?)` is the first public
route for a non-hero citizen. It allocates a fresh `CitizenId` beyond every
existing citizen, instantiates a `Citizen` with the founder's profile, places
it at `AtHome` without assignment, and publishes `MigrantArrived`. The
controller exposes a `TryRecruitMigrant` wrapper with autosave and the
`CitizensChanged` signal so the UI can refresh rosters and macro views. The
roster view and assignments are presented through the existing
`AssignmentPanel` and `BuildingDetailView`; a dedicated `MigrantPanel`
mediates the action via `ModalHost`, reusing the same focus chain as the
expedition panel. Recruitment is the foundation for the next slice:
expeditions returning with a `MigrantArrived` outcome instead of a generic
`Stone` reward, and a `RosterView` that lists every non-hero citizen with
competency, stamina, and assignment.

### Boundary enforcement

Domain events retain only causal and semantic data. Presentation owns both
player-facing copy through `WorldEventTextFormatter` and the `WorldEventKind` →
icon mapping through `IconPaths`; asset paths and localized text never travel
through `WorldEvent`. `DomainBoundaryTests` scans every C# source below
`game/scripts/Domain/` and fails if it finds a Godot reference or `res://` path,
turning the domain/presentation rule into an executable constraint.

### Presentation snapshots

`CityWorldController` projects the mutable world into immutable,
Godot-free read models: `CityStatusSnapshot`, `ConstructionSnapshot`, and
`BuildingDetailSnapshot`; `CityMacroSnapshot` additionally projects every
`NaturalResourcePatch` type, its independently gatherable units, and each
Cultivation Site's state/timing on its stable lot. `CityStatusSnapshot`
projects daily ration, Food horizon, protected target, time to first harvest,
ledger-backed resource totals/availability, founding lineage, and current
population/housing capacity. The status
strip, construction panel, building-detail
shell, worker slots, assignment panel, production panel, and forest gather panel
render those snapshots instead of traversing `CityWorld` or retaining domain
entities. Commands still flow through the controller and domain; snapshots are
read-only copies and never become a second source of truth. The v14 slot
enumerates every citizen through the same `CityMacroSnapshot.CitizenItem`
record, so the future `RosterView` will render without re-querying
`CityWorld`. Domain `PatchChanged` events cross the controller as
`NaturalResourceStateChanged`; the macro view rebuilds its snapshot after a
successful gather, while the controller marks the world dirty for autosave.
`CultivationSiteChanged` crosses as `CultivationSiteStateChanged`; sow and
harvest wrappers save immediately and the macro view rebuilds from a fresh
snapshot.

### Founder narrative boundary

The astral onboarding keeps authored content and hidden weights in
`FounderNarrativeCatalog`, stable answers in `FounderNarrativeSession`, and
full recomputation in `FounderNarrativeScorer`. These domain types do not
import Godot. `AstralOnboardingView` owns layout, focus, progressive board
reveal, and text fades. `FounderArrivalSequence` owns only the fall, placeholder
impact, and title-card presentation.

The result enters the world through `HeroCreationRequest` and creates one
ordinary `Citizen` with the Hero role. `CitizenOrigin.AstralFounder` is compact
persisted metadata, not a parallel founder entity. The first free
`ConstructionLot` remains the authoritative fall/building-site relation.
`HeroCreated` is emitted only after the initial atomic save succeeds; a failed
save keeps the answer session and retries without creating a second citizen.

## 9. UI themes and resolution

The project uses one shared Control hierarchy and one global `Theme`; lineage
identity never duplicates scenes or functional controls. `LineageThemeRegistry`
loads the exported `StyleBoxTexture` resources under
`res://assets/ui/lineages/<lineage>/`, caches them, and resolves missing
components through the same-lineage `panel` before falling back to the existing
project theme. `LineageThemeSignals` is the presentation-only autoload that
notifies visible controls when the founder's persisted lineage changes.

Fonts and icons remain independent of lineage. Geist Pixel is used for display
titles, Jersey 10 for headings, and Pixelify Sans for controls and reading text.
Pixelify Sans is imported with grayscale antialiasing, light hinting, disabled
subpixel positioning, and fixed 1.0 oversampling. The reference viewport is
explicitly 1280×720 with `canvas_items` stretch and `expand` aspect handling.

## 10. Planned event-based simulation

`WorldTimeAdvance` is now the single domain seam used by offline catch-up to
advance an elapsed tick range. It performs one batch for a world without
buildings or projects. A structured but quiescent city (no work assignments)
also batches all ticks that stay within the current day/night phase: upkeep,
WellFed expiry, stop causes, and the clock are applied arithmetically, while
sunrise/sunset remain canonical stepped ticks. Completed projects, forests due
for demolition, and assigned workers force the canonical
`CityWorld.AdvanceWorldTick` path — which since A2 is the *only* tick method, so
"catch-up" now differs from live play in batching strategy alone, never in
rules. A citizen in transit clamps the batch to one tick before their arrival,
so the arrival itself is always reached by a stepped tick; batching onto it
would advance the clock past a scheduled state change without running it.
Snapshot JSON and the full causal-event sequence are tested for equivalence over
multiple days. New strategies belong
behind this seam and need the same proof before replacing per-tick execution.
Offline reports capture a log cursor before the batch, so new event kinds do
not require category-specific counters and cannot replay older events.

The current offline simulation batches deterministic ticks. Its intended
evolution is event-based. The v13 reconnaissance slice is the first domain
feature to lean on that boundary: `CityWorld.CompleteFinishedExpeditions` is
invoked from `AdvanceWorldTick`, so live and offline progress through the
same canonical tick. The reservation is committed deterministically (the
reward amount is `Min(targetAmount, reservationAmount)` and never rolls);
the only event-driven slice left undone is the scheduler that would let
cities skip ahead to the next interesting tick without iterating. The
expected event kinds, the `CauseEventId` wiring, and the persistence
contract are all in place; the remaining work is the active-world
event-driven scheduling itself.

- The world does not tick every real second.
- Discrete events are produced as time elapses (e.g. "one armor set
  completed", "coal ran out", "an expedition returned", "the hospital
  reached critical capacity"). The v13 reconnaissance slice already
  publishes `ExpeditionDispatched`/`Returned`/`Failed`/`Cancelled` through
  this same log; only the time-jump scheduling remains.
- Events are causal — each event refers to the state that caused it.
- The event log is the source of truth for the causal report.

This section is the evolution contract. The current consolidation provides the
advance seam and event cursor only; active-world event scheduling remains a
future slice and must be introduced incrementally.

## 11. What is explicitly out of scope

The following are out of scope for the initial architecture and any
system work that follows:

- A backend, a server, a database, a microservice, an API, a CDN.
- A mobile application, even as a future stub.
- Multiplayer, networking, account systems.
- Modding tools, custom editors, Godot plugins beyond standard use.
- Telemetry, analytics, crash reporting, A/B testing.
- A second gameplay loop, a second city, a meta layer between cities.
- A graphical installer, a launcher, a settings UI.

These are listed so that the next agent or contributor does not
"helpfully" add them. The README and `AGENTS.md` repeat the same
boundary.

## 12. Evolution of the architecture

The architecture is allowed to evolve. The rules for evolving it are:

1. **The simulation must remain independent of the engine.** That is
   the strongest boundary. Domain code does not import `Godot.*`.
2. **Documentation must change with the architecture.** A change to the
   folder layout, dependency rule, or build command must be reflected
   in `README.md` and the relevant `docs/` file in the same commit.
3. **No speculative abstractions.** A pattern is added when there is a
   concrete need in code, not before.
4. **No premature systems.** Mobile, networking, and the full city are not
   built before the prototype validates the need.
