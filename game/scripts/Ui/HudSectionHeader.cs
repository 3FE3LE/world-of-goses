#nullable enable

using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// The compact HUD's section divider: a short recessed strip carrying a section
/// name, and optionally a count on its right.
/// </summary>
/// <remarks>
/// <para>
/// The strip is <c>HudHeaderSurface</c>, which is <em>darker</em> than the panel it
/// sits in rather than lighter. That is measured, not stylistic: in the reference
/// the panel body fills at luminance 12 and the section strip at 8. A header that
/// reads as raised chrome belongs to a heavier UI language than this one, where
/// hierarchy is carried by recession and a one-pixel rule.
/// </para>
/// <para>
/// A <c>[GlobalClass]</c> node rather than a PackedScene for the reason
/// <c>UI_PATTERNS.md</c> §2.4 gives and <see cref="StatChip"/> already follows:
/// these are built procedurally by whichever panel needs a section, never dropped
/// into a scene by hand, so the editor reuse a PackedScene buys does not apply.
/// </para>
/// </remarks>
[GlobalClass]
public partial class HudSectionHeader : PanelContainer
{
    private readonly Label _title;
    private readonly Label _trailing;

    public HudSectionHeader(string title, string trailing = "")
    {
        ThemeTypeVariation = "HudHeaderSurface";
        CustomMinimumSize = new Vector2(0, Tokens.HudHeaderHeight);
        MouseFilter = MouseFilterEnum.Ignore;

        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        AddChild(row);

        _title = new Label
        {
            Text = title,
            ThemeTypeVariation = "HudLabel",
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddChild(_title);

        _trailing = new Label
        {
            Text = trailing,
            ThemeTypeVariation = "HudNumeric",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = !string.IsNullOrEmpty(trailing),
        };
        row.AddChild(_trailing);
    }

    /// <summary>Replaces the section name without rebuilding the strip.</summary>
    public void SetTitle(string title) => _title.Text = title;

    /// <summary>
    /// Replaces the right-hand count. An empty value hides the label rather than
    /// leaving an empty cell that still claims its minimum width.
    /// </summary>
    public void SetTrailing(string trailing)
    {
        _trailing.Text = trailing;
        _trailing.Visible = !string.IsNullOrEmpty(trailing);
    }
}
