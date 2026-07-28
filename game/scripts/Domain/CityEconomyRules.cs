namespace WorldofGoses.Domain;

/// <summary>
/// Coarse economic cadence for the first playable city loop. A world tick is
/// clock resolution, not one completed unit of labour or one meal.
/// </summary>
public static class CityEconomyRules
{
    public const int ProductionCycleTicks = 10;
    public const int MealIntervalTicks = 300;
    public const int AbstractTravelTicks = 30;
    public const int FarmStorageCapacity = 60;
    public const int QuarryStorageCapacity = 80;

    public static bool IsProductionCycle(int worldTick) =>
        worldTick > 0 && worldTick % ProductionCycleTicks == 0;

    public static bool IsMealTick(int worldTick) =>
        worldTick > 0 && worldTick % MealIntervalTicks == 0;
}
