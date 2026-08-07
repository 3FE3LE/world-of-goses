#nullable enable

using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// One icon-plus-text pair: the smallest unit of readable state in the UI.
/// </summary>
/// <remarks>
/// <para>
/// Keeps the icon-on-the-left layout consistent wherever a compact fact is shown.
/// Deliberately carries no panel chrome of its own, so it inlines into a strip, a
/// row or a card without drawing a second surface inside the first.
/// </para>
/// <para>
/// Icons ship with a white SVG fill and are tinted at construction with the active
/// lineage's accent. A chip does not subscribe to lineage changes itself — the
/// surface that owns the chips re-tints them, so one signal handler serves a whole
/// strip instead of one per chip. <c>IconButton</c> shows the cost of the other
/// approach: it needs an <c>IsInstanceValid</c> guard because a static event
/// outlives freed nodes.
/// </para>
/// <para>
/// A <c>[GlobalClass]</c> node rather than a PackedScene because chips are only
/// ever built procedurally, never placed in the editor, so the editor reuse that
/// <c>UI_PATTERNS.md</c> §2 asks a PackedScene for does not apply. §2.4 routes a
/// widget with its own construction logic here.
/// </para>
/// </remarks>
[GlobalClass]
public partial class StatChip : HBoxContainer
{
    /// <summary>Variation used when a caller does not name one.</summary>
    public const string DefaultLabelVariation = "BodySmall";

    private readonly Label _label;

    public StatChip(string iconPath, string text, string labelVariation = DefaultLabelVariation)
    {
        MouseFilter = MouseFilterEnum.Pass;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        CustomMinimumSize = new Vector2(0, Tokens.ChipHeight);
        AddThemeConstantOverride("separation", Tokens.SpacingBase);
        TooltipText = string.Empty;

        var iconCell = new MarginContainer
        {
            CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.ChipHeight),
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        // One pixel down: the glyphs sit optically high against a 16 px baseline.
        iconCell.AddThemeConstantOverride("margin_top", 1);
        AddChild(iconCell);

        var icon = new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(iconPath),
            StretchMode = TextureRect.StretchModeEnum.Keep,
            CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = LineageThemeRegistry.IconAccent,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        iconCell.AddChild(icon);

        _label = new Label
        {
            Text = text,
            // An empty variation falls back rather than throwing: callers that
            // pass a computed variation should still render readable text.
            ThemeTypeVariation = string.IsNullOrEmpty(labelVariation)
                ? DefaultLabelVariation
                : labelVariation,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(_label);
    }

    /// <summary>
    /// Replaces the text without rebuilding the icon, so a value can refresh in
    /// place instead of the owner discarding and recreating the chip.
    /// </summary>
    public void UpdateText(string text) => _label.Text = text;
}
