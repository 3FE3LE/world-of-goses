---
name: lineages-and-cultures
description: >
  Cross-cutting skill for the eight lineages (Ardhen, Eirune, Kovari, Myrven,
  Vaelun, Orveth, Caelith, Theryn). Use whenever a task touches lineage in any
  domain — citizen, narrative, presentation, audio, city, expeditions. There
  is no lineage agent. This skill is consulted by whichever domain is
  changing.
license: World of Goses project license
compatibility: Documentation-only; references docs/world/lineages.md and the per-lineage documents.
metadata:
  domain: lineages
  layer: cross-cutting
  audience: every agent
---

# Lineages and cultures

## Purpose

Make sure that every lineage change respects the invariants that protect the
game from becoming a class-based system or a generic multiplier system.
Lineage is cultural identity with optional mechanical flavor — never a gate,
never an automatic bonus.

## When to use

- Adding or modifying a lineage-affinity effect.
- Adding lineage-aware UI theming, sprite language, or sonic identity.
- Reviewing a feature that "uses lineage" for any reason.

## Required documentation

- `docs/world/lineages.md` — canonical lineage
  index (one chapter per lineage follows in bible/14–21).
- `docs/systems/14-21_LINEAGES_*.md` — detailed
  culture + system guideline + signature + cube vertex per lineage.
- `docs/systems/kovari-cube.md` — canonical cube
  mechanics (axes, vertices, elemental affinities, stats, equipment,
  shadow mode, migration).
- `docs/systems/citizens.md`
  — lineage rules in the citizen chapter.
- `docs/world/lineages.md` — visual translation table.

## Conditional documentation

- `docs/presentation/visual-language.md` —
  for sprite language, borders, palette, and per-lineage UI.
- `docs/presentation/audio.md` — for per-lineage
  sonic identity.
- `docs/world/vision-and-pillars.md` pillar 5
  — environmental axis (independent of lineage).

## Core invariants

- Lineages are not professions and not combat classes. *(bible/06)*
- Lineages do not block professions, do not guarantee competence, and do not
  replace real experience. *(bible/04, bible/06)*
- Affinities accelerate learning and transfer. They do not grant exclusive
  ownership of a trade. *(bible/06)*
- An affinity must not become an automatic production bonus. *(bible/04)*
- Every profession admits eight approaches. *(bible/06)*
- The environmental axis is independent of lineage identity, and must not
  become binary morality. *(bible/02, bible/10)*
- Lineage UI themes may change palette, borders, corners, fills, shadows,
  patterns, selection, micro-animations, and icon treatment — never
  navigation, hierarchy, semantics, minimum sizes, or accessibility.
  *(bible/08)*

## Expected workflow

1. Identify which lineage the change touches and which aspect (cultural,
  visual, sonic, mechanical flavor).
2. Read the canonical chapter and the lineage design matrix.
3. Confirm the change does not block a profession, guarantee competence,
  or become an automatic bonus.
4. For UI or audio changes, restrict the variation to the allowed surface
  listed in `bible/08`.
5. For mechanical flavor (e.g. a learning-speed modifier), keep it
  proportional to invested time, never a fixed bonus.
6. Add tests that prove the change does not produce a fixed bonus, a
  blocked profession, or a guaranteed competence.
7. Update `docs/world/lineages.md` if the
  canonical lineage description changed.

## Files commonly involved

- Domain: `game/scripts/Domain/LineageDefinition.cs`, `LineageId.cs`,
  `CitizenProfile.cs`, `CompetencyEntry.cs`.
- Presentation: `game/scripts/visual/CharacterVisualRegistry.cs`,
  `LineageSpritePlayer.cs`, `LineageThemeRegistry.cs` (tests),
  `game/scripts/Ui/GameUiShell.cs`, `ModalHost.cs`, `PanelHeader.cs`.
- Audio: `game/assets/audio/` (currently empty — no buses yet).
- Tests: `tests/WorldofGoses.Tests/LineageThemeRegistryTests.cs`,
  `CharacterVisualRegistryTests.cs`.

## Tests to run

- `dotnet test --filter "FullyQualifiedName~Lineage"`
- `dotnet test --filter "FullyQualifiedName~CharacterVisual"`
- `dotnet test --filter "FullyQualifiedName~DomainBoundary"`

## Cross-domain consultation rules

- `citizens-rpg` whenever lineage affects competency, role, or commitment.
- `narrative-lore` whenever cultural voice or founder arc is touched.
- `presentation-experience` whenever UI theming or sprite language is
  touched.
- `city-simulation` whenever a per-lineage production effect is considered.
  This is the rule most likely to be violated — refuse fixed multipliers.
- `technical-foundation` whenever lineage data is persisted.

## Things not to do

- Do not create a lineage agent. Lineages have no owning agent.
- Do not turn lineage into a class, a profession, or a combat role.
- Do not introduce a per-lineage production multiplier.
- Do not introduce a per-lineage unlock list.
- Do not introduce a "best" lineage. All eight are peers.
- Do not change the functional UI for one lineage in a way that affects
  navigation, hierarchy, semantics, sizes, or accessibility.

## Definition of done

- The change respects all of the invariants above.
- The lineage-affinity effect is, at most, a proportional modifier of an
  existing rule, never a fixed bonus.
- The change is covered by tests that pin the rule (e.g. "this modifier is
  zero at zero investment").
- The lineage design matrix or canonical chapter is updated if a new
  per-lineage visual or cultural element is introduced.