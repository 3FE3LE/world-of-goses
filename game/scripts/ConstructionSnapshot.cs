#nullable enable
using System.Collections.Generic;
using WorldofGoses.Domain;

namespace WorldofGoses;

public sealed record ConstructionSnapshot(
    bool HasHero,
    string? HeroName,
    BuildingId? HomeBuildingId,
    ConstructionSnapshot.ProjectItem? Project,
    IReadOnlyList<ConstructionSnapshot.CitizenItem> AvailableCitizens,
    IReadOnlyList<ConstructionSnapshot.OptionItem> Options)
{
    public bool HasHome => HomeBuildingId.HasValue;

    public sealed record CitizenItem(CitizenId Id, string Name);

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
        IReadOnlyList<RecipeInput> RemainingInputs)
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
                current.StopCause, assigned, new List<RecipeInput>(current.RemainingInputs));
        }

        var available = new List<CitizenItem>();
        foreach (var citizen in world.Citizens.Values)
        {
            if (current is not null && current.IsAssigned(citizen.Id)) continue;
            if (citizen.CurrentAssignment.HasValue && citizen.CurrentAssignment != current?.Id) continue;
            available.Add(new CitizenItem(citizen.Id, citizen.Name));
        }

        var options = new List<OptionItem>();
        foreach (var kind in new[] { ConstructionKind.BasicShelter, ConstructionKind.Farm, ConstructionKind.Quarry })
        {
            var materials = new List<MaterialItem>();
            var recipe = Recipes.ConstructionRecipeFor(kind);
            if (recipe is not null)
            {
                foreach (var input in recipe.RequiredInputs)
                {
                    materials.Add(new MaterialItem(input.Resource, input.Amount,
                        world.TotalStockOf(input.Resource), ConstructionRules.DepositOf(input.Amount)));
                }
            }
            options.Add(new OptionItem(kind, materials));
        }

        return new ConstructionSnapshot(world.Hero is not null, world.Hero?.Name, homeId,
            projectItem, available, options);
    }

    public OptionItem OptionFor(ConstructionKind kind)
    {
        foreach (var option in Options)
        {
            if (option.Kind == kind) return option;
        }
        return new OptionItem(kind, System.Array.Empty<MaterialItem>());
    }
}
