#nullable enable
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// A worksite whose single mechanical state is the cumulative
/// progress toward the finished building. The
/// <see cref="Id"/> is reserved for the building that will appear
/// once the project completes, so the presentation layer can keep
/// one <c>BuildingId</c> across both lifecycles.
/// </summary>
public sealed class ConstructionProject
{
    private readonly List<CitizenId> _assigned = new();

    public ConstructionProject(
        BuildingId id,
        ConstructionKind kind,
        string displayName,
        int requiredWork,
        int workerCapacity,
        bool enabled = true)
    {
        Id = id;
        Kind = kind;
        DisplayName = displayName;
        RequiredWork = requiredWork;
        WorkerCapacity = workerCapacity;
        Enabled = enabled;
    }

    public BuildingId Id { get; }
    public ConstructionKind Kind { get; }
    public string DisplayName { get; }
    public int Progress { get; internal set; }
    public int RequiredWork { get; }
    public int WorkerCapacity { get; }
    public bool Enabled { get; internal set; }
    public int LastTickProgressAdded { get; internal set; }
    public ConstructionStopCause StopCause { get; internal set; } = ConstructionStopCause.NoWorkers;

    public IReadOnlyList<CitizenId> AssignedCitizenIds => _assigned;
    public int AssignedCount => _assigned.Count;

    public bool IsComplete => Progress >= RequiredWork;
    public bool IsAtWorkerCapacity => _assigned.Count >= WorkerCapacity;

    public bool IsAssigned(CitizenId citizenId)
    {
        for (int i = 0; i < _assigned.Count; i++)
        {
            if (_assigned[i] == citizenId) return true;
        }
        return false;
    }

    internal bool TryAssign(CitizenId citizenId)
    {
        if (IsAssigned(citizenId)) return false;
        if (_assigned.Count >= WorkerCapacity) return false;
        if (citizenId == default) return false;
        _assigned.Add(citizenId);
        return true;
    }

    internal bool TryUnassign(CitizenId citizenId)
    {
        for (int i = 0; i < _assigned.Count; i++)
        {
            if (_assigned[i] == citizenId)
            {
                _assigned.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
}
