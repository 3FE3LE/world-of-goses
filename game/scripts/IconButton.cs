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

    /// <summary>
    /// Whether the label is rendered. False collapses the button to its icon
    /// while keeping <see cref="ButtonText"/> intact, so the label can come back
    /// without the caller having to remember what it was.
    /// </summary>
    /// <remarks>
    /// Lives here rather than in the surface that wants the compaction because
    /// <see cref="SetIconAndLabel"/> has other callers: the macro view rewrites
    /// the construction and camera buttons as their state changes. If compaction
    /// were applied from outside, the next one of those writes would silently
    /// restore the text. Routing both through <see cref="ApplyContent"/> means
    /// the two cannot fight.
    /// </remarks>
    public bool ShowLabel
    {
        get => _showLabel;
        set
        {
            if (_showLabel == value) return;
            _showLabel = value;
            ApplyContent();
        }
    }

    private bool _showLabel = true;

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
        Text = _showLabel ? (ButtonText ?? string.Empty) : string.Empty;
        Icon = string.IsNullOrEmpty(IconPath)
            ? null
            : ResourceLoader.Load<Texture2D>(IconPath);
    }

    private void OnLineageChanged(string lineage)
    {
        // The static LineageThemeRegistry event can outlive this node
        // when the node is freed through a path that bypasses _ExitTree
        // (e.g., the parent frees a child whose notification chain was
        // already torn down). AddThemeColorOverride on a disposed Godot
        // Control throws ObjectDisposedException, and the rest of the
        // registry subscribers would lose their callback to that error
        // because the invocation list stops at the first throw. Discard
        // the event when the wrapper has already been released so the
        // surviving buttons keep updating. The defensive check costs a
        // couple of nanoseconds on the hot path because most buttons are
        // inside the tree at the moment a lineage change fires.
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        ApplyAccent();
    }

    private void ApplyAccent()
    {
        Color accent = LineageThemeRegistry.IconAccent;
        AddThemeColorOverride("icon_normal_color", accent);
        AddThemeColorOverride("icon_hover_color", accent);
        AddThemeColorOverride("icon_pressed_color", accent);
        AddThemeColorOverride("icon_focus_color", accent);
    }
}
