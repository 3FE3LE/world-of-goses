using System;
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class ParcelPlacementPersistenceTests
{
    [Fact]
    public void AvailableLots_ExcludeOnlyLotsWithLiveNaturalResourceUnits()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();

        IReadOnlyList<ConstructionLot> lots = world.AvailableConstructionLots();

        Assert.NotEmpty(lots);
        Assert.DoesNotContain(
            lots,
            lot => lot.ParcelId == new ParcelId(1)
                && lot.LotColumn == 0
                && lot.LotRow == 0);
        Assert.Contains(
            lots,
            lot => lot.ParcelId == new ParcelId(1)
                && lot.LotColumn == 2
                && lot.LotRow == 2);
    }

    [Fact]
    public void AuthorizedProject_UsesExplicitPlayerSelectedLot()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 1);
        ConstructionLot selected = world.AvailableConstructionLots().Last();

        ConstructionAuthorizationResult result = world.TryAuthorizeConstruction(
            ConstructionKind.BasicShelter,
            selected);

        Assert.True(result.IsSuccess);
        ParcelPlacement placement = world.ParcelPlacements[result.ProjectId!.Value];
        Assert.Equal(selected.ParcelId, placement.ParcelId);
        Assert.Equal(selected.LotColumn, placement.LotColumn);
        Assert.Equal(selected.LotRow, placement.LotRow);
    }

    [Fact]
    public void AuthorizedProject_ReservesFirstAvailableStandardLot()
    {
        CityWorld world = TestHelpers.NewConstructionWorld();
        ConstructionProject project = world.Projects.Values.Single();
        ParcelPlacement placement = world.ParcelPlacements[project.Id];

        Assert.Equal(new ParcelId(1), placement.ParcelId);
        Assert.Equal(2, placement.LotColumn);
        Assert.Equal(2, placement.LotRow);
        Assert.Equal(BuildingFootprintCatalog.StandardWithSideSetbacksId,
            placement.FootprintProfileId);
    }

    [Fact]
    public void CompletedProject_KeepsItsPlacementAsBuilding()
    {
        CityWorld world = TestHelpers.NewConstructionWorld();
        ConstructionProject project = world.Projects.Values.Single();
        ParcelPlacement before = world.ParcelPlacements[project.Id];

        Building completed = TestHelpers.FastForwardToCompletion(world, project.Id);

        ParcelPlacement after = world.ParcelPlacements[completed.Id];
        Assert.Same(before, after);
        Assert.Empty(world.Projects);
    }

    [Fact]
    public void CancelledProject_ReleasesItsLot()
    {
        CityWorld world = TestHelpers.NewConstructionWorld();
        BuildingId projectId = world.Projects.Values.Single().Id;

        Assert.True(world.CancelProject(projectId));

        Assert.False(world.ParcelPlacements.ContainsKey(projectId));
    }

    [Fact]
    public void Roundtrip_PreservesPlacementGeometry()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        WorldSave save = WorldPersistence.Capture(world);

        CityWorld restored = CityWorld.FromSave(
            WorldPersistence.DeserializeFromJson(
                WorldPersistence.SerializeToJson(save)));

        Assert.Equal(world.ParcelPlacements.Count, restored.ParcelPlacements.Count);
        foreach (ParcelPlacement expected in world.ParcelPlacements.Values)
        {
            Assert.Equal(expected.ParcelId, restored.ParcelPlacements[expected.EntityId].ParcelId);
            Assert.Equal(expected.LotColumn,
                restored.ParcelPlacements[expected.EntityId].LotColumn);
            Assert.Equal(expected.LotRow,
                restored.ParcelPlacements[expected.EntityId].LotRow);
            Assert.Equal(expected.FootprintProfileId,
                restored.ParcelPlacements[expected.EntityId].FootprintProfileId);
        }
    }

    [Fact]
    public void Validate_RejectsOverlappingPlacements()
    {
        WorldSave save = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        ParcelPlacementSave first = save.ParcelPlacements[0];
        ParcelPlacementSave second = save.ParcelPlacements[1];
        second.ParcelId = first.ParcelId;
        second.LotColumn = first.LotColumn;
        second.LotRow = first.LotRow;

        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    [Fact]
    public void MigrateV8ToV9_AssignsExistingBuildingsDeterministically()
    {
        WorldSave legacy = WorldPersistence.Capture(TestHelpers.NewProductionWorld());
        legacy.Version = 8;
        legacy.ParcelPlacements.Clear();

        WorldSave migrated = WorldPersistence.MigrateV8ToV9(legacy);

        Assert.Equal(9, migrated.Version);
        Assert.Equal(3, migrated.ParcelPlacements.Count);
        Assert.Collection(
            migrated.ParcelPlacements,
            first => Assert.Equal((0, 0), (first.LotColumn, first.LotRow)),
            second => Assert.Equal((1, 0), (second.LotColumn, second.LotRow)),
            third => Assert.Equal((2, 0), (third.LotColumn, third.LotRow)));
        WorldSave current = WorldPersistence.MigrateV9ToV10(migrated);
        current = WorldPersistence.MigrateV10ToV11(current);
        current = WorldPersistence.MigrateV11ToV12(current);
        WorldPersistence.Validate(current);
    }

    [Fact]
    public void MigrateV9ToV10_BoundsWoodUnitsToPhysicalParcelLots()
    {
        WorldSave legacy = WorldPersistence.Capture(TestHelpers.NewHeroWorld());
        legacy.Version = 9;
        legacy.Parcels.Add(new ParcelSave
        {
            Id = 1,
            LogicalColumn = 0,
            LogicalRow = 0,
            IsUnlocked = true,
        });
        legacy.NaturalResourcePatches.Add(new NaturalResourcePatchSave
        {
            Id = 100,
            ParcelId = 1,
            ResourceType = ResourceType.Wood.ToString(),
            UnitReserves = Enumerable.Repeat(1, 80).ToList(),
        });

        WorldSave migrated = WorldPersistence.MigrateV9ToV10(legacy);

        Assert.Equal(10, migrated.Version);
        NaturalResourcePatchSave patch = Assert.Single(migrated.NaturalResourcePatches);
        Assert.Equal(2, patch.UnitReserves.Count);
        Assert.Equal(new[] { 40, 40 }, patch.UnitReserves);
        migrated = WorldPersistence.MigrateV10ToV11(migrated);
        migrated = WorldPersistence.MigrateV11ToV12(migrated);
        WorldPersistence.Validate(migrated);
    }
}
