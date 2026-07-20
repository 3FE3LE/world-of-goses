using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Output of <see cref="OfflineProgression.ApplyAll"/>: aggregate
/// counters derived from the world's <see cref="WorldEventLog"/>
/// plus the chronological list of events so the presentation layer
/// can render either a one-line banner or a full causal timeline.
///
/// The aggregate counts exist for legacy callers and as a quick
/// summary; the source of truth is the <see cref="Events"/> list.
/// </summary>
public sealed class OfflineProgressionReport
{
    public static readonly OfflineProgressionReport None = new(
        0, 0, 0, TimeSpan.Zero, Array.Empty<WorldEvent>());

    public int TicksApplied { get; }
    public int StockAdded { get; }
    public int StockWasted { get; }
    public TimeSpan SimulatedTime { get; }
    public IReadOnlyList<WorldEvent> Events { get; }

    public bool HadProgression => TicksApplied > 0 || Events.Count > 0;

    public OfflineProgressionReport(
        int ticksApplied,
        int stockAdded,
        int stockWasted,
        TimeSpan simulatedTime,
        IReadOnlyList<WorldEvent> events)
    {
        TicksApplied = ticksApplied;
        StockAdded = stockAdded;
        StockWasted = stockWasted;
        SimulatedTime = simulatedTime;
        Events = events;
    }
}