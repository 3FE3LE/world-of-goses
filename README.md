# World of Goses

A persistent pixel-art desktop game about a single living city. The world
keeps advancing while the game is closed, and the player guides its
development through institutions, production, and expeditions — without
directly controlling every action.

The player governs one persistent city. There is no meta-progression
between cities and no bonus for restarting: to begin again, the player
deletes the current city or uses a different account. The only thing that
transfers between playthroughs is the player's accumulated knowledge.

Documentation starts at the [docs index](docs/README.md): what each system is,
why it exists and what its invariants are. Open work is in
[GitHub Issues](https://github.com/3FE3LE/world-of-goses/issues), never in the
documentation. The history of completed increments lives in
[CHANGELOG.md](CHANGELOG.md), and the contract for any agent or contributor in
[AGENTS.md](AGENTS.md) and [CLAUDE.md](CLAUDE.md).

---

## Tech stack

| Layer          | Choice                                                   |
| -------------- | -------------------------------------------------------- |
| Engine         | Godot 4.7.x with the `.NET` build                        |
| Language       | C# on `.NET 8.0` (Android exports target `net9.0`)       |
| Editor         | Visual Studio Code with the C# Dev Kit and Godot Tools   |
| Pixel art tool | Pixelorama                                               |
| Primary OS     | Windows                                                  |
| Terminal       | PowerShell 7                                              |
| Persistence    | Local JSON snapshots, schema-versioned, atomic write      |
| Backend        | **Not implemented yet** — local-only by design            |

Pixel art sources live in `art/source/`, exports in `art/exports/`,
imported resources under `game/assets/`.

---

## Requirements

- Godot 4.7.x (`.NET` build) — <https://godotengine.org/download>
- `.NET` SDK 8.0 (or newer) — <https://dotnet.microsoft.com/download>
- Visual Studio Code with the C# Dev Kit and Godot Tools extensions
- Pixelorama — only if you are authoring art
- PowerShell 7

## Opening the project

1. Clone this repository.
2. Install the requirements above.
3. Open Godot 4.7 `.NET`.
4. Choose **Import** and select `game/project.godot`.
5. Open the project in Visual Studio Code if you intend to write C#.

## Building, running, and testing

From a PowerShell 7 terminal:

```powershell
# Build the game project (run from game/ or the repo root)
dotnet build

# Run all xUnit tests (run from tests/WorldofGoses.Tests/)
dotnet test

# Verify the English/Spanish gettext catalogs (run from the repo root)
.\tools\Test-LocalizationCatalog.ps1
# Run only after intentionally adding or removing catalog entries
.\tools\Test-LocalizationCatalog.ps1 -UpdateTemplate

# Generate a session snapshot
.\tools\New-SessionSnapshot.ps1 -Mode Fast   # git state only, runs in <1s
.\tools\New-SessionSnapshot.ps1 -Mode Full   # build + tests + headless boot + capture
```

To play, open `game/project.godot` in Godot and press **F5**. The first
session-end `Full` snapshot must run before the session's first commit —
see [CLAUDE.md](CLAUDE.md) §5.1 for the session-state contract.

---

## Pixel art pipeline (Pixelorama → PNG → Godot)

1. **Author** sprites, frame-by-frame animation, tilesets, buildings,
   effects, icons, and UI elements in Pixelorama.
2. **Save** the editable source as `.pxo` (or `.pxm`) files in
   `art/source/<category>/`.
3. **Export** the visual output as PNG or sprite sheets into
   `art/exports/<category>/`.
4. **Import** them into the Godot project under
   `game/assets/<category>/`. Configure filter, mipmaps, and frames per
   row from the Godot editor.
5. **Wire** the resources into `SpriteFrames`, `TileSets`, and scenes via
   `AnimatedSprite2D`, `TileMapLayer`, `AnimationPlayer`, and particles.
6. **Drive** the visual representation from C# logic. Godot renders, C#
   decides what is happening and why.

The conceptual rule:

> Pixelorama defines how it looks.
> Godot defines how it is represented and animated.
> C# defines what is happening and why.

Naming conventions, file layout, and import rules are detailed in
[ART_PIPELINE.md](docs/presentation/art-pipeline.md).

---

## Repository layout

```text
world-of-goses/
├── AGENTS.md, CLAUDE.md      # agent / contributor contracts
├── CHANGELOG.md              # narrative history of completed increments
├── README.md                 # this file
├── game/                     # the Godot project (engine + scripts + scenes + assets)
├── art/                      # Pixelorama sources and exports
├── src/                      # engine-free .csproj files (Domain, Application, Persistence)
├── docs/                     # systems, world, presentation, engineering, history
├── tests/WorldofGoses.Tests/ # xUnit suite (domain + UI composition)
├── scripts/                  # agent-context sync helpers
└── tools/                    # snapshot, capture, localization, palette generators
```

The Godot project is intentionally isolated under `game/`. Domain logic
lives in `game/scripts/Domain/`; presentation lives in `game/scripts/Ui/`
and `game/scenes/`.

---

## Conventions

The full prose lives in
[docs/engineering/conventions.md](docs/engineering/conventions.md). One-paragraph
summary:

- **C#:** PascalCase types and methods, camelCase locals, `_camelCase`
  private fields, sealed-by-default, one public type per file. Nullable
  reference types enabled. Composition over inheritance; no architectural
  patterns without a concrete current need. Domain logic stays out of
  Godot nodes (`DomainBoundaryTests` enforces it).
- **Godot 4.7 `.NET`:** PascalCase node names, `[Export]` for
  designer-facing values, signals for cross-node events, `AnimatedSprite2D`
  / `AnimationPlayer` / `TileMapLayer` per convention. `.tscn` and `.tres`
  are version-controlled.
- **Pixel art:** integer scale, nearest filter, integer positions. Source
  in `art/source/`, exports in `art/exports/`, imports in `game/assets/`.
  No hand-edited PNGs.
- **Persistence:** JSON snapshots under user-local app data,
  schema-versioned, atomic write with `.bak` sidecar. See
  `WorldSave.CurrentVersion`.
- **UI:** every new screen, modal, button, chip, or row follows one of
  the three patterns declared in [UI_PATTERNS.md](docs/presentation/ui-patterns.md),
  with explicit `theme_type_variation`, signal-driven state binding, and
  the close-path matrix the modal/focus rules demand. Read it before
  authoring any UI.

---

## Provisional names

**All current names are provisional.** "World of Goses", the working
lineage names, the working UI labels, and the working in-game vocabulary
are placeholders. They exist to make design discussions concrete and will
be revisited once the prototype validates the architecture. Do not treat
them as final shipping terminology.

---

## License

The source-code license is **still undecided**. The code, art, and
documentation are not currently open source and may not be redistributed
without explicit permission from the project owner. A `LICENSE` file will
be added once a license is chosen.

## Contributing

This is currently a solo project, but the repository is set up for future
contributors.

- Read [AGENTS.md](AGENTS.md) and the documents in [docs/](docs/) before
  opening an issue or pull request.
- Read [UI_PATTERNS.md](docs/presentation/ui-patterns.md) before authoring any UI.
- Keep changes small, verifiable, and aligned with the current prototype
  scope.
- Do not commit secrets, API keys, tokens, signing keys, or
  machine-specific configuration.
- Do not add NuGet packages, Godot plugins, or other dependencies without
  a concrete need stated in the change.
- Do not push, publish, or create a remote repository without explicit
  authorization.
- Do not introduce a backend, a database, authentication, microservices,
  or any other architectural pattern before the prototype validates the
  need.

Until a contribution guide is formalized, please coordinate directly with
the project owner before making non-trivial changes.