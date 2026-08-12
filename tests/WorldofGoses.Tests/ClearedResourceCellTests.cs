using System;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// A fully gathered resource unit is free ground: its sprite is hidden and
/// CityWorld reports the cell as available. The save validator used to
/// disagree, so building on a cleared cell was accepted by every placement
/// gate and then made every subsequent save throw — silently, because
/// CityWorldController.TrySaveNow swallows the failure into a warning.
/// </summary>
public sealed class ClearedResourceCellTests
{
    [Fact]
    public void ClearedResourceCell_AcceptsConstruction_AndTheSaveStillValidates()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        (NaturalResourcePatch patch, ConstructionRowId row, int column) = FirstUnitCell(world);

        Assert.Equal(FrontageCellState.NaturalResource, world.FrontageState(row, column));
        ClearUnitZero(world, patch);
        Assert.Equal(0, patch.UnitReserves[0]);
        Assert.Equal(FrontageCellState.Available, world.FrontageState(row, column));

        ConstructionLot lot = LotCovering(world, row, column);
        ConstructionAuthorizationResult authorization =
            world.TryAuthorizeConstruction(ConstructionKind.BasicShelter, lot);
        Assert.True(authorization.IsSuccess, authorization.Outcome.ToString());

        WorldSave save = WorldPersistence.Capture(world);
        WorldPersistence.Validate(save);

        CityWorld restored = WorldPersistence.FromSave(save);
        ParcelPlacement placement = restored.ParcelPlacements.Values.Single();
        Assert.Equal(row, placement.RowId);
        Assert.Equal(lot.StartColumn, placement.StartColumn);
    }

    [Fact]
    public void StockedResourceCell_UnderAPlacement_IsStillRejected()
    {
        WorldSave save = SaveWithConstructionOverAClearedCell(out int patchId);
        NaturalResourcePatchSave patch =
            save.NaturalResourcePatches.Single(candidate => candidate.Id == patchId);

        // The resource is back on a cell a building now occupies. Relaxing the
        // validator must not relax this case too.
        patch.UnitReserves[0] = 5;

        InvalidOperationException error =
            Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(save));
        Assert.Equal(
            "Save contains a construction reservation over a natural resource.",
            error.Message);
    }

    [Fact]
    public void DuplicateUnitPositions_AreStillRejected_EvenWhenDepleted()
    {
        WorldSave save = SaveWithConstructionOverAClearedCell(out int patchId);
        NaturalResourcePatchSave patch =
            save.NaturalResourcePatches.Single(candidate => candidate.Id == patchId);
        Assert.True(patch.UnitPositions.Count > 1);

        // The reserve filter guards only the build-blocking set. The authored
        // layout's uniqueness check must keep seeing every position, or a
        // corrupt patch passes validation.
        patch.UnitReserves[0] = 0;
        patch.UnitReserves[1] = 0;
        patch.UnitPositions[1] = new NaturalResourceUnitPositionSave
        {
            RowWithinParcel = patch.UnitPositions[0].RowWithinParcel,
            FrontageColumnWithinParcel = patch.UnitPositions[0].FrontageColumnWithinParcel,
        };

        InvalidOperationException error =
            Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(save));
        Assert.Equal(
            "Save contains overlapping or invalid natural-resource positions.",
            error.Message);
    }

    [Fact]
    public void ReturnFoundingCargo_NeverRestocksACellABuildingOccupies()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();

        // ReturnFoundingCargo only moves the founding-era ground resources, so
        // this needs a Branches patch rather than a Forest.
        NaturalResourcePatch patch = world.NaturalResourcePatches.Values.First(
            candidate => candidate.ResourceType == ResourceType.Branches);
        CityParcel parcel = world.Parcels[patch.ParcelId];
        NaturalResourceUnitPosition position = patch.UnitPositions[0];
        ConstructionRowId row = position.GlobalRow(parcel);
        int column = position.GlobalFrontageColumn(parcel);

        // Carry away the target node so it becomes returnable cargo. The other
        // nodes under the shelter's window only need to be gone; gathering them
        // too would hit CarriedGroundResourceCapacity and leave one standing.
        int carried = world.GatherFromPatch(patch.Id, 0, patch.UnitReserves[0]);
        Assert.True(carried > 0);
        Assert.Equal(0, patch.UnitReserves[0]);
        for (int offset = 0; offset < BuildingReservation.MinimumFrontageColumns; offset++)
        {
            ClearCellWithoutCarrying(world, row, column + offset);
            Assert.Equal(
                FrontageCellState.Available,
                world.FrontageState(row, column + offset));
        }

        world.Resources.DepositToCityInventory(ResourceType.Wood, 8);
        ConstructionAuthorizationResult authorization = world.TryAuthorizeConstruction(
            ConstructionKind.BasicShelter,
            LotCovering(world, row, column));
        Assert.True(authorization.IsSuccess, authorization.Outcome.ToString());
        Assert.Equal(
            FrontageCellState.ReservedByBuilding,
            world.FrontageState(row, column));
        Assert.Equal(carried, world.ReturnableFoundingCargoCount());

        int returned = world.ReturnFoundingCargo();

        // Nothing is refused and nothing is destroyed...
        Assert.Equal(carried, returned);
        Assert.Equal(0, world.ReturnableFoundingCargoCount());
        // ...but the built-over unit stays cleared: the cargo went elsewhere.
        Assert.Equal(0, patch.UnitReserves[0]);
        Assert.Equal(
            FrontageCellState.ReservedByBuilding,
            world.FrontageState(row, column));
        WorldPersistence.Validate(WorldPersistence.Capture(world));
    }

    private static WorldSave SaveWithConstructionOverAClearedCell(out int patchId)
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        (NaturalResourcePatch patch, ConstructionRowId row, int column) = FirstUnitCell(world);
        ClearUnitZero(world, patch);
        ConstructionAuthorizationResult authorization = world.TryAuthorizeConstruction(
            ConstructionKind.BasicShelter,
            LotCovering(world, row, column));
        Assert.True(authorization.IsSuccess, authorization.Outcome.ToString());
        patchId = patch.Id;
        return WorldPersistence.Capture(world);
    }

    /// <summary>
    /// Empties whichever resource unit sits on the given cell without routing
    /// anything through the city inventory, so the carrying cap cannot leave a
    /// node standing inside the window under test.
    /// </summary>
    private static void ClearCellWithoutCarrying(
        CityWorld world,
        ConstructionRowId row,
        int column)
    {
        foreach (NaturalResourcePatch patch in world.NaturalResourcePatches.Values)
        {
            if (!world.Parcels.TryGetValue(patch.ParcelId, out CityParcel? parcel)) continue;
            for (int unitId = 0; unitId < patch.UnitPositions.Count; unitId++)
            {
                NaturalResourceUnitPosition position = patch.UnitPositions[unitId];
                if (position.GlobalRow(parcel) != row
                    || position.GlobalFrontageColumn(parcel) != column
                    || patch.UnitReserves[unitId] <= 0)
                {
                    continue;
                }
                patch.GatherUnit(unitId, patch.UnitReserves[unitId]);
                return;
            }
        }
    }

    /// <summary>
    /// Fully gathers the patch's first unit through the real gather path, so the
    /// cell becomes free ground *and* the wood reaches the city inventory —
    /// otherwise the founding shelter fails on MissingMaterials.
    /// </summary>
    private static void ClearUnitZero(CityWorld world, NaturalResourcePatch patch)
    {
        int gathered = world.GatherWood(new BuildingId(patch.Id), 0, patch.UnitReserves[0]);
        Assert.True(gathered > 0);
        Assert.Equal(0, patch.UnitReserves[0]);
    }

    private static (NaturalResourcePatch Patch, ConstructionRowId Row, int Column) FirstUnitCell(
        CityWorld world)
    {
        NaturalResourcePatch patch = world.NaturalResourcePatches.Values.First();
        CityParcel parcel = world.Parcels[patch.ParcelId];
        NaturalResourceUnitPosition position = patch.UnitPositions[0];
        return (patch, position.GlobalRow(parcel), position.GlobalFrontageColumn(parcel));
    }

    private static ConstructionLot LotCovering(
        CityWorld world,
        ConstructionRowId row,
        int column) =>
        world.AvailableConstructionLots().First(candidate =>
            candidate.RowId == row
            && column >= candidate.StartColumn
            && column < candidate.StartColumn + candidate.FrontageColumns);
}
