using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>Read-only projection of the city-wide rules currently in force.</summary>
public sealed record CityPolicySnapshot(
    int CurrentTick,
    int WorkdayStartTick,
    int WorkdayEndTick,
    bool IsWorkday,
    int ProductionCycleTicks,
    bool ProductionRequiresWorkers,
    bool ProductionStopsAtMaximum,
    bool ProductionResumesAtMinimum,
    bool OffDutyCitizensReturnToShelter,
    bool AuthorizedConstructionAdvancesAutomatically,
    bool ConstructionAuthorizationIsAutomatic)
{
    public static CityPolicySnapshot From(CityWorld world) => new(
        world.CurrentTick,
        GameClock.WorkdayStartTick,
        GameClock.WorkdayEndTick,
        GameClock.IsWorkday(world.CurrentTick),
        CityEconomyRules.ProductionCycleTicks,
        ProductionRequiresWorkers: true,
        ProductionStopsAtMaximum: true,
        ProductionResumesAtMinimum: true,
        OffDutyCitizensReturnToShelter: true,
        AuthorizedConstructionAdvancesAutomatically: true,
        ConstructionAuthorizationIsAutomatic: false);
}
