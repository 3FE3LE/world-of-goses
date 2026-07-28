#nullable enable
using System.Collections.Generic;
using WorldofGoses.Domain;

namespace WorldofGoses;

public sealed record BuildingDetailSnapshot(
    BuildingId Id,
    string DisplayName,
    string FullDisplayLabel,
    BuildingKind Kind,
    string ResourceLabel,
    string ResourceUnit,
    int Stock,
    int StorageCapacity,
    int ProductionRate,
    int ProductionCycleTicks,
    bool ProductionEnabled,
    int MinStock,
    int MaxStock,
    int Priority,
    ProductionStopCause StopCause,
    int WorkerCapacity,
    int VisibleWorkerCount,
    int HiddenWorkerCount,
    int WoodReserve,
    IReadOnlyList<RecipeInput> PendingInputs,
    IReadOnlyList<BuildingDetailSnapshot.CitizenItem> AssignedCitizens,
    IReadOnlyList<BuildingDetailSnapshot.CitizenItem> AvailableCitizens,
    IReadOnlyList<BuildingDetailSnapshot.CitizenItem> VisibleCitizens)
{
    public int AssignedCount => AssignedCitizens.Count;
    public bool IsHome => Kind == BuildingKind.Home;
    public bool IsTownHall => Kind == BuildingKind.TownHall;
    public bool IsForest => Kind == BuildingKind.Forest;

    public sealed record CitizenItem(CitizenId Id, string Name, LineageId Lineage, GenderId Gender, AppearanceVariantId Appearance);

    public static BuildingDetailSnapshot? From(CityWorld world, BuildingId buildingId)
    {
        var building = world.GetBuilding(buildingId);
        if (building is null) return null;

        var assigned = new List<CitizenItem>();
        foreach (var id in building.AssignedCitizenIds)
        {
            var citizen = world.GetCitizen(id);
            if (citizen is not null) assigned.Add(ToItem(citizen));
        }

        var available = new List<CitizenItem>();
        foreach (var citizen in world.AvailableCitizensByPriority()) available.Add(ToItem(citizen));

        var visible = new List<CitizenItem>();
        foreach (var id in world.GetCurrentlyVisibleOccupants(building))
        {
            var citizen = world.GetCitizen(id);
            if (citizen is not null) visible.Add(ToItem(citizen));
        }

        return new BuildingDetailSnapshot(building.Id, building.DisplayName, building.FullDisplayLabel,
            building.Kind, building.ResourceLabel, building.ResourceUnit, building.Stock,
            building.StorageCapacity, world.CurrentProductionRate(building.Id), CityEconomyRules.ProductionCycleTicks,
            building.ProductionEnabled,
            building.MinStock, building.MaxStock, building.Priority, building.StopCause,
            building.WorkerCapacity, building.VisibleWorkerCount, building.HiddenWorkerCount,
            building.WoodReserve, new List<RecipeInput>(building.PendingInputs), assigned, available, visible);
    }

    private static CitizenItem ToItem(Citizen citizen) =>
        new(citizen.Id, citizen.Name, citizen.Profile.Lineage, citizen.Profile.Gender, citizen.AppearanceVariant);
}
