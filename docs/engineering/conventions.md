# Repository conventions

> The detailed prose that used to live in `AGENTS.md`. Read this when the
> router in `AGENTS.md` directs you here, or when you need the full
> rationale behind a rule. If this file ever contradicts `AGENTS.md`, the
> root file wins.

## 1. Current architecture

```
Domain and simulation        (C#, no Godot types in core logic)
Godot representation          (scenes, nodes, animations, input)
Assets                        (PNG / sprite sheets / audio, under game/assets/)
Local persistence             (JSON snapshots, auto-save, offline catch-up)
Tests                         (xUnit domain and persistence suite)
```

The simulation does **not** depend directly on sprites, cameras, or
animations. The visual representation reacts to domain state. C# selects
the current animation/state and Godot renders it.

## 2. Technologies

- Godot `.NET` 4.7.x.
- C# 12 on `.NET 8.0` (Android export target: `net9.0`).
- Visual Studio Code as the editor.
- Pixelorama for pixel art.
- PowerShell 7 as the terminal.
- Local storage only. No backend. No database. No network code. No auth.

## 3. Available commands

From a PowerShell 7 terminal at the repository root:

```powershell
# Build the C# project (must be run from game/ for Godot's project layout)
cd game
dotnet build

# Tests
cd ../tests/WorldofGoses.Tests
dotnet test

# Agent-context sync and validation
cd ../..
pwsh ./scripts/Sync-AgentContext.ps1 -Apply
pwsh ./scripts/Validate-AgentContext.ps1
```

To open the project in Godot, launch Godot 4.7 `.NET` and import
`game/project.godot`.

There is no linter or CI configured yet. Do not invent commands. Do not
install global tools.

## 4. Directory structure

```
world-of-goses/
├── .git/
├── AGENTS.md
├── CLAUDE.md
├── README.md
├── .gitignore
├── docs/
│   ├── README.md          # documentation index
│   ├── systems/           # what each game system is, and its invariants
│   ├── world/             # vision, pillars, lineages
│   ├── presentation/      # visual language, UI patterns, audio, art pipeline
│   ├── engineering/       # architecture, state authority, conventions, verification
│   ├── ai/                # agent routing layer
│   ├── history/           # decision records
│   └── session-state/     # generated measurement + dated frame
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
├── tests/
│   └── WorldofGoses.Tests/  # xUnit domain/persistence tests
├── tools/             # repo-local helper scripts
└── scripts/           # sync / validation scripts
```

`game/.godot/`, `game/bin/`, `game/obj/`, `.vscode/`, `*.tmp`,
`*.autosave`, `Thumbs.db`, `.DS_Store`, `*.exe`, `*.dll`, `*.pck`,
`*.zip`, and similar artifacts are ignored. See `.gitignore` at the root.

## 5. C# conventions

- PascalCase for types, methods, properties, and constants.
- camelCase for parameters, locals, and public fields where fields are
  necessary.
- `_camelCase` for private fields. Use auto-properties where appropriate.
- One public type per file. The filename matches the type name.
- Use `partial` only when required by the engine (Godot source
  generators).
- Use `record`, `record struct`, or value types where data is
  structural.
- Use `sealed` by default for classes not designed for inheritance.
- No `var` for primitive types when the type is not obvious from the
  right side; otherwise `var` is acceptable.
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

## 6. Godot conventions

- Godot 4.7 `.NET` source generators produce `*.<Name>.cs` partials. Do
  not edit them.
- Use PascalCase for node names. Match the C# type for one-to-one mapping.
- Use `Export` only for designer-facing values, not for runtime-only
  state.
- Use signals for cross-node events. Do not call into other nodes'
  internals directly when a signal exists.
- Group names are constants. No magic strings.
- Use `AnimatedSprite2D` for frame-based animation, `AnimationPlayer` for
  procedural animation, `TileMapLayer` for tilesets.
- Particles, lighting, and audio are Godot's responsibility.
- Scenarios (`.tscn`) and resources (`.tres`) are version-controlled.
  Generated cache (`.godot/`, `.import/`, `*.import`) is ignored.

## 7. Separation between domain and presentation

- Domain types live in C# classes that **do not** import `Godot.*` or
  reference nodes, sprites, or cameras.
- Domain state is exposed through plain C# APIs. The visual layer reads
  state and translates it to animations, particles, and sounds.
- Input handling is presentation. Decision-making is domain.
- The first time a system needs to react to a Godot signal, wrap the
  signal handler in a thin presentation adapter that calls into the
  domain. Do not move domain logic into the node.

## 8. Asset rules

- All pixel art is authored in Pixelorama, source files in
  `art/source/<category>/`.
- Exports (PNG, sprite sheets) go to `art/exports/<category>/`.
- Final, imported assets used by Godot live in `game/assets/<category>/`.
- Do not edit exported PNGs by hand. Re-export from the Pixelorama
  source.
- Do not commit generated `.import` files if they are part of the
  Godot-generated cache (see `.gitignore`); the rule is to commit the
  source of truth and let Godot regenerate.
- Audio and music are part of the same pipeline under
  `art/source/audio/` (sources) and `game/assets/audio/` (imports).
- Reference material (mood boards, inspiration, color scripts) goes in
  `art/references/`. It is **not** game art. Do not import it.

## 9. Persistence rules

- Local persistence is implemented as validated JSON snapshots under
  the user's local application-data directory.
- Persistence DTOs and file operations live under
  `game/scripts/Domain/Persistence/`; domain entities do not carry JSON
  attributes or file-system concerns.
- Saves use a schema version, a last-seen UTC timestamp, a temporary
  file, and a `.bak` sidecar when replacing an existing snapshot.
- The controller auto-loads the primary slot, auto-saves periodically
  and on window close only after hero onboarding, and starts a new empty
  world when no valid snapshot is available. The current schema version
  is `WorldSave.CurrentVersion` (see code for the current value).
- Offline progression currently supports:
  - saving world state,
  - saving the timestamp of the last update,
  - computing elapsed time,
  - applying a capped batch of deterministic production ticks,
  - fast-forwarding an empty hero-only world without production work,
  - producing a basic aggregate report.
- A causal event log is still planned. Do not mistake the current
  aggregate report for the final event-based simulation described in the
  vision.

## 10. Scope restrictions

> **Do not attempt to build the entire game in a single task.**
> **Make small, verifiable changes that remain consistent with the
> current prototype.**

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

## 11. How to verify changes

- For C# changes, run `dotnet build` from the `game/` directory. The
  project must compile without errors. Warnings are reported honestly.
- For Godot scenes or `.tscn` files, confirm the project still opens in
  Godot 4.7 `.NET` without errors.
- For art, confirm the Pixelorama source opens and re-exports cleanly.
- For documentation, confirm the markdown renders correctly and that
  internal links resolve to existing files.
- For any change that affects runtime behavior, do not claim it works
  without compiling or observing it.

## 12. Documentation rules

- Documentation lives in `docs/`, in the top-level `README.md`,
  `AGENTS.md`, and `CLAUDE.md`. Do not scatter `.md` files elsewhere.
- Keep documentation in English.
- Update the relevant documentation when an architectural decision,
  workflow, or convention changes.
- Internal documents may mention design inspirations. Public-facing
  names, art, lore, and implementations must remain independently
  created. Do not promote provisional inspiration names to final shipping
  terminology.
- Do not add documentation that restates the obvious. Do not remove
  documentation without replacing it with something at least as
  specific.

## 13. Inspect before editing

> **Always inspect the current state of the relevant files before
> editing them.**

Read the file, look at adjacent code, and check for existing patterns
that the new code should match. Do not assume the structure from
memory.

## 14. Do not claim without verifying

> **Do not claim something works without compiling or verifying it.**

If a change has not been compiled, do not say it compiles. If a build
fails, say so with the output. If a step was skipped, say so. If a file
was not opened, do not describe its contents. Honesty about state is a
hard rule.