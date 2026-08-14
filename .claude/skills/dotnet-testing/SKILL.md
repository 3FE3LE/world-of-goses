---
name: dotnet-testing
description: >
  Use for xUnit and Microsoft.Testing.Platform in
  tests/WorldofGoses.Tests. Engine API specifics are delegated to the
  verified upstream .NET provider registered by
  Install-GodotDotNetSkills.ps1. Project rules still come from
  technical-foundation.
license: World of Goses project license
compatibility: xUnit; .NET 8; Microsoft.Testing.Platform.
metadata:
  type: technical-capability
  layer: testing
  audience: technical-foundation, every domain agent
---

# .NET testing

## Purpose

A small, stable adapter for the test layer. The local skill enforces
only the project's testing invariants; tool specifics are delegated
to the verified upstream provider.

## When to use

- Adding or modifying a test in `tests/WorldofGoses.Tests/`.
- Diagnosing a failing test, flaky test, or test-discovery issue.
- Adding fixtures, helpers, or test-only DTOs.
- Touching `WorldofGoses.Tests.csproj` or its dependencies.

## Provider delegation

The verified upstream .NET provider (Microsoft, `dotnet/skills`) is
the only authoritative source for xUnit and
Microsoft.Testing.Platform specifics. It is installed by
`Install-GodotDotNetSkills.ps1` and recorded in
the provider registered by `Install-GodotDotNetSkills.ps1`. The working subset is currently
`dotnet-test` and `dotnet-msbuild`; `dotnet` is on-demand, and
`dotnet-diag` is deferred until a performance budget is set.

## Core invariants

- Tests live in `tests/WorldofGoses.Tests/`. No test code under
  `game/`.
- Tests are deterministic; no live clock, no real filesystem
  outside the test temp area, no real `res://` paths.
- Domain round-trip tests use the actual persistence code path
  through `WorldPersistence`.
- A regression test ships with every bug fix; a round-trip test
  ships with every persistence change.

## Required documentation

- `docs/engineering/architecture.md`.
- `docs/ai/CONTEXT_MAP.md` → Technical → Tests.

## Workflow

1. Load this skill with `technical-foundation` and `repo-navigation`.
2. For a new test, follow the existing `TestHelpers` style and
   the corresponding domain test class.
3. For a failing test, use the verified upstream provider's
   diagnosis pattern; do not guess.
4. Verify with `cd tests/WorldofGoses.Tests; dotnet test`.

## Cross-domain consultation rules

- Always paired with the domain that owns the tested code.
- For architecture or persistence questions, also load
  `technical-foundation`.

## Things not to do

- Do not duplicate the upstream provider's content here.
- Do not introduce a new test framework; the project is xUnit.
- Do not delete an existing test without a written reason in the
  report and a CHANGELOG entry.

## Definition of done

- `dotnet test` is green.
- The new test fails on the old code, passes on the new code.
- The upstream provider used is named in the change report.
