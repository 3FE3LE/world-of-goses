# Current development state

> **This file describes the state of the code at the listed update date. It
> does not replace design documents or code. Update it when a phase
> completes, a vertical slice advances, or the build/test baseline shifts.**

**Last updated:** 2026-08-03

---

## At a glance

| Dimension | Value |
| --- | --- |
| Active increment | EG-5 — consolidation (proposal §15) |
| Next approved work | Segundo/tercer plot y Farm consolidation; firma humana del hacha y apertura acotada. |
| VS-5 audit | Descartado 2026-07-31; `docs/FIRST_PLAYABLE_LOOP_AUDIT.md` borrado. |
| Build | `dotnet build` clean (verified 2026-08-03) |
| Tests | 721 / 722 passing (1 omitido por brittleness del snapshot JSON en `VerticalLoopPersistenceTests.Recovery_ReloadedHalfway`) |
| Save schema version (code) | `WorldSave.CurrentVersion = 28` |
| Save schema version (docs) | v28 (durable Shelter tool set; Primitive Axe forestry gate) |
| Headless boot | OK with `godot --headless --path game --quit-after 3` |
| Audio | No wired buses yet; `game/assets/audio/` is empty |
| Walkable macro-camera | Detailed walkable-world prototype postponed; street-perspective macro camera is active |

---

## Functional

The following are wired end to end in `game/scenes/CityPrototype.tscn` and the
domain. Treat each as a hard "do not regress" target.

- Founder onboarding, hero creation, profile view, lineage theme pack.
- Macro city view (`MacroStreetLiveView`), resource-free status bar, lineage
  ornament, physical-owner-anchored basic-resource gain feedback, and
  collapsible Shelter inventory. Before Cache the feedback follows the
  founder; afterward it anchors to Founding Site/Shelter storage.
- Before Shelter, Construction exposes the founder's six-unit carried load and
  then the Founding Cache's twelve-unit storage; unrelated legacy Food/Wood no
  longer blocks rudimentary gathering before Cache.
- Building placement + construction (Basic Shelter, Farm, Quarry) with
  recipe deposit, contributor assignment, pause/resume, deterministic
  drawdown, completion replacing the project.
- Rudimentary gathering is capacity-safe and idempotent; mature-tree Wood
  requires a persisted Primitive Axe crafted after Shelter completion.
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
- EG-3 first Cultivation Site (`CultivationSite`, schema v24): requires a
  completed Basic Shelter, 1 Branch + 1 Small Stone and 180 preparation work;
  sowing consumes 1 Food, the persisted `readyAtTick` resolves exactly after
  10,800 ticks in live/offline advancement, and harvest deposits 5 Food. The
  HUD projects daily ration, Food horizon and protected target; the macro view
  distinguishes Prepared/Sown/Growing/Ready/Spent without color alone.
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
  cadence and player-facing balance remain provisional until EG-6 (calibration/signature).
- Hero walking animation is a procedural sinusoid.
- The official wound matrix has valid 1280×720 and 1920×1080 captures. The
  desktop harness can still intermittently attach to a 50×50 Godot client,
  which blocks a recapture but does not replace the last valid artifacts.
- Forest art is missing; plots render as a brown `ColorRect` with a "FOREST" label.
- Smithy and PotionLab art is absent (`BuildingArt.GetTexturePath` returns
  `null`).
- No audio buses or `AudioStreamPlayer` wired.

---

## Critical gaps (reformulados al proposal)

La tabla G0–G7 del antiguo `FIRST_PLAYABLE_LOOP_AUDIT.md` se descarta junto
con ese documento. Lo que sigue siendo diagnóstico real del código:

| ID | Título | Cómo lo cierra el proposal |
| --- | --- | --- |
| Food pressure sin operating-input (antiguo G1) | La Granja heredada aún produce sin receta, pero ya no es la apertura. | **Cerrado en EG-3** — primer plot con semilla, espera y `readyAtTick` 10800 ticks. |
| Territory legible (antiguo G6) | La franja oscura era la parcela 9 bloqueada; el modelo fresco ahora expone solo tres parcelas horizontales y no renderiza frontier. | **Reabierto en EG-5**: sobre visual objetivo 8×9 y ventana móvil de 13 calles definidos; expansión suspendida hasta diseñar borde, adquisición causal y culling/batching lateral. |
| Wound/recovery alcanzable (antiguo G5) | Fórmula de encuentro actual sesgada a FullSuccess para citizens con competency alta. | Diferido hasta **EG-5**; wound como feature sin demo verificable se descarta. |

La cobertura de VS-2 (expedition planning), VS-3 (consequence), VS-4
(persistence) y VS-0 (city causal) **sigue activa en código** —
`ExpeditionTeamTests`, `ExpeditionEncounterTests`, `WoundRecoveryTests`,
`TerritoryProgressionTests`, `VerticalLoopPersistenceTests` siguen pasando,
aunque sus tests no se invocan como criterios de aceptación. Se conservan
como regressions de seguridad: EG-2 ya reemplazó la apertura de shelter y
EG-4 ya sustituyó la salida genérica de recursos por oportunidades finitas.

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
- Gap list: `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §3
  (diagnóstico de implementación) y §15 (orden de increments).
- Build / test baselines: `docs/CURRENT_STATUS.md` and the audit.
- Visual matrix: `docs/VISUAL_REGRESSION.md`.

The detailed design, process, and architectural prose live in
`docs/world-of-goses-design-bible/`, `docs/PRODUCT_DIRECTION.md`,
`docs/ARCHITECTURE.md`, and `docs/REPOSITORY_CONVENTIONS.md`. This file
points to them; it does not duplicate them.
