#nullable enable
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Persistent bottom-left HUD panel showing details about whatever the
/// player last left-clicked in the world. Left-click selects (shows info
/// here); right-click is reserved for the icon-only action button(s) —
/// see <see cref="Prototypes.MacroStreetLiveView"/>. Generic on purpose:
/// <see cref="ShowSelection"/> takes an icon/title/detail triplet so
/// trees today, and buildings/citizens later, share one HUD surface
/// instead of each getting a bespoke corner widget.
/// </summary>
public partial class SelectionInfoPanel : PanelContainer
{
    private const int IconBoxSize = 40;
    private const int PanelWidth = 220;

    private TextureRect _icon = null!;
    private Label _title = null!;
    private Label _detail = null!;

    public override void _Ready()
    {
        // Never blocks world clicks — this panel only ever displays what
        // was already selected elsewhere, nothing on it is interactive yet.
        MouseFilter = MouseFilterEnum.Ignore;
        OverlayLayers.Apply(this, OverlayLayers.SelectionInfo);
        ThemeTypeVariation = "OverlayPanel";
        CustomMinimumSize = new Vector2(PanelWidth, 0);
        Hide();

        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 10);
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

        _title = new Label { ThemeTypeVariation = "SectionTitle", MouseFilter = MouseFilterEnum.Ignore };
        text.AddChild(_title);

        _detail = new Label
        {
            ThemeTypeVariation = "BodySmall",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        text.AddChild(_detail);
    }

    /// <summary>Populates and shows the panel for a newly selected entity.</summary>
    public void ShowSelection(Texture2D? icon, string title, string detail)
    {
        _icon.Texture = icon;
        _title.Text = title;
        _detail.Text = detail;
        Show();
        AnchorBottomLeft();
    }

    /// <summary>
    /// Re-applied every frame while visible (see <see cref="_Process"/>) —
    /// a single one-shot placement (even deferred) raced Godot's own
    /// container minimum-size settling on the very first show, briefly
    /// computing a wildly-too-tall Size before self-correcting; continuous
    /// reapplication is cheap and immune to that race regardless of which
    /// frame the layout actually settles on.
    /// </summary>
    private void AnchorBottomLeft()
    {
        if (GetParent() is not Control parent) return;
        ResetSize();
        Position = new Vector2(16, parent.Size.Y - Size.Y - 16);
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        AnchorBottomLeft();
    }
}
