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
| `world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE.md` | Stack, layer separation, simulation rules, pixel-perfect rules, walkable-camera direction, persistence direction, guard-rails, open design questions. **No sequencing** — the order of work is the proposal §15 and `TO_DO.md`. |
| `world-of-goses-design-bible/11_ELEMENTAL_AFFINITIES_AND_WORLD_INTERACTIONS.md` | Tierra, Agua, Fuego, Aire, Éter y Silencio como contrato común entre onboarding, caras del Cubo, equipamiento, ambiente, ciudad, salud, expediciones y combate. |
| `world-of-goses-design-bible/12_DYNAMIC_FRONTAGE_PLOTS_AND_CORRIDORS.md` | Dynamic frontage rows, construction reservations, structural footprints, corridors, expansion, and their persistence contract. |
| `world-of-goses-design-bible/13_KOVARI_CUBE.md` | Cubo Kovari: geometría, ejes mecánicos, vértices, afinidades elementales, stats derivados, equipamiento, modo sombra. |
| `world-of-goses-design-bible/14_LINEAGES_ARDHEN.md` | Ardhen — cultura + sistema de Anclajes (estructura, integridad, mantenimiento) + vértice del Cubo. |
| `world-of-goses-design-bible/15_LINEAGES_EIRUNE.md` | Eirune — cultura + sistema de Corola (clima, agricultura, redes vivas) + vértice del Cubo. |
| `world-of-goses-design-bible/16_LINEAGES_KOVARI.md` | Kovari — cultura + Cubo aplicado a stats y builds + vértice del Cubo. |
| `world-of-goses-design-bible/17_LINEAGES_MYRVEN.md` | Myrven — cultura + sistema de Máscaras (identidad, ciudadanía, diplomacia) + vértice del Cubo. |
| `world-of-goses-design-bible/18_LINEAGES_VAELUN.md` | Vaelun — cultura + sistema de Brújula (rutas, mapa, expediciones) + vértice del Cubo. |
| `world-of-goses-design-bible/19_LINEAGES_ORVETH.md` | Orveth — cultura + sistema de Relicario (comercio, reservas, custodia) + vértice del Cubo. |
| `world-of-goses-design-bible/20_LINEAGES_CAELITH.md` | Caelith — cultura + sistema de Ciclo (conocimiento, diagnóstico, investigación) + vértice del Cubo. |
| `world-of-goses-design-bible/21_LINEAGES_THERYN.md` | Theryn — cultura + sistema de Octagrama (música, ambiente, ritmo de combate) + vértice del Cubo. |
| `world-of-goses-design-bible/22_STATISTICS_PROGRESSION_AND_COMBAT_FORMULAS.md` | Naturaleza de combate del `Citizen`, competencias y progresión, armas y equipamiento, cuatro familias de estadísticas derivadas, curvas y límites. Coeficientes `v0.1` de balance. |
| `world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md` | El periodo jugable entre el onboarding astral y el primer amanecer: ruta lineal, espíritu de fuego, módulos del Sitio Fundacional, transición a expediciones. Aceptada 2026-08-06, DEC-0014. |

The chapter number is the chapter's stable identity, not its position: it is
never reused and never reordered. Chapters 11, 22 and 23 lived at the root of
`docs/` until 2026-08-07; 22 and 23 kept their titles but changed number
because 12 and 19 were already taken.

## Implementation-aware docs

These files describe what the code does today, the recommended slice sequence, and the validation snapshot. They **point to the bible for design questions** instead of restating them.

| File | Owns |
| --- | --- |
| `CURRENT_STATUS.md` | What slice is live, what the next proof should be, verification commands. |
| `session-state/` | **Generated, not written.** The measured baseline and a dated screenshot of the city at the start of each session. When it disagrees with the prose above, it wins. |
| `ARCHITECTURE.md` | Folder layout, the engine/domain boundary, three visual scales, persistence boundary, what is out of scope. |
| `ARCHITECTURE_HARDENING_REPORT.md` | **Dated final state of A0–A12, not a contract.** The assembly graph after Persistence and Application left the Godot project, who owns `CityWorld`, the stable save IDs, and the structural guards that keep the boundary compiler-enforced. `ARCHITECTURE.md` remains the authority; this records how the seams were closed. |
| `VISUAL_REGRESSION.md` | Reproducible capture harness, required UI-state matrix, and human sign-off contract. |
| `UI_PATTERNS.md` | North-star rules for reusable UI: PackedScene / `[GlobalClass]` / static factory, naming, state binding via signals, theming hierarchy, save/load integration, navigation, per-PR audit checklist. **Read this before authoring any new screen or widget.** |
| `UI_AUDIT.md` | Manual checklist + history of signature for the current UI state after each stabilisation slice. |
| `HUD_REVIEW_2026-08-10.md` | **Dated findings report, not a contract.** Static review of the in-game HUD: what to keep, nine ranked problems (P1–P9), low-risk improvements and three radical options. Owns no backlog — `TO_DO.md` remains the actionable queue. Its §1.8 and P8 carry corrections from the `AccordionHost` restructure landed the same day. |
| `VALIDATION.md` | Honest cross-check of the current slice against the bible's vision and pillars, ranked gaps. |
| `PERFORMANCE_BUDGETS.md` | Frame-time targets per scenario, how they are measured, and what the capture harness does when one is exceeded. |
| `EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` | **Approved direction.** The three resource horizons, the founding camp, and the first expeditions. EG-A0 numbers remain provisional. Cited by several `CONTEXT_MAP.md` routes. |
| `EXPEDITIONS_AND_COMBAT_INTEGRATION_ROADMAP.md` | **Sequencing, not design.** Dependency order for landing automatic combat and expeditions. The design it sequences lives in bible chapters 05, 11 and 22. |
| `CITIZEN_OFFLINE_ROUTINE_AUDIT.md` | **Historical record.** Stabilization audit of semantic persistence, work routines, and visual reconstruction, 2026-07-29. |
| `technical/sprite-generator-feasibility.md` | **Technical report.** Feasibility of a modular equipment compositor, separating verified facts from inferences and recommendations. Not an approved plan. |
| `ART_PIPELINE.md` | Pixelorama → PNG → Godot file flow, naming conventions, SpriteFrames / TileMapLayer wiring, anti-patterns. |
| `ASSET_INVENTORY.md` | Inventory and licensing triage of downloaded art packs, selected promotions into runtime assets, and the staged integration plan. |
| `LINEAGE_DESIGN_MATRIX.md` | Visual translation matrix shipped with the Universal LPC lineage character pack. |
| `MANIFEST.json` | Machine-readable manifest for the 16 imported lineage character scenes. |
| `LICENSING_AND_ATTRIBUTION.md` | Universal LPC provenance, transformations, distribution obligations, and attribution pointers. |
| `licenses/` | Full and selected LPC credits plus the generator's GPL-3.0 license text. |
| `PRODUCT_DIRECTION.md` | **Process guide.** Core loop, validation sequence, UI/UX rules, drift checklist, alignment questions. Points to the bible for design answers. |
| `REPOSITORY_CONVENTIONS.md` | **Process guide.** The full prose behind the rules routed from `AGENTS.md` / `CLAUDE.md`. If it contradicts the root file, the root file wins. |
| `ai/` | **Routing layer.** `CONTEXT_MAP.md` (which agent and which skills per request), `CROSS_DOMAIN_INVARIANTS.md`, `DECISION_LOG.md`, `AGENT_COLLABORATION_PROTOCOL.md`, `CURRENT_DEVELOPMENT_STATE.md`, `FEATURE_HANDOFF_TEMPLATE.md`. It routes work; it is not a design source. |
| `GAME_VISION.md` | **Pointer.** The vision, principles, and pillars live in the bible; this file maps old sections to their new homes. |
| `LINEAGES_AND_PROFESSIONAL_AFFINITIES.md` | **Pointer.** The eight lineages, twelve-family vocabulary, five layers, and balance rules live in the bible; this file maps old sections to their new homes. |
| `DESIGN_INFLUENCES.md` | **Pointer + audit trail.** The IP boundary and naming discipline live in the bible; the retired MVP shorthand stays here as a historical note. |
| `_archive/design-bible-10-prototype-roadmap-2026-08-07.md` | **Historical plan.** The scene map and fifteen-step order extracted from bible chapter 10 when it was split. Superseded by the proposal's EG sequence; kept to compare what was planned against what was built. Do not edit. |
| `_archive/ravatha-source-2026-08-04/` | **Historical source.** Original Ravatha lore package, RAVATHA_LINEAGE_SYSTEM guidelines and Kovari Cube onboarding doc, archived after being consolidated into the bible. Do not edit; consult only as a reference for the consolidation. |

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
7. **This index is complete or it is broken.** Every document under
   `docs/` appears in one of the tables above, `_archive/` included. A
   document that exists but is not indexed is invisible to the next
   session, which is how the four numbered orphans survived until
   2026-08-07. `pwsh ./scripts/docs/classify.ps1` fails when a live
   document is missing from this file.
8. **New UI flows through `UI_PATTERNS.md`.** Before opening a screen
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
| "What are the real build/test/schema numbers right now?" | `session-state/STATE.txt` |
| "What did the city actually look like last session?" | `session-state/<date>-macro-1280x720.png` |
| "How did we get here?" | `../CHANGELOG.md` |
| "How is the code organised today?" | `ARCHITECTURE.md` |
| "How should I build a new screen or widget?" | `UI_PATTERNS.md` |
| "What does the current UI look like in practice?" | `UI_AUDIT.md` |
| "How do I validate a new slice?" | `PRODUCT_DIRECTION.md` |
| "What must the slice still prove?" | `VALIDATION.md` |
| "Where do files live and how are they named?" | `ART_PIPELINE.md` |
