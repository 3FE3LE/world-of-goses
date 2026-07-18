# AGENTS.md

> Instructions for any AI agent (or human contributor acting as one) working
> inside this repository. These rules are part of the project contract.
> They take precedence over generic defaults and over patterns carried over
> from other codebases.

---

## 1. Product context

World of Goses is a persistent pixel-art desktop game about a single living
city. The player governs one city at a time. The world continues advancing
while the game is closed. There is no meta-progression between cities and
no bonus for restarting. The city is evaluated across multiple independent
dimensions, not a single overall level. Expeditions are automatic and
configured, not directly controlled. All current names — including the
project name itself — are provisional.

The full vision is in [`docs/GAME_VISION.md`](docs/GAME_VISION.md). The
living direction and alignment criteria are in
[`docs/PRODUCT_DIRECTION.md`](docs/PRODUCT_DIRECTION.md). The
current implementation handoff and next starting point are in
[`docs/CURRENT_STATUS.md`](docs/CURRENT_STATUS.md). The
acknowledged design lineage is in
[`docs/DESIGN_INFLUENCES.md`](docs/DESIGN_INFLUENCES.md). The initial
architecture is in [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## 2. Current architecture

```
Domain and simulation        (C#, no Godot types in core logic)
Godot representation          (scenes, nodes, animations, input)
Assets                        (PNG / sprite sheets / audio, under game/assets/)
Local persistence            (JSON snapshots, auto-save, offline catch-up)
Tests                         (xUnit domain and persistence suite)
```

The simulation does **not** depend directly on sprites, cameras, or
animations. The visual representation reacts to domain state. C# selects
the current animation/state and Godot renders it.

## 3. Technologies

- Godot `.NET` 4.7.x.
- C# 12 on `.NET 8.0` (Android export target: `net9.0`).
- Visual Studio Code as the editor.
- Pixelorama for pixel art.
- PowerShell 7 as the terminal.
- Local storage only. No backend. No database. No network code. No auth.

## 4. Available commands

From a PowerShell 7 terminal at the repository root, the currently
relevant commands are:

```powershell
# Build the C# project (must be run from game/ for Godot's project layout)
cd game
dotnet build
```

To open the project in Godot, launch Godot 4.7 `.NET` and import
`game/project.godot`. Domain and persistence tests can be run with:

```powershell
cd tests/WorldofGoses.Tests
dotnet test
```

There is no linter or CI configured yet. Do not invent commands. Do not
install global tools.

## 5. Directory structure

```
world-of-goses/
├── .git/
├── AGENTS.md
├── README.md
├── .gitignore
├── docs/
│   ├── GAME_VISION.md
│   ├── PRODUCT_DIRECTION.md
│   ├── CURRENT_STATUS.md
│   ├── ARCHITECTURE.md
│   ├── ART_PIPELINE.md
│   └── DESIGN_INFLUENCES.md
├── art/
│   ├── source/        # Pixelorama .pxo / .pxm, references
│   │   ├── characters/
│   │   ├── buildings/
│   │   ├── terrain/
│   │   ├── effects/
│   │   └── ui/
│   ├── references/    # Mood boards, inspiration, color scripts (no game art)
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

`game/.godot/`, `game/bin/`, `game/obj/`, `.vscode/`, `*.tmp`, `*.autosave`,
`Thumbs.db`, `.DS_Store`, `*.exe`, `*.dll`, `*.pck`, `*.zip`, and similar
artifacts are ignored. See `.gitignore` at the root.

## 6. C# conventions

- PascalCase for types, methods, properties, and constants.
- camelCase for parameters, locals, and public fields where fields are
  necessary.
- `_camelCase` for private fields. Use auto-properties where appropriate.
- One public type per file. The filename matches the type name.
- Use `partial` only when required by the engine (Godot source generators).
- Use `record`, `record struct`, or value types where data is structural.
- Use `sealed` by default for classes not designed for inheritance.
- No `var` for primitive types when the type is not obvious from the right
  side; otherwise `var` is acceptable.
- No magic strings for asset paths, scene names, group names, or input
  actions. Define constants.
- No `using` directives inside namespaces. Conventional style: `using`
  outside, namespace block, then types.
- Nullable reference types: enabled where the project supports them.
- Prefer composition. Do not create deep inheritance hierarchies.
- Do not add architectural patterns (mediator, command bus, ECS, etc.)
  without a concrete current need.
- Do not put domain logic inside visual nodes when it can be separated.
- Use exceptions for exceptional cases only, not for control flow.

## 7. Godot conventions

- Godot 4.7 `.NET` source generators produce `*.<Name>.cs` partials.
  Do not edit them.
- Use PascalCase for node names. Match the C# type for one-to-one mapping.
- Use `Export` only for designer-facing values, not for runtime-only state.
- Use signals for cross-node events. Do not call into other nodes'
  internals directly when a signal exists.
- Group names are constants. No magic strings.
- Use `AnimatedSprite2D` for frame-based animation, `AnimationPlayer` for
  procedural animation, `TileMapLayer` for tilesets.
- Particles, lighting, and audio are Godot's responsibility.
- Scenarios (`.tscn`) and resources (`.tres`) are version-controlled.
  Generated cache (`.godot/`, `.import/`, `*.import`) is ignored.

## 8. Separation between domain and presentation

- Domain types live in C# classes that **do not** import `Godot.*` or
  reference nodes, sprites, or cameras.
- Domain state is exposed through plain C# APIs. The visual layer reads
  state and translates it to animations, particles, and sounds.
- Input handling is presentation. Decision-making is domain.
- The first time a system needs to react to a Godot signal, wrap the
  signal handler in a thin presentation adapter that calls into the
  domain. Do not move domain logic into the node.

## 9. Asset rules

- All pixel art is authored in Pixelorama, source files in
  `art/source/<category>/`.
- Exports (PNG, sprite sheets) go to `art/exports/<category>/`.
- Final, imported assets used by Godot live in `game/assets/<category>/`.
- Do not edit exported PNGs by hand. Re-export from the Pixelorama source.
- Do not commit generated `.import` files if they are part of the
  Godot-generated cache (see `.gitignore`); the rule is to commit the
  source of truth and let Godot regenerate.
- Audio and music are part of the same pipeline under `art/source/audio/`
  (sources) and `game/assets/audio/` (imports).
- Reference material (mood boards, inspiration, color scripts) goes in
  `art/references/`. It is **not** game art. Do not import it.

## 10. Persistence rules

- Local persistence is implemented as validated JSON snapshots under the
  user's local application-data directory.
- Persistence DTOs and file operations live under
  `game/scripts/Domain/Persistence/`; domain entities do not carry JSON
  attributes or file-system concerns.
- Saves use a schema version, a last-seen UTC timestamp, a temporary file,
  and a `.bak` sidecar when replacing an existing snapshot.
- The controller auto-loads the primary slot, auto-saves periodically and
  on window close, and retains the seeded world if loading fails.
- Offline progression currently supports:
  - saving world state,
  - saving the timestamp of the last update,
  - computing elapsed time,
  - applying a capped batch of deterministic production ticks,
  - and producing a basic aggregate report.
- A causal event log is still planned. Do not mistake the current aggregate
  report for the final event-based simulation described in the vision.

## 11. Scope restrictions

> **Do not attempt to build the entire game in a single task.**
> **Make small, verifiable changes that remain consistent with the current
> prototype.**

Concretely, the following are **out of scope** until the prototype
validates them:

- Backend services, databases, APIs, authentication.
- Mobile applications, even if mentioned in the README as a future
  possibility.
- Multiplayer, networking, account systems.
- Procedural content generation, save migration tools, modding tools.
- The full city, full production, full expedition systems, full
  healthcare system, full combat, full economy.
- A graphical installer, a launcher, a settings UI.
- Telemetry, analytics, crash reporting.
- Custom editors, plugins, or tooling in Godot beyond what the project
  needs today.
- A second gameplay loop, a second city, a meta layer between cities.

## 12. How to verify changes

- For C# changes, run `dotnet build` from the `game/` directory. The
  project must compile without errors. Warnings are reported honestly.
- For Godot scenes or `.tscn` files, confirm the project still opens in
  Godot 4.7 `.NET` without errors.
- For art, confirm the Pixelorama source opens and re-exports cleanly.
- For documentation, confirm the markdown renders correctly and that
  internal links resolve to existing files.
- For any change that affects runtime behavior, do not claim it works
  without compiling or observing it.

## 13. Files that must not be modified or deleted automatically

- `game/project.godot`
- `game/World of Goses.csproj`
- `game/World of Goses.sln`
- `game/.editorconfig`
- `game/.gitattributes`
- `game/icon.svg` and `game/icon.svg.import`
- `README.md`, `AGENTS.md`
- `docs/*`
- Any file under `art/` or `game/assets/` that is not a fresh, intentional
  addition

These files may only be modified when the change is the explicit purpose
of the task, and any modification must be explained in the final report.
**Do not delete** any existing file, scene, script, or asset without a
clear reason stated in the final report.

## 14. Secrets

> **Never read, add, or commit secrets, API keys, tokens, signing keys,
> keystores, or credentials.**

If a secret is encountered in the repository, stop and report it. Do not
echo it. Do not add it to a new file. Do not commit around it.

## 15. No premature backend

> **Do not implement a backend, a database, a server, a microservice, or
> any networked component before the prototype validates the need.**

The current architecture is local-only. The README explicitly says
"Backend: Not implemented yet". That is intentional. Adding a backend
without authorization is out of scope.

## 16. No unjustified dependencies

> **Do not add NuGet packages, Godot plugins, or other third-party
> dependencies without a concrete need stated in the change.**

Each dependency must be justified by:

1. A current, concrete use case in code.
2. An explanation of why the standard library / engine / existing
   project code is not enough.
3. An active maintenance status.

If the dependency is indispensable for compiling existing code, that is
the only exception.

## 17. Documentation rules

- Documentation lives in `docs/` and in the top-level `README.md` and
  `AGENTS.md`. Do not scatter `.md` files elsewhere.
- Keep documentation in English.
- Update the relevant documentation when an architectural decision,
  workflow, or convention changes.
- Internal documents may mention design inspirations. Public-facing names,
  art, lore, and implementations must remain independently created. Do
  not promote provisional inspiration names to final shipping terminology.
- Do not add documentation that restates the obvious. Do not remove
  documentation without replacing it with something at least as specific.

## 18. Inspect before editing

> **Always inspect the current state of the relevant files before
> editing them.**

Read the file, look at adjacent code, and check for existing patterns
that the new code should match. Do not assume the structure from memory.

## 19. Do not claim without verifying

> **Do not claim something works without compiling or verifying it.**

If a change has not been compiled, do not say it compiles. If a build
fails, say so with the output. If a step was skipped, say so. If a file
was not opened, do not describe its contents. Honesty about state is a
hard rule.

## 20. Update documentation when architecture changes

> **When an architectural decision changes — folder layout, dependency
> rule, build command, technology choice, scope boundary — update the
> `README.md` and the relevant `docs/` file in the same change.**

Out-of-date documentation is worse than no documentation.

## 21. Commit authorship

- Commits must use only the Git author and committer identity already configured
  by the user in the current environment.
- Do not add `Co-authored-by`, `Signed-off-by`, generated-by notices, agent names,
  Codex attribution, or any other authorship trailer or message that attributes
  repository work to an AI agent.
- Agents must not change `user.name`, `user.email`, signing configuration, or any
  other Git identity setting.
