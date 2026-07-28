# First Playable Loop Audit

Audit date: 2026-07-28  
Scope: repository state at the audit commit/worktree; no gameplay implementation was changed.  
Verification: `dotnet build` from `game/` passed with 0 warnings and 0 errors; `dotnet test` from `tests/WorldofGoses.Tests/` passed 464/464 tests.

## 1. Executive summary

World of Goses is **roughly halfway to a closed first playable loop**. It is no longer a collection of wholly isolated experiments: onboarding, founder creation, gathering, construction, recruitment, assignment, causal production, a timed reconnaissance, persistence, offline catch-up, and causal history are connected to the main `CityPrototype.tscn` flow. The foundation is unusually strong for this stage, especially the single `Citizen` model, deterministic domain, resource reservations, versioned atomic saves, event causality, and test coverage.

The loop is not closed because its second half is still an abstract shortcut:

```text
real founder → real gathering → real construction → free migrant → real assignment
→ causal but weakly pressured production → one-button timer expedition
→ fixed reward → unchanged territory → no injury/recovery → no new decision
```

The critical missing product proof is not more buildings or content. It is one causal chain in which named citizens are removed from city work, prepared for an expedition, pass through at least one explicit encounter, return with persistent consequences, change a parcel or route, force a new city decision, and survive save/load. The smallest coherent implementation should extend the existing `Citizen`, `Expedition`, `CityParcel`, `WorldEventLog`, `CityResourceLedger`, controller, and panels. A rewrite is neither required nor justified.

The highest-priority technical decision is to introduce one authoritative, mutually exclusive citizen commitment/condition model that covers city work, construction, expedition membership, and recovery. `CurrentAssignment`, active-expedition lookup, `CurrentLocation`, stamina, and `CitizenBehavior` currently cooperate but do not form one persisted invariant. Closing that seam first prevents every later expedition, injury, UI, and save feature from duplicating availability rules.

## 2. Current repository state

### Domain and application boundary

The core domain under `game/scripts/Domain/` imports no Godot namespace. `DomainBoundaryTests.cs` enforces the boundary. `CityWorld` is the aggregate and public use-case facade; it delegates assignment consistency to `CitizenAssignmentService`, production ticks to `BuildingProductionSimulation`, construction ticks to `ConstructionSimulation`, stock/reservations to `CityResourceLedger`, and history to `WorldEventLog`.

There is no separate application assembly. Application orchestration is split between the pure `CityWorld` facade and the Godot-facing `CityWorldController`. That is acceptable for the current slice, but expedition resolution is already large enough that the next implementation should extract a pure domain/application collaborator rather than enlarge either Godot panels or `_Process`.

### Main connected presentation

`game/project.godot` starts `game/scenes/CityPrototype.tscn`. That scene instances `OnboardingView.tscn`, the city macro view, construction and building-detail panels, `AssignmentPanel`, `ProductionPanel`, `MigrantPanel`, `ExpeditionPanel`, Chronicle/offline report UI, and `CityWorldController`. `CityMacroView.cs` opens Citizens and Reconnaissance from real macro action buttons. These are not editor-only fixtures.

The current main flow supports:

- Twelve-fragment astral onboarding, founder naming/body presentation, founder creation, fall, title card, and arrival.
- Natural wood patches, direct gathering, placement, construction projects, contributor assignment, recipe drawdown, completion, Farm and Quarry.
- Deterministically generated non-hero citizens, a selectable roster, assignment/removal, stamina, competency experience, production, stock caps, and visible stop causes.
- A single active reconnaissance using only the founder, reserving one Wood, lasting four game days, and returning one Stone.
- Autosave, close-save, version migrations through schema v14, backup writes, load validation, offline catch-up, and a bounded causal Chronicle.

### Important provisional or dormant seams

- `Upkeep.StonePerTick` exists and is tested, but `CityWorld.ApplyUpkeep` and `ApplyUpkeepBatch` are intentionally no-ops.
- `Recipes.OperatingRecipeFor` has no Farm or Quarry recipes; current production pays only labor, time, stamina, and storage.
- `CitizenBehaviorState.Injured` currently means stamina reached zero while working. It is not a health/injury model and automatically leaves `Injured` when stamina is restored.
- `CityParcel` contains only coordinates plus `IsUnlocked`; all founding parcels are created unlocked.
- `Expedition` is a scheduled reservation/reward record. It has no team, route, phase, encounter, threat, retreat policy, cargo loss, wound, or territorial outcome.
- Recruitment has an `AtCapacity` enum value but no capacity rule; the UI action is free, repeatable, and unconditional after founder creation.
- Audio streams, players, buses, and the proposed first audio pack are absent from the connected scene.
- Placeholder and licensed/generated visual assets are extensive, but expedition presentation and final system iconography are not present.

## 3. Currently possible playable flow

The actual connected sequence is:

1. `AstralOnboardingView` drives `FounderNarrativeSession` and scoring.
2. `CityWorldController.TryCreateHero` calls `CityWorld.TryCreateHero`; `Citizen` receives the `Hero` role rather than becoming a separate type.
3. `FounderArrivalSequence` and `CityMacroView` present the fall and city arrival.
4. The player gathers Wood from a `NaturalResourcePatch` through `CityWorld.GatherWood`; the founder must be unassigned and not away.
5. The player places and authorizes Basic Shelter, Farm, or Quarry. `ConstructionSimulation` advances assigned contributors and consumes the persisted remaining recipe.
6. Completed buildings become `Building` entities. The building detail UI can assign/remove real citizens and configure production.
7. The Citizens panel can create unlimited deterministic migrants at no cost or world precondition. Migrants persist and can be assigned.
8. Farm/Quarry produce during day ticks when enabled, staffed, not capped, and workers have stamina. Production builds competency experience. Storage caps and stop causes are visible.
9. The Reconnaissance panel can dispatch only the unassigned founder if one Wood is available and no expedition is active.
10. Four days of normal ticks pass. There is no represented journey or encounter. The reservation commits, one Stone is deposited, the founder returns fully unchanged, and Chronicle records departure/return.
11. Autosave and offline catch-up preserve the connected state.

This flow is executable without editor tooling, but steps 7 and 9–10 are prototypes rather than the requested RPG/city consequence loop. There is no loop-ending unlock or newly forced decision, so repetition only accumulates citizens/resources.

## 4. Target playable flow

The vertical slice should prove this minimal sequence:

```text
Founder onboarding and arrival
→ gather and build Basic Shelter
→ build Farm and Quarry in a deliberate order
→ receive one constrained recruitment opportunity
→ assign named citizens to food/stone/construction
→ consume food or supplies through an explainable pressure
→ remove chosen citizens from incompatible city commitments
→ reserve a small expedition loadout and choose one retreat posture
→ outbound phase → one deterministic encounter → objective → return phase
→ apply cargo/resource result and one possible persistent wound/fatigue result
→ advance one parcel/route from locked to available through explicit states
→ recover the injured citizen at Basic Shelter using time plus Food
→ choose between recovery, production, construction, and another expedition
→ save, quit, load, and repeat without debug actions
```

Minimal does not mean arbitrary. One destination, one encounter family, one injury type, one recovery recipe, one territorial chain, and one team size range are enough if every result is causal, explained, persisted, and extensible.

## 5. System matrix

Status vocabulary: **Functional** means connected to the main scene and supported by domain tests; **Partial** means real rules exist but the slice lacks required behavior; **Prototype** means a deliberately simplified interaction stands in for the intended system; **Placeholder** means presentation/data exists without the intended system; **Disconnected** means code exists but does not affect the playable flow; **Missing** means no authoritative implementation was found.

| System | Status | Evidence | Blocker | Priority |
| --- | --- | --- | --- | --- |
| Onboarding | Functional | `FounderNarrativeSession`, `FounderNarrativeScorer`, `AstralOnboardingView`, `OnboardingView.tscn`; `OnboardingDomainTests`, `ProfileAndOnboardingTests` | No loop blocker | P3 |
| Founder | Functional | `CityWorld.TryCreateHero`, `HeroCreationRequest`, `FounderArrivalSequence`, `HeroProfileView`; `FirstRunRegressionTests` | No loop blocker | P3 |
| Lineages | Partial, correctly qualitative | `LineageId`, `LineageDefinition`, `ProfileCatalog`, `LineageThemeRegistry`; `LineageThemeRegistryTests` | No current mechanical consequence; must not become a production multiplier | P3 |
| City inventory | Functional foundation | `CityInventory`, `CityResourceLedger`, `WorldSave.CityInventory`; `CityResourceLedgerTests` | No global capacity or cargo semantics | P1 |
| Resources | Functional foundation | `ResourceType`, `ResourceLocation`, reservations, natural patches, building stock | Few resource sinks and no expedition equipment/cargo | P1 |
| Initial gathering | Functional but narrow | `CityWorld.GatherWood`, `ResourceActionMenu`, `NaturalResourcePatch`; `ForestTests` | Founder-only action; no work order or gathering competency decision | P2 |
| Construction | Functional | `ConstructionProject`, `ConstructionSimulation`, `ConstructionRules`, `ConstructionPlacementOverlay`; construction test suite | No new territorial unlock feeds construction | P1 |
| Basic Shelter | Functional building, partial systemic role | `CreateCompletedBuilding` maps shelter to `BuildingKind.Home`; onboarding/construction flow tests | Does not provide recovery capacity or treatment | P0 |
| Farm | Functional causal producer, partial economy | `BuildingKind.Farm`, `BuildingProductionSimulation`, building detail/production panels | No operating input; food pressure is incidental stamina recovery only | P0 |
| Quarry | Functional causal producer, partial economy | `BuildingKind.Quarry`, production calculator/simulation | No operating input; Stone has no active upkeep sink | P0 |
| Citizens | Functional foundation | Single `Citizen` with roles, competencies, profile, assignment, stamina, history events | No durable health, commitments, personal history, expedition records | P0 |
| Recruitment | Prototype | `CityWorld.TryRecruitMigrant`, `MigrantPanel`; `FirstRunRegressionTests` | Free, unlimited, immediate; `AtCapacity` is never returned | P1 |
| Assignments | Functional for buildings/projects | `CitizenAssignmentService`, `AssignmentPanel`, `AssignmentRow`; assignment tests | Exclusivity is spread across assignment plus expedition lookup; no recovery commitment | P0 |
| Production | Partial but genuinely causal | worker count, competency experience, stamina, day/night, policy, capacity in `BuildingProductionSimulation` | Farm/Quarry lack accessible-resource/tool inputs and meaningful demand | P0 |
| Consumption | Partial/disconnected | Food is consumed for stamina recovery; construction recipes consume resources; `Upkeep` exists | No continuous pressure strong enough to force city choices; upkeep is dormant | P0 |
| Storage | Partial | per-building capacity, min/max policy, city inventory and reservation availability | City inventory has no capacity; UI does not express all location/cargo constraints | P1 |
| Parcels | Partial foundation | `CityParcel`, `ParcelGrid`, `ParcelPlacement`, `OrthogonalParcelTerrain`; parcel tests | Boolean only; founding grid unlocked; no expedition transition or unlock content | P0 |
| Expeditions | Prototype, connected | `Expedition`, `ExpeditionRequest.Reconnaissance`, `CityWorld.StartExpedition`, `ExpeditionPanel`; resource-ledger/FSM tests | Timer + fixed reward, one founder, one active expedition | P0 |
| Combat/encounter | Missing | No domain combatant, encounter, threat, resolution, or test; detailed sprites do not constitute a system | No causal expedition risk or consequence | P0 |
| Retreat | Missing | Cancel releases reservation and returns founder, but it is a UI cancellation, not a configured retreat rule | No defeat/survival decision | P1 |
| Return | Prototype | `CompleteFinishedExpeditions`, `ReturnLeadFromExpedition`, return event | Instant at end tick; no return leg or altered cargo/state | P0 |
| Wounds | Missing; stamina state is not a substitute | `CitizenBehaviorState.Injured` is entered by `ConsumeStamina(0)` and left by `RestoreStamina`; no wound data/DTO | Cannot persist expedition consequences or reduce availability correctly | P0 |
| Recovery | Missing | `PrimaryHome` exists; night/stamina regeneration exists; no treatment case | City cannot answer expedition harm | P0 |
| Unlocks | Missing from gameplay | `CityParcel.Unlock` exists but no call from expedition; all founding parcels start unlocked | No transformed territory or new post-return opportunity | P0 |
| Save/load | Functional for current model | `WorldPersistence`, `WorldSave.CurrentVersion = 14`, temp/backup write, validation/migrations; persistence tests | New commitments, health, encounter, parcel states must be added atomically | P0 cross-cutting |
| Offline progression | Functional for current ticks, partial target | `OfflineProgression`, `WorldTimeAdvance`; offline tests | Assigned work still steps ticks; future expedition/recovery batching not modeled | P1 |
| State/blocker UI | Functional for current production/construction, incomplete for target | `ProductionStopCause`, `ConstructionStopCause`, snapshots/panels, Chronicle | Expedition dispatch disables without fully explaining every reason; no health/territory explanation | P0 |
| Provisional audio | Missing | No connected `AudioStreamPlayer`, audio buses, or files under `game/assets/audio` found | Does not block logic closure; weakens feedback only | P2 |
| Provisional assets | Extensive but uneven | lineage sprites/themes, placeholder buildings/terrain/cursors; art inventory and licensing docs | No expedition scene, wound/cargo/territory visuals; Forest still provisional | P2 |
| Tests | Strong for current domain | 464 passing tests across onboarding, construction, production, persistence, offline, events, UI snapshots | No end-to-end vertical-loop test; no health/territory/encounter tests because systems do not exist | P0 |

## 6. Critical gaps to close the loop

### G0 — Authoritative citizen commitment and condition

- **Why it blocks:** work, construction, expedition, rest, and recovery must be mutually exclusive. Today availability is derived from `CurrentAssignment` while expedition absence is queried from `CityWorld`, and `Citizen.Availability` ignores expedition state.
- **Dependents:** assignment UI, expedition team selection, health, recovery, macro visibility, save validation, offline progression.
- **Minimum:** one persisted commitment/state representation owned by `Citizen` or an aggregate-owned registry, with explicit transitions and rejection reasons. Keep roles/competencies additive.
- **Later:** concurrent secondary memberships, schedules, shifts, institutions, complex leave.
- **Technical risk:** medium because schema and multiple call sites change.
- **Design risk:** low; the principle is explicit.
- **Priority:** P0, first.

### G1 — Meaningful city pressure and causal production completion

- **Why it blocks:** with unlimited migrants, no housing cap, dormant upkeep, and almost no operating consumption, assignment is optimization rather than sacrifice.
- **Dependents:** recruitment, expedition opportunity cost, recovery, repeatability.
- **Minimum:** add one explainable demand: daily/phase Food consumption for present citizens and expedition/recovery supplies, with shortage consequences limited to availability/stamina rather than death. Add one operating dependency only if it creates a decision the player can satisfy.
- **Later:** tools, energy, logistics, nutrition classes, wages, complex upkeep.
- **Technical risk:** medium due live/offline equivalence.
- **Design risk:** high; overtuning can stall the founding sequence.
- **Priority:** P0.

### G2 — Constrained recruitment

- **Why it blocks:** free unlimited population erases housing and labor scarcity.
- **Dependents:** cost of opportunity, food pressure, shelter meaning, expedition team composition.
- **Minimum:** one recruitment opportunity unlocked by Basic Shelter, capacity of two or three residents, and a clear cost/cooldown or expedition-delivered migrant. Use the existing `AtCapacity` outcome.
- **Later:** migration cultures, families, demographics, diplomacy.
- **Technical risk:** low.
- **Design risk:** medium; do not let a failed recruit soft-lock the slice.
- **Priority:** P1 before expedition balancing.

### G3 — Expedition plan and team

- **Why it blocks:** current reconnaissance contains no person choice beyond an automatically selected founder.
- **Dependents:** opportunity cost, competencies, supplies, encounter, injury.
- **Minimum:** select 1–2 real citizens, validate availability, reserve one supply bundle, choose one retreat posture, and persist the captured plan.
- **Later:** formations, equipment slots, many roles/skills, tactical priorities.
- **Technical risk:** medium.
- **Design risk:** medium; one competency comparison must remain legible.
- **Priority:** P0.

### G4 — Expedition phase and encounter resolution

- **Why it blocks:** elapsed time alone cannot prove automated expedition gameplay.
- **Dependents:** return, wounds, cargo loss, territory, Chronicle.
- **Minimum:** persisted phases `Preparing/Outbound/Encounter/Objective/Returning/Resolved`; one deterministic encounter whose outcome uses team condition, one competency, supplies, and retreat posture. Store the result, not frame animation.
- **Later:** combat engine, multiple enemies, skills, equipment quality, procedural segments.
- **Technical risk:** medium-high; must remain deterministic offline.
- **Design risk:** high; causality and explanation matter more than content count.
- **Priority:** P0.

### G5 — Persistent wound and shelter recovery

- **Why it blocks:** return has no city consequence, and stamina recovers automatically.
- **Dependents:** availability, production modifiers, new decisions, persistence, offline.
- **Minimum:** one `CitizenCondition`/wound with severity, work/expedition restriction, recovery ticks, Food cost, originating event, and treatment at Basic Shelter. Do not overload stamina or `Behavior`.
- **Later:** body locations, doctors, medicine, rehabilitation, mortality.
- **Technical risk:** medium due state transitions and save schema.
- **Design risk:** medium; first injury cannot create an unrecoverable lock.
- **Priority:** P0.

### G6 — Territorial state machine and unlock consequence

- **Why it blocks:** the expedition returns resources but changes no future option.
- **Dependents:** construction placement, new decision, repeatable loop.
- **Minimum:** one adjacent target parcel with four states such as `Locked → Reconnoitred → RouteSecured → Available`; the first expedition advances it and reveals one construction/gathering opportunity.
- **Later:** all documented parcel states, biomes, ecological policies, threats, route networks.
- **Technical risk:** medium due migration and rendering.
- **Design risk:** low-medium if the unlock is concrete.
- **Priority:** P0.

### G7 — Full snapshot and offline equivalence for new state

- **Why it blocks:** a desktop persistent game cannot close the loop if expedition/recovery/territory are lost or resolve differently while closed.
- **Dependents:** every G0–G6 system.
- **Minimum:** next schema version captures commitments, team, phase/result, conditions, recovery, and parcel state; validate references; migrate v14 defaults; batch to the next discrete boundary.
- **Later:** generalized migration tooling and event replay.
- **Technical risk:** high but contained by existing persistence architecture.
- **Design risk:** low.
- **Priority:** P0 cross-cutting, implemented with each phase rather than postponed.

## 7. Existing but disconnected systems

1. `Upkeep.StonePerTick` and `UpkeepTests` are intentionally disconnected by empty `ApplyUpkeep` methods. Do not simply reactivate them: abstract Stone disappearance would contradict the causal-production principle. Replace the placeholder with a visible Food/recovery/supply demand.
2. `CityParcel.Unlock` and persisted `IsUnlocked` have no gameplay caller. The placement system is ready to consume unlocks, but founding setup unlocks the whole current grid.
3. `ExpeditionRewardKind.Migrant` and migrant-return persistence exist, but `ExpeditionPanel` only constructs `Reconnaissance`, whose reward is Stone.
4. `ExpeditionStatus.Failed` is reachable only if reservation commit/recruitment fails, not from an encounter or retreat.
5. `CitizenBehaviorState.Injured` is connected to stamina exhaustion, not health. It should remain a behavior signal or be renamed later; it cannot serve as a wound record.
6. Competencies affect building output and are accumulated through work, but the expedition ignores them.
7. Founder profile fields include combat style, weapons, risk and leadership, but the expedition ignores all of them. They should not all become mechanics now; one relevant competency/risk input is sufficient.
8. `WorldEventLog` already supports typed expedition/citizen subjects and causal chains, but does not record encounter, wound, treatment, route secured, or parcel available events.
9. The Chronicle/offline report can communicate causes, but expedition outcome detail is only dispatch/return amount.
10. Prototype walkable macro/interior scenes are explicitly isolated and should remain postponed; they do not close the loop.

## 8. Architecture risks

### Split availability invariant

`Citizen.Availability` only checks `CurrentAssignment`; `CityWorld.AvailableCitizens` separately checks active expeditions; health/recovery will add a third check. Without consolidation, UI and use cases will disagree. This is the most immediate structural risk.

### Aggregate growth

`CityWorld.cs` owns creation, gathering, construction, assignment facade, production orchestration, expeditions, recruitment, parcels, restore, and migration-facing reconstruction. Existing extractions show the right direction. Add `ExpeditionSimulation`/resolution and a focused condition/recovery service, with `CityWorld` retaining aggregate authorization and events.

### Application layer ambiguity

Godot panels call controller methods, which call aggregate methods. This is currently thin enough, but encounter decisions must not be authored in `ExpeditionPanel` or `_Process`. A pure use-case service or aggregate collaborator should resolve discrete phases.

### FSM semantic overload

`CitizenBehaviorState.Injured` is triggered by zero stamina and automatically transitions after stamina regeneration. Durable injury is a different axis. Mixing them would make saves, treatment, and UI ambiguous.

### Persistence schema breadth

The persistence system is strong, but adding optional fields without cross-entity validation could admit expedition members who are simultaneously assigned, conditions referencing missing cause events, or available parcels without their prerequisite route. Extend `Validate` with each new invariant.

### Tick-by-tick offline work

Worlds with assignments still step all ticks. That is acceptable at current scale, but expedition phases and recovery should be modeled as next-boundary discrete calculations from the start. Do not add per-citizen/per-second loops.

### Presentation references into aggregate internals

`ExpeditionPanel` and `MigrantPanel` iterate `World` collections directly. This is manageable today but will leak rules as complexity grows. Add read-only snapshots for expedition planning/outcome and citizen availability rather than making panels reconstruct eligibility.

### Dead/provisional code becoming authority

`Upkeep` and the current `Injured` label look authoritative because they have tests. Tests prove their current code, not that they satisfy the design. The new slice should explicitly retire or reframe them rather than silently build on misleading semantics.

## 9. Design risks

1. **Unlimited recruitment removes scarcity.** Named citizens become production multipliers instead of people whose absence matters.
2. **Production has inputs in code but not in the current Farm/Quarry loop.** Waiting for counters dominates because the player rarely diagnoses a supply chain.
3. **Automatic worker release at max stock is useful delegation but can erase intentional staffing decisions if not explained.** Chronicle/UI should say who became available and why.
4. **The only expedition choice is whether to click Dispatch.** No team or retreat decision means no RPG expression.
5. **A fixed one-Wood-to-one-Stone timer resembles an idle conversion recipe.** It must become a journey with person-specific risk and territorial meaning.
6. **Founder exclusivity makes “hero” look like a separate class in practice even though the data model is correct.** The first recruit must be eligible for expedition participation or hero recognition through the same `Citizen` entity.
7. **Stamina called injury trivializes wounds.** Automatic night recovery contradicts “no instant/general healing” if reused for expedition harm.
8. **All current parcels being unlocked makes expansion decorative.** A locked target visible before departure is needed to frame the expedition purpose.
9. **Resource rewards without recorded personal change favor accumulation over history.** At minimum, encounter participation, wound, and return should attach causal events to citizens.
10. **Rich founder profile fields risk becoming unused character-sheet decoration.** Use only fields tied to an actual decision; postpone the rest rather than fabricate bonuses.

## 10. Elements that make the game resemble a generic city builder

| Current signal | Why it reads generically | Reorientation |
| --- | --- | --- |
| Construction menu exposes Shelter/Farm/Quarry as the main progression | Buildings appear to be the goals | Present each building as capacity for people: shelter beds/recovery, farm assignments/food security, quarry competence/material decision |
| Free repeated Recruit button | Citizens read as count increments | Make each arrival an opportunity with identity, capacity, and a decision about housing/work/risk |
| Production rate rises with every assigned citizen | People can feel interchangeable despite names | Show named contribution, competence growth, condition, and the cost of moving that person elsewhere |
| Farm and Quarry run without operating material chains | Core play becomes assign-and-wait | Introduce one understandable pressure and explicit blocker, not a broad economy |
| Reconnaissance is a resource converter | Expedition reads as another building timer | Select people/supplies/retreat, resolve an encounter, return through a phase, and change territory/person state |
| Fixed Stone reward | Accumulation is the only consequence | Pair material reward with knowledge/route state and citizen history |
| No injury/recovery | Leaving town has no lasting human cost | Persist one wound and make Shelter treatment compete with work/supplies |
| Boolean unlocked parcels, all initially open | Expansion is placement space | Show a named target route/parcel and causal state transitions |
| Chronicle focuses heavily on production events | City history reads as an operations log | Add citizen/expedition/territory causal events and compact routine production more aggressively |
| Founder is the only expedition participant | Hero looks like a special unit | Allow ordinary `Citizen` members and use a role/recognition transition after survival |

The city-builder component should remain: buildings, stock, policies, lots, and production are the material infrastructure that creates choices about people. The correction is to make every important infrastructure change answer “which citizens can act, what do they risk, and what history does this create?”

## 11. Changes that reinforce RPG identity

1. Make every availability row name the citizen and explain the exclusive commitment or condition preventing selection.
2. Let competency experience already earned in Farm/Quarry influence one expedition encounter in a small, transparent way.
3. Record expedition participation and outcome as citizen history references to causal `WorldEventId`s; avoid a large narrative framework.
4. Grant or progress the existing `Hero` role through a post-return recognition decision, not a separate hero entity.
5. Make Shelter provide a visible recovery slot. The player chooses whether a citizen occupies it and spends Food/time.
6. Add a return summary that names contributions, wound/cargo/route outcomes, and their causes.
7. Give the first unlocked parcel one meaningful use choice—e.g. secure gathering access versus reserve it for later construction—without implementing the full environmental axis.
8. Preserve generated migrants' distinct profiles and expose only the competencies relevant to the present decision.
9. Use Chronicle events as accumulated city history: recruited, assigned, departed, encounter survived, wounded, treated, route secured, returned.
10. Keep lineage qualitative in this closure. Cultural mechanics, social recognition systems, and deep synergies belong after the loop proves that named people matter.

## 12. Phased implementation plan

### Phase 0 — Invariant sanitation

1. Define the minimal citizen commitment/condition contract and transition results.
2. Replace duplicated availability checks with one aggregate query used by assignment, gathering, expedition, and snapshots.
3. Separate stamina exhaustion behavior from durable health condition semantics.
4. Add tests for exclusivity and save validation before changing UI.

**Exit proof:** one citizen cannot be simultaneously assigned, on expedition, or in recovery; every rejection has a stable reason; v14 migration defaults are valid.

### Phase 1 — Functional city pressure

1. Constrain recruitment by Shelter capacity/opportunity.
2. Add one causal Food demand and shortage explanation, balanced to avoid a first-run lock.
3. Ensure Farm and Quarry blockers expose workers, stamina, stock capacity, and any new required input.
4. Add a city/citizen availability snapshot so UI does not infer rules.

**Exit proof:** moving one of two or three citizens between Farm, Quarry, construction, and rest changes a visible forecast and creates a genuine trade-off.

### Phase 2 — Complete expedition

1. Expand `ExpeditionRequest` into a minimal persisted plan: destination, 1–2 member ids, supply bundle, retreat posture.
2. Add discrete phases and one deterministic encounter resolver.
3. Remove all team members from city availability for the whole expedition.
4. Model outbound, encounter, objective, and return boundaries; no per-frame domain simulation.
5. Replace the one-button panel with team/supply/retreat preparation and a phase/outcome view.

**Exit proof:** a real citizen team leaves, crosses one encounter, reaches or abandons the objective, returns, and produces an explainable result.

### Phase 3 — Consequences and territory

1. Add one persistent wound/condition and Basic Shelter recovery case.
2. Apply work/expedition restrictions and a modest performance consequence.
3. Add minimal parcel/route states and connect expedition result to state advancement.
4. Reveal one new gathering/construction opportunity.
5. Add causal citizen, recovery, and territory events plus return feedback.

**Exit proof:** return forces a city decision between treatment, staffing, resources, and the new opportunity.

### Phase 4 — Persistence and offline closure

Persistence must be developed alongside Phases 0–3; this phase is the integration audit:

1. Bump schema and verify migration from a representative v14 save.
2. Validate all member/commitment/condition/parcel references.
3. Prove save/load at preparation, outbound, encounter boundary, return, injury, recovery, and unlocked state.
4. Prove live/offline equivalence by advancing to discrete boundaries rather than seconds.

**Exit proof:** quitting at every important boundary cannot duplicate rewards, citizens, wounds, reservations, or unlocks.

### Phase 5 — Vertical-slice validation

1. Add a domain integration test covering the whole loop.
2. Add controller/scene seam tests for player-reachable actions.
3. Run a clean save from onboarding through the post-return decision.
4. Quit/relaunch mid-expedition and mid-recovery.
5. Validate 1024×576, 1280×720, and 1600×900 containment plus keyboard/gamepad focus.
6. Add only the provisional audio/visual feedback needed to read dispatch, encounter result, return, wound, treatment, and unlock.

**Exit proof:** the 17 acceptance criteria below pass without editor/debug fixtures.

## 13. Vertical slice acceptance criteria

1. A clean slot completes onboarding and creates exactly one persistent founder `Citizen` with the Hero role.
2. The player gathers Wood and builds Basic Shelter, Farm, and Quarry through normal UI.
3. At least one distinct non-hero citizen arrives through a constrained, explainable recruitment opportunity.
4. Citizens can be assigned and removed from buildings/projects through the UI.
5. Farm and Quarry produce only when their explicit causal requirements pass.
6. At least one consumption/pressure rule forces a staffing or supply decision and cannot silently drain stock.
7. A citizen has one authoritative commitment; incompatible assignment, expedition, and recovery transitions are rejected with visible reasons.
8. The player creates an expedition plan with real `CitizenId`s, supplies, destination, and one retreat posture.
9. The expedition persists through outbound, at least one encounter, objective/abort, and return.
10. Encounter resolution is deterministic from persisted inputs and produces an explainable causal result.
11. Return changes at least two of: resources/cargo, citizen condition/history, route/parcel state.
12. At least one valid result produces a persistent wound or temporary condition that blocks/reduces work and another expedition.
13. Basic Shelter can treat that condition using time plus a visible resource, with no automatic instant reset.
14. One locked route/parcel progresses through explicit states and exposes a new opportunity.
15. The post-return city offers a meaningful choice among treatment, production, construction/new opportunity, and another expedition.
16. Save/load and offline catch-up preserve and resolve all state exactly once at every phase boundary.
17. After recovery/resource preparation, the loop can be repeated without resetting the city or using editor/debug actions.

Non-functional acceptance:

- Domain code imports no Godot APIs.
- No citizen is represented by a permanently active Godot node.
- No offline system iterates wall-clock seconds or presentation frames.
- Every disabled critical action exposes a reason in text, not only color/icon state.
- Build has no errors/warnings, automated tests pass, and the main Godot scene opens without errors.

## 14. Work to postpone

- Deep mechanics for all eight lineages.
- Full profession catalog, skill trees, education, mentorship, ranks, and institutions.
- Multiple biomes, large route graphs, and the complete documented parcel-state vocabulary.
- Complex combat, weapons, armor quality, formations, ability priorities, and multiple encounter types.
- Detailed medicine, doctors, medicines, surgery, rehabilitation, death, and generational systems.
- Political, cultural, environmental, economic, trade, and demographic simulation.
- Full relationship graphs, social synergies, factions, recognition hierarchies, and emergent narrative tooling.
- Final art/audio, full expedition animation list, final building interiors, and custom system icon packs.
- Walkable macro-camera integration and detailed elevation prototypes.
- Massive-population optimization without profiler evidence.
- Full idle/offline simulation for systems not yet part of a player decision.
- Backend, database, network, auth, telemetry, launcher, settings UI, modding, migration tools, or a second city/meta-loop.

## 15. Files and components likely to change

This is a forecast, not authorization to edit every file.

### Existing domain files

- `game/scripts/Domain/Citizen.cs` — commitment/condition/history references; keep one citizen type.
- `game/scripts/Domain/Availability.cs` and `CitizenBehavior.cs` — clarify availability versus behavior; avoid using stamina injury as health.
- `game/scripts/Domain/CitizenAssignmentService.cs` — consume the authoritative availability transition.
- `game/scripts/Domain/CityWorld.cs` — aggregate authorization, new collaborators, territory/recovery orchestration.
- `game/scripts/Domain/Expedition.cs`, `ExpeditionRequest.cs`, `ExpeditionStatus.cs` — plan, team, phases, outcome.
- `game/scripts/Domain/CityParcel.cs` — extensible minimal territorial state.
- `game/scripts/Domain/Recipes.cs`, `BuildingProductionSimulation.cs`, `ProductionStopCause.cs`, `StaminaRules.cs` — one pressure/consumption rule and explanations.
- `game/scripts/Domain/WorldEvent.cs`, `WorldEventLog.cs`, `WorldEventRetention.cs` — new significant event kinds and causal links.
- `game/scripts/Domain/OfflineProgression.cs`, `WorldTimeAdvance.cs` — phase/recovery boundary batching.

### Likely new domain files

- `CitizenCommitment.cs` or an equivalently focused type.
- `CitizenCondition.cs` and `RecoveryPlan.cs`.
- `ExpeditionPlan.cs`, `ExpeditionPhase.cs`, `ExpeditionOutcome.cs`, and `ExpeditionSimulation.cs`.
- `ParcelState.cs` or `RouteState.cs`.

One public type per file should be preserved. Exact names should be chosen during implementation after inspecting adjacent conventions.

### Persistence

- `game/scripts/Domain/Persistence/WorldSave.cs`, `WorldPersistence.cs`.
- `CitizenSave.cs`, `ExpeditionSave.cs`, `ParcelSave.cs`.
- Likely new DTO files for condition/recovery and expedition members/outcome.

### Presentation/application

- `game/scripts/CityWorldController.cs` — thin use-case calls and signals only.
- `game/scripts/ExpeditionPanel.cs` and `game/scenes/Components/ExpeditionPanel.tscn` — preparation, phase, outcome.
- `game/scripts/MigrantPanel.cs` — constrained opportunity/capacity explanation.
- `AssignmentPanel.cs`, `BuildingDetailSnapshot.cs`, `CityMacroSnapshot.cs` — authoritative eligibility and condition display.
- `ProductionPanel.cs`, `CityStatusSnapshot.cs`, `CityStatusPanel.cs` — consumption and blockers.
- `CityMacroView.cs`, `OrthogonalParcelTerrain.cs`, `CityPrototype.tscn` — target parcel, return/unlock feedback, connection only.
- `Ui/WorldEventTextFormatter.cs` and localization catalogs — causal event copy.

### Assets/audio

Only fresh intentional additions should be considered: provisional icons/feedback for expedition phases, wound/recovery, route/parcel state, and the minimal audio events. Existing assets must not be overwritten.

## 16. Required tests

### Commitment and availability

- Assigning, expedition dispatch, and recovery are pairwise exclusive.
- Every transition releases or preserves reservations/assignments atomically on failure.
- Availability snapshot and domain authorization return the same reason.
- Migrant capacity/recruitment opportunity prevents unlimited creation and cannot soft-lock the initial city.

### Production and consumption

- Farm/Quarry stop causes identify no workers, exhaustion, storage full, missing input, and shortage.
- Consumption cannot partially debit a failed batch.
- Live and offline results match across day/night and shortage boundaries.
- Moving one citizen changes production and opportunity availability exactly once.

### Expedition

- Plan rejects missing/duplicate/unavailable members and insufficient supplies.
- Dispatch reserves supplies and commits/releases them exactly once.
- Phase transitions follow the legal order, including retreat and return.
- Encounter result is deterministic for the same plan/world state.
- A team member cannot work/gather/recover while away.
- Return applies cargo, condition, citizen history, and parcel/route outcome atomically.
- Cancel-before-departure differs from retreat-after-encounter.

### Health and recovery

- Wounds persist independently from stamina/behavior.
- A wound blocks or reduces the intended actions.
- Recovery requires Shelter capacity, time, and resource; no instant restoration occurs.
- Recovery continues equivalently live/offline and records causal events.
- Save/load mid-recovery does not consume twice or complete twice.

### Territory

- Only legal parcel/route transitions succeed.
- Expedition outcome advances exactly the intended target.
- Unlock makes a real lot/resource/opportunity available and survives save/load.
- Migration maps v14 `IsUnlocked` safely into the new state.

### Persistence and integration

- New schema round-trips every cross-entity relationship.
- Validation rejects duplicate members, missing citizens, conflicting commitments, orphan conditions/reservations, and illegal parcel states.
- Representative v14 saves migrate and retain the current city.
- Save/reload at every expedition phase produces no duplicate reward/event/unlock.
- One end-to-end domain test covers onboarding-created founder state through the second post-return decision.
- One main-scene seam test proves all required actions are reachable without visual-regression/debug fixtures.

## 17. Open questions that truly block development

Only four choices need product approval before implementation; the broader bible questions do not block this slice.

1. **First city pressure:** should the minimum recurring pressure be resident Food consumption, expedition supplies, or both? Recommendation: small resident Food consumption plus explicit expedition/recovery Food bundles, tuned so Farm recovery is possible before failure.
2. **First encounter fantasy:** environmental obstacle, hostile creature, or social contact? Recommendation: environmental obstacle/threat. It exercises exploration, condition, retreat, and route security without prematurely defining full combat.
3. **First territorial reward:** new Wood patch, Quarry access, or one buildable parcel? Recommendation: secure one adjacent parcel containing a visible resource opportunity; this makes territory and infrastructure interact immediately.
4. **First wound consequence:** fully unavailable or reduced performance? Recommendation: one wound with “cannot expedition; reduced work output,” recoverable at Shelter. Full unavailability is clearer but risks a two-citizen soft lock.

Team size, exact numbers, names, and final visuals can be tuned inside these decisions and do not justify blocking architecture work.

## 18. Final recommendation

The project is **halfway to the first complete loop**. Its systems are not merely disconnected: the first half is real, integrated, deterministic, persisted, and well tested. The second half—expedition, consequences, territory, and the resulting new decision—is still a connected placeholder. Calling the project “close” would understate the product/design work remaining; calling it “only disconnected prototypes” would ignore the strong vertical foundation already operating in the main scene.

The recommended implementation order is:

1. Authoritative citizen commitment/condition invariant.
2. Minimal city pressure and constrained recruitment.
3. Persisted expedition plan/team/phases.
4. One deterministic encounter and return leg.
5. One wound plus Shelter recovery.
6. One route/parcel state chain and concrete unlock.
7. Save/offline integration at every step.
8. End-to-end slice validation and minimal feedback.

**First concrete technical task after audit approval:** implement and test the authoritative citizen commitment/availability model, including a v14-compatible persistence default and stable rejection reasons, then route existing assignment, gathering, and expedition dispatch through it without changing current player-visible behavior. This is the smallest safe step that unlocks team expeditions and recovery while reducing—not increasing—architectural duplication.
