# Context map

> Routing table. Classify a request here **before** reading design documents.
> Load the primary skill, then only the conditional skills whose trigger fires.
> Never load the whole `docs/` tree.

Skill ids resolve to `.agents/skills/<id>/SKILL.md`.
Agent ids resolve to `.agents/agents/<id>/AGENT.md`.

## How to use this file

1. Find the row that matches the request. If several match, the task is
   cross-domain — use `gameplay-integrator`.
2. Read the **primary skill** in full.
3. Read a **conditional skill** only when its trigger applies.
4. Open the **canonical docs** the skill names, not the whole bible.
5. Inspect the **code** listed before proposing a change.

If no route matches, use `gameplay-integrator` and add the missing route to
this file as part of the change.

---

## Global defaults

`core-game-vision` is required for any task that can change what the player
does, decides, or perceives. It is *not* required for a pure mechanical
refactor, a test-only change, or a typo fix.

`technical-foundation` is required whenever persistence, offline progression,
determinism, or the domain/presentation boundary is touched — regardless of
which domain owns the feature.

---

## Onboarding and founder

### Onboarding
- **Primary agent:** `narrative-lore`
- **Required skills:** `narrative-lore`, `core-game-vision`
- **Conditional skills:** `citizens-rpg`, `lineages-and-cultures`, `presentation-experience`, `technical-foundation`
- **Canonical docs:** `docs/world-of-goses-design-bible/07_ONBOARDING_AND_FOUNDER.md`, `docs/world-of-goses-design-bible/13_KOVARI_CUBE.md`
- **Code:** `game/scripts/Domain/FounderNarrativeCatalog.cs`, `FounderNarrativeSession.cs`, `FounderNarrativeScorer.cs`, `FounderNarrativeModels.cs`, `game/scripts/AstralOnboardingView.cs`, `OnboardingView.cs`
- **Consult `citizens-rpg` when:** the produced profile, aptitudes, or competencies change.
- **Consult `lineages-and-cultures` when:** lineage is inferred, presented, or selected.
- **Consult `technical-foundation` when:** the profile shape is persisted.

### Founder
- **Primary agent:** `narrative-lore`
- **Required skills:** `narrative-lore`, `core-game-vision`
- **Conditional skills:** `citizens-rpg`, `city-simulation`, `technical-foundation`
- **Canonical docs:** `docs/world-of-goses-design-bible/07_ONBOARDING_AND_FOUNDER.md`, `docs/world-of-goses-design-bible/13_KOVARI_CUBE.md`
- **Code:** `game/scripts/FounderArrivalSequence.cs`, `game/scripts/Domain/HeroCreationRequest.cs`, `HeroCreationResult.cs`, `CitizenProfile.cs`
- **Hard rule:** the founder must not become a permanent global bonus. See `CROSS_DOMAIN_INVARIANTS.md` → Citizens.

### First night / fire spirit
- **Primary agent:** `narrative-lore`
- **Required skills:** `narrative-lore`, `core-game-vision`, `presentation-experience`
- **Conditional skills:** `city-simulation` (Founding Site modules and recipes), `lineages-and-cultures` (per-lineage reactions), `technical-foundation` (persistence seam, schema version)
- **Canonical docs:** `docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md`, `docs/ai/DECISION_LOG.md` → DEC-0014, `docs/ai/CROSS_DOMAIN_INVARIANTS.md` → First night
- **Code:** `game/scripts/Domain/FirstNightState.cs`, `FirstNightStage.cs`, `FirstNightRules.cs`, `FoundingSiteRules.cs`, `FireSpiritDialogueCatalog.cs`, `game/scripts/FirstNightSpeechBubble.cs`, `FirstNightScene.cs`, `FireSpiritVisual.cs`, `FirstNightEmbers.cs`, `FirstNightContextCommentary.cs`, `CityWorldController.cs` (`FirstNightStageChanged` signal), `ExpeditionPanel.cs` (`SpiritTrailObjectiveButtonPath`), `ExpeditionPlanningSnapshot.cs` (`SpiritTrailUnlocked`), `game/locale/en.po` + `es.po` (`firstnight.*` keys)
- **Hard rule:** the route is strictly linear; variations per `LineageId` are textual reactions only, never route branches. `DialogueRunner.RunAsync` is **not** used here — `FirstNightState.CurrentDialogueNodeId` is the persistence seam. Quantities come from `FoundingSiteRules.InputsFor`, never from a hardcoded msgid. The night advances on a closed dialogue node or a completed module, never on the clock.
- **Consult `city-simulation` when:** the campfire, bedroll, cache, or canopy recipes change — the night's quantities must follow.
- **Consult `lineages-and-cultures` when:** the per-lineage reaction copy changes or a new lineage is added.
- **Consult `technical-foundation` when:** the schema bump question returns, or the `SpiritDeparted` event needs to persist a new field.

### Tools and inventory
- **Primary agent:** _(none yet — populate when a route touches this surface)_
- **Required skills:** _(none yet)_
- **Conditional skills:** _(none yet)_
- **Canonical docs:** _(none yet)_
- **Code:** _(none yet)_
- **Note:** inserted per the route-must-not-be-invented rule (see `docs/README.md` → "Authority hierarchy"). The current EG-5 backlog does not touch this surface; if a future agent acts here, it should populate the primary / required / conditional sections before opening work. Backlog items that imply tools or inventory (M-22 asset promotion, a future inventory UI) should land in this route, not as ad-hoc presentation code.

### Recruitment
- **Primary agent:** `citizens-rpg`
- **Required skills:** `citizens-rpg`, `core-game-vision`
- **Conditional skills:** `city-simulation`, `narrative-lore`, `lineages-and-cultures`, `technical-foundation`
- **Canonical docs:** `docs/world-of-goses-design-bible/04_CITIZENS_PROFESSIONS_AND_HEROES.md`, `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §13
- **Code:** `game/scripts/Domain/CitizenProspect.cs`, `CitizenOrigin.cs`, `game/scripts/MigrantPanel.cs`
- **Consult `city-simulation` when:** housing capacity, consumption, or production is affected.
- **Consult `narrative-lore` when:** there is dialogue, an origin story, or a historical event.
- **Consult `technical-foundation` when:** persistence changes, migrations are needed, or invariants change.

---

## Citizens

### Citizen
- **Primary agent:** `citizens-rpg`
- **Required skills:** `citizens-rpg`, `core-game-vision`
- **Conditional skills:** `technical-foundation`, `city-simulation`, `expeditions-territory`
- **Canonical docs:** `docs/world-of-goses-design-bible/04_CITIZENS_PROFESSIONS_AND_HEROES.md`
- **Code:** `game/scripts/Domain/Citizen.cs`, `CitizenId.cs`, `CitizenCommitment.cs`, `CitizenCommitmentKind.cs`, `CitizenAvailabilityReason.cs`, `CitizenAssignmentService.cs`, `Availability.cs`
- **Single-writer:** `Citizen.cs` is a shared-area file. See `AGENT_COLLABORATION_PROTOCOL.md`.

### Professions
- **Primary agent:** `citizens-rpg`
- **Required skills:** `citizens-rpg`, `core-game-vision`
- **Conditional skills:** `lineages-and-cultures`, `city-simulation`, `narrative-lore`
- **Canonical docs:** `docs/world-of-goses-design-bible/04_CITIZENS_PROFESSIONS_AND_HEROES.md` (twelve professional families, five competence layers)
- **Code:** `game/scripts/Domain/CompetencyId.cs`, `CompetencyEntry.cs`, `ProfessionFamilyId.cs`, `Role.cs`, `RoleId.cs`
- **Hard rule:** professions are accumulated state on `Citizen`, never a subclass.

### Heroes
- **Primary agent:** `citizens-rpg`
- **Required skills:** `citizens-rpg`, `core-game-vision`
- **Conditional skills:** `expeditions-territory`, `narrative-lore`
- **Canonical docs:** `docs/world-of-goses-design-bible/04_CITIZENS_PROFESSIONS_AND_HEROES.md`
- **Code:** `game/scripts/Domain/Role.cs`, `RoleId.cs`, `Citizen.cs`
- **Hard rule:** hero is a rank/recognition on a citizen, not an entity.

### Injuries
- **Primary agent:** `citizens-rpg`
- **Required skills:** `citizens-rpg`, `core-game-vision`
- **Conditional skills:** `expeditions-territory`, `city-simulation`, `technical-foundation`
- **Canonical docs:** `docs/world-of-goses-design-bible/02_CORE_GAMEPLAY_PILLARS.md` (pillar 6), `05_EXPEDITIONS.md`, `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §13
- **Code:** `game/scripts/Domain/CitizenVitalStatus.cs`, `StaminaRules.cs`
- **Open question:** the relationship between persistent wounds and the existing stamina model is **not settled**. See `DECISION_LOG.md` → DEC-0011 (Proposed). Do not assume a wound subsystem exists; read the gap statement first.

### Recovery
- **Primary agent:** `citizens-rpg`
- **Required skills:** `citizens-rpg`, `core-game-vision`
- **Conditional skills:** `city-simulation`, `technical-foundation`, `presentation-experience`
- **Canonical docs:** `docs/world-of-goses-design-bible/02_CORE_GAMEPLAY_PILLARS.md` (no instant healing), `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §13
- **Code:** `game/scripts/Domain/CitizenVitalStatus.cs`, `Building.cs` (Basic Shelter), `CitizenNeedsRules.cs`
- **Consult `city-simulation` when:** recovery consumes beds, staff, food, or medicine.

---

## City

### Construction
- **Primary agent:** `city-simulation`
- **Required skills:** `city-simulation`, `core-game-vision`
- **Conditional skills:** `citizens-rpg`, `technical-foundation`, `presentation-experience`
- **Canonical docs:** `docs/world-of-goses-design-bible/03_CITY_TERRITORY_AND_GROWTH.md`
- **Code:** `game/scripts/Domain/ConstructionProject.cs`, `ConstructionRules.cs`, `ConstructionSimulation.cs`, `ConstructionStopCause.cs`, `ConstructionAuthorizationResult.cs`, `game/scripts/ConstructionPanel.cs`
- **Hard rule:** construction is collaborative, phased work whose duration emerges from assigned citizens and conditions — not a fixed timer.

### Production
- **Primary agent:** `city-simulation`
- **Required skills:** `city-simulation`, `core-game-vision`
- **Conditional skills:** `citizens-rpg`, `technical-foundation`, `lineages-and-cultures`
- **Canonical docs:** `docs/world-of-goses-design-bible/02_CORE_GAMEPLAY_PILLARS.md` (pillar 4, causal production)
- **Code:** `game/scripts/Domain/BuildingProductionCalculator.cs`, `BuildingProductionSimulation.cs`, `Recipes.cs`, `ProductionStopCause.cs`, `game/scripts/ProductionPanel.cs`
- **Hard rule:** a building does not produce merely by existing. Every stop cause must be visible.

### Consumption
- **Primary agent:** `city-simulation`
- **Required skills:** `city-simulation`, `core-game-vision`
- **Conditional skills:** `citizens-rpg`, `technical-foundation`
- **Canonical docs:** `docs/world-of-goses-design-bible/02_CORE_GAMEPLAY_PILLARS.md`, `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §2.3 / §7 (Food horizon seam, EG-3)
- **Code:** `game/scripts/Domain/Upkeep.cs`, `CitizenNeedsRules.cs`, `CityEconomyRules.cs`, `CityInventory.cs`, `CityResourceLedger.cs`
- **Note:** `Upkeep.ApplyUpkeep` is currently an intentional no-op. Confirm before assuming upkeep runs.

### Farm
- **Primary agent:** `city-simulation`
- **Required skills:** `city-simulation`
- **Conditional skills:** `citizens-rpg`, `presentation-experience`, `technical-foundation`
- **Code:** `game/scripts/Domain/BuildingKind.cs`, `Recipes.cs`, `BuildingProductionCalculator.cs`
- **Note:** no operating input recipe is registered yet (`Recipes.OperatingRecipeFor`).

### Quarry
- **Primary agent:** `city-simulation`
- **Required skills:** `city-simulation`
- **Conditional skills:** `citizens-rpg`, `presentation-experience`, `technical-foundation`
- **Code:** `game/scripts/Domain/BuildingKind.cs`, `Recipes.cs`, `BuildingProductionCalculator.cs`, `TerrainWearGrid.cs`

### Shelter
- **Primary agent:** `city-simulation`
- **Required skills:** `city-simulation`
- **Conditional skills:** `citizens-rpg` (recovery, housing capacity), `presentation-experience`, `technical-foundation`
- **Code:** `game/scripts/Domain/Building.cs`, `BuildingKind.cs`, `ConstructionRules.cs`
- **Consult `citizens-rpg` when:** the shelter hosts recovery or bounds population.

---

## Expeditions and territory

### Expeditions
- **Primary agent:** `expeditions-territory`
- **Required skills:** `expeditions-territory`, `core-game-vision`
- **Conditional skills:** `citizens-rpg`, `city-simulation`, `technical-foundation`, `narrative-lore`
- **Canonical docs:** `docs/world-of-goses-design-bible/05_EXPEDITIONS.md`
- **Code:** `game/scripts/Domain/Expedition.cs`, `ExpeditionId.cs`, `ExpeditionStatus.cs`, `ExpeditionPhase.cs`, `ExpeditionRequest.cs`, `game/scripts/ExpeditionPanel.cs`
- **Mandatory consultation:** `citizens-rpg` for personal effects, `city-simulation` for costs/rewards/unlocks, `technical-foundation` for persistence and offline behavior.

### Encounters
- **Primary agent:** `expeditions-territory`
- **Required skills:** `expeditions-territory`, `core-game-vision`
- **Conditional skills:** `citizens-rpg`, `technical-foundation`, `narrative-lore`
- **Canonical docs:** `docs/world-of-goses-design-bible/05_EXPEDITIONS.md`, `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §8.4 (resource sortie cohesion)
- **Code:** `game/scripts/Domain/ExpeditionEncounterOutcome.cs`
- **Hard rule:** the encounter must be deterministic and reproducible from persisted state.

### Retreat
- **Primary agent:** `expeditions-territory`
- **Required skills:** `expeditions-territory`
- **Conditional skills:** `citizens-rpg`, `city-simulation`, `technical-foundation`
- **Canonical docs:** `docs/world-of-goses-design-bible/05_EXPEDITIONS.md`
- **Code:** `game/scripts/Domain/ExpeditionPhase.cs`, `ExpeditionStatus.cs`
- **Hard rule:** retreat is a configured posture, not a failure state without consequence.

### Return
- **Primary agent:** `expeditions-territory`
- **Required skills:** `expeditions-territory`, `core-game-vision`
- **Conditional skills:** `citizens-rpg`, `city-simulation`, `technical-foundation`
- **Canonical docs:** `docs/world-of-goses-design-bible/05_EXPEDITIONS.md`
- **Code:** `game/scripts/Domain/Expedition.cs`, `ExpeditionPhase.cs`, `CityResourceLedger.cs`
- **Hard rule:** an expedition includes the return leg. Survivors come back carrying their consequences.

### Territory
- **Primary agent:** `expeditions-territory`
- **Required skills:** `expeditions-territory`, `city-simulation`
- **Conditional skills:** `core-game-vision`, `technical-foundation`, `narrative-lore`
- **Canonical docs:** `docs/world-of-goses-design-bible/03_CITY_TERRITORY_AND_GROWTH.md`, `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §13 (territory advances as part of EG-4 resource sorties)
- **Code:** `game/scripts/Domain/CityParcel.cs`, `ParcelGrid.cs`, `ParcelId.cs`, `ParcelPlacement.cs`

### Parcels
- **Primary agent:** `city-simulation`
- **Required skills:** `city-simulation`
- **Conditional skills:** `expeditions-territory`, `presentation-experience`, `technical-foundation`
- **Canonical docs:** `docs/world-of-goses-design-bible/03_CITY_TERRITORY_AND_GROWTH.md`, `docs/world-of-goses-design-bible/12_DYNAMIC_FRONTAGE_PLOTS_AND_CORRIDORS.md`
- **Code:** `game/scripts/Domain/CityParcel.cs`, `ParcelGrid.cs`, `ParcelPlacement.cs`, `ConstructionRowId.cs`, `BuildingReservation.cs`, `CorridorReservation.cs`, `PassageClass.cs`, `BuildingFootprintCatalog.cs`

---

## Lineages and narrative

### Lineages
- **Primary agent:** depends on what changes. Lineages have **no owning agent**.
- **Required skills:** `lineages-and-cultures`, plus the skill of the domain being changed
- **Conditional skills:** `narrative-lore`, `citizens-rpg`, `presentation-experience`, `city-simulation`
- **Canonical docs:** `docs/world-of-goses-design-bible/06_LINEAGES.md` (index), `docs/world-of-goses-design-bible/13_KOVARI_CUBE.md`, the per-lineage chapters `docs/world-of-goses-design-bible/14_LINEAGES_ARDHEN.md` through `docs/world-of-goses-design-bible/21_LINEAGES_THERYN.md`, `docs/LINEAGE_DESIGN_MATRIX.md`
- **Code:** `game/scripts/Domain/LineageDefinition.cs`, `LineageId.cs`, `game/scripts/visual/CharacterVisualRegistry.cs`
- **Hard rule:** lineages are not classes, do not block professions, and must not become automatic multipliers.

### Narrative
- **Primary agent:** `narrative-lore`
- **Required skills:** `narrative-lore`, `core-game-vision`
- **Conditional skills:** `lineages-and-cultures`, `citizens-rpg`, `presentation-experience`
- **Canonical docs:** `docs/world-of-goses-design-bible/01_GAME_VISION.md`, `06_LINEAGES.md`, `07_ONBOARDING_AND_FOUNDER.md`, `13_KOVARI_CUBE.md`, `14-21_LINEAGES_*.md`
- **Code:** `game/scripts/Domain/Dialogue.cs`, `DialogueRunner.cs`, `FounderNarrativeCatalog.cs`, `game/scripts/Ui/WorldEventTextFormatter.cs`
- **Hard rule:** `narrative-lore` may not invent mechanics or bonuses. It proposes; the mechanical domain decides.

### Chronicle
- **Primary agent:** `narrative-lore`
- **Required skills:** `narrative-lore`
- **Conditional skills:** `technical-foundation`, `presentation-experience`, `city-simulation`
- **Code:** `game/scripts/Domain/WorldEvent.cs`, `WorldEventLog.cs`, `WorldEventRetention.cs`, `game/scripts/Ui/WorldEventTextFormatter.cs`, `game/scripts/OfflineReportPanel.cs`
- **Consult `technical-foundation` when:** event retention, causality chains, or persistence change.

---

## Technical

### Persistence
- **Primary agent:** `technical-foundation`
- **Required skills:** `technical-foundation`
- **Conditional skills:** the skill of every domain whose state is serialized
- **Canonical docs:** `docs/ARCHITECTURE.md`, `docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md`
- **Code:** `game/scripts/Domain/Persistence/WorldSave.cs` (`CurrentVersion`), `WorldPersistence.cs`, `IncompatibleSaveVersionException.cs`, and the `*Save.cs` DTOs
- **Hard rule:** any new persisted field requires a version decision, round-trip tests, and a documented migration path.

### Offline progression
- **Primary agent:** `technical-foundation`
- **Required skills:** `technical-foundation`
- **Conditional skills:** `city-simulation`, `expeditions-territory`, `citizens-rpg`
- **Code:** `game/scripts/Domain/OfflineProgression.cs`, `OfflineProgressionReport.cs`, `WorldTimeAdvance.cs`, `GameClock.cs`
- **Hard rule:** live advancement and offline catch-up must use the same domain rules. Never simulate second by second.

### Architecture changes
- **Primary agent:** `technical-foundation`
- **Required skills:** `technical-foundation`, `core-game-vision`
- **Conditional skills:** all affected domains; use `gameplay-integrator` if two or more pillars are involved
- **Canonical docs:** `docs/ARCHITECTURE.md`, `docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md`, `docs/REPOSITORY_CONVENTIONS.md`
- **Escalate:** an architecture change that invalidates saves without a migration strategy must stop and ask.

### Tests
- **Primary agent:** the agent owning the domain under test
- **Required skills:** `technical-foundation`, plus the domain skill
- **Conditional skills:** `vertical-slice-validation` when the test covers the playable loop
- **Code:** `tests/WorldofGoses.Tests/`
- **Command:** `cd tests/WorldofGoses.Tests; dotnet test`

### Refactors
- **Primary agent:** `technical-foundation`
- **Required skills:** `technical-foundation`
- **Conditional skills:** every domain whose files are touched
- **Hard rule:** a refactor changes structure, not behavior. If behavior changes, it is a feature and needs the feature route.
- **Escalate:** a refactor that would require removing or replacing a central system must stop and ask.

### Bugs
- **Primary agent:** the agent owning the domain of the defect
- **Required skills:** the domain skill
- **Conditional skills:** `technical-foundation` when persistence, simulation, or architecture is implicated
- **Workflow:** see `AGENT_COLLABORATION_PROTOCOL.md` → "Bug workflow".
- **Hard rule:** implement the most local fix plus a regression test. Do not escalate a bug into a general refactor.

---

## Presentation

### UI
- **Primary agent:** `presentation-experience`
- **Required skills:** `presentation-experience`
- **Conditional skills:** `core-game-vision`, the domain skill whose state is displayed, `lineages-and-cultures`
- **Canonical docs:** `docs/UI_PATTERNS.md`, `docs/UI_AUDIT.md`, `docs/world-of-goses-design-bible/08_VISUAL_UI_AND_ASSET_GUIDELINES.md`
- **Code:** `game/scripts/Ui/`, `game/scenes/`, the `*Snapshot.cs` presentation DTOs
- **Hard rule:** presentation renders state; it does not decide rules. No domain logic in `_Process`.
- **Verification:** a click-to-X flow is not done until verified with a real click. Code reading and headless boot are not sufficient.

### Pixel art
- **Primary agent:** `presentation-experience`
- **Required skills:** `presentation-experience`
- **Conditional skills:** `lineages-and-cultures`, `core-game-vision`
- **Canonical docs:** `docs/world-of-goses-design-bible/08_VISUAL_UI_AND_ASSET_GUIDELINES.md`, `docs/ART_PIPELINE.md`, `docs/ASSET_INVENTORY.md`, `docs/LICENSING_AND_ATTRIBUTION.md`
- **Code:** `art/source/`, `art/exports/`, `game/assets/`, `game/scripts/visual/`
- **Hard rule:** integer scale, nearest filter, no antialiasing. Placeholders are not final direction.

### Audio
- **Primary agent:** `presentation-experience`
- **Required skills:** `presentation-experience`
- **Conditional skills:** `lineages-and-cultures`, `narrative-lore`
- **Canonical docs:** `docs/world-of-goses-design-bible/09_AUDIO_GUIDELINES.md`, `docs/LICENSING_AND_ATTRIBUTION.md`
- **Code:** `game/assets/audio/` (currently empty — no buses are wired yet)
- **Future:** audio may split into a dedicated `audio-direction` agent. See `README.md` → "Adding a new agent".

---

## Cross-cutting

### Any task touching two or more pillars
- **Primary agent:** `gameplay-integrator`
- **Required skills:** `core-game-vision`, plus the primary skill of each affected domain
- **Use when:** progression changes; city, citizens, and expeditions integrate; a foundational decision moves; or the owning agent is unclear.

### Any completed change
- **Reviewer:** `quality-guardian` (read-only)
- **Required skills:** `vertical-slice-validation`, `core-game-vision`
- **Rule:** the reviewer must not be the agent that implemented the change.
