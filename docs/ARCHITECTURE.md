# Architecture

> The initial architecture of World of Goses. This document describes the
> current project layout and the small set of boundaries that the rest of
> the code will be written against. It does not commit to systems that
> are not yet validated by a prototype.

The conceptual design bible lives at
[`docs/world-of-goses-design-bible/`](world-of-goses-design-bible/README.md);
[`world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md`](world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md)
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
| Expedition-detail  | Fully detailed side-facing sprites, frame-by-frame animation.           | Future                  |

Concretely:

- **Macro.** A city view uses one small marker per current citizen. Markers
  are not individually interactive yet, but their count is derived from the
  domain rather than from a fixed population fixture.
- **Building-detail.** When a building is selected, the view opens
  with one visible worker per *visual capacity* slot. Every visible
  worker corresponds to a real `CitizenId`. Workers that are assigned
  but exceed the visual capacity are reported as "working inside" by
  the domain; the presentation layer surfaces this number verbatim.
- **Expedition-detail.** Not implemented yet. Future expedition scenes
  will use fully detailed side-facing sprites authored as
  Pixelorama sprite sheets (`art/source/characters/...`) and driven by
  Godot `AnimatedSprite2D` with `AnimationPlayer` transitions.

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
  place that creates and disposes sprites.
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
  no valid v4 snapshot is available. The controller's load path runs
  `MigrateV2ToV3` then `MigrateV3ToV4` on the raw JSON before `Validate`,
  so older saves upgrade non-fatally: v2 → v3 introduces the reactive
  policy triplet and IronStock; v3 → v4 introduces explicit gender identity
  (defaulting to Masculine when missing).
- Offline elapsed time is capped and applied as deterministic batched ticks;
  an empty hero-only world uses an equivalent idle fast-forward.

### Material reserves vs. produced-resource storage

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

`WorldEvent.CauseEventId` is wired through `CityWorld.FindCauseEvent`. The
`OfflineReportPanel` renders a "Decisions needed" list grouped by subject
above the chronological rows so the player can scan what requires attention
without scrolling the full timeline.

`OfflineProgressionReport` carries both aggregate counters and the causal events
generated during catch-up. The current `WorldEventLog` is bounded and in memory:
restore clears it, it is not part of `WorldSave`, and offline catch-up repopulates
it. Persistence/streaming for a long-horizon history and the final relationship
between real time and world time remain undecided.

### Boundary enforcement

Domain events retain only causal and semantic data. Presentation owns the
`WorldEventKind` → icon mapping through `IconPaths`; asset paths never travel
through `WorldEvent`. `DomainBoundaryTests` scans every C# source below
`game/scripts/Domain/` and fails if it finds a Godot reference or `res://` path,
turning the domain/presentation rule into an executable constraint.

### Presentation snapshots

`CityWorldController` projects the mutable world into immutable,
Godot-free read models: `CityStatusSnapshot`, `ConstructionSnapshot`, and
`BuildingDetailSnapshot`. The status strip, construction panel, building-detail
shell, worker slots, assignment panel, production panel, and forest gather panel
render those snapshots instead of traversing `CityWorld` or retaining domain
entities. Commands still flow through the controller and domain; snapshots are
read-only copies and never become a second source of truth.

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

The current offline simulation batches deterministic ticks. Its intended
evolution is event-based:

- The world does not tick every real second.
- Discrete events are produced as time elapses (e.g. "one armor set
  completed", "coal ran out", "an expedition returned", "the hospital
  reached critical capacity").
- Events are causal — each event refers to the state that caused it.
- The event log is the source of truth for the causal report.

This section is a contract for the future architecture. It is not an
implementation plan. The first prototype does not need it.

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
