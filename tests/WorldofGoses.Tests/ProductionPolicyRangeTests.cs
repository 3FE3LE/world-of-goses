using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Reactive range policy: <see cref="Building.ConfigureProductionPolicy"/>
/// validates the min/max/priority triplet, stores the values, and the
/// building produces until <see cref="Building.MaxStock"/>, then stops.
/// <see cref="Building.MinStock"/> equal to <see cref="Building.MaxStock"/>
/// is the "fixed stockpile" pattern and is allowed.
/// </summary>
public class ProductionPolicyRangeTests
{
    [Fact]
    public void ConfigureProductionPolicy_StoresAllThreeFields()
    {
        var building = TestHelpers.NewBuilding(storageCapacity: 20);

        building.ConfigureProductionPolicy(enabled: true, minStock: 5, maxStock: 15, priority: 2);

        Assert.True(building.ProductionEnabled);
        Assert.Equal(5, building.MinStock);
        Assert.Equal(15, building.MaxStock);
        Assert.Equal(2, building.Priority);
    }

    [Fact]
    public void ConfigureProductionPolicy_MinGreaterThanMax_Throws()
    {
        var building = TestHelpers.NewBuilding(storageCapacity: 20);
        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => building.ConfigureProductionPolicy(true, minStock: 10, maxStock: 5, priority: 0));
    }

    [Fact]
    public void ConfigureProductionPolicy_MinEqualMax_IsAllowed()
    {
        // Fixed-cap policy: produce exactly 8 stone and stop. The
        // building oscillates each tick (full → missing-1 → full).
        var building = TestHelpers.NewBuilding(storageCapacity: 20);
        building.ConfigureProductionPolicy(true, minStock: 8, maxStock: 8, priority: 0);

        Assert.Equal(8, building.MinStock);
        Assert.Equal(8, building.MaxStock);
    }

    [Fact]
    public void ReactiveResume_StockDropsToMinStock_UnblocksBuilding()
    {
        // Fill the Quarry, then drain it via upkeep until
        // Stock <= MinStock. Production outpaces upkeep when both run
        // at once, so the test pauses the building first.
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        quarry.ConfigureProductionPolicy(true, minStock: 3, maxStock: 10, priority: 0);

        // Fill the quarry.
        while (quarry.Stock < quarry.MaxStock)
        {
            world.AdvanceWorldTick();
        }
        Assert.Equal(quarry.MaxStock, quarry.Stock);
        Assert.Equal(ProductionStopCause.TargetReached, quarry.StopCause);

        // Pause production so upkeep is the only drain.
        quarry.ConfigureProductionPolicy(false, minStock: 3, maxStock: 10, priority: 0);
        int safety = 200;
        while (quarry.Stock > quarry.MinStock && safety-- > 0)
        {
            world.AdvanceWorldTick();
        }
        Assert.True(quarry.Stock <= quarry.MinStock,
            $"Expected Stock <= {quarry.MinStock} but got {quarry.Stock}.");

        // Resume the policy. The next tick should clear the
        // TargetReached sentinel and produce again.
        quarry.ConfigureProductionPolicy(true, minStock: 3, maxStock: 10, priority: 0);
        world.AdvanceWorldTick();

        Assert.NotEqual(ProductionStopCause.TargetReached, quarry.StopCause);
    }
}