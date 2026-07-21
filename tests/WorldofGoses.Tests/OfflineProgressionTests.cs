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

        var report = OfflineProgression.ApplyAll(world, ticksToApply: 5);

        Assert.True(report.HadProgression);
        Assert.True(quarry.Stock > 0);
        Assert.True(farm.Stock > 0);
        Assert.Equal(5, world.CurrentTick);
    }

    [Fact]
    public void ApplyAll_RespectsEachBuildingsPolicy()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var farm = world.GetBuilding(new BuildingId(2))!;
        farm.ConfigureProductionPolicy(enabled: false, minStock: 0, maxStock: farm.StorageCapacity, priority: 0);

        OfflineProgression.ApplyAll(world, ticksToApply: 5);

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

        var report = OfflineProgression.Apply(world, buildingId, ticksToApply: 5);

        Assert.Equal(5, report.TicksApplied);
        Assert.True(report.HadProgression);
        Assert.Equal(5, world.CurrentTick);
        // With upkeep draining 1 stone/tick and Quarry producing 2/tick,
        // net stock grows 1/tick; expected final stock is 6 (initial 0
        // + 5 net). StockAdded counts production only (10), not the
        // net change.
        Assert.Equal(6, world.PrimaryBuilding.Stock);
        Assert.Equal(10, report.StockAdded);
        Assert.Equal(0, report.StockWasted);
        Assert.Equal(branExpBefore + 5, bran.GetExperience(CompetencyId.Mining));
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

        Assert.True(report.TicksApplied < 100);
        Assert.Equal(stoneCap, world.PrimaryBuilding.Stock);
        // The hero's mining experience crosses the bonus threshold at tick
        // 18 (3 base + 15 gained = 18 exp; floor(1*21/20) = 1, so total
        // 2/tick; tick 18 exp = 21 → bonus fires). 17 ticks × 2 + 1 tick
        // × 3 = 37 produced before target cap. StockAdded counts
        // production only; the test asserts "no phantom experience"
        // by checking the hero's exp matches productive ticks.
        Assert.Equal(37, report.StockAdded);
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
        building.ConfigureProductionPolicy(enabled: false, minStock: 0, maxStock: building.StorageCapacity, priority: 0);

        var report = OfflineProgression.Apply(world, building.Id, ticksToApply: 60);

        Assert.False(report.HadProgression);
        Assert.Equal(0, building.Stock);
    }

    [Fact]
    public void Apply_ExhaustedWorkers_StopsAtExhaustionTick()
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
        farm.ConfigureProductionPolicy(enabled: false, minStock: 0, maxStock: farm.StorageCapacity, priority: 0);

        var report = OfflineProgression.Apply(world, quarry.Id, ticksToApply: 100);

        Assert.Equal(5, report.TicksApplied);
        Assert.Equal(10, report.StockAdded);
        Assert.Equal(0, bran.CurrentStamina);
        Assert.Equal(0, erin.CurrentStamina);
    }

    [Fact]
    public void Apply_WithFoodLoaded_RunsLongerThanExhaustedBaseline()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var bran = world.GetCitizen(new CitizenId(1))!;
        var erin = world.GetCitizen(new CitizenId(2))!;
        bran.ConsumeStamina(bran.CurrentStamina - 6);
        erin.ConsumeStamina(erin.CurrentStamina - 6);
        // Pre-deposit enough food for both workers to eat every tick.
        world.DepositFood(StaminaRules.MaxStamina);

        var report = OfflineProgression.Apply(world, quarry.Id, ticksToApply: 100);

        // With food (buff active) workers sustain; target reached in 18
        // ticks (the hero's mining bonus kicks in at tick 18, producing 3).
        Assert.Equal(18, report.TicksApplied);
        Assert.Equal(37, report.StockAdded);
        Assert.Equal(quarry.StorageCapacity, quarry.Stock);
    }

    [Fact]
    public void Apply_AfterExhaustion_BuildingStopCauseIsWorkersExhausted()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var farm = world.GetBuilding(new BuildingId(2))!;
        foreach (var citizen in world.Citizens.Values)
        {
            citizen.ConsumeStamina(citizen.CurrentStamina);
        }
        farm.ConfigureProductionPolicy(enabled: false, minStock: 0, maxStock: farm.StorageCapacity, priority: 0);

        OfflineProgression.Apply(world, quarry.Id, ticksToApply: 10);

        Assert.Equal(ProductionStopCause.WorkersExhausted, quarry.StopCause);
    }
}
