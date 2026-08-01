using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// EG-1 resource seam coverage. Branches, Plant Fiber, Small Stone and
/// Wild Food appear on free parcels of a fresh city, gather into the
/// city inventory under the correct resource type, and respect the
/// carried capacity of six units. V20 → V21 migration is the only
/// schema hop that EG-1 introduces; everything else is structural.
/// </summary>
public class Eg1ResourceSeamTests
{
    [Fact]
    public void ResourceType_GainsFourNewKinds()
    {
        // Sanity: the four new kinds parse cleanly and stay distinct
        // from the legacy set. If a future migration renames them,
        // this assertion is the earliest signal to update.
        Assert.Equal("Branches", ResourceType.Branches.ToString());
        Assert.Equal("PlantFiber", ResourceType.PlantFiber.ToString());
        Assert.Equal("SmallStone", ResourceType.SmallStone.ToString());
        Assert.Equal("WildFood", ResourceType.WildFood.ToString());
        Assert.NotEqual(ResourceType.Wood, ResourceType.Branches);
    }

    [Fact]
    public void SeedStartingOpportunities_PlacesEgA0Distribution()
    {
        var world = TestHelpers.NewHeroWorld();
        // Two forests seeded by SeedStartingForests consume parcels 1–2.
        world.SeedStartingForests();
        // The four EG-A0 types go onto the next four free parcels.
        world.SeedStartingOpportunities();

        int branches = 0, plantFiber = 0, smallStone = 0, wildFood = 0;
        foreach (NaturalResourcePatch patch in world.NaturalResourcePatches.Values)
        {
            switch (patch.ResourceType)
            {
                case ResourceType.Branches: branches += patch.UnitReserves.Count; break;
                case ResourceType.PlantFiber: plantFiber += patch.UnitReserves.Count; break;
                case ResourceType.SmallStone: smallStone += patch.UnitReserves.Count; break;
                case ResourceType.WildFood: wildFood += patch.UnitReserves.Count; break;
            }
        }
        // EG-A0 distribution: 7 bundles × 2, 3 × 2, 3 × 2, 4 × 2.
        Assert.Equal(7, branches);
        Assert.Equal(3, plantFiber);
        Assert.Equal(3, smallStone);
        Assert.Equal(4, wildFood);
    }

    [Fact]
    public void SeedStartingForests_UsesSixMatureTreesWithEightWoodEach()
    {
        CityWorld world = TestHelpers.NewHeroWorld();

        world.SeedStartingForests();

        NaturalResourcePatch[] forests = world.NaturalResourcePatches.Values
            .Where(patch => patch.ResourceType == ResourceType.Wood)
            .ToArray();
        Assert.Equal(2, forests.Length);
        Assert.Equal(6, forests.Sum(forest => forest.UnitReserves.Count));
        Assert.All(forests.SelectMany(forest => forest.UnitReserves),
            reserve => Assert.Equal(8, reserve));
        Assert.Equal(48, forests.Sum(forest => forest.TotalReserve));
    }

    [Fact]
    public void SeedStartingOpportunities_IsIdempotent()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        int initialPatches = world.NaturalResourcePatches.Count;
        world.SeedStartingOpportunities();
        Assert.Equal(initialPatches, world.NaturalResourcePatches.Count);
    }

    [Fact]
    public void GatherFromPatch_CreditsCarriedInventoryForNewResource()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();

        NaturalResourcePatch branches = FindFirstPatch(world, ResourceType.Branches);
        int gathered = world.GatherFromPatch(branches.Id, unitId: 0, amount: 1);
        Assert.Equal(1, gathered);
        Assert.Equal(1, world.CarriedGroundResourceCount());
    }

    [Fact]
    public void GatherFromPatch_CapturesValidPatchVisitAndRoundTrips()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        NaturalResourcePatch branches = FindFirstPatch(world, ResourceType.Branches);

        Assert.Equal(1, world.GatherFromPatch(branches.Id, unitId: 0, amount: 1));

        WorldSave captured = WorldPersistence.Capture(world);
        WorldPersistence.Validate(captured);
        CitizenSave hero = Assert.Single(captured.Citizens);
        Assert.Null(hero.LastVisitedResourceBuildingId);
        Assert.Equal(branches.Id, hero.LastVisitedResourcePatchId);
        Assert.Equal(0, hero.LastVisitedResourceUnitId);

        var restored = new CityWorld();
        restored.Restore(captured);
        Assert.Equal(branches.Id, restored.Hero!.LastVisitedResourcePatchId);
        Assert.Equal(0, restored.Hero.LastVisitedResourceUnitId);
        WorldPersistence.Validate(WorldPersistence.Capture(restored));
    }

    [Fact]
    public void GatherFromPatch_RespectsCarriedCapacityOfSix()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();

        // Fill the cap exactly so the next gather must clamp.
        NaturalResourcePatch branches = FindFirstPatch(world, ResourceType.Branches);
        for (int i = 0; i < CityWorld.CarriedGroundResourceCapacity; i++)
        {
            int patchId = branches.Id;
            int unit = (i % branches.UnitReserves.Count);
            // Force unit 0 to be drained first by stepping it manually
            // so subsequent units are reachable without exhausting the
            // patch on a single unit.
            world.GatherFromPatch(patchId, unit, 1);
        }
        Assert.Equal(CityWorld.CarriedGroundResourceCapacity, world.CarriedGroundResourceCount());

        // One more unit of any carried-ground type must be refused.
        int refused = world.GatherFromPatch(branches.Id, unitId: 0, amount: 1);
        Assert.Equal(0, refused);
    }

    [Fact]
    public void GatherFromPatch_DoesNotCapWood()
    {
        // Wood is not in CarriedGroundResourceTypes — it still flows
        // into per-building storage. The legacy Forest gather path is
        // the regression that this assertion protects.
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        NaturalResourcePatch forest = FindFirstPatch(world, ResourceType.Wood);
        int before = world.CarriedGroundResourceCount();
        int gathered = world.GatherFromPatch(forest.Id, unitId: 0, amount: 1);
        Assert.Equal(1, gathered);
        Assert.Equal(before, world.CarriedGroundResourceCount());
    }

    [Fact]
    public void MigrateV20ToV21_BumpsVersionWithoutOtherChanges()
    {
        var world = TestHelpers.NewHeroWorld();
        WorldSave save = WorldPersistence.Capture(world);
        save.Version = 20;
        // The four new resource kinds are not invented by the migration
        // — they belong to a fresh seed that runs on load. A v20 save
        // that lacked them before still lacks them after.
        int patchesBefore = save.NaturalResourcePatches.Count;
        WorldSave migrated = WorldPersistence.MigrateV20ToV21(save);
        Assert.Equal(21, migrated.Version);
        Assert.Equal(patchesBefore, migrated.NaturalResourcePatches.Count);
        WorldPersistence.Validate(WorldPersistence.MigrateToCurrent(migrated));
    }

    [Fact]
    public void MigrateToCurrent_RollsV20ThroughV21EndToEnd()
    {
        // The full chain must still validate so a player loading an
        // EG-0 save on top of EG-1 code does not crash on the missing
        // migration hop.
        var world = TestHelpers.NewHeroWorld();
        WorldSave save = WorldPersistence.Capture(world);
        save.Version = 20;
        WorldSave migrated = WorldPersistence.MigrateToCurrent(save);
        Assert.Equal(WorldSave.CurrentVersion, migrated.Version);
        WorldPersistence.Validate(migrated);
    }

    [Fact]
    public void MigrateV22ToV23_CorrectsLegacyForestsAndPreservesDepletionRatio()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        WorldSave save = WorldPersistence.Capture(world);
        save.Version = 22;
        NaturalResourcePatchSave[] woodPatches = save.NaturalResourcePatches
            .Where(patch => patch.ResourceType == ResourceType.Wood.ToString())
            .ToArray();
        Assert.Equal(2, woodPatches.Length);
        woodPatches[0].UnitReserves = Enumerable.Repeat(5, 8).ToList();
        woodPatches[1].UnitReserves = Enumerable.Repeat(0, 8).ToList();
        BuildingSave legacyForest = save.Buildings.First(
            building => building.Kind == BuildingKind.Forest.ToString());
        legacyForest.WoodUnitReserves = Enumerable.Repeat(40, 8).ToList();
        legacyForest.WoodReserve = 320;
        legacyForest.StorageCapacity = 640;
        legacyForest.MinStock = 0;
        legacyForest.MaxStock = 640;
        legacyForest.TargetStock = 640;

        WorldSave migrated = WorldPersistence.MigrateV22ToV23(save);

        Assert.Equal(23, migrated.Version);
        Assert.Equal(new[] { 3, 0, 0 }, woodPatches[0].UnitReserves);
        Assert.Equal(new[] { 0, 0, 0 }, woodPatches[1].UnitReserves);
        Assert.Equal(new[] { 8, 8, 8 }, legacyForest.WoodUnitReserves);
        Assert.Equal(24, legacyForest.MaxStock);
        Assert.Equal(24, legacyForest.TargetStock);
        WorldSave current = WorldPersistence.MigrateV23ToV24(migrated);
        WorldPersistence.Validate(current);
    }

    private static NaturalResourcePatch FindFirstPatch(
        CityWorld world, ResourceType type)
    {
        foreach (NaturalResourcePatch patch in world.NaturalResourcePatches.Values)
        {
            if (patch.ResourceType == type) return patch;
        }
        throw new System.InvalidOperationException(
            $"No patch of type {type} found in the seeded world.");
    }
}
