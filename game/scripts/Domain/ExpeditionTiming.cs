using System;

namespace WorldofGoses.Domain;

/// <summary>Named world-clock milestones for the bounded opening route.</summary>
public static class ExpeditionTiming
{
    /// <summary>
    /// The trail's length in pixels. **This is the declared fact**; the four
    /// hours the walk takes are derived from it at walking pace.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It was the other way round: a flat
    /// <c>4 * GameClock.TicksPerInGameHour</c> with the route's positions
    /// interpolated to fill it. The route spanned about 750 px, so a party
    /// covered 1.25 px per tick — a fiftieth of the 64 px/tick the same people
    /// walk at inside the city, and the same inversion
    /// <see href="https://github.com/3FE3LE/world-of-goses/issues/58">#58</see>
    /// found there: distance fitted to a duration instead of the reverse.
    /// </para>
    /// <para>
    /// The length is chosen so the walked duration stays at the 600 ticks it
    /// has always been. Nothing about the pace of the opening route changes;
    /// what changes is that the number is now a consequence, so a longer trail
    /// takes longer and running one shortens it.
    /// </para>
    /// </remarks>
    public const double SpiritTrailLengthPx =
        4 * GameClock.TicksPerInGameHour * CityTravel.WalkPixelsPerTick;

    /// <summary>Duration of the trail on foot, derived from its length.</summary>
    public const int SpiritTrailDurationTicks =
        (int)(SpiritTrailLengthPx / CityTravel.WalkPixelsPerTick);

    /// <summary>
    /// How far out the objective sits. The party walks there and walks back, so
    /// it is half of what the trail costs in total.
    /// </summary>
    public const double OutboundLengthPx = SpiritTrailLengthPx / 2;

    // Waypoints along the outbound leg, in pixels from the city.
    public const double RouteMinimumX = 0;
    public const double RouteMaximumX = OutboundLengthPx;
    public const double CityPositionX = 0;
    public const double EncounterPositionX = 0.36 * OutboundLengthPx;
    public const double SpiritTrailObjectivePositionX = OutboundLengthPx;

    /// <summary>
    /// The tick a leg ends on, derived from how far it runs at walking pace.
    /// </summary>
    /// <remarks>
    /// These were authored constants —
    /// <c>TicksPerInGameHour / 2</c> and <c>5 * TicksPerInGameHour / 2</c>— while
    /// the waypoints were fractions of the route. Each leg therefore covered its
    /// share of the distance in a share of the time that had nothing to do with
    /// the pace: measured, the outbound leg ran at 133 px/tick, the objective
    /// leg at 63 and the return at 124, for a party that walks at 64. The phase
    /// changed at a moment and the position was interpolated to arrive there —
    /// distance fitted to time, the same inversion one level down.
    /// </remarks>
    public const int SpiritTrailEncounterOffsetTicks =
        (int)(EncounterPositionX / CityTravel.WalkPixelsPerTick);

    public const int SpiritTrailObjectiveOffsetTicks =
        (int)(OutboundLengthPx / CityTravel.WalkPixelsPerTick);

    /// <summary>
    /// What the same trail costs at a given pace. Walking is
    /// <see cref="SpiritTrailDurationTicks"/>; running it is the seam for a
    /// forced march, which buys time and spends the reserve that lets a party
    /// close distance when it arrives.
    /// </summary>
    public static int DurationTicksAt(double pixelsPerTick) =>
        pixelsPerTick <= 0
            ? SpiritTrailDurationTicks
            : Math.Max(1, (int)Math.Ceiling(SpiritTrailLengthPx / pixelsPerTick));

    public static int EncounterOffsetTicks(Expedition expedition)
    {
        ArgumentNullException.ThrowIfNull(expedition);
        return IsSpiritTrail(expedition)
            ? SpiritTrailEncounterOffsetTicks
            : TravelDuration(expedition) / 4;
    }

    public static int ObjectiveOffsetTicks(Expedition expedition)
    {
        ArgumentNullException.ThrowIfNull(expedition);
        return IsSpiritTrail(expedition)
            ? SpiritTrailObjectiveOffsetTicks
            : TravelDuration(expedition) * 3 / 4;
    }

    public static bool IsSpiritTrail(Expedition expedition) =>
        expedition.ResourceOpportunityKind == ResourceOpportunityKind.SpiritTrailSearch;

    public static double TravelPositionX(Expedition expedition, int currentTick)
    {
        ArgumentNullException.ThrowIfNull(expedition);
        // Ticks spent *walking*, which is not the same as ticks since dispatch:
        // whatever the road charged — a fight, a detour — was time the party
        // stood still. Subtracting it shifts every leg later instead of
        // stretching the remaining ones, so the walk holds one pace from the
        // city gate to the city gate and a delay reads as a delay rather than
        // as everyone suddenly walking slower on the way home.
        int elapsed = Math.Clamp(
            currentTick - expedition.StartTick - expedition.EstimateDeltaTicks,
            0,
            TravelDuration(expedition));
        if (expedition.Phase is ExpeditionPhase.Returning or ExpeditionPhase.Retreating)
        {
            int returnStart = expedition.RetreatTriggered
                ? TravelDuration(expedition) / 2
                : ObjectiveOffsetTicks(expedition);
            double from = expedition.RetreatTriggered
                ? EncounterPositionX
                : SpiritTrailObjectivePositionX;
            return Lerp(
                from,
                CityPositionX,
                Ratio(elapsed - returnStart, TravelDuration(expedition) - returnStart));
        }
        if (expedition.Phase == ExpeditionPhase.Objective)
        {
            return Lerp(
                EncounterPositionX,
                SpiritTrailObjectivePositionX,
                Ratio(
                    elapsed - EncounterOffsetTicks(expedition),
                    ObjectiveOffsetTicks(expedition) - EncounterOffsetTicks(expedition)));
        }
        return Lerp(
            CityPositionX,
            EncounterPositionX,
            Ratio(elapsed, EncounterOffsetTicks(expedition)));
    }

    private static double Ratio(int elapsed, int duration) =>
        duration <= 0 ? 1 : Math.Clamp(elapsed / (double)duration, 0, 1);

    private static double Lerp(double from, double to, double ratio) =>
        from + (to - from) * ratio;

    /// <summary>
    /// How long the journey is <em>walked</em> for, ignoring whatever the road
    /// charged. It is the estimate, which is exactly the distance at pace.
    /// </summary>
    private static int TravelDuration(Expedition expedition) =>
        Math.Max(1, expedition.EstimatedEndTick - expedition.StartTick);
}
