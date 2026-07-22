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
///
/// <para>
/// The reactive production policy is a triplet: <see cref="MinStock"/>,
/// <see cref="MaxStock"/>, and <see cref="Priority"/>. The building
/// produces until it reaches <see cref="MaxStock"/>, then stops. It
/// resumes automatically when stock drops to <see cref="MinStock"/>
/// (or below). <see cref="Priority"/> is a sort hint stored today
/// for future auto-assignment; the domain does not act on it.
/// </para>
/// </summary>
public sealed class Building
{
    /// <summary>
    /// Number of consecutive ticks the building has sat at
    /// <see cref="MaxStock"/> without any consumption. When the
    /// counter reaches this value, all assigned workers are released
    /// so they can be re-deployed elsewhere. Shorter than the duration
    /// of a typical production peak in a supply chain with constant
    /// consumption, so a temporary stock spike does not empty the
    /// worksite.
    /// </summary>
    public const int MaxStockReleaseCooldown = 6;

    private readonly List<CitizenId> _assigned = new();
    private readonly List<RecipeInput> _pendingInputs = new();
    private int _maxStockHoldTicks;

    public BuildingId Id { get; }
    public string DisplayName { get; }
    public BuildingKind Kind { get; }
    public ResourceType ProducedResourceType { get; }
    public CompetencyId ProducedCompetencyId { get; }
    public int WorkerCapacity { get; private set; }
    public int VisualCapacity { get; private set; }
    public int BaseProductionPerWorker { get; private set; }
    public int StorageCapacity { get; }
    public int Stock { get; private set; }

    /// <summary>
    /// Separate counter for material inputs the building consumes
    /// (iron, future: fuel, tools). Kept distinct from <see cref="Stock"/>
    /// so the operating-recipe drawdown does not visually shrink the
    /// produced-resource amount the player sees in the HUD. Tests
    /// and seed scenarios populate this via <see cref="DepositIron"/>.
    /// </summary>
    public int IronStock { get; private set; }

    /// <summary>
    /// How much wood is still in the forest. Each <see cref="BuildingKind.Forest"/>
    /// starts with a positive reserve; the hero drains it by gathering.
    /// Kept distinct from <see cref="Stock"/> so the Forest's
    /// remaining-reserve visualisation never overlaps with the
    /// gathered-and-available amount the player can spend on construction.
    /// </summary>
    public int WoodReserve { get; private set; }

    public bool ProductionEnabled { get; private set; }
    public int MinStock { get; private set; }
    public int MaxStock { get; private set; }
    public int Priority { get; private set; }

    /// <summary>
    /// Materials the operating building still owes the city in order
    /// to produce. Empty by default; the simulation populates and
    /// drains this list as inputs are consumed per producing tick.
    /// The presentation layer surfaces this verbatim so the player
    /// can see what is missing.
    /// </summary>
    public IReadOnlyList<RecipeInput> PendingInputs => _pendingInputs;

    /// <summary>
    /// Consecutive ticks the building has been at max stock. Observer
    /// value exposed for tests and the UI; driven by
    /// <see cref="TickMaxStockWatch"/>.
    /// </summary>
    public int MaxStockHoldTicks => _maxStockHoldTicks;

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
        bool productionEnabled = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(workerCapacity);
        ArgumentOutOfRangeException.ThrowIfNegative(visualCapacity);
        ArgumentOutOfRangeException.ThrowIfNegative(baseProductionPerWorker);
        ArgumentOutOfRangeException.ThrowIfNegative(storageCapacity);
        if (initialStock < 0 || initialStock > storageCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(initialStock));
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
        MinStock = 0;
        MaxStock = storageCapacity;
        Priority = 0;
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

    /// <summary>
    /// Replace the worker-capacity triplet for a Forest whose old save
    /// was produced before the wood-gathering slice landed. Old saves
    /// serialised Forest with all three set to 0 (a marker for
    /// "non-productive in v2"); the migration layer uses this method
    /// to bump those defaults so a freshly-loaded city can assign
    /// workers. Existing citizens assigned to this forest are kept on
    /// the roster up to the new capacity — if the new capacity is
    /// smaller, the most-recently-assigned citizen stays (any other
    /// excess assignments would have to be cleaned up before load).
    /// </summary>
    public void ReplaceForestCapacity(int workerCapacity, int visualCapacity, int baseProductionPerWorker)
    {
        if (Kind != BuildingKind.Forest)
        {
            throw new InvalidOperationException(
                "ReplaceForestCapacity only applies to Forest-kind buildings.");
        }
        WorkerCapacitySetter(workerCapacity, visualCapacity, baseProductionPerWorker);
    }

    /// <summary>
    /// Internal setter for the production triplet. Avoids the readonly
    /// field restriction in Building's constructor by giving the
    /// migration path a write-through on the values it needs to
    /// correct on restore.
    /// </summary>
    private void WorkerCapacitySetter(int workerCapacity, int visualCapacity, int baseProductionPerWorker)
    {
        WorkerCapacity = workerCapacity;
        VisualCapacity = visualCapacity;
        BaseProductionPerWorker = baseProductionPerWorker;
    }

    /// <summary>
    /// Produces when authorised, has at least one worker, and has
    /// room below <see cref="MaxStock"/>. The reactive resume from
    /// <see cref="MinStock"/> is handled by <see cref="ResumeIfBelowMin"/>:
    /// once stock falls to or below the minimum, the world tick
    /// calls this to clear the <see cref="ProductionStopCause.TargetReached"/>
    /// sentinel so the next tick produces again.
    /// </summary>
    public bool CanProduce =>
        ProductionEnabled && AssignedCount > 0 && Stock < MaxStock;

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

    /// <summary>
    /// Configures the reactive production policy. Validation:
    /// <c>0 &lt;= minStock &lt;= maxStock &lt;= StorageCapacity</c> and
    /// <c>priority &gt;= 0</c>. <see cref="MinStock"/> equal to
    /// <see cref="MaxStock"/> is allowed: the building oscillates
    /// between full and one-below-full each tick.
    /// </summary>
    public void ConfigureProductionPolicy(bool enabled, int minStock, int maxStock, int priority)
    {
        if (minStock < 0 || minStock > StorageCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(minStock));
        }
        if (maxStock < 0 || maxStock > StorageCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(maxStock));
        }
        if (minStock > maxStock)
        {
            throw new ArgumentOutOfRangeException(nameof(minStock),
                $"MinStock ({minStock}) cannot exceed MaxStock ({maxStock}).");
        }
        if (priority < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        ProductionEnabled = enabled;
        MinStock = minStock;
        MaxStock = maxStock;
        Priority = priority;
    }

    /// <summary>
    /// Called by the world tick when stock has fallen to or below
    /// <see cref="MinStock"/>. Clears the <see cref="ProductionStopCause.TargetReached"/>
    /// sentinel so the building can produce again next tick. No-op
    /// when the building is currently stopped for another reason.
    /// </summary>
    internal void ResumeIfBelowMin()
    {
        if (StopCause == ProductionStopCause.TargetReached)
        {
            StopCause = ProductionStopCause.NoWorkers;
        }
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

    /// <summary>
    /// Adds the given iron to the input reserve. Used by tests to
    /// seed scenarios and by the construction-cancellation refund.
    /// </summary>
    public void DepositIron(int amount)
    {
        if (amount <= 0) return;
        IronStock += amount;
    }

    /// <summary>
    /// Consumes iron from the input reserve. Returns <c>false</c>
    /// when the building does not hold enough.
    /// </summary>
    public bool TryConsumeIron(int amount)
    {
        if (amount < 0) return false;
        if (IronStock < amount) return false;
        IronStock -= amount;
        return true;
    }

    /// <summary>
    /// Sets the forest's starting wood reserve. Used by seed scenarios
    /// (the founding hero world places two Forests with this much
    /// wood still in them).
    /// </summary>
    public void SeedWoodReserve(int amount)
    {
        if (amount < 0) return;
        WoodReserve = amount;
    }

    /// <summary>
    /// Drains <paramref name="amount"/> wood from the reserve without
    /// touching <see cref="Stock"/>. Called by the world tick after
    /// it has decided how much wood the assigned workers foraged this
    /// tick — the <see cref="CityWorld"/> routes the same amount into
    /// <see cref="AddStock"/> as the produced output.
    /// </summary>
    public void DecrementWoodReserve(int amount)
    {
        if (amount <= 0) return;
        int actual = amount < WoodReserve ? amount : WoodReserve;
        WoodReserve -= actual;
    }

    /// <summary>
    /// Drains wood from the forest's remaining reserve and credits
    /// it to <see cref="Stock"/> (which the construction recipe gate
    /// then consumes). Returns the amount actually gathered, which
    /// may be less than <paramref name="amount"/> when the reserve
    /// runs dry.
    /// </summary>
    public int GatherWood(int amount)
    {
        if (amount <= 0) return 0;
        int available = WoodReserve < amount ? WoodReserve : amount;
        if (available <= 0) return 0;
        WoodReserve -= available;
        int room = StorageCapacity - Stock;
        int added = available < room ? available : room;
        Stock += added;
        return added;
    }

    /// <summary>
    /// Replaces the pending-inputs list verbatim. Used by the
    /// simulation after the per-tick drawdown step so the UI always
    /// reflects what is still owed.
    /// </summary>
    internal void SetPendingInputs(IEnumerable<RecipeInput> inputs)
    {
        _pendingInputs.Clear();
        foreach (var input in inputs)
        {
            _pendingInputs.Add(input);
        }
    }

    /// <summary>
    /// Tracks consecutive ticks at <see cref="MaxStock"/> and returns
    /// <c>true</c> once the counter reaches
    /// <see cref="MaxStockReleaseCooldown"/>. The check is done after
    /// production so the watch fires whether the production is
    /// supply-chain-balanced (consumption offset by production) or
    /// supply-chain-absent (no consumption at all). The call sites are
    /// responsible for resetting the counter when the production
    /// stream pauses (e.g. workers exhausted, building paused).
    /// </summary>
    public bool TickMaxStockWatch()
    {
        if (Stock < MaxStock)
        {
            _maxStockHoldTicks = 0;
            return false;
        }
        _maxStockHoldTicks++;
        return _maxStockHoldTicks >= MaxStockReleaseCooldown;
    }
}