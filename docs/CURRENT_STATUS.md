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
- xUnit suite: **345 / 345 passing**.
- Godot headless loads the main scene and current primary slot without scene,
  resource, signal, or C# errors.
- `tools/Capture-VisualMatrix.ps1` produces dimension-checked window-client
  captures at 1024×576, 1280×720, and 1600×900. The first `macro-current`
  review passed viewport containment and exposed M-16, a citizen label/icon overlap.
- The current slice combines founding-hero onboarding with gender selection,
  the hero sprite walking on the empty field, interactive construction for
  Basic Shelter / Farm / Quarry, data-driven recipes with min/max stock
  policy, causal event logging with `CauseEventId` chains, wood gathering
  from founding Forests, 16 animated LPC lineage character variants, and the
  completed presentation-boundary/UI interaction stabilization slice.

## 2. Founding-hero slice (with gender identity)

A fresh `CityWorld` contains no citizens and no buildings. The player completes a six-step onboarding flow that chooses:

- Name, one of eight working lineages, and one of two body variants (Feminine / Masculine).
- Three personal aptitudes.
- Three professional families from the twelve-family vocabulary.
- One elemental affinity.
- One combat style and one or two weapon preferences.
- Three personality traits.
- One political orientation and one spiritual posture.

The body variant replaces the previous `appearanceSeed & 1` derivation so the
player picks the sprite explicitly; the seed still encodes visual variety
inside a variant. Completing the flow creates exactly one `Citizen` with the
`Hero` role, full stamina, no assignment, and `AtHome` location, and seeds two
Forests (id 100, 101) with `WoodReserve = 8` each. The hero profile is visible
in a responsive read-only profile screen showing the imported LPC sprite in its
idle animation; the macro view shows the hero sprite walking side-to-side in
the centre of the field while no buildings exist and the macro mode is
`MacroMode.Empty`.

The eight lineage definitions are canonical in the design bible at
[`world-of-goses-design-bible/06_LINEAGES.md`](world-of-goses-design-bible/06_LINEAGES.md).
In this slice they are qualitative identity metadata. They do not block
professions, establish permanent ceilings, or grant automatic production bonuses.
Practical experience and future education/skill systems must
outweigh birth over time.

## 3. First authorised construction

After onboarding, the construction menu is reachable directly from
`MacroMode.Empty` via a "Build shelter" button on the macro view. Selecting
Basic Shelter, Farm, or Quarry enters placement mode instead of activating an
existing building behind the menu. Confirming a valid plot creates a persisted
`ConstructionProject`; contributors can be assigned or removed, work can be
paused and resumed, and deterministic ticks advance the project through
visible phases subject to day/night, stamina, and recipe drawdown.
Completion replaces the project with the resulting building without seeding
it at world creation.

### Recipe gate

Every productive construction costs materials:

| Kind | Recipe inputs (total) | Deposit (25 % rounded up) |
| --- | --- | --- |
| Basic Shelter | 4 wood | 1 wood |
| Farm | 6 wood | 2 wood |
| Quarry | 8 wood + 4 food | 2 wood + 1 food |

Authorisation consumes the deposit atomically with full rollback on partial
failure (`MissingMaterials`). The remainder drains 1 unit per work interval;
a shortfall mid-life emits `WorldEventKind.ProductionBlocked` and stops
progress until the city gathers more. The construction menu's "Cancel" keeps
already consumed inputs spent and discards the record of inputs that had not
yet been debited.

### Gathering wood

The two founding Forests are not productive buildings. Each holds a
`WoodReserve`. Forest art is still missing, so the construction panel exposes
the temporary but explicit action "Send <hero> to gather 2 wood"; the forest
detail panel retains the same action for when a plot becomes visible. Drained wood
lands on the Forest's `Stock`, which the recipe gate then consumes. The
`CityStatusPanel` chip reads `Wood: X gathered · Y in forests` so the player
sees both the spending pool and the remaining source pool.

Before authorisation, the construction panel lists each option's total material
need and current availability. Buttons are disabled when the deposit cannot be
paid, and rejected authorisations render an explicit reason instead of silently
discarding `ConstructionAuthorizationResult`.

## 4. Production policy: min / max stock + priority

`Building.ConfigureProductionPolicy(bool enabled, int minStock, int maxStock, int priority)`
replaces the v2 single-target policy. The reactive loop produces until
`Stock < MaxStock`, stops, and resumes automatically once stock drops to or
below `MinStock`. `Priority` is a sort hint stored for the future
auto-assignment slice; the domain does not act on it today (the player remains
sovereign via `AssignmentPanel`). `MinStock == MaxStock` is the "fixed stockpile"
pattern and is allowed.

## 5. Causal event log

`WorldEvent.CauseEventId` is now wired. `StockProduced` references the previous
`StockProduced` for the same building (or the day's `DayBegan` for the first
one of the day). `ProductionBlocked` references the most recent matching event
so the offline report can surface causal chains. The `OfflineReportPanel`
groups `ProductionBlocked` events by subject and renders a compact
"Decisions needed" list above the chronological rows.

Subjects use typed identity (`World`, `Building`, `ConstructionProject`,
`Citizen`, or future `Expedition`) plus an optional entity ID; their captured
name is display data, not identity. Player-facing summaries are formatted in
presentation rather than stored in the domain event.

The player-facing Chronicle compacts consecutive equivalent production events
into one accumulated row, suppresses repeated steady-state notices, and formats
timestamps as simulated `Day N · HH:mm` values. Its count reflects rendered
entries rather than raw repetitions. Collapsed mode shows only the latest entry;
expanding restores the bounded scrollable history without changing simulation
state.

## 6. Existing city systems retained as concepts

The domain still contains buildings, production policies, assignments, stamina,
food, upkeep, day/night mobilisation, and offline progression. They are no
longer instantiated by the new-game path. The Basic Shelter now proves the
empty-to-built transition while Quarry and Farm remain explicit test fixtures.

The old pre-seeded Quarry/Farm/Home scenarios remain available only as explicit
test fixtures in `TestHelpers`; they are not the game's current startup data.

## 7. Stamina, day/night, and idle worlds

Assigned workers continue to pay stamina on producing buildings, eat food when
available, and regenerate through the existing WellFed rules. A hero-only world
advances its clock and decays buffs during live and offline time without trying
to produce or consume building resources. The offline path uses an idle
fast-forward for an empty building collection rather than iterating thousands
of no-op building ticks.

`WorldTimeAdvance` also batches structured cities with no work assignments
within each uninterrupted day/night phase. It steps only temporal boundaries
so mobilisation and causal events remain canonical. Worlds with assigned work
still use `AdvanceWorldTick` for every tick until their production/construction
simulators are extracted and can prove the same snapshot/event equivalence.

Assignment consistency has moved behind the internal
`CitizenAssignmentService`. `CityWorld` remains the public facade, so existing
controller, persistence, UI, and test call sites are unchanged. Productive
building ticks now run through `BuildingProductionSimulation`; resource and
event ownership remain in the aggregate through narrow delegates.
`ConstructionSimulation` now owns project work/rest ticks and transactional
drawdown, completing H-21. Authorisation and project completion remain aggregate
operations because they create/remove world entities.

## 8. Persistence

- Schema version is now **7**.
- A v4 citizen save includes a complete `CitizenProfileSave` plus
  `Gender`, competencies, roles, assignment, stamina, and WellFed state. A v4
  building save includes the reactive policy triplet `MinStock`, `MaxStock`,
  and `Priority`. A v4 project save includes `DepositedInputs` and
  `RemainingInputs` so an interrupted project resumes deterministically.
- A v5+ snapshot also stores at most 128 significant causal events. Incremental
  production/progress and day/night cycles are excluded; repeated steady states
  are compacted and dangling causes are removed before serialization.
- Schema v6 persists `IronStock` and resource reservations with typed owners.
  Validation rejects duplicate IDs, missing project owners, invalid resource
  kinds, and commitments above physical stock.
- Schema v7 persists stable Forest wood-unit reserves and each citizen's last
  visited resource as `buildingId + unitId + logicalSlot`; no Godot coordinates
  enter the domain snapshot. The logical slot keeps the macro citizen in place
  after the depleted Forest entity is removed.
- Schema v8 persists minimal unlocked/locked parcels and natural-resource
  patches independently from construction entities. The current Forest
  building remains only as a compatibility storage adapter for gathered Wood.
- Schema v9 persists non-overlapping parcel lots, spans, orientation, and
  footprint profiles for projects and buildings. Project completion retains
  the same placement identity.
- Macro buildings and projects are positioned from their persisted parcel/lot
  instead of an insertion-order horizontal row. The current plot widget is
  rendered at 0.5 macro scale while logical footprints remain authoritative.
- The controller walks every migration from v2 through v9 on raw JSON before
  `Validate`, so older saves upgrade non-fatally.
- A playable v9 snapshot must contain exactly one hero citizen; zero
  buildings is valid. v3 saves default missing `Gender` to Masculine so the
  hero's body variant stays stable across the bump.
- After a successful hero creation, the normal atomic write replaces the slot
  and preserves the previous file as `.bak`.
- Partial onboarding is not saved in this slice. Closing before confirmation
  starts the flow again without destroying the old slot.
- Structural and cross-entity validation runs before restore.

`CityResourceLedger` now centralizes totals, deposits, atomic recipe drawdown,
and location-aware runtime reservations over the existing building stores.
Reservations can be released, committed, or transferred between a construction
project and future expedition owner. Schema v7 restores reservations and their
ID sequence, so committed supplies survive close and offline catch-up.

## 9. Presentation, themes, and navigation

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

The UI stabilization slice is closed. Shared navigation and assignment actions
use canonical components/factories with visible text, consistent metrics,
contrast-safe state colours, hover/focus feedback, and safe-area offsets.
Building plots derive interaction bounds from the visible subject rather than a
legacy container: Forest keeps its territorial footprint, while Shelter, Farm,
Quarry, construction stages, and citizen labels align to their rendered art.
Clicking an in-progress construction opens its progress panel, and assignment
changes update both detail and macro citizen representations immediately.

### Tooltips

Native Godot popup with a project-wide base `Label/font = Pixelify Sans`
set in `default_theme.tres`. Every tooltip (engine default or
`ThemeTypeVariation = "TooltipText"`/`"BodyText"` override) uses the
Pixelify family. No custom overlay — the user perceived previous attempts
as stretched boxes, so we leave the engine popup unmodified and rely on
the theme to do the typography work.

### Reusable button factory

`Ui/StandardButtons.cs` centralises the buttons that more than one screen
reaches for (`BackToCityButton`, `ViewHeroButton`). Every consumer hits
the same factory so the icon, label, theme variation, and tooltip are
identical — previously `HeroProfileView._backButton` was a plain `Button`
while `BuildingDetailView.BackButton` was an `IconButton` with an arrow
glyph and the `HeroAccessButton` macro shortcut shipped without its user
icon. The factory eliminates that divergence.

### Modal + close UX

`Ui/ModalHost.cs` owns the scrim, the centre container, the
`ui_cancel` (ESC) handler, and scrim-click dismissal. `Ui/PanelHeader.cs`
gives any panel a Jersey 10 title plus an `IconButton` close (X) bound
to the host's `Closed` signal. Construction modal closes via X,
`ui_cancel` (ESC), or click on the scrim — three independent routes so
the player is never stuck if the construction state blocks an option.

### Forest gathering (organic)

Forests are productive buildings like Farms and Quarries:
`SeedStartingForests` configures them with `workerCapacity: 2`,
`visualCapacity: 2`, `baseProductionPerWorker: 1`. Assigning workers
moves wood from `Building.WoodReserve` to `Building.Stock` each tick
(1 wood per worker, capped by the remaining reserve). When the reserve
reaches 0, `DemolishDepletedForests` removes the building from the
world, the plot disappears from the macro stage, and a
`WorldEventKind.ForestDemolished` event is recorded for the log. The
detail panel shows `Wood: X / Y (reserve R)` so the player can see the
limit before the Forest vanishes.

### Production panel simplification

`ProductionPanel` is a single production-toggle, single-rate-line view:
title, stock with reserve (Forest only), rate line, input due line,
stop-cause line, on/off `IconButton` (play/pause glyph). The reactive
`MinStock` / `MaxStock` / `Priority` triplet exists in the domain
(`Building.ConfigureProductionPolicy`) but is not surfaced in the
detail panel; `CityWorld.SetProductionEnabled` flips just the bool,
keeping the triplet intact for future slices that re-expose it.

## 10. Known limitations

- Lineage and profile choices are stored and presented but do not yet modify
  learning, retention, errors, fatigue, teaching, or production. Those effects
  require the future skill-system slice.
- The playable bootstrap is now `Forest → Shelter/Farm → Food → Quarry → Stone`.
  Farm and Quarry have no material operating recipe yet; labour, stamina, and
  time are their current running costs. Iron remains a reserved future resource
  for a real tools/fuel chain and no longer gates early construction or operation.
  A shared inventory abstraction is still future work.
- The hero walking animation is a procedural sinusoid (3.6 s period, 220 px
  amplitude). It pauses on hover and the canonical carrier is hidden or moved
  when another view owns the hero; richer contextual posture remains future work.
- The causal event log covers the current prototype actions; it is not yet the
  complete long-horizon event model described by the design bible.
- The project has a correct 1280×720 responsive canvas and nearest canvas-texture
  filtering, but integer camera/sprite placement and per-import filtering have
  not been verified end to end; the presentation is not yet proven pixel-perfect.
- Combat, expeditions, health, relationships, institutions, migration, and
  environmental alignment remain future systems.
- Building art remains provisional. Detailed citizens now use the imported LPC
  set; Forest plots render without art (no `forest_idle.png` yet) so the
  detail view shows only the gather panel.
- The macro view now has a presentation-only orthogonal foundation: eight
  provisional parcels and integer-scaled ground tiles. Forest entities are no
  longer rendered as building cards: their current reserve projects into
  interactive trees. Hover uses the CC0 axe cursor; left/right click opens the
  resource menu; Gather moves the macro hero representation to the tree and
  gathers 2 wood on arrival. Minimal parcel locked/unlocked state and per-tree
  patch identity are now persistent; construction placement, 40-wood balance,
  regeneration, and offline resource catch-up remain pending.
- There is no automated Godot UI test harness; headless boot and manual flow
  verification remain required.

## 11. Recommended next slice

The presentation-boundary and UI interaction hardening slice is complete, and
the macro direction is now committed to an elevated orthogonal grid. The next
city-growth proof should introduce the smallest persistent parcel model:
authored initial parcels with locked/unlocked and current-use state. It should
not add free dragging during normal navigation.

After that decision, choose one bounded proof: either the skill-system hook or
a small production-chain slice. The persistent status bar should remain a
compact situational summary; deeper building/resource axes belong in an
on-demand city overview rather than expanding the HUD per building.

## 12. Verification commands

From `C:\dev\world-of-goses`:

```powershell
# 1. Build the game (Domain + presentation). Expect 0 warnings / 0 errors.
cd game
dotnet build

# 2. Run the full test suite. Expect 330 passing.
cd ../tests/WorldofGoses.Tests
dotnet test

# 3. Headless boot — confirms the current local slot and scene load.
#    The automated persistence tests, not an arbitrary local slot, prove
#    the v2 → v3 → v4 migrations.
C:\Tools\Godot\Godot_v4.7.1-stable_mono_win64_console.exe `
  --headless --path ..\..\game --quit-after 3
```

There is no linter or CI configured yet. Do not install global tools.

## 13. Design record

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

## 14. Verification history

The previous Quarry/Farm/Home slice was verified before this reset. Its
production, stamina, mobilisation, and persistence behaviours are still covered
by explicit test scenarios, not by production startup data.

The current baseline was verified with:

- `dotnet build game/World of Goses.csproj` — 0 warnings, 0 errors.
- `dotnet test tests/WorldofGoses.Tests/WorldofGoses.Tests.csproj --no-build` — 327 passing.
- Godot 4.7.1 `.NET` headless editor boot — current scene/slot loaded with no
  C# or scene errors; shutdown reported only `Scan thread aborted` while the
  editor filesystem scan was being stopped by `--quit-after`.

The manual onboarding flow must still be exercised in a graphical Godot run.

## 15. Open product questions

- Which skill formulas turn qualitative affinities into small, causal early
  learning effects?
- How should education, mentorship, history, health, and institutions change
  the weight of lineage over time?
- Which original public-facing names replace provisional design terms after
  originality review?
- When does the player's first wood-gathering click happen — automatically on
  first idle frame, or only on explicit interaction?

These are open design questions, not permission to reintroduce a starter seed.

## 16. Latest interaction stabilization

- Resource trees now receive pointer input through the full-screen center
  layout. Hover can install the resource cursor and left/right click can open
  the contextual gather menu.
- Authorizing the founding Basic Shelter automatically assigns the available
  founder. Loading an older stalled shelter also performs this repair once,
  without overriding an existing assignment.
- The construction modal keeps its header and footer fixed while its body
  scrolls. Mouse-wheel input anywhere over the panel advances that body.

## 17. File map

- Domain: `game/scripts/Domain/`
- Persistence: `game/scripts/Domain/Persistence/`
- Onboarding: `game/scripts/OnboardingView.cs`, `game/scenes/OnboardingView.tscn`
- Hero profile: `game/scripts/HeroProfileView.cs`, `game/scenes/HeroProfileView.tscn`
- Construction: `game/scripts/ConstructionPanel.cs`, domain construction types
- Forest gather: `game/scripts/ForestGatherPanel.cs`
- Event log: `game/scripts/Domain/WorldEventLog.cs`, `game/scripts/OfflineReportPanel.cs`
- Reusable UI: `game/scripts/Ui/`, `game/scenes/Components/`
- Presentation snapshots: `game/scripts/*Snapshot.cs`
- Lineage themes: `game/scripts/LineageThemeRegistry.cs`, `game/assets/ui/lineages/`
- Lineage characters: `game/assets/characters/lineages/`, `game/scripts/visual/`
- Walking hero: `game/scripts/MacroCitizenActivity.cs`
- Main scene: `game/scenes/CityPrototype.tscn`
- Tests: `tests/WorldofGoses.Tests/`
- Canonical lineage design: [`docs/world-of-goses-design-bible/06_LINEAGES.md`](world-of-goses-design-bible/06_LINEAGES.md)
- Building art catalog: `game/scripts/BuildingArt.cs` — single source of truth that maps every `BuildingKind` to its `res://` texture path and canvas size.

## 18. First MVP pixel art (slice 7 — landed)

Three placeholder PNGs now anchor the macro city view at the agreed canvas sizes and replace the previous generic `building_placeholder.png`:

| Subject | PNG (in `art/exports/buildings/` and `game/assets/buildings/`) | Canvas    | `BuildingKind`            |
| ------- | -------------------------------------------------------------- | --------- | ------------------------- |
| Home    | `home_idle.png`                                                | 64 × 64   | `Home` (Basic Shelter)    |
| Quarry  | `quarry_idle.png`                                              | 128 × 128 | `Quarry`                  |
| Farm    | `farm_idle.png`                                                | 128 × 128 | `Farm`                    |

The catalog lives at `game/scripts/BuildingArt.cs`. `BuildingPlot` defaults to the quarry texture; scenes can override the path via the inspector or by calling `BuildingArt.GetTexturePath(kind)`. The three PNGs currently have **no Pixelorama source** — `art/source/buildings/README.md` documents what `.pxo` files must replace them with, at the same canvas sizes so layout code does not need to re-anchor.

`Smithy`, `PotionLab`, and `Forest` still have no art at any level.
`BuildingArt.GetTexturePath` returns `null` for them; rendering code must
handle the missing case rather than crash.

## 19. Detailed citizen sprites

The previous `worker_placeholder.png` was removed. Building-detail worker slots
now resolve one of 16 lineage/gender scenes through
`CharacterVisualRegistry`. Each scene exposes 14 animations (`idle`,
`combat_idle`, `walk`, `run`, `jump`, `climb`, `sit`, `hurt`, `slash`,
`thrust`, `halfslash`, `backslash`, `shoot`, `spellcast`) in four
directions using 128 × 128 cells. `LineageSpritePlayer` owns animation
selection; the building-detail slot explicitly selects `idle_down` and does not
apply a second looping locomotion animation to the container.

Universal LPC attribution and redistribution requirements are recorded in
`docs/LICENSING_AND_ATTRIBUTION.md` and `docs/licenses/`.

## 20. Visual and audio lineage identity

The eight lineages now have a documented **visual identity** (per-lineage architectural silhouettes, materials, and UI tokens) and a documented **audio identity** (per-lineage timbral family and rhythmic character):

- Visual: [`docs/world-of-goses-design-bible/06_LINEAGES.md`](world-of-goses-design-bible/06_LINEAGES.md) § *Architecture* per lineage; condensed in [`08_VISUAL_UI_AND_ASSET_GUIDELINES.md`](world-of-goses-design-bible/08_VISUAL_UI_AND_ASSET_GUIDELINES.md) § *Identidad resumida*.
- Audio: [`docs/world-of-goses-design-bible/09_AUDIO_GUIDELINES.md`](world-of-goses-design-bible/09_AUDIO_GUIDELINES.md) § *Identidad por linaje*.

These identities are not yet encoded in the project (the three placeholder PNGs are culture-neutral); they are documented so the next character and building art slices know what each lineage should look and sound like.

## 21. Outstanding open questions

The design bible maintains an explicit list of decisions still pending. They are not gaps in the slice — they are gaps in the game:

See [`docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md`](world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md) § *Preguntas abiertas* for the canonical list (cosmology, environmental axis name, time scale, combat elements, weapon families, ageing, migration, cultural mixing, politics, economy, population capacity, music, first biome, first systemic conflict).

The local ranking of those questions by immediate leverage lives in [`VALIDATION.md`](VALIDATION.md) § *Outstanding gaps*.

Keep documentation and code aligned as construction prerequisites deepen.
