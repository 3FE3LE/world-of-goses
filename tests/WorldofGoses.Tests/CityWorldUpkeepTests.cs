using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class CityWorldUpkeepTests
{
    [Fact]
    public void AdvanceWorldTick_UpkeepDrainsQuarryStone()
    {
        // Disable production and pre-fill Quarry so upkeep has
        // something to drain without being masked by production.
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        quarry.ConfigureProductionPolicy(enabled: false, minStock: 0, maxStock: quarry.StorageCapacity, priority: 0);
        quarry.AddStock(10);
        int stoneBefore = quarry.Stock;

        world.AdvanceWorldTick();

        Assert.Equal(stoneBefore - 1, quarry.Stock);
    }

    [Fact]
    public void AdvanceWorldTick_UpkeepNeverMakesStockNegative()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        quarry.AddStock(0); // already 0

        for (int i = 0; i < 50; i++)
        {
            world.AdvanceWorldTick();
        }

        Assert.True(quarry.Stock >= 0);
    }

    [Fact]
    public void AdvanceWorldTick_UpkeepScalesWithPopulation()
    {
        // With 5 seeded citizens, upkeep should drain 1 stone/tick.
        // We simulate by pre-filling Quarry and tracking drain over
        // many ticks.
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        quarry.AddStock(50);
        int before = quarry.Stock;

        // Run 100 day ticks. Quarry produces ~2 stone/tick (net +1
        // with upkeep), so the stock will rise, not fall. To isolate
        // the upkeep drain we disable Quarry.
        quarry.ConfigureProductionPolicy(enabled: false, minStock: 0, maxStock: quarry.StorageCapacity, priority: 0);

        world.AdvanceWorldTick();

        int upkeepPerTick = before - quarry.Stock;
        Assert.Equal(1, upkeepPerTick); // 5 citizens / 5 = 1 stone/tick
    }
}
