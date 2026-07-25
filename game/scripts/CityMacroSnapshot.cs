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
    public int CivilBuildingCount
    {
        get
        {
            int count = 0;
            foreach (var item in Buildings)
            {
                if (item.Kind != BuildingKind.Forest) count++;
            }
            return count;
        }
    }

    public sealed record HeroVisual(CitizenId Id, LineageId Lineage, GenderId Gender, AppearanceVariantId Appearance);

    /// <summary>
    /// Citizen summary used by the macro view. Carries the visible
    /// state so the UI can render a status icon next to the name
    /// without re-querying the domain.
    /// </summary>
    public sealed record CitizenItem(
        CitizenId Id,
        string Name,
        bool IsHero,
        bool IsAvailable,
        bool IsOnExpedition,
        BuildingId? CurrentAssignment,
        CitizenLocation Location,
        int CurrentStamina,
        int MaxStamina,
        BuildingId? LastVisitedResourceBuildingId,
        int? LastVisitedResourceUnitId,
        int? LastVisitedResourcePositionIndex);

    public sealed record PlotItem(
        BuildingId Id,
        BuildingKind Kind,
        string DisplayName,
        bool IsUnderConstruction,
        bool Enabled,
        int WoodReserve,
        IReadOnlyList<int> WoodUnitReserves,
        int TicksUntilRegeneration,
        int Progress,
        int RequiredWork,
        ParcelId? ParcelId,
        int ParcelColumn,
        int ParcelRow,
        int LotColumn,
        int LotRow,
        int LotWidth,
        int LotHeight,
        string? FootprintProfileId,
        BuildingOrientation Orientation);

    public static CityMacroSnapshot From(CityWorld world)
    {
        var buildings = new List<PlotItem>();
        foreach (var building in world.Buildings.Values)
        {
            if (building.Kind == Domain.BuildingKind.Forest) continue;
            world.ParcelPlacements.TryGetValue(
                building.Id,
                out ParcelPlacement? placement);
            CityParcel? parcel = placement is not null
                && world.Parcels.TryGetValue(placement.ParcelId, out CityParcel? resolved)
                    ? resolved
                    : null;
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
                WoodReserve: building.WoodReserve,
                WoodUnitReserves: new List<int>(building.WoodUnitReserves),
                TicksUntilRegeneration: 0,
                Progress: 0,
                RequiredWork: 0,
                placement?.ParcelId,
                ParcelColumn: parcel?.LogicalColumn ?? 0,
                ParcelRow: parcel?.LogicalRow ?? 0,
                LotColumn: placement?.LotColumn ?? 0,
                LotRow: placement?.LotRow ?? 0,
                LotWidth: placement?.LotWidth ?? 1,
                LotHeight: placement?.LotHeight ?? 1,
                FootprintProfileId: placement?.FootprintProfileId,
                Orientation: placement?.Orientation ?? BuildingOrientation.South));
        }
        foreach (NaturalResourcePatch patch in world.NaturalResourcePatches.Values)
        {
            if (patch.ResourceType != ResourceType.Wood) continue;
            world.Parcels.TryGetValue(patch.ParcelId, out CityParcel? parcel);
            buildings.Add(new PlotItem(
                new BuildingId(patch.Id),
                BuildingKind.Forest,
                "Trees",
                IsUnderConstruction: false,
                Enabled: patch.TotalReserve > 0,
                WoodReserve: patch.TotalReserve,
                WoodUnitReserves: new List<int>(patch.UnitReserves),
                TicksUntilRegeneration:
                    GameClock.TicksPerInGameDay
                    - world.CurrentTick % GameClock.TicksPerInGameDay,
                Progress: 0,
                RequiredWork: 0,
                ParcelId: patch.ParcelId,
                ParcelColumn: parcel?.LogicalColumn ?? 0,
                ParcelRow: parcel?.LogicalRow ?? 0,
                LotColumn: 0,
                LotRow: 0,
                LotWidth: 1,
                LotHeight: 1,
                FootprintProfileId: null,
                Orientation: BuildingOrientation.South));
        }

        var projects = new List<PlotItem>();
        foreach (var project in world.Projects.Values)
        {
            world.ParcelPlacements.TryGetValue(
                project.Id,
                out ParcelPlacement? placement);
            CityParcel? parcel = placement is not null
                && world.Parcels.TryGetValue(placement.ParcelId, out CityParcel? resolved)
                    ? resolved
                    : null;
            projects.Add(new PlotItem(
                project.Id,
                project.ResultingKind,
                project.DisplayName,
                IsUnderConstruction: true,
                project.Enabled,
                WoodReserve: 0,
                WoodUnitReserves: System.Array.Empty<int>(),
                TicksUntilRegeneration: 0,
                project.Progress,
                project.RequiredWork,
                placement?.ParcelId,
                ParcelColumn: parcel?.LogicalColumn ?? 0,
                ParcelRow: parcel?.LogicalRow ?? 0,
                LotColumn: placement?.LotColumn ?? 0,
                LotRow: placement?.LotRow ?? 0,
                LotWidth: placement?.LotWidth ?? 1,
                LotHeight: placement?.LotHeight ?? 1,
                FootprintProfileId: placement?.FootprintProfileId,
                Orientation: placement?.Orientation ?? BuildingOrientation.South));
        }

        HeroVisual? hero = world.Hero is { } citizen
            ? new HeroVisual(citizen.Id, citizen.Profile.Lineage, citizen.Profile.Gender, citizen.AppearanceVariant)
            : null;

        var citizens = new List<CitizenItem>();
        foreach (var resident in world.Citizens.Values)
        {
            bool isOnExpedition = world.IsCitizenOnActiveExpedition(resident.Id);
            citizens.Add(new CitizenItem(
                resident.Id,
                resident.Name,
                resident.IsHero,
                !resident.CurrentAssignment.HasValue && !isOnExpedition,
                isOnExpedition,
                resident.CurrentAssignment,
                resident.CurrentLocation,
                resident.CurrentStamina,
                resident.MaxStamina,
                resident.LastVisitedResourceBuildingId,
                resident.LastVisitedResourceUnitId,
                resident.LastVisitedResourcePositionIndex));
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
