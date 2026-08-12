# State authority

> Who owns each mutable truth in World of Goses, what kind of truth it is,
> and what may read it. This file is canonical for the *ownership* question;
> [`ARCHITECTURE.md`](ARCHITECTURE.md) stays canonical for the assembly
> boundaries and [`ai/CROSS_DOMAIN_INVARIANTS.md`](ai/CROSS_DOMAIN_INVARIANTS.md)
> for the rules that span pillars.

The problem this file exists to prevent is not "we have enums". It is **two
places that both believe they know what a citizen is doing.** When that
happens the two drift, and the bug that follows is unfixable in either
place, because neither is wrong on its own terms.

---

## 1. The taxonomy

Five categories. Deliberately small: a new one has to earn its place by
naming a distinction the existing five cannot express, not by being a
convenient shelf for an odd case.

### Lifecycle State

A mutually exclusive, authoritative position in the life of one entity or
process. Changes only through explicit domain commands with semantic names.
Has invariants, a defined set of legal transitions, and — usually — an
absorbing terminal state.

*Test:* can the entity be in two of these at once? If yes it is not a
lifecycle state.

### Orthogonal Condition

A fact that coexists with a lifecycle state instead of replacing it.
Modelling it as a lifecycle state produces the cartesian explosion
(`WorkingWounded`, `TravellingWounded`, `RestingWoundedHungry`) and, worse,
makes the condition silently veto things it has no business vetoing.

*Test:* would you have to multiply the state count to express it? Then it
is a condition.

### Intent / Order

What the player wants to keep being true. Survives temporary interruptions
by design — that survival is the whole point, not an implementation detail.
Re-evaluated when the interruption ends rather than blindly resumed.

*Test:* should it still be there after the citizen comes back from
something unrelated? Then it is an intent.

### Derived Projection

A deterministic reading of canonical facts, computed on demand. Never
persisted when it can be recomputed, never mutated, never the thing another
rule reads to make a decision that the underlying facts could have answered.

*Test:* if two processes computed it from the same save, would they agree?
They must — and if they would, it must not be stored.

### Presentation State

Ephemeral, engine-side, visual or interaction-only: which animation is
playing, what is hovered, what is selected, which accordion is open. Never
decides gameplay, never round-trips into the domain, never persisted in the
world save.

*Test:* if the player resized the window, would losing it matter to the
simulation? If no, it belongs here — and only here.

---

## 2. Registry

`Owner` is the type that holds the field and is allowed to change it.
`Writers` lists everything that may legally call a mutator.

### Citizens

| Concept | Category | Owner | Authoritative? | Persisted | Writers | Reconstruction | Invariants |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `CitizenCommitment` | Lifecycle State | `Citizen` | yes | yes (`CommitmentKind` + `CommitmentEntityId`) | `Citizen` internals, driven by `CityWorld` / `CitizenAssignmentService` | restored verbatim | mutually exclusive; a non-`None` kind requires a positive entity id; an `Expedition` commitment names an active expedition that lists the citizen |
| `CitizenWorkOrder` | Intent / Order | `Citizen` | yes | yes (`WorkOrderKind` + `WorkOrderEntityId`) | `Citizen.TrySetWorkOrder` / `ReleaseCommitment` | restored verbatim | survives expedition and recovery; cleared only by an explicit release |
| `CitizenLocation` | Lifecycle State | `Citizen` | yes | yes (`CurrentLocation`) | `Citizen.SetLocation` / `BeginTravel*` / `RestoreLocation` | restored verbatim | see transit metadata below |
| `TransitStartedAtTick`, `IsReturningHome` | Lifecycle State (metadata of `InTransit`) | `Citizen` | yes | yes | same as `CitizenLocation` | restored verbatim, **never invented** | null unless `InTransit`; an *expedition* traveller is `InTransit` with **no** start tick, because the expedition owns the timing |
| `TravelArrivalTick` | Derived Projection | `Citizen` | no | **no** | — | `TransitStartedAtTick + AbstractTravelTicks` | null whenever there is no start tick; this is what excludes expedition travel from `CompleteDueTravel` |
| `CitizenVitalStatus` | Orthogonal Condition | `Citizen` | yes | yes | `Citizen.BeginVitalRecovery` / `MarkFood*` / `CompleteVitalRecovery` | restored verbatim | `BlockedNoFood` is reachable only from `Recovering`; resuming requires `CitizenNeedsRules.CanResume` |
| `CitizenWound` | Orthogonal Condition | `Citizen` | yes | yes (severity, origin event, remaining ticks) | `Citizen.SustainWound` / `RestoreWound` / recovery advance | restored verbatim | independent of stamina; caps `EffectiveMaxStamina`; only inflicted when `CityWorld.CanCarryWound` — see §4 |
| Stamina (`CurrentStamina`, `MaxStamina`, `WellFedRemainingTicks`) | Orthogonal Condition | `Citizen` | yes | yes | `ConsumeStamina` / `RestoreStamina` / well-fed mutators | restored verbatim | clamped to `EffectiveMaxStamina`; **never** changes any activity value |
| `CurrentHealthAndCondition` | Orthogonal Condition | `Citizen` | yes | yes | `CombatExpeditionService`, `CityWorld.ApplyCombatSessionConsequences` | restored verbatim | derived from combat causes, never assigned arbitrarily |
| `Availability` / `AvailabilityReason` | Derived Projection | `Citizen` | no | no | — | from `Commitment` + `Wound` | — |
| `CitizenRoutineSnapshot` | Derived Projection | `CityWorld.GetCitizenRoutine` | no | **no** | — | recomputed from commitment, location, transit metadata, vital status, wound, work order, clock and buildings | pure and repeatable; two worlds restored from one save produce identical snapshots |
| ~~`CitizenBehaviorState`~~ | **removed** | — | — | — | — | — | see §3 |

### Expeditions and combat

| Concept | Category | Owner | Authoritative? | Persisted | Writers | Reconstruction | Invariants |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `ExpeditionStatus` | Lifecycle State | `Expedition` | yes | yes | `Expedition` internals, from `CityWorld` | restored verbatim | `Returned` / `Failed` / `Cancelled` / `Retreated` are absorbing |
| `ExpeditionPhase` | Lifecycle State | `Expedition` | yes | yes | `BeginEncounter`, `TryAdvancePhase`, `Mark*` | restored verbatim | six legal hops only (`Outbound→Encounter`, `Encounter→Objective\|Retreating`, `Retreating→Returning`, `Objective→Returning`), then `Resolved` |
| `ExpeditionEncounterOutcome` | Lifecycle State (write-once) | `Expedition` | yes | yes | `CompleteEncounter` | restored verbatim, cross-checked against the combat replay | set exactly once; validation rejects a save whose outcome disagrees with the replay |
| `ExpeditionRetreatPosture` | Intent / Order | `Expedition` | yes | yes | immutable after dispatch | restored verbatim | a configured posture, not a failure state |
| `CombatSession` | Lifecycle State | `CityWorld._combatSessions` | yes | yes, **as a replay** (`CombatStepsAdvanced` + `CombatCommands`) | `CityWorld.AdvanceExpeditionPhases`, `SetCombatAutoSkillsEnabled`, `TryActivateMemberSkill` | rebuilt by replaying the deterministic engine from the expedition's own id and start tick | never serialised as materialised combat state — there is one set of combat rules, not two |
| `CombatSessionSnapshot` | Derived Projection | `CombatSession.Snapshot()` | no | no | — | from the live encounter | read-only; projecting it must not advance it |
| `ResourceOpportunityState` | Lifecycle State | `ResourceOpportunity` | yes | yes | `TryReserve` / `Release` / `Deplete` (all `internal`) | restored verbatim | `Reserved` ⇔ names its expedition; `Depleted` is absorbing |
| `ParcelTerritoryState` | Lifecycle State | `CityParcel` | yes | yes | `CityParcel.AdvanceTerritory` | restored verbatim | monotonic |

### City

| Concept | Category | Owner | Authoritative? | Persisted | Writers | Reconstruction | Invariants |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `ConstructionProject` progress and module | Lifecycle State | `ConstructionProject` | yes | yes | `ConstructionSimulation`, `CityWorld` authorisation | restored verbatim | inputs already consumed stay consumed on cancel |
| `ConstructionStopCause` | Derived Projection | `ConstructionSimulation` | no | no | — | from workers, materials, clock, authorisation | every stop cause must be visible |
| `ProductionStopCause` | Derived Projection | `BuildingProductionCalculator` | no | no | — | from workers, schedule, stamina, storage, policy | as above |
| `CultivationPlotState` | Lifecycle State | `CultivationSite` | yes | yes | `TrySow` / `AdvanceTo` / `TryHarvest` | restored verbatim, timing re-validated against `CultivationRules` | `Spent` is absorbing; timing fields must match the state |
| Inventory and `ResourceReservation` | Lifecycle State | `CityResourceLedger` | yes | yes | ledger commands | restored verbatim | a reservation is committed or released exactly once |
| `EdibleStock` | Derived Projection | `CityWorld` | no | no | — | unreserved `Food` + `WildFood`, counted as `TryConsumeFood` spends it | — |

### Narrative

| Concept | Category | Owner | Authoritative? | Persisted | Writers | Reconstruction | Invariants |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `FirstNightStage` | Lifecycle State | `FirstNightState` | yes | yes | `CityWorld` first-night commands | restored verbatim | strictly linear; advances on a world fact (module completed, dialogue closed), never on the clock; `Concluded` is absorbing and requires a concluding tick |
| `CurrentDialogueNodeId` | Lifecycle State | `FirstNightState` | yes | yes | as above | restored verbatim | the resumability seam — `DialogueRunner`'s `await` position cannot be saved, a node id can |
| Fire-spirit position | Presentation State | `FirstNightScene` | no | no | — | derived from `FirstNightStage` | the city persists no authoritative visual coordinates |
| `WorldEventLog` | Lifecycle State (append-only) | `WorldEventLog` | yes | yes | `Record` | restored verbatim | append-only, bounded retention, causal links preserved |

### Presentation

Everything under `game/scripts/Ui/`, `game/scripts/Prototypes/` and the
scenes: selection, hover, camera mode and follow target, accordion
expansion, panel scroll position, current animation, tween progress. None
of it is persisted in the world save and none of it is readable by the
domain. The `*Snapshot` records in `game/scripts/Application/` are Derived
Projections crossing into this layer — they are read models, and a scene
holding one may not write through it.

---

## 3. What happened to `CitizenBehaviorState`

It was a second authority for a question four other authorities already
answered, and it mixed four different kinds of truth into one enum:

| Value | What it actually was | Who already owned it |
| --- | --- | --- |
| `Working` / `Resting` / `Idle` | semantic activity | `CitizenRoutineActivity` (a projection) |
| `Travelling` | location | `CitizenLocation` + transit metadata |
| `OnExpedition` | an external lifecycle | `CitizenCommitment` + `Expedition` |
| `Injured` | *stamina at zero* — not even the wound condition it reads as | stamina, and separately `CitizenWound` |

It was driven by `Citizen.SetLocation`, the stamina mutators and the
expedition hooks, through `FiniteStateMachine<CitizenBehaviorState>` whose
`TryTransition` returns a `bool` that **no call site checked**. A rejected
transition was therefore indistinguishable from an accepted one, and the
value silently fell out of step with the facts around it. It was then
copied verbatim onto `CitizenRoutineSnapshot` — so the derived projection
carried a stale second opinion as a passenger.

It was never persisted, and outside `Citizen` it had exactly one non-test
reader (the snapshot field). **It was deleted outright** — no
compatibility projection was needed, because the blast radius was one
field and one enum. `CitizenBehaviorRules`, `CitizenBehaviorTransition` and
`FiniteStateMachine<TState>` went with it: the FSM class had one consumer
in the entire tree, and keeping a generic transition-table abstraction
alive for zero consumers is infrastructure debt, not a seam.

**No FSM framework replaced it, and none should.** The real lifecycle
machines here — `Expedition`, `CultivationSite`, `ResourceOpportunity`,
`FirstNightState` — are already better state machines than a generic class
would make them: private setters, semantically named commands, validation
in the command, invariants checked in the constructor, and absorbing
terminal states. A generic `TryTransition(to, "some string trigger")` would
replace all of that with a stringly-typed table and a boolean nobody reads.

If a future NPC genuinely needs autonomous behaviour, the thing to add is
that NPC's own lifecycle with its own named commands — not a revival of a
shared behaviour enum for citizens who do not have one.

---

## 4. Progress liveness

> **The domain never inflicts a durable consequence the city has no legal
> route to resolve.**

`DEC-0011` defines wound treatment as Basic Shelter + time + an explicit
resource cost. A wound also makes its carrier unavailable for gathering,
construction and expeditions — which are the only three ways the city could
earn that shelter and that cost. So a wound inflicted on a city that has
neither is not "hard": it is terminal, and for a one-citizen city it ends
the run with no legal action left in any panel.

`WoundRules.CanCityCarryWound` is the gate, read through
`CityWorld.CanCarryWound` and checked in `ApplyExpeditionWound` before any
`WoundSustained` event is recorded. A setback in a city that cannot carry a
wound still costs the run — elapsed time, the combat's health and
condition, no reward — it just does not also become an injury nobody can
treat. Once the city *is* equipped, every ordinary rule stands unchanged,
including the Food cost in `TryBeginWoundRecovery`.

This is `EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §8.2 ("the first
guided sortie cannot silently kill or irreversibly trap the city") and §16
("prevent soft lock") expressed as code rather than as intent. Regressions
live in `OpeningProgressLivenessTests`, which sweeps the founder
configurations onboarding can really produce rather than one fixed affinity
per lineage.

---

## 5. The animation boundary

Not implemented yet. Stated here so that when it is, it is built in the one
direction that keeps this file true:

```
  domain facts            Commitment · Location · transit metadata ·
  (authoritative)         Wound · VitalStatus · stamina · WorkOrder
        │
        ▼
  gameplay projection     CitizenRoutineSnapshot
  (derived, engine-free)  Activity · ContextLocation · BlockReason · timing
        │
        ▼
  presentation projection an animation-intent record, computed in the
  (derived, engine-free)  presentation layer from the snapshot
        │
        ▼
  Godot                   AnimationPlayer / AnimationTree plays the clip
```

Each arrow points one way and only one way. Concretely:

- **Godot never decides gameplay by observing itself.** No rule may ask
  which animation is playing, whether a tween finished, or where a sprite
  is on screen, in order to learn whether a citizen is committed, wounded,
  travelling or on an expedition. Journeys already end on world time alone
  (`DEC-0023`); a hidden view or a dropped frame cannot change production,
  and the animation layer inherits that rule rather than negotiating it.
- **The animation projection is derived, not stored.** It is not persisted
  and it is not fed back into the domain. If it needs a fact the routine
  snapshot does not carry, the fix is to derive that fact into the
  snapshot — not to keep a parallel copy in a node.
- **Animation is a consequence.** `Idle`, `Walk`, `Work`, `Rest`,
  `Attack`, `Hit`, `Downed` are readings of domain facts. They are never
  the reason a domain fact changes.

---

## 6. Adding or changing an authority

1. Name the category from §1. If none fits, that is a design conversation,
   not a sixth category invented in passing.
2. Add the row to §2 in the same change.
3. If it is a Derived Projection, prove it: a test that two worlds restored
   from one save compute the same value, and a test that it does not appear
   in the serialized save.
4. If it is a Lifecycle State, enumerate the legal transitions and assert
   that every other pair is rejected — the whole matrix, not a sample. See
   `StateAuthorityInvariantTests.EveryUndocumentedExpeditionPhaseHopIsRejected`.
5. If it is persisted, it needs a `WorldSave.CurrentVersion` decision,
   round-trip tests, and a live/offline equivalence test.
6. If a second place would now also know the answer, stop. That is the
   defect this file exists to prevent.
