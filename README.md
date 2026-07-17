# World of Goses

> **Status:** very early pre-production. Repository structure, tooling and
> foundational documentation only. No gameplay systems are implemented yet.

A persistent pixel-art desktop game about a single living city. The world
continues advancing while the game is closed, and the player guides its
development through institutions, production, and expeditions — without
directly controlling every action.

---

## 1. Project status

This repository currently contains:

- The Godot `.NET` project skeleton in `game/`.
- The `art/` source and export directories for pixel art.
- The `docs/` design and architecture documents.
- The `README.md`, `AGENTS.md`, and `.gitignore` at the repository root.

It does **not** yet contain playable systems, scenes with logic, art assets, or
local persistence. The current priority is structure, documentation, and a
verifiable build.

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

The full design vision is documented in [`docs/GAME_VISION.md`](docs/GAME_VISION.md).
The acknowledged design lineage is documented in
[`docs/DESIGN_INFLUENCES.md`](docs/DESIGN_INFLUENCES.md).

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

The domain layer (`Building`, `Citizen`, `BuildingProductionCalculator`,
`CityWorld`) is fully covered by xUnit. Visual / interaction-layer
behaviour (`BuildingPlot`, `CityMacroView`, `BuildingDetailView`) is exercised
manually with **F5** in Godot and not by automated tests in this slice.

## 10. Repository structure

```text
world-of-goses/
├── .git/
├── AGENTS.md
├── README.md
├── .gitignore
├── docs/
│   ├── GAME_VISION.md
│   ├── ARCHITECTURE.md
│   ├── ART_PIPELINE.md
│   └── DESIGN_INFLUENCES.md
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

## 13. First prototype scope

The first prototype is **not** the complete city. It is a small vertical
slice that may evolve into the full game.

The current slice demonstrates:

- A macro city view with selectable Quarry and Farm placeholders and a
  small amount of decorative citizen activity.
- A detailed building view with a configurable visual worker limit and
  visible worker entry / exit transitions.
- Individual citizen records shared between the two views. A citizen
  is the only person entity in the domain: roles, competencies,
  recognitions, and hero status are attached concepts, not subclasses.
- Worker assignment and removal, with a deterministic production
  counter that responds to the current assignment.

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

Items 1–6 and 8 are **complete**. The current slice validates the
macro↔detail transition, worker visibility, assign/remove, heterogeneous
buildings, persistence, offline progression, and a 100-test xUnit suite.

This list is not a contract. Items may be reordered, dropped, or expanded as
the prototype teaches us what the project actually needs.

1. ✅ **Repository** — Confirm structure, build, and documentation.
2. ✅ **First prototype scene** — Building macro/detail navigation + worker slots.
3. ✅ **Domain layer** — `Citizen` / `Building` / `CityWorld` with deterministic seed.
4. ✅ **Persistence boundary (Slice A)** — Serialize `CityWorld` to validated,
   versioned JSON; auto-load and auto-save without exposing serialization
   concerns on domain entities.
5. ✅ **Offline progression (Slice B)** — Track `lastSeenAt`; on launch,
   advance the world by N ticks equal to elapsed time (capped). Tests:
   tick arithmetic, production accumulates, experience carries. This is
   the **defining** feature of the game per `docs/GAME_VISION.md`.
6. ✅ **Multi-building expansion (Slice C)** — Quarry and Farm use distinct
   resource and competency data; the macro view selects either building.
7. **First MVP pixel art** — Replace `building_placeholder.png` and
   `worker_placeholder.png` with the first Pixelorama batch. Slot into
   `BuildingPlot` and `VisibleWorkerSlot` without re-anchoring.
8. ✅ **End-to-end validation** — Run the prototype against the acceptance
   criteria of `docs/GAME_VISION.md`; flag any drift.

## 15. Provisional names

**All current names are provisional.** "World of Goses", all working
lineage names, all working UI labels, and all working in-game vocabulary
are placeholders. They exist to make the design discussions concrete and
will be revisited once the prototype validates the architecture. Do not
treat them as final shipping terminology.

## 16. License

The source-code license for this project is **still undecided**. The code,
art, and documentation are not currently open source and may not be
redistributed without explicit permission from the project owner. A
LICENSE file will be added once a license is chosen.

## 17. Contributing

This is currently a **solo project**. The repository is set up so that
other contributors can join later, but the workflow is informal.

- Read `AGENTS.md` and the documents in `docs/` before opening an issue or
  pull request.
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
