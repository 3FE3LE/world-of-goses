using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Forest / wood gathering slice: the founding hero world starts with
/// two Forests, each holding an initial wood reserve. Gathering
/// drains the reserve into the Forest's Stock; the Basic Shelter
/// recipe consumes from there.
/// </summary>
public class ForestTests
{
    [Fact]
    public void GatherWoodUnit_DepletesSelectedStableUnit()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        Building forest = world.GetBuilding(new BuildingId(100))!;

        int gathered = world.GatherWood(
            forest.Id,
            unitId: 0,
            amount: CityWorld.StartingTreeWoodReserve + 1);

        Assert.Equal(CityWorld.StartingTreeWoodReserve, gathered);
        Assert.Equal(0, forest.WoodUnitReserves[0]);
        Assert.Equal(CityWorld.StartingTreeWoodReserve, forest.WoodUnitReserves[^1]);
        Assert.Equal(
            forest.Id,
            world.Hero!.LastVisitedResourceBuildingId);
        Assert.Equal(0, world.Hero.LastVisitedResourceUnitId);
        Assert.Equal(0, world.Hero.LastVisitedResourcePositionIndex);
    }

    [Fact]
    public void SelectedUnitAndHeroVisit_Roundtrip()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(
            new BuildingId(100),
            unitId: 2,
            amount: CityWorld.StartingTreeWoodReserve);

        CityWorld restored = CityWorld.FromSave(
            WorldPersistence.DeserializeFromJson(
                WorldPersistence.SerializeToJson(
                    WorldPersistence.Capture(world))));

        Assert.Equal(0, restored.GetBuilding(new BuildingId(100))!.WoodUnitReserves[2]);
        Assert.Equal(new BuildingId(100), restored.Hero!.LastVisitedResourceBuildingId);
        Assert.Equal(2, restored.Hero.LastVisitedResourceUnitId);
        Assert.Equal(2, restored.Hero.LastVisitedResourcePositionIndex);
    }

    [Fact]
    public void MigrateV6ToV7_ExpandsAggregateReserveIntoStableUnits()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        WorldSave save = WorldPersistence.Capture(world);
        save.Version = 6;
        BuildingSave forest = save.Buildings.First(
            building => building.Kind == BuildingKind.Forest.ToString());
        int expectedReserve = forest.WoodReserve!.Value;
        forest.WoodUnitReserves.Clear();

        WorldSave migrated = WorldPersistence.MigrateV6ToV7(save);

        Assert.Equal(7, migrated.Version);
        Assert.Equal(expectedReserve, forest.WoodUnitReserves.Count);
        Assert.All(forest.WoodUnitReserves, reserve => Assert.Equal(1, reserve));
        WorldSave current = WorldPersistence.MigrateV7ToV8(migrated);
        current = WorldPersistence.MigrateV8ToV9(current);
        current = WorldPersistence.MigrateV9ToV10(current);
        current = WorldPersistence.MigrateV10ToV11(current);
        current = WorldPersistence.MigrateV11ToV12(current);
        current = WorldPersistence.MigrateV12ToV13(current);
        current = WorldPersistence.MigrateV13ToV14(current);
        WorldPersistence.Validate(current);
    }

    [Fact]
    public void DepletedPatch_KeepsCompatibilityStorageForFutureRegeneration()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        Building forest = world.GetBuilding(new BuildingId(100))!;
        for (int unitId = 0; unitId < forest.WoodUnitReserves.Count; unitId++)
        {
            world.GatherWood(
                forest.Id,
                unitId,
                CityWorld.StartingTreeWoodReserve);
        }
        Assert.True(forest.TryConsumeStock(forest.Stock));

        world.AdvanceWorldTick();

        Assert.NotNull(world.GetBuilding(forest.Id));
        Assert.Equal(7, world.Hero!.LastVisitedResourcePositionIndex);
        WorldPersistence.Validate(WorldPersistence.Capture(world));
    }

    [Fact]
    public void NewHeroWorld_SeedsTwoForests_WithWoodReserve()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();

        Assert.Equal(2, CountForests(world));
        foreach (var forest in Forests(world))
        {
            Assert.Equal(CityWorld.StartingForestWoodReserve, forest.WoodReserve);
            Assert.Equal(0, forest.Stock);
        }
    }

    [Fact]
    public void SoftReset_KeepsFounderProfileAndRecreatesOnlyStartingWorld()
    {
        CityWorld world = TestHelpers.NewConstructionWorld();
        Citizen founder = world.Hero!;

        CityWorld restarted = world.CreateRestartedCityKeepingHero();

        Assert.Equal(0, restarted.CurrentTick);
        Assert.Single(restarted.Citizens);
        Assert.Empty(restarted.Projects);
        Assert.Equal(founder.Name, restarted.Hero!.Name);
        Assert.Same(founder.Profile, restarted.Hero.Profile);
        Assert.Null(restarted.Hero.CurrentAssignment);
        Assert.Equal(2, CountForests(restarted));
    }

    [Fact]
    public void GatherWood_MovesFromReserveIntoCityInventory()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        var forest = FirstForest(world);

        int gathered = world.GatherWood(forest.Id, 3);

        Assert.Equal(3, gathered);
        Assert.Equal(CityWorld.StartingForestWoodReserve - 3, forest.WoodReserve);
        Assert.Equal(0, forest.Stock);
        Assert.Equal(3, world.TotalStockOf(ResourceType.Wood));
    }

    [Fact]
    public void GatherWood_WhenFounderIsAssigned_IsRejectedWithoutMutation()
    {
        CityWorld world = TestHelpers.NewConstructionWorld();
        Building forest = FirstForest(world);
        int reserveBefore = forest.WoodReserve;
        int stockBefore = forest.Stock;

        int gathered = world.GatherWood(forest.Id, unitId: 0, amount: 2);

        Assert.Equal(0, gathered);
        Assert.Equal(reserveBefore, forest.WoodReserve);
        Assert.Equal(stockBefore, forest.Stock);
    }

    [Fact]
    public void GatherWood_AfterFounderIsUnassigned_IsAllowedAgain()
    {
        CityWorld world = TestHelpers.NewConstructionWorld();
        ConstructionProject project = world.Projects.Values.Single();
        Building forest = FirstForest(world);

        Assert.True(world.TryUnassignFromProject(
            project.Id,
            world.Hero!.Id).IsSuccess);

        Assert.Equal(1, world.GatherWood(forest.Id, unitId: 1, amount: 1));
    }

    [Fact]
    public void GatherWood_CapsAtRemainingReserve()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        var forest = FirstForest(world);

        int gathered = world.GatherWood(forest.Id, CityWorld.StartingForestWoodReserve + 10);

        Assert.Equal(CityWorld.StartingForestWoodReserve, gathered);
        Assert.Equal(0, forest.WoodReserve);
        Assert.Equal(0, forest.Stock);
        Assert.Equal(
            CityWorld.StartingForestWoodReserve,
            world.TotalStockOf(ResourceType.Wood));
    }

    [Fact]
    public void TryAuthorizeBasicShelter_RequiresWood()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        // No wood gathered yet.
        var result = world.TryAuthorizeBasicShelter();

        Assert.False(result.IsSuccess);
        Assert.Equal(ConstructionAuthorizationOutcome.MissingMaterials, result.Outcome);
    }

    [Fact]
    public void TryAuthorizeBasicShelter_WithWoodDeposit_SucceedsAndSeedsRemainder()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        var forest = FirstForest(world);

        // Deposit = ceil(4 * 0.25) = 1 wood. One gather unlocks it.
        world.GatherWood(forest.Id, 1);

        var result = world.TryAuthorizeBasicShelter();
        Assert.True(result.IsSuccess);

        var project = world.GetProject(result.ProjectId!.Value)!;
        var byResource = new System.Collections.Generic.Dictionary<ResourceType, int>();
        foreach (var input in project.RemainingInputs)
        {
            byResource[input.Resource] = input.Amount;
        }
        Assert.Equal(3, byResource[ResourceType.Wood]);
    }

    [Fact]
    public void TotalWood_AggregatesForestStocks()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        var forest1 = FirstForest(world);
        var forest2 = Forests(world).Skip(1).First();

        world.GatherWood(forest1.Id, 2);
        world.GatherWood(forest2.Id, 3);

        Assert.Equal(5, world.TotalWood);
        Assert.Equal(CityWorld.StartingForestWoodReserve * 2 - 5, world.TotalWoodReserve);
    }

    [Fact]
    public void DepletedForest_RemainsUntilGatheredStockIsConsumed()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        var forest = FirstForest(world);

        int gathered = world.GatherWood(forest.Id, CityWorld.StartingForestWoodReserve);
        world.AdvanceWorldTick();

        Assert.Equal(CityWorld.StartingForestWoodReserve, gathered);
        Assert.Equal(CityWorld.StartingForestWoodReserve, world.TotalStockOf(ResourceType.Wood));
        Assert.NotNull(world.GetBuilding(forest.Id));
        var material = Assert.Single(
            ConstructionSnapshot.From(world)
                .OptionFor(ConstructionKind.BasicShelter)
                .Materials);
        Assert.Equal(CityWorld.StartingForestWoodReserve, material.Available);
        var farmMaterial = Assert.Single(
            ConstructionSnapshot.From(world)
                .OptionFor(ConstructionKind.Farm)
                .Materials);
        Assert.Equal(6, farmMaterial.Required);
        Assert.Equal(CityWorld.StartingForestWoodReserve, farmMaterial.Available);
    }

    [Fact]
    public void DepletedForest_DoesNotGeneratePhantomWoodFromWorkers()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        var forest = FirstForest(world);
        var second = TestHelpers.NewCitizen(100);
        world.RegisterCitizen(second);
        world.GatherWood(forest.Id, CityWorld.StartingForestWoodReserve);
        Assert.True(world.TryAssignCitizen(forest.Id, world.Hero!.Id).IsSuccess);
        Assert.True(world.TryAssignCitizen(forest.Id, second.Id).IsSuccess);
        Assert.True(world.ConfirmCitizenArrivedAtAssignment(world.Hero.Id, forest.Id));
        Assert.True(world.ConfirmCitizenArrivedAtAssignment(second.Id, forest.Id));
        int staminaBefore = world.Hero.CurrentStamina;

        world.AdvanceWorldTick();

        Assert.Equal(0, forest.Stock);
        Assert.Equal(
            CityWorld.StartingForestWoodReserve,
            world.TotalStockOf(ResourceType.Wood));
        Assert.Equal(0, forest.LastTickProduction);
        Assert.Equal(ProductionStopCause.MissingInputs, forest.StopCause);
        Assert.Equal(staminaBefore, world.Hero.CurrentStamina);
    }

    [Fact]
    public void Recipe_BasicShelter_RequiresFourWood()
    {
        var recipe = Recipes.ConstructionRecipeFor(ConstructionKind.BasicShelter);

        Assert.NotNull(recipe);
        var wood = recipe!.RequiredInputs.Single(i => i.Resource == ResourceType.Wood);
        Assert.Equal(4, wood.Amount);
    }

    private static int CountForests(CityWorld world)
    {
        int count = 0;
        foreach (var b in world.Buildings.Values)
        {
            if (b.Kind == BuildingKind.Forest) count++;
        }
        return count;
    }

    private static System.Collections.Generic.IEnumerable<Building> Forests(CityWorld world)
    {
        foreach (var b in world.Buildings.Values)
        {
            if (b.Kind == BuildingKind.Forest) yield return b;
        }
    }

    private static Building FirstForest(CityWorld world)
    {
        foreach (var b in world.Buildings.Values)
        {
            if (b.Kind == BuildingKind.Forest) return b;
        }
        throw new System.InvalidOperationException("No forest in world.");
    }
}
