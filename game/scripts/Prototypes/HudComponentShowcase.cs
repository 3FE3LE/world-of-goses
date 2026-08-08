#nullable enable

using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Renders every compact-HUD primitive, and every state each one can take, on a
/// single screen at the two official review sizes.
/// </summary>
/// <remarks>
/// <para>
/// Separate from <c>ComponentShowcase</c> rather than folded into it. That scene
/// reviews the <em>screen</em> primitives — 18 px body text, 40 px actions, 6 px
/// panel frames — and this one reviews a deliberately different scale. Putting both
/// on one surface would leave neither enough width, and would invite the reader to
/// compare two systems that are not meant to agree.
/// </para>
/// <para>
/// <b>Three of the six states here are specimens, not live controls.</b> A static
/// capture can hold exactly one focused control, and hover needs a pointer the
/// harness is not holding at capture time. So <c>default</c>, <c>disabled</c>,
/// <c>selected</c> and one <c>focus</c> are real controls in their real state, and
/// <c>hover</c> and <c>pressed</c> are drawn by applying that state's stylebox as a
/// control's normal style. A specimen proves the stylebox renders; it does not prove
/// the control reaches that state. Anything depending on the transition still needs a
/// real pointer, which is what <c>VISUAL_REGRESSION.md</c>'s click-driven rows are for.
/// </para>
/// <para>
/// The first column's tier block is load-bearing. The reference builds its whole
/// hierarchy out of one border weight and three fills that sit within twelve
/// luminance steps of each other, so the thing most likely to break here is not a
/// component but the <em>separation between surfaces</em> — and it breaks invisibly,
/// by a fill drifting two steps until a card stops lifting off its panel. Both
/// readings, nested and unnested, are on screen for that reason.
/// </para>
/// <para>
/// This scene also settled the border question. It first drew three weights side by
/// side — 1 px from the composited <c>tile_0069</c> outline, 3 px from
/// <c>tile_0019</c>, 4 px from <c>tile_0018</c> — and at 1920x1080 the two heavier
/// frames lost plainly: <c>tile_0019</c>'s corner studs read as artefacts and
/// <c>tile_0018</c> doubled its own edge. Every HUD surface now shares the one-pixel
/// frame, and the losing tiles were never promoted.
/// </para>
/// <para>
/// A developer surface, like <c>TypographySpecimen</c>. Its strings are literals
/// rather than <c>UiText</c> keys because nothing here is player-facing, and adding
/// two dozen catalogue entries for a review scene would be noise in both `.po` files.
/// </para>
/// </remarks>
public partial class HudComponentShowcase : Control
{
    public override void _Ready()
    {
        var safeArea = new SafeAreaMarginContainer();
        safeArea.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(safeArea);

        var columns = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", Tokens.SpacingComfortable);
        safeArea.AddChild(columns);

        columns.AddChild(BuildSurfacesColumn());
        columns.AddChild(BuildControlsColumn());
        columns.AddChild(BuildDataColumn());
    }

    private static VBoxContainer NewColumn(string heading)
    {
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        column.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        column.AddChild(new Label { Text = heading, ThemeTypeVariation = "HudBrand" });
        return column;
    }

    private static Label Caption(string text) => new()
    {
        Text = text,
        ThemeTypeVariation = "HudCaption",
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };

    private static PanelContainer Surface(string variation, Control content)
    {
        var surface = new PanelContainer { ThemeTypeVariation = variation };
        surface.AddChild(content);
        return surface;
    }

    private static VBoxContainer Stack(int separation = Tokens.SpacingTight)
    {
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", separation);
        return stack;
    }

    /// <summary>A one-line body of text, ready to drop into a surface.</summary>
    private static VBoxContainer Line(string text, string variation = "HudBody")
    {
        var stack = Stack();
        stack.AddChild(new Label { Text = text, ThemeTypeVariation = variation });
        return stack;
    }

    /// <summary>
    /// The three nested surface tiers, and the border-weight comparison the hybrid
    /// chrome decision rests on.
    /// </summary>
    private static Control BuildSurfacesColumn()
    {
        var column = NewColumn("Surfaces");

        // Tiers, actually nested: the reference separates them by only ~4 and ~9
        // luminance steps, so they have to be seen inside one another to be judged.
        var body = Stack(Tokens.SpacingBase);
        body.AddChild(new HudSectionHeader("ESTADO", "3"));
        body.AddChild(Surface("HudInset", Line("HudInset — recessed")));
        body.AddChild(Surface("HudCard", Line("HudCard — raised")));
        body.AddChild(Surface("HudDock", Line("HudDock")));
        column.AddChild(Surface("HudSurface", body));

        // The tiers again, this time side by side rather than nested. Separated by
        // only 4 and 8 luminance steps, they have to survive both readings: a card
        // must lift off the panel it sits in *and* be tellable from a panel it does
        // not touch.
        column.AddChild(new HudSectionHeader("TIERS, UNNESTED"));
        foreach ((string variation, string label) in new[]
                 {
                     ("HudInset", "HudInset — fill lum 8"),
                     ("HudSurface", "HudSurface — fill lum 12"),
                     ("HudCard", "HudCard — fill lum 20"),
                 })
        {
            column.AddChild(Surface(variation, Line(label, "HudCaption")));
        }

        column.AddChild(Caption(
            "Every frame here is one pixel, from the same Kenney tile_0069 outline. "
            + "The tiers differ by fill alone, which is what the reference does."));

        return column;
    }

    /// <summary>Every interactive primitive, in every state it can take.</summary>
    private static Control BuildControlsColumn()
    {
        var column = NewColumn("Controls");

        column.AddChild(new HudSectionHeader("HUDBUTTON", "6 states"));

        var normal = new Button { Text = "Default", ThemeTypeVariation = "HudButton" };
        normal.CustomMinimumSize = new Vector2(0, Tokens.HudControlHeight);
        var focused = new Button { Text = "Focus", ThemeTypeVariation = "HudButton" };
        focused.CustomMinimumSize = new Vector2(0, Tokens.HudControlHeight);
        focused.FocusMode = FocusModeEnum.All;
        var disabled = new Button
        {
            Text = "Disabled",
            ThemeTypeVariation = "HudButton",
            Disabled = true,
            CustomMinimumSize = new Vector2(0, Tokens.HudControlHeight),
        };

        column.AddChild(normal);
        column.AddChild(focused);
        column.AddChild(Specimen("Hover (specimen)", "HudButton", "hud_button_hover"));
        column.AddChild(Specimen("Pressed (specimen)", "HudButton", "hud_button_pressed"));
        column.AddChild(disabled);

        var selected = new Button
        {
            Text = "Selected",
            ThemeTypeVariation = "HudButtonSelected",
            CustomMinimumSize = new Vector2(0, Tokens.HudControlHeight),
        };
        column.AddChild(selected);

        var danger = new Button
        {
            Text = "[!] Cancel — warning",
            ThemeTypeVariation = "HudButtonDanger",
            CustomMinimumSize = new Vector2(0, Tokens.HudControlHeight),
        };
        column.AddChild(danger);

        column.AddChild(new HudSectionHeader("COLLAPSIBLE"));
        column.AddChild(new CollapsiblePanelHeader("CIUDAD", expanded: true));
        column.AddChild(new CollapsiblePanelHeader("REGISTRO", expanded: false));
        column.AddChild(Caption(
            "Expanded and collapsed differ by chevron, not by tint alone."));

        column.AddChild(new HudSectionHeader("BADGE"));
        var badgeRow = new HBoxContainer();
        badgeRow.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        badgeRow.AddChild(new HudBadge("12"));
        badgeRow.AddChild(new HudBadge("3"));
        badgeRow.AddChild(new Label
        {
            Text = "the only amber surface",
            ThemeTypeVariation = "HudCaption",
            VerticalAlignment = VerticalAlignment.Center,
        });
        column.AddChild(badgeRow);

        // The sixth state. Unlike the other five it is not a stylebox a control
        // switches into — every primitive expresses it differently, and each one has
        // to prove it does so without relying on colour.
        column.AddChild(new HudSectionHeader("WARNING", "[!]"));
        var warned = Stack();
        warned.AddChild(new HudMetricRow("Almacen", "lleno", IconPaths.Warning));
        warned.AddChild(new HudResourceRow(IconPaths.Leaf, "Alimentos", "12", "-9"));
        warned.AddChild(new HudProgressBar(0.94, showPercent: true));
        column.AddChild(Surface("HudSurface", warned));
        column.AddChild(Caption(
            "Warning is a glyph, a sign and a number — never a tint. Desaturate this "
            + "block and every one of the three still reads."));

        // Focus is a real state and the only one a screenshot cannot provoke by
        // hovering, so grab it deliberately.
        focused.CallDeferred(Control.MethodName.GrabFocus);

        return column;
    }

    /// <summary>
    /// Draws one state's stylebox as a control's normal style. Honest about what it
    /// is: the label says "specimen" so a reader never mistakes it for a live state.
    /// </summary>
    private static Button Specimen(string label, string variation, string styleboxName)
    {
        var button = new Button
        {
            Text = label,
            ThemeTypeVariation = variation,
            CustomMinimumSize = new Vector2(0, Tokens.HudControlHeight),
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        var stylebox = ResourceLoader.Load<StyleBox>(
            $"res://assets/ui/composites/{styleboxName}.tres");
        if (stylebox is not null) button.AddThemeStyleboxOverride("normal", stylebox);
        return button;
    }

    /// <summary>The rows that carry values, plus the compact type scale itself.</summary>
    private static Control BuildDataColumn()
    {
        var column = NewColumn("Data");

        var city = Stack();
        city.AddChild(new HudSectionHeader("ESTADO"));
        city.AddChild(new HudMetricRow("Felicidad", "78%", IconPaths.Heart));
        city.AddChild(new HudMetricRow("Orden", "92%", IconPaths.Shield));
        city.AddChild(new HudMetricRow("Almacen", "lleno", IconPaths.Warning));
        city.AddChild(new HSeparator { ThemeTypeVariation = "HudSeparator" });
        city.AddChild(new HudMetricRow("Progreso", "42%"));
        city.AddChild(new HudProgressBar(0.42, showPercent: false));
        column.AddChild(Surface("HudSurface", city));

        var stock = Stack();
        stock.AddChild(new HudSectionHeader("RECURSOS"));
        stock.AddChild(new HudResourceRow(IconPaths.Leaf, "Alimentos", "1320", "+28"));
        stock.AddChild(new HudResourceRow(IconPaths.Tree, "Madera", "860", "+16"));
        stock.AddChild(new HudResourceRow(IconPaths.Coin, "Metal", "210", "-6"));
        column.AddChild(Surface("HudSurface", stock));

        var expedition = Stack();
        expedition.AddChild(new Label { Text = "Bosque Silente", ThemeTypeVariation = "HudHeader" });
        var facts = new HBoxContainer();
        facts.AddThemeConstantOverride("separation", Tokens.SpacingComfortable);
        facts.AddChild(StatChip.HudIconValue(IconPaths.Clock, "1d 12h"));
        facts.AddChild(StatChip.HudIconValue(IconPaths.Users, "4"));
        expedition.AddChild(facts);
        expedition.AddChild(new HudProgressBar(0.71, showPercent: true, tall: true));
        column.AddChild(Surface("HudCard", expedition));

        column.AddChild(new HudSectionHeader("TYPE SCALE"));
        var type = Stack();
        foreach ((string variation, string sample) in new[]
                 {
                     ("HudBrand", "HudBrand 20 — Geist Pixel"),
                     ("HudHeader", "HudHeader 18 — Jersey 10"),
                     ("HudLabel", "HudLabel 16 — Jersey 10"),
                     ("HudBody", "HudBody 16 — Pixelify Sans"),
                     ("HudNumeric", "HudNumeric 16 — 1320 · 42% · 8/10"),
                     ("HudCaption", "HudCaption 14 — Dia 17 · Aserradero completado"),
                 })
        {
            type.AddChild(new Label { Text = sample, ThemeTypeVariation = variation });
        }
        column.AddChild(Surface("HudInset", type));
        column.AddChild(Caption("14 px is unapproved until it is read here at 1280x720."));

        return column;
    }
}
