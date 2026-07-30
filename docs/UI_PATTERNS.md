# UI Patterns and Reusability Guide

> North-star rules for **how** every presentational slice in this
> project is built. The fantasy, axes, and content live in the design
> bible (`docs/world-of-goses-design-bible/`); this file owns the
> component patterns, naming, state-binding, and migration checklist
> that keep the UI legible as the project grows across the city,
> lineages, and expeditions axes with offline persistence.

## 1. What the UI will have to render

The game is a single-city idle manager with RPG lineages and future
expeditions, persistent across sessions. Every UI screen we ship maps
to one of these axes:

| Axis | Player-facing surface | State owner |
| --- | --- | --- |
| **City overview** | Macro view, plots, status strip, log | `CityWorldController` → signals |
| **Buildings** | Detail panel, construction modal, gather/produce view | `BuildingDetailSnapshot` / `ConstructionSnapshot` |
| **Hero** | Profile screen, lineage showcase | `Citizen`, `LineageDefinition` |
| **Citizens roster** | List with assign / unassign actions | `CityWorldController.AvailableCitizensByPriority()` |
| **Expeditions** *(future)* | Planning modal, dispatch view, return summary | `ExpeditionController` (TBD) |
| **Chronicle / log** | Offline report, decision-needed feed | `WorldEventLog` |
| **Game time** | Pause / resume, day-night ribbon | `CityWorld.AdvanceWorldTick` cadence |

Every screen above must obey the rules in §3-§7. They apply today to
the city, hero, chronicle, and modal surfaces already shipping, and
they apply by extension to the future expeditions and citizens roster
work.

## 2. The three component patterns

Three reusable patterns cover the vast majority of UI widgets. Choose
exactly one per widget by answering the question in the *When* column.

| Pattern | When to apply | Concrete form | Naming |
| --- | --- | --- | --- |
| **PackedScene (.tscn reusable)** | Layout with several children, padding, margins, anchors, or themed chrome. Editor reuse matters. | `game/scenes/Components/<Name>.tscn`. Loaded via `PackedScene` from callers. Scene owns the layout; script owns behaviour. | `Components/<PascalCase>.tscn`. |
| **`[GlobalClass]` C# node** | Custom node class registered with the Godot editor, useful as a typed Control property or repeated typed widget with internal logic and a small footprint. | `partial class <Name> : <BaseControl>` decorated with `[GlobalClass]` in `game/scripts/Ui/`. Source generator registers it; instances appear in the **Add Node** menu. | `Ui/<PascalCase>Component.cs`. |
| **Static factory in C#** | Buttons / chips / rows created procedurally that must be identical across every callsite. Eliminates divergent per-callsite layouts. | `public static class Ui/StandardButtons` (or `…Chips`, `…Rows`) returning `new <Widget>()` with the project's canonical settings. | `Ui/<Plural>.cs`, methods `<PascalCase>`. |

### 2.1 PackedScene

Use a PackedScene when the widget:

- Combines two or more children (label + value, row + button, etc.).
- Needs anchors or containers that are tedious to spell by hand every
  time.
- Will appear in the **inspector** (designers tweak it).

Example targets: `StatChip.tscn` (icon + label + value), `EventRow.tscn`
(icon + summary + tick), `AssignmentRow.tscn` (citizen name + assign
button), `CityPlot.tscn` (placeholder rect + headline label).

Loader pattern:

```csharp
private static readonly PackedScene StatChipScene =
    GD.Load<PackedScene>("res://scenes/Components/StatChip.tscn");

private StatChip InstantiateStatChip() {
    var chip = (StatChip)StatChipScene.Instantiate();
    chip.Configure(IconPaths.User, "Population", "12");
    return chip;
}
```

### 2.2 `[GlobalClass]`

Use `[GlobalClass]` when:

- The widget has internal state or behaviour that PackedScene cannot
  encode (signals, methods, configuration API).
- The widget gets dragged into the scene tree by a designer.
- Repeated configuration matters (e.g. always at the same depth under
  a card container).

Current registered controls: `ModalHost`, `PanelHeader`,
`AssignmentRow`, and `SafeAreaMarginContainer`. Future targets include
`StatChip` and `ExpeditionCard`.

```csharp
[GlobalClass]
public partial class ExpeditionCard : PanelContainer {
    [Export] public ExpeditionSnapshot? Snapshot { get; set; }
    public event Action<ExpeditionId> DepartPressed;
    public void Refresh(ExpeditionSnapshot snapshot) { /*…*/ }
}
```

This `ExpeditionCard` then shows up in Godot's editor menu and can be
attached to a `.tscn` directly.

### 2.3 Static factory

Use a factory when the widget is:

- Created entirely from C# (no `.tscn` editor reuse).
- Repeated across multiple script files (the diagnostic that lit up
  this guide: `HeroProfileView._backButton` was a plain `Button` while
  `BuildingDetailView.BackButton` was an `IconButton` with an arrow,
  and `HeroAccessButton` shipped without its user icon).
- Cheap to construct (single Control instance).

Targets: every action button, every chip, every small row whose
look-and-feel must be identical.

```csharp
public static class StandardButtons {
    public static IconButton BackToCityButton() { /* consistent shape */ }
    public static IconButton ViewHeroButton() { /* consistent shape */ }
    public static IconButton PauseResumeButton(bool running) { /* … */ }
}
```

When you find two callsites creating the same kind of widget with
slightly different properties, **promote them to a factory**.

`IconButton` uses Godot's native `Button.Text` and `Button.Icon` renderer.
Do not hide the native text behind an internal `Label`: the nested label does
not inherit a Button-based theme variation reliably and can disappear or
measure differently between containers.

Theme color entries use Godot's native `<Variation>/colors/<property>` keys.
`font_colors` is not a valid Theme registry namespace and silently falls back
to engine colors.

### 2.4 When NOT to fall back to a raw `new Button`

Every raw `new Button { Text = … }` is a future maintenance liability.
The rule: if the widget is reused more than once, route through a
factory; if its layout is non-trivial, route through PackedScene; if
its behaviour is bespoke, route through `[GlobalClass]`. A raw `Button`
is only acceptable for **single-use, throwaway** debug widgets.

## 3. Project-wide naming conventions

| Layer | Pattern | Example |
| --- | --- | --- |
| Folder | `game/scripts/Ui/` for reusable controls. `game/scripts/<Screen>/` is fine for screen-local scripts. | `Ui/ModalHost.cs`. |
| Class | PascalCase. Ends in `Component` only if there is no clearer noun. | `ModalHost`, `PanelHeader`, `StatChip`. |
| File | Matches class name. One class per file. | `ModalHost.cs`. |
| Signals | Past-tense verb-noun PascalCase. | `Closed`, `DepartPressed`, `CitizenActivated`. |
| ExtResource ID in `.tscn` | Lowercase tag, optional numeric suffix. | `18_modalhost`, `19_tooltip`. |
| `.tscn` factory paths | `res://scenes/Components/<PascalCase>.tscn`. | `res://scenes/Components/StatChip.tscn`. |
| Theme variation names | Match the `default_theme.tres` registry; never coin a new variation inline. | `GameTitle`, `PanelTitle`, `TooltipText`, `BodyText`, `NumericText`, `ButtonText`. |

## 4. State binding — signals, not polling

Every UI screen reads state through **immutable snapshots** owned by the
controller and refreshed via **domain signals**. The pattern:

```csharp
public override void _Ready() {
    _controller.HeroCreated += OnHeroStateChanged;
    _controller.SelectionChanged += OnSelectionChanged;
    _controller.BuildingStateChanged += OnBuildingStateChanged;
    Refresh();
}

private void Refresh() {
    var snapshot = _controller.GetCityStatusSnapshot();
    _summaryLabel.Text = snapshot.HeroName;
}

private void OnHeroStateChanged(int _) => Refresh();
```

The screen **never**:

- Reads `CityWorld._citizens` directly.
- Polls in `_process` / `_physics_process`.
- Mutates domain objects in response to its own UI events (`Pressed`
  emits a signal *out*; the controller decides what to do).

Domain events the UI must understand:

| Signal | When | Carries |
| --- | --- | --- |
| `WorldTickAdvanced` | every tick | current tick |
| `HeroCreated` | once | citizen id |
| `BuildingStateChanged` | building produced / demolished / configured | building id |
| `ProjectStateChanged` | construction progress changed | project id |
| `SelectionChanged` | player switched view | enum |
| `CitizenAssignmentRejected` | assignment invalid | outcome |

When you add an axis (e.g. expeditions), add **one** signal set that
the screen subscribes to; do not poll the world in `_process`.

## 5. Theming — three-font hierarchy

The project ships **exactly three** font families and seventeen explicit
Label/Button text variations, declared once in
`game/assets/ui/default_theme.tres`. The
rules:

1. **Geist Pixel** — identity, drama, era changes.
   `GameTitle`, `ScreenTitle`, `EventTitle`.
2. **Jersey 10** — structure, navigation, buttons, panel chrome,
   sub-titles, building names.
   `PanelTitle`, `SectionTitle`, `ButtonText`, `ButtonPrimary`,
   `ButtonWarning`, `TabText`, `BuildingName`.
3. **Pixelify Sans** — reading, content, tooltips, body, numbers.
   `BodyText`, `BodySmall`, `ErrorText`, `TooltipText`, `DialogText`,
   `TableText`, `NumericText`.

Every Label and every Button MUST set one of these variations
explicitly — never rely on the engine's default font. The base
`Label` and `Button` themes in `default_theme.tres` carry Pixelify
defaults so an unannotated control still looks like the project, but
new code must declare the variation it wants.

Adding a new variation requires:

1. A clear, justified use case documented in the PR.
2. The variation declared in `default_theme.tres` with a single source
   of font / size / colour triple.
3. An entry in the role mapping above and, when the font hierarchy changes,
   the typography section of
   `docs/world-of-goses-design-bible/08_VISUAL_UI_AND_ASSET_GUIDELINES.md`.

Coin-ing variations inline (`theme_override_font_sizes/font_size = 42`)
is allowed only for **single-screen, throwaway** debug UI.

### Pixel-font rendering profile

All three project fonts are dynamic TTF sources but must rasterize as solid
pixel typography. Their committed `.ttf.import` profiles therefore keep
`antialiasing=0`, `generate_mipmaps=false`,
`multichannel_signed_distance_field=false`, `subpixel_positioning=0`, and
`oversampling=0.0`.
`oversampling=0.0` delegates the factor to the viewport so 1080p receives a
fresh target-size raster instead of a 720p glyph atlas enlarged by 1.5. The
global canvas texture filter remains Nearest. Do not enable MSDF for these
fonts: it always uses grayscale antialiasing and weakens the hard-edged pixel
language, especially at body sizes. Run `tools/Test-PixelFontImports.ps1` and
review `TypographySpecimen.tscn` at 1280×720 and 1920×1080 after changing a
font, font size, stretch setting, or theme variation.

## 6. Save / load integration

### Player-facing time

Simulation ticks are an internal domain and persistence unit. UI labels,
tooltips, buttons, Chronicle rows, and reports must never expose a tick count or
mislabel ticks as real seconds. Use `SimulationTimeText.FormatDurationLocalized`
for elapsed/remaining durations and `SimulationTimeText.FormatLocalized` for a
specific world date. Player-facing durations are expressed as world days,
hours, and minutes.

The UI must not store state the player cares about. Every state object
that affects the simulation lives in the domain (`Building`,
`Citizen`, `ConstructionProject`, future `Expedition`, future
`BuildingPolicy`). The UI is a read-only view of those plus
ephemeral UX state (current screen, scroll position, last opened
building) that does NOT round-trip through `WorldSave`.

When the future slice introduces **UI-driven settings** that should
persist (e.g. the `MinStock`/`MaxStock`/`Priority` triplet exists in
the domain but isn't surfaced; when it is, those values live in
`Building.ProductionPolicy` and round-trip through `BuildingSave`).
Use the same rule: **persisted state lives in the domain record**.
The UI field widget reads/writes the domain via
`CityWorldController.SetProductionEnabled(...)`, never directly on
`Building`.

Ephemeral UI state (current modal, focus, scroll position) lives in
the script's private fields and is rebuilt from scratch on load.

## 7. Navigation, focus, and input

Four rules apply to every screen:

1. **Grab a default focus** on enter (`_backButton.GrabFocus()` on
   `HeroProfileView`, the appropriate primary button on the
   construction modal). Gamepad players must land on a focused
   control.
2. **Close affordances are modal-relative, not screen-relative.**
   A modal uses X / ESC / click-on-scrim; a sub-screen uses a back
   button (factory-built, see § 2.3); the macro view has no "close"
   because it IS the persistent home. Be explicit which surface you
   are on. Macro actions (`View hero`, `Construction`) are visible only
   on the macro view; sub-screens own a local title + Back header.
3. **`ui_cancel` (ESC) is owned by the topmost modal**, not by the
   screen underneath. `ModalHost` captures `ui_cancel` only while the
   modal is open. The macro view does nothing with ESC.
4. **Use one selection router until nested navigation exists.** The current
   prototype has mutually-exclusive macro, building-detail, and hero-profile
   selections plus one modal layer, so `CityWorldController.Selection` and
   `ModalHost` are the deliberately small navigation stack. Introduce a
   general push/pop stack only when a second nested screen or modal requires it.
5. **Pointer gestures belong to the hovered UI first.** In particular, the
   wheel remains reserved for a visible ancestor `ScrollContainer` even when
   that container is already at its first or last row. World cameras must use
   `UiInputBoundary` before treating an unhandled wheel event as zoom; reaching
   a scroll limit must never leak the gesture into the city behind the panel.

Mouse + gamepad coexistence is the default expectation: hover
triggers tooltips, but gamepad focus also drives the selection ring
without collision. Keep `FocusMode = All` on every action control.

## 8. Anti-patterns — never do these

| Anti-pattern | Why it bites | Fix |
| --- | --- | --- |
| `new Button { Text = "X" }` inside a screen. | The widget drifts from the canonical version; the player sees two "back" buttons that look different. | Route through `Ui/StandardButtons.<X>Button()`. |
| Two `.tscn` files instantiating the same kind of button with different `icon_path`/`label`/size. | Same drift, plus it survives code review by hiding in scene files. | Consolidate via the factory or via `Components/<Name>.tscn`. |
| Reading a domain field from a UI `_Process` tick. | Couples the UI to internal layout and runs work the simulation doesn't need. | Subscribe to the relevant signal. |
| Setting `position` on a child of a `Container`. | The container overrides it; the value silently does nothing. | Drop the child into the container and use size flags / separation. |
| Adding a new theme variation per call site. | 14 explicit variations already; wildcards defeat centralised theming. | Reuse an existing variation or add the variation to `default_theme.tres` with a typed justification. |
| Building modal that cannot be closed without selecting an option. | Player with no materials is stuck. | Always X / ESC / click-on-scrim, plus the options, in that order. |
| Custom tooltip overlay that resizes a Panel with `PanelContainer.MinimumSize` set to (200, 1). | Creates elongated tooltips that don't match the engine popup shape. | Use the engine popup with the project's `Label/font` base. |

## 9. Migration & contributor checklist

When touching an existing screen:

1. **Audit `.tscn` siblings.** Open the screen's parent `.tscn`; for
   every direct child that displays text or accepts input, confirm
   `theme_type_variation = "<One of the registered variations>"` is present.
2. **Audit `new Label / new Button / new *Container / new SpinBox`**
   inside scripts. Every one of those must either (a) belong to a
   `Components/<Name>.tscn`, (b) be a `[GlobalClass]` typed widget,
   or (c) come from `Ui/Standard*`.
3. **Audit signals**. Every UI script subscribes via `_controller.<X>`
   events. No raw `_world.…` access.
4. **Audit close paths.** Every modal stack must close via at least
   one of X / ESC / click-on-scrim. Add the missing one.
5. **Audit save round-trip.** Any persisted UI setting must live on
   the domain record (`Building.ConfigureProductionPolicy`,
   future `Expedition.Policy`, etc.).

PRs touching the UI must include the audit findings in the
description (`- affected: CityMacroView, CityStatusPanel, BackButton`).

## 10. Forward-looking — expeditions and citizens roster

When the expeditions and citizens roster screens arrive they MUST
inherit the patterns in this file:

- **Expedition planning modal**: open via `ModalHost`. Three tabs
  (crew, route, supplies). Use `Components/<TabContent>.tscn` for
  each. Wrap in a `PanelHeader`. Close via X / ESC / scrim, like
  construction.
- **Citizen roster**: bottom-dock collapsible, focused on the
  controller's `AvailableCitizensByPriority` order. Use a PackedScene
  `Components/CitizenRow.tscn`. Assignment controls come from a
  `StandardAssignmentButton` factory so the panel and the
  expedition-crew picker share vocabulary.
- **Notification feed**: derived from `WorldEventLog`. The
  `OfflineReportPanel` already implements the structure — extend it
  with a "decisions needed" anchor reachable from anywhere on the
  macro view via an icon-button factory.

## 11. Quick reference — where things live

| Concern | File |
| --- | --- |
| Project-wide theme | `game/assets/ui/default_theme.tres` |
| Modal scaffold | `game/scripts/Ui/ModalHost.cs` |
| Modal header | `game/scripts/Ui/PanelHeader.cs` |
| Reusable buttons | `game/scripts/Ui/StandardButtons.cs` |
| Assignment row | `game/scenes/Components/AssignmentRow.tscn` |
| Safe-area container | `game/scripts/Ui/SafeAreaMarginContainer.cs` |
| Tooltip helpers | `game/scripts/Ui/TooltipPanel.cs` |
| Snapshot contracts | `game/scripts/*Snapshot.cs` (`CityMacroSnapshot`, `HeroProfileSnapshot`, `BuildingDetailSnapshot`, `ConstructionSnapshot`, `CityStatusSnapshot`) |
| City world façade | `game/scripts/CityWorldController.cs` |
| Component PackedScenes | `game/scenes/Components/` |
| Current audit state | `docs/UI_AUDIT.md` |
| Status snapshot | `docs/CURRENT_STATUS.md` |
