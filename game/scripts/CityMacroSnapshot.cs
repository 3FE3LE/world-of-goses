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
    public sealed record HeroVisual(CitizenId Id, LineageId Lineage, GenderId Gender);

    /// <summary>
    /// Citizen summary used by the macro view. Carries the visible
    /// state so the UI can render a status icon next to the name
    /// without re-querying the domain.
    /// </summary>
    public sealed record CitizenItem(
        string Name,
        bool IsAvailable,
        CitizenLocation Location,
        int CurrentStamina,
        int MaxStamina);

    public sealed record PlotItem(
        BuildingId Id,
        BuildingKind Kind,
        string DisplayName,
        bool IsUnderConstruction,
        bool Enabled,
        int Progress,
        int RequiredWork);

    public static CityMacroSnapshot From(CityWorld world)
    {
        var buildings = new List<PlotItem>();
        foreach (var building in world.Buildings.Values)
        {
            // Forests are gatherable only while they still have wood in
            // their reserve. Other buildings stay enabled in the
            // snapshot regardless of stock; construction projects use
            // the same field for pause state (see below).
            bool enabled = building.Kind == Domain.BuildingKind.Forest
                ? building.WoodReserve > 0
                : true;
            buildings.Add(new PlotItem(
                building.Id,
                building.Kind,
                building.DisplayName,
                IsUnderConstruction: false,
                Enabled: enabled,
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
                project.Enabled,
                project.Progress,
                project.RequiredWork));
        }

        HeroVisual? hero = world.Hero is { } citizen
            ? new HeroVisual(citizen.Id, citizen.Profile.Lineage, citizen.Profile.Gender)
            : null;

        var citizens = new List<CitizenItem>();
        foreach (var resident in world.Citizens.Values)
        {
            citizens.Add(new CitizenItem(
                resident.Name,
                !resident.CurrentAssignment.HasValue,
                resident.CurrentLocation,
                resident.CurrentStamina,
                resident.MaxStamina));
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
