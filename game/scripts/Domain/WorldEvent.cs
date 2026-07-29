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
    ForestDemolished,
    MigrantArrived,
    ExpeditionDispatched,
    ExpeditionReturned,
    ExpeditionFailed,
    ExpeditionCancelled,
    FoodRationShortfall,
    ExpeditionEncounterResolved,
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

public enum WorldEventSubjectKind
{
    World,
    Building,
    ConstructionProject,
    Citizen,
    Expedition,
}

/// <summary>
/// Durable identity of the entity an event concerns. <see cref="DisplayName"/>
/// is a captured label for presentation and is deliberately not used as identity.
/// </summary>
public readonly record struct WorldEventSubject(
    WorldEventSubjectKind Kind,
    int? EntityId,
    string DisplayName)
{
    public static WorldEventSubject World(string displayName) =>
        new(WorldEventSubjectKind.World, null, displayName);

    public static WorldEventSubject Building(BuildingId id, string displayName) =>
        new(WorldEventSubjectKind.Building, id.Value, displayName);

    public static WorldEventSubject ConstructionProject(BuildingId id, string displayName) =>
        new(WorldEventSubjectKind.ConstructionProject, id.Value, displayName);

    public static WorldEventSubject Citizen(CitizenId id, string displayName) =>
        new(WorldEventSubjectKind.Citizen, id.Value, displayName);

    public static WorldEventSubject Expedition(int id, string displayName) =>
        new(WorldEventSubjectKind.Expedition, id, displayName);
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
    public WorldEventSubject Subject { get; }
    public string SubjectName => Subject.DisplayName;
    public int Amount { get; }
    public WorldEventId? CauseEventId { get; }

    public WorldEvent(
        WorldEventId id,
        int tick,
        WorldEventKind kind,
        WorldEventSubject subject,
        int amount,
        WorldEventId? causeEventId)
    {
        Id = id;
        Tick = tick;
        Kind = kind;
        Subject = subject;
        Amount = amount;
        CauseEventId = causeEventId;
    }

}
