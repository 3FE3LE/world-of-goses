# Current development state

> **This file describes the state of the code at the listed update date. It
> does not replace design documents or code. Update it when a phase
> completes, a vertical slice advances, or the build/test baseline shifts.**

**Last updated:** 2026-07-29

---

## At a glance

| Dimension | Value |
| --- | --- |
| Active vertical slice | VS-5 signature and repetition in progress |
| Next approved work | Finish VS-5 diagnostic, then EG-0 prerequisite |
| Build | `dotnet build` clean (verified 2026-07-29) |
| Tests | 553 / 553 passing (verified 2026-07-29) |
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
