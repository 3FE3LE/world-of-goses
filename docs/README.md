# World of Goses — Documentation index

> Single map of every document that lives under `docs/`. This folder
> hosts one **canonical design source** (the design bible) and a small
> set of **implementation-aware** docs that explain what the code does
> today. When the two disagree on *what the game is*, the bible wins.
> When they disagree on *what ships next*, the implementation docs
> win.

## Canonical design source

[`world-of-goses-design-bible/`](world-of-goses-design-bible/README.md) is the single source of truth for the game's design. Every section that used to be duplicated across the old `docs/*.md` files now lives there. Add new vision, pillar, lineage, audio, or visual-direction content to the bible, then link to it from elsewhere.

| Chapter | Owns |
| --- | --- |
| `world-of-goses-design-bible/01_GAME_VISION.md` | Fantasy, single-city rule, absence rule, 14 design principles, originality boundary, naming discipline, IP boundary. |
| `world-of-goses-design-bible/02_CORE_GAMEPLAY_PILLARS.md` | Nine pillars: city dev, expeditions, citizens with trajectory, causal production, territory, health, environment (regenerative/extractive), delegation, organic difficulty. |
| `world-of-goses-design-bible/03_CITY_TERRITORY_AND_GROWTH.md` | Macro view, building-detail scenes, parcel states, expansion, eight axes of growth, architecture by culture. |
| `world-of-goses-design-bible/04_CITIZENS_PROFESSIONS_AND_HEROES.md` | The single `Citizen` entity, identity composition, hero states, scale separation, five layers of competence, twelve professional families, data and balance rules. |
| `world-of-goses-design-bible/05_EXPEDITIONS.md` | Expedition pillar, automatic control, configuration surface, defeat consequences, priority animations. |
| `world-of-goses-design-bible/06_LINEAGES.md` | Eight lineages with idea, nature, culture, affinities, tensions, and architectural identity per lineage. |
| `world-of-goses-design-bible/07_ONBOARDING_AND_FOUNDER.md` | Onboarding tone, ambiguous situations, profile schema, founder influence on the city. |
| `world-of-goses-design-bible/08_VISUAL_UI_AND_ASSET_GUIDELINES.md` | Pixel art direction, three visual scales, typography hierarchy, Sixteen Pixel Perfect, per-lineage visual identity. |
| `world-of-goses-design-bible/09_AUDIO_GUIDELINES.md` | Synthetic retro audio, bus layout, per-lineage sonic identity, first audio pack. |
| `world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md` | Stack, simulation rules, scene map, pixel-perfect rules, persistence direction, guard-rails, open questions. |

## Implementation-aware docs

These files describe what the code does today, the recommended slice sequence, and the validation snapshot. They **point to the bible for design questions** instead of restating them.

| File | Owns |
| --- | --- |
| `CURRENT_STATUS.md` | What slice is live, what the next proof should be, verification commands. |
| `ARCHITECTURE.md` | Folder layout, the engine/domain boundary, three visual scales, persistence boundary, what is out of scope. |
| `UI_PATTERNS.md` | North-star rules for reusable UI: PackedScene / `[GlobalClass]` / static factory, naming, state binding via signals, theming hierarchy, save/load integration, navigation, per-PR audit checklist. **Read this before authoring any new screen or widget.** |
| `UI_AUDIT.md` | Manual checklist + history of signature for the current UI state after each stabilisation slice. |
| `VALIDATION.md` | Honest cross-check of the current slice against the bible's vision and pillars, ranked gaps. |
| `ART_PIPELINE.md` | Pixelorama → PNG → Godot file flow, naming conventions, SpriteFrames / TileMapLayer wiring, anti-patterns. |
| `LINEAGE_DESIGN_MATRIX.md` | Visual translation matrix shipped with the Universal LPC lineage character pack. |
| `MANIFEST.json` | Machine-readable manifest for the 16 imported lineage character scenes. |
| `LICENSING_AND_ATTRIBUTION.md` | Universal LPC provenance, transformations, distribution obligations, and attribution pointers. |
| `licenses/` | Full and selected LPC credits plus the generator's GPL-3.0 license text. |
| `PRODUCT_DIRECTION.md` | **Process guide.** Core loop, validation sequence, UI/UX rules, drift checklist, alignment questions. Points to the bible for design answers. |
| `GAME_VISION.md` | **Pointer.** The vision, principles, and pillars live in the bible; this file maps old sections to their new homes. |
| `LINEAGES_AND_PROFESSIONAL_AFFINITIES.md` | **Pointer.** The eight lineages, twelve-family vocabulary, five layers, and balance rules live in the bible; this file maps old sections to their new homes. |
| `DESIGN_INFLUENCES.md` | **Pointer + audit trail.** The IP boundary and naming discipline live in the bible; the retired MVP shorthand stays here as a historical note. |

The optional lineage splash workflow is documented beside the tool at
[`art/world-of-goses-minimax-splash-generator/README.md`](../art/world-of-goses-minimax-splash-generator/README.md).
Its generated output is local concept material, not a runtime dependency or an
automatic addition to `game/assets/`.

## Authority rules

1. The most recent explicit decision wins.
2. The product vision wins over a temporary prototype.
3. The domain wins over its visual representation.
4. The player experience wins over a thorough simulation that adds no
   gameplay value.
5. A mechanic is not implemented only because it is technically
   possible.
6. **Design content lives in the bible exactly once.** If a section
   appears here, it is either (a) implementation-aware (status, code
   structure, validation snapshot, process guide) or (b) a pointer to
   the bible chapter that owns the content.
7. **New UI flows through `UI_PATTERNS.md`.** Before opening a screen
   or widget, read `UI_PATTERNS.md`. The three reusable patterns
   (PackedScene, `[GlobalClass]`, static factory) plus the per-PR
   audit checklist there are the guardrail against the divergent
   per-callsite widget definitions that already cost a slice.

## When the two sets disagree

| Topic | Owner |
| --- | --- |
| "What is the fantasy?" | bible `01_GAME_VISION.md` |
| "What principles bind the design?" | bible `01_GAME_VISION.md` § *Principios de diseño* |
| "What pillars must the prototype prove?" | bible `02_CORE_GAMEPLAY_PILLARS.md` |
| "What does each lineage mean and look like?" | bible `06_LINEAGES.md` + `08_VISUAL_UI_AND_ASSET_GUIDELINES.md` |
| "How should it sound?" | bible `09_AUDIO_GUIDELINES.md` |
| "What is the current slice and what ships next?" | `CURRENT_STATUS.md` |
| "How is the code organised today?" | `ARCHITECTURE.md` |
| "How should I build a new screen or widget?" | `UI_PATTERNS.md` |
| "What does the current UI look like in practice?" | `UI_AUDIT.md` |
| "How do I validate a new slice?" | `PRODUCT_DIRECTION.md` |
| "What must the slice still prove?" | `VALIDATION.md` |
| "Where do files live and how are they named?" | `ART_PIPELINE.md` |
