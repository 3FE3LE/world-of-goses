# Validation against the design bible

**Last aligned:** 2026-08-10

**Code baseline:** closure verification: clean build, 1154/1155 tests
(1 skipped), schema v33, 1049 localization template IDs and 324 runtime keys.
The dated measurement in `docs/session-state/STATE.txt` predates this increment;
the Full snapshot pipeline did not complete, so that artifact was not rewritten.

**Active proof:** EG-5V — Founder Spirit Trail visual vertical.

This document is the current cross-check between the connected prototype and
the design bible. It does not replace `CURRENT_STATUS.md` for implementation
facts or `EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §17 for closure.

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
| EG-5V Founder Spirit Trail visual | ⚠️ active | Founder-only lateral encounter within ~5 minutes, objective continuation and return on one clock. |
| EG-5C agricultural consolidation | ⚠️ queued | Plots 2–3 and Farm after EG-5V. |
| EG-6 signature/repetition | ⚠️ queued | Full normal-UI run and second cycle without reset/debug. |

The domain already proves two expeditions with the same founder and a reload
between cycles. This is not sufficient to close EG-5V, EG-5C or EG-6 because
discoverability, layout, focus, copy and real relaunch behavior must also be
observed. VS-5 remains historical and does not gate the active sequence.

## 3. Architecture compliance

| Area | Status |
| --- | --- |
| Domain contains no Godot imports/resource paths | ✅ enforced |
| One sealed `Citizen` person entity | ✅ |
| Visual node is representation, not persisted citizen | ✅ |
| Semantic citizen persistence; no authoritative visual coordinates | ✅ |
| Versioned validated snapshots and explicit migrations | ✅ v2→v33 |
| Atomic local save with `.bak` | ✅ |
| Dirty-aware three-minute autosave and close-save | ✅ |
| Live/offline share domain transitions | ✅ for current loop |
| Assigned-work catch-up batched to semantic boundaries | ⚠️ still tick-stepped |
| UI consumes snapshots consistently | ⚠️ most surfaces; spatial Expedition feedback remains debt |
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

⚠️ The minimal expedition seam now connects the first Spirit Trail to a
world-owned incremental `CombatSession` observed by `ExpeditionLiveView`.
Basic Attack, AUTO/manual Active Skill use, cooldown, health, enemies, outcome
and save/load replay are integrated on the single world tick. Spatial
advance/range/knockback, the post-dawn Cache-gate exception, four-hour/no-Food
Spirit Trail contract and full objective/return signature remain open; broader
equipment, formations and combat depth stay deferred.

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
| Building detail/construction | ✅ | EG-5C agricultural consolidation and human signature. |
| Citizens roster/debug context | ✅ | Human normal-flow signature. |
| Expedition planning/status | ✅ first cut | Current domain picker remains 1–2; the first Spirit Trail enforces Founder-only and projects four future slots. |
| Expedition live view | ⚠️ active | Lateral observable encounter preserves 1x/2x/4x; spatial advance and full objective/return signature remain. |
| Policies | ✅ read-only first cut | Schedule remains provisional. |
| Chronicle/blockers | ✅ | Long-horizon persisted history later. |
| Camera/input | ✅ | Free default; explicit follow; UI wheel boundary. |
| Visual regression | ⚠️ | EG-5V/EG-5C keyboard, gamepad and full-flow signatures. |
| Audio | ❌ | No wired buses/streams. |
| Final system art | ⚠️ | Several buildings/expedition states remain provisional. |

## 6. Persistence and offline validation

Schema v33 is current. The list below describes the original v19 loop seam;
later migrations are tracked in `CURRENT_STATUS.md` and `ARCHITECTURE.md`:

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

1. Deliver EG-5V end to end: `SpiritDeparted` → Founder dispatch → first visual
   encounter by ~5 minutes → objective → return.
2. Keep city, travel and combat on one unpausable clock; view changes preserve
   the selected 1x / 2x / 4x speed.
3. Resume EG-5C agricultural consolidation.
4. Close EG-6 calibration/signature through normal player-facing actions.

## 8. Explicit deferrals

- Combat beyond the bounded EG-5V encounter; equipment economy, Traits,
  Chains, carriage, `SPACE`, advanced formation and functional Skills 2–4.
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

Also boot the main Godot scene and run the applicable EG-5V, EG-5C and EG-6
player-facing paths against `EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`
§17. The discarded VS-5 procedure is historical and does not gate closure.
Automated correctness is necessary but does not replace the player-facing
signature.
