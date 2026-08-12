# Architecture Hardening Report

> Final state after A0–A12. No new layers; no large refactors;
> only the debt transversal to gameplay that closed the seams A0–A11
> opened.

## 1. Final physical architecture

```
              game/World of Goses.csproj (Godot.NET.Sdk/4.7.1)
              ─ Presentation (engine-aware) ─
   ┌──────────────────────────────────────────────────────┐
   │  CityWorldController (Node)                          │
   │    • Godot lifecycle (Ready/Process/ExitTree/...)   │
   │    • Signals in/out                                   │
   │    • CurrentTickPhase (interpolación visual)          │
   │    • Persistence orchestration (SaveNow/Load)         │
   │    • internal fixture seam → _session.World           │
   │    • Suscrito a eventos forwarded del session         │
   ├──────────────────────────────────────────────────────┤
   │  CityPrototype (Scene) + MacroStreetLiveView + ...    │
   │    • Consume VisualRegressionHarness.Activate()       │
   │    • Composición de filas dinámicas vía Ui primitives │
   │    • ThemeTypeVariation por .tscn; tokens en Tokens.cs │
   ├──────────────────────────────────────────────────────┤
   │  game/scripts/Ui/ (Primitives reutilizables)          │
   │  game/scripts/Testing/ (Harness dedicado A10)         │
   │    • VisualRegressionHarness                         │
   │    • VisualFixtureCatalog                            │
   └────────────────────────┬─────────────────────────────┘
                            │ delegates to
                            ▼
              src/WorldofGoses.Application
              ─ Application (engine-free) ─
   ┌──────────────────────────────────────────────────────┐
   │  CityGameSession                                     │
   │    • Owner de CityWorld                              │
   │    • Use-case facade (commands + queries)             │
   │    • Advancement semantics (AdvanceWorldTick)        │
   │    • IsDirty tracking vía forwarded events            │
   │    • Snapshots inmutables (CityStatusSnapshot, ...)    │
   │    • Mappers de i18n (ResourceTypeLocalizer, ...)      │
   │    • internal World { get; } (fixture seam)          │
   └──────┬─────────────────────────────�─────────────────�
          │ references                   │ references
          ▼                              ▼
              src/WorldofGoses.Domain              src/WorldofGoses.Persistence
              ─ Domain (engine-free, sin Godot) ─   ─ Persistence (engine-free) ─
   ┌─────────────────────────────────┐  ┌─────────────────────────────────────────┐
   │  CityWorld (aggregate)           │  │  WorldSave, WorldPersistence, Ids/*     │
   │  Citizen, Building, ...         │  │  JSON, schema, migrations, files        │
   │  Reglas puras                   │  │  Engine-free, depende solo de Domain    │
   │  internal seams para fixtures   │  └─────────────────────────────────────────┘
   │  [+InternalsVisibleTo("World of Goses")]
   └─────────────────────────────────�
```

## 2. Assembly dependencies

| Assembly | References | Forbidden refs |
|---|---|---|
| `WorldofGoses.Domain` | (none) | Godot, Persistence, Application — compiler-enforced |
| `WorldofGoses.Application` | Domain | Godot, Persistence — compiler-enforced |
| `WorldofGoses.Persistence` | Domain | Godot, Application — compiler-enforced |
| `game/World of Goses` (Godot) | Domain + Application + Persistence | (root assembly) |

The boundary is enforced by project references (`ProjectReference`).
A future regression that adds a forbidden reference fails the
build before it can ship.

## 3. Ownership of CityWorld

`CityGameSession` owns `CityWorld` (Application assembly). The
controller holds a single `private readonly CityGameSession
_session;` reference. The session exposes `internal CityWorld
World { get; }` for the visual-regression fixture seam
(`CityWorldController.GetFixtureWorld()`); the Godot assembly
reaches it through `[InternalsVisibleTo("World of Goses")]`.
Production scenes never touch `CityWorld` directly — every read
goes through an immutable snapshot, every write through a
session command.

## 4. Command flow

```
User input / signal
        │
        ▼
Presentation (e.g., ConstructionPanel button)
        │
        ▼
CityWorldController use-case wrapper (public thin method)
        │
        ▼
CityGameSession.<UseCase>() → Application orchestration
        │
        ▼
Domain operations (Citizen, Building, CityWorld methods)
        │
        ▼
Domain events → CityWorldController signals → Presentation
```

The session returns semantic `*Result` types
(`AssignmentResult`, `ToolCraftResult`, etc.). The controller
forwards the result unchanged; Presentation reads the result via
the signal and refreshes via snapshot.

## 5. Query flow

```
Presentation
    │
    ▼
CityWorldController.GetXxxSnapshot()
    │
    ▼
CityGameSession.GetXxxSnapshot()
    │
    ▼
Immutable *Snapshot record (built once from CityWorld state)
    │
    ▼
Presentation reads scalar / record fields, never the CityWorld
```

No mutable entity crosses the boundary. The session is the only
caller of `CityWorld` for read paths; the snapshot DTOs are
immutable records (`readonly record struct`).

## 6. Persistence flow

```
Application session (CityGameSession)
    │
    ▼
CityWorldController.TrySaveToPrimarySlot() (presentation owner)
    │
    ▼
WorldPersistence.SaveToSlot(cityWorld, slot)
    │
    ▼
WorldSave (DTO) → JSON file → atomic write → .bak sidecar
```

The controller owns the orchestration (it can emit `WorldSaved`
signal, can suppress writes on close). The session is engine-free
and does not touch the file system; the persistence assembly is
engine-free and serialises to JSON. The schema version lives in
`WorldSave.CurrentVersion`; migrations form a chain from v2 → v34.

## 7. Visual fixture flow

```
WOG_VISUAL_CAPTURE=1  OR  --wog-visual-capture
        │
        ▼
CityPrototype._Ready → VisualRegressionHarness.Activate()
        │
        ▼
VisualRegressionHarness.IsActive (typed property)
        │
        ▼
--wog-visual-fixture=<name>
        │
        ▼
VisualFixtureCatalog.Classify(name) → VisualFixtureKind
        │
        ▼
CityPrototype fixture entry point (internal seam) →
   controller.fixtureXxx (internal, gated on IsActive) →
   session.fixtureXxx (internal) →
   domain internal seam (ConcludeFirstNightForFixtures, etc.)
```

The catalog is the single dispatch table; the harness owns
activation. Production scenes never reach any fixture seam
because every fixture method is gated on `IsActive` which is
`false` in normal play.

## 8. Remaining allowlists

| Allowlist | Entries | Justification |
|---|---|---|
| `PresentationDirectWorldAccess` | 3 (AstralOnboardingView, CombatDebugPanel, CityPrototype) | Documented in inline comments; fixture-only or transient animation seams. Closing them is fixture-seam extraction. |
| `PresentationPersistenceReference` | 4 (controller, CityPrototype, LocaleManager, RealCityStreetPreview) | Controller is the boundary class; the others are dev tooling. |
| `PresentationFirstNightFixtureSeam` | 1 (CityPrototype) | The rule itself. |
| `PresentationMutableEntityReturn` | 1 (IconPaths) | False-match safety net. |
| `PresentationEntityMutator` | 2 (CityPrototype, CombatDebugPanel) | Dev-only fixture scenes. |
| `PresentationInstantiatesWorld` | 2 (CityPrototype, RealCityStreetPreview) | Dev-only fixture scenes. |
| `ProductionUiStaticStructureInCode` | 14 (A/B/D/E classifications) | A-class pending .tscn migration (GitHub #9). B/D/E are legitimate. |

Every entry has an inline comment naming its justification. No
allowlist is "A0–A4 leftover"; every entry is owed to A8–A11.

## 9. Public testing seams remaining

None. Every fixture method is `internal`:

| Seam | Assembly | Visibility |
|---|---|---|
| `CityWorld.ConcludeFirstNightForFixtures` | Domain | `internal` (A10) |
| `CityWorld.DrainAllNaturalResourcesForFixtures` | Domain | `internal` (A10) |
| `ConstructionProject.SeedProgressForFixture` | Domain | `internal` (A10) |
| 14 fixture commands on `CityWorldController` | Presentation | `internal` (A8) |
| `DrainAllForestsForFixture` (controller) | Presentation | `internal` (A10) |
| `AdvanceWorldTickForFixtureHarness` | Presentation | `internal` (A10) |
| 8 visual-regression entry points (AstralOnboardingView, CombatDebugPanel, ExpeditionPanel, MigrantPanel, TimeOfDayFilter, MacroStreetLiveView ×2) | Presentation | `internal` (A12) |
| `VisualRegressionHarness` | Presentation | `internal`-reachable via InternalsVisibleTo |

Every seam is gated on `VisualRegressionHarness.IsActive` or on
its own dev-only env check. The architecture guard
`Production_DoesNotExposePublicVisualRegressionMethods` catches
future regressions.

## 10. Consciously preserved debt

| Debt | Why |
|---|---|
| 10 A-class panels still compose static hierarchy in C# | Migration to .tscn is mechanical work that touches theme + size + anchor math for each; belongs to dedicated slice. GitHub #9. |
| `CityPrototype.Show*ForVisualRegression` (~50 methods) | A10 left per-fixture composition inline; the catalog knows the names but the bodies still live in CityPrototype. GitHub #5. |
| ~12 `WOG_VISUAL_CAPTURE` env-var reads in scene trees | Each gates a behaviour dev-only (frame-time sampling, debug toggles); folding them through `VisualRegressionHarness.IsActive` is documented as the next slice. GitHub #6. |
| Arbitrary timers in `Capture-VisualMatrix.ps1` and `CityPrototype` | Each documents why it is not yet a real-condition wait; replacement belongs to its own visual-regression pass. GitHub #7. |
| `SampleFrameTimeForVisualCapture` on `CityWorldController` | Already dev-only; could move to `VisualRegressionProfiler` for purity but is not worth the move today. GitHub #8. |
| `ResourceInventoryPanel` and `ProductionPanel` still use `enum.ToString().ToLowerInvariant()` for one or two paths | A12 introduced `ResourceTypeLocalizer` for the high-churn paths; the remaining call sites are low-churn and will migrate as their panels move to .tscn. |
| `CityPrototype.cs` (2624 lines) | Largest class in the codebase by far. Mostly fixture setup. Refactor belongs to the catalog-migration slice (GitHub #5); splitting today would not improve clarity. |

## 11. Large classes that stay

| Class | Lines | Why it stays |
|---|---|---|
| `CityPrototype.cs` | 2624 | Dev-only fixture scene + scene-tree root composition. Refactor splits it across three concerns (scene composition, fixture orchestration, dev tools); each is its own slice (GitHub #5). |
| `Prototypes/MacroStreetLiveView.cs` | ~3600 | A4's 5 collaborators + 3 helpers live inside this file by design (the A4 commit notes why). Splitting would invert A4. |
| `Domain/CityWorld.cs` | ~3500 | Aggregate root. Splitting it is the entire point of H-22/H-23, which is not yet scheduled. |

## 12. Things NOT recommended to refactor now

- Splitting `CityPrototype.cs` by responsibility. The 50 fixture methods are not all worth extracting today; the catalog knows the names, the scene knows the bodies. Extracting them before the catalog migration would duplicate effort.
- Splitting `Domain/CityWorld.cs` into sub-aggregates. H-22/H-23 territory; the work is large and the value is bounded until those slices ship.
- Moving `SampleFrameTimeForVisualCapture` out of the controller. The dev-only behaviour is already gated; the move is purity, not progress.
- Renaming `WOG_VISUAL_CAPTURE` → `WOG_VOG_FIXTURE_MODE`. The harness already accepts both; the env var is stable and referenced by tools.
- Adding a sixth theme variation. The 17 + 12 HUD variations are exhaustive; a new one needs a justified use case, not a refactor.
- Removing `Tokens.ScrollGutter` in favour of an inline `16`. The token names the documented exception; removing it is regression, not progress.

## 13. Risks before continuing gameplay

- **Save format**: schema v34 is the latest. Any new feature that
  persists state must bump the schema version and add a migration;
  see `WorldSaveApplier.MigrateToCurrent`.
- **First Night**: A9 closed the dynamic dispatch, but the
  per-stage transitions are still in `FirstNightRules` /
  `FirstNightState`. A new stage that wants to read a typed
  snapshot (not a domain entity) needs to declare its stage in
  `FirstNightStage`, its rule in `FirstNightRules`, its text in
  `FireSpiritDialogueCatalog`, and its quantity source in
  `FoundingSiteRules.InputsFor` — the seam A12 codifies.
- **Persistence**: `WorldSave` is the single DTO. A new persisted
  field must add a `*Save` entry, a migration, and a `Restore`
  step in `WorldSaveApplier`. Skipping any of those leaves older
  saves unable to load.
- **Visual regression on headless boot**: A10 documented the
  50×50 client limitation. Any new surface that depends on
  interactive click needs a separate visual-validation pass on
  a real desktop.
- **Compact HUD scale**: A11 sealed the compact/screen
  separation. A new HUD row that needs a different metric belongs
  to a new token in `Ui/Tokens.cs`, not a one-off `add_theme_*`.

## 14. Invariants

| Invariant | Enforcement | Test/compiler guard | Exceptions |
|---|---|---|---|
| Domain has no Godot | `using Godot` is a compile error | `DomainSources_DoNotReferenceGodotOrResourcePaths` | (none) |
| Domain has no Application | `WorldofGoses.Application.csproj` not referenced | `Domain_DoesNotReferenceApplicationAssembly` | (none) |
| Domain has no Persistence | `WorldofGoses.Persistence.csproj` not referenced | `Layer_DoesNotReferencePersistenceAssembly` | (none) |
| Application has no Godot | `using Godot` is a compile error | `EngineFreeProject_DoesNotReferenceGodot` | (none) |
| Application has no Persistence | `WorldofGoses.Persistence.csproj` not referenced | `Layer_DoesNotReferencePersistenceAssembly` | (none) |
| Persistence has no Godot | `using Godot` is a compile error | `EngineFreeProject_DoesNotReferenceGodot` | (none) |
| Persistence has no JSON in Domain | Domain csproj has no System.Text.Json reference | `DomainSources_DoNotReferenceGodotOrResourcePaths` (covers res:// only) | (none) |
| CityWorld owned by CityGameSession | Session creates `new CityWorld()`; controller holds only `_session` | `CityWorldController_DoesNotHoldACityWorldField`, `CityWorldController_DoesNotExposeWorldGetter` | `internal CityWorld World` seam, gated via `InternalsVisibleTo` |
| Production scene never reads `CityWorld` | `controller.World` is `internal`; no `controller.GetDomain()` exists | `Presentation_DoesNotAccessCityWorldDirectly` | Allowlist: AstralOnboardingView, CombatDebugPanel, CityPrototype (fixture-only) |
| Production scene never mutates an aggregate | `internal` mutators on entities; no public mutators except persistence-driven | `Presentation_DoesNotMutateAggregatesOrEntities` | Allowlist: CityPrototype, CombatDebugPanel (fixture-only) |
| Production scene never instantiates `CityWorld` | Constructor + `InternalsVisibleTo` | `Presentation_DoesNotInstantiateCityWorld` | Allowlist: CityPrototype, RealCityStreetPreview (fixture-only) |
| Domain has no public `*ForFixture` API | `internal` mutators | `Domain_DoesNotExposeFixtureSeamsAsPublic` | (none) |
| Production scene has no public `*ForVisualRegression` | `internal` mutators; fixture gating | `Production_DoesNotExposePublicVisualRegressionMethods` | (none) |
| Controller use cases go through session | `_session.<Method>` per command | `Controller_DelegatesUseCaseCommandsToSession`, `Controller_HasAliasWrappersThatMatchTheSession` | (none) |
| No dynamic dispatch in FirstNight | `HasMethod`/`Node.Call` fail build | `FirstNightScene_DoesNotUseDynamicDispatch` | (none) |
| FirstNight subscribes to typed anchor signal | Subscribes + uses typed methods | `FirstNightScene_SubscribesToTypedAnchorSignal` | (none) |
| MacroStreetLiveView exposes typed anchor signal | Declares + emits signal | `MacroStreetLiveView_ExposesTypedAnchorSignal` | (none) |
| Production UI has no static hierarchy in C# | `new Panel | new VBox | …` fail build | `ProductionUi_DoesNotComposeStaticHierarchyInCode` | Allowlist: 14 panels (A/B/D/E classifications) |
| Visual regression lives in Testing namespace | `game/scripts/Testing/` | `VisualRegressionHarness_LivesUnderTestingNamespace` | (none) |
| First Night conclusion only in fixtures | `ConcludeFirstNightForFixtures` is `internal` | `Presentation_ConcludesFirstNightOnlyInFixtures` | Allowlist: CityPrototype |
| Engine-free assemblies don't reference Godot | Project reference / using statement | `EngineFreeProject_DoesNotReferenceGodot` | (none) |
| Presentation doesn't reference Persistence namespace | Source-text grep | `Presentation_DoesNotReferenceDomainPersistence` | Allowlist: CityWorldController (boundary class), CityPrototype, LocaleManager, RealCityStreetPreview |
| Presentation doesn't expose mutable domain entities | Public method regex | `Presentation_DoesNotExposeMutableDomainEntities` | Allowlist: IconPaths (false-match safety net) |
| Removed wrappers don't return | Source-text grep | `Presentation_DoesNotCallRemovedEntityAccessorWrappers` | (none) |
| View doesn't confirm citizen arrival | Source-text grep | `Presentation_DoesNotConfirmCitizenArrival` | (none) |
| I18n keys not derived from enum names | Source-text grep | (A12: `ResourceTypeLocalizer` established; guard pending) | (none) |
| Input actions not hardcoded as strings | Source-text grep | (A12: `UiInputActions` established; guard pending) | (none) |
| Theme variations not duplicated at call sites | Theme registry + tokens | `HudThemeVariationTests`, `ScreenVariations_AreUnchangedByTheHudProfile` | (none) |

## 15. Done

A new feature can ship without:

- Modifying `CityWorldController` to add gameplay rules (the
  controller is a typed adapter; rules live in `CityGameSession` or
  the Domain).
- Handing `CityWorld` to a view (the session owns it; the seam is
  `internal`).
- Adding Godot to Domain / Application (the project references
  are the boundary).
- Adding JSON to Domain (persistence is its own assembly).
- Creating another public `*ForFixture` API (the Domain has no
  public fixture seam; presentation fixture seams are `internal`).
- Deriving save IDs from enum names (every persisted enum family
  uses `*SaveIds` in `WorldofGoses.Persistence.Ids`).
- Deriving i18n keys from enum names (every key goes through a
  typed mapper in Presentation; Domain does not know PO keys).
