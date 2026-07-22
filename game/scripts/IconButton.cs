using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Reusable button composed of a leading icon plus visible text.
/// Used wherever the bible's iconography guideline marks an action
/// as icon+text (construcción, decisiones importantes). For universal
/// actions (close, back, pause) the same class works with just the
/// icon by passing an empty label.
///
/// Layout contract:
/// - Godot's native <see cref="Button"/> renderer owns both icon and text.
/// - Icon sits to the left of the text and both are centred by the theme.
/// - The native renderer guarantees that the configured label participates
///   in minimum-size calculation and cannot disappear behind a child Control.
/// - Icons are imported with the cream tint baked into the SVG
///   (<c>fill="#f2ebd3"</c>) so they stay legible against both the
///   yellow primary and grey secondary panel backgrounds without any
///   runtime tint (which would be multiplicative and darken the
///   stroke).
///
/// Tooltips use Godot's native popup, whose internal Label inherits the
/// project-wide Pixelify base theme.
/// </summary>
[GlobalClass]
public partial class IconButton : Button
{
    [Export] public string IconPath { get; set; } = string.Empty;
    [Export] public string ButtonText { get; set; } = string.Empty;

    public override void _Ready()
    {
        ApplyContent();

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
        ButtonText = label;
        ApplyContent();
    }

    private void ApplyContent()
    {
        Text = ButtonText ?? string.Empty;
        Icon = string.IsNullOrEmpty(IconPath)
            ? null
            : ResourceLoader.Load<Texture2D>(IconPath);
    }

    private void OnLineageChanged(string lineage) => ApplyAccent();

    private void ApplyAccent()
    {
        Color accent = LineageThemeRegistry.IconAccent;
        AddThemeColorOverride("icon_normal_color", accent);
        AddThemeColorOverride("icon_hover_color", accent);
        AddThemeColorOverride("icon_pressed_color", accent);
        AddThemeColorOverride("icon_focus_color", accent);
    }
}
