using System;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class OfflineProgressionTests
{
    [Fact]
    public void ApplyAll_AdvancesQuarryAndFarmDuringAbsence()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var farm = world.GetBuilding(new BuildingId(2))!;

        int tickBefore = world.CurrentTick;
        var report = OfflineProgression.ApplyAll(world, ticksToApply: CityEconomyRules.ProductionCycleTicks);

        Assert.True(report.HadProgression);
        Assert.True(quarry.Stock > 0);
        Assert.True(farm.Stock > 0);
        // WorldWithHome lands at a workday tick (08:00, tick 1200)
        // since the 2026-07-30 workday shift, so the assertion is
        // relative to the world start instead of an absolute tick.
        Assert.Equal(
            tickBefore + CityEconomyRules.ProductionCycleTicks,
            world.CurrentTick);
    }

    [Fact]
    public void ApplyAll_RespectsEachBuildingsPolicy()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var farm = world.GetBuilding(new BuildingId(2))!;
        farm.ConfigureProductionPolicy(enabled: false, minStock: 0, maxStock: farm.StorageCapacity);

        OfflineProgression.ApplyAll(world, ticksToApply: CityEconomyRules.ProductionCycleTicks);

        Assert.True(quarry.Stock > 0);
        Assert.Equal(0, farm.Stock);
    }

    [Fact]
    public void ComputeTicks_ZeroElapsed_IsZero()
    {
        var now = new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero);
        var ticks = OfflineProgression.ComputeTicks(now, now);
        Assert.Equal(0, ticks);
    }

    [Fact]
    public void ComputeTicks_NegativeElapsed_IsZero()
    {
        var now = new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero);
        var future = now.AddSeconds(60);
        var ticks = OfflineProgression.ComputeTicks(now, future);
        Assert.Equal(0, ticks);
    }

    [Fact]
    public void ComputeTicks_OneSecondAt1Hz_IsOne()
    {
        var now = new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero);
        var last = now.AddSeconds(-1);
        var ticks = OfflineProgression.ComputeTicks(
            now, last, maxOffline: TimeSpan.FromDays(7), tickRateHz: 1.0);
        Assert.Equal(1, ticks);
    }

    [Fact]
    public void ComputeTicks_HourElapsedAt1Hz_Is3600()
    {
        var now = new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero);
        var last = now.AddHours(-1);
        var ticks = OfflineProgression.ComputeTicks(
            now, last, maxOffline: TimeSpan.FromDays(7), tickRateHz: 1.0);
        Assert.Equal(3600, ticks);
    }

    [Fact]
    public void ComputeTicks_DaysOverCap_IsCapped()
    {
        var now = new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero);
        var last = now.AddDays(-30);
        var ticks = OfflineProgression.ComputeTicks(
            now, last, maxOffline: TimeSpan.FromDays(7), tickRateHz: 1.0);
        var expected = (int)(7 * 24 * 60 * 60);
        Assert.Equal(expected, ticks);
    }

    [Fact]
    public void ComputeTicks_LastSeenAtEpoch_IsZeroDefensively()
    {
        var now = new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero);
        var zero = DateTimeOffset.FromUnixTimeMilliseconds(0);
        var ticks = OfflineProgression.ComputeTicks(now, zero);
        Assert.Equal(0, ticks);
    }

    [Fact]
    public void ComputeTicks_NonPositiveTickRate_IsZero()
    {
        var now = new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero);
        var last = now.AddHours(-1);
        var ticks = OfflineProgression.ComputeTicks(now, last, tickRateHz: 0);
        Assert.Equal(0, ticks);
    }

    [Fact]
    public void Apply_ZeroTicks_NoStateChange()
    {
        var world = TestHelpers.NewProductionWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var tickBefore = world.CurrentTick;
        var stockBefore = world.PrimaryBuilding.Stock;

        var report = OfflineProgression.Apply(world, buildingId, ticksToApply: 0);

        Assert.Equal(0, report.TicksApplied);
        Assert.False(report.HadProgression);
        Assert.Equal(tickBefore, world.CurrentTick);
        Assert.Equal(stockBefore, world.PrimaryBuilding.Stock);
    }

    [Fact]
    public void Apply_NegativeTicks_NoOp()
    {
        var world = TestHelpers.NewProductionWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var tickBefore = world.CurrentTick;

        var report = OfflineProgression.Apply(world, buildingId, ticksToApply: -10);

        Assert.Equal(0, report.TicksApplied);
        Assert.Equal(tickBefore, world.CurrentTick);
    }

    [Fact]
    public void Apply_FewTicks_StockAndExperienceAccumulate()
    {
        var world = TestHelpers.NewProductionWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var bran = world.GetCitizen(new CitizenId(1))!;
        var branExpBefore = bran.GetExperience(CompetencyId.Mining);

        int tickBefore = world.CurrentTick;
        var report = OfflineProgression.Apply(
            world,
            buildingId,
            ticksToApply: CityEconomyRules.ProductionCycleTicks);

        Assert.Equal(1, report.TicksApplied);
        Assert.True(report.HadProgression);
        // WorldWithHome lands at the workday tick (1200) since the
        // 2026-07-30 shift, so absolute tick post-advance is relative.
        Assert.Equal(
            tickBefore + CityEconomyRules.ProductionCycleTicks,
            world.CurrentTick);
        // Ten clock ticks contain one productive batch. Two assigned Quarry
        // workers add two Stone and gain one experience event each.
        Assert.Equal(2, world.PrimaryBuilding.Stock);
        Assert.Equal(2, report.StockAdded);
        Assert.Equal(0, report.StockWasted);
        Assert.Equal(branExpBefore + 1, bran.GetExperience(CompetencyId.Mining));
    }

    [Fact]
    public void Apply_ManyTicks_StopsAtTargetWithoutPhantomExperienceOrWaste()
    {
        var world = TestHelpers.NewProductionWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var bran = world.GetCitizen(new CitizenId(1))!;
        var branExpBefore = bran.GetExperience(CompetencyId.Mining);
        var stoneCap = world.PrimaryBuilding.StorageCapacity;

        var report = OfflineProgression.Apply(world, buildingId, ticksToApply: 100);

        // With upkeep dormant, the Quarry fills its 20-stone cap in
        // ~10 ticks and the loop short-circuits. The test asserts "no
        // phantom experience" by checking the hero's exp matches
        // productive ticks.
        Assert.Equal(10, report.TicksApplied);
        Assert.Equal(stoneCap, world.PrimaryBuilding.Stock);
        Assert.Equal(stoneCap, report.StockAdded);
        Assert.Equal(0, report.StockWasted);
        Assert.Equal(branExpBefore + report.TicksApplied, bran.GetExperience(CompetencyId.Mining));
    }

    [Fact]
    public void Apply_UnknownBuilding_ReturnsNoneReport()
    {
        var world = TestHelpers.NewProductionWorld();
        var report = OfflineProgression.Apply(world, new BuildingId(999), ticksToApply: 100);
        Assert.False(report.HadProgression);
        Assert.Equal(0, report.TicksApplied);
    }

    [Fact]
    public void Apply_SimulatedTime_MatchesTickRate()
    {
        var world = TestHelpers.NewProductionWorld();
        var buildingId = world.PrimaryBuilding.Id;

        var report = OfflineProgression.Apply(world, buildingId, ticksToApply: 60, tickRateHz: 1.0);
        Assert.Equal(60.0, report.SimulatedTime.TotalSeconds);

        var secondWorld = TestHelpers.NewProductionWorld();
        var report2 = OfflineProgression.Apply(
            secondWorld, secondWorld.PrimaryBuilding.Id, ticksToApply: 60, tickRateHz: 2.0);
        Assert.Equal(30.0, report2.SimulatedTime.TotalSeconds);
    }

    [Fact]
    public void Apply_DisabledPolicy_DoesNotProduce()
    {
        var world = TestHelpers.NewProductionWorld();
        var building = world.PrimaryBuilding;
        building.ConfigureProductionPolicy(enabled: false, minStock: 0, maxStock: building.StorageCapacity);

        var report = OfflineProgression.Apply(world, building.Id, ticksToApply: 60);

        Assert.False(report.HadProgression);
        Assert.Equal(0, building.Stock);
    }

    [Fact]
    public void Apply_ExhaustedWorkers_WaitForFoodWithoutLosingStandingOrder()
    {
        // Pre-deplete the Quarry workers to exactly 6 stamina each
        // and disable the Farm so no food is produced for regen.
        // Each tick pays 1 cost → 5 productive ticks, then no more
        // (cost 6 → 0 means tick 6 contributes nothing).
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var farm = world.GetBuilding(new BuildingId(2))!;
        var bran = world.GetCitizen(new CitizenId(1))!;
        var erin = world.GetCitizen(new CitizenId(2))!;
        bran.ConsumeStamina(bran.CurrentStamina - 6);
        erin.ConsumeStamina(erin.CurrentStamina - 6);
        farm.ConfigureProductionPolicy(enabled: false, minStock: 0, maxStock: farm.StorageCapacity);

        var report = OfflineProgression.Apply(world, quarry.Id, ticksToApply: 100);

        Assert.Equal(0, report.TicksApplied);
        Assert.Equal(0, report.StockAdded);
        Assert.Equal(CitizenVitalStatus.BlockedNoFood, bran.VitalStatus);
        Assert.Equal(CitizenVitalStatus.BlockedNoFood, erin.VitalStatus);
        Assert.Equal(quarry.Id, bran.CurrentAssignment);
        Assert.Equal(quarry.Id, erin.CurrentAssignment);
    }

    [Fact]
    public void Apply_WithFoodLoaded_RunsLongerThanExhaustedBaseline()
    {
        // Upkeep is dormant, so the WellFed buff no longer changes the
        // cap-reach timing — the Quarry fills in 10 ticks either way.
        // This test still pins the "with food" branch, asserting that
        // workers sustain (not exhausted after 5 ticks) and that the
        // cap is reached.
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var bran = world.GetCitizen(new CitizenId(1))!;
        var erin = world.GetCitizen(new CitizenId(2))!;
        // Pre-deposit enough food for both workers to eat every tick.
        world.DepositFood(StaminaRules.MaxStamina);

        var report = OfflineProgression.Apply(world, quarry.Id, ticksToApply: 100);

        Assert.Equal(10, report.TicksApplied);
        Assert.Equal(quarry.StorageCapacity, report.StockAdded);
        Assert.Equal(quarry.StorageCapacity, quarry.Stock);
        Assert.True(bran.CurrentStamina > 0);
        Assert.True(erin.CurrentStamina > 0);
    }

    [Fact]
    public void Apply_AfterExhaustion_ExplainsFoodBlock()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var farm = world.GetBuilding(new BuildingId(2))!;
        foreach (var citizen in world.Citizens.Values)
        {
            citizen.ConsumeStamina(citizen.CurrentStamina);
        }
        farm.ConfigureProductionPolicy(enabled: false, minStock: 0, maxStock: farm.StorageCapacity);

        OfflineProgression.Apply(world, quarry.Id, ticksToApply: 10);

        // The name of this test is the assertion: the food is locked out, so
        // the stop is explained by the food and not by the recovery it blocks.
        Assert.Equal(ProductionStopCause.WorkersBlockedNoFood, quarry.StopCause);
    }
}
