# Current Project Status

> Practical handoff for the next development session. Read this after
> the design bible (`docs/world-of-goses-design-bible/`) and
> `PRODUCT_DIRECTION.md` to understand the implemented founding-hero
> slice and the next decision.

## 0. Document map

This file lives in the implementation-aware doc set under `docs/`.
The companion conceptual design bible lives at
[`docs/world-of-goses-design-bible/`](world-of-goses-design-bible/README.md).
A consolidated index of both sets is in [`docs/README.md`](README.md).

The bible is the source of truth for *what the game is*; this file
is the source of truth for *what the code does today*. When the two
disagree on a design question, the bible wins; when they disagree on
what ships next, this file wins.

---

## 1. Last verified baseline

- Godot `.NET` 4.7.1, C# on `.NET 8.0`.
- `dotnet build` succeeds with 0 errors and 0 warnings.
- xUnit suite: **274 / 274 passing**.
- Godot headless loads the main scene and current primary slot without scene,
  resource, signal, or C# errors.
- The current slice combines founding-hero onboarding, interactive construction
  for Basic Shelter, Farm, and Quarry, a causal event log, and 16 animated LPC
  lineage character variants.

## 2. Founding-hero slice

A fresh `CityWorld` contains no citizens and no buildings. The player completes a five-step onboarding flow that chooses:

- Name and one of eight working lineages.
- Three personal aptitudes.
- Three professional families from the twelve-family vocabulary.
- One elemental affinity.
- One combat style and one or two weapon preferences.
- Three personality traits.
- One political orientation and one spiritual posture.

Completing the flow creates exactly one `Citizen` with the `Hero` role, full stamina, no assignment, and `AtHome` location. The hero profile is visible in a responsive read-only profile screen. The macro view shows one real citizen marker and a clear `No buildings yet` state; it does not create a Home, Quarry, Farm, or any other building implicitly.

The eight lineage definitions are canonical in the design bible at
[`world-of-goses-design-bible/06_LINEAGES.md`](world-of-goses-design-bible/06_LINEAGES.md).
In this slice they are qualitative identity metadata. They do not block
professions, establish permanent ceilings, or grant automatic production
bonuses. Practical experience and future education/skill systems must
outweigh birth over time.

## 3. First authorised construction

After onboarding, the construction menu offers Basic Shelter, Farm, and Quarry
as explicit player decisions. Selecting an option enters placement mode instead
of activating an existing building behind the menu. Confirming a valid plot
creates a persisted `ConstructionProject`; contributors can be assigned or
removed, work can be paused and resumed, and deterministic ticks advance the
project through visible phases subject to day/night and stamina. Completion
replaces the project with the resulting building without seeding it at world
creation.

The world records construction and simulation outcomes in a bounded causal
`WorldEventLog`. The HUD event-log panel reacts to controller signals rather
than polling domain state. Offline progression still retains its aggregate
summary for catch-up reporting; it is not a replacement for the event stream.

## 4. Existing city systems retained as concepts

The domain still contains buildings, production policies, assignments, stamina,
food, upkeep, day/night mobilisation, and offline progression. They are no
longer instantiated by the new-game path. The Basic Shelter now proves the
empty-to-built transition while Quarry and Farm remain explicit test fixtures.

The old pre-seeded Quarry/Farm/Home scenarios remain available only as explicit
test fixtures in `TestHelpers`; they are not the game's current startup data.

## 5. Stamina, day/night, and idle worlds

Assigned workers continue to pay stamina on producing buildings, eat food when
available, and regenerate through the existing WellFed rules. A hero-only world
advances its clock and decays buffs during live and offline time without trying
to produce or consume building resources. The offline path uses an idle
fast-forward for an empty building collection rather than iterating thousands
of no-op building ticks.

## 6. Persistence

- Schema version is now **2**.
- A v2 citizen save includes a complete `CitizenProfileSave` plus competencies,
  roles, assignment, stamina, and WellFed state.
- A playable v2 snapshot must contain exactly one hero citizen; zero buildings
  is valid.
- A v1 slot containing the retired five-citizen / three-building prototype is
  rejected as incompatible and left untouched during onboarding. After a
  successful hero creation, the normal atomic write replaces it and preserves
  the previous file as `.bak`.
- Partial onboarding is not saved in this slice. Closing before confirmation
  starts the flow again without destroying the old slot.
- Structural and cross-entity validation runs before restore.

## 7. Presentation, themes, and navigation

`CityWorldController` emits `HeroCreated`, `WorldTickAdvanced`, project-change,
event-log, selection, and building signals. `OnboardingView` and
`HeroProfileView` are reusable Control scenes. They use containers, scrolling,
explicit focusable controls, and a single back path so the flow works with
mouse, keyboard, and gamepad.

The global theme preserves the Geist Pixel / Jersey 10 / Pixelify Sans hierarchy.
The eight founder lineages resolve exported panel `StyleBoxTexture` resources at
runtime through `LineageThemeRegistry`; missing components fall back to the same
lineage panel and then the project default. `LineageShowcase.tscn` exercises all
eight packs and the expected component fallbacks. The reference viewport is
1280×720 with responsive Control containers.

## 8. Known limitations

- Lineage and profile choices are stored and presented but do not yet modify
  learning, retention, errors, fatigue, teaching, or production. Those effects
  require the future skill-system slice.
- Basic Shelter, Farm, and Quarry are the current placement options, but all
  three recipes keep intentionally minimal prerequisites; materials, knowledge,
  institutions, and richer unlock conditions remain future work.
- The causal event log covers the current prototype actions; it is not yet the
  complete long-horizon event model described by the design bible.
- Combat, expeditions, health, relationships, institutions, migration, and
  environmental alignment remain future systems.
- Building art remains provisional. Detailed citizens now use the imported LPC
  set, while the macro citizen marker and some UI elements remain placeholders.
- There is no automated Godot UI test harness; headless boot and manual flow
  verification remain required.

## 9. Recommended next slice

Deepen the first-building proof with conditions-as-data (materials, knowledge,
institutions, and authorisation) without turning lineage into a production bonus
or profession lock. Preserve live/offline parity for construction progress.

## 10. Verification commands

From `C:\dev\world-of-goses`:

```powershell
cd game
dotnet build

cd ../tests/WorldofGoses.Tests
dotnet test

C:\Tools\Godot\Godot_v4.7.1-stable_mono_win64_console.exe --headless --path ..\..\game --quit-after 3
```

There is no linter or CI configured yet. Do not install global tools.

## 11. Design record

The eight lineages and the professional-affinity contract are owned
by the design bible at
[`docs/world-of-goses-design-bible/06_LINEAGES.md`](world-of-goses-design-bible/06_LINEAGES.md)
and
[`docs/world-of-goses-design-bible/04_CITIZENS_PROFESSIONS_AND_HEROES.md`](world-of-goses-design-bible/04_CITIZENS_PROFESSIONS_AND_HEROES.md).
`docs/LINEAGES_AND_PROFESSIONAL_AFFINITIES.md` and
`docs/DESIGN_INFLUENCES.md` are pointer files now; the design content
moved to the bible. The IP-boundary rules remain documented in the
bible at
[`docs/world-of-goses-design-bible/01_GAME_VISION.md`](world-of-goses-design-bible/01_GAME_VISION.md)
§ *Frontera de inspiración e IP*.

The next session should begin by reading the bible before adding any
lineage mechanic or building seed.

## 12. Verification history

The previous Quarry/Farm/Home slice was verified before this reset. Its
production, stamina, mobilisation, and persistence behaviours are still covered
by explicit test scenarios, not by production startup data.

The current baseline was verified with:

- `dotnet build game/World of Goses.csproj` — 0 warnings, 0 errors.
- `dotnet test tests/WorldofGoses.Tests/WorldofGoses.Tests.csproj --no-restore` — 274 passing.
- Godot 4.7.1 `.NET` headless boot — current slot loaded cleanly.

The manual onboarding flow must still be exercised in a graphical Godot run.

## 13. Open product questions

- Which richer conditions should gate or reshape the Basic Shelter project?
- Which skill formulas turn qualitative affinities into small, causal early
  learning effects?
- How should education, mentorship, history, health, and institutions change
  the weight of lineage over time?
- Which original public-facing names replace provisional design terms after
  originality review?

These are open design questions, not permission to reintroduce a starter seed.

## 14. File map

- Domain: `game/scripts/Domain/`
- Persistence: `game/scripts/Domain/Persistence/`
- Onboarding: `game/scripts/OnboardingView.cs`, `game/scenes/OnboardingView.tscn`
- Hero profile: `game/scripts/HeroProfileView.cs`, `game/scenes/HeroProfileView.tscn`
- Construction: `game/scripts/ConstructionPanel.cs`, domain construction types
- Event log: `game/scripts/Domain/WorldEventLog.cs`, `game/scripts/OfflineReportPanel.cs`
- Lineage themes: `game/scripts/LineageThemeRegistry.cs`, `game/assets/ui/lineages/`
- Lineage characters: `game/assets/characters/lineages/`, `game/scripts/visual/`
- Main scene: `game/scenes/CityPrototype.tscn`
- Tests: `tests/WorldofGoses.Tests/`
- Canonical lineage design: [`docs/world-of-goses-design-bible/06_LINEAGES.md`](world-of-goses-design-bible/06_LINEAGES.md)
- Building art catalog: `game/scripts/BuildingArt.cs` — single source of truth that maps every `BuildingKind` to its `res://` texture path and canvas size.

## 15. First MVP pixel art (slice 7 — landed)

Three placeholder PNGs now anchor the macro city view at the agreed canvas sizes and replace the previous generic `building_placeholder.png`:

| Subject | PNG (in `art/exports/buildings/` and `game/assets/buildings/`) | Canvas    | `BuildingKind`            |
| ------- | -------------------------------------------------------------- | --------- | ------------------------- |
| Home    | `home_idle.png`                                                | 64 × 64   | `Home` (Basic Shelter)    |
| Quarry  | `quarry_idle.png`                                              | 128 × 128 | `Quarry`                  |
| Farm    | `farm_idle.png`                                                | 128 × 128 | `Farm`                    |

The catalog lives at `game/scripts/BuildingArt.cs`. `BuildingPlot` defaults to the quarry texture; scenes can override the path via the inspector or by calling `BuildingArt.GetTexturePath(kind)`. The three PNGs currently have **no Pixelorama source** — `art/source/buildings/README.md` documents what `.pxo` files must replace them with, at the same canvas sizes so layout code does not need to re-anchor.

`Smithy` and `PotionLab` still have no art at any level. `BuildingArt.GetTexturePath` returns `null` for them; rendering code must handle the missing case rather than crash.

## 16. Detailed citizen sprites

The previous `worker_placeholder.png` was removed. Building-detail worker slots
now resolve one of 16 lineage/gender scenes through
`CharacterVisualRegistry`. Each scene exposes `idle`, `walk`, and `slash` in
four directions using 128 × 128 cells. `LineageSpritePlayer` owns animation
selection; the building-detail slot explicitly selects `idle_down` and does not
apply a second looping locomotion animation to the container.

Universal LPC attribution and redistribution requirements are recorded in
`docs/LICENSING_AND_ATTRIBUTION.md` and `docs/licenses/`.

## 17. Visual and audio lineage identity

The eight lineages now have a documented **visual identity** (per-lineage architectural silhouettes, materials, and UI tokens) and a documented **audio identity** (per-lineage timbral family and rhythmic character):

- Visual: [`docs/world-of-goses-design-bible/06_LINEAGES.md`](world-of-goses-design-bible/06_LINEAGES.md) § *Architecture* per lineage; condensed in [`08_VISUAL_UI_AND_ASSET_GUIDELINES.md`](world-of-goses-design-bible/08_VISUAL_UI_AND_ASSET_GUIDELINES.md) § *Identidad resumida*.
- Audio: [`docs/world-of-goses-design-bible/09_AUDIO_GUIDELINES.md`](world-of-goses-design-bible/09_AUDIO_GUIDELINES.md) § *Identidad por linaje*.

These identities are not yet encoded in the project (the three placeholder PNGs are culture-neutral); they are documented so the next character and building art slices know what each lineage should look and sound like.

## 18. Outstanding open questions

The design bible maintains an explicit list of decisions still pending. They are not gaps in the slice — they are gaps in the game:

See [`docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md`](world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md) § *Preguntas abiertas* for the canonical list (cosmology, environmental axis name, time scale, combat elements, weapon families, ageing, migration, cultural mixing, politics, economy, population capacity, music, first biome, first systemic conflict).

The local ranking of those questions by immediate leverage lives in [`VALIDATION.md`](VALIDATION.md) § *Outstanding gaps*.

Keep documentation and code aligned as construction prerequisites deepen.
