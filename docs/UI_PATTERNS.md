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
| **Game time** | Speed multiplier, day-night ribbon (no pause: the world runs while the game is closed) | `CityWorld.AdvanceWorldTick` cadence |

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
`AssignmentRow`, `SafeAreaMarginContainer`, `OnboardingChoiceButton`,
`GenderToggle`, `CubeAxisBar`, `FounderCardPanel`, `StatChip`,
`PrimaryNavDock`, `ContextInspector`, `ActionDock`, `SimulationControls`,
`CitySummaryPanel`, `ExpeditionRail`, `ExpeditionCompactCard`,
`ConstructionQueueItem`, and the three `ActionButton` roles —
`PrimaryActionButton`, `SecondaryActionButton`, `DangerActionButton`.
The compact HUD adds `HudSectionHeader`, `HudMetricRow`, `HudResourceRow`,
`HudProgressBar`, `HudBadge` and `CollapsiblePanelHeader` (§ 5.2).
Future targets must compose these controls rather than introduce a second
expedition frame grammar.

**`HudIconValue` deliberately does not exist.** The compact HUD wanted an
icon-and-value pair and `StatChip` already was one: same 24 px icon cell,
same height, and the label variation was already a parameter. Only the gap
between icon and text differed, so the gap became a parameter and
`StatChip.HudIconValue(...)` names the role. A second widget that renders
the same thing is exactly what §2.4 and the component showcase exist to
catch, and it is worth noticing that the showcase caught this one.

`ContextInspector` was `SelectionInfoPanel`, which the macro view
constructed at runtime and which repositioned itself in `_Process`
**every frame while visible**. The poll was not arbitrary — a one-shot
placement raced Godot's container minimum-size settling and briefly
computed a wildly-too-tall panel. The fix was to stop computing the
position at all: anchored bottom-left with `grow_vertical = Begin`, the
panel is pinned to the bottom and grows upward as its text wraps. **If a
widget is repositioning itself in `_Process`, the anchors are usually
wrong.**

The persistent `CitySummaryPanel` and transient `ContextInspector` have
separate authored left-edge slots. The summary begins at the 8 px safe margin;
the inspector anchors immediately to its right, stays
bottom-aligned, grows upward, and keeps `MouseFilter.Ignore`. Neither surface
positions itself in `_Process`. `ConstructionQueueItem` is the reusable
icon/name/state/progress composition inside the summary; blocked state is
written as localized text and tooltip, never colour alone.

The right-side `ExpeditionRail` is a persistent summary, not a replacement for
`ExpeditionPanel`: compact cards expose active status and route details back to
the existing planning surface. The rail owns pointer and wheel input, including
at scroll limits, and its vertical focus chain reaches details, valid cancel and
the embedded Chronicle toggle. `ChroniclePanel` has one data/rendering path:
compact mode shows four meaningful rows; expanded mode replaces the expedition
summary inside the same rail and adds the offline summary, grouped actionable
blockers and up to 80 compacted events. `ChronicleEventProjection` remains the
single filtering/compaction rule. Do not recreate a second full-log surface.

`ActionDock` is the bottom-centre contextual tray. It replaced placement
chrome the macro view built inline: a raw `new Button` in a bottom-wide
`HBoxContainer` with **no surface at all**, so the actions floated
directly on the world, plus a separate instruction label anchored to the
top of the screen — two nodes, two visibility flags, and an instruction
as far from its own buttons as the viewport allows. It is not a
permanent toolbar: nothing shows it except a mode with an action to
offer.

`ActionDock`, `PrimaryNavDock`, and `SimulationControls` resolve or build their children
on first access, not only in `_Ready`. Any shared surface a screen
touches from that screen's own `_Ready` must do the same — Godot readies
siblings in tree order, and the consumer frequently comes first.

`PrimaryNavDock` is the semantic successor to the short-lived vertical
`NavigationRail`, which had already replaced the deleted full-width
`MacroActions` strip. The dock is a fixed 300×52 logical surface centred 16 px
above the bottom edge. It owns an icon-only horizontal structure with full
localized tooltips and hands back
typed buttons, so `MacroStreetLiveView` holds one path to the dock instead of literal
paths like `"../MacroActions/Actions/PoliciesButton"`. Deciding what a
button opens stays with the macro view: the dock is chrome, and chrome
does not know what a screen is.

`PrimaryNavDock` and `ActionDock` are mutually exclusive presentations of the
same bottom-centre zone. Normal macro mode shows primary navigation; placement
hides it and shows the contextual instruction/confirm/cancel tray; confirm,
cancel and real `ui_cancel` restore primary navigation. `SimulationControls`
is a separate bottom-right `HudDock` containing the existing `PlayPauseButton`
and `SpeedButton` plus the connected camera-mode world utility. Camera logic
remains in the macro view; only its typed button moved. No duplicate simulation
controls remain in `CityStatusPanel`. `ActionDock` is also a `HudDock`, with
`HudHeader` instruction text and `HudButtonSelected`/`HudButton` actions; it no
longer borrows large-screen `OverlayPanel` or `ButtonText` chrome.

The macro perspective owns the visibility of its authored summary surfaces.
`ActivatePerspective` reveals `CitySummaryPanel`, `ExpeditionRail` and
`SimulationControls`; `Deactivate` hides all three together with primary
navigation and the transient inspector. This keeps onboarding, hero profile and
building detail from inheriting a complete macro HUD while leaving every node
authored under `GameUiShell/ScreenContent`. `ContextInspector` remains
pointer-transparent and upward-growing, but uses `HudCard`/`HudHeader`/
`HudCaption` so the transient cue does not introduce a second frame grammar.

Two things it is worth not relearning:

- **Resolve children on access, not in `_Ready`.** The macro view precedes
  the dock in `CityPrototype.tscn` and Godot readies siblings in tree
  order, so caching the buttons in the dock's `_Ready` returned null and
  crashed the boot. Reordering the scene would have fixed that one caller
  and left the trap set for the next.
- **The dock cannot widen at higher resolutions in this project.**
  `project.godot` uses `stretch/mode=canvas_items` on a 16:9 base of
  1280×720, so 1920×1080 is the *same* logical viewport drawn at 1.5×:
  `GetVisibleRect().Size.X` reads 1280 at both official review sizes.
  There is no extra space to expand into.

`StatChip` landed here rather than as the `StatChip.tscn` §2.1 sketches:
chips are only ever built procedurally, never placed in the editor, so
the editor reuse a PackedScene buys does not apply, and §2.4 routes a
widget with its own construction logic to `[GlobalClass]`. It began as a
`private partial class IconChip` at the bottom of `CityStatusPanel.cs`
— a second public type in a file named after another one, and a shared
widget no other surface could reach.

**Name the role, not the look.** A view picks `PrimaryActionButton` (the one
action the screen is for), `SecondaryActionButton` (everything else
affirmative — the default), or `DangerActionButton` (irreversible). The
standard supplies the variation, the 40 px height and the focus policy;
`ActionButton.DefaultHeight` is shared with `OnboardingChoiceButton` so a row
of actions lines up with a column of choices. This is what makes a re-skin one
edit to `default_theme.tres` instead of an audit of every call site — the
lesson of the 2026-08-06 slate pass, where the surface changed across the whole
game without any view being reconfigured.

`OnboardingChoiceButton` is the selectable narrative option: it carries
the selected state on three channels (the `ButtonPrimary` palette, the
pressed state of a shared `ButtonGroup`, and a check glyph whose slot is
always reserved so the label never jogs), which is how it satisfies the
"never communicate a state by colour alone" invariant. `GenderToggle`
pairs two of them at a fixed width. `CubeAxisBar` renders one
Cuerpo/Vínculo-style pair as `NOMBRE 56 [====|===] 44 NOMBRE`; it is not
a `ProgressBar`, because the theme registers `ProgressBar` on the
built-in type and its green fill carries the success semantic, which
misreads a neutral two-pole distribution. `FounderCardPanel` composes
three of those into the closing card of the founder onboarding.

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
| Spacing and control sizes | Name them in `Ui/Tokens.cs`; do not re-decide a number at the callsite. | `Tokens.SpacingBase`, `Tokens.ChipHeight`, `Tokens.ControlHeight`. |

**Spacing has a scale now.** Typography is fully centralised — every
Label and Button names a variation, and there are zero
`AddThemeFontOverride` / `AddThemeFontSizeOverride` calls in the
repository. Spacing was the opposite: 84 `AddThemeConstantOverride`
calls across 24 files, nearly all `separation` or `margin_*`, each
choosing its own number. `Ui/Tokens.cs` is the missing half. It holds
the values already in the code, named — renumbering spacing moves layout
metrics and needs a visual pass, whereas naming it does not. Add a token
when a constant gains a second consumer, not in advance.

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
explicitly — never rely on the engine's default font. The base `Label`
type in `default_theme.tres` carries Pixelify defaults so an
unannotated label still looks like the project.

**There is no base `Button` registration** (verified 2026-08-07; this
paragraph previously claimed there was). An unannotated `Button`
therefore falls back to the engine's own font and grey styleboxes, and
the rule above is the only thing preventing it. Registering the base
type would be a real safety net, but it also gives every unannotated
button new content margins — a size change — so it belongs with a pass
that can re-check the affected surfaces, not with a chrome swap.

### 5.0 The compact HUD profile

The HUD runs a **second, isolated type scale**. It is not a rescaling of the
seventeen variations above and must never be merged with them: the screens are
read while stopped, the HUD while playing, and the reference the HUD is built
against (`art/references/Proposal 06 — minimalist workstation.png`) puts roughly
twice as many rows on screen as the current screen scale allows.

| Variation | Family | Size | Sized by |
| --- | --- | ---: | --- |
| `HudBrand` | Geist Pixel | 20 | the brand plate |
| `HudHeader` | Jersey 10 | 18 | the 20 px header strip |
| `HudLabel` | Jersey 10 | 16 | section and metric labels |
| `HudBody` | Pixelify Sans | 16 | the 24 px row |
| `HudNumeric` | Pixelify Sans | 16 | figures, right-aligned |
| `HudCaption` | Pixelify Sans | 14 | log lines and deltas |

**14 px is the floor, and it is signed.** It was read in a real 1280×720
capture of `HudComponentShowcase.tscn` before approval — solid pixels, no
grayscale fringe — which is the only evidence that counts for a size below the
project's previous 16 px minimum. `HudThemeVariationTests` enforces the floor;
going under it needs its own capture and its own sign-off, not an edit.

Changing a `Hud*` size does not touch a screen, and changing a screen size does
not touch the HUD. `ScreenVariations_AreUnchangedByTheHudProfile` exists because
that separation fails silently otherwise: reaching for "the body size" and
editing `BodyText` instead of `HudBody` restyles every modal in the game with
nothing to notice it.

### 5.1 Surface variations

Alongside the text variations the theme registers the surfaces
`OverlayPanel` (elevated/modal), `PanelCard` (card inside a panel) and
`StatusStrip` (HUD bars), plus the base `Panel`, `LineEdit`,
`ProgressBar` and `ScrollContainer` types.

`OverlayPanel`, `Panel` and `PanelCard` are backed by composited
9-slice textures under `game/assets/ui/composites/` — a Kenney frame
with the project's fill and border ramp baked in, generated by
`tools/New-CompositeStylebox.ps1`. `StatusStrip` and `ScrollContainer`
are still `StyleBoxFlat`, deliberately: the status strip carries only a
bottom border, and a four-sided frame would draw borders down the
screen edges. See `ASSET_INVENTORY.md` for why a raw pack tile cannot
serve as a panel here.

**Lineage identity arrives through the theme, not around it.**
`Ui/LineageThemePainter` writes the active lineage's panel surface into
`PanelContainer`, `Panel` and `PanelCard` on the project theme whenever
the lineage changes, so a panel needs no override to carry lineage
identity and cannot go stale when the lineage changes mid-session.

The reason so many panels used to override is duller than it looks: the
theme registered `Panel` — the `Panel` *control* — but never
`PanelContainer`, which is what these surfaces actually are. A bare
`PanelContainer` fell through to the engine's grey stylebox, so
overriding was the only way to look like the project at all. It is
registered now.

Two things the painter must keep doing, both learned by measuring:

- **Only the eight real lineages get a lineage frame.** Asking the
  registry for anything else returns its *fallback*,
  `slate_raised_dark` — the raised button texture, a last resort so a
  surface is never unstyled. It is not a card surface, and painting it
  over `PanelCard` replaced the authored composite with mid-tone slate.
- **Content margins are normalised, not inherited.** The lineage assets
  carry 8/7 and the neutral card 14/12. Lineage themes may change
  palette, borders, corners and fills; the invariants forbid them
  changing minimum sizes, and padding is layout.

Some overrides remain — `ConstructionPanel` (an `OverlayPanel`, so
dropping its override would change how modals read), and the inline
non-lineage ones. Before concluding a surface is themed, check whether
its script overrides it.

### 5.2 The compact HUD's chrome — one frame, many fills

The HUD registers twelve more variations: the surfaces `HudSurface`, `HudInset`,
`HudHeaderSurface`, `HudCard`, `HudDock` and `HudBadge`; the buttons `HudButton`,
`HudButtonSelected`, `HudButtonDanger` and `HudCollapsibleHeader`; plus
`HudProgress` and `HudSeparator`.

**Every one of them draws the same authored frame at a different fill.** That is
not a shortcut, it is what the reference does: its whole hierarchy is one border
weight and three fills sitting within twelve luminance steps of each other.
Measured from the reference, converted to the 1280×720 logical viewport:

| Tier | Fill | Lum | Border |
| --- | --- | ---: | --- |
| `HudInset` / `HudHeaderSurface` | `#070A11` | 8 | `#1A1B22` |
| `HudSurface` / `HudDock` | `#090C13` | 12 | `#36373F` |
| `HudCard` / `HudButton` | `#13141D` | 20 | `#31323C` |

Three things here were established by measurement and should not be re-litigated
from taste:

- **The border is one pixel.** The reference draws 1 px and nothing thicker. The
  Kenney Adventure pack's rectangular frames are 3–6 px; its *only* 1 px artefact
  is `Small tiles/Thin outline/tile_0069`, a 10×10 rounded outline with a 1 px
  stroke, and every `hud_*` composite is baked from it. The showcase first drew
  1 px, 3 px and 4 px side by side and the heavier two lost plainly at 1920×1080 —
  `tile_0019`'s corner studs read as artefacts and `tile_0018` doubled its edge.
  Neither was promoted.
- **A header strip is recessed, not raised.** `HudHeaderSurface` fills *darker*
  than the panel containing it (lum 8 against 12). A raised header belongs to a
  heavier language than this one.
- **Borders went cool grey; text did not.** The HUD's chrome is neutral
  (`#36373F`, `#31323C`, `#1A1B22`) and amber (`#D5903E`) is spent only on
  progress fills, the selected state, the danger action and `HudBadge` — the one
  place the accent covers a surface rather than a line, which is why a single
  amber pill is unmissable. HUD *text* stays on the project's warm cream, because
  the reference's is warm too (`#EEE4DA` for a value). The existing screen
  surfaces keep their tan and gold and were not touched.

**HUD surfaces stay out of `LineageThemePainter.RepaintedTypes`, deliberately.**
The lineage assets are ornate frames 6–8 px thick at card weight, and the painter
normalises content margins to the card's 14/12 — roughly twice what a 24 px HUD
row holds. Painting a lineage over `HudSurface` would therefore change *minimum
sizes*, which the cross-domain invariants forbid a lineage theme from doing.
Lineage identity reaches the HUD through `LineageThemeRegistry.IconAccent`
instead: chrome stays neutral, the accent carries the lineage, and every HUD
primitive tints its glyphs with it. Reskinning HUD chrome per lineage would need
a one-pixel asset per lineage, not three more strings in that array.

Two exceptions to "no StyleBoxFlat for visible HUD chrome", both named in
`HudThemeVariationTests` so a third has to be a visible edit: `HudSeparator` is a
`StyleBoxLine`, because a rounded 10×10 outline cannot draw a straight 1 px rule;
and `HudProgress`'s **fill** is flat, because it is a solid colour bar with no
border to author and a 9-slice stretched across a 6 px interior would repeat its
corner along the length. The progress fill is amber rather than the existing
green because green carries the success semantic here — the same reason
`CubeAxisBar` refuses to be a `ProgressBar`.

`PanelAction`, `PanelIdle` and `PanelWarning` were removed on
2026-08-07: all three resolved to the *same* stylebox as `PanelCard`
and no scene or script referenced any of them, so they promised a
distinction the theme did not deliver — a `PanelWarning` panel looked
exactly like a neutral card. If a warning surface is needed, add it
back with a genuinely distinct style and a use case.

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
| Adding a new theme variation per call site. | 17 text variations plus 3 surface variations already (§5, §5.1); wildcards defeat centralised theming. | Reuse an existing variation or add the variation to `default_theme.tres` with a typed justification. |
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
  the embedded `ChroniclePanel` already implements the structure — extend it
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
