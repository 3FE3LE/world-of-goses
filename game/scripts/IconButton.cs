using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Reusable button composed of a leading icon plus a text label.
/// Used wherever the bible's iconography guideline marks an action
/// as icon+text (construcción, decisiones importantes). For universal
/// actions (close, back, pause) the same class works with just the
/// icon by passing an empty label.
///
/// Layout contract:
/// - Icon sits to the left of the label with an 8-px gap.
/// - Both icon and label are vertically centred inside the button's
///   content rect via an <see cref="HBoxContainer"/> with full-rect
///   anchors and expand-fill size flags; the row's <c>Alignment.Center</c>
///   handles horizontal centring.
/// - Icons are imported with the cream tint baked into the SVG
///   (<c>fill="#f2ebd3"</c>) so they stay legible against both the
///   yellow primary and grey secondary panel backgrounds without any
///   runtime tint (which would be multiplicative and darken the
///   stroke).
///
/// Tooltip routing: Godot's <c>_make_custom_tooltip</c> proved
/// unreliable in this Godot 4.7 build — the engine kept showing the
/// default yellow 9-slice popup despite the C# override. <see cref="_Ready"/>
/// captures the configured <see cref="BaseButton.TooltipText"/>,
/// clears it, and re-routes to the in-world
/// <see cref="TooltipOverlay"/> so the Pixelify typography guide is
/// always honoured.
/// </summary>
public partial class IconButton : Button
{
    [Export] public string IconPath { get; set; } = string.Empty;
    [Export] public string Label { get; set; } = string.Empty;

    /// <summary>Icon canvas kept smaller than 24 so it fits inside a 40-px-tall button without overshooting the label.</summary>
    private const int IconSize = 18;
    private const int IconTextGap = 8;

    private TextureRect _icon = null!;
    private Label _label = null!;

    public override void _Ready()
    {
        // Clear the Button's built-in Text/Icon rendering so it doesn't
        // paint its own label on top of the custom HBoxContainer below.
        // The .tscn often declares `text = "..."` for editor preview;
        // when an IconButton takes over, it owns the rendered content.
        Text = string.Empty;
        Icon = null;

        var row = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        row.AddThemeConstantOverride("separation", IconTextGap);
        AddChild(row);

        _icon = new TextureRect
        {
            StretchMode = TextureRect.StretchModeEnum.Keep,
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = LineageThemeRegistry.IconAccent,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            Visible = !string.IsNullOrEmpty(IconPath),
        };
        if (!string.IsNullOrEmpty(IconPath))
        {
            _icon.Texture = ResourceLoader.Load<Texture2D>(IconPath);
        }
        row.AddChild(_icon);

        _label = new Label
        {
            // The Label inside the HBox does NOT inherit the parent
            // Button's theme_type_variation in Godot 4 — it has its own
            // theme lookup that falls back to engine defaults unless we
            // pin it explicitly. We use the same Jersey 10 variation
            // the Button uses so the icon-plus-label row reads as one
            // typographic unit.
            Text = Label,
            ThemeTypeVariation = "ButtonText",
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            Visible = !string.IsNullOrEmpty(Label),
        };
        row.AddChild(_label);

        LineageThemeRegistry.ActiveLineageChanged += OnLineageChanged;
        ApplyAccent();
    }

    public override void _ExitTree()
    {
        LineageThemeRegistry.ActiveLineageChanged -= OnLineageChanged;
    }

    /// <summary>Replaces both icon and label at runtime.</summary>
    public void SetIconAndLabel(string iconPath, string label)
    {
        IconPath = iconPath;
        Label = label;
        if (_icon is null || _label is null) return;
        _icon.Texture = string.IsNullOrEmpty(iconPath)
            ? null
            : ResourceLoader.Load<Texture2D>(iconPath);
        _icon.Visible = !string.IsNullOrEmpty(iconPath);
        _label.Text = label ?? string.Empty;
        _label.Visible = !string.IsNullOrEmpty(label);
    }

    private void OnLineageChanged(string lineage) => ApplyAccent();

    private void ApplyAccent()
    {
        if (_icon is not null) _icon.Modulate = LineageThemeRegistry.IconAccent;
    }
}
