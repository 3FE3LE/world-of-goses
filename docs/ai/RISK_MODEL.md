# Risk model

> Conceptual risk function that classifies a change as `LOW`,
> `MEDIUM`, or `HIGH`. The classification then picks the workflow
> mode in [`WORKFLOW_MODES.md`](WORKFLOW_MODES.md).

## The conceptual function

```
Risk = Persistence
     + DomainSemantics
     + CrossDomainImpact
     + SaveCompatibility
     + PlayerDecisionChange
     + ArchitectureBoundary
```

The function is conceptual — no formula evaluates it. An agent
classifies by asking the questions below and totalling the qualitative
weight.

## Decision questions

For the change at hand, answer each of:

| Question | If YES, contributes |
| --- | --- |
| Does it touch persistence (`game/scripts/Domain/`, `WorldSave`, `*.Save.cs`)? | HIGH |
| Does it change a domain rule, formula, or invariant? | HIGH |
| Does it affect two or more of citizens / city / expeditions / narrative / lineages? | HIGH if both pillars move |
| Does it change save schema, migration, or atomic write? | HIGH |
| Does it change what the player decides, perceives, or risks? | HIGH |
| Does it cross a layer boundary (`Domain` ↔ `Presentation`, `Domain` ↔ `Engine`)? | HIGH |
| Does it introduce a new domain class, system, or invariant? | HIGH |
| Does it change an offline-progression rule? | HIGH |
| Does it introduce or remove a dependency? | HIGH |

If none of the above, ask:

| Question | If YES, contributes |
| --- | --- |
| Is it a new UI component, view, or read-only projection? | MEDIUM |
| Is it a new interaction path that does not change rules? | MEDIUM |
| Is it a new reusable UI pattern? | MEDIUM |
| Is it a small refactor across a layer (single layer)? | MEDIUM |
| Does it touch a scene but not rules? | MEDIUM |

If none of the above either:

| Question | If YES, contributes |
| --- | --- |
| Is it spacing, border, layout, font size, icon replacement? | LOW |
| Is it a visual bug, tooltip, focus fix? | LOW |
| Is it a localized copy correction? | LOW |
| Is it a mechanical rename or comment cleanup? | LOW |
| Is it an equivalent component swap (no semantic change)? | LOW |

## Tiers

### LOW

- Spacing, border, layout, font size, icon replacement.
- Visual bug, tooltip, focus bug.
- Localized copy correction.
- Mechanical rename, comment cleanup, typo.
- Equivalent component swap with no semantic change.
- Test-only fix.

Default workflow: SURGICAL.

### MEDIUM

- New UI component, new read-only projection.
- New interaction path with no rule change.
- New reusable UI pattern.
- Small single-layer refactor.
- Scene restructure without rule change.

Default workflow: FEATURE.

### HIGH

- Save-schema migration, persistence atomic write change.
- Domain rule, formula, citizen lifecycle, economy calculation.
- Combat calculation, offline simulation rule.
- Cross-domain change, new dependency, architecture boundary move.
- New ontology area, new system or invariant.

Default workflow: RELEASE.

## Tie-breaks

When the classification is genuinely ambiguous:

1. **Escalate one level.** Better to spend a few extra seconds than
   to under-ship a regression.
2. **If the change is reversible without save invalidation**, the
   risk is at most MEDIUM.
3. **If the change requires data migration**, the risk is at least
   HIGH.
4. **If the change touches an item in
   [`DOCUMENTATION_IMPACT_GATE.md`](DOCUMENTATION_IMPACT_GATE.md)`**,
   it cannot be SURGICAL — at least FEATURE.

## Tooling

`tools/Get-VerificationPlan.ps1` prints a recommendation based on
file paths and risk heuristics. The script never overrides the
agent's judgment — it is a tie-breaker, not a substitute.

## See also

- [`WORKFLOW_MODES.md`](WORKFLOW_MODES.md) — how risk maps to mode.
- [`DOCUMENTATION_IMPACT_GATE.md`](DOCUMENTATION_IMPACT_GATE.md).
- `docs/ai/AGENT_DISPATCH.md` — symptom-based dispatch.