# Current Project Status

**Last aligned:** 2026-07-31

**Active increment:** EG-2 — founding site seam

**Next approved work:** EG-2 introduce el Founding Site 3×3 con módulos
Campfire → Bedroll/Cache → Canopy, sobre las bases de EG-1. El orden
canónico es el de `EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §15:
EG-0 → EG-1 → EG-2 → EG-3 → EG-4 → EG-5 → EG-6. La aperture del antiguo
VS-5 (17 criterios) se descartó el 2026-07-31: el proyecto aún no tiene las
dos capas de complejidad (Founding Site + plot lifecycle + resource seam)
que pide el proposal antes de hablar de herida, tratamiento y territorios
desbloqueables.

The design bible defines what the game is. This file defines what the connected
code does today. `EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` owns the
acceptance test; `TO_DO.md` owns the actionable queue.

## 1. Verified baseline

- Godot .NET 4.7.1, C#/.NET 8.
- `dotnet build`: 0 errors, 0 warnings.
- `dotnet test`: 663/664 passing (1 omitido por brittleness del JSON snapshot en
  `VerticalLoopPersistenceTests.Recovery_ReloadedHalfway`; el comportamiento no
  cambió, sólo los IDs auto-incrementados de eventos difieren desde que el
  workday se desplazó a 08:00).
- `WorldSave.CurrentVersion`: 21 (EG-1 resource seam). EG-0 still serialises its
  measurement; EG-1 bumps the schema to add Branches/PlantFiber/SmallStone
  and WildFood without inventing any on legacy saves.
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
against a two-Food daily ration does not create meaningful pressure. The
2026-07-30 follow-up corrected the Home detail panel so the
"descansando N / capacidad M" line and the worker slots read the same source
(`VisibleCitizens`), and added a hiding rule for non-founder citizens at
home so closing the Shelter detail view no longer leaves every sleeping
citizen visible at the building's entrance anchor (see `TO_DO.md` 2026-07-30).
The same playtest informed the G1 diagnosis that motivates EG-3; that
work is now sequenced under the proposal §15.
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
- Policies exposes provisional 08:00–16:00 workday, production, off-duty and
  construction rules; the configured workday is suspended before the first
  Basic Shelter exists so the founder can build the founding camp at any
  time of day (founding-camp bypass).
- ESC closes overlays iteratively: ModalHost (topmost modal first), then
  PauseMenu (close only — open path is the dedicated button), then the
  hero profile / building detail view via
  `CityWorldController.ReturnToCity()` from `CityPrototype._UnhandledInput`.
  `ModalHost.CompleteClose` no longer throws when its content is freed
  mid-animation.
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
- Workday hours and travel duration are provisional tuning; the
  founding-camp bypass keeps solo-survival construction going outside the
  08:00–16:00 window until the first Basic Shelter registers.
- Lineages remain qualitative metadata without mechanics.
- Recruitment opportunity, housing numbers and all first-loop costs are
  provisional until EG-6 (calibration/signature).

## 5. Placeholder presentation

- Forest/natural-resource and Town Hall art remain provisional.
- Smithy/PotionLab have no connected playable slice or final art.
- Expedition art, formation/equipment visuals and richer territory feedback are
  absent.
- Detailed citizens use the licensed/attributed LPC lineage set; macro movement
  remains a deliberately small representation.
- No audio buses, streams or final causal sound pack are wired.
- Some resource/system icons remain graphical debt.

## 6. Closed implementation islands and active increments

Las primitivas que ya están cerradas en código y pueden reutilizarse en los
increments EG-*:

| Sistema | Estado | Comentario |
| --- | --- | --- |
| Onboarding / founder | Funcional | `AstralOnboardingView` produce un único `Citizen` persistente con rol `hero`. |
| Commitment exclusivo | Funcional | `Citizen.Commitment` rechaza transiciones incompatibles visiblemente. |
| Construcción / proyectos | Funcional | `ConstructionProject` + deposit + remainder; phased progress. |
| Persistencia offline | Funcional | Schema v20 con EG-0; reload exacto por fase y por frontera offline. |
| Citizens y asignaciones | Funcional | `CitizenRoutine` cubre work, expedition, recovery. |
| Recruitment | Funcional | Town Hall + prospect + vivienda. Wound/territory del VS-3 se conservan en código pero se difieren hasta EG-5. |

Los gaps del antiguo VS-5 se reformularon dentro del proposal. La abundancia
de Food sin receta de insumo (G1) la cierra EG-3; el territorio legible se
mantiene vivo (Parcel 9 ahora `Available`) pero la herida persistente y el
tratamiento se difieren hasta que EG-2 + EG-3 + EG-5 estén en pie.

## 7. Known debt that does not block EG-2

- Confirm live pathfinding through tree rows and gather visibility in the street
  perspective.
- Reconcile domain footprints/corridor vocabulary with the live street/navmesh
  model before expanding territory navigation.
- Add one operating input→output chain before generalizing the economy.
- Add a dedicated expedition snapshot before expanding its UI/state surface.
- Complete large-event feedback and overlay exclusion only where a real EG-1/EG-2
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

La acceptance test del proposal (§17) requerirá un playtest de la apertura
EG-A0 en un slot limpio nuevo; headless boot no es sustituto. La matriz
visual (`docs/VISUAL_REGRESSION.md`) sigue siendo el contrato transversal
de cualquier cambio de UI.

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
- Closure contract: `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`
  §17 (acceptance test del proposal).
