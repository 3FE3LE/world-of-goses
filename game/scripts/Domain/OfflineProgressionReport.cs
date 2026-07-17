using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Output of <see cref="OfflineProgression.Apply"/>: how many
/// ticks were applied, how much stock actually landed in storage,
/// how much was produced-but-clamped, and how long the world
/// simulated.
/// </summary>
public sealed class OfflineProgressionReport
{
    public static readonly OfflineProgressionReport None = new(0, 0, 0, TimeSpan.Zero);

    public int TicksApplied { get; }
    public int StockAdded { get; }
    public int StockWasted { get; }
    public TimeSpan SimulatedTime { get; }

    public bool HadProgression => TicksApplied > 0;

    public OfflineProgressionReport(
        int ticksApplied,
        int stockAdded,
        int stockWasted,
        TimeSpan simulatedTime)
    {
        TicksApplied = ticksApplied;
        StockAdded = stockAdded;
        StockWasted = stockWasted;
        SimulatedTime = simulatedTime;
    }
}
