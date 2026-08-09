# Domain consultation rule

> Reading existing state does **not** activate domain ownership.
> Changing state semantics **does**. This rule keeps a UI tweak
> from pulling in every domain skill.

## The rule

```
READING EXISTING STATE      does not activate domain ownership
CHANGING STATE SEMANTICS    does
```

This rule applies symmetrically to all domains:

- `citizens-rpg`
- `city-simulation`
- `expeditions-territory`
- `narrative-lore`
- `lineages-and-cultures`
- `technical-foundation`

## Examples

### Citizens

| Action | Activates `citizens-rpg`? |
| --- | --- |
| `CitizenSummaryPanel` reads `snapshot.Citizens[0].Name` | NO |
| `CitizenSummaryPanel` reads `snapshot.Citizens[0].WoundSeverity` | NO |
| Add a new field to `Citizen` | YES |
| Change how wound severity is calculated | YES |
| Change how the player can apply first aid | YES |
| Change the persistent contract for a citizen | YES |
| Rename `WoundSeverity` to `InjuryLevel` (mechanical rename) | NO (but see note) |

Note: the rename example is a `SURGICAL` change to the code, but the
**schema bump** that follows it crosses `technical-foundation` and is
at least `FEATURE`.

### City

| Action | Activates `city-simulation`? |
| --- | --- |
| `CitySummaryPanel` reads `snapshot.Population` | NO |
| `CitySummaryPanel` reads `snapshot.Resources.Wood` | NO |
| Change how `Population` is calculated | YES |
| Change what `Population` means | YES |
| Change how the player can alter `Population` | YES |
| Change the persistent contract for a building | YES |

### Expeditions

| Action | Activates `expeditions-territory`? |
| --- | --- |
| `ExpeditionRail` reads `snapshot.Expeditions[0].Status` | NO |
| `ExpeditionRail` reads `snapshot.Expeditions[0].Members` | NO |
| Change how retreat is decided | YES |
| Change what "encounter" means | YES |
| Change the parcel unlock rules | YES |
| Change the persistent contract for an expedition | YES |

### Construction / production / lineage / narrative

Same shape:

- **Reading** existing state → no domain consultation.
- **Changing** the formula, semantics, persistent contract, or player
  decision → domain consultation.

## How this interacts with the workflow modes

| Mode | Domain consultation rule |
| --- | --- |
| SURGICAL | Reading-only is the default. Skill is loaded **only** if the change crosses into semantics. |
| FEATURE | Default: load only the primary skill. Add a domain consultant when the change touches semantics. |
| RELEASE | Default: load every affected domain skill. Reading-only changes still do not pull in the domain unless an invariant is touched. |

## Anti-patterns

- **Loading `city-simulation` because the HUD shows `Population`.**
  The HUD reads state; it does not decide state.
- **Loading `narrative-lore` because the HUD shows the founder's
  name.** The HUD reads a `string`; it does not invent lore.
- **Loading `technical-foundation` because the HUD reads a
  snapshot.** Snapshots are read-only by contract.
- **Loading all domain skills because the prompt mentions a citizen.**
  The prompt may have mentioned a citizen in passing; consult
  `AGENT_DISPATCH.md` §5 self-check before loading.

## Where the rule applies

- `AGENT_DISPATCH.md` §5 — self-check questions. "Am I relevant?"
  must be answered with the rule in mind: reading is not relevant,
  writing is.
- `CONTEXT_MAP.md` — route selections. The route does not override
  the rule; a UI route can land here without activating the domain.
- `AGENT_COLLABORATION_PROTOCOL.md` §1 step 4 — load only the
  conditional skills whose trigger fires. Reading existing state is
  not a trigger.
- The skill files themselves — `citizens-rpg`, `city-simulation`,
  `expeditions-territory`, `narrative-lore`, `lineages-and-cultures`,
  `technical-foundation`.

## See also

- [`WORKFLOW_MODES.md`](WORKFLOW_MODES.md) — SURGICAL / FEATURE / RELEASE.
- [`RISK_MODEL.md`](RISK_MODEL.md) — risk classification.
- [`DOCUMENTATION_IMPACT_GATE.md`](DOCUMENTATION_IMPACT_GATE.md).
- `docs/ai/AGENT_DISPATCH.md` — keyword and symptom-based dispatch.
- `docs/ai/AGENT_COLLABORATION_PROTOCOL.md` §1 — operating flow.