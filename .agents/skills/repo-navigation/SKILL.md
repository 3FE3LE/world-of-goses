---
name: repo-navigation
description: >
  Find the smallest set of code you need to read before acting. Use before
  opening a whole directory, before reading a long file end-to-end, and any
  time an agent is tempted to load "the whole source tree". Prefer semantic
  symbol retrieval, targeted references, and diff-first reviews. If Serena
  is available, prefer it; otherwise use targeted Grep + range reads.
license: World of Goses project license
compatibility: Godot 4.7 / C# / .NET 8 desktop; works alongside Serena MCP when registered.
metadata:
  type: technical-capability
  layer: cross-cutting
  audience: every agent
---

# Repo navigation

## Purpose

Reduce the number of bytes every agent reads before it acts. The nine
project skills describe *what* must be true; this skill describes *how* to
locate the relevant code without paying for the whole repository.

## When to use

- Any task that names a class, file, or behavior.
- Any bug fix (inspect the failing path first).
- Any review (inspect the diff before the surrounding files).
- Any architectural impact (search references before opening consumers).
- Any time an agent is about to open a whole directory "for context".

## Core invariants

- Never read an entire directory for orientation.
- Never load the whole `docs/` tree.
- Never open a long file end-to-end when a symbol-level read suffices.
- Domain code under `game/scripts/Domain/` is owned by `technical-foundation`.
  Other agents may read it but must not edit it.
- The session-start snapshot has the last-known baseline; trust it before
  re-deriving build/test counts.

## Required workflow

1. **Identify the target symbol.** A class name, method, field, or property.
2. **Inspect its definition.** A targeted Read of the file at the symbol's
   line range is usually enough.
3. **Inspect direct references.** Grep for the symbol across the repository;
   capture call sites and test files.
4. **Inspect relevant tests.** Tests in `tests/WorldofGoses.Tests/` are
   authoritative behavior; consult them before guessing.
5. **Expand context only when required.** A neighboring symbol is only
   needed if its behavior actually constrains the change.
6. **For reviews, inspect the diff first.** Do not read surrounding files
   before looking at the change itself.
7. **For bug fixes, inspect the failing path first.** A stack trace or
   failing test narrows the search to one or two files.

## Serena integration

When Serena is available (registered as an MCP server in the harness):

- Prefer `find_symbol` and `find_referencing_symbols` over Grep.
- Prefer `get_symbol_body` over `Read` for large files.
- The default mode is **read-only**; do not use `replace_symbol_body`
  on files under `game/scripts/Domain/` without an explicit
  `technical-foundation` handoff. See `docs/ai/SKILL_MIGRATION.md`.

When Serena is **not** available (Codex, or a harness without MCP):

- Use targeted Grep for symbols; specify a file glob.
- Read the file with an explicit line range; never the whole file unless
  it is under 200 lines.
- Use `git log -L` to inspect the history of a symbol without reading
  the full repository.

## Required documentation

- `docs/ARCHITECTURE.md` — confirms the domain/presentation boundary.
- `docs/REPOSITORY_CONVENTIONS.md` — naming and folder layout.
- `docs/ai/CONTEXT_MAP.md` — the route index.

## Files commonly involved

- `game/scripts/Domain/` — domain logic; read-only for non-technical agents.
- `tests/WorldofGoses.Tests/` — the test surface.
- `scripts/Sync-AgentContext.ps1` and `scripts/Validate-AgentContext.ps1` —
  the agent-context layer.
- `docs/ai/SKILL_MIGRATION.md` — which upstream skills and tools apply.

## Tests to run

None owned. Other skills that own code own the tests for it.

## Cross-domain consultation rules

- Loaded by every agent when its task names a class, file, or behavior.
- Pair with the domain skill that owns the change.

## Things not to do

- Do not load every skill under `.agents/skills/` "just in case".
- Do not request the whole repository from Serena when a single symbol
  resolves the question.
- Do not run `dotnet build` or `dotnet test` from this skill; that is
  `technical-foundation`'s job, gated by the session-snapshot script.

## Definition of done

- The agent located the relevant code without reading more than
  approximately 5 files end-to-end, or 200 lines of any one file.
- The agent named the symbol, its definition, and its direct references.
- If Serena was used, the agent named the tool calls made.
