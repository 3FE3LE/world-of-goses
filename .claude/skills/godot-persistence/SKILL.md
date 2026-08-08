---
name: godot-persistence
description: >
  Use when persistence interacts with the Godot runtime: file paths,
  ResourceLoader / ResourceSaver, PackedScene serialization, or any read
  or write that crosses the engine boundary. Pure domain persistence is
  owned by technical-foundation; this adapter handles the engine seam.
license: World of Goses project license
compatibility: Godot.NET.Sdk 4.7.x; JSON snapshots in user-local app data.
metadata:
  type: technical-capability
  layer: persistence-seam
  audience: technical-foundation
---

# Godot persistence

## Purpose

A small, stable adapter for the seam between the project's pure
domain persistence (see `technical-foundation`) and the Godot
runtime's `ResourceLoader` / `ResourceSaver` and file-system calls.

## When to use

- Resolving a `res://` path or `user://` path.
- Loading or saving a `Resource` subclass or a `PackedScene`.
- Building a save/load that the engine actually serializes.
- Touching `WorldPersistence` or any DTO that crosses the engine seam.

## Provider delegation

The verified upstream provider is the only authoritative source for
Godot 4 persistence APIs. It is installed by
`Install-GodotDotNetSkills.ps1` and recorded in
`docs/ai/SKILL_MIGRATION.md`.

## Core invariants

- The domain does not depend on Godot. Engine-seam code lives
  *outside* `game/scripts/Domain/`. Enforced by
  `DomainBoundaryTests`.
- Saves are versioned (`WorldSave.CurrentVersion`); migrations are
  explicit; snapshots are validated before mutating live state.
- Writes are atomic with a `.bak` sidecar.
- No `res://` paths inside `game/scripts/Domain/`.

## Required documentation

- `docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE.md`.
- `docs/ARCHITECTURE.md`.
- `docs/ai/CONTEXT_MAP.md` → Technical → Persistence.

## Workflow

1. Load this skill with `technical-foundation` and `repo-navigation`.
2. Open the persistence route in `CONTEXT_MAP.md`.
3. For engine API specifics, query the verified upstream provider.
4. Add or update a round-trip test in `tests/WorldofGoses.Tests/`.
5. Verify with `cd game; dotnet build` and
   `cd tests/WorldofGoses.Tests; dotnet test`.

## Cross-domain consultation rules

- Always paired with `technical-foundation`.
- For the domain DTOs and migration code, also load
  `technical-foundation`.

## Things not to do

- Do not introduce a second save format.
- Do not write to `user://` from a domain file.
- Do not bump `WorldSave.CurrentVersion` without a migration plan
  and a round-trip test.

## Definition of done

- A real save file was loaded successfully, or a fixture covering
  the migration is committed.
- `dotnet test` is green.
- The schema version, the migration code, and the round-trip test
  are recorded in the change report.
