#nullable enable

namespace WorldofGoses.Domain;

/// <summary>
/// Current semantic activity derived from durable citizen context. It is not a
/// second state machine and is not persisted: save/load restores the citizen's
/// order, commitment, logical location and transit timing, then this projection
/// explains what those facts mean at the current world tick.
/// </summary>
public enum CitizenRoutineActivity
{
    Leisure = 0,
    Working = 1,
    Resting = 2,
    TravellingToWork = 3,
    TravellingHome = 4,
    WaitingForStorage = 5,
    WaitingForResources = 6,
    WorkplaceIdle = 7,
    OffDuty = 8,
    Recovering = 9,
    OnExpedition = 10,
    Unavailable = 11,
}

public enum CitizenContextLocation
{
    AtShelter = 0,
    AtWorkplace = 1,
    InTransit = 2,
    Unavailable = 3,
}

public enum CitizenRoutineBlockReason
{
    None = 0,
    StorageFull = 1,
    MissingInputs = 2,
    WorkplacePaused = 3,
    OutsideWorkHours = 4,
    Recovering = 5,
    NoFood = 6,
    Wounded = 7,
    NoAssignment = 8,
}

public sealed record CitizenRoutineSnapshot(
    CitizenId CitizenId,
    CitizenRoutineActivity Activity,
    CitizenContextLocation ContextLocation,
    BuildingId? ContextBuildingId,
    BuildingId? ShelterId,
    BuildingId? TransitOriginId,
    BuildingId? TransitDestinationId,
    int? ActivityStartedAtTick,
    int? ExpectedCompletionTick,
    int? NextTransitionTick,
    CitizenRoutineBlockReason BlockReason,
    CitizenBehaviorState Behavior,
    CitizenLocation LogicalLocation,
    CitizenWorkOrder? WorkOrder);
