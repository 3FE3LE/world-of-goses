# Agent collaboration protocol

> Single source of truth for how agents cooperate on a task in this
> repository. Mandatory reading before any agent writes code.

---

## 1. Operating flow

Every task follows this sequence. Steps 1-8 are mandatory even for "small"
fixes; they are cheap and they prevent cross-domain breakage.

1. **Classify the task.** Match it against `docs/ai/CONTEXT_MAP.md`. If
   several rows match, it is cross-domain.
2. **Pick the primary agent.** Cross-domain tasks use `gameplay-integrator`.
3. **Identify consulting agents.** Read the `Consult … when` clauses for the
   matched route.
4. **Load the primary skill.** Open `.agents/skills/<id>/SKILL.md`. Read it.
5. **Load only the conditional skills whose trigger fires.** Resist the
   temptation to load everything.
6. **Read documentation and code.** Start with the canonical docs named in the
   skill, then inspect the actual code. Do not design from memory.
7. **Declare the affected invariants.** Name them by file from
   `docs/ai/CROSS_DOMAIN_INVARIANTS.md`.
8. **Confirm whether persistence, offline progression, or save schema
   change.** If yes, escalate the impact in the plan.
9. **Propose a plan.** List files likely to change, tests required, and the
   rollback story. Stop here if the change is non-routine.
10. **One writer, many consultants.** See §3.
11. **Implement.** Make the smallest change that satisfies the plan.
12. **Run the test suite.** `cd tests/WorldofGoses.Tests; dotnet test`. Report
    results honestly, including skipped or failing tests.
13. **Review with `quality-guardian`.** The reviewer must be a different
    agent than the one that implemented. The reviewer is read-only.
14. **Update documentation and decisions.** If an invariant, a design
    decision, or the current state changed, update the relevant files in the
    same change.
15. **Deliver the handoff.** Use the template in
    `docs/ai/FEATURE_HANDOFF_TEMPLATE.md`.

## 2. Each agent must produce a header

Before any non-trivial work, the acting agent surfaces:

```
Primary agent:
Consulting agents:
Documents loaded:
Affected systems:
Affected invariants:
Files likely to change:
Tests required:
```

The header is internal, not a deliverable. It is the agent's working
manifest.

## 3. Single-writer rule

A task has **one** agent responsible for each shared area. Multiple agents
must not edit the same file or the same logical area in parallel.

Shared areas include:

- `Citizen` and any class that defines personal state
  (`Role`, `CompetencyEntry`, `CitizenProfile`, `CitizenVitalStatus`, …).
- `CityWorld` and city-level simulation (`CityInventory`, `CityResourceLedger`,
  `CityEconomyRules`, `CityParcel`, `ParcelGrid`, `TerrainWearGrid`).
- Persistence: `WorldSave`, `WorldPersistence`, all `*Save.cs` DTOs.
- Main scenes: `game/scenes/CityPrototype.tscn`,
  `game/scenes/OnboardingView.tscn`, `game/scenes/HeroProfileView.tscn`.
- Canonical documents: anything under `docs/`,
  this protocol, the decision log, the context map.
- Shared configuration: `AGENTS.md`, `CLAUDE.md`, `skills-lock.json`.

Parallel work is allowed when:

- The agents are touching independent files.
- The agents are working in separate worktrees, with an explicit integration
  step and a designated integrator.
- No shared file appears in two branches of the same change.

When in doubt: serialize.

## 4. Handoff block

When work moves from one agent to another, the sender produces:

```
Primary domain:
Related domains:
Documentation loaded:
Code inspected:
Invariants affected:
Files changed:
Tests added or updated:
Risks:
Unresolved questions:
Documentation updated:
```

## 5. Bug workflow

1. Classify the domain via `docs/ai/CONTEXT_MAP.md`.
2. Reproduce the defect. Without reproduction, the fix is a guess.
3. Identify the primary agent and any consultants.
4. Implement the **most local** fix that addresses the root cause.
5. Add a regression test that fails on the unfixed code.
6. If persistence, simulation, or architecture is implicated, involve
   `technical-foundation`.
7. Review with `quality-guardian`.
8. Update documentation **only** if a rule, decision, or current state changed.

Do not turn a bug into a general refactor.

## 6. Feature workflow

1. State the player decision the feature introduces.
2. State the consequence the feature communicates.
3. Identify the citizen, city, expedition, or territory affected.
4. Confirm it reinforces the RPG-city-builder-idle identity listed in
   the affected system document under `docs/systems/`.
5. Identify persisted data, including offline impact.
6. Identify presentation required (UI, audio, pixel art).
7. Identify narrative required, if any.
8. Define what is explicitly **out of scope** for this slice.
9. Use one primary agent and named consultants.
10. Implement, test, review, document.

A feature is not approved solely because it is technically possible.

## 7. Reviewer rules

`quality-guardian`:

- Reviews only. Does not write code, scenes, or documentation for the change
  under review.
- May flag missing tests, missing migration strategy, invariant violations,
  and identity erosion.
- Must not be the agent that implemented the change.

The reviewer produces a short verdict and a list of findings. The writer
addresses each finding or explains the decision not to.

## 8. Escalation triggers

Stop and ask the user when you find:

- A contradiction between canonical documents.
- An unresolved product decision that changes the design.
- A risk of invalidating saves without a migration strategy.
- A need to remove or replace a central system.
- A change to the persistent injury / stamina question (see
  `docs/history/decisions.md` → DEC-0011).

## 9. Adding a new agent

Future agents (politics-institutions, economy-trade, relationships-society,
environment-ecology, visual-art-direction, audio-direction, combat-systems,
education-research) follow this recipe. The existing structure does not
need to change.

1. Write `.agents/agents/<id>/AGENT.md` (tool-neutral body).
2. Add or extend a skill in `.agents/skills/<id>/SKILL.md`.
3. Add the new agent and skill to `docs/ai/CONTEXT_MAP.md` for every route
   that uses it.
4. Add the agent to `scripts/Sync-AgentContext.ps1`'s `CanonicalAgents` list
   (it enumerates them already).
5. Run `scripts/Sync-AgentContext.ps1` to mirror into `.claude/` and `.codex/`.
6. Run `scripts/Validate-AgentContext.ps1`.
