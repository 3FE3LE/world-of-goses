using System.Linq;
using WorldofGoses.Domain;
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
    public void GatherWood_MovesFromReserveIntoStock()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        var forest = FirstForest(world);

        int gathered = world.GatherWood(forest.Id, 3);

        Assert.Equal(3, gathered);
        Assert.Equal(CityWorld.StartingForestWoodReserve - 3, forest.WoodReserve);
        Assert.Equal(3, forest.Stock);
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
        Assert.Equal(CityWorld.StartingForestWoodReserve, forest.Stock);
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
        Assert.True(world.TryAssignCitizen(forest.Id, world.Hero!.Id).IsSuccess);
        Assert.True(world.TryAssignCitizen(forest.Id, second.Id).IsSuccess);
        world.GatherWood(forest.Id, CityWorld.StartingForestWoodReserve);
        int staminaBefore = world.Hero.CurrentStamina;

        world.AdvanceWorldTick();

        Assert.Equal(CityWorld.StartingForestWoodReserve, forest.Stock);
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
