# Feature or issue handoff

> Copy this template into the working branch or PR description. Remove any
> section that does not apply; never leave a section empty without a one-line
> reason.

## Request

Plain description of what was asked and why.

## Primary agent

Which agent owns the change. See `docs/ai/CONTEXT_MAP.md`.

## Consulting agents

Which agents were consulted and what they contributed.

## Main domain

Citizens, city, expeditions, narrative, presentation, persistence, etc.

## Related domains

Anything touched, even as a side effect.

## Documentation required

Canonical docs read. Cite the exact files.

## Current behavior

What happens today, including reproducible inputs and observed outputs.

## Desired behavior

What should happen after the change.

## Player decision introduced

The decision the player makes. If none, say so explicitly.

## Consequence communicated

The consequence the player sees. If none, say so explicitly.

## Affected invariants

Name each from `docs/ai/CROSS_DOMAIN_INVARIANTS.md`.

## Domain changes

Files and types added, removed, or modified in the domain.

## Persistence changes

DTOs added, schema version impact, migration path, round-trip evidence.

## Offline progression changes

Impact on `OfflineProgression`, `WorldTimeAdvance`, catch-up behavior.

## Presentation changes

Scenes, UI, audio, sprite, animation, theme changes.

## Narrative changes

Dialogue, chronicle, event, or lore additions.

## Migration requirements

If a save version bumps, document the exact migration. Otherwise say
"no version change required".

## Tests

New and updated tests, including the regression test for a bug fix.

## Out of scope

Things the change explicitly does not touch. Make this list honest.

## Definition of done

- [ ] `dotnet build` clean from `game/`.
- [ ] `dotnet test` from `tests/WorldofGoses.Tests/` passes.
- [ ] No `using Godot` introduced under `game/scripts/Domain/`.
- [ ] Invariants from `docs/ai/CROSS_DOMAIN_INVARIANTS.md` still hold.
- [ ] Documentation updated if a rule, decision, or state changed.
- [ ] `quality-guardian` review verdict filed.
- [ ] If a save version change: a real saved file was loaded successfully or
      the change is justified with a test fixture.

## Review result

Verdict and findings from `quality-guardian`. Each finding is either
"addressed" with a short note, or "won't fix" with a reason.
