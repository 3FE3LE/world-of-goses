#nullable enable
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// The fire spirit of the authored first night
/// (<c>docs/systems/first-night.md</c>): a small manifestation of
/// flame that hovers beside the founder before the campfire is built and
/// settles into the fire afterwards.
///
/// <para>
/// <b>Still a placeholder, but no longer an abstract one.</b> It used to be a
/// sixteen-point ring with a triangle inside, which read as a geometric HUD
/// marker rather than a living thing. None of the three Kenney packs in
/// <c>art/exports/ui/</c> ships a free-standing flame: the closest art is a
/// hearth or brazier, which carries its own stonework and reads as furniture.
/// Cropping a flame out of one of those would mean hand-editing an exported
/// PNG, which <c>docs/presentation/art-pipeline.md</c> §10 forbids. So the shape is
/// authored here — layered flame polygons in the fire palette — until real
/// art exists.
/// </para>
///
/// <para>
/// The flicker follows the project's motion grammar: two discrete states
/// swapped on the shared 12 Hz cadence, never a continuous tween. Position is
/// supplied by the caller and never stored authoritatively.
/// </para>
/// </summary>
public partial class FireSpiritVisual : Node2D
{
    /// <summary>Two frames at the project's 12 Hz presentation cadence.</summary>
    private const double FlickerSeconds = 1.0 / 12.0;

    /// <summary>
    /// Where the spirit sits relative to the founder's feet: clear of his
    /// silhouette horizontally and lifted well off the ground line, so it
    /// reads as hovering beside him instead of burning at his feet.
    /// </summary>
    public static readonly Vector2 SpiritHoverOffset = new(34f, -44f);

    private static readonly Color OuterCalm = new(0.96f, 0.55f, 0.18f, 0.80f);
    private static readonly Color InnerCalm = new(1.0f, 0.82f, 0.42f, 0.95f);
    private static readonly Color OuterLit = new(1.0f, 0.66f, 0.24f, 0.92f);
    private static readonly Color InnerLit = new(1.0f, 0.93f, 0.62f, 1.0f);

    private Polygon2D _outer = null!;
    private Polygon2D _inner = null!;
    private Polygon2D _core = null!;
    private double _flickerAccumulator;
    private bool _lit;

    public override void _Ready()
    {
        // Doc 19 calls it "una pequeña manifestación de fuego". At the earlier
        // size it stood as tall as the founder and sat on his ground line, so
        // it read as a campfire burning in front of the citizen rather than as
        // a spirit hovering next to him.
        _outer = BuildFlame(6f, 10f);
        AddChild(_outer);

        _inner = BuildFlame(3.5f, 6.5f);
        AddChild(_inner);

        _core = BuildFlame(1.5f, 3.5f);
        AddChild(_core);

        ApplyFlicker();

        // Hidden by default. FirstNightScene reveals the spirit only while
        // FirstNightRules.SpiritIsPresent(stage) is true.
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        _flickerAccumulator += delta;
        if (_flickerAccumulator < FlickerSeconds) return;
        _flickerAccumulator = 0.0;
        _lit = !_lit;
        ApplyFlicker();
    }

    /// <summary>
    /// Moves the flame to a world anchor. Called every frame while visible,
    /// because the macro view projects its streets by hand and there is no
    /// parent transform to inherit.
    /// </summary>
    public void MoveTo(Vector2 anchor)
    {
        Position = new Vector2(Mathf.Round(anchor.X), Mathf.Round(anchor.Y));
    }

    /// <summary>Hovers beside the founder, before any fire exists.</summary>
    public void PlaceBesideFounder(Vector2 founderPosition)
    {
        MoveTo(founderPosition + SpiritHoverOffset);
        Visible = true;
    }

    /// <summary>Settles into the campfire once it is built.</summary>
    public void PlaceOnCampfire(Vector2 campfirePosition)
    {
        MoveTo(campfirePosition + new Vector2(0f, -10f));
        Visible = true;
    }

    /// <summary>Hides the visual without releasing it; the scene can re-show it later.</summary>
    public void Vanish()
    {
        Visible = false;
    }

    private void ApplyFlicker()
    {
        _outer.Color = _lit ? OuterLit : OuterCalm;
        _inner.Color = _lit ? InnerLit : InnerCalm;
        _core.Color = _lit ? InnerLit : InnerCalm;
        // The tip stretches on the lit frame. One pixel, snapped — the step is
        // the point, a sub-pixel breath would read as a continuous tween.
        _outer.Scale = new Vector2(1f, _lit ? 1.12f : 1f);
    }

    /// <summary>
    /// A teardrop flame: wide round base, waist, and an off-centre tip, built
    /// on integer-friendly proportions so it stays crisp at the project's
    /// nearest-neighbour filtering.
    /// </summary>
    private static Polygon2D BuildFlame(float halfWidth, float height)
    {
        Vector2[] points =
        {
            new(0f, -height),
            new(halfWidth * 0.55f, -height * 0.55f),
            new(halfWidth, -height * 0.10f),
            new(halfWidth * 0.72f, height * 0.28f),
            new(0f, height * 0.42f),
            new(-halfWidth * 0.72f, height * 0.28f),
            new(-halfWidth, -height * 0.10f),
            new(-halfWidth * 0.55f, -height * 0.55f),
        };
        return new Polygon2D { Polygon = points };
    }
}
