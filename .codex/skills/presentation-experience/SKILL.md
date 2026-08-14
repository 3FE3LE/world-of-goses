---
name: presentation-experience
description: >
  Own scenes, UI, UX, screen flow, accessibility, pixel art, sprites,
  animation, iconography, typography, audio, and feedback. Required
  whenever a task touches a scene, UI panel, animation, asset, audio,
  or feedback. Conditional docs (UI_PATTERNS, ART_PIPELINE, AUDIO,
  VISUAL_REGRESSION, PERFORMANCE, lineage) are loaded only when the
  task's trigger fires — not by default.
license: World of Goses project license
compatibility: Documentation-only.
metadata:
  domain: presentation
  layer: presentation
  audience: every agent
---

# Presentation experience

## Purpose

Make sure that presentation renders state and never decides rules. The
visual layer reads domain state and translates it into scenes, UI,
audio, sprite animations, and feedback. Domain logic stays in
`game/scripts/Domain/`.

## When to use

- Adding or modifying a scene, a UI panel, or a screen flow.
- Adding or modifying a sprite, an animation, an icon, or a font.
- Adding or modifying an audio asset, an audio bus, or a feedback
  reaction.
- Reviewing accessibility, theming, or pixel-perfect correctness.

## Required documentation (always loaded with this skill)

- `docs/engineering/conventions.md` → §7 (Godot conventions) and §9
  (asset rules).

## Conditional documentation (load only when the trigger fires)

| Trigger | Load |
| --- | --- |
| UI layout, panel, screen flow, focus, modal | `docs/presentation/ui-patterns.md` |
| New asset, sprite, atlas, source/export | `docs/presentation/art-pipeline.md`, `docs/presentation/asset-inventory.md`, `docs/presentation/licensing-and-attribution.md` |
| Audio asset, bus, music, SFX, voice | `docs/presentation/audio.md` |
| New visual regression surface or fixture | `docs/engineering/visual-regression.md` |
| Performance claim, frame budget, animation cost | `docs/engineering/performance.md` |
| Per-lineage visual or audio variation | `docs/world/lineages.md` |
| Visual identity or theme work | `docs/presentation/visual-language.md` |

Do not load any of these unless the trigger fires. A spacing tweak
does not need `ART_PIPELINE.md`; a font swap does not need
`AUDIO_GUIDELINES.md`.

## Core invariants

- Presentation renders state; it does not decide rules. No domain
  logic in scenes, in `_Process`, or in `_PhysicsProcess`.
  *(bible/10)*
- Pure 2D pixel art. Integer scale, nearest filter, integer
  positions. Logical resolution 1280 x 720. *(bible/08, bible/10)*
- Motion uses a discrete cadence grammar. Camera and world navigation
  use quantized steps, never smooth continuous 1:1 motion.
  *(bible/08)*
- Do not communicate a state by color alone.
- UI is functionally shared across lineages. Lineage themes may change
  palette, borders, corners, fills, shadows, patterns, selection,
  micro-animations, and icon treatment — never navigation,
  hierarchy, semantics, minimum sizes, or accessibility. *(bible/08)*
- Provisional assets do not define the final art direction.
  *(bible/10)*
- HUD lives on a `CanvasLayer` independent of the world `Camera2D`.
  *(bible/10)*
- Visual art and audio are owned by this skill until the future split.

## Minimal workflow

1. Identify the scene, UI element, asset, or audio change.
2. Decide whether it is a **state read** (presentation only) or a
   **state write** (raise to the owning domain agent). See
   [`docs/ai/DOMAIN_CONSULTATION.md`](../../../docs/ai/DOMAIN_CONSULTATION.md).
3. Load only the conditional docs whose trigger fires.
4. For UI: follow `UI_PATTERNS.md`. Reuse `GameUiShell`, `ModalHost`,
   `PanelHeader`, `SafeAreaMarginContainer`, `StandardButtons`,
   `TooltipPanel`.
5. For pixel art: follow `ART_PIPELINE.md`. Source in
   `art/source/<category>/`. Export to `art/exports/<category>/`.
   Imported asset under `game/assets/<category>/`. Do not hand-edit
   exported PNGs.
6. For animations: `AnimatedSprite2D` for frame-based, `AnimationPlayer`
   for procedural. *(REPOSITORY_CONVENTIONS.md §7)*
7. For audio: follow bible/09 bus tree (Music, Ambience, UI, City,
   Buildings, Expeditions, Voices, Critical).
8. For accessibility: no color-only state, safe areas, minimum sizes,
   localization keys.
9. Verify the change with a **real click**, not just code reading or a
   headless boot.
10. If a new visual regression surface is exposed, update
    `VISUAL_REGRESSION.md`.

## Files commonly involved

- Scenes: `game/scenes/CityPrototype.tscn`, `OnboardingView.tscn`,
  `HeroProfileView.tscn`, `LineageShowcase.tscn`, `PauseMenu.tscn`,
  `game/scenes/Components/*.tscn`,
  `game/scenes/prototypes/*.tscn`.
- UI: `game/scripts/Ui/*.cs`, `game/scripts/visual/*.cs`,
  `game/scripts/*Panel.cs`, `game/scripts/*View.cs`.
- Assets: `art/source/`, `art/exports/`, `game/assets/`.

## Tests to run

- `dotnet test --filter "FullyQualifiedName~UiSnapshot"` — always for
  UI work.
- `dotnet test --filter "FullyQualifiedName~HudComposition"` — when
  HUD composition changed.
- `dotnet test --filter "FullyQualifiedName~Lineage"` — when
  per-lineage presentation touched.
- `dotnet test --filter "FullyQualifiedName~ControllerLoad"` — when
  a scene's controller changed.
- `dotnet test --filter "FullyQualifiedName~Street"` — when
  street / depth projection touched.
- `pwsh tools/Capture-VisualMatrix.ps1` — when a new visual surface is
  exposed.

## Cross-domain consultation rules

- `core-game-vision` only when the change alters player decisions,
  gameplay meaning, information availability, system purpose, fantasy,
  progression, risk/reward, or player agency. See the
  `core-game-vision` skill's "When to use" section.
- `lineages-and-cultures` for any per-lineage visual or audio
  variation.
- `narrative-lore` for dialogue, voice, and chronicle text in the UI.
- `citizens-rpg` / `city-simulation` / `expeditions-territory` only
  when the change writes to domain state, not when it reads it
  (see `DOMAIN_CONSULTATION.md`).
- `technical-foundation` when the change introduces a snapshot, a
  presentation adapter, or a layer-boundary concern.

## Things not to do

- Do not put domain rules in scenes, in `_Process`, or in
  `_PhysicsProcess`.
- Do not query the world from a panel; consume snapshots.
- Do not introduce antialiasing on pixel-art sprites.
- Do not bypass integer positions on pixel-art edges.
- Do not communicate a state by color alone.
- Do not bypass the localization layer.
- Do not hand-edit exported PNGs. Re-export from `art/source/`.
- Do not load the full set of conditional docs by default.
- Do not split visual art and audio out of this skill without the
  documented future split.

## Definition of done

- The presentation reads state; it does not decide rules.
- Pixel-perfect rules are respected (integer scale, nearest filter,
  integer positions).
- Audio, if added, uses the documented bus tree and is licensed
  appropriately.
- Accessibility holds (no color-only state, safe area, minimum sizes,
  localization keys).
- The change was verified with a real click, not just code review.
- The affected `dotnet test` filter is green.
- The visual regression matrix was updated if a new surface is
  exposed.
- The documentation impact gate has been applied — see
  [`docs/ai/DOCUMENTATION_IMPACT_GATE.md`](../../../docs/ai/DOCUMENTATION_IMPACT_GATE.md).