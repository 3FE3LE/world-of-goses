#nullable enable
using System.Globalization;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// One complementary pair of the Kovari Cube — Cuerpo/Vínculo,
/// Estabilidad/Impulso, Dominio/Alcance — rendered as
/// <c>NOMBRE  56 [====|===] 44  NOMBRE</c>.
///
/// <para>
/// Not a <see cref="ProgressBar"/>. The project theme registers
/// <c>ProgressBar</c> on the built-in type rather than a variation, so every
/// bar in the game shares the green fill that also styles
/// <c>ButtonPrimary</c> — the success/primary semantic. A Cube axis is a
/// neutral two-pole distribution in which neither pole is better, so a
/// "filled" green bar actively misreads it. The bible also specifies the
/// <c>56 / 44</c> format rather than a percentage.
/// </para>
///
/// <para>
/// The split comes from <see cref="Control.SizeFlagsStretchRatio"/>, which
/// divides the track exactly with no arithmetic and no resize handler. The
/// dominant pole is identifiable on three independent channels: the printed
/// integer, the segment length, and the segment height. Colour is the fourth
/// and it comes from <see cref="LineageThemeRegistry.IconAccent"/>, so a
/// lineage skin may re-palette the bar without touching its hierarchy.
/// </para>
/// </summary>
[GlobalClass]
public partial class CubeAxisBar : HBoxContainer
{
    public const int RowHeight = 26;

    private const int TrackWidth = 300;
    private const int NameWidth = 104;
    private const int ValueWidth = 32;
    private const int DominantThickness = 12;
    private const int RecessiveThickness = 6;

    /// <summary>Same grey as an unlit fragment pip in the onboarding strip.</summary>
    private static readonly Color Recessive = new(0.35f, 0.37f, 0.48f, 0.55f);

    private Label _leftName = null!;
    private Label _leftValue = null!;
    private Label _rightValue = null!;
    private Label _rightName = null!;
    private ColorRect _leftFill = null!;
    private ColorRect _rightFill = null!;
    private bool _built;

    public override void _Ready() => EnsureBuilt();

    /// <summary>
    /// Builds the row on first need. <see cref="Configure"/> is normally
    /// called by a parent that has just instanced this bar and has not yet
    /// entered the tree, so it cannot rely on <see cref="_Ready"/> having run.
    /// </summary>
    private void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        CustomMinimumSize = new Vector2(0, RowHeight);
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeConstantOverride("separation", Tokens.SpacingBase);

        _leftName = AddLabel("BodySmall", HorizontalAlignment.Right, NameWidth);
        _leftValue = AddLabel("NumericText", HorizontalAlignment.Right, ValueWidth);

        var track = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(TrackWidth, DominantThickness),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        track.AddThemeConstantOverride("separation", 2);
        AddChild(track);
        _leftFill = AddFill(track);
        _rightFill = AddFill(track);

        _rightValue = AddLabel("NumericText", HorizontalAlignment.Left, ValueWidth);
        _rightName = AddLabel("BodySmall", HorizontalAlignment.Left, NameWidth);
    }

    /// <summary>
    /// Renders one complete pair. <c>FounderCubeProfile</c> guarantees by
    /// construction that the two values sum to 100, so this takes both rather
    /// than deriving one and silently hiding a domain violation.
    /// </summary>
    public void Configure(string leftName, int left, string rightName, int right)
    {
        EnsureBuilt();
        _leftName.Text = leftName.ToUpperInvariant();
        _rightName.Text = rightName.ToUpperInvariant();
        _leftValue.Text = left.ToString(CultureInfo.CurrentCulture);
        _rightValue.Text = right.ToString(CultureInfo.CurrentCulture);
        ApplySide(_leftFill, left, left >= right);
        ApplySide(_rightFill, right, right > left);
    }

    private static void ApplySide(ColorRect fill, int weight, bool dominant)
    {
        // A zero-weight side would collapse to nothing and make the track look
        // truncated rather than one-sided; keep a hairline so the axis still
        // reads as a two-pole distribution.
        fill.SizeFlagsStretchRatio = Mathf.Max(weight, 1);
        fill.CustomMinimumSize =
            new Vector2(0, dominant ? DominantThickness : RecessiveThickness);
        fill.Color = dominant ? LineageThemeRegistry.IconAccent : Recessive;
    }

    private Label AddLabel(string variation, HorizontalAlignment alignment, int width)
    {
        var label = new Label
        {
            ThemeTypeVariation = variation,
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(width, 0),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(label);
        return label;
    }

    private static ColorRect AddFill(Container track)
    {
        var fill = new ColorRect
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        track.AddChild(fill);
        return fill;
    }
}
