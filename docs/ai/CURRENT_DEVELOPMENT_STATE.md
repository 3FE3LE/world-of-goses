# Current development state

> **This file describes the state of the code at the listed update date. It
> does not replace design documents or code. Update it when a phase
> completes, a vertical slice advances, or the build/test baseline shifts.**

**Last updated:** 2026-07-29

---

## At a glance

| Dimension | Value |
| --- | --- |
| Active vertical slice | Astral founding-hero slice (see `docs/FIRST_PLAYABLE_LOOP_AUDIT.md` §4) |
| Next approved slice | VS-2 minimal expedition, then VS-3 consequences and territory |
| Build | `dotnet build` clean (verified 2026-07-28) |
| Tests | 455 / 455 passing (per `docs/CURRENT_STATUS.md`); 464 / 464 per audit |
| Save schema version (code) | `WorldSave.CurrentVersion = 16` (see Drift below) |
| Save schema version (docs) | v14 |
| Headless boot | OK with `godot --headless --path game --quit-after 3` |
| Audio | No wired buses yet; `game/assets/audio/` is empty |
| Walkable macro-camera | Postponed (TO_DO §3 H-29) |

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
- Citizens (basic): single sealed `Citizen`, roles, competencies, stamina,
  profile, gender.
- Migrant recruitment (first cut): `MigrantPanel`, deterministic name/profile.
- Reconnaissance (first cut): `ExpeditionPanel`, reserves 1 Wood, runs 4 in-game
  days, returns 1 Stone.
- Persistence v2..v16: atomic JSON write, `.bak` sidecar, structural
  validation, typed `CityResourceLedger` reservations.
- Autosave + offline catch-up (7-day cap) via `OfflineProgression` and
  `WorldTimeAdvance`.
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
- Citizens: training, knowledge, personal history, relationships, health,
  potential all absent.
- Assignments: exclusivity spread across `CurrentAssignment` + active-expedition
  lookup — **not yet one authoritative commitment model** (gap G0).
- Production policy triplet (`MinStock/MaxStock/Priority`) hidden from UI.
- Storage: per-building capacity, no global capacity rule, no full cargo UI.
- Parcels: founding grid unlocked; no expedition-triggered transitions.
- Offline progression: empty/idle worlds fast-forward; assigned-work worlds
  step ticks.
- Upkeep: `Upkeep.ApplyUpkeep` is an intentional no-op.
- Expedition: no team selection, no encounter, no phases.

## Prototype / placeholder

- Expedition is a single-button reconnaissance with a fixed reward.
- Recruitment is free, unlimited, and immediate; `AtCapacity` is never returned.
- Hero walking animation is a procedural sinusoid.
- Visual regression harness is windowed-capture-blocked (50 x 50 client area).
- Forest art is missing; plots render as a brown `ColorRect` with a "FOREST" label.
- Smithy and PotionLab art is absent (`BuildingArt.GetTexturePath` returns
  `null`).
- No audio buses or `AudioStreamPlayer` wired.

---

## Critical gaps (from `docs/FIRST_PLAYABLE_LOOP_AUDIT.md` §6)

| ID | Title | Severity |
| --- | --- | --- |
| G0 | Authoritative citizen commitment/condition | P0 |
| G1 | Meaningful city pressure | P0 |
| G2 | Constrained recruitment | P1 |
| G3 | Expedition plan and team | P0 |
| G4 | Expedition phase and encounter resolution | P0 |
| G5 | Persistent wound and shelter recovery | P0 |
| G6 | Territorial state machine and unlock consequence | P0 |
| G7 | Full snapshot and offline equivalence (cross-cutting) | P0 |

The audit places the project at roughly half of the closed first playable
loop. The first technical task is to implement the authoritative commitment
model (G0) before any other expedition work.

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

- Save schema version. `WorldSave.CurrentVersion` in code is **16**, but
  `docs/CURRENT_STATUS.md` and `docs/ARCHITECTURE.md` still reference **v14**.
  This file records the code value. The doc drift must be corrected in a
  separate, narrow documentation change.
- The audit's "post-onboarding focused fix" describes the G0 commitment work;
  this file aligns with that reading.

## Provenance of facts in this document

- Code facts: derived from the agent's code map (citizen/world/persistence
  paths, test counts, schema version).
- Gap list: `docs/FIRST_PLAYABLE_LOOP_AUDIT.md` §6.
- Build / test baselines: `docs/CURRENT_STATUS.md` and the audit.
- Visual matrix: `docs/VISUAL_REGRESSION.md`.

The detailed design, process, and architectural prose live in
`docs/world-of-goses-design-bible/`, `docs/PRODUCT_DIRECTION.md`,
`docs/ARCHITECTURE.md`, and `docs/REPOSITORY_CONVENTIONS.md`. This file
points to them; it does not duplicate them.
