#nullable enable

using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// A small count carried on top of something else — the pending items behind a
/// dock button, the unread lines behind a log.
/// </summary>
/// <remarks>
/// <para>
/// The badge is the one place the compact HUD spends its accent colour on a
/// surface rather than on a line, which is exactly why it works: amber appears so
/// rarely elsewhere that a single amber pill is unmissable. Spending the accent on
/// ordinary chrome is what would make this invisible.
/// </para>
/// <para>
/// It reuses <c>hud_button_selected</c> for its surface rather than promoting a
/// tenth composite. Both are the same authored frame at the same amber ramp; a
/// badge is a selected chip that happens not to be pressable.
/// </para>
/// </remarks>
[GlobalClass]
public partial class HudBadge : PanelContainer
{
    private readonly Label _count;

    public HudBadge(string text)
    {
        ThemeTypeVariation = "HudBadge";
        CustomMinimumSize = new Vector2(Tokens.HudBadgeHeight, Tokens.HudBadgeHeight);
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        MouseFilter = MouseFilterEnum.Ignore;

        _count = new Label
        {
            Text = text,
            ThemeTypeVariation = "HudCaption",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        // The badge is the only HUD label that sits on a light surface, so it
        // cannot inherit HudCaption's colour, which is tuned for a dark one.
        _count.AddThemeColorOverride("font_color", new Color(0.08f, 0.07f, 0.05f));
        AddChild(_count);
    }

    /// <summary>Updates the count, hiding the badge entirely at zero.</summary>
    public void SetCount(int count)
    {
        _count.Text = count.ToString();
        Visible = count > 0;
    }
}
