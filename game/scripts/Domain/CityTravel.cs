#nullable enable
using System;

namespace WorldofGoses.Domain;

/// <summary>
/// How long it takes to walk across the city.
/// </summary>
/// <remarks>
/// <para>
/// Every in-city journey used to take <c>CityEconomyRules.AbstractTravelTicks</c>
/// — thirty ticks — whatever the distance. Duration was the constant and
/// distance was the variable, which is backwards, and it is why the same citizen
/// appeared to move at one speed walking to a nearby tree and at another walking
/// to a distant worksite. Same system, different apparent speed, because the
/// window was fixed and the ground to cover was not.
/// </para>
/// <para>
/// The city already had the geometry this needs. <c>ParcelPlacement</c> carries
/// a row and a column band per building and is persisted, so distance was always
/// computable; nobody had written the function.
/// </para>
/// <para>
/// What the domain does <em>not</em> have is routing — <c>StreetRoutePlanner</c>
/// lives in presentation. So this measures a cardinal grid distance, which is
/// the shape presentation already walks: along a street, then across streets.
/// A detour around an obstacle is absorbed by presentation inside the window it
/// receives, exactly as it is today, and that is a far smaller error than a flat
/// thirty.
/// </para>
/// </remarks>
public static class CityTravel
{
    /// <summary>Pixels one grid column spans — one 32 px tile of frontage.</summary>
    public const double ColumnPx = 32.0;

    /// <summary>Pixels one construction row spans — one 96 px lot of depth.</summary>
    public const double RowPx = 96.0;

    /// <summary>
    /// Pixels a citizen covers in one tick at walking pace. **This is the
    /// constant that defines walking speed**, and everything else about travel
    /// is derived from it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A tick is one real second at 1× (<c>GameClock.TicksPerInGameDay</c> =
    /// 3600, i.e. one real hour per in-game day), so 64 px per tick is the
    /// 64 px/s that presentation draws at walking cadence. Domain and
    /// presentation therefore agree by construction, and the drawn route no
    /// longer has to be stretched or compressed to land on the arrival tick.
    /// </para>
    /// <para>
    /// The pair it replaced —<c>TicksPerColumn = 4</c>,
    /// <c>TicksPerRow = 10</c>— were chosen by hand to keep a cross-city trip
    /// near the flat thirty ticks it used to cost. With a tick pinned to 24
    /// seconds of in-game time, that made crossing one tile of frontage take
    /// 96 in-game seconds, and every walker had to be slowed to a twelfth of
    /// its own gait to fit.
    /// </para>
    /// </remarks>
    public const double WalkPixelsPerTick = 64.0;

    /// <summary>
    /// Pixels a citizen covers in one tick at running pace — the other cadence
    /// in <c>PixelMotion</c>, 4 px every 1/24 s. Running buys distance per
    /// tick, never a different step.
    /// </summary>
    public const double RunPixelsPerTick = 96.0;

    /// <summary>Ticks to cross one column of frontage at base speed.</summary>
    public const double TicksPerColumn = ColumnPx / WalkPixelsPerTick;

    /// <summary>Ticks to cross from one street to the next at base speed.</summary>
    public const double TicksPerRow = RowPx / WalkPixelsPerTick;

    /// <summary>
    /// Floor on any journey, so two adjacent buildings still cost a walk.
    /// </summary>
    /// <remarks>
    /// Without it a citizen assigned next door would arrive on the tick they
    /// left, and "travelling" would flicker rather than happen. It was eight
    /// while a column cost four ticks; at the derived rate eight ticks is half
    /// a kilometre of frontage, and a floor that high would flatten most of a
    /// city back into one duration — the very symptom this file exists to fix.
    /// </remarks>
    public const int MinimumTravelTicks = 1;

    /// <summary>Ceiling, so a pathological layout cannot strand anyone.</summary>
    public const int MaximumTravelTicks = 240;

    /// <summary>
    /// Cardinal grid distance between two placements, in abstract units where
    /// one unit is one column of frontage.
    /// </summary>
    /// <remarks>
    /// Measured centre to centre. Using the start column instead would make a
    /// wide building's distance depend on which way round it was placed.
    /// </remarks>
    public static double Distance(ParcelPlacement? from, ParcelPlacement? to)
    {
        if (from is null || to is null) return 0;

        double fromColumn = from.StartColumn + (from.FrontageColumns / 2.0);
        double toColumn = to.StartColumn + (to.FrontageColumns / 2.0);
        double columns = Math.Abs(toColumn - fromColumn);
        double rows = Math.Abs(to.RowId.Value - from.RowId.Value);

        return (columns * TicksPerColumn) + (rows * TicksPerRow);
    }

    /// <summary>
    /// Ticks for a citizen of <paramref name="movementSpeed"/> to walk between
    /// two placements.
    /// </summary>
    /// <param name="movementSpeed">
    /// The citizen's derived <c>MovementSpeed</c>, a multiplier around 1.0. It
    /// was computed by the statistics service and read only by combat; walking
    /// across your own city is the other place a person's pace should show.
    /// </param>
    /// <remarks>
    /// <para>
    /// An endpoint the world cannot place falls back to
    /// <see cref="CityEconomyRules.AbstractTravelTicks"/> — the flat duration
    /// every journey used to take — and not to the floor. A building with no
    /// parcel is a real state: a citizen with no workplace, a fixture built
    /// without placing it. Measuring nothing and calling it "next door" would
    /// make those journeys nearly instant, which is a bigger lie than the
    /// constant was, and it would quietly rewrite the economy of every world
    /// that has not placed its buildings.
    /// </para>
    /// <para>
    /// Two placed buildings at the same spot are a different case and do get
    /// the floor: there the distance is known, and it is short.
    /// </para>
    /// </remarks>
    public static int TravelTicks(
        ParcelPlacement? from,
        ParcelPlacement? to,
        double movementSpeed = 1.0)
    {
        if (from is null || to is null) return CityEconomyRules.AbstractTravelTicks;

        double distance = Distance(from, to);
        if (distance <= 0) return MinimumTravelTicks;

        double speed = double.IsFinite(movementSpeed) && movementSpeed > 0
            ? movementSpeed
            : 1.0;

        // Ceiling: a partial tick is a tick still being walked. Truncating it
        // would let a journey arrive fractionally early, and the arrival tick is
        // the one number presentation paces its whole route against.
        int ticks = (int)Math.Ceiling(distance / speed);
        return Math.Clamp(ticks, MinimumTravelTicks, MaximumTravelTicks);
    }
}
