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
    bool HasPrimitiveAxe,
    bool CanCraftPrimitiveAxe,
    ResourceType? PrimitiveAxeMissingResource,
    int FoundingStorageCount,
    int FoundingStorageCapacity,
    IReadOnlyList<ResourceInventoryItem> Resources,
    IReadOnlyList<RecipeInput> PendingInputs,
    IReadOnlyList<BuildingDetailSnapshot.CitizenItem> AssignedCitizens,
    IReadOnlyList<BuildingDetailSnapshot.CitizenItem> AvailableCitizens,
    IReadOnlyList<BuildingDetailSnapshot.CitizenItem> VisibleCitizens,
    IReadOnlyList<BuildingDetailSnapshot.UnavailableCitizenItem> UnavailableCitizens)
{
    public int AssignedCount => AssignedCitizens.Count;
    public bool IsHome => Kind == BuildingKind.Home;
    public bool IsTownHall => Kind == BuildingKind.TownHall;
    public bool IsForest => Kind == BuildingKind.Forest;

    public sealed record CitizenItem(CitizenId Id, string Name, LineageId Lineage, GenderId Gender, AppearanceVariantId Appearance);

    /// <summary>
    /// A citizen who cannot be assigned here right now. Carries the raw
    /// reason and, for building/construction commitments, the plain
    /// (unlocalized) name of where they are committed, so the view layer
    /// can localize the explanation instead of the snapshot doing it.
    /// </summary>
    public sealed record UnavailableCitizenItem(
        CitizenId Id, string Name, CitizenAvailabilityReason Reason, string? LocationName);

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

        var unavailable = new List<UnavailableCitizenItem>();
        foreach (var citizen in world.Citizens.Values)
        {
            if (building.IsAssigned(citizen.Id)) continue;
            if (citizen.IsAvailable) continue;
            string? locationName = citizen.AvailabilityReason is
                CitizenAvailabilityReason.AssignedToBuilding or CitizenAvailabilityReason.AssignedToConstruction
                ? ResolveCommitmentLocationName(world, citizen.Commitment.EntityId)
                : null;
            unavailable.Add(new UnavailableCitizenItem(citizen.Id, citizen.Name, citizen.AvailabilityReason, locationName));
        }

        var resources = new List<ResourceInventoryItem>();
        ResourceType[] foundingResources =
        {
            ResourceType.Food,
            ResourceType.WildFood,
            ResourceType.Wood,
            ResourceType.Branches,
            ResourceType.PlantFiber,
            ResourceType.SmallStone,
        };
        var included = new HashSet<ResourceType>();
        foreach (ResourceType resource in foundingResources)
        {
            resources.Add(ToResourceItem(world, resource));
            included.Add(resource);
        }
        foreach (ResourceType resource in System.Enum.GetValues<ResourceType>())
        {
            if (included.Contains(resource) || world.Resources.Total(resource) <= 0) continue;
            resources.Add(ToResourceItem(world, resource));
        }

        return new BuildingDetailSnapshot(building.Id, building.DisplayName, building.FullDisplayLabel,
            building.Kind, building.ResourceLabel, building.ResourceUnit, building.Stock,
            building.StorageCapacity, world.CurrentProductionRate(building.Id), CityEconomyRules.ProductionCycleTicks,
            building.ProductionEnabled,
            building.MinStock, building.MaxStock, building.Priority, building.StopCause,
            building.WorkerCapacity, building.VisibleWorkerCount, building.HiddenWorkerCount,
            building.WoodReserve,
            world.HasTool(ToolKind.PrimitiveAxe),
            world.ToolCraftAvailability(ToolKind.PrimitiveAxe).IsSuccess,
            world.ToolCraftAvailability(ToolKind.PrimitiveAxe).MissingResource,
            world.FoundingStorageCount(),
            world.GroundResourceCapacity(),
            resources,
            new List<RecipeInput>(building.PendingInputs), assigned, available, visible,
            unavailable);
    }

    internal static ResourceInventoryItem ToResourceItem(CityWorld world, ResourceType resource) =>
        new(resource, world.Resources.Total(resource), world.Resources.Available(resource));

    private static CitizenItem ToItem(Citizen citizen) =>
        new(citizen.Id, citizen.Name, citizen.Profile.Lineage, citizen.Profile.Gender, citizen.AppearanceVariant);

    private static string? ResolveCommitmentLocationName(CityWorld world, int? entityId)
    {
        if (entityId is not int id) return null;
        var buildingId = new BuildingId(id);
        var committedBuilding = world.GetBuilding(buildingId);
        if (committedBuilding is not null) return committedBuilding.DisplayName;
        var project = world.GetProject(buildingId);
        return project?.DisplayName;
    }
}
