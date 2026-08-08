---
name: dotnet-diagnostics
description: >
  Use for performance and diagnostics tasks: profilers, GC analysis,
  CPU sampling, allocation tracking, trace collection, and the .NET
  diagnostic CLI. Engine API specifics are delegated to the verified
  upstream .NET provider registered by Install-GodotDotNetSkills.ps1.
  Project rules still come from technical-foundation.
license: World of Goses project license
compatibility: .NET 8 desktop; Godot profiler for engine-side frames.
metadata:
  type: technical-capability
  layer: diagnostics
  audience: technical-foundation
---

# .NET diagnostics

## Purpose

A small, stable adapter for performance and diagnostics. Local skill
enforces the project's diagnostic process; tool specifics are
delegated to the verified upstream provider.

## When to use

- A frame budget is regressing or has never been measured.
- GC spikes appear in profiling output.
- An `OfflineProgression` catch-up exceeds the time the user is
  willing to wait.
- The user names `dotnet-trace`, `dotnet-counters`, `dotnet-dump`,
  or `dotnet-gcdump` explicitly.

## Provider delegation

The verified upstream .NET provider (Microsoft, `dotnet/skills`) is
the only authoritative source for diagnostic CLI usage. The repo's
working subset is `dotnet-msbuild` (build/quality) plus
`dotnet-diag` (perf/diagnostics) on demand; both are recorded in
`docs/ai/SKILL_MIGRATION.md`.

## Core invariants

- Measure first; only then change code.
- For a frame-budget issue, use the Godot profiler first; the .NET
  profiler second.
- For an offline-progression issue, prove equivalence with live
  advancement before optimizing.
- Do not add a benchmark or diagnostic binary to a release build.

## Required documentation

- `docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE.md`.
- `docs/ai/CONTEXT_MAP.md` → Technical → Architecture changes.

## Workflow

1. Load this skill with `technical-foundation` and `repo-navigation`.
2. Capture a baseline (frame time, GC pause, catch-up wall time)
   before changing code.
3. For engine API specifics, query the verified upstream provider.
4. Add a regression test that fails on the old behavior.
5. Verify with `dotnet test` and a fresh headless-boot snapshot.

## Cross-domain consultation rules

- Always paired with `technical-foundation`.
- For domain regressions, also load the domain skill.

## Things not to do

- Do not duplicate the upstream provider's content here.
- Do not run a profiler inside a CI build.
- Do not change a public API to "fix" a perf number.

## Definition of done

- The baseline and the post-change numbers are recorded in the
  change report.
- `dotnet test` is green.
- The upstream provider used is named in the change report.
