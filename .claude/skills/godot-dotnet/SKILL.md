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

A stable local adapter that points to the current upstream provider
of Godot 4 + C# knowledge. The local skill does not duplicate the
upstream content; it is a routing hint and a guardrail.

## Trigger

- Writing or editing any `partial` C# class that inherits a Godot
  node.
- Wiring `[Export]`, signals, `GetNode<T>`, `_Ready`, `_Process`,
  `_PhysicsProcess`, or `StringName`-based lookups.
- Touching `game/scripts/Ui/`, `game/scripts/visual/`,
  `game/scenes/`, or any `*.tscn` from C# code.
- Building a new scene or resource in code.

## Project invariants

- No `using Godot;` under `game/scripts/Domain/`. Enforced by
  `DomainBoundaryTests`.
- Node scripts are `partial` classes; the class name matches the
  file name; lifecycle overrides use PascalCase (`_Ready`, not
  `_ready`).
- C# is the production language. GDScript is permitted only when a
  `.gd` file is unavoidable (e.g. autoloads the engine requires in
  GDScript) and must be justified in the report.
- Do not silently upgrade `Godot.NET.Sdk`, `TargetFramework`, or
  any NuGet package.
- `partial` classes are reserved for Godot source generators; do
  not introduce new partial-class hierarchies for other reasons.

## Provider

Engine-specific knowledge (lifecycle, signals, the build pipeline,
`Godot.NET.Sdk`) is delegated to the upstream provider installed
by `Install-GodotDotNetSkills.ps1`. See
the provider registered by `Install-GodotDotNetSkills.ps1`. When the
provider changes, only this file and the migration report change;
the rest of the project does not.

The default provider is the Microsoft .NET family plus the C#
subset of the verified Godot provider. Installation is managed
exclusively through `Install-GodotDotNetSkills.ps1`.

## Minimal workflow

1. Load this skill together with `technical-foundation` and
   `repo-navigation`.
2. For a new node, follow the project's existing pattern in
   `game/scripts/Ui/` or `game/scripts/visual/`.
3. For a new scene, prefer `PackedScene` instantiation and a
   `CityWorldController`-style composition.
4. Verify with `cd game; dotnet build`, then the project test
   suite.

## Fallback

If the upstream provider is missing or stale, fall back to the
existing in-tree code patterns under `game/scripts/Ui/` and
`game/scripts/visual/`. The provider's role is engine API detail;
the in-tree code is the project's authoritative pattern.

## Cross-domain consultation

- Always paired with the domain skill that owns the change.
- For persistence, offline progression, or architecture, also load
  `technical-foundation`.
- For UI/UX, animation, audio, or pixel art, also load
  `presentation-experience`.

## Things not to do

- Do not duplicate the upstream provider's content here.
- Do not introduce a GDScript implementation "because the example
  uses GDScript".
- Do not edit `game/project.godot`, `game/World of Goses.csproj`,
  or `game/World of Goses.sln` without an explicit purpose.
- Do not hand-edit mirrors in `.claude/` or `.codex/`.

## Definition of done

- `cd game; dotnet build` is clean.
- The new or modified file passes `DomainBoundaryTests`.
- The upstream provider used (and the tools called, if any) is
  named in the change report.
- `quality-guardian` reviewed the change.