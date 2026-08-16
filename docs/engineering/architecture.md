# Architecture

> The single technical reference for World of Goses: the layers, the
> boundaries they enforce, and how each one is actually enforced. It
> describes the code that exists. Product canon lives under
> [`docs/systems/`](../systems/) and [`docs/world/`](../world/); ownership of
> mutable state lives in [`state-authority.md`](state-authority.md).

## 0. The four layers

```text
Domain          decides what happens and why      (engine-free)
Application     orchestrates use cases            (engine-free)
Persistence     serialises and migrates snapshots (engine-free)
Presentation    shows state and takes input       (Godot)
```

Dependencies point one way only, and the direction is enforced by project
references rather than by review:

| Assembly | References | Forbidden — and why it cannot happen |
|---|---|---|
| `WorldofGoses.Domain` | (none) | Godot, Application, Persistence: no `ProjectReference`, so `using Godot` is a **compile error** |
| `WorldofGoses.Application` | Domain | Godot, Persistence: same mechanism |
| `WorldofGoses.Persistence` | Domain | Godot, Application: same mechanism |
| `game/World of Goses` (Godot) | Domain + Application + Persistence | — (root assembly) |

The three engine-free `.csproj` files live under `src/`; their sources stay at
`game/scripts/Domain`, `game/scripts/Application` and are linked in. Keeping
the project files outside `game/` is what matters: it puts `bin/` and `obj/`
where the engine never scans them.

`CityGameSession` (Application) owns the `CityWorld` aggregate. The controller
holds only the session. Production scenes never touch `CityWorld`: every read
goes through an immutable snapshot record, every write through a session
command that returns a semantic `*Result`.

### How each boundary is enforced

The distinction matters, because "documented" and "enforced" are not the same
strength of guarantee:

| Level | Means | Examples |
|---|---|---|
| **Compiler** | A violation does not build. Strongest; no maintenance. | Engine-free assemblies cannot see Godot. Domain cannot see Persistence. `ResourceTypeLocalizer.Key` throws on an unmapped value. |
| **Test (semantic)** | A violation fails a test that reasons about behaviour. | Live/offline equivalence, exactly-once expedition resolution, stable save ids, i18n keys present in every catalog. |
| **Test (source-text)** | A violation fails a regex scan of the sources. Catches shapes the type system cannot express; can be fooled by an unusual spelling. | `Presentation_DoesNotAccessCityWorldDirectly`, `Ui_DoesNotHardcodeInputActionStrings`, `ProductionUi_DoesNotComposeStaticHierarchyInCode` |
| **Allowlist** | The rule holds everywhere except named files, each with an inline reason. | `ArchitectureBoundaryAllowlist` |
| **Process (CI)** | Enforced by running the real thing. | `tools/Test-GodotBoot.ps1` launches the production main scene headless; the build runs `--no-incremental -warnaserror` |
| **Review only** | Written down, not mechanised. | compact-HUD metric tokens ([`../presentation/ui-patterns.md`](../presentation/ui-patterns.md) §5.0) |

A green suite is not evidence that the game runs: the boot check exists because
A0–A12 once left every guard passing while the shipped game could not start at
all. Anything that only a launch can prove belongs in CI as a launch.

## 1. Goals of the architecture

The architecture has three goals, in order:

1. **Keep the simulation independent of the engine.** Domain types do
   not import `Godot.*`. Domain logic can be unit-tested without the
   engine running.
2. **Make one persistent city tractable.** The code is structured so
   that the two gameplay pillars — city development and expeditions —
   can grow without leaking into each other.
3. **Stay small.** No backend, no microservices, no premature patterns.

## 2. Layers

The layers are **assemblies**, not a convention. Three projects, and the
arrows are the only direction references are allowed to point:

```
  src/WorldofGoses.Domain          Microsoft.NET.Sdk · no GodotSharp
  (game/scripts/Domain/)           the rules, the clock. BCL only.
          ▲                        ↑
          │                        │  Persistence sees Domain internals
          │                        │  through InternalsVisibleTo; the
          │                        │  dependency arrow only goes one way.
          │                        │
  src/WorldofGoses.Persistence     Microsoft.NET.Sdk · no GodotSharp (A6)
  (src/WorldofGoses.Persistence/)  the *Save DTOs, JSON, migrations,
          ▲                        validation, mapper, atomic slot writes
          │                        and .bak sidecar. Domain never references
          │                        this assembly.
          │
  src/WorldofGoses.Application     Microsoft.NET.Sdk · no GodotSharp
  (game/scripts/Application/)      the use cases and the snapshots: read models
          ▲                        that project domain state into what a view
                                   renders, and the engine-free commands that
                                   coordinate one or more domain operations
                                   into a single player intent (A5).
          │
  game/World of Goses.csproj       Godot.NET.Sdk
  (everything else under           scenes, nodes, input, animation, audio,
   game/scripts/)                  and the controller that drives the world.
```

`using Godot` in either of the top two projects is a **compile error**, not a
review comment. That is the whole reason they are separate: the rule used to
be a test that grepped the sources, which catches the honest mistake and
nothing else.

Two consequences worth knowing before you fight them:

- **`internal` in the domain now means something.** It is invisible to
  presentation. Six members were being reached into from the HUD when the
  boundary went up; each was then either promoted to `public` with a doc
  comment saying why, or kept `internal` with the operation moved inside the
  domain where it belonged. Do the same with the next one rather than
  reaching for `InternalsVisibleTo`, which is granted to the test project
  only.
- **A snapshot cannot call `UiText`.** It needs Godot's `TranslationServer`,
  so it will not compile in the application assembly. Translate at the
  `Control` that displays the value. This used to be a runtime crash in the
  Godot-free unit tests; it is now caught at build time.

The simulation does **not** depend on sprites, cameras, or animations. The
visual representation reacts to domain state.

### Godot vs C#

Two questions, and they have different answers.

**Scene, Theme, StyleBox, Inspector** — for what a thing *is*: static layout,
containers, typography, padding and spacing, colours, borders, reusable
styles, assets, and any value a designer should be able to change without a
compiler. A `Control` whose position is arithmetic in `_Ready` is almost
always a `Container` someone did not reach for.

**C#** — for what a thing *does*: behaviour, state, orchestration, calls into
the application layer, runtime binding, genuinely dynamic UI (lists whose
length comes from data), and engine lifecycle integration.

The dividing line is not "how much C#" but whether the value is a decision or
a consequence. `Tokens.ScrollGutter` is a decision and is named once;
where a row ends up on screen is a consequence and belongs to a container.

## 3. Technology choices

- **Engine:** Godot `.NET` 4.7.x.
- **Language:** C# 12 on `.NET 8.0` (Android export: `net9.0`).
- **Editor:** Visual Studio Code.
- **Pixel art:** Pixelorama.
- **Persistence:** Local, versioned JSON snapshots with atomic replacement.
- **Backend:** Not implemented yet. None planned for the prototype.

The choices are stated in `README.md`. They will be revisited only when a
concrete need appears.

## 4. Project layout

```
world-of-goses/
├── .git/
├── AGENTS.md
├── README.md
├── .gitignore
├── docs/
│   ├── README.md          # documentation index
│   ├── systems/           # what each game system is and its invariants
│   ├── world/             # vision, pillars, lineages
│   ├── presentation/      # visual language, UI patterns, audio, art pipeline
│   ├── engineering/       # this file, state authority, conventions, verification
│   ├── ai/                # agent routing layer
│   ├── history/           # decision records
│   └── session-state/     # generated measurement + dated frame
├── src/                   # engine-free .csproj files (Domain, Application, Persistence)
├── art/
│   ├── source/        # Pixelorama sources
│   │   ├── characters/
│   │   ├── buildings/
│   │   ├── terrain/
│   │   ├── effects/
│   │   └── ui/
│   ├── references/    # Mood boards, inspiration (not game art)
│   └── exports/       # PNG / sprite sheets exported from Pixelorama
├── game/
│   ├── project.godot
│   ├── World of Goses.csproj
│   ├── World of Goses.sln
│   ├── assets/        # Imported PNG / audio used by Godot
│   │   ├── characters/
│   │   ├── buildings/
│   │   ├── terrain/
│   │   ├── effects/
│   │   ├── audio/
│   │   └── ui/
│   ├── scenes/        # .tscn files
│   └── scripts/       # .cs files
└── tests/
    └── WorldofGoses.Tests/  # xUnit domain/persistence tests
```

`game/.godot/`, `game/bin/`, `game/obj/`, `.vscode/`, `*.tmp`,
`*.autosave`, `Thumbs.db`, `.DS_Store`, `*.exe`, `*.dll`, `*.pck`, and
similar artifacts are ignored by `.gitignore`.

## 5. The Godot `.NET` boundary

Godot 4.7 `.NET` uses C# source generators that emit `*.<Name>.cs`
partials. The agents and humans working on this project must:

- Never edit generated partials.
- Use `partial` only when required by the engine.
- Keep the domain free of `Godot.*` references.

The boundary in practice:

- **Domain layer:** pure C#. Holds entities, value objects, state
  machines, and rules. No nodes, no scenes, no signals.
- **Presentation layer:** Godot nodes. Reads domain state, drives
  animations, plays sounds, listens for input. Thin adapters.
- **Adapters:** small classes that translate between domain events and
  Godot signals, and between user input and domain commands.

`CityWorld` is the public aggregate facade, not the required home of every rule.
`CitizenAssignmentService` owns building/project assignment consistency,
citizen location transitions, and auto-release while operating only on
collections owned by the aggregate. `BuildingProductionSimulation` owns the
productive tick sequence: recipe gate, food/regeneration, stamina cost,
contributors, output, experience, and stop cause. Resource transfer and causal
log ownership stay in `CityWorld` through narrow delegates until a resource
ledger and an event store own them outright.
`ConstructionSimulation` owns work-interval material drawdown and rollback,
stamina recovery/cost, contribution, project stop causes, blocking events, and
night rest. Authorisation and the final project-to-building transition remain
aggregate operations in `CityWorld`.
Presentation and controllers continue calling `CityWorld`; collaborators are
not service locators and are not exposed across the Godot boundary. Further
extraction requires a concrete slice; the aggregate still intentionally owns
resource topology, causal history, persistence restore, and orchestration.

**A5 — Application facade.** A5 turns the Application assembly into the real
use-case boundary between Godot and the domain. The single sealed class
`CityGameSession` (in `game/scripts/Application/CityGameSession.cs`) is the
only caller of `CityWorld` for gameplay commands and snapshot queries:
construction authorisation, citizen assignment, production policy, expedition
start/cancel/skill, cultivation, gathering, tool crafting, first-night
dialogue, and the world tick itself. `CityWorldController` constructs one
session per controller instance and reduces to a Godot adapter — input →
session call → translate outcome → `EmitSignal`. The session is engine-free,
owns no Godot types, returns the existing `*Result` types and immutable
snapshots, and exposes no `Execute(Action<CityWorld>)`, `GetWorld()` or
`WithWorld(...)` escape hatch. `CityWorld` ownership still lives on the
controller for now; A6/A7 will move it into the session and remove the
`internal CityWorld World => _world` test seam. `UseCaseDelegationTests`
(the new A5 guardrail) lists every use-case method on the controller and
asserts each one's body routes through `_session.<Name>`; a new command
added directly to the controller fails the build before review.

**A6 — Persistence extraction.** A6 moves persistence out of Domain and into
its own engine-free assembly (`src/WorldofGoses.Persistence`). The single
dependency arrow allowed between the two layers is `Persistence → Domain`;
Domain never references Persistence, enforced by
`ArchitectureBoundaryTests.Layer_DoesNotReferencePersistenceAssembly`.
The `*Save` DTOs, `WorldSave` itself, `WorldPersistence`, the v2→v34
migration chain, validation, and the JSON serializer all relocate. The
restore orchestration that lived on `CityWorld.Restore(WorldSave)` moves
to `WorldSaveApplier.ApplyTo(world, save)` in Persistence; `CityWorld`
exposes the few internal fields and helpers (`_citizens`, `_buildings`,
`RegisterBuilding`, `MobiliseForDay`, `ResourcePositionIndex`,
`SetPendingProspectForRestore`, …) through `internal` so the applier
can drive the world without duplicating logic. `WorldPersistence` stays
as the public facade (A6 spec: "Mantener una facade WorldPersistence
temporal si reduce el ruido de call sites") and `ArchitectureBoundaryTests`
extends `EngineFreeProject_DoesNotReferenceGodot` to the new assembly. No
JSON shape, schema version, migration semantic, or slot behaviour was
touched; A6 is mechanical extraction. A7 inherits the controller's direct
persistence seam and will move the slot/save orchestration into the
`CityGameSession` facade.

**A7 — Stable IDs and semantic restore contract.** A7 freezes the wire
IDs of every persisted enum family so renaming a C# enum value does
not silently change the save format. The pattern is one small static
mapper per enum family under
`src/WorldofGoses.Persistence/Ids/` (e.g. `BuildingKindSaveIds`,
`ResourceTypeSaveIds`, `FirstNightStageSaveIds`). Every Capture and
Restore site for a persisted enum goes through its `*SaveIds.ToId`
or `*SaveIds.TryParse`.

**Precisely what is enforced, corrected by the exit gate.** This paragraph
used to claim that "the raw `Enum.ToString()` calls that used to drive
persistence are gone". They are not: 27 of the 30 mappers under
`Persistence/Ids/` still end in `_ => value.ToString()`, and five derive
every id that way. The source-text guardrail
(`NoCaptureOrApplier_CallsEnumToStringDirectly_ForPersistedEnums`) scans two
files — `WorldPersistence.cs` and `WorldSaveApplier.cs` — for the
`EnumType.Member.ToString()` shape, and never looked at the mappers at all.

What actually protects the invariant is the frozen value table.
`StableSaveIdContractTests` pins the exact wire string of every persisted
value with `[InlineData]`, so **renaming** a C# enum member makes `ToId`
return the new name while the table still expects the old one, and the suite
goes red. That is the guarantee A7 exists to give, and it holds.

The hole was **adding** a member: with no frozen row and a `ToString()`
fallback, a new value silently took its C# name as its wire id and the first
save written with it made that permanent.
`PersistedEnumFamily_HasNoUnfrozenMembers` closes it by asserting each
family's member count against its frozen-row count, so a new member fails
until someone decides its id deliberately. The gate verified every covered
family's table is complete today.

Making the mappers exhaustive — so the fallback cannot exist — remains open
debt rather than a claimed guarantee.

A7 also introduced a `WorldRestoreState` semantic type in Domain, with
the intended flow `WorldSave` → Persistence translation →
`WorldRestoreState` → Domain restore. **That type was never wired up and
has been removed.** It had zero references outside its own file for the
whole of A7–A12 while the actual restore ran through
`WorldSaveApplier`, and a documented architecture that is not the
architecture executing is worse than no document.

The restore flow that actually runs, and is now the documented one:

```
WorldSave (JSON DTO, Persistence)
    │  WorldPersistence.Validate  → schema + migration chain (v2 → v34)
    ▼
WorldSaveApplier.ApplyValidatedTo
    │  *SaveIds.TryParse per persisted enum family
    │  EconomicBalanceVersion fixups for pre-balance saves
    │  preflight rehydration into an isolated candidate world
    ▼
CityWorld internal collections (Domain)
```

The exit gate considered completing the `WorldRestoreState` boundary
instead of deleting it, and rejected it on ownership and maintainability
grounds rather than effort:

- **Ownership is already correct and compiler-enforced.** Domain has no
  `ProjectReference` to Persistence, knows nothing of `WorldSave`, JSON,
  or save IDs, and never reaches outward. `WorldSaveApplier` reaches *in*
  through `InternalsVisibleTo`. Inserting `WorldRestoreState` would not
  change who depends on whom; it would only change the shape of the write.
- **It would be a third parallel object model.** `RestoredBuilding`
  duplicated `BuildingSave` field for field, and roughly thirty such
  records duplicated the rest. Every new persisted field would need four
  edits (DTO, mapper, restore record, domain restore) instead of two, and
  every drift between the three models is a silent load bug.
- **No validation would move.** The migration-era fixups keyed on
  `EconomicBalanceVersion` are persistence concerns about old saves on
  disk. They would still run while building the restore record, so Domain
  would receive already-migrated data either way.

What the deletion costs is that `CityWorld`'s collections stay `internal`
rather than `private`. That is a visibility nicety already fenced by an
assembly boundary whose `InternalsVisibleTo` grants are exactly two
(`WorldofGoses.Tests`, `WorldofGoses.Persistence`), both listed in
§"Remaining internal seams".

The invariant A7 actually delivers stands unchanged and is the one worth
having: renaming a C# enum value cannot change the save format, because
every persisted enum goes through a `*SaveIds` mapper and
`StableSaveIdContractTests` freezes the wire strings.

**A9 — First Night typed integration.** A9 closes the last dynamic
dispatch seam between presentation scenes. `FirstNightScene` used to
reach the macro view's founder and campfire anchors through
`HasMethod(methodName)` + `Node.Call(methodName, …)`, and refreshed
its cached positions every frame inside `_Process` whether the camera
moved or not. The macro view now exposes the anchors as a typed C#
record (`WorldDialogueAnchors`) and a typed Godot signal
(`WorldDialogueAnchorsChanged`); `FirstNightScene` subscribes once and
calls the macro view's typed methods
(`GetFoundingArrivalGlobalPosition`, `GetBuildingGlobalPosition`)
directly. Camera and projection changes (lateral pan, depth change,
zoom, follow toggle, reset, building-entry zoom animation) raise the
signal from the macro view's own state-change sites, so the night
scene refreshes only when the projection actually moved. The
`FirstNightScene._Process` method is gone: visual flicker and
animation stay inside `FireSpiritVisual`'s own `_Process`, layout
clamping stays inside `FirstNightSpeechBubble.FollowSpeaker`. The new
guards are:

- `FirstNightScene_DoesNotUseDynamicDispatch` — `HasMethod` and
  `node.Call("name", …)` patterns fail the build. Allowlist is empty
  by design; there is no legitimate use of dynamic dispatch in the
  night anymore.
- `FirstNightScene_SubscribesToTypedAnchorSignal` — the scene must
  subscribe to `WorldDialogueAnchorsChanged` and reference both typed
  anchor getters.
- `MacroStreetLiveView_ExposesTypedAnchorSignal` — the macro view
  declares the typed signal and emits it through
  `EmitSignal(SignalName.WorldDialogueAnchorsChanged)`.

`UpdateFounderPosition(Vector2)` and `UpdateCampfirePosition(Vector2)`
on `FirstNightScene` are now `[Obsolete]` — the macro view no longer
calls them and the only entry path is the typed signal. The visual
behaviour of the first night (spirit, embers, speech bubble) is
unchanged: every place that previously polled every frame now updates
once per real camera change, which is what changes during play
anyway.

**A10 — Visual regression harness.** A10 centralises every
fixture/visual-regression seam under
`game/scripts/Testing/VisualRegressionHarness.cs` and
`game/scripts/Testing/VisualFixtureCatalog.cs`. Three production
APIs that grew to enable screenshots are gone:

- `CityWorldController.DrainAllForestsForVisualRegression` — was
  `public`; A10 moved the same operation to the `internal`
  `DrainAllForestsForFixture` seam and gated it on
  `VisualRegressionHarness.IsActive`.
- `CityWorldController.AdvanceWorldTickForVisualRegression` — was
  `public`; A10 moved it to `AdvanceWorldTickForFixtureHarness`
  with the same gating.
- `CityWorld.ConcludeFirstNightForFixtures` — was `public`; A10 made
  it `internal` and granted the Godot assembly
  `InternalsVisibleTo` so the harness (which lives there) can still
  call it.

Two more Domain methods followed the same path:
`ConstructionProject.SeedProgressForFixture` and
`CityWorld.DrainAllNaturalResourcesForFixtures` are now `internal`;
the test assembly keeps its existing grant, and the Godot assembly
gets one for the harness. Production scenes cannot grow a new
screenshot path through these seams — the guard
`Domain_DoesNotExposeFixtureSeamsAsPublic` fails the build if any
future Domain method ends in `ForFixture(s)` and ships as `public`.

`CityWorldController`'s fixture surface shrank in two stages:

1. The two `public` visual-regression entry points above are gone.
2. The remaining `internal` fixture methods
   (`SeedFixtureWorld`, `RestoreFixtureWorld`,
   `AdvanceWorldTickForFixture`, `RecordFixtureWoundEvent`,
   `SeedProjectProgressForFixture`, `RegisterFixtureCitizen`,
   `NextFixtureCitizenId`, `NextFixtureCitizenIdByMax`,
   `RecordFixtureLogEvent`, `GetFixtureResourceAvailable`,
   `DepositToFixtureInventory`, `GetFixtureHeroProfile`,
   `GetFixtureHero`, `CancelFirstActiveExpeditionForFixture`,
   `GetProjectForFixture`, `GetBuildingForFixture`) stay `internal`
   and remain the seam the harness reaches through. The next slice
   moves their bodies behind the harness so the controller does not
   grow a per-fixture method for every new screenshot.

The visual-regression harness owns activation
(`VisualRegressionHarness.Activate` parses `WOG_VISUAL_CAPTURE` and
the `--wog-visual-capture` / `--wog-visual-fixture=` arguments),
classification (`VisualFixtureCatalog.Classify` returns a typed
`VisualFixtureKind`), and the runtime scenes ask
`VisualRegressionHarness.IsActive` instead of probing
`Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE")` themselves.
The remaining direct env-var reads in
`MacroStreetLiveView`, `ExpeditionRail`, `PauseMenu`, `ConstructionPanel`,
`ExpeditionLiveView`, `BuildingDetailView`, `AstralOnboardingView`,
`ResourceInventoryPanel`, `LocaleManager`, `PanelHeader`,
`ExpeditionStage` stay as-is because each one gates a tiny dev-only
behaviour (frame-time sampling, debug toggles, locale probe). Folding
them through the harness is a refactor for the next slice; A10
removes the fixture-orchestration env-var reads and leaves the
behaviour-gating ones behind with a documented reason.

The fixture seams that A10 leaves behind are:

| Seam | Why it stays |
|---|---|
| `CityWorldController` `internal void` fixture methods (≈14) | The harness is the only legitimate caller; the methods stay `internal` and gated on `VisualRegressionHarness.IsActive`. Moving their bodies into the harness is a future slice. |
| `ConstructionProject.SeedProgressForFixture` (internal) | The harness needs a way to fast-forward a worksite without simulating days. The seam is `internal` and only the test assembly + harness reach it. |
| `CityWorld.DrainAllNaturalResourcesForFixtures` (internal) | The depleted-forest screenshot needs the world with no trees; the seam is `internal` for the same reason. |
| `CityWorld.ConcludeFirstNightForFixtures` (internal) | First-night screenshots sometimes need a post-opening world; the seam is `internal` and called only through the harness. |
| Direct `WOG_VISUAL_CAPTURE` env-var reads in scene trees (≈12 sites) | Each gates a dev-only behaviour (frame-time sampling, debug toggles). Folding them through `VisualRegressionHarness.IsActive` is the next slice; A10 removes the fixture-orchestration reads and documents the rest. |

The new guards are:

- `VisualRegressionHarness_LivesUnderTestingNamespace` — both
  `VisualRegressionHarness.cs` and `VisualFixtureCatalog.cs` live
  under `game/scripts/Testing/`.
- `Domain_DoesNotExposeFixtureSeamsAsPublic` — every Domain
  `*.ForFixture(s)` method must be `internal` (or below).
- `CityWorldController_DoesNotGrowPublicVisualRegressionMethods` —
  no new `public` `*ForVisualRegression` entry points.

**A11 — Static-structure authoring rule.** A11 codifies the rule
that production UI panels whose shape does not depend on runtime
data live in a `.tscn`; the script owns behaviour, state binding,
and the rows that the snapshot drives. The rule exists in three
places that must stay in sync:

- `../presentation/ui-patterns.md` §2 (the three component patterns) and §9
  (the migration checklist), which A11 extended with §9.1
  ("the static-structure rule").
- `ArchitectureBoundaryAllowlist.ProductionUiStaticStructureInCode`,
  which classifies every production UI file as **A** (migrate to
  `.tscn`), **B** (genuinely dynamic collection, stays programmatic),
  **C** (reusable primitive in `Ui/` — already excluded by the
  scanner), **D** (dev/debug tooling), or **E** (runtime-only
  visual object).
- The architecture guard
  `ProductionUi_DoesNotComposeStaticHierarchyInCode`, which
  scans every production screen (excluding `Domain/`,
  `Application/`, `Testing/`, `Ui/` primitives, `Prototypes/`) for
  `new Panel | new Label | new Button | new Container | new HBox |
  new VBox | new Margin | new TextureRect | new PanelContainer |
  new Separator | new HSeparator | new VSeparator | new
  GridContainer | new TabBar | new TabContainer | new
  ScrollContainer | new CenterContainer | new PanelContainer |
  new MarginContainer | new HSplitContainer | new VSplitContainer`
  and fails the build on a future screen that reaches for one of
  those for its top-level layout.

The rule, in one sentence: **what does not depend on data lives in
`.tscn` and Theme; what depends on data lives in C#; nothing lives
in both.**

The canonical example is `PauseMenu`: the entire shell (Scrim,
CenterContainer, Card with `HudSurface`, MarginContainer with the
project's spacing tokens, Heading, MainActions, ResetConfirmation)
is in `game/scenes/PauseMenu.tscn`; the C# side only does
`GetNode<…>(…)`, wires `Pressed` signals, owns the ESC handler and
focus, and refreshes localized text on locale change. Zero
`new Panel` / `new VBox` / `new Button` calls. The same pattern
applies to `OnboardingView`, `HeroProfileView`, `MigrantPanel`,
`ExpeditionPanel`, `AssignmentRow`, `ResourceTree`,
`CultivationActionMenu`, `ResourceActionMenu`, `CombatantView`,
`OctagonalSkillSlot`, `ExpeditionSquadSlot`, `ExpeditionSquadStrip`,
`ExpeditionSkillStrip`.

That migration is **finished** (GitHub #9). All ten panels are either
authored in a `.tscn` or classified as genuinely dynamic; the A row of
`ProductionUiStaticStructureInCode` is empty, and a new panel that
composes its static shell in C# now fails the guard with nowhere to be
excused to. Each slice created the `.tscn`, moved the static shell
there, kept C# for signal wiring and dynamic rows, and removed the
entry from the allowlist once the guard stayed green.

The last of them, `ExpeditionRail`, was held back for two sessions
because its hierarchy is not entirely its own: `ChroniclePanel` builds
the chronicle's header and body, and where the header lands among the
accordion host's children decides whether a second real click on it
reaches the header or the body. What that needed was a capture harness
to check the result against — the answer is a click, not an argument —
not a different design.

The single documented exception to "no inline spacing numbers"
is `Tokens.ScrollGutter`: the gutter has to sit inside the
scrolled content for the vertical scrollbar to behave correctly,
and a `theme_override_*` on the `ScrollContainer`'s own `StyleBox`
moves the bar with the viewport instead. The token names the
value (`16`); a literal at the call site is the documented
exception.

**A12 — Final audit.** A12 closes the last slices of
transversal debt after A0–A11. No new layers, no large refactors.

The i18n mappings that used to derive PO keys from enum names
(`UiText.Get(resourceType.ToString().ToLowerInvariant())`) now
route through typed Presentation mappers. The first is
`Ui/ResourceTypeLocalizer`, which owns the explicit
`ResourceType → PO key` switch; Domain and Application no longer
know any PO key. Future mappers for other enum families
(ConstructionKind, BuildingKind, LineageId) follow the same
pattern.

The fixture entry points that used to be `public` on every
production surface so a screenshot could author its scene are
now `internal` and gated on
`WorldofGoses.Testing.VisualRegressionHarness.IsActive`. A10
closed the seam on `CityWorldController`; A12 closes it on
`AstralOnboardingView.ShowForVisualRegression`,
`CombatDebugPanel.RunForVisualRegression`,
`ExpeditionPanel.ShowWoundRecoveryForVisualRegression`,
`MigrantPanel.ShowForVisualRegression`,
`MigrantPanel.ShowMigrantCubeForVisualRegression`,
`TimeOfDayFilter.PinDayFractionForVisualRegression`,
`MacroStreetLiveView.ShowThirdStreetDepthForVisualRegression`,
and `MacroStreetLiveView.ShowLongTerrariumForVisualRegression`.
The static guard
`ArchitectureBoundaryTests.Production_DoesNotExposePublicVisualRegressionMethods`
catches any future regression.

The UI input actions the codebase uses (`ui_cancel`, `ui_accept`,
`ui_left/right/up/down`) live as `const string` on
`Ui/UiInputActions`. The string literal `"ui_cancel"` is no
longer scattered through 12 callsites; the centralisation is
the seam for a future input-remap surface.

The dependency boundary is enforced by the project references
themselves:

- `WorldofGoses.Domain` references no other game assembly.
- `WorldofGoses.Application` references Domain only.
- `WorldofGoses.Persistence` references Domain only.
- `game/World of Goses.csproj` (Godot) references all three.

The Domain assembly's `[assembly: InternalsVisibleTo("World of Goses")]`
grant is the documented seam that lets the visual-regression
harness reach the narrow `internal void DrainAllNaturalResourcesForFixtures()`
and `internal void ConcludeFirstNightForFixtures()` seams on
`CityWorld` and `ConstructionProject.SeedProgressForFixture`.
Production code cannot grow a new screenshot path through these
methods: there is no public `*ForFixture` API on the Domain, the
Godot fixture surface is `internal`, and the static guard
`Domain_DoesNotExposeFixtureSeamsAsPublic` fails the build on a
future regression.

How each of these is enforced is the table in §0. The allowlist that goes with
them lives in `ArchitectureBoundaryAllowlist`, one entry per exemption with the
reason inline — and its A class, the panels that once composed their static
shell in C#, is now **empty**. The remaining entries are classifications, not
debt: a panel whose body is a dynamic collection stays programmatic on purpose,
and dev-only fixture scenes are allowed to author a world. Adding a new entry to
make the build pass is the wrong move.

**A8 — Session-owned world, presentation as adapter.** A8 closes the
last remaining presentation ownership of `CityWorld`. The aggregate is
constructed and owned by `CityGameSession` (Application); the
controller reduces to a Godot adapter that subscribes to the session's
forwarded events, drives the simulation cadence, and persists the
session's owned world through `WorldPersistence`. The previous
`private readonly CityWorld _world = new();` and the legacy
`internal CityWorld World => _world;` getter on the controller are
gone; the controller holds a single `private readonly CityGameSession
_session;` reference and never touches `CityWorld` outside the
visual-regression fixture seam (`internal CityWorld GetFixtureWorld()`
on the controller, fed by the session's `internal CityWorld World`
getter). The session exposes typed events (`BuildingChanged`,
`ProjectChanged`, `PatchChanged`, `CultivationSiteChanged`,
`ExpeditionChanged`) so the controller subscribes without ever holding
a `CityWorld` reference. The dirty bit (`IsDirty`) lives on the session
and is set whenever the world's events fire or a use-case returns a
successful `*Result`. Persistence orchestration (save / load / reset /
EG-0 report) stays on the controller because the Application assembly
intentionally does not reference the Persistence assembly (A6 rule,
enforced by `Layer_DoesNotReferencePersistenceAssembly`); the
controller reaches the session's owned world through the `internal`
seam and writes through `WorldPersistence.SaveToSlot` /
`WorldPersistence.ApplyTo` / `WorldPersistence.DeleteSlot`. `UseCaseDelegationTests`
still asserts every public controller method routes through
`_session.<Name>`; the new `ArchitectureBoundaryTests` guards are:

- `Presentation_DoesNotInstantiateCityWorld` — only the
  `CityPrototype` and `RealCityStreetPreview` fixture scenes are
  allowed to author fresh worlds; production presentation never builds
  one of its own.
- `Presentation_DoesNotMutateAggregatesOrEntities` — scene code
  outside the controller never calls a public mutator on
  `Citizen`, `Building`, `CityWorld`, `Expedition`,
  `ConstructionProject`, or `CultivationSite`. The controller is the
  documented fixture seam and is exempted by the scanner.
- `CityWorldController_DoesNotHoldACityWorldField` — the controller
  never re-introduces a `private readonly CityWorld _world` field.
- `CityWorldController_DoesNotExposeWorldGetter` — the controller
  never exposes a `CityWorld World` property; the only reach is
  `internal CityWorld GetFixtureWorld()`.
- `Domain_DoesNotReferenceApplicationAssembly` — Domain has no
  `ProjectReference` to Application. The only dependency direction
  between Domain and Application is `Application → Domain`.

The path for a new gameplay feature is now strictly
**Presentation → Application use case → Domain**: a Godot input
handler calls the controller's thin use-case wrapper, which forwards
to the matching method on `CityGameSession`, which orchestrates the
domain. No future slice needs to reach into `CityWorld` from
Presentation.


Citizen responsibility is split between a durable player-authored
`Citizen.WorkOrder` and the mutually-exclusive current `Citizen.Commitment`.
Temporary expedition or vital-recovery execution can therefore suspend work
without deleting its target. On return, the scheduler re-evaluates the standing
order instead of resuming a stale action blindly. `CitizenVitalStatus` is
survival-only: it may pause for food/rest, but never chooses a profession or a
productive target. These additive fields remain compatible with older v14
snapshots; restore infers a missing standing order from `CurrentAssignment`.

Daytime assignment commits the citizen immediately but places their physical
location in `InTransit`. Production and construction simulations count only
assigned citizens whose location is `AtWork`. **World time is the only authority
that ends a journey.** `Citizen.TransitStartedAtTick` and `IsReturningHome` are
the durable facts; `Citizen.TravelArrivalTick` derives the deadline from them,
and `CityWorld.CompleteDueTravel` — reached from the single `AdvanceWorldTick`
that live play and offline catch-up share — is the only code that ends a trip.
A journey coming due outside labour hours reverses toward Home rather than
parking the citizen at a closed worksite, preserving the standing order. Neither
path stores pixel coordinates. Recovery exposes `WorkersRecovering` or
`WorkersBlockedNoFood`; once fed and above the resume threshold, the same
standing order is re-evaluated and travelled again.

Until A2 (`DEC-0023`) this worked the other way: live play could only end a
journey when `MacroStreetLiveView` reported its sprite had reached an anchor,
while offline catch-up ended the same journey on elapsed ticks. That was two
semantic authorities for one fact, and it meant an animation that never ran — a
hidden view, an unsolvable route — could hold a citizen in transit indefinitely
and keep their workplace on `WorkersInTransit` forever.

`CityEconomyRules` separates the one-second clock resolution from economic
events. Productive buildings resolve deterministic batches every ten ticks;
food is considered on a separate meal cadence, so animation frames and clock
ticks do not imply resource creation or consumption. Both live and offline
progression use the same absolute-tick predicates. The additive
`EconomicBalanceVersion` save field applies the first storage rebalance once to
older snapshots without rewriting explicitly tuned capacities in new saves.

`GameClock` / `CityWorld.CurrentTick` are the single world-time authority for
city, expedition travel and combat. `ExpeditionLiveView` consumes that state;
it does not own a timer. The controller exposes only global 1x, 2x and 4x
cadence choices. There is no paused world state, and opening Menu or switching
presentation surfaces cannot change the selected speed.

That is the whole clock rule, and it is short on purpose:

- **One clock.** City, travel and combat advance on the same timeline, in
  parallel. Offline catch-up uses the same clock and the same domain
  transitions.
- **No pause.** A modal may capture input and cover the scene; it may not
  freeze the domain.
- **No second clock.** A view-local timer that advances anything is the failure
  mode this rule exists to prevent: it desynchronises city, travel, combat,
  save/load and catch-up, each in a different way.

Simulation shape follows from the same constraint. Avoid: updating every
citizen in `_Process`, simulating each offline second, one node per
inhabitant, pathfinding for invisible population, global mutable state, a
premature event bus, unjustified dependencies. Favour: discrete events, batch
computation, compact data, state on demand.

Macro workplace routing targets the front approach band rather than the
occupied building centre. A carrier is visible while travelling, hidden on the
macro map while the citizen works inside, and mounted into the interior worker
slot when building detail is open. This remains presentation state; the domain
stores only `InTransit` or `AtWork`, never pixel coordinates.

`MacroStreetLiveView` draws journeys; it does not end them. The founding hero
and every other citizen use the same `StreetRoutePlanner`, obstacle topology and
quantized cadence; the founder keeps a dedicated carrier path only for
founder-specific actions such as gathering. The contextual `BuildingInspector`
(formerly a top-level `BuildingDetailView` shell, retired per issue #20)
continues to read the same `BuildingDetailSnapshot` and renders only
citizens already at `AtWork`.

A drawn route is paced against the domain's own window rather than against the
render cadence: `PacedRouteSteps` spreads the route's steps across
`TransitStartedAtTick → TravelArrivalTick`, so the walk finishes on the tick the
domain has already chosen, 2x/4x accelerate it for free, and a dropped frame
merely catches up on the next one. Steps stay discrete — the pacing changes only
their timing, never the cadence grammar. The same `ReconstructRouteProgress` that
resumes a part-elapsed journey after a load is what advances a fresh one, so
restore and live play are one code path instead of two.

Routes with no domain journey behind them — gathering, ambient wandering — have
no arrival tick to be paced against and keep the plain cadence gait.
`ArchitectureBoundaryTests.Presentation_DoesNotConfirmCitizenArrival` keeps the
old authority from returning, with an allowlist that is empty by construction.

**A4 — macro view composition.** `MacroStreetLiveView` composes five
single-responsibility collaborators plus three pure-helper classes (A4):
`MacroStreetRenderer` (records + per-street draw + band-layer stack + hit-rect
publication), `MacroInteractionController` (selection + hover state + the one
`HitTest` that resolves a pointer position to a tree, a citizen or a building —
left and right click share it, so the two buttons cannot drift to different
targets for the same pixel),
`CitizenJourneyPresenter` (founder state + per-citizen journey dictionary +
`StreetNavigationServerPlanner`), `MacroCameraController` (zoom + free/follow
mode + lateral/depth anchor + pan + transition timing + building-entry push),
and `PlacementPresenter` (placement-mode state: the active flag, the chosen
kind, the projected lot and cell boxes, and the hovered / selected lot). The
placement clickable rects deliberately stay in `MacroHitRects`, the one
hit-rect bag the whole macro view shares, so the presenter takes them as an
argument instead of keeping a second copy. The pure helpers —
`MacroViewConstants`, `MacroProjectionHelpers`, `MacroObstacleGeometry`,
`MacroSelectionTextBuilder` — centralise numerics, projection maths, obstacle
geometry, and selection text. `MacroHitRects` and `MacroPlotLookup` are the
record-bag seams between the renderer and the interaction / journey paths. No
interfaces, no DI, no service locator. The view orchestrates the collaborators
but does not know their internals; the collaborators do not know about each
other. The presenter never ends a journey — `PacedRouteSteps` still paces
against the domain window, and `ArchitectureBoundaryTests.Presentation_DoesNotConfirmCitizenArrival`
keeps the old authority from returning.

What A4 has not done: the interaction *handlers* stay in the view on purpose.
`MacroInteractionController` answers "what is at this point" and owns the
selection and hover state, but `SelectTree`, `SelectBuildingPlot`,
`OpenGatherMenu`, `OpenCultivationMenu` and `UpdateWorldHover` still live in
the view, because moving them would mean handing the controller the context
inspector, both action menus, the terrain atlas and the controller facade —
trading one large class for a second one. The remaining follow-up is the
folder: `game/scripts/Prototypes/` is no longer a prototype and belongs under
`Presentation/Macro/`. Eight assertions in `HudCompositionTests` grep the view
by hardcoded path segments (`"game", "scripts", "Prototypes"`), so the move is
a rename plus those eight paths, and should be its own commit rather than a
rider on a behavioural change.

The macro camera is free by default. Selection changes information and action
context only; following the selected citizen requires the explicit camera toggle. WASD
and the arrow keys always pan the camera. In an unobstructed macro view their
physical key events are handled before GUI focus dispatch, so they cannot also
move focus across HUD buttons; gamepad D-pad remains available for focus.
Manual pan releases follow mode and
never changes the founder's physical street position.

`StreetDepthProjection` has one focus-relative perspective at every zoom level;
zoom is always a uniform node transform and never renormalizes or stretches the
terrain. Its visible window is bounded to thirteen construction streets: two
foreground streets, the focused street and ten receding streets. The fourth
position counting the focus crosses the near plane. `MacroStreetLiveView`
shifts this window as the camera advances through a larger semantic territory,
so off-window parcels are neither drawn nor folded onto the fixed horizon.

Citizen persistence remains semantic in schema v19: work order, commitment,
logical location, travel start and direction are authoritative; pixel position,
route cursor, sprite, animation, and node state are not stored. After restore and
offline advancement, `CitizenRoutineSnapshot` derives the current activity,
contextual building/Shelter, blocker, and next transition. The macro view derives
building anchors from the current placement and reconstructs an unfinished route
from semantic timing without writing that visual position back to the domain.

## 6. State, rules, and animation

The conceptual rule:

> Pixelorama defines how it looks.
> Godot defines how it is represented and animated.
> C# defines what is happening and why.

In practice:

- A Pixelorama source is the visual definition of a frame.
- A Godot `SpriteFrames` resource (or `TileSet`) is the runtime
  representation.
- A C# class decides which frame to display, when to transition, and
  what state the citizen / building / object is in.

The visual layer never invents state. It reads it.

**Which C# class, though, is the question this section used to leave
open.** [`state-authority.md`](state-authority.md) answers it: it is the
canonical registry of every mutable truth in the simulation, the five
categories those truths fall into (lifecycle state, orthogonal condition,
intent, derived projection, presentation state), who may write each one,
and how each is reconstructed from a save. It also holds the contract for
the animation layer that does not exist yet — domain facts → routine
snapshot → animation projection → Godot, one direction only.

Read it before adding a field that answers a question something else in
the tree can already answer. The failure mode it prevents is not an
untidy enum; it is two owners of one truth, drifting apart, with neither
wrong on its own terms.

## 7. Person entity: one citizen, many attachments

There is exactly one person entity in the domain: `Citizen`. The
prototype does **not** introduce specialised subclasses for hero,
miner, doctor, artisan, leader, adventurer, or any other role. Those
concepts are *attachments* composed onto a citizen:

- **Citizen profiles.** A citizen stores one immutable profile attachment containing lineage, personal aptitudes, professional affinities, elemental affinity, combat and weapon preferences, personality traits, political orientation, and spiritual posture. These are identity metadata in the current slice; they do not replace competencies or practical history.
- **Competencies.** A citizen accumulates experience in named
  competencies (e.g. mining). The current slice implements mining
  only; the model is open-ended and not bounded to a fixed number of
  competency slots.
- **Roles and recognitions.** A citizen may hold any number of roles
  at any time (e.g. founder, healer, expedition leader). Hero status
  is one of these roles — not a subclass.
- **Memberships and ranks.** The same composition mechanism carries
  institutional affiliations.
- **Availability.** Coarse state describing whether the citizen is
  free or currently assigned to a workplace. Future slices may extend
  this (injured, traveling, on leave) without changing the citizen
  model.

The vertical slice in `game/scripts/Domain/` exposes the minimum
needed by the current prototype (`CitizenId`, `LineageId`, profile option
IDs, `CompetencyId`, `RoleId`, `CompetencyEntry`, `Role`, `CitizenProfile`,
`Availability`, and `Citizen`). Professional history, education, health,
relationships, and expedition history remain future attachments.

A hero in this model is a citizen whose role list contains a `hero`
recognition. Any citizen is potentially eligible for expedition duty
through player choice and city systems — the domain does not enforce
statistical gates.

## 7a. Three visual scales

The prototype establishes three conceptual visual scales, even though
only the first two are implemented now.

| Scale              | Purpose                                                                 | Implementation          |
| ------------------ | ----------------------------------------------------------------------- | ----------------------- |
| Macro              | Communicate city-wide activity from a distance.                         | Implemented (this slice)|
| Building-detail    | Show workers inside a specific building; allow direct interaction.      | Implemented (this slice)|
| Expedition-detail  | Fully detailed side-facing sprites, frame-by-frame animation.           | Next approved vertical  |

Concretely:

- **Macro.** A city view uses one small marker per current citizen. Markers
  are not individually interactive yet, but their count is derived from the
  domain rather than from a fixed population fixture.
- **Building-detail.** When a building is selected, the view opens
  with one visible worker per *visual capacity* slot. Every visible
  worker corresponds to a real `CitizenId`. Workers that are assigned
  but exceed the visual capacity are reported as "working inside" by
  the domain; the presentation layer surfaces this number verbatim.
- **Expedition-detail.** The first lateral `ExpeditionLiveView` is implemented
  below `GameUiShell/ScreenContent`. It observes a domain `CombatSession` keyed
  by `ExpeditionId`; hiding the view never owns, stops or recreates combat.
  Each world tick advances one logical combat step and `ResolveToEnd` consumes
  that same incremental path for tests/debug. Schema v33 persists the logical
  step plus replayable AUTO/manual commands, then reconstructs health,
  cooldowns, RNG and `CombatLog` from the same seed and resolvers. Schema v34
  adds per-expedition combat-rules versioning: a v33 session replays its legacy
  balance, while a newly dispatched opening trail uses the non-persistent
  tutorial baseline. Later scenes
  will use fully detailed side-facing sprites authored as
  Pixelorama sprite sheets (`art/source/characters/...`) and driven by
  Godot `AnimatedSprite2D` with `AnimationPlayer` transitions.
  This observable path is currently restricted to the Founder-only
  `SpiritTrailSearch`; other expedition kinds keep the prior aggregate
  encounter resolver. Quiescent offline batching is disabled while its combat
  session is active so one world tick always means one combat step. The named
  `ExpeditionTiming` milestones drive the four-hour route: half-hour Encounter,
  post-combat Objective travel, physical objective arrival and visible Return.
  `SupplyRequirement.None` and `ExpeditionReward.Discovery` keep this narrative
  route outside the material reservation ledger.

Placeholder dimensions used by the prototype so that final art slots
in without re-anchoring:

```text
Base terrain unit:        64 × 64
Detailed citizen canvas:  approximately 64 × 96
Macro citizen:            approximately 6 × 6 (within the 4–8 range)
Building plot footprint:  192 × 192 (3 × 3 base units)
```

The numbers above live as constants in
`game/scripts/PresentationConstants.cs` so future art can rely on the
same anchors.

Logical placement does not use pixel rectangles. A parcel contributes nine
one-tile frontage columns to each of three construction rows. Standard
buildings reserve a sliding window of three contiguous columns with fixed
three-tile depth; the domain permits later growth up to six columns. Windows
may cross adjacent parcel boundaries only when every contributing parcel is
available. `BuildingReservation` owns this interval while
`ObstacleFootprintTemplate` describes the smaller solid geometry and authored
clearances in integer half-tiles for resources, constructions and
infrastructure. Protected `CorridorReservation` intervals cannot be consumed
by construction.

Schema v25 persists row, start column, total frontage, fixed depth and
directional expansion counts. The v24→v25 migration maps every former 3×3 lot
deterministically to three frontage columns while preserving the entity ID and
legacy anchor fields for diagnostics. The macro snapshot projects reservation
and solid footprint separately: placement draws the reserved area, while
`MacroStreetLiveView` sends only the clearance-derived solid interval to
navigation. Schema v26 persists one `NaturalResourceUnitPosition` per reserve
entry (`RowWithinParcel`, `FrontageColumnWithinParcel`). Fresh layouts use
`NaturalResourceLayoutPlanner` with the founder's stable seed; v25 saves are
deterministically reflowed around construction, corridors and the protected
founder arrival cell. Fresh positions are scattered across available cells
rather than authored as repeated compact rows. Patches of different resource
types may share a parcel.
Each live resource blocks one frontage cell for construction and uses the same
obstacle pipeline through `NaturalResourceFootprintCatalog`; trees have no
special collision rule. Godot translates this domain geometry to projected
pixels and anchors citizens on the street side; it does not decide placement
legality.

`ConstructionPlacementSnapshot` projects every visible frontage cell and every
candidate three-column window together with the domain-owned
`FrontageCellState`. `MacroStreetLiveView` uses that single read model for the
full two-axis grid, blocked-cell marks, hover feedback and final selection, so
hover cannot promise a placement that confirmation later rejects under a
different presentation-only rule.

The first agricultural authorization is a bounded exception to ordinary
building completion. `ConstructionKind.CultivationSite` still reserves a
normal persisted three-column frontage window, but completion replaces the
project with the Godot-free
`CultivationSite` domain entity rather than a productive `Building`. That
entity owns only `Prepared`/`Sown`/`Growing`/`Ready`/`Spent`, `PlantedTick` and
`ReadyAtTick`; crop visuals remain a projection. Sowing and harvesting are
explicit commands, while both live ticks and `WorldTimeAdvance` resolve the
same absolute readiness boundary. Growth never depends on a scene node or an
assigned citizen remaining attached.

### Screen composition shell

`CityPrototype.tscn` owns one typed `GameUiShell` (`VBoxContainer`) with two
non-overlapping regions: the persistent `CityStatusPanel` and an expanding
`ScreenContent`. Macro, building-detail, and hero-profile screens are siblings
inside `ScreenContent`; they no longer compensate for the status bar with
per-screen top offsets. `GameUiShell` validates both direct slots at startup,
enforces their order, and exposes typed references so the layout is a runtime
contract rather than a node-name convention. Full-screen onboarding and tutorial surfaces remain
outside the shell because they intentionally cover the normal screen flow.

Full views do not scroll as a default composition strategy. Scrolling belongs
to bounded subsections whose data can grow without a practical visual limit
(for example Chronicle entries or citizen assignment lists). The shell keeps
screen chrome stable; each screen must still fit its primary composition in the
available content region and delegate overflow only to the growing subsection.

`CityStatusPanel` renders a small global ticker from `CityStatusSnapshot`:
one resource icon plus its available amount, with stored and reserved totals
in the tooltip. The projection reads `CityResourceLedger`; it does not create
a second inventory or change physical ownership. `BuildingDetailSnapshot`
continues to project the fuller inventory read model consumed by the Shelter's
collapsible resource panel; presentation never queries scene nodes to infer
stock. `StockProduced` and `CropHarvested` remain persisted domain
events for metrics and causal history, while `ChronicleEventProjection` excludes
them from the player-facing Chronicle. Basic ground-resource gathering emits
a transient `ResourceGainPopup` at the current physical owner (founder,
Founding Cache, or Shelter); this feedback is presentation-only and cannot
mutate inventory. Before Cache, the popup samples the founder carrier's world
position at each quantized motion step; building-owned feedback remains fixed
to the projected storage anchor.

`CitySummaryPanel` consumes that same immutable `CityStatusSnapshot` as a
persistent, collapsible left-side read model. It shows the founding lineage,
truthful population/housing occupancy, ledger availability, and every exposed
construction project with work progress and its explicit stop cause. It does
not estimate production deltas or construction duration because no aggregate
rate currently exists. Collapse is presentation-only and never enters
`WorldSave`. The authored macro HUD slots remain non-overlapping: the
`CitySummaryPanel` begins at the left safe margin, with transient
`ContextInspector` immediately to its right and bottom-aligned. The fixed
bottom-centre `PrimaryNavDock` is mutually exclusive with contextual
`ActionDock`. `SimulationControls`, `PlayPauseButton` and the paused speed state
no longer exist. Camera, Speed and Menu share the right-edge utility cluster in
`CityStatusPanel`; Speed cycles 1x / 2x / 4x and Menu never freezes the world.
The macro-only summary siblings start hidden and are revealed/hidden by
`MacroStreetLiveView`'s existing
`ActivatePerspective`/`Deactivate` routing, so full profile/detail perspectives
cannot retain the city rails accidentally; no global HUD manager or runtime
surface construction is involved.

The persistent right-side `ExpeditionRail` consumes an immutable
`ExpeditionRailSnapshot` projected by `CityWorldController`. It exposes only
active domain expeditions, their real members, persisted phase, committed
supplies and authoritative tick interval; it introduces no queue or dispatch
rules. Its embedded `ChroniclePanel` delegates meaningful-event filtering and
compaction to `ChronicleEventProjection`, then uses the existing localized
`WorldEventTextFormatter`. Compact and expanded states share the same rail and
controller. Both section headers stay mounted on the rail's own column at all
times; their two bodies — the expedition scroll and the chronicle scroll —
share a single `AccordionHost`, which keeps exactly one of them visible. That
host is the rail's only vertically expanding child. The arrangement is
deliberate: two `ExpandFill` siblings previously divided one column between
claimants whose minimum sizes moved as their contents folded, and the loser was
squeezed to a near-zero rect while its children stayed `Visible` and undrawn —
measured at 2 px against a 25 px card. Because a Godot `Container` excludes
invisible children from its minimum size, one visible body means there is no
division to lose, and no ancestor needs `QueueSort`, `ResetSize` or
`UpdateMinimumSize` after a toggle. Expansion replaces the expedition summary
and adds offline catch-up summary, grouped blocker decisions and bounded
history. The accordion restores the expedition summary when Chronicle closes,
so neither surface can strand the other in an invisible state.
There is no adjacent or duplicate Chronicle surface.

The observable encounter owns a pure one-dimensional `CombatSpatialState` per
participant inside the deterministic `CombatSession`: `PositionX`, movement
speed, attack range, controlled body radius, facing, Stability and Impulse.
Actors only approach a target until its body envelopes are within
`AttackRange`; there is no reverse/preferred-range branch, so ranged actors do
not kite. Damage remains in `TechniqueResolver`; a hit that applies `Knockdown`
— and only such a hit — displaces its target through centralized
Impulse/Stability knockback, scaled by the physical share of the blow — and only
if that Knockdown survived the target's Control Resistance. Every measurable
consequence of a log entry travels as a typed `CombatImpact` (displacement,
signed health delta, physical share) beside the human-readable `Detail`, so
presentation never parses a formatted number back out of a string. Session replay rebuilds
the same spatial state from seed, steps and commands, so schema v34 needs no
second serialized position stream. `ExpeditionStage` and `CombatantView` only
project snapshots/events and may interpolate visual pixels; Godot positions,
animations and missed frames never decide impact or mutate the domain.

Before a Cache exists, the four rudimentary resources are a six-unit founder
load rather than general city storage. `ConstructionSnapshot` projects that
load to the Construction surface; after Cache it projects aggregate 12-unit
site storage, and `BuildingDetailSnapshot` takes over after Shelter
consolidation. Legacy Food/Wood inventory is deliberately excluded from the
pre-Cache carrying headroom. The popup anchor follows the same derived owner,
so this presentation transition adds no persisted location field or schema
version.

## 7b. Citizen visual carrier: one citizen, one sprite

The domain guarantees a single `Citizen` instance per identity. The
visual layer must honour that guarantee — and the design bible confirms
it (see `../systems/citizens.md`)
when it states that no subclass duplication or per-context visual
variant is permitted. The presentation rule therefore is:

> **One citizen → one sprite instance, alive for the citizen's lifetime.**

Views (the macro hero sprite, the building-detail worker slot, the
future hero profile detail, the future expedition cast) **do not create
sprites**. They ask the central registry where the citizen's sprite
should be and animate it to that position. The sprite is a single
object that moves between contexts.

### Concretely

- `CitizenSpriteBank` (autoload) owns a
  `Dictionary<CitizenId, CitizenSpriteCarrier>`. The bank is the only
  place that creates and disposes sprites. Inactive carriers park under the
  bank; the active view mounts the canonical instance into its local visual
  host so normal clipping and scene order apply. Carrier initialization is
  internal to the assembly and stale visuals are hidden before deferred
  disposal, preserving the one-visible-instance invariant during replacement.
- `CitizenSpriteCarrier` (Node2D) wraps one `LineageSpritePlayer` and
  tracks its state — `Home`, `Entering`, `Working`, `Exiting`. It never
  has a duplicate; it is the canonical visual for one citizen.
- A view (slot, macro marker, profile panel) holds a **position** and tells
  the carrier "go to this position" — never "create a sprite here". If the
  active context changes, the single carrier moves or snaps to the new
  position; two contexts cannot render duplicate instances simultaneously.
- The carrier's animation is keyed by `WalkSpeedPxPerSec` (a constant
  derived from the sprite's own animation cadence, not an arbitrary
  duration). Entry and exit durations are `distance / speed`, so the
  visual speed stays consistent regardless of layout.

### Why this matters

- **No duplication.** A worker cannot be visible in two places at once.
  The "re-assign while exiting shows two sprites" class of bugs cannot
  exist by construction.
- **No allocation churn.** Re-entering a building does not instantiate
  a new sprite; it moves the existing one. The sprite's cached state
  (current animation, position) survives context changes.
- **One identity, one visual.** The hero profile, the macro
  hero marker, and the building-detail worker slot all reference the
  same carrier. Splash art, portraits, and stats panels become read
  models over the same `Citizen` instance.
- **Deterministic memory.** The bank caps sprite count at the number of
  citizens that have needed a detailed visual. `PruneExcept` removes stale
  carriers when the active world's citizen set shrinks, so tracked carriers
  remain a subset of current citizen identities. A lineage or gender mismatch
  for a reused id replaces the stale visual instead of retaining it.

### Out of scope for this rule

The rule applies to **ents with persistent identity** (citizens,
eventually named buildings, expedition parties). Anonymous decorations (dust
particles, weather, one-shot celebratory sprites, resource-item effects) do
not get a carrier. Pooling is introduced only when a repeated effect exists
and profiling shows allocation churn; the current prototype has no item-sprite
consumer or pool.

## 8. Local persistence

Local persistence is implemented through plain DTOs and `WorldPersistence` in
the engine-free `WorldofGoses.Persistence` assembly (`src/WorldofGoses.Persistence/`):

- Domain entities remain free of serialization attributes and file-system
  operations.
- A versioned JSON snapshot stores world state and the last-seen UTC time.
- Writes use a temporary file and preserve the previous snapshot as `.bak`.
- Structural and cross-entity invariants are validated before restore.
- The Godot controller auto-loads the primary slot, auto-saves periodically and
  on window close after onboarding, and starts a new empty world when no valid
  snapshot is available. Migrations are sequential and run on the raw JSON
  before `Validate`, so an older save upgrades non-fatally through every step
  rather than being read by a version-aware parser.
- **The migration chain is not narrated here.** Every step, with the reason it
  exists and what it refuses to invent, is the XML documentation on
  `WorldSave.CurrentVersion`, beside the constant it describes. Prose in a
  separate file drifted from it three times; `WorldSaveApplier.MigrateToCurrent`
  is the code that runs.
- A resource visit persists exactly one semantic owner — a building id or a
  natural-patch id — together with its unit and logical terrain slot, so
  ground-resource gathering stays valid across save/load.

Adding a persisted field means all four of: a `*Save` entry, a version bump, a
migration, and a `Restore` step in `WorldSaveApplier`. Skipping any one of them
leaves older saves unable to load.
- The real-time autosave cadence is centralized at three minutes. Periodic and
  close checks skip unchanged worlds; explicit consequential commands
  may still force an immediate atomic save. Save feedback is temporary UI, not a
  persistent HUD status.
- Offline elapsed time is capped and applied as deterministic batched ticks;
  an empty hero-only world uses an equivalent idle fast-forward.

### Material reserves vs. produced-resource storage

`CityResourceLedger` is the common location-aware facade over these physical
stores. It does not copy stock into a global counter: entries retain their
building and storage kind. It provides atomic recipe consumption and runtime
reservations owned by a construction project or an expedition. Schema v7
persists reservations, typed owners, and IDs; restore validates commitments
against physical stock and resumes allocation after the largest retained ID.
Schema v13 reuses the same model for expeditions: the supply cost is held
during the active window and committed on a successful return (or released
on cancel/failure) without moving the goods. The validator rejects any
`Expedition`-owned reservation whose id is not present in
`WorldSave.Expeditions`.
Natural-resource patches persist each visible unit independently and attach it
to a stable parcel. Forest compatibility state still owns gathered Wood stock
while recipes transition to a parcel-independent city store. Citizen visits
are stored as exactly one building/patch domain ID plus a logical terrain slot
rather than viewport coordinates. The logical slot remains valid when a visible
unit is depleted or its presentation disappears.

`Building` carries two distinct counters so the operating-recipe drawdown
does not visually shrink the produced-resource amount:

- `Stock` is the building's produced-resource output (Stone, Food, etc.).
- `IronStock` remains persisted for schema compatibility and future tools/fuel
  work, but no current early-game construction or operating recipe consumes it.
- `WoodReserve` is the Forest-style remaining source, drained by the
  manual "Gather wood" action into the Forest's `Stock`, which the
  construction recipe gate then consumes.

`TryConsumeResource(type, amount)` can still route Iron to `IronStock`, Wood to
gathered Forest `Stock`, and everything else to produced `Stock`. Current recipe
drawdown uses Wood and Food; the Iron path is retained without making it a
bootstrap requirement.

### Causal event log

`WorldEvent` carries a typed subject kind, optional entity ID, captured display
label, and typed `CauseEventId`. `CityWorld.FindCauseEvent` compares building
identity rather than display names. The
`ChroniclePanel` renders a "Decisions needed" list grouped by subject
above the chronological rows so the player can scan what requires attention
without scrolling the full timeline.

`OfflineProgressionReport` carries both aggregate counters and causal events
generated during catch-up. Schema v5 persists at most 128 significant events.
Per-tick production/progress and day/night noise are excluded; repeated adjacent
steady states are compacted, and causes outside the retained subset are cleared.
Restore preserves retained IDs and resumes allocation after the largest ID.
Schema v13 introduces four persistent kinds: `ExpeditionDispatched`,
`ExpeditionReturned`, `ExpeditionFailed`, and `ExpeditionCancelled`. The
expedition's dispatch event id is captured on `Expedition.SetDispatchEventId`
and reused as the `CauseEventId` of the matching return or failure, so the
Chronicle surfaces a one-row chain per expedition. Schema v14 adds
`MigrantArrived` and retires the `BuildingKind.Forest` building entity;
wood lives in `NaturalResourcePatches` and `CityInventory`, so the migration
keeps existing reserves without losing the player's gathered stock.
Schema v15 persists 1-2 expedition members, v16 persists the phase and
deterministic encounter result, and v17 requires every member to hold the
accumulated Hero role. Schema v18 persists the retreat posture and retained
dispatch event id. Legacy v17 plans continue after a setback; new plans may
instead enter `Retreating`, keep their citizen commitments through the return
leg, commit supplies, and resolve as `Retreated` without an objective reward.
Quiescent offline advancement batches active expeditions only to their next
persisted phase boundary, then runs the same phase transition/resolution used
by live ticks. Their dispatch events are pinned inside the 128-event persisted
window until resolution, so encounter, retreat, return, and pre-travel
cancellation keep a causal parent across save/load.
Schema v19 persists a citizen wound independently from stamina, including its
severity, originating event, and remaining treatment ticks. It also replaces
the parcel unlock boolean as the authoritative runtime state with
`Locked → Reconnoitred → RouteSecured → Available` while retaining the legacy
boolean in the DTO for migration. Recovery commitments reference Basic Shelter;
active wound origins remain pinned until treatment completes. Live and offline
ticks share the same recovery and territory-resolution behavior. Quiescent
catch-up subtracts recovery in bounded batches up to the next treatment or
day/night boundary. The first successful reconnaissance emits the three legal
parcel transitions as one ordered causal chain so that its return immediately
opens the new construction lot; later content may put those transitions behind
separate route requirements.

### Recruitment

`CityWorld.TryRecruitMigrant(CitizenProfile, string?)` is the first public
route for a non-hero citizen. It allocates a fresh `CitizenId` beyond every
existing citizen, instantiates a `Citizen` with the founder's profile, places
it at `AtHome` without assignment, and publishes `MigrantArrived`. The
controller exposes a `TryRecruitMigrant` wrapper with autosave and the
`CitizensChanged` signal so the UI can refresh rosters and macro views. The
roster view and assignments are presented through the existing
`AssignmentPanel` and `BuildingDetailView`; a dedicated `MigrantPanel`
mediates the action via `ModalHost`, reusing the same focus chain as the
expedition panel. Recruitment is the foundation for the next slice:
expeditions returning with a `MigrantArrived` outcome instead of a generic
`Stone` reward, and a `RosterView` that lists every non-hero citizen with
competency, stamina, and assignment.

### Boundary enforcement

Domain events retain only causal and semantic data. Presentation owns both
player-facing copy through `WorldEventTextFormatter` and the `WorldEventKind` →
icon mapping through `IconPaths`; asset paths and localized text never travel
through `WorldEvent`. `DomainBoundaryTests` scans every C# source below
`game/scripts/Domain/` and fails if it finds a Godot reference or `res://` path,
turning the domain/presentation rule into an executable constraint.

### Presentation snapshots

`CityWorldController` projects the mutable world into immutable,
Godot-free read models: `CityStatusSnapshot`, `ConstructionSnapshot`, and
`BuildingDetailSnapshot`; `CityMacroSnapshot` additionally projects every
`NaturalResourcePatch` type, its independently gatherable units, and each
Cultivation Site's state/timing on its stable lot. `CityStatusSnapshot`
projects daily ration, Food horizon, protected target, time to first harvest,
ledger-backed resource totals/availability, founding lineage, and current
population/housing capacity. The status
strip, construction panel, building-detail
shell, worker slots, assignment panel, production panel, and forest gather panel
render those snapshots instead of traversing `CityWorld` or retaining domain
entities. Commands still flow through the controller and domain; snapshots are
read-only copies and never become a second source of truth. The v14 slot
enumerates every citizen through the same `CityMacroSnapshot.CitizenItem`
record, so the future `RosterView` will render without re-querying
`CityWorld`. Domain `PatchChanged` events cross the controller as
`NaturalResourceStateChanged`; the macro view rebuilds its snapshot after a
successful gather, while the controller marks the world dirty for autosave.
`CultivationSiteChanged` crosses as `CultivationSiteStateChanged`; sow and
harvest wrappers save immediately and the macro view rebuilds from a fresh
snapshot.

### Founder narrative boundary

The astral onboarding keeps authored content and hidden weights in
`FounderNarrativeCatalog`, stable answers in `FounderNarrativeSession`, and
full recomputation in `FounderNarrativeScorer`. These domain types do not
import Godot. `AstralOnboardingView` owns layout, focus, progressive board
reveal, and text fades. `FounderArrivalSequence` owns only the fall, placeholder
impact, and title-card presentation.

The result enters the world through `HeroCreationRequest` and creates one
ordinary `Citizen` with the Hero role. `CitizenOrigin.AstralFounder` is compact
persisted metadata, not a parallel founder entity. The first free
`ConstructionLot` remains the authoritative fall/building-site relation.
`HeroCreated` is emitted only after the initial atomic save succeeds; a failed
save keeps the answer session and retries without creating a second citizen.

## 9. UI themes and resolution

The project uses one shared Control hierarchy and one global `Theme`; lineage
identity never duplicates scenes or functional controls. `LineageThemeRegistry`
loads the exported `StyleBoxTexture` resources under
`res://assets/ui/lineages/<lineage>/`, caches them, and resolves missing
components through the same-lineage `panel` before falling back to the existing
project theme. `LineageThemeSignals` is the presentation-only autoload that
notifies visible controls when the founder's persisted lineage changes.

Fonts and icons remain independent of lineage. Jacquard 24 and Jacquard 12 are
used for display titles and the HUD brand, Jacquarda Bastarda 9 for the founder's
name, Jersey 15 and Jersey 10 for headings, controls and reading text, and
Micro 5 for the compact HUD's rows and figures. All six are grid fonts and all
six are imported the same way: no antialiasing, light hinting, disabled subpixel
positioning, and fixed 1.0 oversampling — enforced by
`tools/Test-PixelFontImports.ps1`. The reference viewport is
explicitly 1280×720 with `canvas_items` stretch and `expand` aspect handling.

**Pixel-perfect rules**, which the rest of the presentation layer inherits:
logical resolution 1280×720, nearest filtering, integer positions, integer
scale, and no fractional coordinates for edges. The two official review sizes
are 1280×720 and 1920×1080 (exactly 1.5×), so the logical composition must be
identical at both.

The world (macro city and detailed scenes) lives under a `Camera2D`/`Node2D`
and the HUD lives in an independent `CanvasLayer` the camera never affects.
That layer split is what makes a free camera safe: an earlier decision avoided
`Camera2D` entirely to keep the HUD still, which is no longer necessary.
Camera modes, the two depth models and the surfaces that own each zone are
canon in [`../presentation/visual-language.md`](../presentation/visual-language.md).

## 10. Spatial grammar (macro & expedition)

World of Goses projects both city and expedition through **one
shared depth-band grammar**: a 2D presentation that simulates pseudo-3D
perspective via non-uniform trapezoidal bands, not actual 3D, not
2.5D elevation. The grammar is **presentation only**; gameplay
remains 1D in both pillars.

```text
shared depth-band primitives (presentation)
        │
        ├── MacroStreetRenderer            (urban: parcels, buildings,
        │                                   territory, wear, navigation)
        │
        └── ExpeditionPathRenderer         (path + chunks + parallax)

Travel.PositionX            ──► world offset of the path renderer
Combat Combatant.PositionX  ──► projection of combatants on the playable band
```

The two renderers share **vocabulary**, not instances: they consume
common projection / terrain primitives (see `StreetDepthProjection`,
`TerrainAtlas`, the band geometry exposed for the expedition in #19)
and they each keep their own domain semantics. `MacroStreetRenderer`
remains explicitly urban; `ExpeditionPathRenderer` does not know
about plots, buildings, navigation or territory.

The two renderers **never** share a configurable boolean (no
`isExpedition`, `drawBuildings=false`, etc.) to keep semantics apart.
Adding a generic `GodRenderer` flag seam is forbidden; this is one of
the explicit decisions of issue #18/#19.

**Camera policies** differ:

- **Macro** — free camera by default with `MacroCameraController`;
  pan + zoom; the focus shifts and the depth window follows; a
  citizen-selection toggle adds observation follow without becoming
  movement control.
- **Expedition** — mostly locked framing. The party stays near a
  stable focal point on the playable band; the path's *world offset*
  is driven by the same domain `Travel.PositionX` (or, during
  encounter, the combat positions) and never becomes a second
  authoritative position. Parallax is a function of the same offset,
  not a separate clock.

**Invariants registered by #18 (do not reopen):**

1. Depth-band projection is a 2D presentation primitive; it never
   introduces `PositionY`, `DepthPosition`, lanes or a navmesh.
2. Expedition domain positions stay 1D (`Travel.PositionX`,
   `Combatant.PositionX`). Visual depth is presentation state only.
3. `Travel.PositionX` remains the authoritative travel progress;
   combat `PositionX` remains the authoritative encounter progress;
   the world scroll is **derived** from those and never persists a
   parallel offset.
4. The expedition path is **visually** infinite through recycled
   segments; no chunk ever enters domain persistence.
5. `BuildingDetailView` as a fullscreen top-level navigation is
   retired (issue #20). Its capabilities — `BuildingDetailSnapshot`,
   `AssignmentPanel`, `ProductionPanel`, `ResourceInventoryPanel`,
   Home capacity/resting, Primitive Axe crafting, Town Hall
   prospect/recruit — survive inside a contextual `BuildingInspector`
   that sits over the visible macro without hiding it.
6. No `LateralBattlefield`, no fallback stage. Encounter and travel
   share the same `ExpeditionPathRenderer`/stage; the
   "lateral expedition" framing was a transient prototype and is not
   the direction of record.
7. **One playable band, and consumers ask for it.** Terrain, party,
   enemies, objective and dressing all resolve their row through
   `ExpeditionPathRenderer.PlayableDepth` / `IsPlayableDepth`. No
   caller may re-derive it from a row index. It was derived twice
   once — the terrain painted `depth == RowCount - 1` while gameplay
   stood on depth 0 — and the result was a path at the horizon with
   the party walking beside it (#27).
8. **What the renderer computes, it computes in one place.**
   `ExpeditionPathComposition` turns (chunks, world offset, anchor)
   into the exact screen geometry `ExpeditionStage._Draw` paints, and
   `ExpeditionPathCamera` owns the world offset across the Travel →
   Encounter → Return sequence. Both are pure and Godot-free because
   the stage is a `Control` and cannot be instantiated in the test
   assembly: without them, a test of "the path scrolls" can only
   re-enact the calls it believes the stage makes. It is how #22-#25
   came to be closed with a recycler, parallax factors and
   deterministic dressing that between them reached no pixel — every
   part covered, nothing connected. A change that moves a drawing
   decision back into `_Draw` puts it beyond reach of the suite.

## 11. Time advancement

`WorldTimeAdvance` is the single domain seam that advances an elapsed tick
range, and `CityWorld.AdvanceWorldTick` is the only tick method there is — so
offline catch-up differs from live play in **batching strategy alone, never in
rules**.

What may be batched, and what may not:

- A world with no buildings or projects advances in one batch.
- A structured but quiescent city (no work assignments) batches every tick that
  stays inside the current day/night phase: upkeep, WellFed expiry, stop causes
  and the clock are applied arithmetically. Sunrise and sunset stay canonical
  stepped ticks.
- Completed projects, forests due for demolition and assigned workers force the
  canonical stepped path.
- A citizen in transit clamps the batch to one tick *before* their arrival, so
  the arrival is always reached by a stepped tick. Batching onto it would move
  the clock past a scheduled state change without running it.

Snapshot JSON and the full causal-event sequence are tested for equivalence
over multiple days. Any new advancement strategy lives behind this seam and
owes the same proof before it may replace stepped execution.

Events, not seconds, are the unit of meaning:

- The world does not tick every real second.
- Discrete events are produced as time elapses ("one armour set completed",
  "coal ran out", "an expedition returned").
- Events are causal: each refers to the state that caused it.
- The event log is the source of truth for the causal report, and offline
  reports capture a log cursor before the batch, so new event kinds need no
  category-specific counter and cannot replay older events.

## 12. What is explicitly out of scope

The following are out of scope for the initial architecture and any
system work that follows:

- A backend, a server, a database, a microservice, an API, a CDN.
- A mobile application, even as a future stub.
- Multiplayer, networking, account systems.
- Modding tools, custom editors, Godot plugins beyond standard use.
- Telemetry, analytics, crash reporting, A/B testing.
- A second gameplay loop, a second city, a meta layer between cities.
- A graphical installer, a launcher, a settings UI.

These are listed so that the next agent or contributor does not
"helpfully" add them. The README and `AGENTS.md` repeat the same
boundary.

## 13. Evolution of the architecture

The architecture is allowed to evolve. The rules for evolving it are:

1. **The simulation must remain independent of the engine.** That is
   the strongest boundary. Domain code does not import `Godot.*`.
2. **Documentation must change with the architecture.** A change to the
   folder layout, dependency rule, or build command must be reflected
   in `README.md` and the relevant `docs/` file in the same commit.
3. **No speculative abstractions.** A pattern is added when there is a
   concrete need in code, not before.
4. **No premature systems.** Mobile, networking, and the full city are not
   built before the prototype validates the need.
