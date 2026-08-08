#nullable enable

using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// A narrow progress meter, optionally followed by its own percentage.
/// </summary>
/// <remarks>
/// <para>
/// The percentage is not decoration. A bar communicates its state by fill
/// length and fill colour, and the invariants forbid colour alone; at the
/// <see cref="Tokens.HudBarHeight"/> of eight logical pixels the fill edge is
/// also the only geometry a player has to read. Printing the number is the
/// cheap second channel, and it is why the reference shows one beside every
/// bar that matters.
/// </para>
/// <para>
/// An <c>HBoxContainer</c> wrapping a <c>ProgressBar</c> rather than a
/// <c>ProgressBar</c> subclass, because the percentage sits <em>beside</em> the
/// track in the reference, not inside it. Godot's built-in percent label draws
/// centred over the fill, where an eight-pixel track cannot hold it.
/// </para>
/// </remarks>
[GlobalClass]
public partial class HudProgressBar : HBoxContainer
{
    private readonly ProgressBar _bar;
    private readonly Label _percent;

    public HudProgressBar(double ratio, bool showPercent = true, bool tall = false)
    {
        MouseFilter = MouseFilterEnum.Pass;
        AddThemeConstantOverride("separation", Tokens.SpacingBase);

        _bar = new ProgressBar
        {
            ThemeTypeVariation = "HudProgress",
            MinValue = 0,
            MaxValue = 1,
            Value = ratio,
            ShowPercentage = false,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(
                0, tall ? Tokens.HudBarHeightCard : Tokens.HudBarHeight),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_bar);

        _percent = new Label
        {
            Text = FormatPercent(ratio),
            ThemeTypeVariation = "HudNumeric",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = showPercent,
        };
        AddChild(_percent);
    }

    /// <summary>Updates fill and percentage together, so the two cannot disagree.</summary>
    public void SetRatio(double ratio)
    {
        double clamped = Mathf.Clamp(ratio, 0.0, 1.0);
        _bar.Value = clamped;
        _percent.Text = FormatPercent(clamped);
    }

    private static string FormatPercent(double ratio) =>
        $"{Mathf.RoundToInt(Mathf.Clamp(ratio, 0.0, 1.0) * 100)}%";
}
