# Workflow modes

> Three execution modes that scale verification to actual risk. They
> replace the previous "everything is a release" default.
>
> Combined with [`RISK_MODEL.md`](RISK_MODEL.md) and
> [`DOCUMENTATION_IMPACT_GATE.md`](DOCUMENTATION_IMPACT_GATE.md), these
> modes are the routing layer every agent must respect.

## The three modes

### SURGICAL — LOW risk by default

Use for:

- spacing, border, layout, font size, icon replacement
- visual bug, tooltip, localized copy correction
- mechanical rename, equivalent component swap
- typo, comment cleanup, single-line change with no semantic impact
- one-file targeted refactor with a regression test

Workflow (mandatory):

1. Classify.
2. One owner + one primary skill.
3. Symbol-first code retrieval (see `repo-navigation`).
4. Implement the smallest change that satisfies the request.
5. Build + targeted tests only.
6. Documentation impact gate (see
   [`DOCUMENTATION_IMPACT_GATE.md`](DOCUMENTATION_IMPACT_GATE.md)).
7. Commit.

Default-skipped:

- full test suite
- full visual matrix
- full session snapshot
- Quality Guardian review
- all domain skills
- all design docs
- documentation sweeps

### FEATURE — MEDIUM risk by default

Use for:

- HUD refactor, new panel, new screen, new reusable UI pattern
- new read-only projection, new interaction path
- new expedition presentation, new citizen summary view
- new ontology area under an existing domain
- small architecture-impact refactor (single layer)

Workflow (mandatory):

1. Classify once.
2. Single owner + primary skill + only the necessary technical adapter.
3. Targeted canonical docs (only sections the route admits).
4. Symbol-first retrieval.
5. Implement + targeted verification during development.
6. Documentation impact gate.
7. **ONE** proportional quality review
   (`PRESENTATION_REVIEW` for UI, `DOMAIN_REVIEW` otherwise — see
   `quality-guardian` agent).
8. Final verification: build + affected test families + relevant
   visual fixtures + headless boot if scenes changed + localization if
   locale changed.
9. One final commit, or a small number of logical commits.

Default-skipped:

- full test suite (run affected families only)
- full visual matrix (run fixtures touched by the diff only)
- full session snapshot (Fast only during iteration; one Full at close if useful)
- Quality Guardian per subtask (one review per FEATURE)

### RELEASE — HIGH risk by default

Use for:

- milestone, save-schema migration, architecture boundary change
- major gameplay integration, release candidate
- major persistence change, cross-domain invariant change
- new dependency, large toolchain change, large content cut

Workflow (mandatory):

Full workflow:

1. `pwsh ./tools/New-SessionSnapshot.ps1 -Mode Full` (Full snapshot at start).
2. Full build (`cd game; dotnet build`).
3. Full test suite (`cd tests/WorldofGoses.Tests; dotnet test`).
4. Headless boot (covered by Full snapshot).
5. Agent validation
   (`pwsh ./scripts/Validate-AgentContext.ps1`).
6. Localization validation
   (`pwsh ./tools/Test-LocalizationCatalog.ps1`).
7. Full visual matrix
   (`pwsh ./tools/Capture-VisualMatrix.ps1`).
8. `SYSTEM_REVIEW` quality review (see `quality-guardian` agent).
9. CHANGELOG entry for the increment.
10. Session state (Full snapshot committed).
11. Mirror sync (`pwsh ./scripts/Sync-AgentContext.ps1 -Apply`).

RELEASE is **explicitly exceptional**. If you reach for it on a routine
change, stop and re-classify.

## How an agent picks a mode

1. Read the request.
2. Apply the `LOW / MEDIUM / HIGH` heuristic in
   [`RISK_MODEL.md`](RISK_MODEL.md).
3. If the risk is LOW → SURGICAL.
4. If the risk is MEDIUM → FEATURE.
5. If the risk is HIGH → RELEASE.
6. **When ambiguous, escalate one level.** Spending a few extra seconds
   on the right review beats under-shipping a regression.

## Tools that help pick the mode

- `tools/Get-VerificationPlan.ps1` — deterministic planner that reads
  `git diff --name-only` and prints the recommended mode, risk,
  required/skipped commands, and review depth. No LLM involved.

## Anti-patterns

- **Treating every change as a release.** This is the default the
  refactor exists to break. A spacing tweak does not need a Full
  snapshot.
- **Treating a release as a feature.** A save-schema migration is not
  a "medium risk" change; it is HIGH.
- **Skipping the documentation impact gate on a FEATURE.** The gate
  is cheap; it prevents documentation drift.
- **Asking Quality Guardian after every subtask.** One review per
  FEATURE; re-review only if the fix materially changes scope.

## See also

- [`RISK_MODEL.md`](RISK_MODEL.md) — the LOW / MEDIUM / HIGH heuristic.
- [`DOMAIN_CONSULTATION.md`](DOMAIN_CONSULTATION.md) — when reading
  state activates (and does not activate) domain ownership.
- [`DOCUMENTATION_IMPACT_GATE.md`](DOCUMENTATION_IMPACT_GATE.md) —
  when a code change requires a doc update.
- `docs/ai/AGENT_COLLABORATION_PROTOCOL.md` §1 — operating flow.