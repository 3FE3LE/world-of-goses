#nullable enable
using System.Collections.Generic;
using WorldofGoses.Domain;

namespace WorldofGoses;

public sealed record CityMacroSnapshot(
    int CitizenCount,
    CityMacroSnapshot.HeroVisual? Hero,
    IReadOnlyList<CityMacroSnapshot.CitizenItem> Citizens,
    IReadOnlyList<CityMacroSnapshot.PlotItem> Buildings,
    IReadOnlyList<CityMacroSnapshot.PlotItem> Projects,
    IReadOnlyList<WorldEvent> Events)
{
    public sealed record HeroVisual(LineageId Lineage, GenderId Gender);

    public sealed record CitizenItem(string Name, bool IsAvailable);

    public sealed record PlotItem(
        BuildingId Id,
        BuildingKind Kind,
        string DisplayName,
        bool IsUnderConstruction,
        int Progress,
        int RequiredWork);

    public static CityMacroSnapshot From(CityWorld world)
    {
        var buildings = new List<PlotItem>();
        foreach (var building in world.Buildings.Values)
        {
            buildings.Add(new PlotItem(
                building.Id,
                building.Kind,
                building.DisplayName,
                IsUnderConstruction: false,
                Progress: 0,
                RequiredWork: 0));
        }

        var projects = new List<PlotItem>();
        foreach (var project in world.Projects.Values)
        {
            projects.Add(new PlotItem(
                project.Id,
                project.ResultingKind,
                project.DisplayName,
                IsUnderConstruction: true,
                project.Progress,
                project.RequiredWork));
        }

        HeroVisual? hero = world.Hero is { } citizen
            ? new HeroVisual(citizen.Profile.Lineage, citizen.Profile.Gender)
            : null;

        var citizens = new List<CitizenItem>();
        foreach (var resident in world.Citizens.Values)
        {
            citizens.Add(new CitizenItem(
                resident.Name,
                !resident.CurrentAssignment.HasValue));
        }

        return new CityMacroSnapshot(
            world.Citizens.Count,
            hero,
            citizens,
            buildings,
            projects,
            new List<WorldEvent>(world.Log.Events));
    }
}
