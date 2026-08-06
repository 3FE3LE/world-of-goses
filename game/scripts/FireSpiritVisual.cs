#nullable enable
using Godot;

namespace WorldofGoses;

/// <summary>
/// Presentation-only placeholder for the fire spirit of the
/// authored first night (<c>docs/19_FIRST_NIGHT_AND_FIRE_SPIRIT.md</c>).
/// The visual is a small ring with a triangular glyph that floats
/// beside the founder before the campfire is built and hovers over
/// the campfire afterwards.
///
/// <para>
/// Position is derived from the calling code: this node stores no
/// authoritative coordinates and never persists them. The pattern
/// mirrors <see cref="FounderArrivalSequence.BuildImpactRing"/>: the
/// ring is a closed <see cref="Line2D"/> of sixteen points placed
/// with <see cref="Vector2.FromAngle"/>, and the glyph is a
/// <see cref="Polygon2D"/> triangle until a dedicated sprite lands.
/// When that happens, the triangle can be swapped for a
/// <see cref="Sprite2D"/> without touching the consumer — the public
/// surface is position-only.
/// </para>
/// </summary>
public partial class FireSpiritVisual : Node2D
{
    private const int RingPoints = 16;
    private const float RingRadius = 14f;
    private const float RingWidth = 2.5f;
    private const float TriangleSize = 6f;

    private static readonly Color RingColor = new(0.95f, 0.78f, 0.35f, 0.85f);
    private static readonly Color TriangleColor = new(1.0f, 0.82f, 0.42f, 0.95f);

    private Line2D _ring = null!;
    private Polygon2D _glyph = null!;

    public override void _Ready()
    {
        _ring = BuildRing();
        AddChild(_ring);

        _glyph = BuildTriangle();
        AddChild(_glyph);

        // Hidden by default. The controller reveals the visual only while
        // FirstNightRules.SpiritIsPresent(stage) is true.
        Visible = false;
    }

    /// <summary>
    /// Positions the spirit next to the founder before the campfire is built.
    /// The caller passes the founder's projected screen position; the spirit
    /// is offset to the right and slightly above so it never overlaps the
    /// founder's sprite.
    /// </summary>
    public void PlaceBesideFounder(Vector2 founderPosition)
    {
        Position = founderPosition + new Vector2(28f, -22f);
        Visible = true;
        _ring.DefaultColor = RingColor;
        _glyph.Color = TriangleColor;
    }

    /// <summary>
    /// Positions the spirit over the campfire once it is built. The ring
    /// widens (a separate visual signal that the spirit has moved into the
    /// flame) and the glyph brightens.
    /// </summary>
    public void PlaceOnCampfire(Vector2 campfirePosition)
    {
        Position = campfirePosition + new Vector2(0f, -10f);
        Visible = true;
        _ring.Width = RingWidth + 1.5f;
        _ring.DefaultColor = new Color(1.0f, 0.62f, 0.28f, 0.95f);
        _glyph.Color = new Color(1.0f, 0.72f, 0.32f, 1.0f);
    }

    /// <summary>Hides the visual without releasing it; the controller can re-show it later.</summary>
    public void Vanish()
    {
        Visible = false;
    }

    private static Line2D BuildRing()
    {
        var ring = new Line2D
        {
            Width = RingWidth,
            DefaultColor = RingColor,
            Closed = true,
        };
        for (int index = 0; index < RingPoints; index++)
        {
            float angle = Mathf.Tau * index / RingPoints;
            ring.AddPoint(Vector2.FromAngle(angle) * RingRadius);
        }
        return ring;
    }

    private static Polygon2D BuildTriangle()
    {
        // Pointing up: tip at top, base centred at the bottom.
        Vector2[] points =
        {
            new(0f, -TriangleSize),
            new(-TriangleSize * 0.866f, TriangleSize * 0.5f),
            new(TriangleSize * 0.866f, TriangleSize * 0.5f),
        };
        return new Polygon2D
        {
            Polygon = points,
            Color = TriangleColor,
        };
    }
}
