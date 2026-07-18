using System;

namespace WorldofGoses.Domain;

/// <summary>
/// "Offline progression" = the world catches up to real-time
/// elapsed while the game was closed. This class keeps that logic
/// pure: it computes how many ticks to apply and applies them
/// deterministically, with no Godot or wall-clock dependency. The
/// controller is responsible for passing in <c>DateTimeOffset.UtcNow</c>
/// at load time.
///
/// One production tick advances the world's tick counter by 1,
/// credits the building's storage with the current rate (clamped
/// to capacity), and grants +1 experience to every assigned
/// worker in the building's <see cref="Building.ProducedCompetencyId"/>.
/// </summary>
public static class OfflineProgression
{
    /// <summary>
    /// Default cap on simulated offline time. Even if the player
    /// has been away for a month, this prevents 30 days × 86400 ticks
    /// = 2.6 million useless iterations on the next launch.
    /// </summary>
    public static readonly TimeSpan DefaultCap = TimeSpan.FromDays(7);

    /// <summary>Default tick rate: 1 tick per second of real time.</summary>
    public const double DefaultTickRateHz = 1.0;

    /// <summary>
    /// Computes how many ticks should be applied given the elapsed
    /// wall-clock time between <paramref name="lastSeenAt"/> and
    /// <paramref name="now"/>. Negative or zero elapsed returns 0.
    /// Elapsed exceeding <paramref name="maxOffline"/> is capped.
    /// </summary>
    public static int ComputeTicks(
        DateTimeOffset now,
        DateTimeOffset lastSeenAt,
        TimeSpan? maxOffline = null,
        double tickRateHz = DefaultTickRateHz)
    {
        if (tickRateHz <= 0) return 0;

        // Legacy guard: a save with LastSeenAtUnixMillis = 0 means
        // "no timestamp recorded". Treat as zero elapsed so upgrades
        // don't accidentally apply years of catch-up.
        if (lastSeenAt.ToUnixTimeMilliseconds() <= 0) return 0;

        var elapsed = now - lastSeenAt;
        if (elapsed <= TimeSpan.Zero) return 0;

        var cap = maxOffline ?? DefaultCap;
        var capped = elapsed > cap ? cap : elapsed;
        return (int)(capped.TotalSeconds * tickRateHz);
    }

    /// <summary>
    /// Applies <paramref name="ticksToApply"/> production ticks to
    /// the given building on the given world, returning a report
    /// describing what actually happened. Negative or zero ticks is
    /// a no-op.
    ///
    /// The production rate is constant during offline catch-up
    /// (worker assignment, base rate, and competencies don't change
    /// in this window), so the rate is computed once and
    /// multiplied. Experience grants are batched through
    /// <see cref="CityWorld.AdvanceTicks"/>. Worst case (7-day cap,
    /// 1 Hz) drops from ~1.8 M rate recomputations + ~3.6 M
    /// experience writes to a single rate call + N experience
    /// grants.
    /// </summary>
    public static OfflineProgressionReport Apply(
        CityWorld world,
        BuildingId buildingId,
        int ticksToApply,
        double tickRateHz = DefaultTickRateHz)
    {
        var report = ApplyBuilding(world, buildingId, ticksToApply, tickRateHz);
        world.AdvanceWorldClock(ticksToApply);
        return report;
    }

    public static OfflineProgressionReport ApplyAll(
        CityWorld world,
        int ticksToApply,
        double tickRateHz = DefaultTickRateHz)
    {
        if (ticksToApply <= 0) return OfflineProgressionReport.None;

        int stockAdded = 0;
        int activeTicks = 0;
        foreach (var buildingId in world.Buildings.Keys)
        {
            var report = ApplyBuilding(world, buildingId, ticksToApply, tickRateHz);
            stockAdded += report.StockAdded;
            activeTicks += report.TicksApplied;
        }

        world.AdvanceWorldClock(ticksToApply);
        if (stockAdded == 0) return OfflineProgressionReport.None;

        return new OfflineProgressionReport(
            ticksApplied: activeTicks,
            stockAdded: stockAdded,
            stockWasted: 0,
            simulatedTime: TimeSpan.FromSeconds(ticksToApply / tickRateHz));
    }

    private static OfflineProgressionReport ApplyBuilding(
        CityWorld world,
        BuildingId buildingId,
        int ticksToApply,
        double tickRateHz)
    {
        if (ticksToApply <= 0) return OfflineProgressionReport.None;

        var building = world.GetBuilding(buildingId);
        if (building is null) return OfflineProgressionReport.None;
        if (!building.CanProduce) return OfflineProgressionReport.None;

        var producedPerTick = BuildingProductionCalculator.ProductionPerTick(building, world.Citizens);
        if (producedPerTick <= 0) return OfflineProgressionReport.None;

        int roomToTarget = building.TargetStock - building.Stock;
        int ticksUntilTarget = (roomToTarget + producedPerTick - 1) / producedPerTick;
        int activeTicks = Math.Min(ticksToApply, ticksUntilTarget);
        int totalAdded = building.AddStock(Math.Min(producedPerTick * activeTicks, roomToTarget));
        world.AdvanceBuildingTicks(buildingId, activeTicks);
        var simulatedTime = TimeSpan.FromSeconds(ticksToApply / tickRateHz);

        return new OfflineProgressionReport(
            ticksApplied: activeTicks,
            stockAdded: totalAdded,
            stockWasted: 0,
            simulatedTime: simulatedTime);
    }
}
