#nullable enable
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Bundled read-only projection of the aggregate facts the macro view
/// (<see cref="Prototypes.MacroStreetLiveView"/>) reads every frame.
/// Replaces the previous 33+ <c>_controller.World.X</c> accesses with
/// one immutable snapshot the view caches and rebuilds on
/// <c>WorldTickAdvanced</c>. The view file is kept intact (A1's hard
/// constraint); only its data sources change.
/// </summary>
public sealed record MacroStreetLiveViewState(
    int CurrentTick,
    int? FoodStock,
    CitizenId? HeroId,
    bool HeroIsAvailable,
    BuildingId? PrimaryHomeId,
    BuildingId? FoundingStorageBuildingId,
    bool HasCultivationSite)
{
    public static MacroStreetLiveViewState From(CityWorld world)
    {
        Citizen? hero = world.Hero;
        int? foundingId = world.FoundingSiteBuildingId();
        return new MacroStreetLiveViewState(
            world.CurrentTick,
            world.FoodStock,
            hero?.Id,
            hero is { IsAvailable: true },
            world.PrimaryHome?.Id,
            foundingId.HasValue ? new BuildingId(foundingId.Value) : null,
            world.CultivationSites.Count > 0);
    }
}

/// <summary>
/// Compact read-only citizen projection used by the macro view when it
/// needs a citizen's routing state without handing the view the live
/// <see cref="Citizen"/> entity. Replaces the
/// <c>_controller.World.GetCitizen(id)</c> reads in
/// <see cref="Prototypes.MacroStreetLiveView"/>.
/// </summary>
public sealed record MacroCitizenSnapshot(
    bool IsAvailable,
    bool IsHero,
    CitizenLocation CurrentLocation,
    BuildingId? CurrentAssignment,
    bool IsReturningHome,
    int CurrentStamina,
    int MaxStamina)
{
    public static MacroCitizenSnapshot? From(CityWorld world, CitizenId id)
    {
        Citizen? citizen = world.GetCitizen(id);
        return citizen is null
            ? null
            : new MacroCitizenSnapshot(
                citizen.IsAvailable,
                citizen.IsHero,
                citizen.CurrentLocation,
                citizen.CurrentAssignment,
                citizen.IsReturningHome,
                citizen.CurrentStamina,
                citizen.MaxStamina);
    }
}