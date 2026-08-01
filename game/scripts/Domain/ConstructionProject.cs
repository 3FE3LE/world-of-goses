#nullable enable
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// A worksite whose single mechanical state is the cumulative
/// progress toward the finished building. The
/// <see cref="Id"/> is reserved for the building that will appear
/// once the project completes, so the presentation layer can keep
/// one <c>BuildingId</c> across both lifecycles.
///
/// <para>
/// <see cref="RemainingInputs"/> tracks the recipe inputs the city
/// still owes the worksite after the up-front deposit was paid. The
/// simulation drains 1 unit per input per <see cref="ConstructionRules.WorkIntervalTicks"/>
/// while the project is active.
/// </para>
/// </summary>
public sealed class ConstructionProject
{
    private readonly List<CitizenId> _assigned = new();
    private readonly List<RecipeInput> _remainingInputs = new();
    private readonly List<RecipeInput> _depositedInputs = new();
    private readonly List<FoundingSiteModule> _completedFoundingModules = new();

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
    public int RequiredWork { get; private set; }
    public int WorkerCapacity { get; }
    public bool Enabled { get; internal set; }
    public int LastTickProgressAdded { get; internal set; }
    public ConstructionStopCause StopCause { get; internal set; } = ConstructionStopCause.NoWorkers;
    public FoundingSiteModule? ActiveFoundingModule { get; private set; }
    public int PhaseStartedAtTick { get; private set; }

    public IReadOnlyList<CitizenId> AssignedCitizenIds => _assigned;
    public int AssignedCount => _assigned.Count;

    /// <summary>
    /// Inputs the city still owes this worksite after the deposit
    /// has been debited. The simulation drains one unit per entry
    /// per work interval; on completion the residue is discarded.
    /// </summary>
    public IReadOnlyList<RecipeInput> RemainingInputs => _remainingInputs;
    public IReadOnlyList<RecipeInput> DepositedInputs => _depositedInputs;
    public IReadOnlyList<FoundingSiteModule> CompletedFoundingModules => _completedFoundingModules;

    public bool IsComplete => Kind != ConstructionKind.FoundingSite
        ? Progress >= RequiredWork
        : ActiveFoundingModule == FoundingSiteModule.Canopy && Progress >= RequiredWork;
    public bool HasActiveWork => Kind != ConstructionKind.FoundingSite || ActiveFoundingModule.HasValue;
    public bool IsAtWorkerCapacity => _assigned.Count >= WorkerCapacity;

    public bool IsAssigned(CitizenId citizenId)
    {
        for (int i = 0; i < _assigned.Count; i++)
        {
            if (_assigned[i] == citizenId) return true;
        }
        return false;
    }

    /// <summary>
    /// The <see cref="BuildingKind"/> this project produces on
    /// completion. The mapping is owned by the domain so the
    /// presentation layer never has to translate construction kinds
    /// into building kinds on its own. Adding a new
    /// <see cref="ConstructionKind"/> requires updating this switch.
    /// </summary>
    public BuildingKind ResultingKind => Kind switch
    {
        ConstructionKind.BasicShelter => BuildingKind.Home,
        ConstructionKind.Farm => BuildingKind.Farm,
        ConstructionKind.Quarry => BuildingKind.Quarry,
        ConstructionKind.TownHall => BuildingKind.TownHall,
        ConstructionKind.FoundingSite => BuildingKind.Home,
        ConstructionKind.CultivationSite => BuildingKind.CultivationSite,
        _ => BuildingKind.Home,
    };

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

    /// <summary>
    /// Replaces the remaining-inputs snapshot verbatim. Called by
    /// the simulation after the per-work-interval drawdown so the
    /// UI always reflects what is still owed.
    /// </summary>
    internal void SetRemainingInputs(IEnumerable<RecipeInput> inputs)
    {
        _remainingInputs.Clear();
        foreach (var input in inputs)
        {
            _remainingInputs.Add(input);
        }
    }

    public bool HasCompletedFoundingModule(FoundingSiteModule module) =>
        _completedFoundingModules.Contains(module);

    public bool CanStartFoundingModule(FoundingSiteModule module) =>
        Kind == ConstructionKind.FoundingSite
        && ActiveFoundingModule is null
        && !HasCompletedFoundingModule(module)
        && FoundingSiteRules.PrerequisitesMet(module, HasCompletedFoundingModule);

    internal bool BeginFoundingModule(
        FoundingSiteModule module,
        int startedAtTick,
        IEnumerable<RecipeInput> depositedInputs)
    {
        if (!CanStartFoundingModule(module)) return false;
        ActiveFoundingModule = module;
        PhaseStartedAtTick = startedAtTick;
        Progress = 0;
        RequiredWork = FoundingSiteRules.WorkPerModule;
        LastTickProgressAdded = 0;
        Enabled = true;
        StopCause = AssignedCount == 0
            ? ConstructionStopCause.NoWorkers
            : ConstructionStopCause.Authorized;
        _remainingInputs.Clear();
        _depositedInputs.Clear();
        _depositedInputs.AddRange(depositedInputs);
        return true;
    }

    internal FoundingSiteModule? CompleteActiveFoundingModule()
    {
        if (Kind != ConstructionKind.FoundingSite
            || ActiveFoundingModule is not FoundingSiteModule module
            || Progress < RequiredWork)
        {
            return null;
        }
        if (!_completedFoundingModules.Contains(module))
        {
            _completedFoundingModules.Add(module);
        }
        ActiveFoundingModule = null;
        Progress = 0;
        LastTickProgressAdded = 0;
        _remainingInputs.Clear();
        StopCause = ConstructionStopCause.AwaitingModule;
        return module;
    }

    internal void RestoreFoundingState(
        FoundingSiteModule? activeModule,
        IEnumerable<FoundingSiteModule> completedModules,
        int phaseStartedAtTick,
        IEnumerable<RecipeInput> depositedInputs)
    {
        ActiveFoundingModule = activeModule;
        PhaseStartedAtTick = phaseStartedAtTick;
        _completedFoundingModules.Clear();
        foreach (FoundingSiteModule module in completedModules)
        {
            if (!_completedFoundingModules.Contains(module))
            {
                _completedFoundingModules.Add(module);
            }
        }
        _depositedInputs.Clear();
        _depositedInputs.AddRange(depositedInputs);
        if (Kind == ConstructionKind.FoundingSite && activeModule is null)
        {
            StopCause = ConstructionStopCause.AwaitingModule;
        }
    }
}
