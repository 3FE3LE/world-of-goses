#nullable enable
using System.Collections.Generic;
using WorldofGoses.Domain;

namespace WorldofGoses;

public sealed record CityMacroSnapshot(
    int CitizenCount,
    CityMacroSnapshot.HeroVisual? Hero,
    IReadOnlyList<CityMacroSnapshot.CitizenItem> Citizens,
    IReadOnlyList<CityMacroSnapshot.ParcelItem> Parcels,
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

    public sealed record ParcelItem(
        ParcelId Id,
        int LogicalColumn,
        int LogicalRow,
        ParcelTerritoryState TerritoryState);

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
        bool IsReturningHome,
        int? TransitStartedAtTick,
        // The tick the domain will end this journey on. The view paces its
        // route against this pair so the drawn arrival and the fact coincide;
        // it never decides either of them.
        int? TransitArrivalTick,
        CitizenRoutineActivity Activity,
        CitizenRoutineBlockReason BlockReason,
        WoundSeverity? WoundSeverity,
        int WoundRecoveryTicksRemaining,
        bool IsReceivingWoundTreatment,
        int CurrentStamina,
        int MaxStamina,
        BuildingId? LastVisitedResourceBuildingId,
        int? LastVisitedResourceUnitId,
        int? LastVisitedResourcePositionIndex,
        LineageId Lineage,
        GenderId Gender,
        AppearanceVariantId Appearance);

    public sealed record PlotItem(
        BuildingId Id,
        BuildingKind Kind,
        ResourceType? GroundResourceType,
        CultivationPlotState? CultivationState,
        int? PlantedTick,
        int? ReadyAtTick,
        string DisplayName,
        bool IsUnderConstruction,
        bool Enabled,
        int Stock,
        int StorageCapacity,
        int WoodReserve,
        IReadOnlyList<int> WoodUnitReserves,
        IReadOnlyList<NaturalResourceUnitPosition> ResourceUnitPositions,
        int Progress,
        int RequiredWork,
        ParcelId? ParcelId,
        int ParcelColumn,
        int ParcelRow,
        int LotColumn,
        int LotRow,
        int LotWidth,
        int LotHeight,
        int RowId,
        int StartColumn,
        int FrontageColumns,
        int DepthRows,
        int StructuralStartHalfColumn,
        int StructuralFrontageHalfColumns,
        string? FootprintProfileId,
        BuildingOrientation Orientation)
    {
        public bool IsStorageFull => StorageCapacity > 0 && Stock >= StorageCapacity;
    }

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
            ObstacleFootprintTemplate? footprint = placement is null
                ? null
                : BuildingFootprintCatalog.Get(placement.FootprintProfileId);
            buildings.Add(new PlotItem(
                building.Id,
                building.Kind,
                GroundResourceType: null,
                CultivationState: null,
                PlantedTick: null,
                ReadyAtTick: null,
                building.DisplayName,
                IsUnderConstruction: false,
                Enabled: enabled,
                Stock: building.Stock,
                StorageCapacity: building.StorageCapacity,
                WoodReserve: building.WoodReserve,
                WoodUnitReserves: new List<int>(building.WoodUnitReserves),
                ResourceUnitPositions: System.Array.Empty<NaturalResourceUnitPosition>(),
                Progress: 0,
                RequiredWork: 0,
                placement?.ParcelId,
                ParcelColumn: parcel?.LogicalColumn ?? 0,
                ParcelRow: parcel?.LogicalRow ?? 0,
                LotColumn: placement?.LotColumn ?? 0,
                LotRow: placement?.LotRow ?? 0,
                LotWidth: placement?.LotWidth ?? 1,
                LotHeight: placement?.LotHeight ?? 1,
                RowId: placement?.RowId.Value ?? 0,
                StartColumn: placement?.StartColumn ?? 0,
                FrontageColumns: placement?.FrontageColumns ?? BuildingReservation.MinimumFrontageColumns,
                DepthRows: placement?.DepthRows ?? BuildingReservation.RequiredDepthRows,
                StructuralStartHalfColumn: placement is null || footprint is null
                    ? 0
                    : checked((placement.StartColumn + placement.LeftExpansionColumns) * 2
                        + footprint.LeftClearance),
                StructuralFrontageHalfColumns: footprint?.SolidArea.Width ?? 0,
                FootprintProfileId: placement?.FootprintProfileId,
                Orientation: placement?.Orientation ?? BuildingOrientation.South));
        }
        foreach (NaturalResourcePatch patch in world.NaturalResourcePatches.Values)
        {
            world.Parcels.TryGetValue(patch.ParcelId, out CityParcel? parcel);
            buildings.Add(new PlotItem(
                new BuildingId(patch.Id),
                BuildingKind.Forest,
                patch.ResourceType,
                CultivationState: null,
                PlantedTick: null,
                ReadyAtTick: null,
                patch.ResourceType.ToString(),
                IsUnderConstruction: false,
                Enabled: patch.TotalReserve > 0,
                Stock: 0,
                StorageCapacity: 0,
                WoodReserve: patch.TotalReserve,
                WoodUnitReserves: new List<int>(patch.UnitReserves),
                ResourceUnitPositions: new List<NaturalResourceUnitPosition>(patch.UnitPositions),
                Progress: 0,
                RequiredWork: 0,
                ParcelId: patch.ParcelId,
                ParcelColumn: parcel?.LogicalColumn ?? 0,
                ParcelRow: parcel?.LogicalRow ?? 0,
                LotColumn: 0,
                LotRow: 0,
                LotWidth: 1,
                LotHeight: 1,
                RowId: 0,
                StartColumn: 0,
                FrontageColumns: BuildingReservation.MinimumFrontageColumns,
                DepthRows: BuildingReservation.RequiredDepthRows,
                StructuralStartHalfColumn: 0,
                StructuralFrontageHalfColumns: 0,
                FootprintProfileId: null,
                Orientation: BuildingOrientation.South));
        }
        foreach (CultivationSite site in world.CultivationSites.Values)
        {
            world.ParcelPlacements.TryGetValue(site.Id, out ParcelPlacement? placement);
            CityParcel? parcel = placement is not null
                && world.Parcels.TryGetValue(placement.ParcelId, out CityParcel? resolved)
                    ? resolved
                    : null;
            ObstacleFootprintTemplate? footprint = placement is null
                ? null
                : BuildingFootprintCatalog.Get(placement.FootprintProfileId);
            buildings.Add(new PlotItem(
                site.Id,
                BuildingKind.CultivationSite,
                GroundResourceType: null,
                site.State,
                site.PlantedTick,
                site.ReadyAtTick,
                "Cultivation Site",
                IsUnderConstruction: false,
                Enabled: site.State is CultivationPlotState.Prepared
                    or CultivationPlotState.Ready,
                Stock: 0,
                StorageCapacity: 0,
                WoodReserve: 0,
                WoodUnitReserves: System.Array.Empty<int>(),
                ResourceUnitPositions: System.Array.Empty<NaturalResourceUnitPosition>(),
                Progress: 0,
                RequiredWork: 0,
                placement?.ParcelId,
                ParcelColumn: parcel?.LogicalColumn ?? 0,
                ParcelRow: parcel?.LogicalRow ?? 0,
                LotColumn: placement?.LotColumn ?? 0,
                LotRow: placement?.LotRow ?? 0,
                LotWidth: placement?.LotWidth ?? 1,
                LotHeight: placement?.LotHeight ?? 1,
                RowId: placement?.RowId.Value ?? 0,
                StartColumn: placement?.StartColumn ?? 0,
                FrontageColumns: placement?.FrontageColumns ?? BuildingReservation.MinimumFrontageColumns,
                DepthRows: placement?.DepthRows ?? BuildingReservation.RequiredDepthRows,
                StructuralStartHalfColumn: placement is null || footprint is null
                    ? 0
                    : checked((placement.StartColumn + placement.LeftExpansionColumns) * 2
                        + footprint.LeftClearance),
                StructuralFrontageHalfColumns: footprint?.SolidArea.Width ?? 0,
                FootprintProfileId: placement?.FootprintProfileId,
                Orientation: placement?.Orientation ?? BuildingOrientation.South));
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
            ObstacleFootprintTemplate? footprint = placement is null
                ? null
                : BuildingFootprintCatalog.Get(placement.FootprintProfileId);
            projects.Add(new PlotItem(
                project.Id,
                project.ResultingKind,
                GroundResourceType: null,
                CultivationState: null,
                PlantedTick: null,
                ReadyAtTick: null,
                project.DisplayName,
                IsUnderConstruction: true,
                project.Enabled,
                Stock: 0,
                StorageCapacity: 0,
                WoodReserve: 0,
                WoodUnitReserves: System.Array.Empty<int>(),
                ResourceUnitPositions: System.Array.Empty<NaturalResourceUnitPosition>(),
                project.Progress,
                project.RequiredWork,
                placement?.ParcelId,
                ParcelColumn: parcel?.LogicalColumn ?? 0,
                ParcelRow: parcel?.LogicalRow ?? 0,
                LotColumn: placement?.LotColumn ?? 0,
                LotRow: placement?.LotRow ?? 0,
                LotWidth: placement?.LotWidth ?? 1,
                LotHeight: placement?.LotHeight ?? 1,
                RowId: placement?.RowId.Value ?? 0,
                StartColumn: placement?.StartColumn ?? 0,
                FrontageColumns: placement?.FrontageColumns ?? BuildingReservation.MinimumFrontageColumns,
                DepthRows: placement?.DepthRows ?? BuildingReservation.RequiredDepthRows,
                StructuralStartHalfColumn: placement is null || footprint is null
                    ? 0
                    : checked((placement.StartColumn + placement.LeftExpansionColumns) * 2
                        + footprint.LeftClearance),
                StructuralFrontageHalfColumns: footprint?.SolidArea.Width ?? 0,
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
            CitizenRoutineSnapshot routine = world.GetCitizenRoutine(resident.Id)!;
            citizens.Add(new CitizenItem(
                resident.Id,
                resident.Name,
                resident.IsHero,
                resident.IsAvailable,
                isOnExpedition,
                resident.CurrentAssignment,
                resident.CurrentLocation,
                resident.IsReturningHome,
                resident.TransitStartedAtTick,
                resident.TravelArrivalTick,
                routine.Activity,
                routine.BlockReason,
                resident.Wound?.Severity,
                resident.Wound?.RecoveryTicksRemaining ?? 0,
                resident.Commitment.Kind == CitizenCommitmentKind.Recovery,
                resident.CurrentStamina,
                resident.MaxStamina,
                resident.LastVisitedResourceBuildingId,
                resident.LastVisitedResourceUnitId,
                resident.LastVisitedResourcePositionIndex,
                resident.Profile.Lineage,
                resident.Profile.Gender,
                resident.AppearanceVariant));
        }

        var parcels = new List<ParcelItem>();
        foreach (CityParcel parcel in world.Parcels.Values)
        {
            // Persisted frontier parcels remain in old saves, but expansion is
            // suspended until its visual language is designed. Do not render a
            // dark empty appendage that looks like broken terrain.
            if (!parcel.IsUnlocked) continue;
            parcels.Add(new ParcelItem(
                parcel.Id,
                parcel.LogicalColumn,
                parcel.LogicalRow,
                parcel.TerritoryState));
        }

        return new CityMacroSnapshot(
            world.Citizens.Count,
            hero,
            citizens,
            parcels,
            buildings,
            projects,
            new List<WorldEvent>(world.Log.Events));
    }
}
