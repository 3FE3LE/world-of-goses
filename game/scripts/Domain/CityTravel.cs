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
    /// <summary>
    /// Ticks to cross one column of frontage at base speed.
    /// </summary>
    /// <remarks>
    /// PROVISIONAL BALANCE. Calibrated so a typical cross-city trip lands near
    /// the thirty ticks every trip used to cost, which keeps the economy
    /// roughly where it was: what changes is that a short trip is now cheaper
    /// and a long one dearer, not that everything got slower.
    /// </remarks>
    public const double TicksPerColumn = 4.0;

    /// <summary>
    /// Ticks to cross from one street to the next at base speed.
    /// </summary>
    /// <remarks>
    /// Dearer than a column because a street change is a depth move across a
    /// carriageway rather than a step along a frontage.
    /// </remarks>
    public const double TicksPerRow = 10.0;

    /// <summary>
    /// Floor on any journey, so two adjacent buildings still cost a walk.
    /// </summary>
    /// <remarks>
    /// Without it a citizen assigned next door would arrive on the tick they
    /// left, and "travelling" would flicker rather than happen.
    /// </remarks>
    public const int MinimumTravelTicks = 8;

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
