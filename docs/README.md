# Documentation

> Everything under `docs/` explains **what exists**: what each system is, why it
> exists, what rules bind it, and how the pieces relate. Nothing here is a
> queue.

## The four places truth lives

| Question | Where it is answered |
| --- | --- |
| What is still to be done? | **GitHub Issues** — `gh issue list`. Never a document. |
| How is this actually implemented? | **The code and its tests.** |
| What is this system, and what may it never do? | **`docs/`** — this tree. |
| How did we get here? | **`CHANGELOG.md`** and `git log`; the reasoning behind a counterintuitive decision is in [`history/decisions.md`](history/decisions.md). |

A canonical document never carries "pending", "next steps", "phases of
implementation" or a status table. `scripts/docs/classify.ps1` fails when one
appears.

## Systems — [`systems/`](systems/)

What each game system is, what problem it solves for the player, and its
invariants.

| Document | Owns |
| --- | --- |
| [`systems/citizens.md`](systems/citizens.md) | The single `Citizen` entity, identity composition, hero states, the five layers of competence, the twelve professional families, data and balance rules. |
| [`systems/kovari-cube.md`](systems/kovari-cube.md) | The three continuous pairs that describe a person, how a lineage vertex and the onboarding produce them, the derived physical expression and the natural weapon families. |
| [`systems/statistics-and-combat.md`](systems/statistics-and-combat.md) | The authorised sources for every derived statistic, the four families of stats, the curves, caps and reference citizens. |
| [`systems/elemental-affinities.md`](systems/elemental-affinities.md) | Earth, Water, Fire, Air, Aether and Silence: what each means, how it manifests, its risks, and the one universal elemental channel. |
| [`systems/city-and-territory.md`](systems/city-and-territory.md) | Macro view, building-detail scenes, parcel states, expansion, the axes of city growth, architecture by culture. |
| [`systems/frontage-and-corridors.md`](systems/frontage-and-corridors.md) | Dynamic frontage rows, construction reservations, structural footprints, corridors, lateral expansion and their persistence contract. |
| [`systems/expeditions.md`](systems/expeditions.md) | The outbound → encounter → objective → return commitment, the shared depth-band grammar, automatic combat, the first Spirit Trail. |
| [`systems/onboarding-and-founder.md`](systems/onboarding-and-founder.md) | The twelve scored fragments, the founder card, the materialised weapon, the astral arrival canon and the prologue's restrictions. |
| [`systems/first-night.md`](systems/first-night.md) | The playable period between the astral onboarding and the first dawn: the fire spirit, the founding modules, the transition to expeditions. |

## World — [`world/`](world/)

| Document | Owns |
| --- | --- |
| [`world/vision-and-pillars.md`](world/vision-and-pillars.md) | The fantasy, the single-city rule, the absence rule, the 14 design principles, the nine gameplay pillars, the originality/IP boundary and the cross-cutting guardrails. |
| [`world/lineages.md`](world/lineages.md) | The eight lineages: idea, nature, culture, affinities, tensions, architectural identity, and the visual translation matrix. |
| [`world/lineages/`](world/lineages/) | One document per lineage — culture, its own cultural system, and its Cube vertex. |

## Presentation — [`presentation/`](presentation/)

| Document | Owns |
| --- | --- |
| [`presentation/visual-language.md`](presentation/visual-language.md) | Pixel-art direction, the three visual scales, depth models, camera modes, typography hierarchy, per-lineage visual identity. |
| [`presentation/ui-patterns.md`](presentation/ui-patterns.md) | The three reusable UI patterns, naming, signal-based state binding, the theme hierarchy, the compact HUD profile, focus and input. **Read before authoring any screen or widget.** |
| [`presentation/audio.md`](presentation/audio.md) | The sonic identity: retro synthesis language, sound layers, per-lineage character, comfort and variation, bus layout, licensing and pipeline. |
| [`presentation/art-pipeline.md`](presentation/art-pipeline.md) | Pixelorama → PNG → Godot file flow, naming, `SpriteFrames`/`TileMapLayer` wiring, anti-patterns. |
| [`presentation/asset-inventory.md`](presentation/asset-inventory.md) | The downloaded art packs, their licensing triage and which files were promoted into runtime assets. |
| [`presentation/licensing-and-attribution.md`](presentation/licensing-and-attribution.md) | Universal LPC provenance, transformations and distribution obligations. |
| [`presentation/licenses/`](presentation/licenses/) | Full and selected LPC credits plus the generator's GPL-3.0 text. |
| `presentation/MANIFEST.json` | Machine-readable manifest for the 16 imported lineage character scenes. |

## Engineering — [`engineering/`](engineering/)

| Document | Owns |
| --- | --- |
| [`engineering/architecture.md`](engineering/architecture.md) | The four layers and how each boundary is enforced, the Godot/.NET seam, the three visual scales in code, the single world clock, persistence, the shared spatial grammar, what is out of scope. |
| [`engineering/state-authority.md`](engineering/state-authority.md) | Who owns each mutable truth: the five categories, the per-concept registry of owner / persistence / writers / invariants, and how each is reconstructed from a save. |
| [`engineering/conventions.md`](engineering/conventions.md) | Repository layout, C# and Godot naming, commit and asset conventions. |
| [`engineering/design-review.md`](engineering/design-review.md) | The north star, the loop every feature serves, the UI/UX constraint, the drift warnings and the review checklist. |
| [`engineering/visual-regression.md`](engineering/visual-regression.md) | The capture harness, the required fixture matrix, the human sign-off checklist and the dated review record. |
| [`engineering/performance.md`](engineering/performance.md) | Frame-time targets per scenario, how they are measured, what the harness does when one is exceeded. |

## Routing, history and measurement

| Path | Owns |
| --- | --- |
| [`ai/`](ai/) | **Routing layer.** Which agent and which skills handle a request (`CONTEXT_MAP.md`), the hard cross-domain constraints, the collaboration protocol, the documentation-impact gate. It routes work; it is not a design source. |
| [`history/decisions.md`](history/decisions.md) | Decision records — kept only where a current rule is counterintuitive and the reason is not visible in the code. |
| [`session-state/`](session-state/) | **Generated, never hand-written.** The measured baseline and a dated frame of the city. When it disagrees with prose, it wins — and the prose loses the number rather than being corrected. |

The optional lineage splash workflow is documented beside the tool at
[`art/world-of-goses-minimax-splash-generator/README.md`](../art/world-of-goses-minimax-splash-generator/README.md).
Its output is local concept material, not a runtime dependency.

## Authority rules

1. The most recent explicit decision wins.
2. The product vision wins over a temporary prototype.
3. The domain wins over its visual representation.
4. The player experience wins over a thorough simulation that adds no
   gameplay value.
5. A mechanic is not implemented only because it is technically possible.
6. **Each fact lives in exactly one document.** If two documents would explain
   the same rule, one of them links to the other.
7. **This index is complete or it is broken.** Every document under `docs/`
   appears above; `pwsh ./scripts/docs/classify.ps1` fails when one does not.
8. **No document holds actionable future work.** That belongs to GitHub Issues.
   The same script fails on a backlog heading in a canonical document.
9. **No document restates a measured number.** Build, test, schema and
   catalogue counts belong to `session-state/STATE.txt` only.
