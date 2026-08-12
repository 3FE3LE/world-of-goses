#nullable enable

using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// One labelled figure: name on the left, value hard right, and an optional
/// trailing glyph that carries the same state the value does.
/// </summary>
/// <remarks>
/// The trailing glyph is the reason this is not just two labels in a box. A
/// happiness of 78% reads well or badly depending on a threshold, and the
/// invariants forbid communicating that by colour alone. The glyph is the second
/// channel; callers that have no state to signal simply omit it and the row
/// reserves nothing.
/// </remarks>
[GlobalClass]
public partial class HudMetricRow : HBoxContainer
{
    private readonly Label _label;
    private readonly Label _value;
    private readonly TextureRect _glyph;

    public HudMetricRow(string label, string value, string glyphPath = "")
    {
        CustomMinimumSize = new Vector2(0, Tokens.HudRowHeight);
        MouseFilter = MouseFilterEnum.Pass;
        AddThemeConstantOverride("separation", Tokens.SpacingBase);

        _label = new Label
        {
            Text = label,
            ThemeTypeVariation = "HudLabel",
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
            // The name yields, the figure never does. A Label's minimum width
            // is its whole text, so without this the row's minimum width is
            // label + value and the row simply grows past the 240 px panel —
            // the ScrollContainer then clips the overflow, which falls on the
            // value because it sits on the right. That is how "3 días",
            // "Activa" and "60%" came to render with their last glyph sliced
            // in half in Spanish while English, whose labels are shorter, fit
            // and looked fine.
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        AddChild(_label);

        _value = new Label
        {
            Text = value,
            ThemeTypeVariation = "HudNumeric",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_value);

        _glyph = new TextureRect
        {
            StretchMode = TextureRect.StretchModeEnum.Keep,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = LineageThemeRegistry.IconAccent,
            Visible = !string.IsNullOrEmpty(glyphPath),
        };
        if (!string.IsNullOrEmpty(glyphPath))
        {
            _glyph.Texture = ResourceLoader.Load<Texture2D>(glyphPath);
        }
        AddChild(_glyph);
    }

    /// <summary>Refreshes the figure in place rather than rebuilding the row.</summary>
    public void SetValue(string value) => _value.Text = value;

    /// <summary>Swaps the state glyph, or clears it when given an empty path.</summary>
    public void SetGlyph(string glyphPath)
    {
        _glyph.Texture = string.IsNullOrEmpty(glyphPath)
            ? null
            : ResourceLoader.Load<Texture2D>(glyphPath);
        _glyph.Visible = !string.IsNullOrEmpty(glyphPath);
    }
}
