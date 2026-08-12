#nullable enable
using System.Collections.Generic;
using WorldofGoses.Domain;

namespace WorldofGoses;

public sealed record CityStatusSnapshot(
    int CurrentTick,
    string? LineageName,
    IReadOnlyList<ResourceInventoryItem> Resources,
    int CitizenCount,
    int HousingCapacity,
    int FoodStock,
    int MaxFoodStock,
    int DailyFoodRation,
    int FoodHorizonDays,
    int ProtectedFoodTarget,
    int? TicksUntilFirstHarvest,
    int WoodStock,
    int WoodReserve,
    int CitizensAtWork,
    int CitizensAtHome,
    IReadOnlyList<CityStatusSnapshot.ProjectItem> Projects,
    IReadOnlyList<CityStatusSnapshot.BuildingItem> Buildings,
    IReadOnlyList<string> FreeCitizenNames,
    string? HeroName,
    bool IsLaborTime)
{
    public bool IsEmpty => Buildings.Count == 0 && Projects.Count == 0;

    /// <summary>
    /// A worksite in the city-status presentation snapshot. <paramref name="StopCause"/>
    /// keeps the summary honest: a project can sit at 0/180 indefinitely because its
    /// contributor is exhausted, still walking, or waiting on a module, and
    /// without it the chip reported progress without ever saying why.
    /// </summary>
    public sealed record ProjectItem(
        string DisplayName,
        int Progress,
        int RequiredWork,
        int AssignedCount,
        int WorkerCapacity,
        bool Enabled,
        ConstructionStopCause StopCause);

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

    public static CityStatusSnapshot From(CityWorld world)
    {
        var resources = new List<ResourceInventoryItem>();
        foreach (ResourceType resource in System.Enum.GetValues<ResourceType>())
        {
            int total = world.Resources.Total(resource);
            if (total <= 0) continue;
            resources.Add(new ResourceInventoryItem(
                resource,
                total,
                world.Resources.Available(resource)));
        }

        var projects = new List<ProjectItem>();
        foreach (var project in world.Projects.Values)
        {
            projects.Add(new ProjectItem(project.DisplayName, project.Progress, project.RequiredWork,
                project.AssignedCount, project.WorkerCapacity, project.Enabled, project.StopCause));
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
        // The authored first night holds the displayed clock at 05:59 while it
        // runs. The tick itself is never frozen — freezing it would stop
        // construction and make the very milestone the night waits on
        // unreachable — but the player must not watch the sun come up while
        // the spirit is still teaching them to survive the dark.
        // FirstNightState.DisplayedTick has existed and been unit-tested since
        // the night landed; nothing had ever called it.
        return new CityStatusSnapshot(
            world.FirstNight?.DisplayedTick(world.CurrentTick) ?? world.CurrentTick,
            world.Hero is null
                ? null
                : ProfileCatalog.Get(world.Hero.Profile.Lineage).DisplayName,
            resources,
            world.Citizens.Count,
            world.HousingCapacity,
            world.FoodStock, world.MaxFoodStock,
            world.DailyFoodRation, world.FoodHorizonDays,
            world.ProtectedFoodTarget, world.TicksUntilFirstHarvest,
            world.TotalWood, world.TotalWoodReserve,
            atWork, atHome, projects, buildings, freeNames, world.Hero?.Name,
            world.IsLaborTime());
    }
}
