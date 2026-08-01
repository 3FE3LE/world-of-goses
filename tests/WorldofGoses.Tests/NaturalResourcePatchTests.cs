using System;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class NaturalResourcePatchTests
{
    [Fact]
    public void Dawn_DoesNotRegenerateFiniteMatureTrees()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        NaturalResourcePatch patch = world.NaturalResourcePatches.Values.First();
        world.GatherWood(
            new BuildingId(patch.Id),
            unitId: 0,
            amount: CityWorld.StartingTreeWoodReserve);

        WorldTimeAdvance.Advance(world, GameClock.TicksPerInGameDay);

        Assert.Equal(0, patch.UnitReserves[0]);
        Assert.Equal(CityWorld.StartingForestUnitCount, patch.UnitReserves.Count);
    }

    [Fact]
    public void Dawn_DoesNotSproutAdditionalTreeUnits()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        NaturalResourcePatch patch = world.NaturalResourcePatches.Values.First();
        world.GatherWood(new BuildingId(patch.Id), amount: 1);
        WorldTimeAdvance.Advance(world, GameClock.TicksPerInGameDay);

        Assert.Equal(CityWorld.StartingForestUnitCount, patch.UnitReserves.Count);
    }

    [Fact]
    public void FiniteTrees_LiveAndOfflineBatchProduceEquivalentSnapshot()
    {
        CityWorld source = TestHelpers.NewHeroWorld();
        source.SeedStartingForests();
        NaturalResourcePatch sourcePatch = source.NaturalResourcePatches.Values.First();
        source.GatherWood(
            new BuildingId(sourcePatch.Id),
            unitId: 0,
            amount: CityWorld.StartingTreeWoodReserve);
        WorldSave save = WorldPersistence.Capture(source);
        CityWorld live = CityWorld.FromSave(save);
        CityWorld offline = CityWorld.FromSave(save);

        for (int tick = 0; tick < GameClock.TicksPerInGameDay; tick++)
        {
            live.AdvanceWorldTick();
        }
        WorldTimeAdvance.Advance(offline, GameClock.TicksPerInGameDay);

        Assert.Equal(
            WorldPersistence.SerializeToJson(
                WorldPersistence.Capture(live, DateTimeOffset.UnixEpoch)),
            WorldPersistence.SerializeToJson(
                WorldPersistence.Capture(offline, DateTimeOffset.UnixEpoch)));
    }

    [Fact]
    public void StartingResources_AreAttachedToPersistentParcels()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();

        Assert.Equal(9, world.Parcels.Count);
        Assert.Equal(8, world.Parcels.Values.Count(parcel => parcel.IsUnlocked));
        Assert.Equal(2, world.NaturalResourcePatches.Count);
        Assert.All(world.NaturalResourcePatches.Values, patch =>
        {
            Assert.Equal(ResourceType.Wood, patch.ResourceType);
            Assert.True(world.Parcels.ContainsKey(patch.ParcelId));
        });
    }

    [Fact]
    public void GatheredUnit_RoundtripPreservesPatchAndSelectedDepletion()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        NaturalResourcePatch patch = world.NaturalResourcePatches.Values.First();

        int gathered = world.GatherWood(new BuildingId(patch.Id), unitId: 0, amount: 1);
        WorldSave save = WorldPersistence.Capture(world);
        CityWorld restored = CityWorld.FromSave(
            WorldPersistence.DeserializeFromJson(
                WorldPersistence.SerializeToJson(save)));

        Assert.Equal(1, gathered);
        Assert.Equal(
            CityWorld.StartingTreeWoodReserve - 1,
            restored.NaturalResourcePatches[patch.Id].UnitReserves[0]);
        Assert.Equal(
            patch.TotalReserve,
            restored.NaturalResourcePatches[patch.Id].TotalReserve);
    }

    [Fact]
    public void MigrateV7ToV8_CreatesParcelsAndPatchesFromLegacyForests()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        WorldSave legacy = WorldPersistence.Capture(world);
        legacy.Version = 7;
        legacy.Parcels.Clear();
        legacy.NaturalResourcePatches.Clear();

        WorldSave migrated = WorldPersistence.MigrateV7ToV8(legacy);

        Assert.Equal(8, migrated.Version);
        Assert.Equal(2, migrated.Parcels.Count);
        Assert.Equal(2, migrated.NaturalResourcePatches.Count);
        // The step under test is asserted above; the rest of the way to today
        // is the chain's own job, so this test survives future schema bumps.
        WorldSave current = WorldPersistence.MigrateToCurrent(migrated);
        Assert.Equal(WorldSave.CurrentVersion, current.Version);
        WorldPersistence.Validate(current);
    }

    [Fact]
    public void SeedStartingForests_DoesNotDuplicatePersistedDepletedPatches()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        NaturalResourcePatch patch = world.NaturalResourcePatches.Values.First();
        world.GatherWood(
            new BuildingId(patch.Id),
            amount: CityWorld.StartingForestWoodReserve);
        world.TryConsumeResource(ResourceType.Wood, CityWorld.StartingForestWoodReserve);
        world.AdvanceWorldTick();
        CityWorld restored = CityWorld.FromSave(WorldPersistence.Capture(world));

        restored.SeedStartingForests();

        Assert.Equal(2, restored.NaturalResourcePatches.Count);
    }
}
