---
name: godot-dotnet
description: >
  Use for Godot 4.7 runtime integration in C#/.NET. Delegate engine API
  specifics to the verified upstream provider registered by
  Install-GodotDotNetSkills.ps1 (the only supported source for this skill).
  Project rules still come from technical-foundation and the domain skill
  that owns the change. Do not introduce GDScript implementations.
license: World of Goses project license
compatibility: Godot.NET.Sdk 4.7.x; .NET 8 desktop; .NET 9 Android conditional.
metadata:
  type: technical-capability
  layer: engine-integration
  audience: agents that need to write Godot 4 C# code
---

# Godot .NET (C#) integration

## Purpose

A stable local adapter that points to the current upstream provider of
Godot 4 + C# knowledge. The local skill does not duplicate the upstream
content; it is a routing hint and a guardrail.

## When to use

- Writing or editing any `partial` C# class that inherits a Godot node.
- Wiring `[Export]`, signals, `GetNode<T>`, `_Ready`, `_Process`,
  `_PhysicsProcess`, or `StringName`-based lookups.
- Touching `game/scripts/Ui/`, `game/scripts/visual/`, `game/scenes/`,
  or any `*.tscn` from C# code.
- Building a new scene or resource in code.

## Provider delegation

Engine-specific knowledge (lifecycle, signals, the build pipeline,
`Godot.NET.Sdk`) is delegated to the upstream provider currently
installed by `Install-GodotDotNetSkills.ps1`. See
`docs/ai/SKILL_MIGRATION.md` for the verified source/IDs. When the
provider changes, only this file and the migration report change; the
rest of the project does not.

The default provider is the Microsoft .NET family plus the C# subset of
the verified Godot provider. Their installation is managed exclusively
through `Install-GodotDotNetSkills.ps1`.

## Core invariants

- No `using Godot;` under `game/scripts/Domain/`. Enforced by
  `DomainBoundaryTests`.
- Node scripts are `partial` classes; the class name matches the file
  name; lifecycle overrides use PascalCase (`_Ready`, not `_ready`).
- C# is the production language. GDScript is permitted only when a `.gd`
  file is unavoidable (e.g. autoloads the engine requires in GDScript)
  and must be justified in the report.
- Do not silently upgrade `Godot.NET.Sdk`, `TargetFramework`, or any
  NuGet package.
- `partial` classes are reserved for Godot source generators; do not
  introduce new partial-class hierarchies for other reasons.

## Required documentation

- `docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE.md` —
  domain/presentation boundary and the no-Godot-in-domain rule.
- `docs/REPOSITORY_CONVENTIONS.md` — C# and Godot conventions.
- `docs/ai/CONTEXT_MAP.md` → Technical → Architecture changes for
  layer-split questions.

## Conditional documentation

- The upstream provider's own skill content. Loaded only when an
  engine API question is open. The provider is documented in
  `docs/ai/SKILL_MIGRATION.md`; do not look it up elsewhere.

## Workflow

1. Load this skill together with `technical-foundation` and
   `repo-navigation`.
2. Open the canonical docs above; only after that, query the upstream
   provider for engine API specifics.
3. For a new node, follow the project's existing pattern in
   `game/scripts/Ui/` or `game/scripts/visual/`.
4. For a new scene, prefer `PackedScene` instantiation and a
   `CityWorldController`-style composition; see existing
   `game/scenes/`.
5. Verify with `cd game; dotnet build`, then the project test suite.

## Cross-domain consultation rules

- Always paired with the domain skill that owns the change.
- For persistence, offline progression, or architecture, also load
  `technical-foundation`.
- For UI/UX, animation, audio, or pixel art, also load
  `presentation-experience`.

## Things not to do

- Do not duplicate the upstream provider's content here.
- Do not introduce a GDScript implementation "because the example uses
  GDScript".
- Do not edit `game/project.godot`, `game/World of Goses.csproj`, or
  `game/World of Goses.sln` without an explicit purpose.
- Do not hand-edit mirrors in `.claude/` or `.codex/`.

## Definition of done

- `cd game; dotnet build` is clean.
- The new or modified file passes `DomainBoundaryTests`.
- The upstream provider used (and the tools called, if any) is named in
  the change report.
- `quality-guardian` reviewed the change.
