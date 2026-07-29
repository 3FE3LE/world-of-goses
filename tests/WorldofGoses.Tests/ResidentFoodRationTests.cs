using System.Linq;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Once-per-day resident Food ration (docs/FIRST_PLAYABLE_LOOP_AUDIT.md §17):
/// every citizen costs Food at dawn regardless of stamina/work, so recruiting
/// carries a real ongoing cost even for idle citizens the stamina-driven meal
/// system would otherwise never charge.
/// </summary>
public class ResidentFoodRationTests
{
    [Fact]
    public void Dawn_WithSufficientFood_ConsumesExactlyOnePerResident()
    {
        // Food is stored on Farm-kind buildings (CityResourceLedger sums
        // "every building that produces it"), so a Farm must exist for a
        // deposit to land anywhere; TestHelpers.WorldWithHome() registers
        // exactly that placeholder.
        var world = TestHelpers.WorldWithHome();
        world.DepositFood(50);
        int foodBefore = world.TotalStockOf(ResourceType.Food);

        TestHelpers.AdvanceToNextDawn(world);

        Assert.Equal(foodBefore - 1, world.TotalStockOf(ResourceType.Food));
        Assert.DoesNotContain(world.Log.Events, evt => evt.Kind == WorldEventKind.FoodRationShortfall);
    }

    [Fact]
    public void Dawn_WithNoFood_LeavesStockUntouchedAndLogsShortfall()
    {
        var world = TestHelpers.NewHeroWorld();
        Assert.Equal(0, world.TotalStockOf(ResourceType.Food));

        TestHelpers.AdvanceToNextDawn(world);

        Assert.Equal(0, world.TotalStockOf(ResourceType.Food));
        var shortfall = world.Log.Events.Last(evt => evt.Kind == WorldEventKind.FoodRationShortfall);
        Assert.Equal(1, shortfall.Amount);
    }

    [Fact]
    public void Dawn_ScalesWithResidentCount()
    {
        var world = TestHelpers.WorldWithHome();
        world.RegisterCitizen(TestHelpers.NewCitizen(2));
        world.RegisterCitizen(TestHelpers.NewCitizen(3));
        world.DepositFood(50);
        int foodBefore = world.TotalStockOf(ResourceType.Food);

        TestHelpers.AdvanceToNextDawn(world);

        Assert.Equal(foodBefore - 3, world.TotalStockOf(ResourceType.Food));
    }

    [Fact]
    public void Dawn_EmptyCity_NeverLogsShortfall()
    {
        // No citizens at all (not even a hero): the demand must stay zero,
        // never an abstract floor — an empty city owes nothing.
        var world = new CityWorld();

        TestHelpers.AdvanceToNextDawn(world);

        Assert.DoesNotContain(world.Log.Events, evt => evt.Kind == WorldEventKind.FoodRationShortfall);
    }

    [Fact]
    public void OfflineCatchUp_ConsumesTheSameRationAsLive()
    {
        var liveWorld = TestHelpers.WorldWithHome();
        liveWorld.DepositFood(50);
        TestHelpers.AdvanceToNextDawn(liveWorld);

        var offlineWorld = TestHelpers.WorldWithHome();
        offlineWorld.DepositFood(50);
        WorldTimeAdvance.Advance(offlineWorld, GameClock.TicksPerInGameDay);

        Assert.Equal(
            liveWorld.TotalStockOf(ResourceType.Food),
            offlineWorld.TotalStockOf(ResourceType.Food));
    }
}
