---
name: godot-presentation
description: >
  Use for Godot 4.7 presentation concerns: Control/Container layout,
  theming, focus navigation, AnimationPlayer, AnimationTree, Tween, audio
  buses, and the asset pipeline. Project rules still come from
  presentation-experience and the relevant domain skill. Engine API
  specifics are delegated to the verified upstream provider registered
  by Install-GodotDotNetSkills.ps1.
license: World of Goses project license
compatibility: Godot.NET.Sdk 4.7.x; pixel art integer scale + nearest filter.
metadata:
  type: technical-capability
  layer: presentation
  audience: presentation-experience and gameplay-integrator
---

# Godot presentation

## Purpose

A stable local adapter that points to the current upstream provider of
Godot 4 presentation knowledge without baking a vendor name into the
project. The local skill enforces only the project's presentation
invariants; engine API specifics are delegated.

## When to use

- Building or modifying Control nodes (anchors, containers, themes,
  focus).
- Wiring AnimationPlayer, AnimationTree, or code-driven Tween.
- Wiring AudioStreamPlayer and bus routing.
- Working with the asset pipeline (Pixelorama → PNG → Godot; the
  `art/source/`, `art/exports/`, `game/assets/` directories).
- Authoring or editing `.tscn` or `.tres` files.

## Provider delegation

The verified upstream provider is the only authoritative source for
Godot 4 presentation APIs. It is installed by
`Install-GodotDotNetSkills.ps1` and recorded in
`docs/ai/SKILL_MIGRATION.md`. When the provider changes, only this
file and the migration report change.

## Core invariants

- Pixel art is integer scale, nearest filter, no antialiasing.
  Placeholders are not final direction.
- Presentation renders state; it does not decide rules.
- No domain logic in `_Process`.
- `partial` classes only for Godot source generators.
- `*.tscn` and `*.tres` are version-controlled; no hand-edited PNGs in
  `game/assets/`.

## Required documentation

- `docs/UI_PATTERNS.md`, `docs/UI_AUDIT.md`.
- `docs/world-of-goses-design-bible/08_VISUAL_UI_AND_ASSET_GUIDELINES.md`.
- `docs/ART_PIPELINE.md`, `docs/ASSET_INVENTORY.md`,
  `docs/LICENSING_AND_ATTRIBUTION.md`.
- `docs/world-of-goses-design-bible/09_AUDIO_GUIDELINES.md` for audio.

## Workflow

1. Load this skill with `presentation-experience` and
   `repo-navigation`.
2. Open the project's presentation docs above.
3. For engine API specifics, query the verified upstream provider.
4. For a click-to-X flow, verify with a real click — code reading and
   headless boot are not sufficient. See
   `docs/ai/CONTEXT_MAP.md` → Presentation → UI.

## Cross-domain consultation rules

- Always paired with `presentation-experience`.
- For per-lineage UI variations, also load `lineages-and-cultures`.
- For UI that displays citizen, city, or expedition state, also load
  the relevant domain skill.

## Things not to do

- Do not duplicate the upstream provider's content here.
- Do not hand-edit the generated mirror skills under `.claude/` or
  `.codex/`.
- Do not bypass the asset pipeline.

## Definition of done

- The change renders correctly in the running Godot client, captured
  via `tools/Capture-VisualMatrix.ps1` when presentation-affecting.
- A click-to-X flow has been exercised with a real input event, not
  inferred from code.
- The upstream provider used is named in the change report.
