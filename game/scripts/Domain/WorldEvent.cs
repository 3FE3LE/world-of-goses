#nullable enable
using Godot;

namespace WorldofGoses.Domain;

/// <summary>
/// Categorical kind for a <see cref="WorldEvent"/>. Drives the icon
/// used in the offline report panel and the colour hint the panel
/// applies to the row. Keep this list small and additive — every
/// new kind is a new player-visible event class.
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

    /// <summary>
    /// Icon path used by the offline report panel for this kind.
    /// Returns null for kinds the panel does not yet render.
    /// </summary>
    public string? IconPath => Kind switch
    {
        WorldEventKind.StockProduced => "res://assets/ui/icons/24/coin.svg",
        WorldEventKind.StockCapped => "res://assets/ui/icons/24/check.svg",
        WorldEventKind.WorkersExhausted => "res://assets/ui/icons/24/warning.svg",
        WorldEventKind.WorkerRecovered => "res://assets/ui/icons/24/heart.svg",
        WorldEventKind.DayBegan => "res://assets/ui/icons/24/sun.svg",
        WorldEventKind.NightBegan => "res://assets/ui/icons/24/moon.svg",
        WorldEventKind.ProjectProgressed => "res://assets/ui/icons/24/building.svg",
        WorldEventKind.ProjectPaused => "res://assets/ui/icons/24/pause.svg",
        WorldEventKind.ProjectResumed => "res://assets/ui/icons/24/play.svg",
        WorldEventKind.ProjectCompleted => "res://assets/ui/icons/24/check.svg",
        WorldEventKind.BuildingCreated => "res://assets/ui/icons/24/house.svg",
        WorldEventKind.WellFedExpired => "res://assets/ui/icons/24/clock.svg",
        _ => null,
    };
}