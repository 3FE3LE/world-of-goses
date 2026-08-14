# CLAUDE.md

> Contract for Claude Code (and any agent that loads it) working in this
> repository. Shares its source of truth with [`AGENTS.md`](AGENTS.md); the
> hard rules and the routing rules are the same. This file only adapts the
> language to Claude Code's runtime.

## 0. Project in one paragraph

World of Goses is a persistent pixel-art desktop game about a single living
city. The player governs one city at a time. The world advances while the
game is closed. There is no meta-progression between cities and no bonus
for restarting. Expeditions are configured and automatic. All current
names — including the project name itself — are provisional.

Documentation index: [`docs/README.md`](docs/README.md).
System canon: [`docs/systems/`](docs/systems/) · product canon: [`docs/world/`](docs/world/).
Review guide: [`docs/engineering/design-review.md`](docs/engineering/design-review.md).
Architecture: [`docs/engineering/architecture.md`](docs/engineering/architecture.md).
Repository conventions in full: [`docs/engineering/conventions.md`](docs/engineering/conventions.md).

**Open work lives in GitHub Issues, not in the documentation.** `gh issue list`
is the backlog; `docs/` explains only what exists. Never add a "pending",
"next steps" or "implementation phases" section to a canonical document.

## 1. Routing

Before doing anything else, classify the request with
[`docs/ai/AGENT_DISPATCH.md`](docs/ai/AGENT_DISPATCH.md) to infer the
agent from the prompt's keywords or symptoms. Then open
[`docs/ai/CONTEXT_MAP.md`](docs/ai/CONTEXT_MAP.md) and match the request
to a route. Load the primary skill listed by the route. Load conditional
skills only when their trigger fires. **Never load the whole `docs/` tree.**

Pick a workflow mode (`SURGICAL` / `FEATURE` / `RELEASE`) and risk tier
(`LOW` / `MEDIUM` / `HIGH`) using
[`docs/ai/WORKFLOW_MODES.md`](docs/ai/WORKFLOW_MODES.md) and
[`docs/ai/RISK_MODEL.md`](docs/ai/RISK_MODEL.md). Reading existing state
does not activate a domain skill — see
[`docs/ai/DOMAIN_CONSULTATION.md`](docs/ai/DOMAIN_CONSULTATION.md).

Canonical skills live at `.agents/skills/<id>/SKILL.md`. Canonical agents
live at `.agents/agents/<id>/AGENT.md`. Claude Code discovers mirrors at
`.claude/skills/<id>/SKILL.md` and `.claude/agents/<id>.md`. **Edit
canonical files only; never hand-edit mirrors.** Run
`pwsh ./scripts/Sync-AgentContext.ps1 -Apply` then
`pwsh ./scripts/Validate-AgentContext.ps1` before any commit.

Cross-domain invariants live at
[`docs/ai/CROSS_DOMAIN_INVARIANTS.md`](docs/ai/CROSS_DOMAIN_INVARIANTS.md).
Any change that violates one of them must be redesigned.

The cooperation rules between agents are in
[`docs/ai/AGENT_COLLABORATION_PROTOCOL.md`](docs/ai/AGENT_COLLABORATION_PROTOCOL.md).
The single-writer rule, the bug workflow, the feature workflow, and the
reviewer rule are mandatory.

## 2. Skills and agents available to you

Skills (loaded under demand):

- `core-game-vision` — load whenever the task can change what the player
  does, decides, or perceives.
- `citizens-rpg` — Citizen entity, identity, commitments, injuries,
  recovery.
- `city-simulation` — buildings, construction, recipes, production,
  consumption, storage, systemic pressure.
- `expeditions-territory` — expeditions, encounters, retreat, return,
  parcels, territory.
- `narrative-lore` — cosmology, founder, dialogue, chronicle, voice and
  tone.
- `lineages-and-cultures` — cross-cutting; no lineage agent exists; load
  when lineage is touched.
- `technical-foundation` — domain/presentation boundary, persistence,
  schema versioning, determinism, offline progression, performance, tests.
- `presentation-experience` — scenes, UI, UX, pixel art, sprites,
  animation, audio, feedback.
- `vertical-slice-validation` — current slice state, slice acceptance
  criteria, identity erosion check.

Agents (read-only, exposed as subagents under `.claude/agents/`):

- `gameplay-integrator` — cross-domain coordinator; routes tasks that
  touch two or more pillars.
- `citizens-rpg` — owns the single `Citizen` entity.
- `city-simulation` — owns city simulation.
- `expeditions-territory` — owns expeditions and territory.
- `narrative-lore` — owns narrative and lore.
- `technical-foundation` — owns architecture, persistence, simulation.
- `presentation-experience` — owns scenes, UI, audio, pixel art.
- `quality-guardian` — read-only reviewer. Tools: `Read, Grep, Glob`.
  Must not be the agent that implemented the change under review.

## 3. Hard rules (always on)

These are the rules that any agent must follow even if it never reads
another line. They mirror `AGENTS.md` §3.

- **Documentation and tests are the source of truth.** Agent personas
  only route work.
- **Load under demand.** Use `CONTEXT_MAP.md`.
- **Single writer.** See `AGENT_COLLABORATION_PROTOCOL.md`.
- **Verify, do not assume.** Do not claim a change works without
  compiling, testing, or observing it.
- **Never invent schemas or rules absent from the docs.** Surface gaps
  instead.
- **No `using Godot` under `game/scripts/Domain/` or
  `game/scripts/Application/`.** Both are engine-free assemblies, so this
  is a build error. Never add a GodotSharp reference to either; move the
  engine-facing part to presentation instead.
- **`internal` in the domain is a boundary.** Promote with a reason or move
  the operation into the domain. `InternalsVisibleTo` is for tests only.
- **Static styling belongs in Scene/Theme/StyleBox.** Semantic Theme type
  variation before a local override; `Container` before manual positioning;
  no static scene trees built in C#; no new global managers or event buses.
  See `docs/engineering/architecture.md` §2 "Godot vs C#".
- **No secret.** Never read, add, or commit secrets.
- **No premature backend.** Local-only persistence.
- **No unjustified dependencies.** Justify every NuGet / plugin / SDK.
- **Do not touch these without explicit purpose** (and explain in the
  report): `game/project.godot`, `game/World of Goses.csproj`,
  `game/World of Goses.sln`, `game/.editorconfig`, `game/.gitattributes`,
  `game/icon.svg`, `game/icon.svg.import`, `README.md`, `AGENTS.md`,
  `CLAUDE.md`, anything under `docs/`, anything under `art/` or
  `game/assets/` that is not a fresh, intentional addition.
  **Do not delete** existing files without a clear reason in the report.
- **Commit authorship.** Use only the Git identity already configured by
  the user. Do not add `Co-authored-by`, `Signed-off-by`, generated-by
  notices, agent names, or Codex attribution. The repository carries
  `.githooks/commit-msg` and `tools/Install-AuthorGuardHook.ps1`; on every
  session start the snapshot script sets `core.hooksPath = .githooks`, so a
  commit that tries to credit an AI agent **fails**. The override
  `git commit --no-verify` exists, and using it requires a written reason
  in the final report.
- **Documentation must follow architecture.** Update the relevant `docs/`
  file in the same change.
- **Every session records its state.** See §5.1. The `SessionStart` hook in
  `.claude/settings.json` refreshes `docs/session-state/STATE.txt` for you;
  the part you owe is the rest: before the session's **first commit**, run
  `pwsh ./tools/New-SessionSnapshot.ps1 -Mode Full`, add
  `docs/session-state/` to that commit, and extend `CHANGELOG.md`. A session
  that changes nothing commits nothing and owes nothing.

## 4. Conventions

For the full prose see [`docs/engineering/conventions.md`](docs/engineering/conventions.md).
One-paragraph summary:

- C#: PascalCase types and methods, camelCase locals, `_camelCase` private
  fields, sealed-by-default, one public type per file, partials only for
  Godot source generators, no magic strings, no `using` inside
  namespaces, nullable reference types enabled, composition over
  inheritance, no architectural patterns without need, domain logic out
  of nodes.
- Godot 4.7 `.NET`: PascalCase node names, `[Export]` for designer-facing
  values, signals for cross-node events, `AnimatedSprite2D` /
  `AnimationPlayer` / `TileMapLayer` per convention, `.tscn` and `.tres`
  version-controlled.
- Assets: Pixelorama → PNG → Godot. Source in `art/source/`, exports in
  `art/exports/`, imports in `game/assets/`. No hand-edited PNGs.
- Persistence: JSON snapshots under user-local app data, schema version,
  atomic write with `.bak` sidecar. See `WorldSave.CurrentVersion`.

## 5. Verification

Verification scales with the workflow mode. A `SURGICAL` change does
not need a Full snapshot; a `FEATURE` change runs the affected test
families and the relevant fixtures; a `RELEASE` change runs the
full workflow. See [`docs/ai/WORKFLOW_MODES.md`](docs/ai/WORKFLOW_MODES.md)
and [`docs/ai/RISK_MODEL.md`](docs/ai/RISK_MODEL.md).

```powershell
# Recommended: ask the deterministic planner what to run.
pwsh ./tools/Get-VerificationPlan.ps1
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

`docs/session-state/` holds the *measured* baseline. Never restate a build,
test, schema or catalogue number in prose: hand-copied numbers drift, and they
did — a document once claimed 728 passing tests against a real 730 and 761
template ids against a real 804. When a document and the measurement disagree,
the measurement wins and the prose loses the number entirely.

```powershell
# Automatic at session start (SessionStart hook). Git and source only,
# no dotnet, no Godot, under a second. -Quiet suppresses the console
# report so the agent's context window is not bloated by routine
# snapshot prose on every session start.
pwsh ./tools/New-SessionSnapshot.ps1 -Mode Fast -Quiet

# Required only for RELEASE-mode changes before the session's first
# commit. Measures build, tests, headless boot, agent context and
# catalogs, and captures a dated 1280x720 frame of the live city.
pwsh ./tools/New-SessionSnapshot.ps1 -Mode Full
```

Then, in that same commit:

1. `docs/session-state/STATE.txt` and the dated `.png`.
2. A `CHANGELOG.md` entry for the increment — what a player can now do that
   they could not before, the schema range crossed, the measured baseline.
   Not a list of touched files; `git log` already owns that.

The capture needs a real Godot window and can intermittently report a `50×50`
client (`docs/engineering/visual-regression.md`). The script records that failure and
continues; it never blocks a session. Use `-SkipCapture` where no interactive
desktop exists. Never hand-edit `STATE.txt` — the next session start
overwrites it, and a state file you can write by hand proves nothing.

## 6. Source-of-truth hierarchy

When two documents disagree:

1. The most recent explicit decision wins.
2. The product vision wins over a temporary prototype.
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
  [`docs/history/decisions.md`](docs/history/decisions.md) → DEC-0011).