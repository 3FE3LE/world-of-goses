using System;

namespace WorldofGoses.Domain;

/// <summary>Named world-clock milestones for the bounded opening route.</summary>
public static class ExpeditionTiming
{
    public const int SpiritTrailDurationTicks = 4 * GameClock.TicksPerInGameHour;
    public const int SpiritTrailEncounterOffsetTicks = GameClock.TicksPerInGameHour / 2;
    public const int SpiritTrailObjectiveOffsetTicks = 5 * GameClock.TicksPerInGameHour / 2;
    public const double RouteMinimumX = 0;
    public const double RouteMaximumX = 1000;
    public const double CityPositionX = 100;
    public const double EncounterPositionX = 360;
    public const double SpiritTrailObjectivePositionX = 850;

    public static int EncounterOffsetTicks(Expedition expedition)
    {
        ArgumentNullException.ThrowIfNull(expedition);
        return IsSpiritTrail(expedition)
            ? SpiritTrailEncounterOffsetTicks
            : Duration(expedition) / 4;
    }

    public static int ObjectiveOffsetTicks(Expedition expedition)
    {
        ArgumentNullException.ThrowIfNull(expedition);
        return IsSpiritTrail(expedition)
            ? SpiritTrailObjectiveOffsetTicks
            : Duration(expedition) * 3 / 4;
    }

    public static bool IsSpiritTrail(Expedition expedition) =>
        expedition.ResourceOpportunityKind == ResourceOpportunityKind.SpiritTrailSearch;

    public static double TravelPositionX(Expedition expedition, int currentTick)
    {
        ArgumentNullException.ThrowIfNull(expedition);
        int elapsed = Math.Clamp(
            currentTick - expedition.StartTick,
            0,
            Duration(expedition));
        if (expedition.Phase is ExpeditionPhase.Returning or ExpeditionPhase.Retreating)
        {
            int returnStart = expedition.RetreatTriggered
                ? Duration(expedition) / 2
                : ObjectiveOffsetTicks(expedition);
            double from = expedition.RetreatTriggered
                ? EncounterPositionX
                : SpiritTrailObjectivePositionX;
            return Lerp(
                from,
                CityPositionX,
                Ratio(elapsed - returnStart, Duration(expedition) - returnStart));
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

    private static int Duration(Expedition expedition) =>
        Math.Max(1, expedition.EndTick - expedition.StartTick);
}
