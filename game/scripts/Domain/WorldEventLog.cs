#nullable enable
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Append-only chronological log of <see cref="WorldEvent"/> records
/// emitted by the simulation. Owned by <see cref="CityWorld"/>; the
/// offline report reads from this log instead of asking the world for
/// aggregate counters so the two never disagree.
///
/// Events are appended in tick order; the log is the source of truth
/// for the causal narrative the player sees after an offline stretch.
/// The log is reset whenever the world is restored from persistence
/// (the offline catch-up repopulates it from scratch).
/// </summary>
public sealed class WorldEventLog
{
    private readonly List<WorldEvent> _events = new();
    private int _nextId = 1;

    /// <summary>Read-only view of every event recorded so far.</summary>
    public IReadOnlyList<WorldEvent> Events => _events;

    /// <summary>Appends a new event and returns it.</summary>
    public WorldEvent Record(
        int tick,
        WorldEventKind kind,
        string subjectName,
        int amount = 0,
        string? causeEventId = null)
    {
        var id = new WorldEventId(_nextId++);
        var summary = Summarise(kind, subjectName, amount);
        var evt = new WorldEvent(id, tick, kind, subjectName, amount, causeEventId, summary);
        _events.Add(evt);
        return evt;
    }

    /// <summary>Removes every recorded event. Called on world restore.</summary>
    public void Clear()
    {
        _events.Clear();
        _nextId = 1;
    }

    private static string Summarise(WorldEventKind kind, string subjectName, int amount) => kind switch
    {
        WorldEventKind.StockProduced => $"{subjectName} produced +{amount}",
        WorldEventKind.StockCapped => $"{subjectName} reached target stock",
        WorldEventKind.WorkersExhausted => $"{subjectName} stopped: workers exhausted",
        WorldEventKind.WorkerRecovered => $"{subjectName} resumed: workers recovered",
        WorldEventKind.DayBegan => "Sun rose — workers mobilised to their stations",
        WorldEventKind.NightBegan => "Sun set — workers returned home to rest",
        WorldEventKind.ProjectProgressed => $"{subjectName} made +{amount} work",
        WorldEventKind.ProjectPaused => $"{subjectName} paused by the player",
        WorldEventKind.ProjectResumed => $"{subjectName} resumed by the player",
        WorldEventKind.ProjectCompleted => $"{subjectName} completed",
        WorldEventKind.BuildingCreated => $"{subjectName} became a building",
        WorldEventKind.WellFedExpired => $"{subjectName} lost the WellFed buff",
        WorldEventKind.ProductionBlocked => $"{subjectName} waiting: missing inputs",
        _ => subjectName,
    };
}