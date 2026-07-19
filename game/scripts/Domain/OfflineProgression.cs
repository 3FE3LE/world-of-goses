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
/// drains passive upkeep, decrements buff timers, runs day/night
/// behaviour per building (day: produce; night: rest), and (during
/// the day) credits each building's storage with its current rate
/// (clamped to target stock). Granting experience to contributing
/// workers happens inside the same path so live and offline
/// produce identical stamina, food, and stock effects.
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
    /// Applies <paramref name="ticksToApply"/> ticks to the world
    /// for the given building. Each tick is a full world tick
    /// (upkeep + buff decrement + day/night branching), so the
    /// <paramref name="buildingId"/> parameter only controls which
    /// building's production drives the report.
    /// </summary>
    public static OfflineProgressionReport Apply(
        CityWorld world,
        BuildingId buildingId,
        int ticksToApply,
        double tickRateHz = DefaultTickRateHz)
    {
        var report = ApplyBuilding(world, buildingId, ticksToApply, tickRateHz);
        // World clock is already advanced inside AdvanceWorldTick;
        // nothing else to do here.
        _ = tickRateHz; // accepted for API symmetry with ApplyAll
        return report;
    }

    public static OfflineProgressionReport ApplyAll(
        CityWorld world,
        int ticksToApply,
        double tickRateHz = DefaultTickRateHz)
    {
        if (ticksToApply <= 0) return OfflineProgressionReport.None;

        int stockAdded = 0;
        int lastActiveTicks = 0;
        for (int t = 0; t < ticksToApply; t++)
        {
            world.AdvanceWorldTick();
            bool anyProduced = false;
            foreach (var building in world.Buildings.Values)
            {
                if (building.LastTickProduction > 0)
                {
                    stockAdded += building.LastTickProduction;
                    anyProduced = true;
                }
            }
            if (anyProduced) lastActiveTicks = t + 1;
        }

        if (stockAdded == 0) return OfflineProgressionReport.None;

        return new OfflineProgressionReport(
            ticksApplied: lastActiveTicks,
            stockAdded: stockAdded,
            stockWasted: 0,
            simulatedTime: TimeSpan.FromSeconds(ticksToApply / tickRateHz));
    }

    /// <summary>
    /// Per-tick loop on a single building. Tracks the building's
    /// own <c>LastTickProduction</c>; breaks when production stops
    /// (target reached with no upkeep headroom, or workers
    /// exhausted). The world clock is advanced inside each tick.
    /// </summary>
    private static OfflineProgressionReport ApplyBuilding(
        CityWorld world,
        BuildingId buildingId,
        int ticksToApply,
        double tickRateHz)
    {
        if (ticksToApply <= 0) return OfflineProgressionReport.None;

        var building = world.GetBuilding(buildingId);
        if (building is null) return OfflineProgressionReport.None;

        var initialRate = BuildingProductionCalculator.ProductionPerTick(building, world.Citizens);
        if (initialRate <= 0) return OfflineProgressionReport.None;

        int stockAdded = 0;
        int ticksApplied = 0;
        for (int t = 0; t < ticksToApply; t++)
        {
            world.AdvanceWorldTick();
            int produced = building.LastTickProduction;
            if (produced > 0)
            {
                stockAdded += produced;
                ticksApplied++;
                if (building.Stock >= building.TargetStock) break;
            }
            else
            {
                // Nothing added this tick (night, exhausted, paused,
                // full + upkeep making no headroom). Stop counting.
                break;
            }
        }

        var simulatedTime = TimeSpan.FromSeconds(ticksToApply / tickRateHz);
        return new OfflineProgressionReport(
            ticksApplied: ticksApplied,
            stockAdded: stockAdded,
            stockWasted: 0,
            simulatedTime: simulatedTime);
    }
}
