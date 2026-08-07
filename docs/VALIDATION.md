# Validation against the design bible

**Last aligned:** 2026-07-30

**Code baseline:** clean build, 586/586 tests, schema v19, successful Godot
headless boot.

**Active proof:** VS-5 player-facing signature and repetition.

This document is the current cross-check between the connected prototype and
the design bible. It does not replace `CURRENT_STATUS.md` for implementation
facts or `FIRST_PLAYABLE_LOOP_AUDIT.md` for the 17 closure criteria.

Markers: ✅ implemented · ⚠️ partial/first cut · ❌ absent/deferred.

## 1. Vision alignment

| Vision constraint | Status | Current evidence |
| --- | --- | --- |
| One persistent city, no restart bonus | ✅ | One `CityWorld`, primary local slot, no meta-progression. |
| Absence is not punished artificially | ✅ | Offline catch-up advances authorized simulation up to the documented cap. |
| No sovereign decisions without authorization | ✅ | Assignments, construction, production, recruitment acceptance, expedition and treatment use explicit commands/policies. |
| Citizens create decisions and history | ✅ first vertical loop | Named citizens carry commitments, competence, wounds and expedition consequences. |
| Causality over unexplained randomness | ✅ | Deterministic production/encounter, typed blockers and causal Chronicle events. |
| Domain is not presentation | ✅ | `DomainBoundaryTests`; simulation does not depend on nodes, animation or camera. |
| No instant general healing | ✅ first case | Moderate wound requires Shelter, Food and time. |
| No arbitrary overall level/unlock | ✅ | Territory and buildings depend on resources, placement, expedition result and authorization. |
| Lineage is not profession/class | ✅ | Qualitative profile metadata; no permanent production multiplier. |

The current implementation reinforces city development, automated expeditions,
citizen trajectory, causal production, territory, health and delegation. The
environmental, institutional and organic-difficulty pillars remain much broader
than the prototype.

## 2. First playable loop

| Slice | Status | Remaining evidence |
| --- | --- | --- |
| VS-0 causal city | ✅ | Keep as regression target. |
| VS-1 recruitment/pressure | ⚠️ implemented first cut | Human flow and Food calibration. |
| VS-2 expedition | ✅ | Human presentation signature. |
| VS-3 consequence/territory | ✅ | Human presentation signature. |
| VS-4 persistence/offline | ✅ | Human relaunches at two boundaries. |
| VS-5 signature/repetition | ⚠️ active | Full normal-UI run and second cycle without reset/debug. |

The domain already proves two expeditions with the same founder and a reload
between cycles. This is not sufficient to close VS-5 because discoverability,
layout, focus, copy and real relaunch behavior must also be observed.

## 3. Architecture compliance

| Area | Status |
| --- | --- |
| Domain contains no Godot imports/resource paths | ✅ enforced |
| One sealed `Citizen` person entity | ✅ |
| Visual node is representation, not persisted citizen | ✅ |
| Semantic citizen persistence; no authoritative visual coordinates | ✅ |
| Versioned validated snapshots and explicit migrations | ✅ v2→v19 |
| Atomic local save with `.bak` | ✅ |
| Dirty-aware three-minute autosave and close-save | ✅ |
| Live/offline share domain transitions | ✅ for current loop |
| Assigned-work catch-up batched to semantic boundaries | ⚠️ still tick-stepped |
| UI consumes snapshots consistently | ⚠️ most surfaces; Expedition remains debt |
| Causal long-horizon persisted history | ⚠️ bounded 128-event log |

## 4. Pillar status

### City development

⚠️ The bootstrap is real: natural Wood → construction → Farm/Quarry/Town Hall,
named workers, per-building storage and a daily Food demand. Missing depth:

- One operating input/output chain.
- Shared cargo/logistics and global capacity rules.
- Knowledge/institution gates.
- Broader environmental and cultural consequences.

### Automated expeditions

✅ The minimal seam exists: real members, supplies, destination, retreat policy,
deterministic encounter, objective/retreat, return and causal outcome. Equipment,
formations, combat abilities, multiple encounters and detailed expedition art
are deferred.

### Citizens with trajectory

⚠️ Citizens retain identity, profile, roles, competencies, work order,
commitment, stamina, wound and treatment. Training systems, knowledge,
relationships, culture, ageing and deeper history remain absent.

### Causal production and delegation

✅ First cut. Buildings need workers, schedule, condition, storage and enabled
policy; blockers are visible and standing work orders survive temporary stops.
Farm and Quarry still lack operating-material recipes, so the economy is not a
complete production-chain proof.

### Territory

✅ First cut. One expedition target advances through four explicit states and
exposes a real lot. A broader route/biome/resource network is deferred.

### Health and consequences

✅ First cut. One moderate wound is distinct from stamina and needs causal
treatment. Staff, medicine, beds, surgery, rehabilitation, mortality and
multiple conditions remain deferred.

### Environment, institutions and organic difficulty

❌ Beyond the current Food/housing pressure and natural Wood source, these
pillars remain future work. They must arrive as bounded decisions, not batches
of passive fields or generic city-builder counters.

## 5. Presentation and interaction

| Surface | Status | Remaining boundary |
| --- | --- | --- |
| Macro street-perspective city | ✅ sole runtime representation | Confirm tree-row pathfinding/gather visibility. |
| Building detail/construction | ✅ | VS-5 multi-citizen focus/signature. |
| Citizens roster/debug context | ✅ | Human normal-flow signature. |
| Expedition planning/status | ✅ first cut | Snapshot extraction before more dimensions. |
| Policies | ✅ read-only first cut | Schedule remains provisional. |
| Chronicle/blockers | ✅ | Long-horizon persisted history later. |
| Camera/input | ✅ | Free default; explicit follow; UI wheel boundary. |
| Visual regression | ⚠️ | VS-5 keyboard/gamepad and full-flow signature. |
| Audio | ❌ | No wired buses/streams. |
| Final system art | ⚠️ | Several buildings/expedition states remain provisional. |

## 6. Persistence and offline validation

Schema v19 captures the state required by the current loop:

- Citizen identity, profile, roles, competence, work order, commitment,
  contextual location, transit timing, stamina and wounds.
- Construction/buildings, production policy and resource reservations.
- Town Hall prospect and housing-dependent acceptance.
- Expedition members, supplies, phase, encounter outcome, retreat posture and
  causal event identity.
- Treatment state and territory progression.

Automated integration proves reload at Encounter, Objective/Retreating,
Returning and halfway through treatment. The remaining proof is launching the
actual application at the mid-expedition and mid-treatment boundaries and
confirming that presentation reconstructs the current state without replaying
elapsed travel.

## 7. Highest-leverage remaining work

Before a new product slice:

1. Complete VS-5 through normal player-facing actions.
2. Calibrate the daily Food pressure and recruitment opportunity in that run.
3. Confirm pathfinding/visual ownership for multiple citizens and tree rows.
4. Close the visual/focus matrix for the surfaces exercised by the loop.

After VS-5, choose one bounded depth slice. Current candidates, not approvals:

1. One operating input→output production chain.
2. A snapshot boundary for expedition preparation/status before expanding it.
3. A small competence/learning hook that experience can overcome and that does
   not turn lineage into a permanent bonus.
4. A second meaningful territorial choice rather than a broader empty map.

## 8. Explicit deferrals

- Full combat/equipment/formations.
- Deep healthcare, mortality and generations.
- Profession trees, education and institutions.
- Relationship, political, cultural and trade simulations.
- Large territory graphs. (Per-lineage ground biomes landed 2026-08-06 as presentation only — see DEC-0017.)
- Mass-population rendering without profiler evidence.
- Final art/audio pack.
- Backend, networking, telemetry, launcher, settings UI, modding or a second
  city/meta-loop.

## 9. Verification contract

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

Also boot the main Godot scene and run the VS-5 procedure in
`FIRST_PLAYABLE_LOOP_AUDIT.md`. Automated correctness is necessary but does not
replace the player-facing signature.
