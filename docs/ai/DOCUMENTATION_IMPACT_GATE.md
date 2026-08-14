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
| Reusable UI contract (new pattern, new widget, renamed widget) | `docs/presentation/ui-patterns.md` |
| Architecture / boundary change (new layer, new ownership rule) | `docs/engineering/architecture.md`, `docs/engineering/state-authority.md` |
| Canonical game design change (new mechanic, new fantasy, new pillar) | the owning file under `docs/systems/` or `docs/world/` |
| New asset promoted (`art/source` → `art/exports` → `game/assets`) | `docs/presentation/asset-inventory.md`, `docs/presentation/licensing-and-attribution.md` |
| New visual regression surface or fixture | `docs/engineering/visual-regression.md` |
| New persistent field, schema bump, migration | `WorldSave.CurrentVersion` XML docs, `docs/engineering/architecture.md` §8 |
| Work started, finished or reprioritised | the GitHub issue — **never** a document |
| Canonical decision changed (DEC-#### entries) | `docs/history/decisions.md` |
| New agent or skill added / removed | `docs/ai/CONTEXT_MAP.md`, `docs/ai/AGENT_DISPATCH.md` |
| Repository convention change | `docs/engineering/conventions.md` |
| Otherwise | **NO DOCUMENT UPDATE** |

## Anti-patterns

- **Documentation sweeps.** Opening every doc the change touches to
  "keep them in sync" is not maintenance; it is noise. The gate is
  designed to stop this.
- **Editing canon to reflect the implementation.** A canonical
  document is the source of truth, not the implementation log.
  If the implementation contradicts canon, fix the
  implementation.
- **Recording progress in a document.** Status, milestones and next steps
  belong to GitHub Issues and `CHANGELOG.md`, never to canon.
- **Adding a CHANGELOG entry for a test-only fix or a typo.** See
  [`WORKFLOW_MODES.md`](WORKFLOW_MODES.md) for the CHANGELOG policy.
- **Re-reading the whole `docs/` tree "to be safe".** That is what
  `repo-navigation` is for.

## How the gate interacts with workflow modes

| Mode | Doc-update expectation |
| --- | --- |
| SURGICAL | Apply the gate; almost always results in **no** doc update. |
| FEATURE | Apply the gate once at closure; expect one or two targeted doc updates if a contract moved. |
| RELEASE | Apply the gate at every step where a contract moved; expect several targeted doc updates plus a `CHANGELOG.md` entry. |

## Worked examples

| Change | Docs updated |
| --- | --- |
| 8 px HUD spacing tweak | none (no contract change) |
| Rename `NavigationRail` to `PrimaryNavDock` | `presentation/ui-patterns.md` (if it referenced the old name) |
| New save field with migration | `WorldSave.CurrentVersion` XML docs, `CHANGELOG.md` |
| New fixture in the visual regression matrix | `engineering/visual-regression.md` |
| Promotion of `art/source/foo.svg` → `game/assets/foo.png` | `presentation/asset-inventory.md`, `presentation/licensing-and-attribution.md` |
| Bump a hard rule about expedition return | `systems/expeditions.md` |
| New naming convention | `engineering/conventions.md` |
| Test-only fix | none |
| Mechanical rename of a private field | none |

## See also

- [`WORKFLOW_MODES.md`](WORKFLOW_MODES.md) — CHANGELOG policy by mode.
- [`RISK_MODEL.md`](RISK_MODEL.md) — risk classification.
- `docs/ai/AGENT_COLLABORATION_PROTOCOL.md` §1 step 14 — original
  "update only if invariant / decision / state changed" rule that
  this gate formalizes.