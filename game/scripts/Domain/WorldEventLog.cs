#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace WorldofGoses.Domain;

/// <summary>
/// Append-only chronological log of <see cref="WorldEvent"/> records
/// emitted by the simulation. Owned by <see cref="CityWorld"/>; the
/// offline report reads from this log instead of asking the world for
/// aggregate counters so the two never disagree.
///
/// Events are appended in tick order; the log is the source of truth
/// for the causal narrative the player sees after an offline stretch.
/// Persistence decides which events merit long-term retention; this log keeps
/// the complete event stream for the current simulation session.
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
        WorldEventSubject subject,
        int amount = 0,
        WorldEventId? causeEventId = null)
    {
        var id = new WorldEventId(_nextId++);
        var evt = new WorldEvent(id, tick, kind, subject, amount, causeEventId);
        _events.Add(evt);
        return evt;
    }

    /// <summary>Removes every recorded event. Called on world restore.</summary>
    public void Clear()
    {
        _events.Clear();
        _nextId = 1;
    }

    /// <summary>Replaces the session log with validated persisted events.</summary>
    public void Restore(IEnumerable<WorldEvent> events)
    {
        _events.Clear();
        _events.AddRange(events.OrderBy(evt => evt.Id.Value));
        _nextId = _events.Count == 0 ? 1 : _events[^1].Id.Value + 1;
    }
}
