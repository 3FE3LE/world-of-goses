---
name: presentation-experience
description: >
  Own scenes, UI, UX, screen flow, accessibility, pixel art, sprites,
  animation, iconography, typography, audio, and feedback. Required whenever
  a task touches a scene, a UI panel, an animation, an asset, audio, or
  feedback. This skill currently owns visual art and audio together; future
  work may split them into visual-art-direction and audio-direction agents.
  Until then, no other agent should make presentation decisions for these
  domains.
license: World of Goses project license
compatibility: Documentation-only; references the design bible, ART_PIPELINE.md, UI_PATTERNS.md, and AUDIO_GUIDELINES.
metadata:
  domain: presentation
  layer: presentation
  audience: every agent
---

# Presentation experience

## Purpose

Make sure that presentation renders state and never decides rules. The
visual layer reads domain state and translates it into scenes, UI, audio,
sprite animations, and feedback. Domain logic stays in `game/scripts/Domain/`.

## When to use

- Adding or modifying a scene, a UI panel, or a screen flow.
- Adding or modifying a sprite, an animation, an icon, or a font.
- Adding or modifying an audio asset, an audio bus, or a feedback
  reaction.
- Reviewing accessibility, theming, or pixel-perfect correctness.

## Required documentation

- `docs/world-of-goses-design-bible/08_VISUAL_UI_AND_ASSET_GUIDELINES.md`.
- `docs/world-of-goses-design-bible/09_AUDIO_GUIDELINES.md`.
- `docs/ART_PIPELINE.md`.
- `docs/UI_PATTERNS.md`.
- `docs/UI_AUDIT.md`.
- `docs/REPOSITORY_CONVENTIONS.md` → §7 (Godot conventions) and §9 (asset
  rules).

## Conditional documentation

- `docs/VISUAL_REGRESSION.md` — when the change affects the visual review
  matrix.
- `docs/PERFORMANCE_BUDGETS.md` — when the change may affect frame time
  (animations, particles, sprites).
- `docs/LICENSING_AND_ATTRIBUTION.md` — when adding or changing assets.
- `docs/lineage_design_matrix.md` — when changing the per-lineage sprite
  language or palette.

## Core invariants

- Presentation renders state; it does not decide rules. No domain logic in
  scenes, in `_Process`, or in `_PhysicsProcess`. *(bible/10)*
- Pure 2D pixel art. Integer scale, nearest filter, integer positions.
  Logical resolution 1280 x 720. *(bible/08, bible/10)*
- Motion uses a discrete cadence grammar. Camera and world navigation use
  quantized steps, never smooth continuous 1:1 motion. *(bible/08)*
- Do not communicate a state by color alone.
- UI is functionally shared across lineages. Lineage themes may change
  palette, borders, corners, fills, shadows, patterns, selection,
  micro-animations, and icon treatment — never navigation, hierarchy,
  semantics, minimum sizes, or accessibility. *(bible/08)*
- Provisional assets do not define the final art direction. *(bible/10)*
- HUD lives on a `CanvasLayer` independent of the world `Camera2D`.
  *(bible/10)*
- Visual art and audio are owned by this skill until the future split.

## Expected workflow

1. Identify the scene, UI element, asset, or audio change.
2. Read the relevant documentation above.
3. Decide whether the change is a state read (presentation only) or a
   state write (raise it to the owning domain agent).
4. For UI, follow `docs/UI_PATTERNS.md`. Reuse `GameUiShell`,
   `ModalHost`, `PanelHeader`, `SafeAreaMarginContainer`,
   `StandardButtons`, `TooltipPanel`.
5. For pixel art, follow `docs/ART_PIPELINE.md`. Source in
   `art/source/<category>/`. Export to `art/exports/<category>/`. Imported
   asset lives under `game/assets/<category>/`. Do not hand-edit
   exported PNGs.
6. For animations, use `AnimatedSprite2D` for frame-based, use
   `AnimationPlayer` for procedural. *(REPOSITORY_CONVENTIONS.md §7)*
7. For audio, follow `docs/world-of-goses-design-bible/09_AUDIO_GUIDELINES.md`.
   The bus tree must isolate Music, Ambience, UI, City, Buildings,
   Expeditions, Voices, Critical.
8. For accessibility, do not communicate by color alone; respect safe
   areas; respect minimum sizes; provide localization keys.
9. Verify the change with a **real click**, not just code reading or a
   headless boot. A click-to-X flow that cannot be exercised is not done.
10. Update `docs/VISUAL_REGRESSION.md` if a new visual surface is exposed.

## Files commonly involved

- Scenes: `game/scenes/CityPrototype.tscn`, `OnboardingView.tscn`,
  `HeroProfileView.tscn`, `LineageShowcase.tscn`, `PauseMenu.tscn`,
  `game/scenes/Components/*.tscn`,
  `game/scenes/prototypes/*.tscn`.
- UI: `game/scripts/Ui/*.cs`, `game/scripts/visual/*.cs`,
  `game/scripts/*Panel.cs`, `game/scripts/*View.cs`.
- Assets: `art/source/`, `art/exports/`, `game/assets/`.
- Tests: `tests/WorldofGoses.Tests/UiSnapshotTests.cs`,
  `LineageThemeRegistryTests.cs`, `CharacterVisualRegistryTests.cs`,
  `MacroStreetLiveViewTests.cs`, `StreetDepthProjectionTests.cs`,
  `StreetRoutePlannerTests.cs`, `ControllerLoadSeamTests.cs`.

## Tests to run

- `dotnet test --filter "FullyQualifiedName~UiSnapshot"`
- `dotnet test --filter "FullyQualifiedName~Lineage"`
- `dotnet test --filter "FullyQualifiedName~CharacterVisual"`
- `dotnet test --filter "FullyQualifiedName~ControllerLoad"`
- `dotnet test --filter "FullyQualifiedName~Street"`
- `pwsh tools/Capture-VisualMatrix.ps1` — when in scope.

## Cross-domain consultation rules

- `core-game-vision` for any change that affects what the player does,
  decides, or perceives.
- `lineages-and-cultures` for any per-lineage visual or audio variation.
- `narrative-lore` for dialogue, voice, and chronicle text in the UI.
- `citizens-rpg` when the UI represents personal state.
- `city-simulation` when the UI represents city state.
- `expeditions-territory` when the UI represents expedition state.
- `technical-foundation` when the change introduces a snapshot, a
  presentation adapter, or a layer-boundary concern.

## Things not to do

- Do not put domain rules in scenes, in `_Process`, or in `_PhysicsProcess`.
- Do not query the world from a panel; consume snapshots.
- Do not introduce antialiasing on pixel-art sprites.
- Do not bypass integer positions in pixel-art edges.
- Do not communicate a state by color alone.
- Do not bypass the localization layer.
- Do not hand-edit exported PNGs. Re-export from `art/source/`.
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
- `UiSnapshotTests`, the visual regression matrix (where applicable), and
  the domain boundary tests are green.