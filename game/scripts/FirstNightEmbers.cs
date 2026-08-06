#nullable enable
using Godot;

namespace WorldofGoses;

/// <summary>
/// Presentation placeholder for the embers the campfire leaves behind
/// once the fire spirit has departed at dawn
/// (<c>docs/19_FIRST_NIGHT_AND_FIRE_SPIRIT.md</c> §11). The embers
/// replace the spirit's inhabited ring on the same screen position,
/// so the campfire visually transitions from "spirit-present" to
/// "spirit-departed" without any geometry shift.
///
/// <para>
/// The shape is a closed quadrilateral of four points in an orange
/// gradient — small enough to read as tizones (the Spanish term the
/// docs use) rather than a campfire. When art lands, the
/// <see cref="Line2D"/> can be replaced by a sprite without
/// touching the consumer.
/// </para>
/// </summary>
public partial class FirstNightEmbers : Node2D
{
    private const float EmbersSize = 10f;
    private static readonly Color EmbersColor = new(1.0f, 0.55f, 0.18f, 0.78f);

    private Line2D _ring = null!;

    public override void _Ready()
    {
        _ring = new Line2D
        {
            Width = 2f,
            DefaultColor = EmbersColor,
            Closed = true,
        };
        // Square of four points, sized to read as tizones rather than a
        // full campfire. The shape is decorative — a future sprite will
        // replace this primitive without changing the host.
        _ring.AddPoint(new Vector2(-EmbersSize, -EmbersSize * 0.6f));
        _ring.AddPoint(new Vector2(EmbersSize, -EmbersSize * 0.6f));
        _ring.AddPoint(new Vector2(EmbersSize * 0.7f, EmbersSize * 0.5f));
        _ring.AddPoint(new Vector2(-EmbersSize * 0.7f, EmbersSize * 0.5f));
        AddChild(_ring);

        Visible = false;
    }

    /// <summary>Positions the embers at the campfire's screen location.</summary>
    public void PlaceAt(Vector2 campfirePosition)
    {
        Position = campfirePosition + new Vector2(0f, -4f);
        Visible = true;
    }

    /// <summary>Hides the embers; the controller may re-show them later.</summary>
    public void Vanish()
    {
        Visible = false;
    }
}
