#nullable enable
namespace WorldofGoses.Domain;

/// <summary>
/// Categorical kind for a <see cref="WorldEvent"/>. Presentation may
/// map these semantic values to icons and colours, but the domain does
/// not carry asset paths or engine-specific data.
/// </summary>
public enum WorldEventKind
{
    StockProduced,
    StockCapped,
    WorkersExhausted,
    WorkerRecovered,
    DayBegan,
    NightBegan,
    ProjectProgressed,
    ProjectPaused,
    ProjectResumed,
    ProjectCompleted,
    BuildingCreated,
    WellFedExpired,
    ProductionBlocked,
}

/// <summary>
/// Opaque identifier for an event in the log. Returned as a string
/// form via <see cref="ToString"/> so the offline report can carry
/// causal links back to earlier events without exposing the integer.
/// </summary>
public readonly record struct WorldEventId(int Value)
{
    public override string ToString() => $"evt-{Value:D4}";
}

/// <summary>
/// One discrete fact the world produced at a specific tick. Events
/// are the source of truth for the offline report and the future
/// causal log; the aggregate counters in
/// <see cref="OfflineProgressionReport"/> are derived from this list
/// so the two never disagree.
///
/// Events are produced by <see cref="CityWorld.AdvanceWorldTick"/> as
/// side effects of the simulation; they are intentionally
/// immutable so consumers can sort, filter, and re-emit them
/// without coordinating with the producer.
/// </summary>
public sealed class WorldEvent
{
    public WorldEventId Id { get; }
    public int Tick { get; }
    public WorldEventKind Kind { get; }
    public string SubjectName { get; }
    public int Amount { get; }
    public string? CauseEventId { get; }
    public string Summary { get; }

    public WorldEvent(
        WorldEventId id,
        int tick,
        WorldEventKind kind,
        string subjectName,
        int amount,
        string? causeEventId,
        string summary)
    {
        Id = id;
        Tick = tick;
        Kind = kind;
        SubjectName = subjectName;
        Amount = amount;
        CauseEventId = causeEventId;
        Summary = summary;
    }

}
