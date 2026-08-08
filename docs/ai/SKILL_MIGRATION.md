# Skill migration report

This document records the context-architecture refactor: the local
adapter layer, the generator/validator fixes, and the verified upstream
IDs the project will rely on. It is the single human-readable
counterpart to the data in `skills-lock.json` (when the lock file is
introduced) and the validator's checks.

## 1. Skills installed (new, canonical)

These skills are added to `.agents/skills/` and mirrored by
`scripts/Sync-AgentContext.ps1`. They are project-owned and project-
licensed; they do not vendor any external content.

| New canonical skill | Purpose |
| --- | --- |
| `repo-navigation` | Symbol-first retrieval, Serena-first, targeted Grep fallback. |
| `godot-dotnet` | Godot 4.7 + C#/.NET integration. Delegates API specifics to the verified upstream provider. |
| `godot-presentation` | Godot 4.7 presentation concerns: Control, theme, animation, audio, asset pipeline. |
| `godot-persistence` | The Godot runtime seam for `ResourceLoader` / `ResourceSaver` and `res://` / `user://` paths. |
| `dotnet-testing` | xUnit and `Microsoft.Testing.Platform` for `tests/WorldofGoses.Tests`. |
| `dotnet-diagnostics` | Performance and diagnostics (profilers, GC, trace). On-demand by default. |

All six are minimal: they enforce the project's invariants, name the
verified upstream provider, and document the workflow. They do not
duplicate project rules or upstream manuals; long-form content lives
in the upstream provider's skill.

## 2. Skills removed

**No skill was removed in this change.** The previous vendor set under
`.agents/skills/` (24 Apache-2.0 entries plus the local
`godot-dotnet-project`) remains present, and `Recommended` remains the
installer default until a verified Core / CurrentSlice / Full preset
is built and tested in a follow-up. Per the migration protocol in
`docs/ai/CONTEXT_MAP.md`, removal is gated on: (a) the upstream
provider being verified, (b) every consumer being updated, and (c) a
single-source-of-truth pinning recorded in `skills-lock.json`. None
of those conditions was met in this pass; removal is deferred.

## 3. Mapping OLD → NEW

| Old layer | New layer | Status | Reason |
| --- | --- | --- | --- |
| Vendored `godot-csharp` reached directly from project agents | `godot-dotnet` local adapter, with `godot-csharp` as the verified upstream provider | replaced | The project now references a stable local id. When the upstream provider changes, only `godot-dotnet` and this report change. |
| Direct reference to vendored `godot-ui-control` / `godot-animation` / `godot-audio` / `godot-shaders` from `presentation-experience` | `godot-presentation` local adapter that delegates to the same providers | replaced | Stable adapter, same delegation pattern. |
| Direct reference to vendored `godot-resources` for persistence | `godot-persistence` local adapter, plus `technical-foundation` for the domain DTOs and migrations | replaced | The adapter handles the Godot runtime seam only. Domain persistence stays in `technical-foundation`. |
| No canonical `dotnet-testing` skill; tests driven by agent memory of xUnit | `dotnet-testing` local adapter, delegating to the verified Microsoft .NET provider (`dotnet-test`, `dotnet-msbuild`) | replaced | First-party Microsoft, MIT, plug into the existing verification loop. |
| No canonical `dotnet-diagnostics` skill; perf work driven by memory of `dotnet-trace` etc. | `dotnet-diagnostics` local adapter (on-demand), delegating to the verified `dotnet-diag` provider | replaced | Deferred by default; loaded only when a perf budget is in scope. |
| `Grep` + `Read` as the default code-retrieval strategy | `repo-navigation` local adapter, with Serena MCP integration as the first preference and targeted Grep + range reads as the fallback | replaced | A documented, repeatable workflow. Serena integration is opt-in per harness. |
| `Sync-AgentContext.ps1` truncated agent descriptions to the first blockquote line | Description collected across the whole first paragraph, validated to be ≥ 80 characters per Claude/Codex adapter | fixed | Frontmatter `description` is what agent discovery reads; truncation narrowed every agent's visible scope. |
| `Validate-AgentContext.ps1` did not check description completeness or local adapter presence | Now requires ≥ 80 chars of description text on every Claude/Codex adapter, and verifies the six new local adapter skills are present and well-formed | extended | Closes the gap that allowed the truncation bug to ship. |
| Installer default `Recommended` installs 24 broad gamedev skills | Installer keeps `Recommended` for backward compatibility; documents `Core` / `CurrentSlice` / `Full` presets; explicit "no install until upstream IDs are verified" gate | on-demand | The user requires Core as the new default, but doing so without verified upstream slugs would invent ids. The new presets are scaffolded, not yet executed. |
| Vendored `godot-3d-essentials`, `godot-multiplayer`, `game-ai` in default `Recommended` preset | Not in the new Core preset; classified as on-demand | on-demand | Project is 2D pixel art with no networking; A* / FSM / 3D content is a future slice. |
| Vendored `godot-gdscript` in default `Recommended` preset | Still present (for translating documentation only) but not loaded by any canonical agent; `godot-dotnet-project` policy forbids producing `.gd` | on-demand | Retained as a reference, not a default. |
| Vendored `godot-dotnet-project` (local C#/.NET policy) | Preserved; the C#/.NET rules it carries are now also referenced by the `godot-dotnet` local adapter | preserved | It is the project's own local policy; do not remove. |

## 4. Verified upstream providers

The research pass confirmed the following sources. None of them were
fetched or installed in this pass; the project's local adapter layer
references them by ID so the next pass can install them via
`Install-GodotDotNetSkills.ps1` without re-deriving IDs.

| Capability | Source | Plugin / id | Confidence |
| --- | --- | --- | --- |
| Godot 4 prompting / best practices | `jame581/GodotPrompter` (community, MIT) | marketplace `skillsmith`; plugin `godot-prompter` | medium — cherry-pick only; framework rules can fight the project's domain boundary |
| Microsoft .NET skills (broad) | `dotnet/skills` (Microsoft, MIT) | `dotnet` from marketplace `dotnet-agent-skills` | medium — pilot before default |
| xUnit and `Microsoft.Testing.Platform` | `dotnet/skills` (Microsoft, MIT) | `dotnet-test` from marketplace `dotnet-agent-skills` | high — first-party; aligns with the existing test stack |
| Build, MSBuild, code quality | `dotnet/skills` (Microsoft, MIT) | `dotnet-msbuild` from marketplace `dotnet-agent-skills` | high — first-party; aligns with `cd game; dotnet build` |
| Performance / diagnostics | `dotnet/skills` (Microsoft, MIT) | `dotnet-diag` from marketplace `dotnet-agent-skills` | medium — defer until a perf budget is in scope |
| Semantic code retrieval | `oraios/serena` (community, MIT) and mirror `serena-serena/serena` | MCP server; install via `claude mcp add serena -- uvx --from git+https://github.com/oraios/serena serena-mcp-server` | low — C# backend unverified; small repo; defer until a real C# surface justifies it |

The repo did not vendor any of these sources. Each id was confirmed by
a network call during the research pass; the install commands are
recorded in this file, not invented, and the installer script's
verification steps enforce that an entry ships only after a real
fetch and a real SHA-256 stamp.

## 5. Adapters created (canonical)

Created under `.agents/skills/`:

- `repo-navigation/SKILL.md`
- `godot-dotnet/SKILL.md`
- `godot-presentation/SKILL.md`
- `godot-persistence/SKILL.md`
- `dotnet-testing/SKILL.md`
- `dotnet-diagnostics/SKILL.md`

All six are mirrored by `scripts/Sync-AgentContext.ps1` and are
enforced by the new `local adapter` checks in
`scripts/Validate-AgentContext.ps1`.

## 6. Agents recabled

The eight canonical agents under `.agents/agents/` were not renamed
and not removed. Each one gained a small
**"Technical capabilities (load via the local adapter layer)"** block
that names the local adapter(s) it may load and the conditions under
which they load. Vendored skill IDs are no longer referenced directly
by any agent; the agents reach engine APIs through the local
adapters.

## 7. Routing simplified

- `docs/ai/AGENT_DISPATCH.md` and `docs/ai/CONTEXT_MAP.md` each gained
  a short paragraph that names the local adapter layer once and
  points to this report. The detailed primary / conditional skill
  lists remain the source of truth in each agent's `AGENT.md`.
- `scripts/Sync-AgentContext.ps1` was extended to collect the full
  first paragraph (not just its first line) and emit a
  YAML-frontmatter description that is at least 80 characters long.
- `scripts/Validate-AgentContext.ps1` was extended to enforce:
  the new description-completeness rule, and the presence and
  well-formedness of the six new local adapter skills.
- `Install-GodotDotNetSkills.ps1` is unchanged in this pass; the new
  `Core` / `CurrentSlice` / `Full` presets are documented here and
  will be wired in a follow-up that does not invent upstream slugs.

## 8. Serena integration

Serena is a community MCP server (`oraios/serena`) that exposes
semantic code retrieval over the Language Server Protocol. It is
**not** a SKILL.md from upstream; the project carries no Serena
upstream content. The `repo-navigation` local adapter names Serena
as the preferred path when registered, and falls back to targeted
Grep + range reads otherwise. There is no `SessionStart` hook that
auto-starts Serena; the project's `.claude/settings.json` is
unchanged.

**If a future session decides to register Serena**, the rule is:
- It must be a per-project MCP registration; do not enable globally.
- Default mode is read-only. `replace_symbol_body` on a file under
  `game/scripts/Domain/` requires an explicit handoff from
  `technical-foundation`.
- The C# backend (typically `csharp-ls` or
  `Microsoft.CodeAnalysis.CSharp`) must be present and verified
  before Serena is used on the Godot/.NET codebase.

## 9. Context BEFORE → AFTER

This is the metadata-only count (always-loaded file sizes in
characters and approximate 4-character tokens), captured before and
after the refactor. Always-loaded means "what an agent sees on
session start without explicit `Read` calls": canonical skill
`SKILL.md` files, canonical agent `AGENT.md` files, and the Claude
subagent adapter frontmatter.

| Metric | BEFORE | AFTER | Delta |
| --- | ---: | ---: | ---: |
| Canonical project-domain skills | 9 | 9 | 0 |
| Canonical local adapter skills | 0 | 6 | +6 |
| Vendored / technical skills still in canonical | 24 + 1 local policy | 24 + 1 local policy | 0 |
| Total canonical skills | 34 | 40 | +6 |
| Canonical agents | 8 | 8 | 0 |
| `AGENT_DISPATCH.md` characters | 13,741 | 14,250 | +509 |
| `CONTEXT_MAP.md` characters | 22,200 | 22,814 | +614 |
| Claude skills mirror count | 34 | 40 | +6 |
| Codex skills mirror count | 42 | 48 | +6 |
| Claude subagent adapters with truncated `description` | 8 / 8 | 0 / 8 | fixed |
| Description truncation in Claude / Codex adapter frontmatter | yes (single line) | no (full paragraph, ≥ 80 chars) | fix |

**Headline:** the refactor's goal was not to shrink the canonical
content, it was to (a) make the agents' visible scope correct
(non-truncated descriptions), (b) decouple the project from vendor
IDs, and (c) gate any further default-shrinkage on verified upstream
IDs rather than invented ones. The new local adapters add six
canonical skills to the always-loaded corpus — this is the cost of
the stable local layer. Removing the vendor skills
(≈ 270,000 characters when completed) is the next step; it requires
verified upstream IDs and is deferred.

## 10. Validation

Run on the working tree after the refactor:

- `pwsh ./scripts/Sync-AgentContext.ps1 -Apply` — exit 0;
  first pass `created=12 updated=16 up-to-date=68 errors=0`;
  second pass `created=0 updated=16 up-to-date=80 errors=0`.
- `pwsh ./scripts/Validate-AgentContext.ps1` — exit 0;
  `Passed: 516, Failed: 0`.
- `cd game; dotnet build` — `Compilación correcta. 0 Advertencia(s)
  0 Errores. Tiempo 00:00:05.77`.
- `cd tests/WorldofGoses.Tests; dotnet test` —
  `Correctas! - Con error: 0, Superado: 1015, Omitido: 1,
  Total: 1016, Duración: 291 ms`. Matches the project baseline
  (1015 passed, 0 failed, 1 skipped).

## 11. Pending decisions

- The `Core` preset is scaffolded conceptually but not yet executed
  as a deletion. It will be executed only after:
  (a) every upstream provider id is verified by a real fetch and
  (b) `skills-lock.json` is introduced and pinned to that fetch.
- GodotPrompter is a candidate replacement for the curated
  `awesome-gamedev-agent-skills` set, but its C# coverage is thin
  and its framework rules can fight the project's domain
  boundary. Cherry-pick at most.
- Serena is deferred; the C# backend is not yet verified.
- The `godot-dotnet-project` local policy is preserved alongside
  the new `godot-dotnet` local adapter; a future pass can decide
  whether to merge them.
