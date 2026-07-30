using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Temporary inspection surface for routine/load issues. Contains semantic
/// IDs and ticks only; no scene node or coordinate can become authoritative.
/// </summary>
public sealed record CitizenDebugSnapshot(
    CitizenId CitizenId,
    string Name,
    CitizenRoutineSnapshot Routine,
    BuildingId? AssignedBuildingId,
    BuildingId? ShelterId,
    bool IsWorkday,
    long LastSimulationProcessedAtUnixMillis);
