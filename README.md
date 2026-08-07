# World of Goses

> **Status:** EG-5 — consolidation. The current slice is the EG-A0
> opening: twelve-fragment astral onboarding that produces one
> Kovari-Cube founder, an authored first night (00:00 → 06:00) where
> a fire spirit teaches why ground materials matter, a Founding Site
> lifecycle that grows from Campfire into Basic Shelter through three
> authored modules, one Cultivation Site, the first resource
> expeditions (SpiritTrail and FallenWood), the deterministic
> combat/expedition vertical slice, and Town-Hall recruitment behind
> a housing gate. Local persistence, offline progression, eight
> lineage-driven panel skins, and the dynamic frontage grid from
> EG-4 sit underneath. Schema `WorldSave.CurrentVersion` is **31**;
> the xUnit suite measures **913 passing**, 1 skipped (known JSON
> snapshot brittleness).
>
> **Before authoring any new screen or widget, read
> [`docs/UI_PATTERNS.md`](docs/UI_PATTERNS.md).** It codifies the
> three reusable UI patterns (PackedScene, `[GlobalClass]`, static
> factory), the signal-based state binding rule, the 3-font theming
> hierarchy, save/load integration, navigation/focus, anti-patterns,
> and a per-PR audit checklist. Every UI PR follows it; every UI
> shortcut that bypasses it reproduces the divergent widget
> definitions we already paid to consolidate.

A persistent pixel-art desktop game about a single living city. The world
continues advancing while the game is closed, and the player guides its
development through institutions, production, and expeditions — without
directly controlling every action.

---

## 1. Project status

This repository currently contains:

- A playable Godot `.NET` prototype in `game/`.
- The `art/` source and export directories for pixel art.
- Domain and persistence tests under `tests/`.
- Design, architecture, validation, direction, and status documents in `docs/`.
- The `README.md`, `AGENTS.md`, and `.gitignore` at the repository root.

The current implementation handoff and next recommended slice are maintained in
[`docs/CURRENT_STATUS.md`](docs/CURRENT_STATUS.md).
UI changes use the capture and human-review contract in
[`docs/VISUAL_REGRESSION.md`](docs/VISUAL_REGRESSION.md); headless boot alone is
not accepted as visual evidence.

## 2. Game vision

The player governs one persistent city. There is no meta-progression between
cities and no bonus for restarting — to begin again, the player must delete
the current city or use a different account. The only thing that transfers
between playthroughs is the player's accumulated knowledge.

The city continues to advance while the game is closed. Player absence does
not apply artificial penalties. The world executes previously authorized
orders, configured policies, production chains, medical treatments, approved
construction, active expeditions, inventory replenishment, and citizen
training. It does not make sovereign decisions that belong to the player
unless that authority has been explicitly delegated through institutions or
protocols.

The full design vision, principles, pillars, lineages, audio and
visual identities are documented in the design bible at
[`docs/world-of-goses-design-bible/`](docs/world-of-goses-design-bible/README.md).
The bible is the **single source of truth** for *what the game is*;
do not duplicate its content into other docs.

The **process guide** for how to validate, sequence, and review slices
is in [`docs/PRODUCT_DIRECTION.md`](docs/PRODUCT_DIRECTION.md).
The current implementation status and next starting point are in
[`docs/CURRENT_STATUS.md`](docs/CURRENT_STATUS.md).
The implementation architecture is in
[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).
The **UI patterns** rulebook for any new screen or widget is in
[`docs/UI_PATTERNS.md`](docs/UI_PATTERNS.md).
The current UI state and the manual checklist live in
[`docs/UI_AUDIT.md`](docs/UI_AUDIT.md).
The pixel-art file flow is in [`docs/ART_PIPELINE.md`](docs/ART_PIPELINE.md).
The honest cross-check against the bible is in
[`docs/VALIDATION.md`](docs/VALIDATION.md).
A single map of every doc lives in
[`docs/README.md`](docs/README.md).

`docs/GAME_VISION.md`, `docs/LINEAGES_AND_PROFESSIONAL_AFFINITIES.md`,
and `docs/DESIGN_INFLUENCES.md` are **pointer files** that map their old
sections to the bible. They are kept only because `AGENTS.md` and
historical commit messages cite them.

## 3. Gameplay pillars

### 3.1 City development

A multi-dimensional evaluation of the city, **not** a single overall level.
Development is measured across independent dimensions including age and
historical continuity, cultural development, political development, economic
development, geographic development, demographic complexity, professional
coverage, knowledge redundancy, institutional capacity, and generational
transmission of experience.

Buildings are not unlocked by an arbitrary level. They require real
conditions: knowledge, personnel, supplies, infrastructure, administration,
economic capacity, and political decision.

A society may become agricultural, academic, mercantile, industrial, nomadic,
military, raider-based, or an emergent combination. The game does not impose
a single correct model of development.

### 3.2 Expeditions

Expeditions are automatic. There is no direct combat control. The player
configures members, roles, positioning, target priorities, automatic skill
usage, retreat policy, equipment, supplies, route, objective, and survival
priorities.

Expeditions may explore, expand territory, contact other settlements,
recruit migrants, respond to threats, discover knowledge, obtain material
samples, find exploitable resources, negotiate access to technologies, learn
about policies and institutions, and generate historical opportunities for the
city. They are not an infinite source of loot. Equipment depends on
materials, technological capacity, known designs, artisan experience,
manufacturing quality, and city logistics.

## 4. Technology stack

| Layer            | Choice                                                         |
| ---------------- | -------------------------------------------------------------- |
| Engine           | Godot `.NET` (Godot 4.7.x)                                     |
| Language         | C# (`.NET 8.0` baseline; `net9.0` for Android exports)         |
| Editor           | Visual Studio Code                                             |
| Pixel art tool   | Pixelorama                                                     |
| Primary OS       | Windows                                                        |
| Terminal         | PowerShell 7                                                   |
| Initial storage  | Local                                                          |
| Backend          | **Not implemented yet**                                        |

The first playable target is the Godot project inside `game/`. Art is created
in `art/source/`, exported to `art/exports/`, and imported into
`game/assets/`.

## 5. Target platforms

- Windows
- Linux
- macOS

A companion mobile application for Android and iOS may be developed later.
The mobile application will allow players to observe and manage selected
systems, but it will not run the full game.

## 6. Development requirements

- Godot 4.7.x (`.NET` build) — <https://godotengine.org/download>
- `.NET` SDK 8.0 (or newer) — <https://dotnet.microsoft.com/download>
- Visual Studio Code with the C# Dev Kit and Godot Tools extensions
- Pixelorama — <https://orama-interactive.itch.io/pixelorama> (art only)
- PowerShell 7 (Windows terminal)

## 7. How to open the project

1. Clone this repository.
2. Install the requirements listed above.
3. Open Godot 4.7 `.NET`.
4. Choose **Import** and select `game/project.godot`.
5. Open the project in Visual Studio Code if you intend to write C#.

## 8. How to compile or run it

From a PowerShell 7 terminal, in the `game/` directory:

```powershell
dotnet build
```

To run the project, open `game/project.godot` in Godot and press **F5**.

There is no automated end-to-end gameplay test yet. The current verification
target is a successful `dotnet build`.

## 9. Running the tests

```bash
dotnet test
```

Validate the English/Spanish gettext catalogs and ensure `messages.pot` is
up to date from the repository root:

```powershell
.\tools\Test-LocalizationCatalog.ps1

# Run only after intentionally adding or removing catalog entries.
.\tools\Test-LocalizationCatalog.ps1 -UpdateTemplate
```

The domain layer (`Building`, `Citizen`, `BuildingProductionCalculator`,
`CityWorld`) is fully covered by xUnit. Visual / interaction-layer
behaviour (`MacroStreetLiveView`, `BuildingDetailView`) is exercised
manually with **F5** in Godot and not by automated tests in this slice.

## 10. Repository structure

```text
world-of-goses/
├── .git/
├── AGENTS.md
├── README.md
├── .gitignore
├── docs/
│   ├── README.md                                  # consolidated doc index
│   ├── CURRENT_STATUS.md                          # current slice, next proof
│   ├── ARCHITECTURE.md                            # engine/domain boundary
│   ├── ART_PIPELINE.md                            # Pixelorama → PNG → Godot
│   ├── VALIDATION.md                              # cross-check vs bible
│   ├── PRODUCT_DIRECTION.md                       # process guide
│   ├── GAME_VISION.md                             # pointer → bible
│   ├── LINEAGES_AND_PROFESSIONAL_AFFINITIES.md    # pointer → bible
│   ├── DESIGN_INFLUENCES.md                       # pointer + audit trail
│   └── world-of-goses-design-bible/               # canonical design source
├── art/
│   ├── source/
│   │   ├── characters/
│   │   ├── buildings/
│   │   ├── terrain/
│   │   ├── effects/
│   │   └── ui/
│   ├── references/
│   └── exports/
├── game/
│   ├── project.godot
│   ├── World of Goses.csproj
│   ├── World of Goses.sln
│   ├── assets/
│   │   ├── characters/
│   │   ├── buildings/
│   │   ├── terrain/
│   │   ├── effects/
│   │   ├── audio/
│   │   └── ui/
│   ├── scenes/
│   ├── scripts/
│   ├── icon.svg
│   └── (other files generated by Godot and .NET)
└── tests/
    └── WorldofGoses.Tests/
```

The Godot project is intentionally isolated under `game/`. Pixel art sources
live under `art/source/` and exports under `art/exports/`. The final,
imported assets for the Godot project live under `game/assets/`.

## 11. Pixelorama → PNG → Godot art workflow

1. **Author** sprites, frame-by-frame animation, tilesets, buildings, effects,
   icons, and UI elements in Pixelorama.
2. **Save** the editable source as `.pxo` (or `.pxm`) files in
   `art/source/<category>/`.
3. **Export** the visual output as PNG or sprite sheets into
   `art/exports/<category>/`.
4. **Import** the exported PNGs into the Godot project under
   `game/assets/<category>/`. Configure the Godot import settings (filter,
   mipmaps, frames per row) from the Godot editor.
5. **Wire** the imported resources into `SpriteFrames`, `TileSets`, and
   scenes, with `AnimatedSprite2D`, `TileMapLayer`, `AnimationPlayer`, and
   particles as appropriate.
6. **Drive** the visual representation from C# logic. C# selects the current
   animation/state and Godot renders it.

Naming conventions, file layout, and import rules are detailed in
[`docs/ART_PIPELINE.md`](docs/ART_PIPELINE.md).

The conceptual rule is:

> Pixelorama defines how it looks.
> Godot defines how it is represented and animated.
> C# defines what is happening and why.

## 12. Basic conventions

- **Naming**: PascalCase for C# types and methods, camelCase for parameters
  and locals, `_camelCase` for private fields. Filenames mirror type names.
- **Encoding**: UTF-8, LF line endings (see `.gitattributes`).
- **Domain logic**: lives in C# classes, never inside visual nodes that can be
  separated.
- **Composition over inheritance**: prefer small composable parts over deep
  class hierarchies.
- **Records and value objects**: used where data is structural rather than
  behavioral.
- **No magic strings**: define constants for asset paths, scene names, group
  names, and input actions.
- **No speculative abstractions**: do not introduce patterns without a
  concrete current need.
- **No premature systems**: do not implement networking, mobile, or other
  speculative systems before the prototype validates the need.
- **UI follows `UI_PATTERNS.md`.** Every new screen, modal, button, chip, or
  row goes through one of the three patterns declared there, with explicit
  `theme_type_variation`, signal-driven state binding, and the close-path
  matrix that the modal/focus rules demand.

## 13. First prototype scope

The first prototype is **not** the complete city. It is a small vertical
slice that may evolve into the full game. The canonical proposal that
defines this slice lives at
[`docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`](docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md);
its §17 is the acceptance test EG-5/EG-6 must pass before any broader
product slice is approved.

The current slice demonstrates:

- **Twelve-fragment astral onboarding** that produces exactly one
  principal `Citizen` (hero role). The output is the canonical
  onboarding result — lineage, elemental affinity, narrative memory
  and the three Kovari-Cube axes (Cuerpo/Vínculo,
  Estabilidad/Impulso, Dominio/Alcance) as two-pole bars — and nothing
  else (DEC-0013).
- **Authored first night** between `00:00` and `06:00`, where a fire
  spirit teaches why ground materials matter. Nine stages advance
  **only** on world facts (a completed module, a closed dialogue
  node), never on the clock. The tick never freezes; the dawn is
  the stage transition, the displayed hour stalls at `05:59` while
  the night runs, and the whole calendar defers while the night is
  live (no ration, no day boundary). A `Bedroll` finally has
  mechanical meaning: without it the night refuses to sleep.
  Existing saves enter with the night **already concluded**, so a
  played opening is never asked to repeat milestones it has passed.
- **Founding Site lifecycle**: Campfire → Bedroll/Cache → Canopy on
  one persistent site identity and parcel, with deposit, contributor
  assignment, pause/resume and offline progression. Adding the
  Canopy consolidates the same site into the Basic Shelter without
  spawning a new building.
- **Basic Shelter, Farm, Quarry, Town Hall** as placeable construction
  projects, plus a single **Cultivation Site** that becomes available
  after the Shelter (1 Branch + 1 Small Stone, 180 preparation work,
  1 Food seed, exact three-day growth, 5 Food harvest).
- **Resource expeditions on a dynamic frontage grid**: SpiritTrail
  (unlocked at dawn) and FallenWoodSearch share a finite opportunity
  curve with bounded return capacity; dispatch reserves supply and
  opportunity, completion depletes them, retreat releases them.
- **Deterministic combat/expedition vertical slice**: a per-encounter
  working copy (`CombatantState`) carries the real `CitizenId`,
  channel coefficients obey a fixed-budget invariant so an evolution
  redistributes instead of grants, and the encounter, route decision
  and return resolve end-to-end inside the domain. The pre-EG-4
  `Expedition` resource timer is still separate; consolidating the
  two is the main technical debt of this slice.
- **Recruitment through the Town Hall**: a prospect persists, is
  bounded by housing capacity, and is accepted into the city only
  when capacity is free. Recruited citizens use the same visual
  travel system as the founder; mid-transit load reconstructs
  elapsed progress.
- **Individual citizen records** shared between the views. A citizen
  is the only person entity in the domain: roles, competencies,
  recognitions, lineage, combat nature and profile choices are
  attached state, not subclasses. Each citizen carries
  `CurrentStamina`, a `WellFedRemainingTicks` buff counter, an
  authoritative `Commitment`, a durable `WorkOrder`, a
  `CurrentLocation` and one persistent moderate wound with Basic
  Shelter treatment (1 Food, 3600 ticks).
- **Eight runtime-selectable lineage panel skins** backed directly by
  exported `StyleBoxTexture` resources, with deterministic fallbacks
  and a showcase scene.
- **A versioned local snapshot** (`WorldSave.CurrentVersion = 31`)
  with bounded causal history, atomic write with `.bak`, and a
  location-aware resource ledger whose typed reservations survive
  close/offline progression. Offline progression runs before visual
  instantiation and uses the same domain transitions as live
  advancement.
- **The existing building-detail, assignment, stamina, production,
  day/night, upkeep and offline systems** as reusable domain
  concepts and explicit test scenarios rather than new-game seed
  data.

The architecture establishes three conceptual visual scales — macro,
building-detail, and expedition-detail — although only the first two
are implemented now. Future expedition scenes will use fully detailed
side-facing sprites and frame-by-frame animation; that work is
explicitly out of scope for this slice.

The priority of the slice is to validate the boundary between the
pure-C# domain and the Godot presentation layer, and to confirm that
the architecture supports later additions (more buildings, more
citizen competencies, multiple workplaces, expedition scenes) without
re-architecting.

## 14. Short initial roadmap

The opening is sequenced as
`EG-0 → EG-1 → EG-2 → EG-3 → EG-4 → EG-5 → EG-6` and is defined by
[`docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`](docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md)
§15. The xUnit suite measures **913 passing**, 1 skipped (known JSON
snapshot brittleness); the schema is **v31**; the agent-context
validator runs **448 checks**; the EN/ES gettext catalogs cover
**918 template IDs** and **283 runtime keys**.

The following increments have landed on top of the early persistence
and offline-progression spine (items 1–8) and are now part of the
connected baseline. Items 9 onward sit above that spine.

1. ✅ **Repository** — Confirm structure, build, and documentation.
2. ✅ **First prototype scene** — Building macro/detail navigation + worker slots.
3. ✅ **Domain layer** — `Citizen` / `Building` / `CityWorld` with composable attachments and explicit hero onboarding.
4. ✅ **Persistence boundary (Slice A)** — Serialize `CityWorld` to validated,
   versioned JSON; auto-load and auto-save without exposing serialization
   concerns on domain entities. Schema v7 also preserves stable natural-resource
   units and semantic citizen visits without serializing Godot coordinates.
5. ✅ **Offline progression (Slice B)** — Track `lastSeenAt`; on launch,
   advance the world by N ticks equal to elapsed time (capped). Tests:
   tick arithmetic, production accumulates, experience carries. This is
   the **defining** feature of the game per the design bible's
   *Persistencia* section.
6. ✅ **Multi-building expansion (Slice C)** — Quarry and Farm use distinct
   resource and competency data; the macro view selects either building.
7. ✅ **First MVP pixel art** — Replace `building_placeholder.png` and
   `worker_placeholder.png` with the first Pixelorama batch. Slot into
   `BuildingPlot` and `VisibleWorkerSlot` without re-anchoring. The
   first three building PNGs (`home_idle`, `quarry_idle`, `farm_idle`)
   are in place; the worker sprite is intentionally absent until a
   real `worker.pxo` lands, and `VisibleWorkerSlot` renders empty
   instead of crashing.
8. ✅ **End-to-end validation** — Run the prototype against the acceptance
   criteria of the design bible (`docs/world-of-goses-design-bible/`);
   `docs/VALIDATION.md` flags any drift.
9. ✅ **Stamina-gated production (Slice D)** — `Citizen.Stamina`,
   per-tick cost, food-driven regen, `WorkersExhausted` cause. Quarry
   and Farm both consume stamina.
10. ✅ **Day, Night, and Upkeep (Slice E)** — Shared world clock at 1 Hz,
    day/night cycle, passive stone upkeep scaled by population, WellFed
    stamina buff that decays per tick and resets when the citizen eats.
11. ✅ **Citizen Mobilisation (Slice F)** — `Citizen.CurrentLocation` is
    separate from `CurrentAssignment`. Sunset moves everyone to Home;
    sunrise returns assigned citizens to work. Save restore seeds the
    initial location from the loaded tick so the visualisation matches
    the clock on the first frame after a load.
12. ✅ **Interruptible citizen work** — a player-authored `Citizen.WorkOrder`
    survives temporary expedition and life-support interruptions, while
    `Citizen.Commitment` represents the mutually-exclusive current engagement.
    Food/rest may suspend and later re-evaluate work but never choose a new job.
    Assignment, gathering, expedition dispatch, save validation, and UI-facing
    availability reasons now consume the same domain state; older v14 saves
    infer it from their existing relationships.
13. ✅ **Arrival-gated work** — daytime assignment reserves the citizen and
    marks them `InTransit`; production, stamina cost, experience, construction
    progress, and full-stock pausing wait for semantic arrival. Godot can
    confirm its route on physical arrival; only offline catch-up uses the
    deterministic domain travel duration, so live ticks cannot hide or start
    producing with a citizen who is still visibly walking.
14. ✅ **Batched founding economy** — the one-second world clock no longer
    represents a completed labour action or meal. Farm, Quarry, and assigned
    gathering resolve every 10 seconds; citizens eat at home when recovery or
    the night meal cadence requires it, and production UI reports amounts per
    batch. Founding Farm/Quarry storage is 60/80, including a one-time
    additive rebalance for older snapshots.
15. ✅ **Ravatha / Cubo Kovari / onboarding consolidation** — the
    three delivered packages fold into the design bible as chapters
    13–21 (`bible/13_KOVARI_CUBE.md` plus one chapter per lineage).
    The eight line signatures (Anclaje, Corola, Reconfiguración,
    Rumbo, Custodia, Adaptación, Resonancia, Síntesis) are canonised;
    `FounderOnboardingResult` is reduced to lineage, elemental
    affinity, cube profile and narrative memory (DEC-0013). The old
    `.zip` packages live under `docs/_archive/ravatha-source-2026-08-04/`.
16. ✅ **Derived stats with auditable breakdown** — each `Citizen`
    exposes derived physical/elemental power, life, defences,
    mitigations, regeneration, healing and tempo stats from a single
    domain query, with the contributing sources listed. Equipment
    acts on channels (Weight, Demand, MaxIntegrity, CurrentCondition,
    ElementalResonance, ElementalTolerance, WearProfile), never as a
    power multiplier. Schema v30 carries the persistent cube sources;
    v29 carries the canonical onboarding result.
17. ✅ **EG-4 — resource expeditions on a dynamic frontage grid**
    (v24 → v28). Fixed nine-lot parcels become continuous frontage
    rows with persisted corridors; resource units occupy only their
    own cell instead of claiming the surrounding 3×3 lot. Campfire
    and Cache each expose one finite Food and one finite Wood
    opportunity; dispatch reserves supply, opportunity and bounded
    return capacity; the durable Primitive Axe is craftable at the
    Shelter from 1 Branch + 1 Small Stone.
18. ✅ **Combat / expedition vertical slice** — a per-encounter
    working copy carries the real `CitizenId`; channel coefficients
    obey a fixed-budget invariant so an evolution redistributes
    instead of grants; the encounter, route decision and return
    resolve end-to-end inside the domain, deterministic from a seed
    and reachable in-engine via the `combat-debug` fixture.
    Provisional balance is centralised in `CombatBalanceConfig`;
    consolidating this slice with the pre-EG-4 resource expedition
    is the next debt, not part of it.
19. ✅ **Authored first night (state)** — `FirstNightSave` lands in
    v31; the night stages and the open dialogue node persist so a
    loaded save resumes on the same line. Existing cities enter
    with the night **already concluded**.
20. ✅ **Authored first night (playable)** — fire spirit dialogue
    catalog (six main nodes, eight lineage variants per node, 48
    `firstnight.*` keys), non-modal `FirstNightDialogueStrip` on
    `OverlayLayers.Tutorial`, fire spirit visual whose position is
    derived from the stage and never persisted, contextual
    `FirstNightContextCommentary` while gathering, dawn emits
    `WorldEventKind.SpiritDeparted` once, embers primitive over the
    campfire after departure, and `SpiritTrailSearch` resource
    opportunity unlocks for the first expedition. Bible chapter 23 is
    accepted as canonical (DEC-0014).
21. 🔵 **EG-5 — consolidation (active)** — second/third Cultivation
    Site and Farm consolidation; the first forestry capability is
    the durable Primitive Axe, already wired through EG-4 and v31.
    Once EG-5 → EG-6 are accepted, the persistent wound/treatment
    loop (deferred from VS-3) is the next product objective.
22. ⏭ **EG-6 — calibration/signature** — closes the EG-A0 acceptance
    test defined by the proposal §17 (recruitment and Food pressure
    player-facing calibration; founding-site signature). Required
    before any broader product slice is approved.

This list is not a contract. Items may be reordered, dropped, or expanded as
the prototype teaches us what the project actually needs.

## 15. Founding hero and first night

The canonical entry point is a twelve-fragment astral onboarding that
ends on a single onboarding card showing lineage, elemental affinity,
the physical expression derived from that affinity and the three
Kovari-Cube axes as two-pole bars. The persisted onboarding result is
`FounderOnboardingResult { Lineage, ElementalAffinity, CubeProfile,
NarrativeMemory }` — nothing else (DEC-0013). The eight lineages
influence learning context qualitatively; they do not block
professions or add automatic production bonuses. The full lineage
contract, the cube geometry and the founder sequence live in the
design bible at
[`docs/world-of-goses-design-bible/07_ONBOARDING_AND_FOUNDER.md`](docs/world-of-goses-design-bible/07_ONBOARDING_AND_FOUNDER.md),
[`docs/world-of-goses-design-bible/06_LINEAGES.md`](docs/world-of-goses-design-bible/06_LINEAGES.md),
[`docs/world-of-goses-design-bible/13_KOVARI_CUBE.md`](docs/world-of-goses-design-bible/13_KOVARI_CUBE.md)
and chapters `14_LINEAGES_*.md` through `21_LINEAGES_THERYN.md`.

Schema v31 rejects retired startup data during onboarding and replaces
the slot atomically with the canonical result, preserving the old
snapshot as `.bak`. A new city begins at `Day 1 00:00`, which is
already night — that is the first night. From the founder's
manifestation to the dawn, a fire spirit teaches why the ground
materials matter, and the night advances module by module rather
than by the clock. The full contract lives at
[`docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md`](docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md).

After the dawn the founder carries `Citizen.Commitment` for the
Founding Site lifecycle: Campfire, then Bedroll and Cache, then
Canopy, on one persistent site identity. Adding the Canopy
consolidates the same site into the Basic Shelter without spawning a
new building — that is the first construction decision, no longer a
free-standing Basic Shelter project. The next proof is the EG-A0
acceptance test in
[`docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`](docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md)
§17 (EG-5 → EG-6), not a re-introduction of starter seed data.

## 16. Provisional names

**All current names are provisional.** "World of Goses", all working
lineage names, all working UI labels, and all working in-game vocabulary
are placeholders. They exist to make the design discussions concrete and
will be revisited once the prototype validates the architecture. Do not
treat them as final shipping terminology.

## 17. License

The source-code license for this project is **still undecided**. The code,
art, and documentation are not currently open source and may not be
redistributed without explicit permission from the project owner. A
LICENSE file will be added once a license is chosen.

## 18. Contributing

This is currently a **solo project**. The repository is set up so that
other contributors can join later, but the workflow is informal.

- Read `AGENTS.md` and the documents in `docs/` before opening an issue or
  pull request.
- **Read `docs/UI_PATTERNS.md` before authoring any new UI** (screen,
  modal, button, chip, row). It is the guardrail against the divergent
  per-callsite widget definitions that already cost a stabilisation slice.
- Keep changes small, verifiable, and aligned with the current prototype
  scope.
- Do not commit secrets, API keys, tokens, signing keys, or machine-specific
  configuration.
- Do not add NuGet packages, Godot plugins, or other dependencies without a
  concrete need stated in the change.
- Do not push, publish, or create a remote repository without explicit
  authorization.
- Do not introduce a backend, a database, authentication, microservices, or
  any other architectural pattern before the prototype validates the need.

Until a contribution guide is formalized, please coordinate directly with
the project owner before making non-trivial changes.
