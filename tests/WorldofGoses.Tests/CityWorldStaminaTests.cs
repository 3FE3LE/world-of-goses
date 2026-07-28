using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class CityWorldStaminaTests
{
    [Fact]
    public void ProductionScenario_FoodStockIsZero()
    {
        var world = TestHelpers.NewProductionWorld();
        Assert.Equal(0, world.FoodStock);
        Assert.True(world.MaxFoodStock > 0);
    }

    [Fact]
    public void ProductionScenario_AllCitizensStartAtMaxStamina()
    {
        var world = TestHelpers.NewProductionWorld();
        foreach (var citizen in world.Citizens.Values)
        {
            Assert.Equal(citizen.MaxStamina, citizen.CurrentStamina);
        }
    }

    [Fact]
    public void AdvanceWorldTick_WithoutFood_DrainsStamina()
    {
        var world = TestHelpers.NewProductionWorld();
        int totalBefore = SumStamina(world);

        TestHelpers.AdvanceToNextProductionCycle(world);

        // 4 workers across Quarry + Farm, cost 1 each, no food yet.
        // After tick 1: Farm has produced 1 food, so the workers
        // that eat before the cost pay net 0. The rest lose 1.
        // Net per-tick drain must be >= 0 and < total.
        int totalAfter = SumStamina(world);
        Assert.True(totalAfter < totalBefore);
        Assert.True(totalAfter > 0);
    }

    [Fact]
    public void AdvanceWorldTick_WithSeededFood_NetZeroStaminaChange()
    {
        var world = TestHelpers.NewProductionWorld();
        // Pre-load enough food to cover every worker's regen.
        world.DepositFood(StaminaRules.MaxStamina);
        int totalBefore = SumStamina(world);

        TestHelpers.AdvanceToNextProductionCycle(world);

        // Every assigned worker eats first (cost neutralised), then
        // pays the cost. Net change for non-clamped workers is 0;
        // any worker that hit MaxStamina loses 1 (ate for nothing).
        int totalAfter = SumStamina(world);
        Assert.True(totalAfter >= totalBefore - 4);
    }

    [Fact]
    public void AdvanceWorldTick_AllWorkersExhausted_ProducesZeroAndSetsStopCause()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var farm = world.GetBuilding(new BuildingId(2))!;

        // Exhaust every worker and lock food out so regen can't undo it.
        foreach (var citizen in world.Citizens.Values)
        {
            citizen.ConsumeStamina(citizen.CurrentStamina);
        }
        farm.ConfigureProductionPolicy(enabled: false, minStock: 0, maxStock: farm.StorageCapacity, priority: 0);

        int quarryStockBefore = quarry.Stock;
        TestHelpers.AdvanceToNextProductionCycle(world);

        Assert.Equal(quarryStockBefore, quarry.Stock);
        Assert.Equal(ProductionStopCause.WorkersRecovering, quarry.StopCause);
        // Farm is paused (no food produced), not exhausted — its
        // assigned worker never paid a cost this tick.
        Assert.Equal(ProductionStopCause.Paused, farm.StopCause);
    }

    [Fact]
    public void AdvanceWorldTick_WithFood_KeepsStaminaAboveZero()
    {
        var world = TestHelpers.NewProductionWorld();
        var bran = world.GetCitizen(new CitizenId(1))!;
        var erin = world.GetCitizen(new CitizenId(2))!;

        // Drain the Quarry workers to 1 and pre-load food. With cost
        // 2 and regen 2 (base + buff), workers cycle around a stable
        // value (1 → 2 eat → 4 regen → 2 cost) and stay in the
        // contributing set instead of falling into WorkersExhausted.
        bran.ConsumeStamina(bran.CurrentStamina - 1);
        erin.ConsumeStamina(erin.CurrentStamina - 1);
        world.DepositFood(50);

        for (int tick = 0; tick < CityEconomyRules.MealIntervalTicks; tick++)
        {
            world.AdvanceWorldTick();
        }

        Assert.True(bran.CurrentStamina > 0);
        Assert.True(erin.CurrentStamina > 0);
    }

    [Fact]
    public void TryConsumeFood_Insufficient_ReturnsFalse_LeavesStockUnchanged()
    {
        var world = TestHelpers.NewProductionWorld();
        Assert.False(world.TryConsumeFood(5));
        Assert.Equal(0, world.FoodStock);
    }

    [Fact]
    public void DepositFood_ClampsAtFarmStorageCapacity()
    {
        var world = TestHelpers.NewProductionWorld();
        int added = world.DepositFood(10_000);
        Assert.Equal(world.MaxFoodStock, added);
        Assert.Equal(world.MaxFoodStock, world.FoodStock);
    }

    [Fact]
    public void BuildingCanProduce_StillTrueWhenWorkersExhausted()
    {
        // Regression: CanProduce is policy + workers + room, NOT
        // stamina. The stamina check lives in the tick body and the
        // stop cause carries the exhaustion signal.
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        Assert.True(quarry.CanProduce);

        foreach (var citizen in world.Citizens.Values)
        {
            citizen.ConsumeStamina(citizen.CurrentStamina);
        }
        Assert.True(quarry.CanProduce);
    }

    private static int SumStamina(CityWorld world)
    {
        int total = 0;
        foreach (var citizen in world.Citizens.Values)
        {
            total += citizen.CurrentStamina;
        }
        return total;
    }
}
