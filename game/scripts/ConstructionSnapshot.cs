#nullable enable
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;

namespace WorldofGoses;

public sealed record ConstructionSnapshot(
    bool HasHero,
    string? HeroName,
    BuildingId? HomeBuildingId,
    bool HasCultivationSite,
    bool HasTownHall,
    ConstructionSnapshot.ProjectItem? Project,
    IReadOnlyList<ConstructionSnapshot.CitizenItem> AvailableCitizens,
    IReadOnlyList<ConstructionSnapshot.UnavailableCitizenItem> UnavailableCitizens,
    IReadOnlyList<ConstructionSnapshot.OptionItem> Options,
    IReadOnlyList<ConstructionSnapshot.FoundingModuleOptionItem> FoundingModuleOptions,
    bool HasFoundingCache,
    int FoundingStorageCount,
    int FoundingStorageCapacity,
    IReadOnlyList<ResourceInventoryItem> FoundingResources,
    int ReturnableFoundingCargoCount)
{
    public bool HasHome => HomeBuildingId.HasValue;

    public sealed record CitizenItem(CitizenId Id, string Name);

    /// <summary>
    /// A citizen who cannot contribute to the current project right now.
    /// Carries the raw reason and, for building/construction commitments,
    /// the plain (unlocalized) location name; the view layer localizes it.
    /// </summary>
    public sealed record UnavailableCitizenItem(
        CitizenId Id, string Name, CitizenAvailabilityReason Reason, string? LocationName);

    public sealed record MaterialItem(ResourceType Resource, int Required, int Available, int DepositRequired)
    {
        public bool HasDeposit => Available >= DepositRequired;
    }

    public sealed record OptionItem(ConstructionKind Kind, IReadOnlyList<MaterialItem> Materials)
    {
        public bool CanPayDeposit
        {
            get
            {
                foreach (var material in Materials)
                {
                    if (!material.HasDeposit) return false;
                }
                return true;
            }
        }
    }

    public sealed record FoundingModuleOptionItem(
        FoundingSiteModule Module,
        bool PrerequisitesMet,
        bool Completed,
        IReadOnlyList<MaterialItem> Materials)
    {
        public bool CanAuthorize => PrerequisitesMet
            && !Completed
            && Materials.All(material => material.Available >= material.Required);
    }

    public sealed record ProjectItem(
        BuildingId Id,
        string DisplayName,
        BuildingKind ResultingKind,
        int Progress,
        int RequiredWork,
        int WorkerCapacity,
        bool Enabled,
        ConstructionStopCause StopCause,
        IReadOnlyList<CitizenItem> AssignedCitizens,
        IReadOnlyList<RecipeInput> RemainingInputs,
        FoundingSiteModule? ActiveFoundingModule,
        IReadOnlyList<FoundingSiteModule> CompletedFoundingModules)
    {
        public int AssignedCount => AssignedCitizens.Count;
        public bool IsAtCapacity => AssignedCount >= WorkerCapacity;
    }

    public static ConstructionSnapshot From(CityWorld world)
    {
        BuildingId? homeId = null;
        foreach (var building in world.Buildings.Values)
        {
            if (building.Kind == BuildingKind.Home)
            {
                homeId = building.Id;
                break;
            }
        }

        ProjectItem? projectItem = null;
        ConstructionProject? current = null;
        foreach (var project in world.Projects.Values)
        {
            current = project;
            break;
        }

        if (current is not null)
        {
            var assigned = new List<CitizenItem>();
            foreach (var id in current.AssignedCitizenIds)
            {
                var citizen = world.GetCitizen(id);
                if (citizen is not null) assigned.Add(new CitizenItem(id, citizen.Name));
            }
            projectItem = new ProjectItem(current.Id, current.DisplayName, current.ResultingKind,
                current.Progress, current.RequiredWork, current.WorkerCapacity, current.Enabled,
                current.StopCause, assigned, new List<RecipeInput>(current.RemainingInputs),
                current.ActiveFoundingModule,
                new List<FoundingSiteModule>(current.CompletedFoundingModules));
        }

        var available = new List<CitizenItem>();
        var unavailable = new List<UnavailableCitizenItem>();
        foreach (var citizen in world.Citizens.Values)
        {
            if (current is not null && current.IsAssigned(citizen.Id)) continue;
            if (citizen.IsAvailable)
            {
                available.Add(new CitizenItem(citizen.Id, citizen.Name));
                continue;
            }
            string? locationName = citizen.AvailabilityReason is
                CitizenAvailabilityReason.AssignedToBuilding or CitizenAvailabilityReason.AssignedToConstruction
                ? ResolveCommitmentLocationName(world, citizen.Commitment.EntityId)
                : null;
            unavailable.Add(new UnavailableCitizenItem(citizen.Id, citizen.Name, citizen.AvailabilityReason, locationName));
        }

        var options = new List<OptionItem>();
        foreach (var kind in new[] { ConstructionKind.FoundingSite, ConstructionKind.BasicShelter, ConstructionKind.CultivationSite, ConstructionKind.Farm, ConstructionKind.Quarry, ConstructionKind.TownHall })
        {
            var materials = new List<MaterialItem>();
            var recipe = Recipes.ConstructionRecipeFor(kind);
            if (recipe is not null)
            {
                foreach (var input in recipe.RequiredInputs)
                {
                    int deposit = kind is ConstructionKind.FoundingSite
                        or ConstructionKind.CultivationSite
                        ? input.Amount
                        : ConstructionRules.DepositOf(input.Amount);
                    materials.Add(new MaterialItem(input.Resource, input.Amount,
                        world.Resources.Available(input.Resource), deposit));
                }
            }
            options.Add(new OptionItem(kind, materials));
        }

        var moduleOptions = new List<FoundingModuleOptionItem>();
        if (current?.Kind == ConstructionKind.FoundingSite)
        {
            foreach (FoundingSiteModule module in System.Enum.GetValues<FoundingSiteModule>())
            {
                var materials = new List<MaterialItem>();
                foreach (RecipeInput input in FoundingSiteRules.InputsFor(module))
                {
                    materials.Add(new MaterialItem(
                        input.Resource,
                        input.Amount,
                        world.Resources.Available(input.Resource),
                        input.Amount));
                }
                moduleOptions.Add(new FoundingModuleOptionItem(
                    module,
                    FoundingSiteRules.PrerequisitesMet(module, current.HasCompletedFoundingModule),
                    current.HasCompletedFoundingModule(module),
                    materials));
            }
        }

        bool hasFoundingCache = current?.Kind == ConstructionKind.FoundingSite
            && current.HasCompletedFoundingModule(FoundingSiteModule.Cache);
        var foundingResources = new List<ResourceInventoryItem>();
        ResourceType[] visibleFoundingResources = hasFoundingCache
            ? new[]
            {
                ResourceType.Food,
                ResourceType.WildFood,
                ResourceType.Wood,
                ResourceType.Branches,
                ResourceType.PlantFiber,
                ResourceType.SmallStone,
            }
            : new[]
            {
                ResourceType.WildFood,
                ResourceType.Branches,
                ResourceType.PlantFiber,
                ResourceType.SmallStone,
            };
        foreach (ResourceType resource in visibleFoundingResources)
        {
            foundingResources.Add(BuildingDetailSnapshot.ToResourceItem(world, resource));
        }

        int foundingStorageCount = hasFoundingCache
            ? world.FoundingStorageCount()
            : world.CarriedGroundResourceCount();
        bool hasCultivationSite = world.CultivationSites.Count > 0;
        bool hasTownHall = false;
        foreach (var building in world.Buildings.Values)
        {
            if (building.Kind == BuildingKind.TownHall)
            {
                hasTownHall = true;
                break;
            }
        }
        return new ConstructionSnapshot(world.Hero is not null, world.Hero?.Name, homeId,
            hasCultivationSite, hasTownHall,
            projectItem, available, unavailable, options, moduleOptions,
            hasFoundingCache,
            foundingStorageCount,
            world.GroundResourceCapacity(),
            foundingResources,
            world.ReturnableFoundingCargoCount());
    }

    private static string? ResolveCommitmentLocationName(CityWorld world, int? entityId)
    {
        if (entityId is not int id) return null;
        var buildingId = new BuildingId(id);
        var committedBuilding = world.GetBuilding(buildingId);
        if (committedBuilding is not null) return committedBuilding.DisplayName;
        var project = world.GetProject(buildingId);
        return project?.DisplayName;
    }

    public OptionItem OptionFor(ConstructionKind kind)
    {
        foreach (var option in Options)
        {
            if (option.Kind == kind) return option;
        }
        return new OptionItem(kind, System.Array.Empty<MaterialItem>());
    }

    public FoundingModuleOptionItem? FoundingOptionFor(FoundingSiteModule module)
    {
        foreach (FoundingModuleOptionItem option in FoundingModuleOptions)
        {
            if (option.Module == module) return option;
        }
        return null;
    }
}
