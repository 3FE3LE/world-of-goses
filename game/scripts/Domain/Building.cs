using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// A city building that produces a single resource. A building
/// carries TWO independent classifications as data:
///
/// 1. <see cref="Kind"/> — its architectural/usage type (quarry,
///    farm, smithy, future: potion lab, weaving hall, etc.).
/// 2. <see cref="ProducedResourceType"/> — what it puts out
///    (stone, food, iron, future: potions, cloth, etc.).
///
/// Decoupling these means future slices can introduce new kinds or
/// new resources without touching this class — the values flow in
/// at construction time.
///
/// The display labels (<see cref="ResourceLabel"/>,
/// <see cref="ResourceUnit"/>) are also data, set per-building in
/// the seed/factory.
/// </summary>
public sealed class Building
{
    private readonly List<CitizenId> _assigned = new();

    public BuildingId Id { get; }
    public string DisplayName { get; }
    public BuildingKind Kind { get; }
    public ResourceType ProducedResourceType { get; }
    public CompetencyId ProducedCompetencyId { get; }
    public int WorkerCapacity { get; }
    public int VisualCapacity { get; }
    public int BaseProductionPerWorker { get; }
    public int StorageCapacity { get; }
    public int Stock { get; private set; }
    public bool ProductionEnabled { get; private set; }
    public int TargetStock { get; private set; }

    public string ResourceLabel { get; }
    public string ResourceUnit { get; }

    public IReadOnlyList<CitizenId> AssignedCitizenIds => _assigned;
    public int AssignedCount => _assigned.Count;
    public bool IsAtWorkerCapacity => _assigned.Count >= WorkerCapacity;

    public Building(
        BuildingId id,
        string displayName,
        BuildingKind kind,
        ResourceType producedResourceType,
        CompetencyId producedCompetencyId,
        int workerCapacity,
        int visualCapacity,
        int baseProductionPerWorker,
        int storageCapacity,
        string resourceLabel,
        string resourceUnit,
        int initialStock = 0,
        bool productionEnabled = true,
        int? targetStock = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(workerCapacity);
        ArgumentOutOfRangeException.ThrowIfNegative(visualCapacity);
        ArgumentOutOfRangeException.ThrowIfNegative(baseProductionPerWorker);
        ArgumentOutOfRangeException.ThrowIfNegative(storageCapacity);
        if (initialStock < 0 || initialStock > storageCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(initialStock));
        }
        int resolvedTargetStock = targetStock ?? storageCapacity;
        if (resolvedTargetStock < 0 || resolvedTargetStock > storageCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(targetStock));
        }

        Id = id;
        DisplayName = displayName;
        Kind = kind;
        ProducedResourceType = producedResourceType;
        ProducedCompetencyId = producedCompetencyId;
        WorkerCapacity = workerCapacity;
        VisualCapacity = visualCapacity;
        BaseProductionPerWorker = baseProductionPerWorker;
        StorageCapacity = storageCapacity;
        ResourceLabel = resourceLabel;
        ResourceUnit = resourceUnit;
        Stock = initialStock;
        ProductionEnabled = productionEnabled;
        TargetStock = resolvedTargetStock;
    }

    /// <summary>
    /// "Quarry (Stone)" or "Farm (Food)" — combines the building's
    /// human name with its produced resource label so each
    /// instance has a distinct identity.
    /// </summary>
    public string FullDisplayLabel => $"{DisplayName} ({ResourceLabel})";

    public int VisibleWorkerCount =>
        _assigned.Count < VisualCapacity ? _assigned.Count : VisualCapacity;

    public int HiddenWorkerCount =>
        _assigned.Count > VisualCapacity ? _assigned.Count - VisualCapacity : 0;

    public bool IsAssigned(CitizenId citizenId) =>
        _assigned.Contains(citizenId);

    public bool CanProduce =>
        ProductionEnabled && AssignedCount > 0 && Stock < TargetStock;

    /// <summary>
    /// Reason the building produced zero (or no attempt was made) on
    /// the last tick. Set by <see cref="CityWorld"/> after each tick;
    /// the presentation layer reads it to explain state. Defaults to
    /// <see cref="ProductionStopCause.NoWorkers"/> until the first
    /// tick runs, which overwrites it.
    /// </summary>
    public ProductionStopCause StopCause { get; internal set; } = ProductionStopCause.NoWorkers;

    /// <summary>
    /// How many stone (or food) units this building actually added
    /// to storage on the most recent tick. Zero on nights, on
    /// non-productive days, or when production was capped at zero
    /// by room. Used by <see cref="OfflineProgression"/> to track
    /// per-tick production without diffing stock around upkeep.
    /// </summary>
    public int LastTickProduction { get; internal set; }

    public void ConfigureProductionPolicy(bool enabled, int targetStock)
    {
        if (targetStock < 0 || targetStock > StorageCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(targetStock));
        }

        ProductionEnabled = enabled;
        TargetStock = targetStock;
    }

    internal AssignmentResult TryAssign(CitizenId citizenId)
    {
        if (IsAssigned(citizenId))
        {
            return AssignmentResult.Fail(AssignmentOutcome.AlreadyAssigned, citizenId, Id);
        }
        if (IsAtWorkerCapacity)
        {
            return AssignmentResult.Fail(AssignmentOutcome.AtCapacity, citizenId, Id);
        }
        _assigned.Add(citizenId);
        return AssignmentResult.Ok(citizenId, Id);
    }

    internal AssignmentResult TryUnassign(CitizenId citizenId)
    {
        var index = _assigned.IndexOf(citizenId);
        if (index < 0)
        {
            return AssignmentResult.Fail(AssignmentOutcome.NotAssigned, citizenId, Id);
        }
        _assigned.RemoveAt(index);
        return AssignmentResult.Ok(citizenId, Id);
    }

    public int AddStock(int amount)
    {
        if (amount <= 0) return 0;
        int room = StorageCapacity - Stock;
        int added = amount < room ? amount : room;
        Stock += added;
        return added;
    }

    public bool TryConsumeStock(int amount)
    {
        if (amount < 0) return false;
        if (Stock < amount) return false;
        Stock -= amount;
        return true;
    }
}
