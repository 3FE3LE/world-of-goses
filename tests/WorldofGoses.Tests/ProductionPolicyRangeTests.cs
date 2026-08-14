using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Reactive range policy: <see cref="Building.ConfigureProductionPolicy"/>
/// validates the min/max pair, stores the values, and the building
/// produces until <see cref="Building.MaxStock"/>, then stops.
/// <see cref="Building.MinStock"/> equal to <see cref="Building.MaxStock"/>
/// is the "fixed stockpile" pattern and is allowed.
/// </summary>
public class ProductionPolicyRangeTests
{
    [Fact]
    public void ConfigureProductionPolicy_StoresBothFields()
    {
        var building = TestHelpers.NewBuilding(storageCapacity: 20);

        building.ConfigureProductionPolicy(enabled: true, minStock: 5, maxStock: 15);

        Assert.True(building.ProductionEnabled);
        Assert.Equal(5, building.MinStock);
        Assert.Equal(15, building.MaxStock);
    }

    [Fact]
    public void ConfigureProductionPolicy_MinGreaterThanMax_Throws()
    {
        var building = TestHelpers.NewBuilding(storageCapacity: 20);
        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => building.ConfigureProductionPolicy(true, minStock: 10, maxStock: 5));
    }

    [Fact]
    public void ConfigureProductionPolicy_MinEqualMax_IsAllowed()
    {
        // Fixed-cap policy: produce exactly 8 stone and stop. The
        // building oscillates each tick (full → missing-1 → full).
        var building = TestHelpers.NewBuilding(storageCapacity: 20);
        building.ConfigureProductionPolicy(true, minStock: 8, maxStock: 8);

        Assert.Equal(8, building.MinStock);
        Assert.Equal(8, building.MaxStock);
    }

    [Fact]
    public void ReactiveResume_StockDropsToMinStock_UnblocksBuilding()
    {
        // Upkeep is dormant, so the only way to drop the quarry below
        // MinStock is to drain it directly. This still exercises the
        // "reactive resume" path: once Stock <= MinStock, the next
        // tick should clear the TargetReached sentinel and produce.
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        quarry.ConfigureProductionPolicy(true, minStock: 3, maxStock: 10);

        // Fill the quarry.
        while (quarry.Stock < quarry.MaxStock)
        {
            world.AdvanceWorldTick();
        }
        Assert.Equal(quarry.MaxStock, quarry.Stock);
        Assert.Equal(ProductionStopCause.TargetReached, quarry.StopCause);

        // Drain directly below MinStock. Upkeep is no longer the drain.
        int drain = quarry.Stock - quarry.MinStock + 1;
        quarry.TryConsumeStock(drain);
        Assert.True(quarry.Stock <= quarry.MinStock,
            $"Expected Stock <= {quarry.MinStock} but got {quarry.Stock}.");

        // The next tick should clear the TargetReached sentinel and
        // produce again because Stock <= MinStock triggers ResumeIfBelowMin.
        world.AdvanceWorldTick();

        Assert.NotEqual(ProductionStopCause.TargetReached, quarry.StopCause);
    }
}
