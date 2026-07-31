# Current Project Status

**Last aligned:** 2026-07-30

**Active vertical slice:** VS-5 — player-facing signature and repetition

**Next approved work:** EG-0 is implemented (schema v20); the remaining VS-5
diagnostic run now also produces the EG-0 report. Then EG-3 (Food horizon), the
only increment that closes G1, and then the VS-5 signature. EG-1/EG-2/EG-4/EG-5
follow as their own slice — running them first would rewrite the opening and
reopen acceptance criteria 1-5, which the human run has already signed. See
`EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §15.

The design bible defines what the game is. This file defines what the connected
code does today. `FIRST_PLAYABLE_LOOP_AUDIT.md` owns the 17 VS-5 acceptance
criteria; `TO_DO.md` owns the actionable queue.

## 1. Verified baseline

- Godot .NET 4.7.1, C#/.NET 8.
- `dotnet build`: 0 errors, 0 warnings.
- `dotnet test`: 652/653 passing (1 omitido por brittleness del JSON snapshot en
  `VerticalLoopPersistenceTests.Recovery_ReloadedHalfway`; el comportamiento no
  cambió, sólo los IDs auto-incrementados de eventos difieren desde que el
  workday se desplazó a 08:00).
- `WorldSave.CurrentVersion`: 20 (EG-0 opening measurement).
- Godot headless boot loads the current scene/slot without C# or scene errors.
- EN/ES catalogs: 677 template IDs and 303 runtime keys validated.
- Agent-context validation: 436 checks passing.
- Official visual review sizes: 1280×720 and 1920×1080.

## 2. Active proof

The first playable loop is implemented in code:

```text
onboarding → gathering → construction → constrained recruitment
→ named assignments and Food pressure → configured expedition
→ deterministic encounter/objective or retreat/return
→ wound and territory consequence → treatment/new decision
→ save/load → second cycle without reset
```

The clean-slot human run has signed founder creation, gathering, Shelter/Farm/
Quarry/Town Hall construction, constrained recruitment, multi-citizen work,
navigation/entry, production and UI wheel isolation. It also found that 60 Food
against a two-Food daily ration does not create meaningful pressure, reopening
G1. The 2026-07-30 follow-up corrected the Home detail panel so the
"descansando N / capacidad M" line and the worker slots read the same source
(`VisibleCitizens`), and added a hiding rule for non-founder citizens at
home so closing the Shelter detail view no longer leaves every sleeping
citizen visible at the building's entrance anchor (see `TO_DO.md` 2026-07-30).
VS-5 still needs:

1. One complete normal-UI run from a clean slot.
2. A visible relaunch during an expedition.
3. A visible relaunch during treatment.
4. EG-0+ correction and revalidation of daily Food pressure.
5. A second player-facing cycle without reset/debug actions.
6. 1280×720 and 1920×1080 containment plus keyboard/gamepad focus signature
   for the surfaces exercised by the loop.

No broader product slice is approved until all 17 audit criteria pass.

## 3. Connected functionality

### Founder and citizens

- Twelve-fragment astral onboarding, hidden scoring, explicit name/body choice,
  lineage reveal, founder arrival and profile.
- Exactly one sealed `Citizen` person entity. Heroism, profession, competence,
  work, expedition and health are attached state, not subclasses.
- Deterministic recruited-citizen identity/profile and selectable Citizens
  roster.
- Authoritative `Citizen.Commitment` across work, construction, expedition and
  recovery; durable `Citizen.WorkOrder` survives temporary interruption.
- Stamina/food/rest cycle plus one persistent moderate wound and treatment.
- Semantic routine projection: activity, contextual location, blocker, origin,
  destination and transition timing. No authoritative visual coordinates are
  persisted.

### City, construction and economy

- Natural resource patches provide gathered Wood and occupy persisted parcel
  lots.
- Placeable Basic Shelter, Farm, Quarry and Town Hall construction projects.
- Atomic recipe deposit/drawdown, pause/resume, contributor assignment and
  completion preserving placement identity.
- Farm/Quarry production uses workers, schedule, stamina, competency, storage
  and min/max policy. Ten-tick production cadence and visible stop causes.
- Daily Food ration per resident plus Food-funded stamina recovery and wound
  treatment. Shortage is causal and visible; it does not silently kill/delete.
- Town Hall hosts at most one expedition prospect. Acceptance requires housing
  capacity; the prospect persists but cannot work before acceptance.

### Expedition, health and territory

- Persisted team of 1–2 real citizen IDs, supply reservation, destination and
  retreat posture.
- Outbound → Encounter → Objective or Retreating → Returning → Resolved.
- Deterministic encounter from persisted team condition/competence/supplies.
- Exact-once supply and reward resolution, causal Chronicle and member release.
- Moderate wound independently persists from stamina, limits effective stamina
  and blocks another expedition.
- Basic Shelter treatment costs one Food and 3600 ticks.
- One target parcel advances Locked → Reconnoitred → RouteSecured → Available
  and exposes a construction lot.

### Persistence and offline simulation

- Atomic JSON snapshot, temporary write, `.bak`, structural validation and
  explicit migration chain from v2 through v19.
- Citizens, prospects, buildings/projects, resources/reservations, expedition,
  wounds/treatment, territory and significant causal events round-trip.
- Offline progression runs before visual instantiation and uses the same domain
  transitions as live advancement.
- Integration tests reload every expedition boundary and halfway through
  treatment; resolution/debits/events occur exactly once.
- A second expedition after recovery and save/reload is covered without reset.
- Dirty-aware autosave runs every three real minutes and on close/pause when
  needed; explicit saves remain possible through existing actions.

### Presentation and input

- `MacroStreetLiveView` is the only runtime macro-city representation.
- Building/project/natural-resource positions derive from persisted parcel/lot
  identity; citizen visuals derive from semantic context and building anchors.
- Founder and recruited citizens use the same visual travel system. Mid-transit
  load reconstructs elapsed progress rather than replaying from the origin.
- Idle/wait citizens may wander through existing pathfinding without mutating
  domain location.
- Buildings provide derived entrance, exit, work, waiting and leisure anchors.
- Free camera by default. Selection does not enable follow; explicit toggle/F
  follows the selected citizen. WASD/arrows only pan the camera and manual pan
  releases follow.
- Scrollable UI owns wheel input even at its first/last row; the world does not
  zoom through panels.
- Policies exposes provisional 00:00–16:00 workday, production, off-duty and
  construction rules.
- HUD carries immediate time/resources/alerts; save confirmation is temporary.
- Chronicle compacts routine events and preserves causal links for significant
  results/blockers.
- Localization is native PO-based EN/ES with hot switching and validation.

## 4. Partial or provisional systems

- VS-1 recruitment and Food pressure need player-facing calibration/signature.
- Farm and Quarry have no operating-material recipes; labor, schedule, stamina,
  storage and policy are their current running constraints.
- Production priority is persisted but remains a future scheduling hint.
- Storage is per-building plus city inventory/reservations; no complete global
  capacity/cargo/logistics model exists.
- Assigned-work offline catch-up still steps ticks; recovery and expedition use
  semantic boundaries.
- Expedition presentation is planning/status UI, not a side-view journey.
- `ExpeditionPanel` should receive a dedicated snapshot before adding another
  dimension.
- Event history retains at most 128 significant events; pinned causal origins
  need scale review before mass wounds/population.
- Workday hours and travel duration are provisional tuning.
- Lineages remain qualitative metadata without mechanics.
- Recruitment opportunity, housing numbers and all first-loop costs are
  provisional until VS-5 play calibration.

## 5. Placeholder presentation

- Forest/natural-resource and Town Hall art remain provisional.
- Smithy/PotionLab have no connected playable slice or final art.
- Expedition art, formation/equipment visuals and richer territory feedback are
  absent.
- Detailed citizens use the licensed/attributed LPC lineage set; macro movement
  remains a deliberately small representation.
- No audio buses, streams or final causal sound pack are wired.
- Some resource/system icons remain graphical debt.

## 6. Closed gaps and active signature

| ID | Status |
| --- | --- |
| G0 commitment/condition | Implemented; VS-5 UI signature pending. |
| G1 city pressure | Reopened by human run: 60 Food vs 2/day created no decision. |
| G2 constrained recruitment | Town Hall/prospect/housing first cut; VS-5 signature pending. |
| G3 expedition plan/team | Closed in VS-2. |
| G4 phases/encounter/retreat/return | Closed in VS-2. |
| G5 wound/recovery | Closed in VS-3. |
| G6 territory/unlock | Closed in VS-3. |
| G7 persistence/offline equivalence | Closed in VS-4. |

The active slice remains the VS-5 diagnostic. G1 is reopened by observed balance
failure; the approved follow-up is the bounded EG-0 sequence in
`EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`, followed by a fresh VS-5
signature.

## 7. Known debt that does not block VS-5

- Confirm live pathfinding through tree rows and gather visibility in the street
  perspective.
- Reconcile domain footprints/corridor vocabulary with the live street/navmesh
  model before expanding territory navigation.
- Add one operating input→output chain before generalizing the economy.
- Add a dedicated expedition snapshot before expanding its UI/state surface.
- Complete large-event feedback and overlay exclusion only where a real VS-5
  interaction requires it.
- Defer MultiMesh until more than 20–25 citizens are visible or profiler data
  justifies it.
- Defer dialogue UI/content until a real conversational NPC is approved.

## 8. Out of scope

- Backend, database, server, API, CDN, auth, telemetry or networking.
- Mobile, multiplayer, accounts, launcher, installer or full settings UI.
- Second city, meta-progression, restart bonus or second gameplay loop.
- Full combat, equipment, formations, mortality and generations.
- Deep politics, culture, environment, trade, economy and demographics.
- Full profession/education/institution/relationship systems.
- Multiple biomes/large route graph and final art/audio.
- Massive-population optimization without profiler evidence.

## 9. Verification commands

From `C:\dev\world-of-goses`:

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

Headless boot:

```powershell
C:\Tools\Godot\Godot_v4.7.1-stable_mono_win64.exe `
  --headless --path game --quit-after 3
```

VS-5 additionally requires the normal-UI/manual procedure in
`FIRST_PLAYABLE_LOOP_AUDIT.md`; headless boot is not a substitute.

## 10. Current file map

- Domain: `game/scripts/Domain/`
- Persistence: `game/scripts/Domain/Persistence/`
- Controller: `game/scripts/CityWorldController.cs`
- Main scene: `game/scenes/CityPrototype.tscn`
- Macro city/navigation: `game/scripts/Prototypes/MacroStreetLiveView.cs`
- Construction/building UI: `game/scripts/ConstructionPanel.cs`,
  `game/scripts/BuildingDetailView.cs`
- Citizens: `game/scripts/MigrantPanel.cs`, citizen snapshots/routines
- Expedition: `game/scripts/ExpeditionPanel.cs`, expedition domain files
- Chronicle: `game/scripts/OfflineReportPanel.cs`, domain event log
- Policies: `game/scripts/PoliciesPanel.cs`
- Reusable UI/input: `game/scripts/Ui/`
- Tests: `tests/WorldofGoses.Tests/`
- Active backlog: `TO_DO.md`
- Closure contract: `docs/FIRST_PLAYABLE_LOOP_AUDIT.md`
