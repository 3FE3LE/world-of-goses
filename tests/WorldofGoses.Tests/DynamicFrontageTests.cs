using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using WorldofGoses.Prototypes;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class DynamicFrontageTests
{
    [Fact]
    public void ParcelCoordinates_MapToContinuousFrontageCoordinates()
    {
        Assert.Equal(new ConstructionRowId(5), ParcelGrid.ConstructionRow(1, 2));
        Assert.Equal(21, ParcelGrid.GlobalFrontageColumn(2, 1));
        Assert.Equal(22, ParcelGrid.GlobalFrontageColumn(2, 1, 1));
    }

    [Fact]
    public void AvailableWindows_AreNotRestrictedToLegacyLotBoundaries()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();

        Assert.Contains(
            world.AvailableConstructionLots(),
            window => window.StartColumn % ParcelGrid.TilesPerStandardLot != 0);
    }

    [Fact]
    public void PlacementSnapshot_ExposesGridCellsAndBlockedWindowsBeforeSelection()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();

        ConstructionPlacementSnapshot snapshot = ConstructionPlacementSnapshot.From(world);

        Assert.NotEmpty(snapshot.Cells);
        Assert.Contains(snapshot.Cells,
            cell => cell.State == FrontageCellState.NaturalResource);
        Assert.Contains(snapshot.Windows, window => window.IsValid);
        Assert.Contains(snapshot.Windows,
            window => window.State == FrontageCellState.NaturalResource);
        Assert.All(snapshot.Windows, window => Assert.Equal(
            world.ConstructionLotState(window.Lot),
            window.State));
    }

    [Fact]
    public void NaturalResource_OccupiesOnlyItsExplicitFrontageCell()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        NaturalResourcePatch patch = world.NaturalResourcePatches.Values.First();
        CityParcel parcel = world.Parcels[patch.ParcelId];
        NaturalResourceUnitPosition position = patch.UnitPositions[0];

        Assert.Equal(
            FrontageCellState.NaturalResource,
            world.FrontageState(
                position.GlobalRow(parcel),
                position.GlobalFrontageColumn(parcel)));
        Assert.Contains(
            Enumerable.Range(0, ParcelGrid.FrontageColumnsPerParcel),
            localColumn => world.FrontageState(
                position.GlobalRow(parcel),
                parcel.LogicalColumn * ParcelGrid.FrontageColumnsPerParcel + localColumn)
                == FrontageCellState.Available);
        ObstacleFootprintTemplate footprint =
            NaturalResourceFootprintCatalog.StandardGroundResource;
        Assert.Equal(ParcelGrid.HalfTilesPerTile, footprint.ReservedArea.Width);
        Assert.Equal(2, footprint.FrontClearance);
    }

    [Fact]
    public void SeededResources_LeaveFounderCenterAndSameParcelConstructionAvailable()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        CityParcel initialParcel = world.Parcels.Values.Single(
            FoundingLayout.IsInitialParcel);
        ConstructionRowId founderRow = ParcelGrid.ConstructionRow(
            initialParcel.LogicalRow,
            FoundingLayout.FounderRowWithinParcel);
        int founderColumn = initialParcel.LogicalColumn
            * ParcelGrid.FrontageColumnsPerParcel
            + FoundingLayout.FounderFrontageColumnWithinParcel;

        Assert.Equal(FrontageCellState.Available, world.FrontageState(
            founderRow,
            founderColumn));
        Assert.Contains(
            world.AvailableConstructionLots(),
            window => world.NaturalResourcePatches.Values.Any(
                patch => patch.ParcelId == window.ParcelId));
        Assert.Contains(
            world.NaturalResourcePatches.Values.GroupBy(patch => patch.ParcelId),
            parcelGroup => parcelGroup.Select(patch => patch.ResourceType).Distinct().Count() > 1);
    }

    [Fact]
    public void ResourceLayout_IsDeterministicAndNotTheLegacyThreeByThreeMatrix()
    {
        CityWorld first = TestHelpers.NewHeroWorld();
        CityWorld second = TestHelpers.NewHeroWorld();
        first.SeedStartingForests();
        second.SeedStartingForests();

        NaturalResourceUnitPosition[] firstPositions = first.NaturalResourcePatches.Values
            .OrderBy(patch => patch.Id)
            .SelectMany(patch => patch.UnitPositions)
            .ToArray();
        NaturalResourceUnitPosition[] secondPositions = second.NaturalResourcePatches.Values
            .OrderBy(patch => patch.Id)
            .SelectMany(patch => patch.UnitPositions)
            .ToArray();

        Assert.Equal(firstPositions, secondPositions);
        Assert.Contains(firstPositions, position =>
            position.FrontageColumnWithinParcel % ParcelGrid.TilesPerStandardLot != 1);
    }

    [Fact]
    public void ResourceLayout_SeedChangesDeterministicScatterWithoutOverlaps()
    {
        NaturalResourceUnitPosition[] first = NaturalResourceLayoutPlanner.TryAllocate(
            5,
            worldSeed: 123,
            patchId: 100,
            System.Array.Empty<NaturalResourceUnitPosition>())!.ToArray();
        NaturalResourceUnitPosition[] second = NaturalResourceLayoutPlanner.TryAllocate(
            5,
            worldSeed: 456,
            patchId: 100,
            System.Array.Empty<NaturalResourceUnitPosition>())!.ToArray();

        Assert.NotEqual(first, second);
        Assert.Equal(5, first.Distinct().Count());
        Assert.True(first.Select(position => position.RowWithinParcel).Distinct().Count() > 1);
        Assert.All(first, position =>
        {
            Assert.InRange(position.RowWithinParcel, 0, ParcelGrid.ConstructionRowsPerParcel - 1);
            Assert.InRange(position.FrontageColumnWithinParcel, 0, ParcelGrid.FrontageColumnsPerParcel - 1);
        });
    }

    [Fact]
    public void Reservation_RoundtripPreservesContinuousGeometry()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        ConstructionLot window = world.AvailableConstructionLots()
            .First(candidate => candidate.StartColumn % ParcelGrid.TilesPerStandardLot != 0);
        world.GatherWood(new BuildingId(100), 4);

        ConstructionAuthorizationResult result = world.TryAuthorizeConstruction(
            ConstructionKind.BasicShelter,
            window);
        Assert.True(result.IsSuccess, result.Outcome.ToString());

        CityWorld restored = CityWorld.FromSave(WorldPersistence.DeserializeFromJson(
            WorldPersistence.SerializeToJson(WorldPersistence.Capture(world))));
        ParcelPlacement placement = restored.ParcelPlacements[result.ProjectId!.Value];

        Assert.Equal(window.RowId, placement.RowId);
        Assert.Equal(window.StartColumn, placement.StartColumn);
        Assert.Equal(window.FrontageColumns, placement.FrontageColumns);
    }

    [Fact]
    public void DirectionalExpansion_PreservesWhichSideConsumedFrontage()
    {
        var reservation = new BuildingReservation(
            new BuildingId(7),
            new ConstructionRowId(2),
            startColumn: 10,
            frontageColumns: 3);

        BuildingReservation left = reservation.ExpandLeft();
        BuildingReservation right = reservation.ExpandRight();

        Assert.Equal((9, 4, 1, 0),
            (left.StartColumn, left.FrontageColumns,
                left.LeftExpansionColumns, left.RightExpansionColumns));
        Assert.Equal((10, 4, 0, 1),
            (right.StartColumn, right.FrontageColumns,
                right.LeftExpansionColumns, right.RightExpansionColumns));
    }

    [Fact]
    public void ProtectedCorridor_BlocksConstructionAndRoundtrips()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        ConstructionLot window = world.AvailableConstructionLots().First();
        int protectedColumn = window.StartColumn;

        CorridorReservation corridor = Assert.IsType<CorridorReservation>(
            world.TryReserveCorridor(window.RowId, protectedColumn));

        Assert.Equal(
            FrontageCellState.ReservedAsCorridor,
            world.FrontageState(window.RowId, protectedColumn));
        Assert.DoesNotContain(
            world.AvailableConstructionLots(),
            candidate => candidate.RowId == window.RowId
                && candidate.StartColumn <= protectedColumn
                && candidate.StartColumn + candidate.FrontageColumns > protectedColumn);

        CityWorld restored = CityWorld.FromSave(WorldPersistence.DeserializeFromJson(
            WorldPersistence.SerializeToJson(WorldPersistence.Capture(world))));
        Assert.Equal(corridor, restored.CorridorReservations[corridor.Id]);
    }

    [Fact]
    public void ConstructionObstacle_UsesSolidFootprintInsteadOfReservedFrontage()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        CityMacroSnapshot.PlotItem plot = CityMacroSnapshot.From(world).Buildings
            .First(item => item.FootprintProfileId
                == BuildingFootprintCatalog.StandardWithSideSetbacksId);

        StreetRoutePlanner.Interval obstacle = MacroStreetLiveView.BuildingObstacleInterval(
            plot,
            totalFrontageColumns: 45,
            tileUnitPx: 30);

        Assert.Equal(60f, obstacle.End - obstacle.Start);
        Assert.Equal(90, plot.FrontageColumns * 30);
    }

    [Fact]
    public void ObstacleInterval_UsesClearancesInsteadOfReservedWidthForAnyAssetUse()
    {
        StreetRoutePlanner.Interval obstacle =
            MacroStreetLiveView.ObstacleIntervalFromClearances(
                reservedStart: 0f,
                reservedWidth: 90f,
                leftClearance: 30f,
                rightClearance: 30f);

        Assert.Equal(new StreetRoutePlanner.Interval(30f, 60f), obstacle);
    }

    [Fact]
    public void V24Migration_ConvertsLegacyLotToThreeFrontageColumns()
    {
        WorldSave save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        save.Version = 24;
        foreach (ParcelPlacementSave placement in save.ParcelPlacements)
        {
            placement.RowId = 0;
            placement.StartColumn = 0;
        }

        WorldSave migrated = WorldPersistence.MigrateV24ToV25(save);

        Assert.All(migrated.ParcelPlacements, placement =>
        {
            Assert.Equal(3, placement.FrontageColumns);
            Assert.Equal(3, placement.DepthRows);
        });
        WorldPersistence.Validate(WorldPersistence.MigrateV29ToV30(
            WorldPersistence.MigrateV28ToV29(
                WorldPersistence.MigrateV27ToV28(
                    WorldPersistence.MigrateV26ToV27(
                        WorldPersistence.MigrateV25ToV26(migrated))))));
    }

    [Fact]
    public void V25Migration_AssignsCompactExplicitResourcePositions()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        WorldSave save = WorldPersistence.Capture(world);
        save.Version = 25;
        foreach (NaturalResourcePatchSave patch in save.NaturalResourcePatches)
        {
            patch.UnitPositions.Clear();
        }

        WorldSave migrated = WorldPersistence.MigrateV29ToV30(
            WorldPersistence.MigrateV28ToV29(
                WorldPersistence.MigrateV27ToV28(
                    WorldPersistence.MigrateV26ToV27(
                        WorldPersistence.MigrateV25ToV26(save)))));

        Assert.All(migrated.NaturalResourcePatches, patch =>
            Assert.Equal(patch.UnitReserves.Count, patch.UnitPositions.Count));
        WorldPersistence.Validate(migrated);
    }
}
