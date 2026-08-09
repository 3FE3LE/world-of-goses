#nullable enable

using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// The contextual surface: shows what the player last selected in the world, and
/// nothing at all when there is no selection.
/// </summary>
/// <remarks>
/// <para>
/// Generic on purpose. <see cref="ShowSelection"/> takes an icon/title/detail
/// triplet, so trees, buildings and citizens share one surface instead of each
/// growing a bespoke corner widget — and an expedition screen can later point a
/// route node or a bestiary entry at the same component.
/// </para>
/// <para>
/// Was <c>SelectionInfoPanel</c>, which the macro view constructed at runtime and
/// which repositioned itself in <c>_Process</c> **every frame while visible**. The
/// poll was not arbitrary: a one-shot placement raced Godot's container
/// minimum-size settling and briefly computed a wildly-too-tall panel. The real
/// fix is to stop computing the position at all. Anchored bottom-left with
/// <c>grow_vertical = Begin</c>, the panel is pinned to the bottom and grows upward
/// as its text wraps, which is what anchors are for. No frame callback, no race.
/// </para>
/// </remarks>
[GlobalClass]
public partial class ContextInspector : PanelContainer
{
    private const int IconBoxSize = 40;

    /// <summary>Width of the surface. The scene's offsets must agree with this.</summary>
    public const int PanelWidth = 220;

    private TextureRect _icon = null!;
    private Label _title = null!;
    private Label _detail = null!;

    public override void _Ready()
    {
        // Never blocks world clicks: this panel only reports what was selected
        // elsewhere, and nothing on it is interactive.
        MouseFilter = MouseFilterEnum.Ignore;
        ThemeTypeVariation = "HudCard";
        OverlayLayers.Apply(this, OverlayLayers.SelectionInfo);
        CustomMinimumSize = new Vector2(PanelWidth, 0);
        Hide();

        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        AddChild(row);

        var iconCell = new CenterContainer
        {
            CustomMinimumSize = new Vector2(IconBoxSize, IconBoxSize),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddChild(iconCell);
        _icon = new TextureRect
        {
            CustomMinimumSize = new Vector2(IconBoxSize, IconBoxSize),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        iconCell.AddChild(_icon);

        var text = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        text.AddThemeConstantOverride("separation", 2);
        row.AddChild(text);

        _title = new Label
        {
            ThemeTypeVariation = "HudHeader",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        text.AddChild(_title);

        _detail = new Label
        {
            ThemeTypeVariation = "HudCaption",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        text.AddChild(_detail);
    }

    /// <summary>Populates and reveals the surface for a newly selected entity.</summary>
    public void ShowSelection(Texture2D? icon, string title, string detail)
    {
        _icon.Texture = icon;
        _title.Text = title;
        _detail.Text = detail;
        Show();
    }
}
