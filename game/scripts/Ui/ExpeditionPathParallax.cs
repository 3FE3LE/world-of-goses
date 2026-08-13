#nullable enable
namespace WorldofGoses.Ui;

/// <summary>
/// Parallax factors derived from the same world offset that drives
/// the chunk pool (#23/#25). Each factor multiplies the world
/// scroll by a non-per-symmetric ratio so rear / foreground bands
/// move relative to the playable reference. The factors are pure
/// presentation and never enter domain state.
///
/// <para>
/// The acceptance for #25 pins the directional rules:
///   - distance and rear layers shift with the world, *slower* than
///     the playable band; they convey depth.
///   - foreground shifts with the world, *faster* than the playable
///     band; it carries speed.
///   - returning reverses all three without recomputing the world
///     scroll; the same <c>Travel.PositionX</c> input yields the
///     same screen offset on a return leg.
/// </para>
/// </summary>
public static class ExpeditionPathParallax
{
    /// <summary>Distance / void: very slow parallax so the void
    /// reads as infinitely far away.</summary>
    public const float DistanceFactor = 0.10f;

    /// <summary>Rear props / vegetation: shifts with travel, slower
    /// than the playable band.</summary>
    public const float RearFactor = 0.45f;

    /// <summary>Foreground props: shifts faster than the playable
    /// band so the foreground feels close.</summary>
    public const float ForegroundFactor = 1.40f;

    /// <summary>Returns the parallax-adjusted screen offset for a
    /// layer, where <paramref name="worldOffsetUnits"/> is the same
    /// authoritative input that drives the chunk pool.</summary>
    public static float LayerOffset(long worldOffsetUnits, float factor) =>
        worldOffsetUnits * factor;
}
