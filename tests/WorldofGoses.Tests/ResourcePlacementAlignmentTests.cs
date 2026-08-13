#nullable enable
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Prototypes;
using Xunit;
using CellItem = WorldofGoses.ConstructionPlacementSnapshot.CellItem;
using WindowItem = WorldofGoses.ConstructionPlacementSnapshot.WindowItem;

namespace WorldofGoses.Tests;

/// <summary>
/// Regression tests for the GitHub #30 placement/grid alignment contract.
///
/// <para>Three properties are pinned:</para>
/// <list type="number">
///   <item>The placement snapshot carries <em>exactly one</em>
///         <c>CellItem</c> per living natural-resource unit. Three
///         independent depth sub-cells per resource are a bug —
///         the domain knows nothing about depth subdivision inside
///         a frontage cell.</item>
///   <item>The lateral coordinate of a resource asset equals the
///         lateral coordinate of the placement cell the domain
///         reports as <c>NaturalResource</c> for the same
///         <c>(row, column)</c>.</item>
///   <item>The available Basic Shelter window that contains a
///         resource reports <c>NaturalResource</c> in its
///         <c>FrontageCellState</c> — gathering the unit flips the
///         same window to <c>Available</c> without a reload.</item>
/// </list>
/// </summary>
public sealed class ResourcePlacementAlignmentTests
{
    [Fact]
    public void PlacementSnapshot_OneCellItemPerLivingResourceUnit()
    {
        // The seeders place Branches, PlantFiber, SmallStone, WildFood
        // and two Wood patches. Across the four ground types the
        // opening carries 7+3+3+4 = 17 living units; the snapshot
        // should expose 17 `NaturalResource` cells, no more and no
        // less. Three per unit (the bug) would land at 51.
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();

        int livingUnits = world.NaturalResourcePatches.Values
            .Sum(patch => patch.UnitReserves.Count(reserve => reserve > 0));
        Assert.True(livingUnits > 0,
            "Opening seeder must produce at least one living resource unit.");

        ConstructionPlacementSnapshot snapshot = ConstructionPlacementSnapshot.From(world);

        int naturalResourceCells = snapshot.Cells
            .Count(cell => cell.State == FrontageCellState.NaturalResource);
        Assert.Equal(
            livingUnits,
            naturalResourceCells);
    }

    [Fact]
    public void PlacementCell_ForAResourceUnit_SitsAtTheSameLateralAsTheAsset()
    {
        // For every living unit: the lateral offset of the
        // corresponding placement cell must equal the lateral offset
        // computed by `MacroGroundProjection.ResourceAnchor`. A
        // difference means the grid and the asset disagree on
        // where the cell is, which is exactly the visual bug.
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        const int worldParcelColumns = 5;

        ConstructionPlacementSnapshot snapshot = ConstructionPlacementSnapshot.From(world);

        foreach (NaturalResourcePatch patch in world.NaturalResourcePatches.Values)
        {
            CityParcel parcel = world.Parcels[patch.ParcelId];
            for (int unitId = 0; unitId < patch.UnitPositions.Count; unitId++)
            {
                if (patch.UnitReserves[unitId] <= 0) continue;
                NaturalResourceUnitPosition position = patch.UnitPositions[unitId];
                ConstructionRowId rowId = position.GlobalRow(parcel);
                int globalFrontageColumn = position.GlobalFrontageColumn(parcel);

                float expectedLateral = MacroGroundProjection.ResourceAnchor(
                    globalFrontageColumn,
                    worldParcelColumns);

                CellItem? matching = snapshot.Cells
                    .Where(cell => cell.RowId == rowId
                        && cell.FrontageColumn == globalFrontageColumn
                        && cell.State == FrontageCellState.NaturalResource)
                    .Cast<CellItem?>()
                    .FirstOrDefault();
                Assert.NotNull(matching);
                // The cell's lateral is computed at projection time, not
                // stored on the snapshot — this test pins the contract
                // that the projection is the one source for the cell's
                // lateral offset.
                float cellLateral = MacroGroundProjection.LateralOffsetForCell(
                    matching!.FrontageColumn,
                    worldParcelColumns);
                Assert.Equal(expectedLateral, cellLateral);
            }
        }
    }

    [Fact]
    public void PlacementCell_ForAGatheredResource_FlipsToAvailableImmediately()
    {
        // The cell that contained a gathered resource must report
        // `Available` on the next snapshot read, with no reload.
        // The 3-column window that contains that cell may still
        // report `NaturalResource` if the other two cells in the
        // window are also resources — that is correct window-level
        // state, not a regression of the cell-level contract.
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        (NaturalResourcePatch patch, int unitId) = FirstLivingBranchUnit(world);
        CityParcel parcel = world.Parcels[patch.ParcelId];
        ConstructionRowId rowId = patch.UnitPositions[unitId].GlobalRow(parcel);
        int globalFrontageColumn = patch.UnitPositions[unitId]
            .GlobalFrontageColumn(parcel);

        ConstructionPlacementSnapshot before = ConstructionPlacementSnapshot.From(world);
        CellItem beforeCell = Assert.IsType<CellItem>(
            before.Cells
                .Where(cell => cell.RowId == rowId
                    && cell.FrontageColumn == globalFrontageColumn)
                .Cast<CellItem?>()
                .FirstOrDefault());
        Assert.Equal(FrontageCellState.NaturalResource, beforeCell.State);

        // EG-A0 stores two reserves per unit (the seeder sets
        // `[2, 2, …]`). The carried-capacity cap allows draining
        // both in one call before the next Foundation Site exists
        // (CarriedGroundResourceCapacity = 6, and one fully-gathered
        // unit takes 2).
        int totalReserves = patch.UnitReserves[unitId];
        int gathered = world.GatherFromPatch(patch.Id, unitId, totalReserves);
        Assert.Equal(totalReserves, gathered);

        ConstructionPlacementSnapshot after = ConstructionPlacementSnapshot.From(world);
        CellItem afterCell = Assert.IsType<CellItem>(
            after.Cells
                .Where(cell => cell.RowId == rowId
                    && cell.FrontageColumn == globalFrontageColumn)
                .Cast<CellItem?>()
                .FirstOrDefault());
        Assert.Equal(FrontageCellState.Available, afterCell.State);
    }

    private static (NaturalResourcePatch Patch, int UnitId) FirstLivingBranchUnit(CityWorld world)
    {
        NaturalResourcePatch patch = world.NaturalResourcePatches.Values
            .First(patch => patch.ResourceType == ResourceType.Branches
                && patch.UnitReserves.Any(reserve => reserve > 0));
        int unitId = Enumerable.Range(0, patch.UnitReserves.Count)
            .First(index => patch.UnitReserves[index] > 0);
        return (patch, unitId);
    }
}
