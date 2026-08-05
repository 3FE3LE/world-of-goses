using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class ToolGatheringTests
{
    [Fact]
    public void PrimitiveAxe_RequiresShelterAndReachableOpeningMaterials()
    {
        CityWorld withoutShelter = TestHelpers.NewHeroWorld();
        withoutShelter.Resources.DepositToCityInventory(ResourceType.Branches, 1);
        withoutShelter.Resources.DepositToCityInventory(ResourceType.SmallStone, 1);

        Assert.Equal(
            ToolCraftOutcome.ShelterRequired,
            withoutShelter.TryCraftTool(ToolKind.PrimitiveAxe).Outcome);

        CityWorld world = TestHelpers.WorldWithHome();
        world.Resources.DepositToCityInventory(ResourceType.Branches, 1);
        world.Resources.DepositToCityInventory(ResourceType.SmallStone, 1);

        ToolCraftResult crafted = world.TryCraftTool(ToolKind.PrimitiveAxe);

        Assert.True(crafted.IsSuccess);
        Assert.True(world.HasTool(ToolKind.PrimitiveAxe));
        Assert.Equal(0, world.Resources.Available(ResourceType.Branches));
        Assert.Equal(0, world.Resources.Available(ResourceType.SmallStone));
        Assert.Equal(
            ToolCraftOutcome.AlreadyOwned,
            world.TryCraftTool(ToolKind.PrimitiveAxe).Outcome);
    }

    [Fact]
    public void PrimitiveAxe_RoundtripRemainsStoredInShelterInventory()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        world.Resources.DepositToCityInventory(ResourceType.Branches, 1);
        world.Resources.DepositToCityInventory(ResourceType.SmallStone, 1);
        Assert.True(world.TryCraftTool(ToolKind.PrimitiveAxe).IsSuccess);

        CityWorld restored = CityWorld.FromSave(WorldPersistence.Capture(world));

        Assert.True(restored.HasTool(ToolKind.PrimitiveAxe));
        Assert.Equal(
            ToolCraftOutcome.AlreadyOwned,
            restored.ToolCraftAvailability(ToolKind.PrimitiveAxe).Outcome);
    }

    [Fact]
    public void MigrateV27ToV28_InitializesEmptyToolSetWithoutInventingAxe()
    {
        WorldSave save = WorldPersistence.Capture(TestHelpers.NewHeroWorld());
        save.Version = 27;
        save.Tools.Clear();

        WorldSave migrated = WorldPersistence.MigrateV27ToV28(save);

        Assert.Equal(28, migrated.Version);
        Assert.Empty(migrated.Tools);
        WorldPersistence.Validate(WorldPersistence.MigrateV29ToV30(
            WorldPersistence.MigrateV28ToV29(migrated)));
        Assert.False(CityWorld.FromSave(migrated).HasTool(ToolKind.PrimitiveAxe));
    }

    [Fact]
    public void GroundResource_DoubleGatherDepletesExactlyOnce()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingOpportunities();
        NaturalResourcePatch fiber = world.NaturalResourcePatches.Values
            .First(patch => patch.ResourceType == ResourceType.PlantFiber);

        NaturalResourceGatherResult first = world.TryGatherFromPatch(
            fiber.Id,
            unitId: 0,
            amount: 2);
        NaturalResourceGatherResult duplicate = world.TryGatherFromPatch(
            fiber.Id,
            unitId: 0,
            amount: 2);

        Assert.Equal(NaturalResourceGatherOutcome.Gathered, first.Outcome);
        Assert.Equal(2, first.GatheredAmount);
        Assert.Equal(0, fiber.UnitReserves[0]);
        Assert.Equal(NaturalResourceGatherOutcome.NodeUnavailable, duplicate.Outcome);
        Assert.Equal(0, duplicate.GatheredAmount);
        Assert.Equal(2, world.Resources.Available(ResourceType.PlantFiber));
    }

    [Fact]
    public void FullFoundingStorage_BlocksGatherBeforeDrainingNode()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingOpportunities();
        world.Resources.DepositToCityInventory(
            ResourceType.Branches,
            CityWorld.CarriedGroundResourceCapacity);
        NaturalResourcePatch fiber = world.NaturalResourcePatches.Values
            .First(patch => patch.ResourceType == ResourceType.PlantFiber);

        NaturalResourceGatherResult availability =
            world.NaturalResourceGatherAvailability(fiber.Id, unitId: 0);
        NaturalResourceGatherResult attempted =
            world.TryGatherFromPatch(fiber.Id, unitId: 0, amount: 2);

        Assert.Equal(NaturalResourceGatherOutcome.StorageFull, availability.Outcome);
        Assert.Equal(NaturalResourceGatherOutcome.StorageFull, attempted.Outcome);
        Assert.Equal(2, fiber.UnitReserves[0]);
        Assert.Equal(0, world.Resources.Available(ResourceType.PlantFiber));
    }
}
