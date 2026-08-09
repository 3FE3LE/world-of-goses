# Documentation Impact Gate

> Do **not** update documentation just because code changed. Update
> each doc only if its **contract** changed. This gate prevents the
> documentation sweeps that used to fire on every implementation.

## The rule

```
Did the contract for <doc> change?
    YES → update <doc>
    NO  → do not open <doc>
```

A "contract change" is a change to what the doc promises to its
readers: a new invariant, a renamed entry point, a new required
format, a removed section, a new dependency. Pure code-internal
refactors do not change any doc contract.

## Decision table

For each doc, ask: did this change alter the doc's contract?

| If the change is … | Update |
| --- | --- |
| Reusable UI contract (new pattern, new widget, renamed widget) | `docs/UI_PATTERNS.md` |
| Architecture / boundary change (new layer, new ownership rule) | `docs/ARCHITECTURE.md`, `docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE.md` |
| Canonical game design change (new mechanic, new fantasy, new pillar) | `docs/world-of-goses-design-bible/` |
| New asset promoted (`art/source` → `art/exports` → `game/assets`) | `docs/ASSET_INVENTORY.md`, `docs/LICENSING_AND_ATTRIBUTION.md` |
| New visual regression surface or fixture | `docs/VISUAL_REGRESSION.md` |
| New persistent field, schema bump, migration | `docs/ARCHITECTURE.md`, `docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE.md`, design bible as relevant |
| Active milestone / status changed | `docs/CURRENT_STATUS.md`, `docs/ai/CURRENT_DEVELOPMENT_STATE.md` |
| Canonical decision changed (DEC-#### entries) | `docs/ai/DECISION_LOG.md` |
| New agent or skill added / removed | `docs/ai/SKILL_MIGRATION.md`, `docs/ai/CONTEXT_MAP.md`, `docs/ai/AGENT_DISPATCH.md` |
| Repository convention change | `docs/REPOSITORY_CONVENTIONS.md` |
| Otherwise | **NO DOCUMENT UPDATE** |

## Anti-patterns

- **Documentation sweeps.** Opening every doc the change touches to
  "keep them in sync" is not maintenance; it is noise. The gate is
  designed to stop this.
- **Editing the design bible to reflect the implementation.** The
  design bible is the source of truth, not the implementation log.
  If the implementation contradicts the bible, fix the
  implementation.
- **Editing `CURRENT_STATUS.md` on a `SURGICAL` change.** Status is
  for milestones, not for every commit.
- **Adding a CHANGELOG entry for a test-only fix or a typo.** See
  [`WORKFLOW_MODES.md`](WORKFLOW_MODES.md) for the CHANGELOG policy.
- **Re-reading the whole `docs/` tree "to be safe".** That is what
  `repo-navigation` is for.

## How the gate interacts with workflow modes

| Mode | Doc-update expectation |
| --- | --- |
| SURGICAL | Apply the gate; almost always results in **no** doc update. |
| FEATURE | Apply the gate once at closure; expect one or two targeted doc updates if a contract moved. |
| RELEASE | Apply the gate at every step where a contract moved; expect several targeted doc updates plus `CURRENT_STATUS.md` and `CHANGELOG.md`. |

## Worked examples

| Change | Docs updated |
| --- | --- |
| 8 px HUD spacing tweak | none (no contract change) |
| Rename `NavigationRail` to `PrimaryNavDock` | `UI_PATTERNS.md` (if it referenced the old name) |
| New save field with migration | `ARCHITECTURE.md`, `CURRENT_STATUS.md`, `CHANGELOG.md` |
| New fixture in the visual regression matrix | `VISUAL_REGRESSION.md` |
| Promotion of `art/source/foo.svg` → `game/assets/foo.png` | `ASSET_INVENTORY.md`, `LICENSING_AND_ATTRIBUTION.md` |
| Bump a hard rule about expedition return | `world-of-goses-design-bible/05_EXPEDITIONS.md` |
| New CITIZEN.md convention | `REPOSITORY_CONVENTIONS.md` |
| Test-only fix | none |
| Mechanical rename of a private field | none |

## See also

- [`WORKFLOW_MODES.md`](WORKFLOW_MODES.md) — CHANGELOG policy by mode.
- [`RISK_MODEL.md`](RISK_MODEL.md) — risk classification.
- `docs/ai/AGENT_COLLABORATION_PROTOCOL.md` §1 step 14 — original
  "update only if invariant / decision / state changed" rule that
  this gate formalizes.