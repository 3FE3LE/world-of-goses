using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
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

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.Capture(world));

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
        WorldPersistence.Validate(WorldPersistence.MigrateToCurrent(migrated));
        Assert.False(WorldPersistence.FromSave(migrated).HasTool(ToolKind.PrimitiveAxe));
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

    /// <summary>
    /// Mature-tree Wood must require the axe on every path a scene can reach.
    /// CityWorld.GatherWood drains the same reserve without consulting the tool
    /// set, and CityWorldController used to forward to it, so any panel holding
    /// a controller reference could walk straight past the forestry gate. The
    /// wrappers are gone and the domain method is internal; this pins both so a
    /// future convenience wrapper cannot quietly reopen the hole.
    /// </summary>
    [Fact]
    public void NoPublicPathGathersMatureTreeWoodWithoutTheAxe()
    {
        // Source scan rather than reflection, matching DomainBoundaryTests:
        // CityWorldController is a Godot Node and the controller lives in the
        // same assembly, so it could still expose the internal domain method.
        string controllerSource = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "CityWorldController.cs"));
        Assert.False(
            Regex.IsMatch(
                StripComments(controllerSource),
                @"public\s+int\s+GatherWood\b",
                RegexOptions.CultureInvariant),
            "CityWorldController exposes GatherWood again, which skips the axe gate.");

        Assert.DoesNotContain(
            typeof(CityWorld)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(method => method.Name),
            name => name == nameof(CityWorld.GatherWood));

        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        NaturalResourcePatch tree = world.NaturalResourcePatches.Values
            .First(patch => patch.ResourceType == ResourceType.Wood);
        int reserveBefore = tree.TotalReserve;

        NaturalResourceGatherResult blocked =
            world.TryGatherFromPatch(tree.Id, unitId: 0, amount: 2);

        Assert.Equal(NaturalResourceGatherOutcome.MissingRequiredTool, blocked.Outcome);
        Assert.Equal(ToolKind.PrimitiveAxe, blocked.RequiredTool);
        Assert.Equal(reserveBefore, tree.TotalReserve);
        Assert.Equal(0, world.Resources.Available(ResourceType.Wood));
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

    /// <summary>
    /// Drops comments so a signature mentioned in prose cannot fail the scan —
    /// the removal of the wrappers is itself documented in a comment naming
    /// GatherWood.
    /// </summary>
    private static string StripComments(string source) => Regex.Replace(
        source,
        @"//.*?$|/\*.*?\*/",
        string.Empty,
        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant);
}
