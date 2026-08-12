#nullable enable

using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Ui;

/// <summary>
/// One stock line: icon, resource name, the quantity held, and the per-cycle
/// change beside it.
/// </summary>
/// <remarks>
/// <para>
/// The delta is a separate label from the quantity because the two answer
/// different questions and the player scans them in different columns — "how
/// much do I have" reads down the quantity column, "am I gaining or losing"
/// reads down the delta. Concatenating them into one string collapses both
/// columns and makes neither scannable.
/// </para>
/// <para>
/// The sign is written into the text (<c>+28</c>, <c>-3</c>) rather than
/// implied by colour, so a player who cannot separate the two tints still reads
/// the direction. Colour may reinforce it; it may not carry it.
/// </para>
/// </remarks>
[GlobalClass]
public partial class HudResourceRow : HBoxContainer
{
    private readonly Label _amount;
    private readonly Label _delta;

    public HudResourceRow(string iconPath, string name, string amount, string delta = "")
        : this(CreateTextureIcon(iconPath), name, amount, delta)
    {
    }

    /// <summary>
    /// Uses the existing project-owned resource glyph while preserving this
    /// row's name/amount/delta columns.
    /// </summary>
    public HudResourceRow(ResourceType resourceType, string name, string amount, string delta = "")
        : this(CreateResourceIcon(resourceType), name, amount, delta)
    {
    }

    private HudResourceRow(Control icon, string name, string amount, string delta)
    {
        CustomMinimumSize = new Vector2(0, Tokens.HudRowHeight);
        MouseFilter = MouseFilterEnum.Pass;
        AddThemeConstantOverride("separation", Tokens.SpacingBase);

        var iconCell = new MarginContainer
        {
            CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.HudRowHeight),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(iconCell);

        iconCell.AddChild(icon);

        AddChild(new Label
        {
            Text = name,
            ThemeTypeVariation = "HudBody",
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
            // Same rule as HudMetricRow: the name yields, the amount never
            // does. Without it this row's minimum width is icon + full name +
            // amount, which is what made the whole summary column wider than
            // the panel it lives in once the names were translated.
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        });

        _amount = new Label
        {
            Text = amount,
            ThemeTypeVariation = "HudNumeric",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_amount);

        _delta = new Label
        {
            Text = delta,
            ThemeTypeVariation = "HudCaption",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            CustomMinimumSize = new Vector2(Tokens.HudRowHeight + Tokens.SpacingBase, 0),
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = !string.IsNullOrEmpty(delta),
        };
        AddChild(_delta);
    }

    private static TextureRect CreateTextureIcon(string iconPath) => new()
    {
        Texture = ResourceLoader.Load<Texture2D>(iconPath),
        StretchMode = TextureRect.StretchModeEnum.Keep,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
        SizeFlagsVertical = SizeFlags.ShrinkCenter,
        MouseFilter = MouseFilterEnum.Ignore,
        Modulate = LineageThemeRegistry.IconAccent,
    };

    private static ResourceIcon CreateResourceIcon(ResourceType resourceType) => new(resourceType)
    {
        CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
        SizeFlagsVertical = SizeFlags.ShrinkCenter,
    };

    /// <summary>Refreshes both figures together, so a row never shows a stale pair.</summary>
    public void SetValues(string amount, string delta)
    {
        _amount.Text = amount;
        _delta.Text = delta;
        _delta.Visible = !string.IsNullOrEmpty(delta);
    }
}
