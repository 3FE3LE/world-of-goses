# Validation: end-to-end against the design bible

> Snapshot of how the current slice holds up against the project's
> own documents. Written as part of Slice 8 (end-to-end validation).
> It is **not** a test suite and it is **not** aspirational — it is
> an honest cross-check of what code exists today against what was
> promised. Markers: ✅ implemented · ⚠️ partial / shape only ·
> ❌ missing / out of scope.

This snapshot measures the code against the **canonical design source**
— the design bible at
[`world-of-goses-design-bible/`](world-of-goses-design-bible/README.md).
The §2 table below cites the vision chapter (§01) and the nine-pillar
chapter (§02); §4 cites specific sections of the bible. When a future
pillar or lineage lands in code, this file should grow a row for it.

---

## 1. Original slice scope (per `README.md` §13)

The first prototype scope is expanded below into verifiable criteria:

| Criterion | Status | Where |
|---|---|---|
| Complete founding-hero onboarding with gender-aware profile choices | ✅ | `OnboardingView`, `HeroProfileView`, `CitizenProfile`, `ProfileCatalog`, `GenderId` |
| Macro city view with a valid one-hero / zero-building empty state (hero walks on the field) | ✅ | `CityMacroView`, `CityStatusPanel`, `MacroCitizenActivity` |
| Detailed building view with configurable visual worker limit | ✅ | `BuildingDetailView` + `VisibleWorkerSlots`, retained for future construction |
| Individual citizen records shared between views; the citizen is the only person entity (roles, competencies, profile, and recognitions are attached concepts) | ✅ | `Citizen` is a single sealed class; profile, gender, roles, and competencies attach to it |
| Worker assignment / removal with a deterministic production counter | ✅ | `CityWorld.TryAssignCitizen` / `TryUnassignCitizen` and explicit test scenarios |
| Construction authorisation consumes a recipe deposit atomically with rollback | ✅ | `CityWorld.TryAuthorizeConstruction`, `Recipes.ConstructionRecipeFor`, `ConstructionRules.DepositOf` |
| Reactive production policy stops at MaxStock and resumes at MinStock | ✅ | `Building.ConfigureProductionPolicy`, `Building.ResumeIfBelowMin`, `Building.CanProduce` |
| Wood-gathering from natural sources drains a reserve into a spendable pool | ✅ | `BuildingKind.Forest`, `Building.GatherWood`, `CityWorld.GatherWood`, `ForestGatherPanel` |
| Causal event log carries `CauseEventId` chains across production ticks | ✅ | `WorldEvent.CauseEventId`, `CityWorld.FindCauseEvent`, `OfflineReportPanel` "Decisions needed" |

**Scope verdict:** all slice-scope criteria are met. The lineage UI skin and
typography pipeline are integrated; character/building pixel art remains
placeholder (Forest has no art at any level).

---

## 2. Additions beyond the original scope

The user-visible surface area grew from the original scope without
violating any architecture rule:

| Addition | Status | Notes |
|---|---|---|
| Multi-building (Quarry + Farm with distinct data) | ✅ done | `BuildingKind` enum; `ProducedCompetencyId` and resource labels per building |
| Persistence (save / load) | ✅ done | `WorldSave` DTOs, JSON, atomic temp + replace, schema `Version` field |
| Offline progression (catch-up while away) | ✅ done | `OfflineProgression` with 7-day cap, tick arithmetic, experience carries even after storage fills |
| Auto-save (timer + window-close) | ✅ done | `_Process` interval (10 s default, tunable), `_Notification` window-close hook. **No manual Save button** — saving is silent, per the `no progress loss` principle |
| Validation on load | ✅ done | `WorldPersistence.Validate` checks schema, ranges, IDs, collections and bidirectional assignment consistency before restore |
| Persistent production authorization | ✅ first step | Each building persists an enabled state and target stock; manual, live and offline production stop when policy blocks work or the target is reached |
| Shared world advancement | ✅ done | One live world tick and offline catch-up process every authorized building while advancing the world clock once |

---

## 3. Architecture compliance (`docs/ARCHITECTURE.md`)

### 3.1 The five-layer layout

| Layer | Status |
|---|---|
| Domain (no Godot.*) | ✅ enforced by `DomainBoundaryTests` across every Domain C# source |
| Godot representation | ✅ `game/scripts/*.cs` (presentation scripts) + `game/scenes/*.tscn` |
| Assets | ⚠️ lineage UI skins, licensed fonts/icons, and default nine-slice theme integrated; character/building art remains placeholder (Forest plot has no art yet) |
| Local persistence | ✅ implemented (`game/scripts/Domain/Persistence/`) with v3 → v4 migration chain |
| Tests | ✅ `tests/WorldofGoses.Tests/` — **309 / 309 passing** at this snapshot |

### 3.2 The Godot/.NET boundary (§5)

- `partial class` used where required by source generators
- Domain code contains no Godot references or `res://` asset paths
- Presentation stays thin (no business logic in `BuildingPlot` / `CityMacroView` etc.)
- Controllers (`CityWorldController`) are the only place where domain and Godot signals meet

### 3.3 Person entity (§7)

> "Exactly one person entity in the domain: `Citizen`. The
> prototype does **not** introduce specialised subclasses for hero,
> miner, doctor, etc. Those concepts are *attachments*."

✅ Holds. There is one `Citizen` class. Roles, competencies, and availability are attached. The current `RoleId.Miner` is a placeholder; future slice can add `RoleId.Healer`, etc., without touching `Citizen`.

### 3.4 Three visual scales (§7a)

| Scale | Status |
|---|---|
| Macro | ✅ implemented (`MacroCitizenActivity` count is derived from the current citizens) |
| Building-detail | ✅ implemented (`VisibleWorkerSlots` — each visible worker **is** bound to a `CitizenId`) |
| Expedition-detail | ❌ not implemented (out of scope for the first prototype, called out in the doc itself) |

### 3.5 Composition over inheritance (§9.12 of vision)

✅ Holds. `Citizen` and `Building` are sealed domain classes configured through composed data, not specialised subclasses. The controller owns one `CityWorld` with multiple buildings and citizens.

### 3.6 "Domain is not presentation" (§9.13)

✅ Holds and is executable. `WorldEvent` carries semantic event data only;
`OfflineReportPanel` maps kinds to `IconPaths`. `DomainBoundaryTests` scans the
complete Domain source tree for Godot references and resource paths. UI panels
consume immutable presentation snapshots rather than mutable domain entities.

---

## 4. Bible alignment

### 4.1 Main fantasy (§1)

> "A single living city that grows because of decisions made by a
> player who is not always present."

✅ Achieved within scope: the controller advances the world automatically at
1 Hz during play and applies offline catch-up on load. Current activity includes
stone/food production, construction, stamina, and manual forest gathering; the
missing part is the breadth and long-horizon richness described by the bible.

### 4.2 Single-city concept (§2)

✅ Holds. One `CityWorld`, one canonical primary save slot. No meta-progression.
The architecture would let us have multiple `CityWorld`s in
memory, but only one is exposed via the controller.

### 4.3 Absence without artificial penalties (§3)

✅ Holds. `OfflineProgression.ComputeTicks` does not subtract for
being away; it caps the catch-up at 7 days, but the cap is a
*correctness* limit (avoid 2-months-of-tick-loops on launch), not
a penalty. There is no decay, no grinding bonus for being absent.

### 4.4 Two gameplay pillars (§4)

#### 4.4.1 City development (§4.1)

✅ **Material inputs landed; chains still pending.** The architecture now
supports material costs and per-tick drawdown for projects and
operating buildings. The founding Forests are a non-productive
source of wood; Basic Shelter, Farm, and Quarry each carry a
  recipe with a playable wood / food bootstrap. What is still missing:
- Production chains (Smithy consumes Iron and produces Tools; Weaver
  consumes Food and produces Cloth).
- Shared inventory abstraction. Reserved Iron still lives on per-building
  input reserves; food on each Farm's Stock; wood on
  each Forest's Stock. A single city aggregate would let recipes
  source inputs from any building.
- Knowledge, institution, and richer condition gates (the bible's
  "Un edificio no produce por existir" picture is only partly in
  code; the inputs list is the only condition today).
- Explicit "dimensions" object (age, culture, politics, economy,
  geography, demographics, professions, knowledge redundancy,
  institutions, generations).

#### 4.4.2 Expeditions (§4.2)

❌ **Missing entirely.** This is out of the first prototype's
scope, but worth flagging as the next gameplay pillar that needs
its own architectural surface.

### 4.5 Citizens (§5)

| Spec item | Status |
|---|---|
| Multiple competencies per citizen | ✅ |
| Current profession / role | ✅ (via `Role` attachments) |
| Previous professions | ⚠️ partial — `Role` records the tick granted; we don't track role history beyond that |
| Competencies (open-ended) | ✅ — `CompetencyId` is a record struct, not a fixed enum |
| Contextual experience | ✅ — experience scales production |
| Training | ⚠️ partial — auto-training via ticking, no explicit player-driven training |
| Knowledge | ❌ — out of scope |
| Personal history | ❌ — out of scope |
| Culture | ❌ — out of scope |
| Citizen lineage and profile | ✅ — eight qualitative lineages and the complete validated `CitizenProfile` are persisted and presented; future skill effects remain out of scope |
| Body variant (gender) is an explicit player choice | ✅ — `GenderId` carried by `CitizenProfile`, resolved by `CharacterVisualRegistry` |
| Health | ❌ — out of scope |
| Relationships | ❌ — out of scope |
| Potential | ❌ — out of scope |
| Heroes emerge from environment + experience, not random | ✅ — hero is just a `RoleId.Hero` role grant, not a class |

### 4.6 Combat, defeat, healthcare (§6)

❌ **Out of scope** for the first prototype.

### 4.7 Production and storage (§7)

| Spec item | Status |
|---|---|
| Configurable production chains | ❌ — `Building.AddStock` is single-resource; Smithy/Weaver chains are future work |
| Stops when materials / workers / storage missing | ✅ — production gate checks operating-recipe inputs; emits `WorldEventKind.ProductionBlocked` with `CauseEventId` when inputs are short |
| Time does not magically reduce efficiency | ✅ — efficiency is a function of competency experience, not elapsed time |
| A well-configured city may improve while the player is absent | ✅ — experience grows, which compounds the rate |
| Reactive min / max stock range | ✅ — `Building.CanProduce` and `ResumeIfBelowMin` drive the loop |
| Policy applied to a fixed stockpile (`Min == Max`) | ✅ — allowed by design |

### 4.8 Persistent time (§8)

| Spec item | Status |
|---|---|
| Save world state | ✅ |
| Save timestamp of last update | ✅ — `LastSeenAtUnixMillis` |
| Calculate elapsed time | ✅ |
| Process changes through discrete events | ⚠️ partial — we tick once per second of real time, not via a domain-level event log |
| Avoid simulating every individual second | ✅ — capped at 1 Hz and at 7 days |
| **Generate a causal report** | ⚠️ partial — catch-up returns chronological events and the UI groups blocked decisions, but the bounded log is not persisted and does not yet represent a multi-day history |

The causal primitive described in `ARCHITECTURE.md` §10 exists, but the current
log is bounded, in-memory, and cleared on restore. Offline progression still
iterates deterministic ticks; persisted/streamed long-horizon events remain open.

### 4.9 Design principles (§9)

| Principle | Status |
|---|---|
| 1. One city, one story | ✅ |
| 2. No artificial penalties for absence | ✅ |
| 3. No sovereign decisions without authorization | ✅ — every assignment requires `TryAssignCitizen` |
| 4. No single overall level | ✅ architecturally — no "city level" exists |
| 5. No arbitrary unlocks | ⚠️ — Basic Shelter requires explicit authorisation, but richer material/knowledge conditions are still pending |
| 6. No random loot | ✅ (nothing exists to be looted yet) |
| 7. No invisible death | ✅ trivially (no death yet) |
| 8. No instant healing | ✅ trivially (no healing yet) |
| 9. No magic-string efficiency | ✅ — production efficiency is driven by typed competency data; remaining UI text, summaries, IDs, and paths are outside the formula |
| 10. No single correct model of development | ✅ — `BuildingKind` distinguishes current kinds while resource and competency data remain independent |
| 11. Causality over randomness | ✅ — `ProductionPerTick` is pure-deterministic, no `Random` anywhere |
| 12. Composition over inheritance | ✅ |
| 13. Domain is not presentation | ✅ source-scanned by `DomainBoundaryTests` |
| 14. Originality | ✅ — all current names are documented as provisional per `PROVISIONAL_NAMES` |

---

## 5. Outstanding gaps, ranked by leverage

What would unlock the most future work for the smallest change?

| Rank | Gap | Suggested slice | Effort |
|---|---|---|---|
| 1 | Snapshot projections now expose the required state, but blocked/empty/decision states still need a consistent visual hierarchy and focus verification. | **Next slice — UI state visibility.** Render explicit actionable states, then verify keyboard/gamepad focus and multiple aspect ratios. | M |
| 2 | Skill formulas are absent: lineage and personal aptitudes have no effect on early learning, errors, retention, or production. The bible warns against turning lineage into a permanent bonus; the right shape is small, qualitative hooks that experience and tools overwhelm. | **Following product slice — Skill-system hook.** Land one or two early-learning effects derived from `CitizenProfile`, gated by the lineage guardrail in the bible. | L |
| 3 | Production chains are still single-resource. Smithy consuming Iron to produce Tools, Weaver consuming Food to produce Cloth, PotionLab consuming Herbs to produce Potions. | **Production chains.** Add one building that consumes one resource and produces another; share the existing drawdown machinery. | L |
| 4 | Shared inventory abstraction. Iron lives on each Quarry, Food on each Farm, Wood on each Forest. Recipes must source from any matching building, which works today but means a city aggregate counter would be cleaner. | **Shared inventory.** Replace per-building reserves with one `CityWorld.Resource(type)` aggregate; per-building reserves become optional capacity hints. | M |
| 5 | Rich condition gates on unlocks. Today, only material inputs gate a construction; the bible calls for knowledge, institution, and authorisation gates too. | **Knowledge and institution gates.** Extend `TryAuthorizeConstruction` with a pre-flight that checks accumulated knowledge and active institutions. | M |
| 6 | The expedition pillar (§4.2) has no architectural place to live. | **Expedition skeleton.** Add `Domain/Expedition/` with the minimum types an expedition needs (member, target, route, outcome). No gameplay yet — just enough to prove the seams. | M |
| 7 | Causal event log covers the current prototype actions; it is not yet the complete long-horizon event model described by the design bible. | **Long-horizon events.** Replace the bounded `WorldEventLog` with a streamed / persisted event store and surface decisions over multi-day catch-ups. | L |

---

## 6. Concrete observations

These are things the validation surfaced that aren't quite "gaps"
but are worth knowing:

1. **The causal primitive already exists.** Production and blocking events
   carry causal links. The next refinement is persistence and richer event
   coverage, not another in-memory log.
2. **The macro view now reflects the current population.** The hero-only
   onboarding state intentionally has no building plot and shows an explicit
   empty state. Building plots return when construction exists.
3. **Persistence uses one primary slot.** `CityWorldController` loads
   `WorldPersistence.PrimarySaveSlot`, migrates raw v2/v3 data to v4, validates it, and
   leaves an incompatible v1 slot untouched while onboarding is incomplete.
4. **`MainLoop.NotificationWMCloseRequest` is not exposed as a
   named constant in Godot 4.7 C# bindings.** We use the literal
   `1006` with a comment citing the `main_loop.h` value. When Godot
   generates the constant, swap it in. Already documented in
   `CityWorldController._Notification`.
5. **`SceneTree.QuitRequested`** also doesn't generate a C# event
   in 4.7 (it's a signal but the generator skips it). Same fix path.
6. **Macro activity is population-derived.** `MacroCitizenActivity.Populate`
   receives the current citizen count. It remains non-interactive, but it no
   longer encodes a fixed decorative population.

---

## 7. Summary

| Area | Verdict |
|---|---|
| Original slice scope | ✅ all bullets met (incl. gender-aware onboarding, walking hero, forest gathering) |
| Architecture compliance | ✅ Domain boundary is now enforced by an automated source scan |
| GAME_VISION §1–3 (fantasy, single city, absence) | ✅ met |
| GAME_VISION §4.1 (city development) | ✅ materials landed; chains and shared inventory pending |
| GAME_VISION §4.2 (expeditions) | ❌ missing |
| GAME_VISION §5 (citizens, partial) | ✅ gender landed; long attributes list pending |
| GAME_VISION §6 (combat, healthcare) | ❌ out of scope |
| GAME_VISION §7 (production & storage) | ✅ reactive range + materials landed; chains pending |
| GAME_VISION §8 (persistent time) | ⚠️ causal in-session/catch-up report exists; persisted long-horizon history pending |
| GAME_VISION §9 (design principles) | ✅ including an enforced Domain/presentation boundary |
| Slice 7 — First MVP pixel art | ✅ done — Home (64×64), Quarry (128×128), Farm (128×128) placeholders wired through `BuildingArt`; old generic `building_placeholder.png` and `worker_placeholder.png` removed |
| Recipes slice | ✅ recipe/deposit/drawdown pass; cancellation never credits inputs that were not consumed |
| Gender slice | ✅ done — `GenderId` on profile; v3 → v4 migration; body variant chosen in onboarding step 0 |
| Forest/Wood slice | ✅ done — 2 founding Forests, manual gather, Basic Shelter requires 4 wood |
| Tests | 309 / 309 passing |
| Build | clean, 0 warnings, 0 errors |

**Net read:** the gameplay proof is vertically broad and well tested. The strict
domain boundary and construction resource conservation are now guarded by tests.
The vision as a
whole is **far from done** — expeditions, multi-dimensional city
development, healthcare, chains, shared inventory — but each of
those is a clearly-scoped next slice, and none of them is blocked
by what exists today.

The top three investments, in priority order:

1. **UI state visibility.** Use the controller-owned snapshots to make
   blocked/empty/decision states directly visible and navigable.
2. **Skill-system hook.** Add small qualitative effects that experience and
   tools overwhelm, without permanent lineage bonuses.
3. **Production chains.** Prove one input-to-output chain before generalising
   shared inventory.

These are recommendations, not commitments. The user / next agent
decides what to build next.
