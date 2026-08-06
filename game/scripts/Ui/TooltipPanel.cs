#nullable enable
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Builds a tooltip <see cref="Control"/> that respects the project's
/// typography guideline. The popup is a single semi-transparent
/// cream-bordered <see cref="PanelContainer"/> with the project's
/// Pixelify 16 px label inside — no 9-slice, no lineage texture, so it
/// reads consistently across the city and never competes with the
/// panel chrome the player is interacting with.
///
/// Godot's built-in popup ignores <c>ThemeTypeVariation</c> on the
/// internal <c>Label</c>, so every control that wants a typographic
/// tooltip must hook <c>Control._MakeCustomTooltip</c> and delegate
/// here. <see cref="IconButton"/> does so for all inheritors; the
/// manual <see cref="Button"/>s created by <see cref="BuildingPlot"/>,
/// <see cref="VisibleWorkerSlot"/>, <see cref="AssignmentPanel"/>,
/// <see cref="AstralOnboardingView"/>, and <see cref="LineageShowcase"/> apply
/// the same override via <see cref="TooltipButton"/> /
/// <see cref="TooltipPanelContainer"/>.
///
/// The build is intentionally minimal: no event subscriptions, no
/// resource lookups, no allocations beyond the panel itself. Tooltips
/// must render instantly on hover — a heavier helper would introduce a
/// perceptible delay the first time a tooltip shows.
/// </summary>
public static class TooltipPanel
{
    /// <summary>Background alpha. High enough to be readable, low enough to keep the world visible behind.</summary>
    private const float BackgroundAlpha = 0.92f;

    /// <summary>Cream border colour for the box. Matches <c>TooltipText/font_color</c> in the project theme so the box and label feel like one object.</summary>
    private static readonly Color BorderColor = new(0.96f, 0.93f, 0.86f, 1f);

    /// <summary>
    /// Background colour: dark warm tone, no texture. Same hue family
    /// as the rest of the project so the tooltip feels native without
    /// using the same <see cref="StyleBoxTexture"/> the panel chrome uses.
    /// </summary>
    private static readonly Color BackgroundColor = new(
        r: 0.06f,
        g: 0.05f,
        b: 0.04f,
        a: BackgroundAlpha);

    /// <summary>
    /// Builds the tooltip control for <paramref name="text"/>. Caller
    /// assigns the returned control to its
    /// <c>_MakeCustomTooltip</c> override.
    /// </summary>
    public static Control Make(string? text)
    {
        var stylebox = new StyleBoxFlat
        {
            BgColor = BackgroundColor,
            BorderColor = BorderColor,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
            CornerRadiusBottomLeft = 0,
            CornerRadiusBottomRight = 0,
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            CornerDetail = 0,
        };

        var panel = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride("panel", stylebox);

        var label = new Label
        {
            Text = text ?? string.Empty,
            ThemeTypeVariation = "TooltipText",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddChild(label);

        return panel;
    }
}
