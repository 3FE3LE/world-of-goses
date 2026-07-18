using System;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class OfflineProgressionTests
{
    [Fact]
    public void ApplyAll_AdvancesQuarryAndFarmDuringAbsence()
    {
        var world = new CityWorld();
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
        var world = new CityWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var farm = world.GetBuilding(new BuildingId(2))!;
        farm.ConfigureProductionPolicy(enabled: false, targetStock: farm.StorageCapacity);

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
        var world = new CityWorld();
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
        var world = new CityWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var tickBefore = world.CurrentTick;

        var report = OfflineProgression.Apply(world, buildingId, ticksToApply: -10);

        Assert.Equal(0, report.TicksApplied);
        Assert.Equal(tickBefore, world.CurrentTick);
    }

    [Fact]
    public void Apply_FewTicks_StockAndExperienceAccumulate()
    {
        var world = new CityWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var bran = world.GetCitizen(new CitizenId(1))!;
        var branExpBefore = bran.GetExperience(CompetencyId.Mining);
        var stockBefore = world.PrimaryBuilding.Stock;

        var report = OfflineProgression.Apply(world, buildingId, ticksToApply: 5);

        Assert.Equal(5, report.TicksApplied);
        Assert.True(report.HadProgression);
        Assert.Equal(5, world.CurrentTick);
        Assert.Equal(report.StockAdded, world.PrimaryBuilding.Stock - stockBefore);
        Assert.True(report.StockAdded > 0);
        Assert.Equal(0, report.StockWasted);
        Assert.Equal(branExpBefore + 5, bran.GetExperience(CompetencyId.Mining));
    }

    [Fact]
    public void Apply_ManyTicks_StopsAtTargetWithoutPhantomExperienceOrWaste()
    {
        var world = new CityWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var bran = world.GetCitizen(new CitizenId(1))!;
        var branExpBefore = bran.GetExperience(CompetencyId.Mining);
        var stoneCap = world.PrimaryBuilding.StorageCapacity;

        var report = OfflineProgression.Apply(world, buildingId, ticksToApply: 100);

        Assert.True(report.TicksApplied < 100);
        Assert.Equal(stoneCap, world.PrimaryBuilding.Stock);
        Assert.Equal(stoneCap, report.StockAdded);
        Assert.Equal(0, report.StockWasted);
        Assert.Equal(branExpBefore + report.TicksApplied, bran.GetExperience(CompetencyId.Mining));
    }

    [Fact]
    public void Apply_UnknownBuilding_ReturnsNoneReport()
    {
        var world = new CityWorld();
        var report = OfflineProgression.Apply(world, new BuildingId(999), ticksToApply: 100);
        Assert.False(report.HadProgression);
        Assert.Equal(0, report.TicksApplied);
    }

    [Fact]
    public void Apply_SimulatedTime_MatchesTickRate()
    {
        var world = new CityWorld();
        var buildingId = world.PrimaryBuilding.Id;

        var report = OfflineProgression.Apply(world, buildingId, ticksToApply: 60, tickRateHz: 1.0);
        Assert.Equal(60.0, report.SimulatedTime.TotalSeconds);

        var secondWorld = new CityWorld();
        var report2 = OfflineProgression.Apply(
            secondWorld, secondWorld.PrimaryBuilding.Id, ticksToApply: 60, tickRateHz: 2.0);
        Assert.Equal(30.0, report2.SimulatedTime.TotalSeconds);
    }

    [Fact]
    public void Apply_DisabledPolicy_DoesNotProduce()
    {
        var world = new CityWorld();
        var building = world.PrimaryBuilding;
        building.ConfigureProductionPolicy(enabled: false, targetStock: building.StorageCapacity);

        var report = OfflineProgression.Apply(world, building.Id, ticksToApply: 60);

        Assert.False(report.HadProgression);
        Assert.Equal(0, building.Stock);
    }
}
