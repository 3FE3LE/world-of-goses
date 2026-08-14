using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class CityEconomyRulesTests
{
    [Fact]
    public void Production_UsesBatchesRatherThanEveryClockTick()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        Building quarry = world.GetBuilding(new BuildingId(1))!;

        for (int tick = 1; tick < CityEconomyRules.ProductionCycleTicks; tick++)
        {
            world.AdvanceWorldTick();
            Assert.Equal(0, quarry.Stock);
            Assert.Equal(0, quarry.LastTickProduction);
        }

        world.AdvanceWorldTick();
        Assert.True(quarry.LastTickProduction > 0);
    }

    [Fact]
    public void Food_IsNotConsumedRemotelyWhileCitizensRemainAtWork()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        Building quarry = world.GetBuilding(new BuildingId(1))!;
        Building farm = world.GetBuilding(new BuildingId(2))!;
        farm.ConfigureProductionPolicy(false, 0, farm.StorageCapacity);
        world.DepositFood(20);
        int foodBefore = world.FoodStock;

        for (int tick = 1; tick < CityEconomyRules.MealIntervalTicks; tick++)
        {
            world.AdvanceWorldTick();
            if (quarry.Stock > 0) quarry.TryConsumeStock(quarry.Stock);
        }
        Assert.Equal(foodBefore, world.FoodStock);

        world.AdvanceWorldTick();
        Assert.Equal(foodBefore, world.FoodStock);
    }
}
