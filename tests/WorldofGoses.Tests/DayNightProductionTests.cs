using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class DayNightProductionTests
{
    [Fact]
    public void AdvanceWorldTick_DuringDay_ProducesStock()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        int stockBefore = quarry.Stock;

        world.AdvanceWorldTick(); // tick 1, daytime

        Assert.True(quarry.Stock >= stockBefore);
        Assert.NotEqual(ProductionStopCause.Night, quarry.StopCause);
    }

    [Fact]
    public void AdvanceWorldTick_DuringNight_DoesNotProduce()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;

        // Skip to night.
        for (int t = 0; t < GameClock.DayTicks; t++)
        {
            world.AdvanceWorldTick();
        }

        Assert.Equal(ProductionStopCause.Night, quarry.StopCause);
        Assert.Equal(0, quarry.LastTickProduction);
    }

    [Fact]
    public void AdvanceWorldTick_AtNight_WorkersRestAndRecover()
    {
        // Drain workers to 50/100 before night. At night, they
        // should recover via base regen (1/tick).
        var world = TestHelpers.NewProductionWorld();
        var bran = world.GetCitizen(new CitizenId(1))!;
        bran.ConsumeStamina(50);
        int branBefore = bran.CurrentStamina;

        // Skip to mid-night.
        for (int t = 0; t < GameClock.DayTicks + 10; t++)
        {
            world.AdvanceWorldTick();
        }

        Assert.True(bran.CurrentStamina > branBefore,
            $"Expected the hero to recover at night (was {branBefore}, now {bran.CurrentStamina}).");
    }

    [Fact]
    public void AdvanceWorldTick_AtNight_NoStaminaCost()
    {
        // Disable both buildings (no work) and skip straight to
        // night. During night, workers eat (if food) and passively
        // regenerate — but never pay the work cost. After a few
        // hundred night ticks, drained workers must recover.
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var farm = world.GetBuilding(new BuildingId(2))!;
        quarry.ConfigureProductionPolicy(enabled: false, targetStock: quarry.StorageCapacity);
        farm.ConfigureProductionPolicy(enabled: false, targetStock: farm.StorageCapacity);

        var bran = world.GetCitizen(new CitizenId(1))!;
        bran.ConsumeStamina(40); // 60/100

        // Skip to night.
        for (int t = 0; t < GameClock.DayTicks; t++)
        {
            world.AdvanceWorldTick();
        }
        int atNightStart = bran.CurrentStamina;

        // 100 night ticks of regen → +100, capped at MaxStamina.
        for (int t = 0; t < 100; t++)
        {
            world.AdvanceWorldTick();
        }

        Assert.Equal(100, bran.CurrentStamina);
        // Assert no cost was paid (no stamina below start value during
        // night). The hero started night at whatever they had; they never
        // drain further.
        Assert.True(bran.CurrentStamina >= atNightStart);
    }

    [Fact]
    public void AdvanceWorldTick_BuffActive_RefreshesWhenCitizenEats()
    {
        // Load enough food so the hero eats every tick.
        var world = TestHelpers.NewProductionWorld();
        world.DepositFood(StaminaRules.MaxStamina);

        for (int i = 0; i < 10; i++)
        {
            world.AdvanceWorldTick();
        }

        var bran = world.GetCitizen(new CitizenId(1))!;
        Assert.True(bran.WellFedRemainingTicks > 0,
            $"The hero should have refreshed the buff after eating (was {bran.WellFedRemainingTicks}).");
    }

    [Fact]
    public void AdvanceWorldTick_BuffDecaysAcrossTicks()
    {
        // Disable the Farm so the hero can never eat (and therefore
        // never refresh his buff). Buff must decrement every tick.
        var world = TestHelpers.NewProductionWorld();
        var farm = world.GetBuilding(new BuildingId(2))!;
        farm.ConfigureProductionPolicy(enabled: false, targetStock: farm.StorageCapacity);
        var bran = world.GetCitizen(new CitizenId(1))!;
        bran.RefreshWellFedBuff();
        int startBuff = bran.WellFedRemainingTicks;

        for (int i = 0; i < 5; i++)
        {
            world.AdvanceWorldTick();
        }

        Assert.Equal(startBuff - 5, bran.WellFedRemainingTicks);
    }

    [Fact]
    public void AdvanceWorldTick_BuffFloorAtZero()
    {
        // Disable Farm so the buff never refreshes via eating.
        var world = TestHelpers.NewProductionWorld();
        var farm = world.GetBuilding(new BuildingId(2))!;
        farm.ConfigureProductionPolicy(enabled: false, targetStock: farm.StorageCapacity);
        var bran = world.GetCitizen(new CitizenId(1))!;
        bran.RefreshWellFedBuff();

        for (int i = 0; i < StaminaRules.WellFedBuffDuration + 50; i++)
        {
            world.AdvanceWorldTick();
        }

        Assert.Equal(0, bran.WellFedRemainingTicks);
    }
}
