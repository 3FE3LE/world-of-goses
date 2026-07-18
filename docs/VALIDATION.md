# Validation: end-to-end against GAME_VISION and ARCHITECTURE

> Snapshot of how the current slice holds up against the project's
> own documents. Written as part of Slice 8 (end-to-end validation).
> It is **not** a test suite and it is **not** aspirational — it is
> an honest cross-check of what code exists today against what was
> promised. Markers: ✅ implemented · ⚠️ partial / shape only ·
> ❌ missing / out of scope.

---

## 1. Original slice scope (per `README.md` §13)

The first prototype scope is expanded below into verifiable criteria:

| Criterion | Status | Where |
|---|---|---|
| Macro city view with selectable building placeholders and a small amount of decorative citizen activity | ✅ | `CityMacroView` with `MacroCitizenActivity` (decorative 6×6 dots), plus clickable `BuildingPlot`s for the Quarry and Farm |
| Detailed building view with configurable visual worker limit | ✅ | `BuildingPlot` + `BuildingDetailView` + `VisibleWorkerSlots`, with `VisualCapacity` enforced by `Building` |
| Visible worker entry / exit transitions | ✅ | `VisibleWorkerSlot` runs `AnimationPlayer` with `AnimEntry`/`AnimWork`/`AnimExit`. Placeholder sprite only — final art pending the pixel-art slice |
| Individual citizen records shared between views; the citizen is the only person entity (roles, competencies, recognitions are attached concepts, not subclasses) | ✅ | `Citizen` is a single sealed class; roles + competencies attach to it. No subclasses |
| Worker assignment / removal with a deterministic production counter that responds to current assignment | ✅ | `CityWorld.TryAssignCitizen` / `TryUnassignCitizen`, deterministic `BuildingProductionCalculator` driven by `baseProduction + floor(base × 0.05 × exp)` |

**Scope verdict:** all slice-scope criteria are met. Visual fidelity is pending the pixel-art slice; mechanics are wired.

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
| Domain (no Godot.*) | ✅ `game/scripts/Domain/` has zero `using Godot;` |
| Godot representation | ✅ `game/scripts/*.cs` (presentation scripts) + `game/scenes/*.tscn` |
| Assets | ⚠️ placeholders only — final art pending Slice 7 |
| Local persistence | ✅ implemented (`game/scripts/Domain/Persistence/`) |
| Tests | ✅ `tests/WorldofGoses.Tests/` — **108 / 108 passing** at this snapshot |

### 3.2 The Godot/.NET boundary (§5)

- `partial class` used where required by source generators
- Domain code does not import `Godot.*` (verified by grep)
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
| Macro | ✅ implemented (`MacroCitizenActivity` — decorative, not bound to `CitizenId` per the spec) |
| Building-detail | ✅ implemented (`VisibleWorkerSlots` — each visible worker **is** bound to a `CitizenId`) |
| Expedition-detail | ❌ not implemented (out of scope for the first prototype, called out in the doc itself) |

### 3.5 Composition over inheritance (§9.12 of vision)

✅ Holds. `Citizen` and `Building` are sealed domain classes configured through composed data, not specialised subclasses. The controller owns one `CityWorld` with multiple buildings and citizens.

### 3.6 "Domain is not presentation" (§9.13)

✅ Holds. Domain types (`Building`, `Citizen`, `CityWorld`, `OfflineProgression`, `WorldPersistence`) compile and run without any Godot binary. Tests prove this (108 / 108 passing without Godot loaded).

---

## 4. GAME_VISION alignment (`docs/GAME_VISION.md`)

### 4.1 Main fantasy (§1)

> "A single living city that grows because of decisions made by a
> player who is not always present."

✅ Achieved within scope: the world advances on its own during
play (manual `AdvanceProduction` click + offline tick catch-up on
load). What isn't implemented yet is the *richness* of what can
happen — for now, only stone-mining and food-farming exist.

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

⚠️ **Shape only.** The architecture supports multi-dimensional
city development — `BuildingKind` already lets the same surface
produce different resources. What is missing:
- Explicit "dimensions" object (age, culture, politics, economy,
  geography, demographics, professions, knowledge redundancy,
  institutions, generations).
- Production chains (not single-resource ticking).
- Conditions on building unlocks (currently everything is seeded).

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
| Species or race | ❌ — no lineage yet (`DESIGN_INFLUENCES.md` §5 lists three MVP lineages as future work) |
| Health | ❌ — out of scope |
| Relationships | ❌ — out of scope |
| Potential | ❌ — out of scope |
| Heroes emerge from environment + experience, not random | ✅ — hero is just a `RoleId.Hero` role grant, not a class |

### 4.6 Combat, defeat, healthcare (§6)

❌ **Out of scope** for the first prototype.

### 4.7 Production and storage (§7)

| Spec item | Status |
|---|---|
| Configurable production chains | ❌ — current `Building.AddStock` is a single-resource tick |
| Stops when materials / workers / storage missing | ⚠️ partial — stops on storage capacity only |
| Time does not magically reduce efficiency | ✅ — efficiency is a function of competency experience, not elapsed time |
| A well-configured city may improve while the player is absent | ✅ — experience grows, which compounds the rate |

### 4.8 Persistent time (§8)

| Spec item | Status |
|---|---|
| Save world state | ✅ |
| Save timestamp of last update | ✅ — `LastSeenAtUnixMillis` |
| Calculate elapsed time | ✅ |
| Process changes through discrete events | ⚠️ partial — we tick once per second of real time, not via a domain-level event log |
| Avoid simulating every individual second | ✅ — capped at 1 Hz and at 7 days |
| **Generate a causal report** | ⚠️ basic — the macro view shows `+N stone mined, +N exp per worker`. The vision's example report (`08:00 / 10:00 / 11:30 / ...`) is richer than what we produce |

The event-based domain-level simulation (per `ARCHITECTURE.md` §9) is **not** implemented — offline progression batches ticks rather than producing causal events. That is the natural next slice after multi-building and persistence.

### 4.9 Design principles (§9)

| Principle | Status |
|---|---|
| 1. One city, one story | ✅ |
| 2. No artificial penalties for absence | ✅ |
| 3. No sovereign decisions without authorization | ✅ — every assignment requires `TryAssignCitizen` |
| 4. No single overall level | ✅ architecturally — no "city level" exists |
| 5. No arbitrary unlocks | ⚠️ — currently no unlocks at all (seeded only); future slice must build unlock conditions, not gates |
| 6. No random loot | ✅ (nothing exists to be looted yet) |
| 7. No invisible death | ✅ trivially (no death yet) |
| 8. No instant healing | ✅ trivially (no healing yet) |
| 9. No magic-string efficiency | ✅ — only `string` left is the `WorldSave.Kind` for forward-compat enum serialization, well-justified |
| 10. No single correct model of development | ✅ — `BuildingKind` distinguishes current kinds while resource and competency data remain independent |
| 11. Causality over randomness | ✅ — `ProductionPerTick` is pure-deterministic, no `Random` anywhere |
| 12. Composition over inheritance | ✅ |
| 13. Domain is not presentation | ✅ |
| 14. Originality | ✅ — all current names are documented as provisional per `PROVISIONAL_NAMES` |

---

## 5. Outstanding gaps, ranked by leverage

What would unlock the most future work for the smallest change?

| Rank | Gap | Suggested slice | Effort |
|---|---|---|---|
| 1 | Causal report (per vision §8 / arch §9) is still a single string. The right primitive is a domain-side event log that ticks emit to. | **Slice 9 — Event-log domain primitive + causal report.** Open a slice to make `OfflineProgression.Apply` return `IReadOnlyList<WorldEvent>` instead of a flat report; renderer reads events to build the human-readable summary. | M |
| 2 | Citizens are flat records beyond name / competencies / role. The vision enumerates a long list of citizen attributes (health, culture, relationships, …). | **Slice 10 — Extend the citizen attachment model.** Add `CitizenHealth`, `CitizenRelationships`, etc. as separate value objects composed onto `Citizen`, mirroring how the existing `Competencies` and `Roles` work. | L |
| 3 | Unlocks (§9.5): currently no building is "unlocked" at all — they all exist in the seed. | **Slice 11 — Conditions-as-data for building unlocks.** Define a `Prerequisite` value object that a `Building` references; city state evaluates prerequisites; UI surfaces them. | L |
| 4 | The expedition pillar (§4.2) has no architectural place to live. | **Slice 12 — Expedition skeleton.** Add `Domain/Expedition/` with the minimum types an expedition needs (member, target, route, outcome). No gameplay yet — just enough to prove the seams. | M |
| 5 | The lineage + founder system (`DESIGN_INFLUENCES.md` §5–6) is not in code. | **Slice 13 — Lineage as a `Citizen` attachment.** Mirrors how `Role` and `Competency` attach. Founder choice stored in `WorldSave`. | M |

---

## 6. Concrete observations

These are things the validation surfaced that aren't quite "gaps"
but are worth knowing:

1. **Causal granularity already exists in the data.** Every
   production tick emits both a stone delta and a per-worker exp
   bump. The event-log primitive would just be the next refinement,
   not a from-scratch system.
2. **The macro view's hint label says "Click a building to manage
   its workers".** That's accurate today but undersells the seed:
   with the Quarry and Farm both clickable, the player can be told
   that each building produces a different resource. Cheap visual
   polish, not a code change.
3. **Persistence uses one primary slot.** `CityWorldController` loads
   `WorldPersistence.PrimarySaveSlot`, validates it before restore and
   retains the seeded world if the file is missing or invalid.
4. **`MainLoop.NotificationWMCloseRequest` is not exposed as a
   named constant in Godot 4.7 C# bindings.** We use the literal
   `1006` with a comment citing the `main_loop.h` value. When Godot
   generates the constant, swap it in. Already documented in
   `CityWorldController._Notification`.
5. **`SceneTree.QuitRequested`** also doesn't generate a C# event
   in 4.7 (it's a signal but the generator skips it). Same fix path.
6. **Slice-scope claim about "decorative macro dots":**
   `MacroCitizenActivity.Populate` lays 6 dots in deterministic
   positions from a formula. They're not bound to `CitizenId` per
   the architectural spec. ✅ correct, but the docstring and the
   test don't call this out — the next reviewer might assume
   they're bound. Worth adding a one-line comment.

---

## 7. Summary

| Area | Verdict |
|---|---|
| Original slice scope | ✅ all 4 bullets met |
| Architecture compliance | ✅ full (Domain / presentation split, composition, three scales, no Godot in domain) |
| GAME_VISION §1–3 (fantasy, single city, absence) | ✅ met |
| GAME_VISION §4.1 (city development) | ⚠️ shape only — no dimensions, no chains |
| GAME_VISION §4.2 (expeditions) | ❌ missing |
| GAME_VISION §5 (citizens, partial) | ⚠️ core attachments done; long attributes list pending |
| GAME_VISION §6 (combat, healthcare) | ❌ out of scope |
| GAME_VISION §7 (production & storage) | ⚠️ basic ticking only; no chains / materials |
| GAME_VISION §8 (persistent time) | ⚠️ save/load/elapsed ✅; causal report ⚠️ basic |
| GAME_VISION §9 (design principles) | ✅ 13 / 14 fully held; 1 partial (unlocks — none exist yet) |
| Tests | 108 / 108 passing |
| Build | clean, 0 warnings, 0 errors |

**Net read:** the slice is *vertically complete* against the
prototype promise and the architectural boundary. The vision as a
whole is **far from done** — expeditions, multi-dimensional city
development, healthcare, causal reports, lineaging — but each of
those is a clearly-scoped next slice, and none of them is blocked
by what exists today.

The top three investments, in priority order:

1. **Slice 9 — Event-log primitive + causal report.** Most
   leverage: it turns the current offline-progression summary
   into the event-stream model the architecture already prescribes
   and the vision already illustrates.
2. **Slice 12 — Expedition skeleton.** Most gameplay leverage:
   the second pillar is the other half of the design.
3. **Slice 11 — Unlock conditions.** Most architectural leverage:
   it cements the "no arbitrary unlocks" principle and opens the
   door to additive content.

These are recommendations, not commitments. The user / next agent
decides what to build next.
