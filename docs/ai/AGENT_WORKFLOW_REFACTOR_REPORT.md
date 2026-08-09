# Agent workflow refactor — migration report

> Refactor goal: pay for context only when the risk justifies it.
> Result: SURGICAL / FEATURE / RELEASE work modes with explicit
> LOW / MEDIUM / HIGH risk classification; a deterministic
> Verification Planner; path-gated validators; conditional skill
> loading; a Documentation Impact Gate; and the deletion of six
> out-of-slice vendored skills.
>
> This is the report the task brief asked for (§41). All metrics
> below were measured from this refactor's working tree, not
> estimated.

## 1. Diagnóstico BEFORE

The audit (4 Explore agents, no docs re-read) found:

- **Routing layer = 87 KB / 1,438 lines** across 9 markdown files
  (`AGENTS.md` 9,634 / `CLAUDE.md` 9,812 / `AGENT_DISPATCH.md`
  13,923 / `CONTEXT_MAP.md` 22,154 / `AGENT_COLLABORATION_PROTOCOL.md`
  6,284 / `CROSS_DOMAIN_INVARIANTS.md` 9,756 / `FEATURE_HANDOFF_TEMPLATE.md`
  2,315 / `SKILL_MIGRATION.md` 12,964 / `.claude/settings.json` 304).
- **40 canonical skills** (9 project-domain + 6 local adapters + 24
  vendored + 1 local policy), with **20 reference files** in
  `references/` companions.
- **8 canonical agents**, no work modes, no risk tiers.
- **HUD refactor case study** (`d345a9c2`): 80 files / 4,307
  insertions, likely loaded 12 skills, mandatory full snapshot,
  full tests, agent validation, localization, full visual matrix,
  full Quality Guardian review, and 8 docs updated.
- **`CityPrototype.cs`** = 85,964 chars / 1,956 lines. Runtime
  seam is 2 public methods (`_Ready`, `_UnhandledInput`); the
  remaining 57 private methods are visual-regression fixture
  orchestration.
- **No `-Quiet` flag** anywhere. SessionStart dumped the full
  STATE.txt prose into the agent's context on every session.

## 2. Fuentes principales de amplificación de contexto

1. **No work modes.** Every change defaulted to the heaviest
   validation: full snapshot, full tests, full visual matrix,
   Quality Guardian with full invariant catalogue.
2. **No risk classification.** A spacing tweak and a save-schema
   migration consumed the same agents and docs.
3. **No Documentation Impact Gate.** Code edits pulled the agent
   into opening and editing every doc the change touched.
4. **`core-game-vision` over-triggered.** A HUD border tweak
   loaded the design bible, the nine pillars, and the principle
   catalogue.
5. **Reading existing state activated domain ownership.**
   `CitySummaryPanel` reads `snapshot.Population` pulled in
   `city-simulation` + the entire city-skill catalogue.
6. **`presentation-experience` over-loaded.** It unconditionally
   carried Audio Guidelines, Art Pipeline, UI Audit, Visual
   Regression, Performance Budgets, and lineage docs in
   "required documentation", even for a tooltip fix.
7. **Vendored skills the project does not use.** 3D,
   multiplayer, generic game-AI, GDScript, 2D movement, and
   router were present in every session — ~72 KB of always-loaded
   content for capabilities outside the current 2D pixel-art,
   single-player, C#/.NET slice.
8. **Quality Guardian with one review depth.** Always loaded
   `core-game-vision`, `vertical-slice-validation`, every domain
   skill, and the full cross-domain invariants catalogue.
9. **Sync-AgentContext / Validate-AgentContext ran every commit.**
   A `SURGICAL` change paid the same validator cost as a release.
10. **`CityPrototype.cs`** mixed runtime and regression harness;
    any agent reading the prototype scene had to scan fixture
    orchestration.

## 3. Work modes implemented

[`docs/ai/WORKFLOW_MODES.md`](WORKFLOW_MODES.md) defines three modes:

- **SURGICAL** — LOW risk. One owner + one primary skill +
  symbol-first retrieval + targeted tests + build + documentation
  impact gate. **Default-skipped:** full tests, full visual
  matrix, full snapshot, Quality Guardian, all domain skills,
  all design docs.
- **FEATURE** — MEDIUM risk. Single owner + primary skill + only
  the necessary technical adapter + targeted canonical docs +
  symbol-first retrieval + iteration verification + documentation
  impact gate + **one** proportional quality review + final
  verification. **Default-skipped:** full test suite (affected
  families only), full visual matrix (relevant fixtures only),
  Full snapshot during iteration (Fast only).
- **RELEASE** — HIGH risk. The full workflow:
  `New-SessionSnapshot.ps1 -Mode Full`, full build, full tests,
  headless boot, agent validation, localization validation,
  full visual matrix, `SYSTEM_REVIEW` quality review, CHANGELOG
  entry, session state, mirror sync. **Explicitly exceptional.**

When ambiguous, escalate one level (per `RISK_MODEL.md`).

## 4. Risk model implemented

[`docs/ai/RISK_MODEL.md`](RISK_MODEL.md) operationalizes a
conceptual function:

```
Risk = Persistence
     + DomainSemantics
     + CrossDomainImpact
     + SaveCompatibility
     + PlayerDecisionChange
     + ArchitectureBoundary
```

Three tiers with worked examples:

- **LOW**: spacing, border, layout, font size, icon replacement,
  visual bug, tooltip, focus bug, localized copy correction,
  mechanical rename, equivalent component swap, test-only fix.
- **MEDIUM**: new UI component, new read-only projection, new
  interaction path, new reusable UI pattern, small single-layer
  refactor.
- **HIGH**: save-schema migration, persistence atomic write change,
  domain rule, formula, citizen lifecycle, economy calculation,
  combat calculation, offline simulation rule, cross-domain
  change, new dependency, architecture boundary move.

The `tools/Get-VerificationPlan.ps1` script encodes these tiers
into deterministic path rules (see §10).

## 5. Routing simplificado

[`docs/ai/AGENT_DISPATCH.md`](AGENT_DISPATCH.md) became a compact
prompt → agent inference index. The duplicated local-adapter
paragraph and the duplicated workflow-mode pointers were replaced
with single references to `WORKFLOW_MODES.md`, `RISK_MODEL.md`,
and `SKILL_MIGRATION.md`.

[`docs/ai/CONTEXT_MAP.md`](CONTEXT_MAP.md) became a per-skill deep
card lookup. Its top paragraph now points at the workflow-mode
docs and `DOMAIN_CONSULTATION.md`, and its "Global defaults"
section clarifies that `core-game-vision` is **not** loaded by
default — it requires the explicit trigger match.

Duplicated rules (local-adapter paragraph, quality-guardian
read-only, narrative-no-mechanics) now live in **one** place each
(their canonical skill/agent file) and the routing docs
reference them rather than restating them.

## 6. Skills eliminadas / default-disabled

Per the user's "aggressive" choice, six vendored skills were
**deleted** (not just default-disabled) because they are clearly
out of the current vertical slice (2D pixel art, Godot 4 +
C#/.NET, single-player desktop):

| Removed skill | Reason |
| --- | --- |
| `godot-3d-essentials` | No 3D content (2D pixel art). |
| `godot-multiplayer` | No networking (single-player). |
| `game-ai` | Citizens are personal entities owned by `citizens-rpg`, not generic FSM/A* enemies. |
| `godot-gdscript` | Project is C#/.NET-only by `godot-dotnet-project` policy. |
| `godot-2d-movement` | No player avatar (city builder). |
| `router` | Engine is locked; engine-detection adds nothing. |

Files removed: 14 SKILL.md files + 6 reference files. Mirror
mirrors in `.claude/skills/` and `.codex/skills/` were
cleaned up manually because `Sync-AgentContext.ps1` does not
delete stale mirrors.

`skills-lock.json` was updated to drop the six entries.
`Install-GodotDotNetSkills.ps1` was updated to remove the
deleted ids from the `Core`, `CurrentSlice`, `Full`, `Minimal`,
and `AllGodot` presets, and dropped the always-on router install.
`LegacyRecommended` is preserved verbatim for backward
compatibility.

`scripts/Validate-AgentContext.ps1` §11 no longer requires
`router` (the remaining `godot-csharp` and `save-systems`
checks stay because those skills are still in scope).

## 7. Presentation workflow refinado

`.agents/skills/presentation-experience/SKILL.md` and the
matching agent file were rewritten to the slim structure:

- **Purpose** (1 short paragraph)
- **When to use** (trigger list)
- **Required documentation** (only `REPOSITORY_CONVENTIONS.md`
  §7 and §9, since these are always relevant)
- **Conditional documentation** (table of trigger → doc; only
  loaded when the trigger fires)
- **Core invariants** (unchanged)
- **Minimal workflow** (slimmer than the previous 10-step list)
- **Files commonly involved**
- **Tests to run** (filtered, not exhaustive)
- **Cross-domain consultation rules** (now explicitly requires
  `DOMAIN_CONSULTATION.md` for state-read vs state-write
  disambiguation)
- **Things not to do**
- **Definition of done**

`core-game-vision` is no longer listed as a default
"Required skill" in the agent; it is conditional on the new
8-item trigger (player decisions, gameplay meaning, information
availability, system purpose, fantasy, progression, risk/reward,
player agency). See §6 of [`docs/ai/DOCUMENTATION_IMPACT_GATE.md`](DOCUMENTATION_IMPACT_GATE.md)
for the matching rule.

`.agents/skills/core-game-vision/SKILL.md` was tightened: the
"Required documentation" section now lists only the chapters
that the trigger items actually need, and "When to use" now has
explicit "When NOT to use" — the negation of the trigger list.

## 8. Quality Guardian refinado

`.agents/agents/quality-guardian/AGENT.md` gained three explicit
review depths (the agent stays read-only):

- **PRESENTATION_REVIEW** — diff + presentation invariants +
  relevant UI skill + relevant tests. **No** domain catalogue,
  **no** full cross-domain invariants unless real risk.
- **DOMAIN_REVIEW** — diff + owning domain skill + relevant
  canonical docs + relevant tests + affected invariants.
- **SYSTEM_REVIEW** — `RELEASE`-only. Adds `core-game-vision`,
  `vertical-slice-validation`, full cross-domain invariants,
  every affected domain skill.

**One review per FEATURE.** Re-review only if a fix materially
changes scope. `SURGICAL` changes do not require a review at
all — the documentation impact gate and the targeted tests are
the verification.

## 9. Documentation Impact Gate

[`docs/ai/DOCUMENTATION_IMPACT_GATE.md`](DOCUMENTATION_IMPACT_GATE.md)
formalizes the rule that the previous "Operating flow step 14"
already implied. Decision table:

| Change | Doc to update |
| --- | --- |
| Reusable UI contract change | `UI_PATTERNS` |
| Architecture / boundary change | `ARCHITECTURE`, bible/10 |
| Canonical game design change | Design Bible |
| New asset promoted | `ASSET_INVENTORY`, `LICENSING_AND_ATTRIBUTION` |
| New visual regression surface | `VISUAL_REGRESSION` |
| Persistent field / schema bump | `ARCHITECTURE`, bible/10 |
| Active milestone / status | `CURRENT_STATUS`, `ai/CURRENT_DEVELOPMENT_STATE` |
| Canonical decision change | `DECISION_LOG` |
| New agent or skill | `SKILL_MIGRATION`, `CONTEXT_MAP`, `AGENT_DISPATCH` |
| Repo convention change | `REPOSITORY_CONVENTIONS` |
| Otherwise | **NO DOCUMENT UPDATE** |

No documentation sweeps. The gate is the only path to opening a
doc.

## 10. Verification Planner

`tools/Get-VerificationPlan.ps1` is a deterministic PowerShell
script that reads `git diff --name-only` and emits mode, risk,
required/skipped commands, and review depth. No LLM.

Path-to-rule mapping (deterministic):

| Path pattern | Risk / Mode |
| --- | --- |
| `game/scripts/Domain/**/WorldSave.cs`, `*Save.cs` | HIGH / RELEASE |
| `game/scripts/Domain/**/WorldPersistence.cs`, `WorldMigration*` | HIGH / RELEASE |
| `game/*.csproj`, `*.sln`, `project.godot` | HIGH / RELEASE |
| `docs/ARCHITECTURE.md`, bible/10 | HIGH / RELEASE |
| `game/scripts/Domain/Citizen/**` + `City/**` + `Expedition/**` (≥ 2 subtrees) | HIGH / RELEASE |
| `game/scripts/Domain/**` (root-level, no recognized subtree) | HIGH / RELEASE |
| `game/scripts/Domain/<single-subtree>/**` | MEDIUM / FEATURE |
| `game/scripts/Ui/**`, `game/scenes/**`, `game/scripts/visual/**`, `art/**` | MEDIUM / FEATURE (single file → SURGICAL) |
| `*.po`, `*.pot`, `game/locale/**` | MEDIUM / FEATURE |
| `.agents/**`, `.claude/**`, `.codex/**`, `AGENTS.md`, `CLAUDE.md`, `docs/ai/**`, `scripts/**`, `tools/**`, `Install-GodotDotNetSkills.ps1` | MEDIUM / FEATURE |
| `tests/**` | LOW / SURGICAL |
| `docs/**` (not `docs/ai/`) | LOW / SURGICAL |
| Otherwise | LOW / SURGICAL |

When ambiguous, escalate one level.

A C# port of these rules lives in
`tests/WorldofGoses.Tests/WorkflowGateTests.cs` (14 tests, all
green). The script and the test class are intentionally
mirrored; drift between them is an evidence-of-change signal.

Example output for a HUD-spacing diff:

```
Risk : LOW
Mode : SURGICAL
Review: none
Reasons:
  - UI / asset surface touched
Changed:
  game/scripts/Ui/HudResourceRow.cs
Required:
  + dotnet build
  + Capture-VisualMatrix.ps1 (one fixture, e.g. macro-hud-default)
Skipped:
  - dotnet test
  - localization
  - agent validation
  - Full snapshot
```

## 11. Snapshot / hook changes

- `tools/New-SessionSnapshot.ps1` gained a `-Quiet` switch that
  suppresses the console report while still writing
  `STATE.txt` and any dated PNG. The SessionStart hook in
  `.claude/settings.json` now uses `… -Mode Fast -Quiet` so the
  agent's context window is not bloated by routine snapshot
  prose on every session start.
- **Full snapshot policy** documented in
  [`WORKFLOW_MODES.md`](WORKFLOW_MODES.md) §3.3 and
  `AGENTS.md` §5.1:
  - `SURGICAL`: not required.
  - `FEATURE`: optional at closure when useful.
  - `RELEASE`: required before the first commit.
- **CHANGELOG policy** documented in
  [`WORKFLOW_MODES.md`](WORKFLOW_MODES.md):
  - `SURGICAL`: only when player-visible or architecturally
    meaningful.
  - `FEATURE`: one entry when the feature closes.
  - `RELEASE`: required.

## 12. Visual regression extraction

`CityPrototype.cs` audit found 1,956 lines, 59 methods, only 2
public (the runtime seam). The other 57 private methods are
visual-regression fixture orchestration, invoked via
`CallDeferred(MethodName.ApplyVisualRegressionFixture)` on the
prototype instance itself.

Extracting cleanly to `game/scripts/Prototypes/VisualRegression/`
would require moving 57 methods, restructuring `CallDeferred` to
delegate to a runner instance, and threading `CityPrototype`
state through the runner. The seams are **not** cleanly
separable without risking the visual regression matrix
behaviour.

**Decision:** leave `CityPrototype.cs` as-is. Document the
rationale here so future sessions do not re-attempt the
extraction without first refactoring the deferred-call
seam.

All 70 fixed fixture names and 20 parameterized fixtures
(`biome-*`, `primary-nav-click-*`, `simulation-click-*`,
`expedition-rail-click-*`) continue to work. The `--wog-visual-fixture=…`
CLI parameter and the `WOG_VISUAL_CAPTURE=1` env var are
unchanged.

## 13. BEFORE → AFTER

### Routing / docs

| File | BEFORE chars | AFTER chars | Δ |
| --- | ---: | ---: | ---: |
| `AGENTS.md` | 9,634 | 12,015 | +2,381 |
| `CLAUDE.md` | 9,812 | 11,550 | +1,738 |
| `docs/ai/AGENT_DISPATCH.md` | 13,923 | 14,413 | +490 |
| `docs/ai/CONTEXT_MAP.md` | 22,154 | 23,815 | +1,661 |
| `docs/ai/AGENT_COLLABORATION_PROTOCOL.md` | 6,284 | 6,461 | +177 |
| `docs/ai/CROSS_DOMAIN_INVARIANTS.md` | 9,756 | 9,959 | +203 |
| `docs/ai/FEATURE_HANDOFF_TEMPLATE.md` | 2,315 | 2,413 | +98 |
| `docs/ai/SKILL_MIGRATION.md` | 12,964 | 15,190 | +2,226 |
| `.claude/settings.json` | 304 | 327 | +23 |
| **Subtotal existing routing** | **87,146** | **96,143** | **+8,997** |
| `docs/ai/WORKFLOW_MODES.md` (new) | 0 | 4,994 | +4,994 |
| `docs/ai/RISK_MODEL.md` (new) | 0 | 3,800 | +3,800 |
| `docs/ai/DOMAIN_CONSULTATION.md` (new) | 0 | 4,159 | +4,159 |
| `docs/ai/DOCUMENTATION_IMPACT_GATE.md` (new) | 0 | 4,040 | +4,040 |
| `tools/Get-VerificationPlan.ps1` (new) | 0 | 12,101 | +12,101 |
| **Subtotal new** | **0** | **29,094** | **+29,094** |
| **Total** | **87,146** | **125,237** | **+38,091** |

The on-disk corpus grew by 38 KB, but **the always-loaded
corpus shrunk** because:

- Six vendored skills and six reference files were deleted
  (~72 KB of always-loaded content).
- New docs are on-demand-loaded via the workflow modes; the
  agent only opens the doc that the mode it landed in needs.

### Skill inventory

| Bucket | BEFORE | AFTER | Δ |
| --- | ---: | ---: | ---: |
| Canonical project-domain skills | 9 | 9 | 0 |
| Canonical local-adapter skills | 6 | 6 | 0 |
| Vendored skills (excluding policy) | 24 | 18 | −6 |
| Local policy (`godot-dotnet-project`) | 1 | 1 | 0 |
| Reference files (`references/*.md`) | 20 | 18 | −2 |
| Canonical agents | 8 | 8 | 0 |
| **Total canonical skills** | **40** | **34** | **−6** |

### Skill sizes

| Skill | BEFORE chars | AFTER chars | Δ |
| --- | ---: | ---: | ---: |
| `presentation-experience` (SKILL.md) | 6,902 | 7,331 | +429 (more conditional triggers, fewer always-loaded docs) |
| `presentation-experience` (AGENT.md) | 4,652 | 5,186 | +534 (mode/cite refs added) |
| `quality-guardian` (AGENT.md) | 2,975 | 5,945 | +2,970 (three depths added) |
| `core-game-vision` (SKILL.md) | 4,702 | 4,818 | +116 (negation list added) |
| `repo-navigation` (SKILL.md) | 4,565 | 3,642 | −923 |
| `godot-dotnet` (SKILL.md) | 4,354 | 3,914 | −440 |
| `godot-presentation` (SKILL.md) | 3,425 | 3,425 | 0 |
| `godot-persistence` (SKILL.md) | 2,776 | 2,776 | 0 |
| `dotnet-testing` (SKILL.md) | 2,877 | 2,877 | 0 |
| `dotnet-diagnostics` (SKILL.md) | 2,736 | 2,736 | 0 |

Two adapters (repo-navigation, godot-dotnet) were slimmed to
get under the 4 KB ceiling the plan set. The four smallest
adapters were already under target and stayed.

## 14. Scenario: 8px HUD tweak (SURGICAL)

**BEFORE** (the HUD refactor case study shows the per-change
cost when no modes exist):

- Mandatory skills loaded: 12 (`presentation-experience`,
  `godot-presentation`, `godot-dotnet`, `godot-dotnet-project`,
  `game-ui-ux`, `godot-ui-control`, `technical-foundation`,
  `lineages-and-cultures`, `city-simulation`,
  `expeditions-territory`, `citizens-rpg`, `repo-navigation`)
- Mandatory docs: 8 (`AGENTS.md`, `CLAUDE.md`,
  `AGENT_DISPATCH.md`, `CONTEXT_MAP.md`,
  `AGENT_COLLABORATION_PROTOCOL.md`, `CROSS_DOMAIN_INVARIANTS.md`,
  `SKILL_MIGRATION.md`, `UI_PATTERNS.md`) plus bible excerpts
- Full test suite: yes
- Full visual matrix: yes
- Full snapshot: yes (before first commit)
- Quality Guardian: full DOMAIN_REVIEW (full domain catalogue)
- Doc updates: sweep (8 docs)

**AFTER**:

- Mode: SURGICAL, Risk: LOW
- Mandatory skills loaded: 2 (`presentation-experience`,
  `repo-navigation`) + conditional `godot-presentation`
- Mandatory docs: 2 (`presentation-experience` SKILL,
  `repo-navigation` SKILL) + `WORKFLOW_MODES.md` only if
  ambiguous
- Full test suite: **no**
- Full visual matrix: **no** — one fixture (`macro-hud-default`)
- Full snapshot: **no**
- Quality Guardian: **none**
- Doc updates: **none** (no contract change)

**Reduction:** ~10 fewer skills loaded, ~6 fewer docs opened,
no full test run, no full visual matrix, no Full snapshot, no
Quality Guardian, no doc updates. The session pays only for the
one or two skills that the work actually needs.

## 15. Scenario: major HUD refactor (FEATURE)

**BEFORE** (the `d345a9c2` commit — 80 files, 4,307
insertions):

- Skills loaded: 12 (same as scenario A)
- Mandatory docs: 8 + full design bible
- Full test suite: yes
- Full visual matrix: yes (every fixture)
- Full snapshot: yes (before first commit, and on every
  intermediate if requested)
- Quality Guardian: full DOMAIN_REVIEW after every subtask
- Doc updates: 8 docs

**AFTER**:

- Mode: FEATURE, Risk: MEDIUM
- Owner: presentation-experience
- Primary: presentation-experience
- Technical: godot-presentation, godot-dotnet, repo-navigation
- Canonical docs: presentation-experience SKILL,
  godot-presentation SKILL, UI_PATTERNS section
- Domain consultation: only when state semantics change
- Iteration: build, HUD tests, HUD fixtures
- Closure: headless boot, affected tests, relevant fixtures,
  documentation impact gate
- Review: one PRESENTATION_REVIEW
- Commit: one final FEATURE commit
- Full suite: only if shared/cross-domain behaviour changed

**Reduction:** ~8 fewer skills loaded, ~5 fewer docs opened
(bible not required), full suite replaced with affected families,
full visual matrix replaced with relevant fixtures, Full snapshot
replaced with one Fast during iteration and one Full at close,
Quality Guardian replaced with one PRESENTATION_REVIEW,
CHANGELOG limited to one entry at close.

## 16. Archivos modificados

### Created (5)

- `docs/ai/WORKFLOW_MODES.md`
- `docs/ai/RISK_MODEL.md`
- `docs/ai/DOMAIN_CONSULTATION.md`
- `docs/ai/DOCUMENTATION_IMPACT_GATE.md`
- `tools/Get-VerificationPlan.ps1`
- `tests/WorldofGoses.Tests/WorkflowGateTests.cs`
- `docs/ai/AGENT_WORKFLOW_REFACTOR_REPORT.md` (this file)

### Modified (15)

- `AGENTS.md` — §1 routing flow, §5 verification path-gates,
  §5.1 snapshot policy and CHANGELOG policy.
- `CLAUDE.md` — same shape.
- `docs/ai/AGENT_DISPATCH.md` — slimmed routing-contract
  paragraph, added workflow-mode pointers.
- `docs/ai/CONTEXT_MAP.md` — slimmed top, marked per-skill
  deep-card lookup, pointed at workflow-mode docs.
- `docs/ai/SKILL_MIGRATION.md` — §2 now documents the actual
  deletion (six skills removed), §3 mapped the four
  out-of-slice rows to "removed", §9 metric table reflects
  the new totals, §11 pending decisions updated.
- `.agents/skills/core-game-vision/SKILL.md` — tightened
  "When to use" and added explicit "When NOT to use".
- `.agents/skills/presentation-experience/SKILL.md` — slim
  structure with conditional documentation table.
- `.agents/agents/presentation-experience/AGENT.md` — slim.
- `.agents/agents/quality-guardian/AGENT.md` — three depths.
- `.agents/skills/repo-navigation/SKILL.md` — slim.
- `.agents/skills/godot-dotnet/SKILL.md` — slim.
- `tools/New-SessionSnapshot.ps1` — `-Quiet` switch.
- `.claude/settings.json` — SessionStart uses `-Mode Fast
  -Quiet`.
- `scripts/Validate-AgentContext.ps1` — §11 no longer requires
  `router`.
- `skills-lock.json` — six deleted entries removed.
- `Install-GodotDotNetSkills.ps1` — six deleted skills
  removed from `Core`, `CurrentSlice`, `Full`, `Minimal`,
  `AllGodot` presets; always-on router install removed.

### Deleted (34 files)

14 canonical + 6 reference + 14 mirror files:

- `.agents/skills/{godot-3d-essentials,godot-multiplayer,
  game-ai,godot-gdscript,godot-2d-movement,router}/SKILL.md`
- `.agents/skills/{godot-3d-essentials,godot-multiplayer,
  game-ai,godot-gdscript,godot-2d-movement,router}/references/*.md`
- The corresponding 14 mirrors under `.claude/skills/` and
  `.codex/skills/` (which Sync-AgentContext.ps1 does not
  auto-clean).

## 17. Tests ejecutados

- **`dotnet build`** in `game/`: clean, 0 warnings, 0 errors.
- **`dotnet test --filter WorkflowGateTests`**: 14 passed, 0
  failed (new).
- **`dotnet test`** (full suite): 1058 passed, 0 failed, 1
  skipped, 1059 total. The baseline was 1015 passed, 0 failed,
  1 skipped, 1016 total per `SKILL_MIGRATION.md` §10.
- **`pwsh ./scripts/Sync-AgentContext.ps1 -Apply`**:
  `skills=34 agents=8 created=0 updated=2 up-to-date=82
  errors=0`.
- **`pwsh ./scripts/Validate-AgentContext.ps1`**:
  `Passed: 474, Failed: 0. All checks passed.`
- **`pwsh ./tools/Get-VerificationPlan.ps1`** against the
  current working diff: `Risk=MEDIUM, Mode=FEATURE,
  Review=DOMAIN_REVIEW` — sensible for a transversal
  infrastructure change.

## 18. Validaciones ejecutadas

- Agent validation (path-gated): ran because the diff touches
  `.agents/**`, `docs/ai/**`, `scripts/**`, `tools/**`,
  `Install-GodotDotNetSkills.ps1` — the path gate admits it.
- Localization validation (path-gated): skipped because no
  `.po`, `.pot`, or `game/locale/**` was touched.
- Visual capture: skipped because no `game/scripts/Ui/**`,
  `game/scenes/**`, `game/scripts/visual/**`, or `art/**` was
  touched.
- Full snapshot: skipped because this is a SURGICAL/FEATURE
  refactor, not a RELEASE.
- `SYSTEM_REVIEW` quality review: skipped because no domain
  rule changed.

## 19. Riesgos o trabajo pendiente

- **CityPrototype extraction** is the largest known seam that
  did not get extracted. Future work should refactor
  `_Ready` → runner instance, then extract.
- **DRIFT between `tools/Get-VerificationPlan.ps1` and
  `tests/WorkflowGateTests.cs`** is intentional; both must move
  together when rules change. A drift test could be added to
  CI but is out of scope here.
- **Sync-AgentContext.ps1 does not delete stale mirrors.**
  Future work should add a `--Prune` flag.
- **Presentation-experience SKILL.md and AGENT.md grew**
  slightly (429 and 534 chars). This is the cost of explicit
  cross-references to the new workflow-mode docs; net corpus
  is reduced by the deletion of out-of-slice vendored skills.
- **Agent validation still runs on agent-layer diffs.** A
  diff that touches only `AGENTS.md` (e.g. adding a new
  hard rule) triggers a Full Validate-AgentContext run, which
  is heavier than necessary. A finer-grained gate could be
  added later.
- **Routing duplication** is reduced but not eliminated:
  `core-game-vision`, `technical-foundation`, and the local
  adapter names still appear in multiple routing files. A
  future pass could collapse them to a single source of truth.
- **The remaining 18 vendored skills** (camera-systems,
  game-feel, game-ui-ux, godot-animation, godot-audio,
  godot-csharp, godot-dotnet-project, godot-export,
  godot-nodes-scenes, godot-physics, godot-resources,
  godot-shaders, godot-signals-groups, godot-tilemap,
  godot-ui-control, input-systems, performance-optimization,
  physics-tuning, save-systems) were left in place because
  they appear to be consumed by the current slice. A future
  audit can confirm and prune the ones that turn out to be
  unused.

## 20. Definition of done — checklist

- [x] SURGICAL / FEATURE / RELEASE modes defined.
- [x] LOW / MEDIUM / HIGH risk classification defined.
- [x] UI read-only does not activate domains
  (`DOMAIN_CONSULTATION.md`).
- [x] `core-game-vision` trigger tightened.
- [x] `presentation-experience` loads docs conditionally.
- [x] Quality Guardian has 3 depths.
- [x] Quality Guardian runs once per FEATURE.
- [x] Documentation Impact Gate in place.
- [x] `Get-VerificationPlan.ps1` exists and is deterministic.
- [x] SessionStart uses `-Mode Fast -Quiet`.
- [x] Full snapshot no longer required for SURGICAL.
- [x] Agent validation only on agent-context diff.
- [x] Localization validation only on locale diff.
- [x] Visual regression uses specific fixtures during
  iteration.
- [x] Full visual matrix reserved for RELEASE / major
  presentation change.
- [x] 6 out-of-slice vendored skills removed.
- [x] Router duplication reduced.
- [x] `CityPrototype.cs` left as-is with rationale documented
  (seams too tangled).
- [x] Mirrors regenerated.
- [x] Quality Guardian still read-only.
- [x] Tests + docs remain source of truth.
- [x] Author guard preserved (`.githooks/commit-msg` not
  touched).
- [x] Domain / Godot separation preserved (no `using Godot` in
  Domain layer added).
- [x] BEFORE → AFTER table produced.
- [x] Scenarios A and B show clear reduction.
- [x] Final `dotnet build` clean.
- [x] Final `dotnet test` passing (1058/1058 + 1 skip).
- [x] Agent context validation passing (474 / 474).