# AGENTS.md

> Contract for any AI agent (or human contributor acting as one) working in
> this repository. The first file an agent should read. The detailed prose
> it used to contain now lives in [`docs/REPOSITORY_CONVENTIONS.md`](docs/REPOSITORY_CONVENTIONS.md);
> this file is a brief router plus the hard rules that **must** remain
> always-on, including for Claude Code (see [`CLAUDE.md`](CLAUDE.md)) and
> Codex CLI.

## 0. Project in one paragraph

World of Goses is a persistent pixel-art desktop game about a single living
city. The player governs one city at a time. The world advances while the
game is closed. There is no meta-progression between cities and no bonus
for restarting. Expeditions are configured and automatic. All current
names — including the project name itself — are provisional.

The canonical design source is the design bible at
[`docs/world-of-goses-design-bible/`](docs/world-of-goses-design-bible/README.md).
The process guide is [`docs/PRODUCT_DIRECTION.md`](docs/PRODUCT_DIRECTION.md).
The current slice and next starting point are in
[`docs/CURRENT_STATUS.md`](docs/CURRENT_STATUS.md).
The architecture is in [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## 1. How to use this contract

1. **Classify the request.** Open [`docs/ai/AGENT_DISPATCH.md`](docs/ai/AGENT_DISPATCH.md)
   to infer the agent from the prompt's keywords or symptoms. Then open
   [`docs/ai/CONTEXT_MAP.md`](docs/ai/CONTEXT_MAP.md) and match the
   request to a route. If several match, the task is cross-domain.
2. **Pick a workflow mode and risk tier.** See
   [`docs/ai/WORKFLOW_MODES.md`](docs/ai/WORKFLOW_MODES.md) (SURGICAL /
   FEATURE / RELEASE) and [`docs/ai/RISK_MODEL.md`](docs/ai/RISK_MODEL.md)
   (LOW / MEDIUM / HIGH). The mode governs how much verification runs.
3. **Load only what the route names.** Each route lists a primary skill and
   conditional skills. Do not load the whole `docs/` tree. Reading
   existing state does not activate a domain skill — see
   [`docs/ai/DOMAIN_CONSULTATION.md`](docs/ai/DOMAIN_CONSULTATION.md).
4. **Read the canonical docs the skill names.** Inspect the actual code.
5. **List the affected invariants.** They are in
   [`docs/ai/CROSS_DOMAIN_INVARIANTS.md`](docs/ai/CROSS_DOMAIN_INVARIANTS.md).
6. **Cooperate by the protocol.** See
   [`docs/ai/AGENT_COLLABORATION_PROTOCOL.md`](docs/ai/AGENT_COLLABORATION_PROTOCOL.md).
   One agent writes a shared area; the rest consult.
7. **Hand off by the template.** See
   [`docs/ai/FEATURE_HANDOFF_TEMPLATE.md`](docs/ai/FEATURE_HANDOFF_TEMPLATE.md).
8. **Apply the documentation impact gate.** Update docs only when their
   contract changed — see
   [`docs/ai/DOCUMENTATION_IMPACT_GATE.md`](docs/ai/DOCUMENTATION_IMPACT_GATE.md).
9. **Sync and validate before merge.** Run `pwsh ./scripts/Sync-AgentContext.ps1 -Apply`
   then `pwsh ./scripts/Validate-AgentContext.ps1`.

## 2. Skills and agents

Canonical definitions:

- Skills: `.agents/skills/<id>/SKILL.md` — nine skills, tool-neutral.
- Agents: `.agents/agents/<id>/AGENT.md` — eight agents, tool-neutral.

Tool-specific mirrors (generated):

- Claude Code: `.claude/skills/<id>/SKILL.md`, `.claude/agents/<id>.md`.
- Codex: `.codex/skills/<id>/SKILL.md`, `.codex/skills/agent-<id>/SKILL.md`.

**Edit canonical files only. Never hand-edit mirrors.** Mirrors drift is
detected by `Validate-AgentContext.ps1` and blocks merge.

## 3. Hard rules (always on)

These are the rules that any agent — Claude Code, Codex, or another tool —
must follow even if it never reads another line. They are restated,
compressed, and not negotiable.

- **Documentation and tests are the source of truth.** The design bible,
  `docs/`, the C# code, and `tests/`. Agent personas only route work; they
  do not define the project.
- **Load under demand.** Never load the whole documentation set to answer a
  narrow question. Use `docs/ai/CONTEXT_MAP.md`.
- **Single writer.** A task has one agent responsible for each shared
  area. Multiple agents must not edit the same file or shared area
  concurrently. See the protocol.
- **Verify, do not assume.** Do not claim a change works without
  compiling, testing, or observing it. Cite the test name. If a step was
  skipped, say so.
- **Never invent schemas or rules absent from the docs.** If something is
  not documented, surface the gap, do not fabricate.
- **No `using Godot` under `game/scripts/Domain/` or
  `game/scripts/Application/`.** Both compile into engine-free assemblies
  (`src/WorldofGoses.Domain`, `src/WorldofGoses.Application`), so this is a
  build error before it is a test failure. Do not "fix" one by adding a
  GodotSharp reference — move the engine-facing part to presentation. A
  snapshot in particular cannot call `UiText`; translate at the `Control`
  that displays the value.
- **`internal` in the domain is a boundary, not a hint.** If presentation
  needs something `internal`, either promote it with a doc comment saying
  why it is safe, or move the operation into the domain. Do not widen
  `InternalsVisibleTo`; it is for the test project.
- **Static styling belongs in Scene/Theme/StyleBox, not C#.** Padding,
  spacing, typography, colours and static layout go to
  `game/assets/ui/default_theme.tres` or the scene. Reach for a semantic
  Theme type variation before a local override, and for a `Container`
  before arithmetic on a `Control`'s position. C# is for behaviour, state
  and binding. See `docs/ARCHITECTURE.md` §2 "Godot vs C#".
- **Do not build a static scene tree in C# without a reason.** A structure
  that always has the same children is a `.tscn`. Programmatic construction
  is for content whose shape comes from data.
- **No new global managers or event buses.** Signals between a real
  publisher and its subscribers, and explicit dependencies otherwise.
- **No secret.** Never read, add, or commit secrets, API keys, tokens,
  signing keys, keystores, or credentials.
- **No premature backend.** Local-only persistence. No database, server,
  microservice, or networked component until a validated need exists.
- **No unjustified dependencies.** No NuGet, Godot plugin, or third-party
  dependency without a concrete current need and an explanation of why
  the standard library / engine / existing code is not enough.
- **Do not touch these without explicit purpose:**
  - `game/project.godot`
  - `game/World of Goses.csproj`
  - `game/World of Goses.sln`
  - `game/.editorconfig`, `game/.gitattributes`, `game/icon.svg`,
    `game/icon.svg.import`
  - `README.md`, `AGENTS.md`, `CLAUDE.md`
  - Anything under `docs/` other than `docs/ai/` for routing
  - Anything under `art/` or `game/assets/` that is not a fresh,
    intentional addition
  Any modification must be explained in the final report. **Do not delete**
  any existing file, scene, script, or asset without a clear reason stated
  in the final report.
- **Commit authorship.** Use only the Git author and committer identity
  already configured by the user. Do not add `Co-authored-by`,
  `Signed-off-by`, generated-by notices, agent names, or Codex attribution.
  Do not change `user.name`, `user.email`, signing configuration, or any
  other Git identity setting. The repository carries `.githooks/commit-msg`
  and `tools/Install-AuthorGuardHook.ps1`; on every session start the
  snapshot script sets `core.hooksPath = .githooks`, so a commit that tries
  to credit an AI agent **fails**. The override `git commit --no-verify`
  exists, and using it requires a written reason in the final report.
- **Documentation must follow architecture.** When a folder layout,
  dependency rule, build command, technology choice, or scope boundary
  changes, update the relevant `docs/` file in the same change.
- **Every session records its state.** See §5.1. Before the session's
  **first commit**, run `pwsh ./tools/New-SessionSnapshot.ps1 -Mode Full`,
  include `docs/session-state/` in that commit, and extend `CHANGELOG.md`
  with an entry for the increment. A session that changes nothing commits
  nothing and owes nothing. Claude Code additionally refreshes the state
  file automatically through a `SessionStart` hook; Codex has no equivalent
  hook, so here this rule is the only trigger.

## 4. Conventions in one paragraph

See [`docs/REPOSITORY_CONVENTIONS.md`](docs/REPOSITORY_CONVENTIONS.md) for
the full C# conventions (PascalCase, sealed-by-default, partials only for
Godot source generators, no magic strings, etc.), the Godot conventions
(PascalCase node names, `[Export]` for designer-facing values, signals not
direct calls), the asset rules (Pixelorama → PNG → Godot), and the
persistence rules (versioned JSON snapshots, schema version bump, atomic
write with `.bak`).

## 5. Verification

Verification scales with the workflow mode. A `SURGICAL` change does
not need a Full snapshot; a `FEATURE` change runs the affected test
families and the relevant fixtures; a `RELEASE` change runs the
full workflow. See [`docs/ai/WORKFLOW_MODES.md`](docs/ai/WORKFLOW_MODES.md)
and [`docs/ai/RISK_MODEL.md`](docs/ai/RISK_MODEL.md).

```powershell
# Recommended: ask the deterministic planner what to run.
pwsh ./tools/Get-VerificationPlan.ps1

# Or build the plan by hand using the rules below.
```

### Core checks (run whenever code changed)

```powershell
cd game
dotnet build

cd ../tests/WorldofGoses.Tests
dotnet test --filter "<affected test family>"
```

### Path-gated checks (run only when the diff touches the path)

| Path pattern | Run |
| --- | --- |
| `.agents/`, `.claude/`, `.codex/`, `AGENTS.md`, `CLAUDE.md`, `docs/ai/`, `scripts/`, `tools/`, `Install-GodotDotNetSkills.ps1` | `pwsh ./scripts/Sync-AgentContext.ps1 -Apply` then `pwsh ./scripts/Validate-AgentContext.ps1` |
| `*.po`, `*.pot`, `game/locale/`, `UiText.*` calls in `game/scripts/` | `pwsh ./tools/Test-LocalizationCatalog.ps1` |
| `game/scenes/`, `game/scripts/Ui/`, scenes touching visual surfaces | `pwsh ./tools/Capture-VisualMatrix.ps1` with the affected fixture names; full matrix only on `RELEASE` |

### Documentation checks (run only when a document was added, moved, or renamed)

```powershell
pwsh ./scripts/docs/inventory.ps1
pwsh ./scripts/docs/classify.ps1
```

There is no linter or CI configured yet. Do not invent commands. Do not
install global tools.

### 5.1 Session state

`docs/session-state/` holds the *measured* baseline, as opposed to the
hand-written claims in `CURRENT_STATUS.md` and
`docs/ai/CURRENT_DEVELOPMENT_STATE.md`. The two drift: on 2026-08-03 the prose
claimed 728 and 721 passing tests against a real 730, and 761 template IDs
against a real 804. When they disagree, the measurement wins and the prose
gets corrected in the same change.

```powershell
# Cheap. Git and source only: no dotnet, no Godot, under a second. Used by
# the SessionStart hook. -Quiet suppresses the console report so the agent's
# context window is not bloated by routine snapshot prose.
pwsh ./tools/New-SessionSnapshot.ps1 -Mode Fast -Quiet

# Before the session's first commit in a RELEASE-mode change. Measures build,
# tests, headless boot, agent context and catalogs, and captures a dated
# 1280x720 frame of the live city.
pwsh ./tools/New-SessionSnapshot.ps1 -Mode Full

# Same, where no interactive desktop exists.
pwsh ./tools/New-SessionSnapshot.ps1 -Mode Full -SkipCapture
```

**Full snapshot policy** (see [`docs/ai/WORKFLOW_MODES.md`](docs/ai/WORKFLOW_MODES.md)):

- `SURGICAL`: not required.
- `FEATURE`: optional at closure when useful; not required before intermediate
  commits.
- `RELEASE`: required before the first commit.

**CHANGELOG policy** (see [`docs/ai/WORKFLOW_MODES.md`](docs/ai/WORKFLOW_MODES.md)):

- `SURGICAL`: only when the change is player-visible or architecturally
  meaningful.
- `FEATURE`: one entry when the feature closes.
- `RELEASE`: required.

Then, in that same RELEASE commit:

1. `docs/session-state/STATE.txt` and the dated `.png`.
2. A `CHANGELOG.md` entry for the increment — what a player can now do that
   they could not before, the schema range crossed, the measured baseline.
   Not a list of touched files; `git log` already owns that.

The capture needs a real Godot window and can intermittently report a `50×50`
client (`docs/VISUAL_REGRESSION.md`). The script records that failure and
continues; it never blocks a session. Never hand-edit `STATE.txt` — the next
run overwrites it, and a state file you can write by hand proves nothing.

Full prose in [`docs/session-state/README.md`](docs/session-state/README.md).

## 6. Source-of-truth hierarchy

When two documents disagree:

1. The most recent explicit decision wins.
2. The product vision (`docs/world-of-goses-design-bible/`) wins over a
   temporary prototype.
3. The domain wins over its visual representation.
4. The player experience wins over an exhaustive-but-empty simulation.
5. A mechanic is not implemented only because it is technically possible.

Full prose in [`docs/README.md`](docs/README.md).

## 7. Escalation

Stop and ask the user when you find:

- A contradiction between canonical documents.
- An unresolved product decision that changes the design.
- A risk of invalidating saves without a migration strategy.
- A need to remove or replace a central system.
- A change to the persistent-injury / stamina question (see
  [`docs/ai/DECISION_LOG.md`](docs/ai/DECISION_LOG.md) → DEC-0011).