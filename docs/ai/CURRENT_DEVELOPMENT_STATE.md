# Current development state

> **This file describes the state of the code at the listed update date. It
> does not replace design documents or code. Update it when a phase
> completes, a vertical slice advances, or the build/test baseline shifts.**

**Last updated:** 2026-07-30

---

## At a glance

| Dimension | Value |
| --- | --- |
| Active vertical slice | VS-5 signature and repetition in progress |
| Next approved work | Finish VS-5 diagnostic, then EG-0 prerequisite |
| Build | `dotnet build` clean (verified 2026-07-30) |
| Tests | 586 / 586 passing (verified 2026-07-30) |
| Save schema version (code) | `WorldSave.CurrentVersion = 19` |
| Save schema version (docs) | v19 |
| Headless boot | OK with `godot --headless --path game --quit-after 3` |
| Audio | No wired buses yet; `game/assets/audio/` is empty |
| Walkable macro-camera | Detailed walkable-world prototype postponed; street-perspective macro camera is active |

---

## Functional

The following are wired end to end in `game/scenes/CityPrototype.tscn` and the
domain. Treat each as a hard "do not regress" target.

- Founder onboarding, hero creation, profile view, lineage theme pack.
- Macro city view (`MacroStreetLiveView`), status bar, lineage ornament.
- Building placement + construction (Basic Shelter, Farm, Quarry) with
  recipe deposit, contributor assignment, pause/resume, deterministic
  drawdown, completion replacing the project.
- Wood gathering via the Forest detail panel; reserve drains into spendable
  pool.
- Production (Farm, Quarry) with recipe gate, workers, competency XP, stamina,
  day/night, min/max policy, visible stop causes; 10-tick batch cadence.
- Worker assignment service: single source of truth, auto-release at max
  stock, `InTransit`/`AtWork` location model.
- One visible workplace-travel path for founder and other citizens: the macro
  street route confirms arrival, interiors show only `AtWork`, and late arrival
  reverses home with diagnostics instead of freezing at the threshold.
- Free camera by default; selection is independent from follow, and WASD/arrows
  pan the camera without directly controlling the founder. Explicit follow uses
  the citizen selected in the restored Citizens roster.
- Semantic citizen routines expose activity, contextual location, blocker and
  transition timing without persisting visual coordinates. Mid-transit loads
  reconstruct elapsed visual progress from semantic timing and current map
  anchors; temporary idle/wait states may wander locally through pathfinding.
- Ambient day/night tint (`TimeOfDayFilter` + pure `TimeOfDayColor`): a
  full-viewport `ColorRect` driven by `GameClock.DayFraction`. It **multiplies**
  (`CanvasItemMaterial.BlendModeEnum.Mul`), it is not an alpha veil: an alpha
  overlay scales contrast by `1-alpha` and lifts the black point, so a night
  strong enough to read as night flattened the map into fog. `TimeOfDayColor`
  therefore returns the colour of the *light* — alpha always 1, strength in how
  far the channels fall below white, noon exactly white so it is a no-op.
  Two-speed curve: one-hour dawn (05:00-06:00) and dusk (18:00-19:00) bands
  that move fast, joined by long stretches that keep drifting slowly, all
  smoothstepped. Invariants: noon is the identity, the small hours stay clearly
  blue (a warm tint at 03:00 is a regression), no stretch is perfectly
  constant, and no channel ever exceeds white. Pin an hour for review with the
  `time-midnight`/`time-dawn`/`time-noon`/`time-dusk` visual fixtures.
- Ambient tint scope: the tint is an immersion effect for the **map only**.
  It renders on `OverlayLayers.AmbientTint` (5), below `OverlayLayers.Hud`
  (6), which the status strip, macro action bar, `BuildingDetailView` and
  `HeroProfileView` all claim; and it mirrors `MacroStreetLiveView.Visible`
  so full-screen views that replace the map are untinted even if they never
  touch the catalog. HUD chrome that renders tinted is HUD chrome that
  forgot to claim its layer.
- EG-0 opening measurement (`EarlyGameMetrics` + `EarlyGameMetricsReport`,
  schema v20): time to first shelter, resources gathered/spent, idle
  citizen-days, Food horizon, expedition absence. Counters are event- or
  dawn-driven, never per-tick, because `WorldTimeAdvance` batches quiescent
  stretches. The `CityResourceLedger` observer is detached during restore, or
  every reload would book the stockpile as freshly gathered. A v19-migrated
  city reports zero samples instead of invented history. `eg0-report.txt` is
  written beside the save on each successful save.
- Lineage accents (`LineageThemeRegistry.IconAccentByLineage`): Ardhen, Orveth
  and Vaelun were re-spread to copper (~20°), gold (~45°) and khaki (~62°).
  They previously shared a 10° amber band with Orveth and Vaelun only 2° apart,
  so their UI tints were not tellable apart. `tools/New-LineagePalettes.ps1`
  mirrors these values and refuses to generate a set where two accents are
  indistinguishable — close in hue *and* lightness *and* saturation. Caelith
  and Kovari sit 11° apart deliberately; they separate by lightness instead.
- Splash palettes (`art/palettes/`): one shared 36-colour file plus eight
  28-colour lineage files, and a derived 64-colour working file per lineage
  (Pixelorama shows one palette at a time). Generated, not hand-picked.
- Lineage splash illustrations (`LineageSplashRegistry`, 16 files under
  `game/assets/characters/splash/`): eight lineages × two body variants, so a
  splash identifies a *kind of person*, never an individual — two citizens of
  the same lineage and body variant share one. `HeroProfileView` shows it at
  full canvas height on the left with the text column scrolling on the right;
  the small animated sprite is the fallback when the asset is missing. Art is
  authored portrait and displayed downscaled, so the control uses
  `LinearWithMipmaps` — a deliberate local exception to the nearest-filter
  rule that in-world pixel art keeps. The set does not share one aspect ratio
  (nine 3:4, seven 4:5), which is why the width is computed from each
  texture's own proportion instead of being fixed.
- Citizens (basic): single sealed `Citizen`, roles, competencies, stamina,
  profile, gender.
- Migrant recruitment (first cut): `MigrantPanel`, deterministic name/profile.
- Minimal expedition (VS-2): persisted 1-2 hero team and retreat posture,
  deterministic encounter, explicit objective/retreat and return phases.
- Consequences (VS-3): persistent wound/Shelter treatment plus a four-state
  adjacent parcel target connected to successful reconnaissance.
- Persistence v2..v19: atomic JSON write, `.bak` sidecar, structural
  validation, typed `CityResourceLedger` reservations.
- Three-minute dirty-aware autosave + offline catch-up (7-day cap) via
  `OfflineProgression` and `WorldTimeAdvance`; save confirmation is temporary.
- Read-only Policies surface for the provisional workday, production,
  off-duty, and construction-authorization rules.
- Causal event log: `CauseEventId` chains, 128-event retention, `OfflineReportPanel`
  with "Decisions needed" grouping.
- Chronicle UI, GameUiShell, ModalHost, lineage theme.
- Snapshot-based presentation: `CityMacroSnapshot`, `HeroProfileSnapshot`,
  `BuildingDetailSnapshot`, `ConstructionSnapshot`, `CityStatusSnapshot`.
- Domain boundary enforcement (`DomainBoundaryTests`).
- Localization EN/ES with validator `tools/Test-LocalizationCatalog.ps1`.
- 16 lineage/gender detailed citizens (LPC sprites via `CharacterVisualRegistry`).

## Partial

These are real rules, but the implementation is missing required behavior.

- Lineages: qualitative identity metadata only, no mechanical effect.
- Farm/Quarry: no operating input recipe registered.
- Citizens: training, knowledge, relationships, detailed health, and potential
  remain absent; the first durable wound is implemented.
- Assignments use the authoritative persisted citizen commitment across work,
  construction, expedition, and recovery.
- Production policy priority remains a stored future scheduling hint; the
  current building detail exposes enabled/min/max and Policies explains the
  city-wide schedule.
- Storage: per-building capacity, no global capacity rule, no full cargo UI.
- Parcels: the first adjacent target is expedition-driven; broader maps and
  route content remain absent.
- Offline progression: empty/idle worlds fast-forward; assigned-work worlds
  step ticks.
- Upkeep: `Upkeep.ApplyUpkeep` is an intentional no-op.
- Expedition presentation remains a planning/status modal; a detailed side-view
  journey is not implemented.

## Prototype / placeholder

- Expedition art, formation, equipment, multiple encounters, cargo loss, and
  deeper health/territorial outcomes remain placeholder or absent.
- Recruitment uses the Town Hall prospect and housing-capacity flow; its costs,
  cadence and player-facing balance remain provisional until VS-5 signature.
- Hero walking animation is a procedural sinusoid.
- The official wound matrix has valid 1280×720 and 1920×1080 captures. The
  desktop harness can still intermittently attach to a 50×50 Godot client,
  which blocks a recapture but does not replace the last valid artifacts.
- Forest art is missing; plots render as a brown `ColorRect` with a "FOREST" label.
- Smithy and PotionLab art is absent (`BuildingArt.GetTexturePath` returns
  `null`).
- No audio buses or `AudioStreamPlayer` wired.

---

## Critical gaps (from `docs/FIRST_PLAYABLE_LOOP_AUDIT.md` §3)

| ID | Title | Severity |
| --- | --- | --- |
| G0 | Authoritative citizen commitment/condition | Implemented; VS-5 signature pending |
| G1 | Meaningful city pressure | Reopened: 60 Food vs 2/day produced no decision |
| G2 | Constrained recruitment | Town Hall/prospect/housing first cut; VS-5 signature pending |
| G3 | Expedition plan and team | Closed in VS-2 |
| G4 | Expedition phase, encounter, and retreat resolution | Closed in VS-2 |
| G5 | Persistent wound and shelter recovery | Closed in VS-3 |
| G6 | Territorial state machine and unlock consequence | Closed in VS-3 |
| G7 | Full snapshot and offline equivalence (cross-cutting) | Closed in VS-4 |

VS-2 through VS-4 close expedition planning, consequence, territory, and
persistence boundaries. VS-5 is active: an automated integration test proves a
save/reload followed by a second expedition with the same founder and no reset.
The remaining proof is a complete player-facing run and human relaunches at the
mid-expedition and mid-treatment boundaries without editor/debug paths.

## Out of scope (until prototype validates)

- Backend, database, server, API, CDN, auth, telemetry, modding, second city.
- Mobile (even stub).
- Multiplayer, account systems, second gameplay loop.
- Installer, launcher, settings UI.
- Final art, full audio pack, walkable macro-camera integration.
- Combat engine, weapons, formations, mortality, generations.
- Political, cultural, environmental, economic, trade, demographic simulation
  beyond current scope.
- Massive-population optimization without profiler evidence.

---

## Drift to fix in the docs

- No known save-schema drift after the VS-3 v19 update. Historical sections may
  still describe the version that introduced a specific subsystem.
- The audit's "post-onboarding focused fix" describes the G0 commitment work;
  this file aligns with that reading.

## Provenance of facts in this document

- Code facts: derived from the agent's code map (citizen/world/persistence
  paths, test counts, schema version).
- Gap list: `docs/FIRST_PLAYABLE_LOOP_AUDIT.md` §3.
- Build / test baselines: `docs/CURRENT_STATUS.md` and the audit.
- Visual matrix: `docs/VISUAL_REGRESSION.md`.

The detailed design, process, and architectural prose live in
`docs/world-of-goses-design-bible/`, `docs/PRODUCT_DIRECTION.md`,
`docs/ARCHITECTURE.md`, and `docs/REPOSITORY_CONVENTIONS.md`. This file
points to them; it does not duplicate them.
