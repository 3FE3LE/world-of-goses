#nullable enable

using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Ui;

/// <summary>
/// A compact state chip: an icon next to a short localized label.
/// </summary>
/// <remarks>
/// <para>
/// Where <see cref="HudBadge"/> carries a numeric count on top of another
/// surface, <c>HudStateBadge</c> carries a short word next to a glyph so the
/// reader can tell one phase from another without parsing the label first.
/// Both reuse the same authored surface in <c>default_theme.tres</c>; the
/// difference is the cell layout, not the chrome.
/// </para>
/// <para>
/// The widget exists because the expedition rail needs to show phase as a
/// chip (Outbound, Encounter, Objective, Returning, Retreating, Resolved)
/// and the only candidates were the numeric <see cref="HudBadge"/> — a
/// semantic stretch — and a bespoke expedition-only widget, which the
/// presentation rules reject. The smallest reusable extension wins.
/// </para>
/// <para>
/// State meaning is carried by the icon plus the label, never by colour
/// alone. <see cref="IconFor"/> is the single source of truth for the
/// icon-for-phase map so the showcase, the card and the test can lock it
/// down.
/// </para>
/// </remarks>
[GlobalClass]
public partial class HudStateBadge : PanelContainer
{
    /// <summary>
    /// Returns the leading icon path for a given expedition phase.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Outbound and Resolved intentionally share the checkpoint glyph, and
    /// Returning and Retreating share the left arrow. The arrow is the
    /// "going home" semantic; the <em>label</em> tells the reader whether
    /// the team is on schedule or has broken contact. Colour never carries
    /// the difference, only the localized word does.
    /// </para>
    /// </remarks>
    public static string IconFor(ExpeditionPhase phase) => phase switch
    {
        ExpeditionPhase.Outbound => IconPaths.Backpack,
        ExpeditionPhase.Encounter => IconPaths.Shield,
        ExpeditionPhase.Objective => IconPaths.Check,
        ExpeditionPhase.Returning => IconPaths.ArrowLeft,
        ExpeditionPhase.Retreating => IconPaths.ArrowLeft,
        ExpeditionPhase.Resolved => IconPaths.Check,
        _ => IconPaths.Check,
    };

    private readonly TextureRect _icon = null!;
    private readonly Label _label = null!;

    public HudStateBadge(string iconPath, string text)
    {
        ThemeTypeVariation = "HudStateBadge";
        CustomMinimumSize = new Vector2(0, Tokens.HudBadgeHeight);
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        MouseFilter = MouseFilterEnum.Ignore;

        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        AddChild(row);

        _icon = new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(iconPath),
            StretchMode = TextureRect.StretchModeEnum.Keep,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = LineageThemeRegistry.IconAccent,
        };
        row.AddChild(_icon);

        _label = new Label
        {
            Text = text,
            ThemeTypeVariation = "HudCaption",
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        // The badge is the only HUD label that sits on a light surface, so
        // it cannot inherit HudCaption's colour, which is tuned for a dark
        // one. The light surface is what gives the badge its amber accent.
        _label.AddThemeColorOverride("font_color", new Color(0.08f, 0.07f, 0.05f));
        row.AddChild(_label);
    }

    /// <summary>Replaces the label without rebuilding the chip.</summary>
    public void SetText(string text) => _label.Text = text;
}