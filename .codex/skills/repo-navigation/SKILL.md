---
name: repo-navigation
description: >
  Find the smallest set of code to read before acting. Use before opening a
  whole directory, before reading a long file end-to-end, and any time an
  agent is tempted to load "the whole source tree". Prefer semantic symbol
  retrieval, targeted references, and diff-first reviews. If Serena is
  available, prefer it; otherwise use targeted Grep + range reads.
license: World of Goses project license
compatibility: Godot 4.7 / C# / .NET 8 desktop; works alongside Serena MCP when registered.
metadata:
  type: technical-capability
  layer: cross-cutting
  audience: every agent
---

# Repo navigation

## Purpose

Reduce the number of bytes every agent reads before it acts. The
project skills describe *what* must be true; this adapter describes
*how to locate the relevant code* without paying for the whole
repository.

## Trigger

- Any task that names a class, file, or behavior.
- Any bug fix (inspect the failing path first).
- Any review (inspect the diff before the surrounding files).
- Any architectural impact (search references before opening
  consumers).
- Any time an agent is about to open a whole directory "for context".

## Project invariants

- Never read an entire directory for orientation.
- Never load the whole `docs/` tree.
- Never open a long file end-to-end when a symbol-level read
  suffices.
- Domain code under `game/scripts/Domain/` is owned by
  `technical-foundation`. Other agents may read it but must not
  edit it.
- The session-start snapshot holds the last-known baseline; trust it
  before re-deriving build/test counts.

## Provider

- **First choice:** Serena MCP (`find_symbol`,
  `find_referencing_symbols`, `get_symbol_body`).
- **Fallback:** targeted Grep with a file glob + Read with an
  explicit line range. Never `Read` a file over 200 lines end-to-end
  unless it is the only way.
- **For symbol history:** `git log -L`.

## Minimal workflow

1. **Identify the target symbol** — class, method, field, or
   property.
2. **Inspect its definition** — targeted Read at the symbol's line
   range, or `get_symbol_body` if Serena is registered.
3. **Inspect direct references** — Grep for the symbol across the
   repository; capture call sites and test files.
4. **Inspect relevant tests** — `tests/WorldofGoses.Tests/` is
   authoritative behavior.
5. **Expand context only when required** — a neighboring symbol is
   only needed if its behavior actually constrains the change.
6. **For reviews, inspect the diff first.** Do not read surrounding
   files before looking at the change itself.
7. **For bug fixes, inspect the failing path first.** A stack trace
   or failing test narrows the search to one or two files.

## Fallback (no Serena)

- Targeted Grep for symbols; specify a file glob.
- Read the file with an explicit line range; never the whole file
  unless it is under 200 lines.
- Use `git log -L` to inspect the history of a symbol without
  reading the full repository.

## Things not to do

- Do not load every skill "just in case".
- Do not request the whole repository from Serena when a single
  symbol resolves the question.
- Do not run `dotnet build` or `dotnet test` from this skill;
  `technical-foundation` owns those, gated by the session-snapshot
  script.
- Do not open a long file end-to-end when a range read suffices.

## Definition of done

- The agent located the relevant code without reading more than
  approximately 5 files end-to-end, or 200 lines of any one file.
- The agent named the symbol, its definition, and its direct
  references.
- If Serena was used, the agent named the tool calls made.