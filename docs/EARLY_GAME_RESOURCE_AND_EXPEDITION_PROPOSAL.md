# Early-game resources, founding camp, and first expeditions

**Status:** approved direction; EG-A0 numbers remain provisional

**Prepared:** 2026-07-29
**Implementation gate:** finish the remaining VS-5 diagnostic, then open EG-0;
final VS-5 signature follows the corrected early game.

## 1. Decision summary

The current opening should not be fixed by merely lowering every reward or
raising every timer. It should be replaced by a short survival arc with three
different resource horizons:

1. finite materials lying near the founder fund a campfire, bedroll, and cache;
2. short, configured expeditions supply the first meaningful Food and usable
   Wood;
3. crops and managed tree exploitation become slower, local, repeatable systems
   after the founding camp exists.

The recommended spatial model is a **hybrid founding site**. One 3 x 3 lot owns
several functional modules, but those modules contribute to one persistent
site identity. Campfire, bedroll, and cache work independently. Adding the
canopy consolidates the same site into the first Basic Shelter without deleting
and respawning an unrelated building.

All quantities below are balance hypothesis **EG-A0**, not canonical product
rules. They must be tested as a complete loop before promotion.

## 2. Current implementation diagnosis

### 2.1 Resources and gathering

- `CityWorld.SeedStartingForests` creates two natural-resource patches.
- Each patch contains 8 visible tree units and each tree holds 40 Wood.
- A new city therefore starts with 16 trees and 640 extractable Wood.
- The macro view gathers 2 Wood per completed visit. There is no tool,
  processing station, work duration, or transport capacity beyond the visual
  walk to the selected tree.
- Natural tree reserves regenerate by 1 per unit at dawn and each patch may
  sprout another unit in a free lot.
- Gathered Wood enters `CityInventory`, which currently has no capacity.

The complete current construction catalog costs 28 Wood in total:

| Construction | Current total cost |
| --- | ---: |
| Basic Shelter | 4 Wood |
| Farm | 6 Wood |
| Quarry | 8 Wood + 4 Food |
| Town Hall | 10 Wood + 6 Stone |

The initial 640 Wood is therefore more than 22 times the Wood needed by every
connected first-loop construction. The problem is structural, not a small
tuning error: the first natural patch behaves as an effectively unlimited city
stockpile.

### 2.2 Construction and shelter

- `ConstructionProject` persists one progress value, assigned contributors,
  deposited inputs, and remaining inputs.
- `ConstructionRules.PhaseFor` derives six visual labels from that ratio, but
  phases do not have independent requirements or functions.
- A completed project is replaced immediately by its resulting `Building`.
- The Basic Shelter is the only valid first construction and all other
  construction requires a completed Home.
- The founding shelter automatically receives the available founder once its
  remaining materials are available.

This is useful infrastructure for phased work, but it does not yet represent
functional modules or intermediate capabilities.

### 2.3 Farm and Food

- A completed Farm immediately becomes a normal production building.
- With a present worker it can add Food every 10 ticks until its stock policy or
  storage stops it.
- There is no plot, sowing, crop batch, planted timestamp, growth state, or
  harvest boundary.
- Every resident consumes 1 Food at dawn. Food is also used for stamina recovery,
  expedition supplies, Quarry construction, and wound treatment.

The Farm currently solves Food too quickly once built and cannot communicate
the medium-term investment promised by agriculture.

### 2.4 Expeditions

- The implementation already has a persisted team of 1-2 real citizens,
  supplies, retreat posture, deterministic encounter, outbound, encounter,
  objective/retreat, return, exact-once rewards, wounds, and offline catch-up.
- Canonically, only citizens explicitly incorporated as heroes may participate.
- Current request templates are four-day Reconnaissance and Community Contact.
  Reconnaissance reserves 1 Wood and may return 1 Stone; Community Contact
  reserves 2 Food and may return a prospect.
- Resource opportunities, carry limits, route reserves, and Wood/Food sortie
  objectives do not yet exist.

The expedition foundation should be extended, not replaced. A survival sortie
must still be an outbound-objective-return commitment with a real citizen and a
city consequence; it must not become a button that converts time into resources.

### 2.5 Persistence and offline time

The current architecture already persists semantic state and advances the
domain before rendering. Construction, citizen commitments, resources,
expedition phases, wounds, territory, and timestamps round-trip. The missing
early-game concepts need boundary-based timestamps and contextual state, not
node positions or replayed animation.

## 3. Design goals and non-goals

The redesigned opening should make the player decide:

- which survival capability to establish first;
- whether the founder should remain in the camp or leave on a sortie;
- whether scarce Food should be eaten, planted, supplied, or reserved;
- whether the next external trip should secure Food or usable Wood;
- when to invest in agriculture that will not pay back immediately;
- when local tree exploitation is worth the tool, labor, and depletion cost.

It should not add hunger simulation, temperature simulation, free placement,
advanced combat, hunting, fishing, final sprites, or a generic crafting tree in
the first implementation.

## 4. Recommended starting resources: EG-A0

Only four immediately collectible resource types are recommended for the first
test. Leaves are a visual variant of Plant Fiber rather than another counter.
"Natural remains" are presentation, not a fifth resource.

| Resource | Ground distribution | Total | Immediate use |
| --- | ---: | ---: | --- |
| Branches | 7 bundles x 2 | 14 | Fire, bedroll frame, cache, canopy |
| Plant Fiber | 3 clusters x 2 | 6 | Bedding, bindings, canopy |
| Small Stone | 3 clusters x 2 | 6 | Fire ring and later preparation |
| Wild Food | 4 patches x 2 | 8 | Four-day buffer, seed, or supplies |

Ground nodes are finite. Collecting one should take a short contextual action
and use the existing visual pathfinding, but it should not require a tool. The
domain records source identity, remaining reserve, assigned collector if any,
and result; the walk and pickup animation remain presentation.

Starting resources remain on their nodes until collected. They do not count
against storage before collection.

Before the Cache exists, the founder may carry at most 6 collected units. The
Cache holds 12 total units and the consolidated Basic Shelter raises site
storage to 24. These are domain capacities, not visual slot counts. Building a
module consumes carried or stored inputs through the ledger; the game does not
create a hidden pre-camp warehouse.

### Mature trees

- Seed 6 mature trees near the founding area, not 16.
- Each tree represents 8 units of usable Wood, for 48 Wood total potential.
- A mature tree cannot be exploited at the start.
- Unlock exploitation only after the founding shelter and a minimal forestry
  capability: a suitable tool or a worker assigned through a forestry order.
- Extraction should take four work boundaries of 2 Wood, rather than granting
  the full tree instantly.
- Do not regenerate 1 Wood per tree every day. In the first implementation,
  local mature trees are finite. A later regeneration slice may grow a sapling
  over roughly 10-14 in-game days when space and policy permit.

This makes local trees a strategic reserve. Early expeditions remain useful
without making the initial map empty or decorative.

## 5. The founding 3 x 3 site

### 5.1 Alternatives considered

| Model | Strength | Main problem | Verdict |
| --- | --- | --- | --- |
| Separate buildings | Each capability is obvious and independently selectable. | Three tiny buildings clutter one lot, complicate pathing, and make the later shelter replacement arbitrary. | Reject for the founding site. |
| One linear phased building | Simple persistence and clear progression. | The player cannot understand or use fire, rest, and storage independently; order becomes rigid. | Useful base, insufficient alone. |
| Free modules in a lot | Expressive and reusable. | Introduces a placement system before it creates enough decisions; high UI and art cost. | Defer. |
| Hybrid site with named modules | Independent early functions, one site identity, deterministic upgrade. | Needs a small phase/module model in the domain. | **Recommended.** |

### 5.2 Recommended sequence and costs

The player claims one 3 x 3 lot as a Founding Site. It owns one persistent ID,
one placement, and a bounded set of module slots.

| Module/state | Cost | Function when complete |
| --- | --- | --- |
| Campfire | 3 Branches + 2 Small Stone | Establishes the camp anchor and enables nearby survival sorties. |
| Bedroll | 2 Branches + 3 Plant Fiber | Enables proper founder rest at the site. |
| Cache | 2 Branches + 1 Plant Fiber | Stores up to 12 collected units and enables expedition return delivery. |
| Canopy | 4 Branches + 2 Plant Fiber | Consolidates the completed site into a Basic Shelter. |

Total: 11 Branches, 6 Plant Fiber, and 2 Small Stone. The initial distribution
leaves 3 Branches and 4 Small Stone after completing the shelter: enough for the
first plot and two Food-sortie supplies. Wild Food is not consumed by the
shelter.

The first recommended action is the Campfire because it creates both a visible
center and the next meaningful choice. Bedroll and Cache may then be completed
in either order. The Canopy requires all three prior modules.

### 5.3 Transformation rule

The Founding Site does not disappear. When all modules are complete:

1. its same entity/placement ID changes capability set to Basic Shelter;
2. the campfire, bedroll, and cache remain inspectable as origin facts;
3. shelter housing/rest/storage capacity supersedes, rather than duplicates,
   module capacities;
4. visual presentation replaces or composes sprites at a phase boundary;
5. existing assignments and stored resources remain attached to the site.

This model scales to later buildings because phases grant capabilities and have
causal requirements, without requiring every building to be a free-form module
editor.

## 6. General construction phase model

Do not replace `ConstructionProject.Progress` with a giant generic framework.
Add the minimum semantic layer needed by the next slice:

```text
ConstructionPlan
  currentPhaseId
  completedPhaseIds
  phaseStartedAtTick
  assignedCitizenIds
  reserved/deposited inputs
  stopCause

ConstructionPhaseDefinition
  prerequisites
  required inputs
  required work
  granted capabilities
  resulting visual state
```

A phase completes through the existing contribution rules. The world advances
phase boundaries live and offline through the same domain method. Presentation
reads the current visual state but cannot complete a phase.

For ordinary later buildings the phase sequence can remain linear:

```text
Planned -> Materials ready -> Prepared -> Structure -> Functional -> Complete
```

For the Founding Site, Campfire, Bedroll, and Cache are a small prerequisite
graph and Canopy is the consolidation phase. This is the only branch required
by the first implementation.

## 7. Agriculture proposal

### 7.1 Farm starts as plots

The first agricultural authorization claims a lot as a Cultivation Site, not a
finished Farm. It progresses through:

1. prepare one plot;
2. sow one seed unit;
3. grow until `readyAtTick`;
4. harvest;
5. add a second and third plot;
6. consolidate agricultural storage/work infrastructure into a Farm.

Each plot is domain state. Crop visuals only represent `Prepared`, `Sown`,
`Growing`, `Ready`, or `Spent`.

### 7.2 EG-A0 crop timing and yield

- Preparing the first plot costs 1 Branch and 1 Small Stone.
- First crop: ready after 3 full in-game days.
- First plot seed cost: 1 Food.
- First harvest: 5 Food.
- The second and third plots each cost 1 Wood + 1 Small Stone to prepare and
  1 Food to sow.
- Consolidating three prepared plots into the first Farm costs 6 Wood +
  2 Small Stone. One full-success Wood sortie can therefore fund the two later
  plots and consolidation, while a partial result forces a real follow-up
  choice.
- A prepared plot requires work, but growth does not require a citizen to remain
  assigned continuously.
- Later plots and yields remain calibration work; do not generalize crop species
  in the first slice.

At the current 3600 ticks per in-game day, the first crop boundary is 10,800
ticks after sowing. It must resolve through event boundaries during offline
catch-up, not by iterating crop nodes.

### 7.3 Food horizon

The minimum protected Food horizon is:

```text
resident daily ration x (days until first harvest + 1 buffer day)
+ planned expedition Food supply
```

For the lone founder and a three-day crop, this is 5 Food if one Food is
reserved for a Wood sortie. Starting with 8 Wild Food leaves three units of
decision room. Recruitment should not be encouraged before the first harvest
or another visible Food source secures this horizon.

### 7.4 Reference flow across the first days

| Period | Expected decisions, not a mandatory script |
| --- | --- |
| Opening | Collect enough ground material for Campfire; inspect the four-day Food horizon. |
| Day 1 | Build Cache or Bedroll; choose whether to send the founder on the short Food sortie. |
| Day 2 | Complete the remaining camp modules, prepare/sow the first plot, or seek usable Wood. |
| Day 3 | Use external Wood on cultivation or save it for shelter/future infrastructure; manage the last pre-harvest Food. |
| Day 4-5 | Receive the first harvest depending on sowing time, finish the Canopy/Shelter, and decide between more plots, another sortie, or local forestry preparation. |
| Consolidation | Reach three plots, consolidate the Farm, increase storage, and only then approach recruitment/Town Hall pressure. |

The exact day labels are targets for play calibration. The causal order matters
more than forcing every player onto the same calendar.

## 8. First resource expeditions

### 8.1 Unlock and participants

- Unlock the first nearby sortie when Campfire and Cache are functional.
- The founder is eligible because the founder is already an incorporated hero.
- Other citizens remain ineligible until explicitly incorporated as heroes, as
  required by the design bible. Do not create a second anonymous expedition
  worker type.
- Team size remains 1-2 and every participant takes an exclusive Expedition
  commitment.

### 8.2 EG-A0 objectives

| Objective | Duration | Supply | Setback / partial / full return |
| --- | ---: | ---: | ---: |
| Nearby Food Forage | 120 ticks | 1 Branch | 3 / 5 / 7 Food |
| Fallen Wood Search | 180 ticks | 1 Food | 4 / 6 / 8 Wood |

At the current 1 Hz clock these last roughly 2 and 3 real minutes. They are
short enough for the sole-founder opening; the existing four-day expeditions
remain later operations.

The result is not a blind random roll. It derives from a persisted opportunity
and route, member condition and competence, supplies, carry capacity, chosen
retreat posture, and any encounter. The UI shows duration, minimum/expected
return, relevant risks, and what will pause while the founder is away.

The first healthy Food sortie has a guaranteed minimum return of 3 Food. A
setback still matters through consumed time/supply, fatigue, and reduced return.
A persistent wound is possible only when the player sends an unfit member or
chooses to continue after a visible setback; the first guided sortie cannot
silently kill or irreversibly trap the city.

### 8.3 What the city does while the founder is away

- Time, crop growth, Food ration, recovery, and any already-authorized autonomous
  process continue.
- Work or construction that depended on the founder pauses with a visible
  `NoAvailableWorker`-class cause.
- The player can inspect, change policies, and plan, but cannot issue a second
  incompatible commitment to the absent founder.
- Return resolves cargo into the Cache only if capacity exists. Dispatch should
  prevent a trip whose minimum return cannot be stored, or reserve capacity.

### 8.4 Why this is not a resource button

Every resource sortie targets a specific persisted opportunity with:

- location/route identity;
- known remaining reserve or availability window;
- outbound and return legs;
- real team commitments;
- supplies and reserved return capacity;
- carry limit;
- condition-dependent result;
- possible route/territory consequence;
- depletion or cooldown after use.

The two first opportunities are finite and guided by city needs. Repeating them
indefinitely cannot sustain growth. Further sources require reconnaissance,
route security, or territorial access.

## 9. Need-driven guidance

Do not add a modal tutorial chain. Derive at most one primary recommendation
from world facts:

| World fact | Suggested decision |
| --- | --- |
| No campfire | Establish heat and a city anchor. |
| Campfire exists, no cache | Make somewhere to receive gathered cargo. |
| Food horizon below first-harvest requirement | Prepare a nearby Food sortie. |
| No usable Wood for the next authorized phase | Search for fallen Wood. |
| Plot sown, crop growing | Complete rest/storage or plan an expedition. |
| Storage cannot accept expected return | Spend, build capacity, or change objective. |

The explanation must state the cause and remain dismissible. The game does not
auto-dispatch, auto-build, or choose which scarcity matters most.

## 10. Soft-lock protection

The opening remains recoverable without granting infinite free resources:

1. ground generation validates guaranteed minimum totals before the new city is
   accepted;
2. essential modules cannot consume more than those guaranteed totals;
3. the Cache reserves capacity for the minimum expedition return at dispatch;
4. cancellation releases uncommitted reservations; work already performed may
   have a visible partial-loss rule, but it cannot erase the only path forward;
5. the first Food sortie uses a Branch rather than Food, so zero Food does not
   block the Food recovery route;
6. the first healthy Food sortie returns at least 3 Food;
7. the UI warns before sowing or spending below the calculated Food horizon;
8. a new city cannot recruit into an unsupported Food horizon;
9. old saves with an active legacy shelter are allowed to finish it rather than
   being rewritten in place.

These are explicit safety rules, not invisible resource gifts.

## 11. Offline and persistence contract

Persist semantic context only.

### Resource opportunities

- opportunity ID and kind;
- parcel/lot or route anchor;
- remaining reserve;
- availability/cooldown boundary;
- access requirement state.

### Founding site and construction

- stable site/project ID and placement;
- completed/current phases or modules;
- deposited/reserved inputs;
- accumulated work;
- assigned citizens and stop cause;
- phase start/completion boundary.

### Crops

- plot ID and site ID;
- crop definition ID;
- state;
- planted tick and ready tick;
- harvest result not yet collected, when applicable.

### Expeditions

Keep the existing semantic expedition state and add objective/opportunity ID,
carry capacity, reserved return capacity, and causal result inputs. Do not save
visual positions along the route.

Offline catch-up should jump between the next relevant boundary: ration,
construction contribution/phase completion, crop readiness, expedition phase,
return, recovery, and day transition. It should invoke the same domain
transitions as live play and record each exact-once result.

## 12. Existing systems to reuse

- `NaturalResourcePatch`: stable unit reserves and parcel occupancy.
- `CityResourceLedger`: atomic consumption and durable reservations.
- `ConstructionProject` and `ConstructionSimulation`: work, contributors,
  material drawdown, stop causes, and completion events.
- `Citizen.Commitment` and work orders: exclusive availability.
- `Expedition`: team, supplies, retreat, phase chain, deterministic persisted
  result, return, and wounds.
- `WorldTimeAdvance` / `OfflineProgression`: shared live/offline advancement.
- `WorldEventLog`: causal reports and visible blockers.
- parcel lots and placements: stable 3 x 3 site identity.
- macro pathfinding and building anchors: live visual travel only.

## 13. Systems to create or refactor

### Required for the first redesigned slice

- data-driven ground resource kinds beyond Wood;
- bounded storage ownership/capacity for collected city inventory;
- semantic construction phases/modules with granted capabilities;
- crop plot lifecycle with `readyAtTick`;
- resource expedition objective tied to a finite opportunity;
- need recommendation derived from world facts;
- new save schema and migrations.

### Refactor, do not duplicate

- generalize `NaturalResourcePatch`; do not create a parallel pickup system;
- make inventory capacity location-aware through the ledger; do not put limits
  only in the HUD;
- extend `ConstructionProject`; do not create a separate camp progress loop;
- extend `ExpeditionRequest`/`Expedition`; do not add a separate sortie timer;
- extend offline event boundaries; do not simulate crop or construction nodes.

### Explicitly deferred

- tool durability and full crafting;
- tree species and forestry policy;
- multiple crop species, fertility, water, weather, pests;
- hunting/fishing;
- advanced encounter content and combat;
- free-form building modules;
- final art and audio.

## 14. Save migration strategy

The implementation will require a schema version bump because it adds persisted
resource opportunity, phase, crop, and capacity state.

- Existing saves with a completed Home keep it. Never deconstruct it into camp
  modules.
- Existing active Basic Shelter projects remain legacy projects and may finish
  under their captured requirements.
- Existing pre-shelter saves receive validated ground opportunities only if the
  migration can prove they are absent; gathered Wood is preserved.
- Existing tree reserves are clamped or mapped to legacy mature-tree
  opportunities without deleting already gathered inventory.
- Existing expeditions retain their request/result model. New resource
  objective fields are optional for migrated expeditions and required only for
  newly dispatched resource sorties.
- Round-trip and live/offline equivalence tests are mandatory before enabling
  the new founding flow for new cities.

## 15. Incremental implementation order

No implementation begins until VS-5 closes. After that, open a named early-game
slice and implement in narrow, playable increments:

1. **EG-0 — measurement and contract.** Capture time-to-first-shelter,
   resources collected/spent, idle time, first Food horizon, and expedition
   opportunity cost. Approve or revise EG-A0 numbers.
2. **EG-1 — resource/storage seam.** Generalize natural opportunities, add
   bounded Cache storage and migrations. Keep the legacy opening available
   until the new loop is end-to-end.
3. **EG-2 — founding site seam.** Deliver Campfire -> Bedroll/Cache -> Canopy
   -> Basic Shelter in one stable 3 x 3 site, including offline phase completion.
4. **EG-3 — Food horizon seam.** Add one plot, sowing, three-day growth,
   harvest, ration projection, and offline crop readiness.
5. **EG-4 — resource expedition seam.** Add one finite Food opportunity and
   one finite Wood opportunity through the existing full expedition chain.
6. **EG-5 — consolidation.** Add second/third plots, Farm consolidation, and
   gate mature-tree exploitation behind the first real forestry capability.
7. **EG-6 — calibration/signature.** Run two new-city cycles with relaunches,
   one suboptimal-but-recoverable decision, and no debug actions. Only then
   retire the legacy founding flow.

Each increment must preserve a completable city. If the branch cannot yet offer
both a Food recovery route and a shelter-completion route, it is not ready to
replace the current new-city flow.

## 16. Answers to the required questions

1. **Initial Wood on map:** 48 potential Wood in 6 mature trees, unavailable
   at first; plus 14 collectible Branches as a distinct rudimentary material.
2. **Wood per tree:** 8 usable Wood, extracted as four work results of 2.
3. **Tree available at start:** no; it requires a later forestry capability.
4. **Tool-free resources:** Branches, Plant Fiber, Small Stone, and Wild Food.
5. **First construction:** Campfire inside the Founding Site.
6. **Separate or phased:** functional modules inside one persistent hybrid
   site, not separate buildings and not one opaque progress bar.
7. **Shelter transformation:** complete Campfire, Bedroll, Cache, then Canopy;
   preserve the same ID, placement, storage, and history.
8. **First harvest:** 3 in-game days after sowing.
9. **Food before harvest:** protect 5 Food for one founder including a buffer
   and one Wood-sortie supply; start with 8.
10. **First expedition unlock:** when Campfire and Cache are functional.
11. **Participants:** only explicitly incorporated heroes; initially the
    founder, later a 1-2 hero team.
12. **City while away:** autonomous authorized processes continue; founder-
    dependent work pauses with a visible cause; consumption and crops continue.
13. **First durations:** 120 ticks for Food and 180 for Wood under EG-A0.
14. **Real risk:** supply/time cost, exclusive absence, fatigue, reduced or
    failed return, and a warned wound risk when unfit or continuing after a
    setback; no silent lethal opening roll.
15. **Returns:** Food 3/5/7 and Wood 4/6/8 for setback/partial/full.
16. **Prevent soft lock:** guaranteed local minima, capacity reservation,
    recoverable reservations, Branch-funded Food sortie, minimum Food return,
    visible horizon warning, and guarded recruitment.
17. **Avoid resource-button expeditions:** bind them to finite opportunities,
    routes, citizens, supplies, cargo, return capacity, causal outcomes, and
    depletion/territory consequences.
18. **Offline behavior:** crop growth, construction phases, expeditions,
    rations, recovery, resource cooldowns, and returns resolve from persisted
    semantic state through the same domain transitions as live play.

## 17. Acceptance test for the proposal

The design is ready for implementation only if a paper simulation and then a
playable prototype demonstrate all of the following:

- the founder can always reach Campfire, Cache, Food sortie, first plot, and
  Basic Shelter without an invisible bailout;
- choosing Bedroll before Cache and Cache before Bedroll are both valid;
- the first crop cannot solve Food before day 3;
- one poor but explained decision delays progress without destroying the city;
- the first expedition creates a real absence/trade-off and includes return;
- local collection remains useful but cannot finance indefinite growth;
- live and offline runs reach the same state at every semantic boundary;
- the opening contains a meaningful action or decision during every waiting
  window;
- no new system depends on visual nodes, coordinates, or animation state.

## 18. Principal risks

- **Too many counters:** four rudimentary resources are the maximum for the
  first test; merge Plant Fiber or Small Stone if players cannot read their
  distinct decisions.
- **Sole-founder downtime:** durations above 2-3 real minutes risk turning the
  first expedition into inactivity. Measure before increasing them.
- **Hidden safety rails:** guaranteed minima and capacity reservations must be
  visible in previews and causal explanations.
- **Generic construction framework:** implement only the phase graph needed by
  Founding Site and Cultivation Site.
- **Save invalidation:** grandfather completed and active legacy structures;
  do not rewrite player history.
- **Expedition farming:** finite opportunities and territorial replenishment
  must replace infinite repeatable reward templates.
- **Premature tool system:** use one capability gate in the first tree slice;
  defer durability and broad crafting.
