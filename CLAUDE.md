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

Canonical design source: [`docs/world-of-goses-design-bible/`](docs/world-of-goses-design-bible/README.md).
Process guide: [`docs/PRODUCT_DIRECTION.md`](docs/PRODUCT_DIRECTION.md).
Current slice and next proof: [`docs/CURRENT_STATUS.md`](docs/CURRENT_STATUS.md).
Architecture: [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).
Repository conventions in full: [`docs/REPOSITORY_CONVENTIONS.md`](docs/REPOSITORY_CONVENTIONS.md).

## 1. Routing

Before doing anything else, classify the request with
[`docs/ai/AGENT_DISPATCH.md`](docs/ai/AGENT_DISPATCH.md) to infer the
agent from the prompt's keywords or symptoms. Then open
[`docs/ai/CONTEXT_MAP.md`](docs/ai/CONTEXT_MAP.md) and match the request
to a route. Load the primary skill listed by the route. Load conditional
skills only when their trigger fires. **Never load the whole `docs/` tree.**

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
- **No `using Godot` under `game/scripts/Domain/`.** Enforced by
  `DomainBoundaryTests`.
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
  notices, agent names, or Codex attribution.
- **Documentation must follow architecture.** Update the relevant `docs/`
  file in the same change.

## 4. Conventions

For the full prose see [`docs/REPOSITORY_CONVENTIONS.md`](docs/REPOSITORY_CONVENTIONS.md).
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

```powershell
cd game
dotnet build

cd ../tests/WorldofGoses.Tests
dotnet test

cd ../..
pwsh ./scripts/Sync-AgentContext.ps1 -Apply
pwsh ./scripts/Validate-AgentContext.ps1
```

There is no linter or CI configured yet. Do not invent commands. Do not
install global tools.

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
  [`docs/ai/DECISION_LOG.md`](docs/ai/DECISION_LOG.md) → DEC-0011).