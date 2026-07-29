---
name: narrative-lore
description: >
  Own cosmology, world history, Rabata, Burgoses, founder lore, lineage
  culture, dialogue, chronicle, names, descriptions, voice and tone, and
  diegetic text. Required for any task that writes lore, dialogue, event
  text, or UI copy that must be coherent with the world's cultures. This
  skill may not invent mechanics or bonuses.
license: World of Goses project license
compatibility: Documentation-only; references the design bible and game locale catalogs.
metadata:
  domain: narrative
  layer: domain
  audience: every agent
---

# Narrative and lore

## Purpose

Keep the world's voice coherent: the eight lineages, the founder, the
astral arrival, the chronicle, and every piece of diegetic text. Narrative
proposes implications; mechanics decide.

## When to use

- Writing or editing onboarding dialogue, founder narration, or chronicle
  entries.
- Naming a new place, person, faction, building, route, or expedition.
- Translating or localizing diegetic text (`game/locale/`).
- Reviewing voice, tone, or cultural coherence of a feature's copy.

## Required documentation

- `docs/world-of-goses-design-bible/01_GAME_VISION.md` — fantasy and naming
  discipline.
- `docs/world-of-goses-design-bible/06_LINEAGES.md` — per-lineage cultural
  identity.
- `docs/world-of-goses-design-bible/07_ONBOARDING_AND_FOUNDER.md` —
  founder, astral arrival, profile composition.

## Conditional documentation

- `docs/world-of-goses-design-bible/02_CORE_GAMEPLAY_PILLARS.md` pillar 5
  (environment).
- `docs/world-of-goses-design-bible/09_AUDIO_GUIDELINES.md` — per-lineage
  sonic identity, when relevant.
- `docs/world-of-goses-design-bible/05_EXPEDITIONS.md` — for expedition
  narration.
- `docs/UI_PATTERNS.md` — for UI copy rules.
- `docs/LICENSING_AND_ATTRIBUTION.md` — when naming external references.

## Core invariants

- Public-facing names, art, lore, and implementation must remain
  independently created. Provisional inspiration does not promote to
  shipping terminology. *(bible/01)*
- Lineages are cultural identities. Each lineage has its own voice,
  imagery, and sonic identity. *(bible/06)*
- The environmental alignment axis is independent of lineage identity.
  *(bible/02)*
- Narrative never invents mechanics. A narrative proposal that affects
  mechanics must be routed to the owning domain agent and approved there.
- All player-facing text goes through the localization layer and is
  validated by `tools/Test-LocalizationCatalog.ps1`.
- Localization happens at display, never at snapshot. Wrapping `UiText.Get`
  inside a presentation record breaks Godot-free xUnit tests.

## Expected workflow

1. Read the fantasy, founder, and lineage chapters.
2. Identify the lineage(s), cultural register, and diegetic level.
3. Draft in the appropriate voice. Keep copy short and concrete.
4. For dialogue, route the change through `Dialogue.cs` / `DialogueRunner.cs`
   and the founder session catalog.
5. For chronicle entries, append via `WorldEventLog` and the formatter in
   `game/scripts/Ui/WorldEventTextFormatter.cs`.
6. Add keys to `game/locale/messages.pot`, `en.po`, and `es.po`. Validate
   the catalogs.
7. If the change implies a mechanical effect, stop and route to the
   mechanical agent. Do not invent bonuses.

## Files commonly involved

- Domain: `game/scripts/Domain/Dialogue.cs`, `DialogueRunner.cs`,
  `FounderNarrativeCatalog.cs`, `FounderNarrativeModels.cs`,
  `FounderNarrativeScorer.cs`, `FounderNarrativeSession.cs`, `Tr.cs`,
  `WorldEvent.cs`.
- Presentation: `game/scripts/AstralOnboardingView.cs`,
  `OnboardingView.cs`, `FounderArrivalSequence.cs`,
  `game/scripts/Ui/WorldEventTextFormatter.cs`, `Notifier.cs`,
  `TutorialOverlay.cs`.
- Localization: `game/locale/{messages.pot,en.po,es.po}`.
- Tests: `tests/WorldofGoses.Tests/FounderNarrativeTests.cs`,
  `OnboardingDomainTests.cs`, `ProfileAndOnboardingTests.cs`,
  `DialogueRunnerTests.cs`, `WorldEventLogTests.cs`,
  `WorldEventCausalityTests.cs`.

## Tests to run

- `dotnet test --filter "FullyQualifiedName~FounderNarrative"`
- `dotnet test --filter "FullyQualifiedName~Dialogue"`
- `dotnet test --filter "FullyQualifiedName~Onboarding"`
- `dotnet test --filter "FullyQualifiedName~WorldEvent"`
- `pwsh tools/Test-LocalizationCatalog.ps1`

## Cross-domain consultation rules

- `citizens-rpg` whenever the copy refers to a specific citizen's state,
  competency, or commitment.
- `lineages-and-cultures` whenever the copy names a lineage or implies a
  cultural effect.
- `presentation-experience` whenever UI patterns, accessibility, or theme
  are involved.
- `expeditions-territory` whenever the copy describes an expedition
  outcome.
- `city-simulation` whenever the copy describes a city event.
- `technical-foundation` whenever a chronicle entry adds an event type or
  changes retention.

## Things not to do

- Do not invent mechanical bonuses, multipliers, or unlocks.
- Do not use "resonance with another lineage" to explain personal
  aptitudes. *(bible/07)*
- Do not promote a provisional inspiration name to shipping terminology.
- Do not bypass the localization layer for player-facing text.
- Do not translate at the snapshot layer.
- Do not split cultural identity from lineage or imply that one lineage is
  objectively better.

## Definition of done

- Voice and tone match the fantasy, the founder arc, and the lineage.
- All keys exist in the locale catalogs; `Test-LocalizationCatalog.ps1` is
  green.
- Tests cover founder scoring, dialogue runner, and chronicle event
  appending.
- If the change has mechanical implications, the owning agent has signed
  off and `DECISION_LOG.md` is updated if a rule changed.