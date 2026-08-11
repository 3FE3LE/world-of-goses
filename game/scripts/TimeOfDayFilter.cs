#nullable enable
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Ambient tint over the city map that follows the simulation clock.
/// Renders a single full-viewport <see cref="ColorRect"/> on
/// <see cref="OverlayLayers.AmbientTint"/>: above the world, below
/// every piece of HUD chrome. See <see cref="OverlayLayers"/> for the
/// canonical layer catalog.
///
/// This is an immersion effect for the map, not a global colour grade.
/// Two independent guards keep it there, because the layer catalog
/// alone proved too easy to fall out of:
///
/// <list type="number">
/// <item>Z ordering — HUD chrome claims <see cref="OverlayLayers.Hud"/>,
/// which outranks the tint. A control that renders tinted when it
/// should not is a control that forgot to claim that layer.</item>
/// <item>Visibility — the tint mirrors the macro view's own
/// <c>Visible</c> flag, so any full-screen view that replaces the map
/// (building detail, hero profile) is untinted by construction, even
/// if it never touches the catalog.</item>
/// </list>
///
/// The colour is recomputed every <see cref="_Process"/> tick from
/// <see cref="TimeOfDayColor"/>; the helper is pure so the tests in
/// <c>TimeOfDayColorTests</c> can pin the contract without booting
/// the Godot renderer.
/// </summary>
public partial class TimeOfDayFilter : Control
{
    [Export] public NodePath ControllerPath { get; set; } = "../../../CityWorldController";

    /// <summary>The map this tint belongs to. The tint is visible only
    /// while that node is.</summary>
    [Export] public NodePath MacroViewPath { get; set; } = "../MacroStreetLiveView";

    private CityWorldController _controller = null!;
    private CanvasItem? _macroView;
    private ColorRect _rect = null!;
    private Color _color = new(0, 0, 0, 0);
    private double? _pinnedFraction;

    /// <summary>
    /// Pins the tint to one moment of the in-game day so the visual
    /// regression matrix can capture dawn, noon, midnight and dusk
    /// reproducibly. Without this the captured hour is whatever the save
    /// happens to hold, which makes an ambient effect impossible to
    /// review. Visual-regression only; never called in normal play.
    /// </summary>
    public void PinDayFractionForVisualRegression(double fraction)
    {
        _pinnedFraction = fraction;
    }

    public override void _Ready()
    {
        // Stretch over the entire viewport, ignore input so it never
        // steals clicks from the macro view below.
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        // Above the world, below the HUD. Pin to the catalog so the
        // relative ordering is documented in one place.
        OverlayLayers.Apply(this, OverlayLayers.AmbientTint);

        // Optional on purpose: a scene that shows the tint without a
        // macro view (a test bed, a future interior map) stays valid,
        // it simply loses the visibility guard.
        _macroView = GetNodeOrNull<CanvasItem>(MacroViewPath);

        _controller = GetNode<CityWorldController>(ControllerPath);

        _rect = new ColorRect
        {
            Color = TimeOfDayColor.ForFraction(0),
            MouseFilter = MouseFilterEnum.Ignore,
            // Multiply, not alpha-over. An alpha veil scales contrast by
            // (1 - alpha) and lifts the black point, so a night strong
            // enough to read as night flattened the map into fog.
            // Multiplying keeps black at black and preserves the world's
            // full dynamic range, which decouples "how dark is it" from
            // "how much detail survives". Godot's Mul blend ignores the
            // source alpha, which is why TimeOfDayColor encodes strength
            // in the RGB channels and always returns alpha 1.
            Material = new CanvasItemMaterial
            {
                BlendMode = CanvasItemMaterial.BlendModeEnum.Mul,
            },
        };
        // Anchor the rect to the parent Control so it inherits the
        // viewport-sized bounds without a CanvasLayer in the middle.
        _rect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_rect);
    }

    public override void _Process(double delta)
    {
        // Follow the map in and out of view. _Process keeps running while
        // this node is hidden, so the tint resumes with the right colour
        // the moment the player returns to the macro view.
        if (_macroView is not null && Visible != _macroView.Visible)
        {
            Visible = _macroView.Visible;
        }

        if (_pinnedFraction is null && _controller is null) return;
        // Same held clock as the status strip: while the first night runs the
        // ambient tint must stay at night, or the world brightens into day
        // around a player the spirit is still guiding through the dark.
        int? projectedTick = _controller!.GetDisplayedTick();
        int displayedTick = projectedTick ?? _controller.CurrentTick;
        Color next = TimeOfDayColor.ForFraction(
            _pinnedFraction ?? GameClock.DayFraction(displayedTick));
        if (next == _color) return;
        _color = next;
        _rect.Color = next;
    }
}