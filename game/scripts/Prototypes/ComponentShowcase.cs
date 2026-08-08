#nullable enable

using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Renders every shared UI primitive on one screen, so the component system can be
/// reviewed without hunting for a game state that happens to show a given widget.
/// </summary>
/// <remarks>
/// <para>
/// This exists because verification was the weak point. Several primitives are only
/// reachable from narrow game states — <c>AssignmentPanel</c> and
/// <c>ProductionPanel</c> hide themselves for homes and the town hall
/// (<c>Visible = !isHome &amp;&amp; !isTownHall</c>), so no visual-regression fixture
/// renders them at all, and a change to their surface could not be seen. A showcase
/// makes every component reviewable at both official sizes on demand.
/// </para>
/// <para>
/// The last column composes an expedition member row out of nothing but city
/// primitives. That is the reuse proof in miniature: if an expedition screen needs
/// bespoke widgets, this column is where that shows up first.
/// </para>
/// <para>
/// A developer surface, like <c>TypographySpecimen</c>. Its strings are literals
/// rather than <c>UiText</c> keys because nothing here is player-facing, and adding
/// two dozen catalogue entries for a review scene would be noise in both `.po`
/// files.
/// </para>
/// </remarks>
public partial class ComponentShowcase : Control
{
    public override void _Ready()
    {
        var safeArea = new SafeAreaMarginContainer();
        safeArea.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(safeArea);

        var columns = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", Tokens.SpacingLoose);
        safeArea.AddChild(columns);

        columns.AddChild(BuildSurfacesColumn());
        columns.AddChild(BuildActionsColumn());
        columns.AddChild(BuildDataColumn());
        columns.AddChild(BuildCompositionColumn());
    }

    private static VBoxContainer NewColumn(string heading)
    {
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        column.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        column.AddChild(new Label { Text = heading, ThemeTypeVariation = "PanelTitle" });
        return column;
    }

    private static PanelContainer NewCard(string variation, Control content)
    {
        var card = new PanelContainer { ThemeTypeVariation = variation };
        card.AddChild(content);
        return card;
    }

    /// <summary>Every panel surface the theme registers, side by side.</summary>
    private static Control BuildSurfacesColumn()
    {
        var column = NewColumn("Surfaces");

        foreach (string variation in new[] { "PanelCard", "OverlayPanel", "StatusStrip" })
        {
            var body = new VBoxContainer();
            body.AddChild(new Label { Text = variation, ThemeTypeVariation = "SectionTitle" });
            body.AddChild(new Label
            {
                Text = "Body text on this surface.",
                ThemeTypeVariation = "BodySmall",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            });
            column.AddChild(NewCard(variation, body));
        }

        // A bare PanelContainer with no variation: proves the theme registers the
        // type, rather than leaving it on the engine's grey default.
        var bare = new VBoxContainer();
        bare.AddChild(new Label { Text = "PanelContainer (no variation)", ThemeTypeVariation = "BodySmall" });
        column.AddChild(NewCard(string.Empty, bare));

        return column;
    }

    /// <summary>The three action roles plus their states.</summary>
    private static Control BuildActionsColumn()
    {
        var column = NewColumn("Actions");

        var primary = new PrimaryActionButton { Text = "Primary" };
        var secondary = new SecondaryActionButton { Text = "Secondary" };
        var danger = new DangerActionButton { Text = "Danger" };
        var disabled = new SecondaryActionButton { Text = "Disabled", Disabled = true };

        foreach (var button in new Button[] { primary, secondary, danger, disabled })
        {
            column.AddChild(button);
        }

        var icon = new IconButton { ThemeTypeVariation = "ButtonText" };
        icon.SetIconAndLabel(IconPaths.Plus, "IconButton");
        column.AddChild(icon);

        var iconOnly = new IconButton { ThemeTypeVariation = "ButtonText" };
        iconOnly.SetIconAndLabel(IconPaths.Cog, "Collapsed");
        iconOnly.ShowLabel = false;
        iconOnly.TooltipText = "Icon-only, as the navigation rail draws it";
        column.AddChild(iconOnly);

        // Focus is a real state and the only one a screenshot cannot provoke by
        // hovering, so grab it deliberately.
        primary.CallDeferred(Control.MethodName.GrabFocus);

        return column;
    }

    /// <summary>Chips and rows — the components that carry values.</summary>
    private static Control BuildDataColumn()
    {
        var column = NewColumn("Data");

        column.AddChild(new StatChip(IconPaths.Clock, "Day 12 - 07:20", "BuildingName"));
        column.AddChild(new StatChip(IconPaths.User, "3 free citizens"));
        column.AddChild(new StatChip(IconPaths.Warning, "Storage full"));

        var rowHost = new VBoxContainer();
        rowHost.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        rowHost.AddChild(NewAssignmentRow(1, "Aster", "Assign", "Assign to the farm"));
        rowHost.AddChild(NewAssignmentRow(2, "Bryn", "Remove", "Remove from the farm"));
        rowHost.AddChild(NewAssignmentRow(3, "Corin", "Assign", "No capacity left", disabled: true));
        column.AddChild(NewCard("PanelCard", rowHost));

        var bar = new ProgressBar { MinValue = 0, MaxValue = 1, Value = 0.62, CustomMinimumSize = new Vector2(0, 16) };
        column.AddChild(bar);

        return column;
    }

    private static AssignmentRow NewAssignmentRow(
        int id, string name, string action, string tooltip, bool disabled = false)
    {
        var row = GD.Load<PackedScene>("res://scenes/Components/AssignmentRow.tscn")
            .Instantiate<AssignmentRow>();
        row.Configure(id, name, action, tooltip, disabled);
        return row;
    }

    /// <summary>
    /// An expedition member card assembled only from city primitives — the reuse
    /// claim, made checkable.
    /// </summary>
    private static Control BuildCompositionColumn()
    {
        var column = NewColumn("Expedition reuse");

        var member = new VBoxContainer();
        member.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        member.AddChild(new Label { Text = "Aster of Vaelun", ThemeTypeVariation = "SectionTitle" });
        member.AddChild(new StatChip(IconPaths.Heart, "Stamina 8/10"));
        member.AddChild(new StatChip(IconPaths.Shield, "Lightly wounded"));
        member.AddChild(new StatChip(IconPaths.Leaf, "Forager"));

        var posture = new HBoxContainer();
        posture.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        // State carried by a glyph, not by colour alone -- the signed decision the
        // expedition panel already ships.
        posture.AddChild(new SecondaryActionButton { Text = "[X] Return" });
        posture.AddChild(new SecondaryActionButton { Text = "[ ] Press on" });
        member.AddChild(posture);

        column.AddChild(NewCard("PanelCard", member));
        column.AddChild(new Label
        {
            Text = "Built from StatChip, ActionButton and PanelCard only. "
                 + "Anything an expedition needs that is missing here is a gap in the shared set.",
            ThemeTypeVariation = "BodySmall",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        return column;
    }
}
