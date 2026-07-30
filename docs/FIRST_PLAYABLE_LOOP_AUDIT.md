# First Playable Loop Audit

**Last aligned:** 2026-07-29

**Active slice:** VS-5 — player-facing signature and repetition

**Verified baseline:** clean build, 553/553 tests, save schema v19, successful
Godot 4.7.1 headless boot.

## 1. Executive summary

The first playable loop is implemented end to end in domain code and connected
presentation. It is no longer halfway through the original audit plan:
expedition planning, deterministic encounter resolution, retreat, return,
persistent wounds, Shelter treatment, territory advancement, save/load and a
second expedition without reset all have automated coverage.

The loop is not signed yet. VS-5 still requires one complete player-facing run
from a clean slot plus visible relaunches during an expedition and during
treatment. That run must confirm that recruitment and Food pressure create a
legible choice, that every critical blocker is explained, and that no step
requires editor/debug fixtures.

The current proof is:

```text
founder onboarding and arrival
→ gather Wood and build Shelter/Farm/Quarry
→ build Town Hall and obtain one expedition prospect
→ accept a distinct citizen under housing capacity
→ assign named citizens under Food pressure
→ select 1–2 real expedition members, supplies and retreat posture
→ outbound → encounter → objective or retreat → return
→ resource, wound and territory consequences
→ Shelter treatment or another city priority
→ save/reload and repeat without resetting the city
```

No broader content is approved until this flow is signed.

## 2. Current connected implementation

### Citizens and city commitments

- `Citizen` remains the only person entity.
- Roles, competencies, wounds, work orders and commitments are composed onto
  that entity; founder and recruited citizens use the same rules.
- `Citizen.Commitment` is the mutually exclusive active responsibility across
  work, construction, expedition and recovery.
- `Citizen.WorkOrder` preserves player-authorized employment while execution is
  suspended by schedule, stamina, storage, expedition or recovery.
- Semantic routine projection exposes activity, contextual location, blocker,
  transition timing, workplace and Shelter without persisting screen
  coordinates.

### City economy and recruitment

- Natural resource patches provide gathered Wood.
- Shelter, Farm, Quarry and Town Hall use placed construction projects and
  causal material drawdown.
- Farm and Quarry require authorized workers, schedule, stamina, available
  storage and production policy. They currently have no operating-material
  recipe; this remains a later economy-deepening slice.
- Each resident creates one daily Food ration demand. Shortage is explicit and
  does not silently kill or delete citizens.
- Recruitment is no longer free or unlimited. A Town Hall may host at most one
  prospect found by expedition; accepting the prospect requires housing
  capacity. The prospect is persisted but is not a worker until accepted.

### Expedition, consequence and territory

- A plan contains 1–2 real citizen IDs, a destination, reserved supplies and a
  retreat posture.
- The persisted phase sequence is Outbound → Encounter → Objective or
  Retreating → Returning → Resolved.
- Encounter outcome is deterministic from persisted inputs and resolves once.
- Return releases members and reservations exactly once, applies the causal
  resource outcome and can create a persistent moderate wound.
- A wound is independent from stamina, limits effective stamina, blocks another
  expedition and requires Basic Shelter, one Food and 3600 ticks to treat.
- The first target parcel advances through Locked → Reconnoitred → RouteSecured
  → Available and exposes a real construction opportunity.

### Persistence and presentation

- Schema v19 persists commitments, work orders, expedition team/phase/outcome,
  retreat posture, wound/treatment, territory, prospects and causal events.
- Live and offline advancement share domain rules; phase-boundary and
  mid-treatment reload tests prove exact-once resolution.
- Offline progression runs before visual reconstruction. Citizen visuals derive
  positions from semantic context and building anchors.
- Camera selection and follow are separate; WASD/arrows control only the
  camera. Scrollable UI owns wheel input even at its scroll limits.
- Policies exposes the provisional workday and automation rules. Autosave is
  dirty-aware, runs every three real minutes and reports completion briefly.

## 3. Gap status

| ID | Gap | Current status | Remaining proof |
| --- | --- | --- | --- |
| G0 | Authoritative citizen commitment/condition | Implemented | Human multi-citizen exclusivity/blocker signature in VS-5. |
| G1 | Meaningful city pressure | Reopened by VS-5 run | Farm reached 60 Food while two residents consumed 2/day; correct through EG-0+ and rerun. |
| G2 | Constrained recruitment | Implemented first cut | Human Town Hall → prospect → housing acceptance flow. |
| G3 | Expedition plan and team | Closed in VS-2 | VS-5 visual signature only. |
| G4 | Expedition phases, encounter and retreat | Closed in VS-2 | VS-5 visual signature only. |
| G5 | Persistent wound and Shelter recovery | Closed in VS-3 | Human mid-treatment relaunch. |
| G6 | Territorial state and unlock | Closed in VS-3 | Confirm opportunity is legible in the full run. |
| G7 | Snapshot and offline equivalence | Closed in VS-4 | Human relaunch mid-expedition and mid-treatment. |

G0 and G2 need only the remaining player-facing evidence. G1 now requires the
bounded early-game resource/agriculture work proposed in
`EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`; changing the ration alone
would hide the structural abundance problem.

## 4. System matrix

| System | Status | Honest remaining boundary |
| --- | --- | --- |
| Onboarding/founder | Functional | No loop blocker. |
| Natural gathering | Functional | Broader gathering jobs/competence later. |
| Construction/placement | Functional | Rich knowledge/institution gates later. |
| Shelter | Functional | One treatment case; staffing/medicine later. |
| Farm/Quarry | Functional first economy | No operating-material chain yet. |
| Citizens/assignments | Functional | Human multi-citizen signature pending. |
| Recruitment | Functional first cut | Balance and normal-flow signature pending. |
| Food pressure | Functional first cut | Calibration pending. |
| Storage | Partial | Per-building capacity; no global cargo/capacity model. |
| Expedition | Functional VS-2 | Equipment, formations and multiple encounters later. |
| Encounter/retreat/return | Functional VS-2 | Full combat remains out of scope. |
| Wound/recovery | Functional VS-3 | Deeper healthcare remains out of scope. |
| Territory | Functional VS-3 | One target only; broader routes/biomes later. |
| Persistence/offline | Functional VS-4 | Assigned-work catch-up still steps ticks. |
| Chronicle/blockers | Functional for slice | Long-horizon persisted history later. |
| Camera/navigation | Functional first cut | Human pathfinding/visibility confirmation remains. |
| Audio | Missing | Does not block logic; minimal causal feedback is optional for signature. |
| Final art | Placeholder/partial | Forest/Town Hall/expedition presentation remains provisional. |

## 5. Remaining VS-5 procedure

Run the following through normal UI on a clean slot:

1. Complete onboarding and confirm exactly one persistent founder.
2. Gather Wood and place/build Basic Shelter, Farm and Quarry.
3. Build Town Hall, dispatch the prospect expedition and accept the prospect
   with free housing.
4. Assign and remove citizens from work/construction; inspect unavailable
   reasons and visible travel.
5. Advance several days and assess whether daily Food demand creates a real but
   recoverable staffing/supply decision.
6. Select 1–2 expedition members, supplies and retreat posture.
7. Observe departure, encounter, objective or retreat, returning and summary.
8. Confirm at least two consequence axes change: resources, citizen condition
   or territory.
9. Treat the wound or deliberately prioritize another city action.
10. Quit/relaunch during an expedition and confirm one resolution.
11. Quit/relaunch during treatment and confirm one Food debit/completion.
12. Prepare a second expedition without reset or debug tools.
13. Sign 1280×720 and 1920×1080 containment and the keyboard/gamepad paths used
    by the flow.

Record real elapsed time, blockers, unclear copy, soft locks and any divergence
between live and offline behavior.

### VS-5 run evidence — 2026-07-29

The in-progress clean-slot normal-UI run has confirmed:

- one persisted founder `Citizen` with Hero role;
- selection does not enable camera follow and WASD/arrows move only the camera;
- repeated Wood gathering shows the full walk/action/result without teleport;
- Basic Shelter, Farm and Quarry use visible placement, assignment, travel,
  entry, active work and completion transitions;
- the former Quarry-door freeze did not reproduce and Stone production began;
- scrollable construction UI owns wheel input at both limits without zooming
  the city behind it.

The run found one pacing blocker before dispatch: both first-loop expedition
templates advertised four simulated days, equal to four real hours at 1x or one
hour at 4x. This prevented a practical normal-UI signature. The templates were
calibrated to four simulated hours (600 ticks): ten real minutes at 1x or two
and a half minutes at 4x. The outbound/encounter/objective-or-retreat/return
phase chain, supplies, consequences and persistence rules are unchanged.

The same run accepted `Inara`, assigned two named citizens, and observed Farm
and Quarry reach 60/60 and 80/80. At the next snapshot both retained their work
orders while waiting at Home because storage was full. This was behaviorally
coherent, but Food demand was not: two residents consumed only 2 Food/day
against a full 60-Food Farm, so criterion 6 failed and G1 reopened.

**Resume checkpoint:** unassign the founder from Quarry, gather 2 Wood, pause,
dispatch `Reconnaissance`, and relaunch while its persisted phase is still
`Outbound`. Then finish the wound/treatment relaunch and repetition checks.

## 6. Vertical-slice acceptance criteria

| # | Criterion | Automated state | VS-5 human state |
| ---: | --- | --- | --- |
| 1 | Clean slot creates one persistent founder `Citizen` with Hero role. | Covered | Include in run. |
| 2 | Wood gathering builds Shelter, Farm and Quarry through normal UI. | Covered | Include in run. |
| 3 | A distinct non-hero arrives through constrained recruitment. | Covered | Pending signature. |
| 4 | Citizens are assigned/removed through UI. | Covered | Multi-citizen signature pending. |
| 5 | Farm/Quarry produce only when causal requirements pass. | Covered | Observe blockers. |
| 6 | Consumption/pressure forces a visible staffing or supply decision. | Covered structurally | Balance/signature pending. |
| 7 | One authoritative commitment rejects incompatible transitions visibly. | Covered | Pending UI signature. |
| 8 | Expedition plan uses real citizens, supplies, destination and retreat. | Covered | Pending UI signature. |
| 9 | Expedition persists through outbound, encounter, objective/abort and return. | Covered | Pending visible run/relaunch. |
| 10 | Encounter is deterministic and causally explained. | Covered | Confirm copy. |
| 11 | Return changes at least two consequence axes. | Covered | Confirm visible result. |
| 12 | One result produces a persistent restrictive wound/condition. | Covered | Confirm visible result. |
| 13 | Shelter treatment uses visible resource plus time; no instant reset. | Covered | Pending visible run/relaunch. |
| 14 | Locked territory advances through explicit states and exposes an opportunity. | Covered | Confirm legibility. |
| 15 | Post-return city offers treatment/production/expansion/expedition choices. | Covered structurally | Pending player signature. |
| 16 | Save/load and offline catch-up resolve every boundary exactly once. | Covered | Human relaunches pending. |
| 17 | The loop repeats without reset or editor/debug actions. | Covered | Human repetition pending. |

Non-functional requirements:

- Domain code imports no Godot API.
- No citizen requires a permanently active visual node to simulate.
- Offline progression does not depend on render, animation or pathfinding.
- Every disabled critical action explains its reason in text.
- Build is clean, tests pass, localization validates and the main scene boots.
- UI wheel/keyboard/pointer input does not leak into the city behind an active
  control.

## 7. Current technical and product debt

These items do not block VS-5 unless the human run reproduces them:

1. Assigned-work offline catch-up still steps simulation ticks; future work
   should batch to semantic boundaries without changing outcomes.
2. Farm and Quarry have no operating-material recipes. Add one understandable
   input/output chain before designing a generic economy.
3. Per-building storage and city inventory are not a complete cargo/logistics
   model.
4. `ExpeditionPanel` still reads more aggregate state than the preferred
   snapshot boundary; extract a snapshot before adding another dimension.
5. Event retention is bounded to 128 significant events. Revisit pinned causal
   origins before scaling wounds/population.
6. Workday hours and travel duration remain provisional tuning values.
7. Forest, Town Hall and expedition visuals remain provisional; system icons
   still have graphical debt.
8. The current pathfinding correction between tree rows needs human confirmation
   in the live perspective.

## 8. Work explicitly postponed

- Deep mechanics for all lineages and professions.
- Education, mentorship, institutions and relationship graphs.
- Multiple biomes, large route networks and complete territory vocabulary.
- Combat engine, equipment quality, formations, abilities and encounter sets.
- Detailed medicine, staff, surgery, rehabilitation, mortality and generations.
- Political, cultural, environmental, trade and demographic simulation.
- Final art/audio and full expedition animation.
- Massive-population optimization without profiler evidence.
- Backend, networking, auth, telemetry, launcher, settings UI, modding or a
  second city/meta-loop.

## 9. Required evidence

Automated regression names that anchor the closure include:

- `VerticalLoopPersistenceTests.Expedition_ReloadedAtEveryPhaseBoundary_ResolvesExactlyOnce`
- `VerticalLoopPersistenceTests.Recovery_ReloadedHalfway_ConsumesAndCompletesExactlyOnce`
- `VerticalSliceRepetitionTests.RecoveredCity_CanCompleteSecondExpeditionWithoutReset`
- `CitizenRoutineTests`
- `ExpeditionTeamTests`
- `ExpeditionEncounterTests`
- `WoundRecoveryTests`
- `TerritoryProgressionTests`
- `FirstRunRegressionTests`

Before declaring VS-5 closed:

```powershell
cd game
dotnet build

cd ../tests/WorldofGoses.Tests
dotnet test

cd ../..
pwsh ./tools/Test-LocalizationCatalog.ps1
pwsh ./scripts/Sync-AgentContext.ps1 -Apply
pwsh ./scripts/Validate-AgentContext.ps1
```

Also run the Godot headless boot and the player-facing/manual procedure in §5.

## 10. Final recommendation

Do not add a new building family, combat system, skill tree, NPC dialogue or
population-scale optimization next. Complete VS-5, record any real blocker,
fix only what the run exposes, and sign or reopen G0–G2 with evidence. Once all
17 criteria pass, select one bounded depth slice from the cleaned `TO_DO.md`.
