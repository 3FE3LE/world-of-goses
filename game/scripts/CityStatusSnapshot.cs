#nullable enable
using System.Collections.Generic;
using WorldofGoses.Domain;

namespace WorldofGoses;

public sealed record CityStatusSnapshot(
    int CurrentTick,
    int FoodStock,
    int MaxFoodStock,
    int WoodStock,
    int WoodReserve,
    int CitizensAtWork,
    int CitizensAtHome,
    IReadOnlyList<CityStatusSnapshot.ProjectItem> Projects,
    IReadOnlyList<CityStatusSnapshot.BuildingItem> Buildings,
    IReadOnlyList<string> FreeCitizenNames,
    string? HeroName,
    bool HasController,
    int CurrentSpeed)
{
    public bool IsEmpty => Buildings.Count == 0 && Projects.Count == 0;

    public sealed record ProjectItem(
        string DisplayName,
        int Progress,
        int RequiredWork,
        int AssignedCount,
        int WorkerCapacity,
        bool Enabled);

    public sealed record BuildingItem(
        BuildingKind Kind,
        string DisplayName,
        int Stock,
        int StorageCapacity,
        int MinStock,
        int MaxStock,
        string ResourceUnit,
        int AssignedCount,
        int WorkerCapacity,
        ProductionStopCause StopCause);

    public static CityStatusSnapshot From(CityWorld world, bool hasController = false, int currentSpeed = 1)
    {
        var projects = new List<ProjectItem>();
        foreach (var project in world.Projects.Values)
        {
            projects.Add(new ProjectItem(project.DisplayName, project.Progress, project.RequiredWork,
                project.AssignedCount, project.WorkerCapacity, project.Enabled));
        }

        var buildings = new List<BuildingItem>();
        foreach (var building in world.Buildings.Values)
        {
            buildings.Add(new BuildingItem(building.Kind, building.DisplayName, building.Stock, building.StorageCapacity,
                building.MinStock, building.MaxStock, building.ResourceUnit, building.AssignedCount,
                building.WorkerCapacity, building.StopCause));
        }

        var freeNames = new List<string>();
        int atWork = 0;
        int atHome = 0;
        foreach (var citizen in world.Citizens.Values)
        {
            if (citizen.CurrentLocation == CitizenLocation.AtWork) atWork++;
            else atHome++;
            if (citizen.IsAvailable) freeNames.Add(citizen.Name);
        }

        // Upkeep is dormant. Previously a chip rendered
        // "{UpkeepPerTick} stone/tick (upkeep)" from this snapshot;
        // the chip and the call site are gone, and the snapshot no
        // longer carries the field.
        return new CityStatusSnapshot(world.CurrentTick,
            world.FoodStock, world.MaxFoodStock, world.TotalWood, world.TotalWoodReserve,
            atWork, atHome, projects, buildings, freeNames, world.Hero?.Name,
            hasController, currentSpeed);
    }
}
