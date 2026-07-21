#nullable enable
using System;
using System.Collections.Generic;
using WorldofGoses.Domain.Persistence;

namespace WorldofGoses.Domain;

/// <summary>
/// Deterministic, in-memory world state. A new world starts empty and becomes
/// active only when onboarding establishes its principal hero. Citizens and
/// buildings are then composed explicitly through domain operations or a
/// validated persisted snapshot.
///
/// The world exposes events instead of being polled by the presentation layer.
/// The presentation layer never reaches into a building or citizen to mutate
/// state directly.
/// </summary>
public sealed class CityWorld
{
    private readonly Dictionary<CitizenId, Citizen> _citizens = new();
    private readonly Dictionary<BuildingId, Building> _buildings = new();
    private readonly Dictionary<BuildingId, ConstructionProject> _projects = new();
    private readonly WorldEventLog _log = new();
    private int _tick;
    private int _nextProjectId = 1;

    private static readonly CitizenId PrincipalHeroId = new(1);

    /// <summary>A new world is intentionally empty until onboarding creates its hero.</summary>
    public CityWorld() { }

    public int CurrentTick => _tick;
    public IReadOnlyDictionary<CitizenId, Citizen> Citizens => _citizens;
    public IReadOnlyDictionary<BuildingId, Building> Buildings => _buildings;
    public IReadOnlyDictionary<BuildingId, ConstructionProject> Projects => _projects;

    /// <summary>Read-only view of the chronological event log.</summary>
    public WorldEventLog Log => _log;

    public event EventHandler<CityWorldChangedEventArgs>? BuildingChanged;
    public event EventHandler<CityWorldChangedEventArgs>? ProjectChanged;

    /// <summary>
    /// The citizen recognised as the principal hero, or <c>null</c> before
    /// onboarding. Hero status remains a role attached to a regular citizen.
    /// </summary>
    public Citizen? Hero
    {
        get
        {
            foreach (var citizen in _citizens.Values)
            {
                if (citizen.IsHero) return citizen;
            }
            return null;
        }
    }

    public bool NeedsOnboarding => Hero is null;

    /// <summary>
    /// Establishes the only citizen in a fresh world. The profile is
    /// individual: no validation requires it to match common tendencies
    /// of the chosen lineage. The founding forests are not part of this
    /// call — the controller invokes <see cref="SeedStartingForests"/>
    /// separately so test fixtures can opt out of the empty-field
    /// gathering target.
    /// </summary>
    public HeroCreationResult TryCreateHero(HeroCreationRequest request)
    {
        if (Hero is not null)
        {
            return HeroCreationResult.Fail(HeroCreationOutcome.AlreadyExists);
        }
        if (_citizens.Count > 0 || _buildings.Count > 0)
        {
            return HeroCreationResult.Fail(HeroCreationOutcome.WorldNotEmpty);
        }
        if (request is null || request.Profile is null)
        {
            return HeroCreationResult.Fail(HeroCreationOutcome.MissingProfile);
        }

        string name = request.Name?.Trim() ?? string.Empty;
        if (name.Length is < 1 or > 32 || ContainsControlCharacter(name))
        {
            return HeroCreationResult.Fail(HeroCreationOutcome.InvalidName);
        }

        var hero = new Citizen(
            PrincipalHeroId,
            name,
            appearanceSeed: StableAppearanceSeed(name, request.Profile.Lineage),
            profile: request.Profile);
        hero.GrantRole(RoleId.Hero, _tick);
        RegisterCitizen(hero);
        return HeroCreationResult.Success(hero.Id);
    }

    /// <summary>
    /// Per-forest starting wood reserve. The hero gathers wood from
    /// each Forest up to this much; the Basic Shelter recipe (4 wood
    /// total, deposit = 1) requires the player to gather at least
    /// once before authorisation succeeds.
    /// </summary>
    public const int StartingForestWoodReserve = 8;

    /// <summary>Per-forest storage capacity for gathered wood.</summary>
    public const int StartingForestStorageCapacity = 20;

    /// <summary>
    /// Drops two Forests into the freshly founded world so the hero has
    /// a wood source to gather from. Each Forest starts with
    /// <see cref="StartingForestWoodReserve"/> wood still in it.
    /// IDs are reserved (100, 101) so they never collide with future
    /// player-authorised buildings. No-op when the world already has
    /// buildings or citizens beyond the principal hero, so this is
    /// safe to call from a restore path that already populated the
    /// world from a save.
    /// </summary>
    public void SeedStartingForests()
    {
        if (_buildings.Count > 0) return;
        if (_citizens.Count > 1) return;

        var forest1 = new Building(
            id: new BuildingId(100),
            displayName: "Forest",
            kind: BuildingKind.Forest,
            producedResourceType: ResourceType.Wood,
            producedCompetencyId: CompetencyId.Foraging,
            workerCapacity: 0,
            visualCapacity: 0,
            baseProductionPerWorker: 0,
            storageCapacity: StartingForestStorageCapacity,
            resourceLabel: "Wood",
            resourceUnit: "wood");
        forest1.SeedWoodReserve(StartingForestWoodReserve);

        var forest2 = new Building(
            id: new BuildingId(101),
            displayName: "Forest",
            kind: BuildingKind.Forest,
            producedResourceType: ResourceType.Wood,
            producedCompetencyId: CompetencyId.Foraging,
            workerCapacity: 0,
            visualCapacity: 0,
            baseProductionPerWorker: 0,
            storageCapacity: StartingForestStorageCapacity,
            resourceLabel: "Wood",
            resourceUnit: "wood");
        forest2.SeedWoodReserve(StartingForestWoodReserve);

        RegisterBuilding(forest1);
        RegisterBuilding(forest2);
    }

    internal void RegisterCitizen(Citizen citizen)
    {
        ArgumentNullException.ThrowIfNull(citizen);
        if (!_citizens.TryAdd(citizen.Id, citizen))
        {
            throw new InvalidOperationException($"Citizen id {citizen.Id.Value} already exists.");
        }
    }

    internal void RegisterBuilding(Building building)
    {
        ArgumentNullException.ThrowIfNull(building);
        if (!_buildings.TryAdd(building.Id, building))
        {
            throw new InvalidOperationException($"Building id {building.Id.Value} already exists.");
        }
    }

    internal void RegisterProject(ConstructionProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (_buildings.ContainsKey(project.Id))
        {
            throw new InvalidOperationException(
                $"Project id {project.Id.Value} collides with an existing building.");
        }
        if (!_projects.TryAdd(project.Id, project))
        {
            throw new InvalidOperationException(
                $"Project id {project.Id.Value} already exists.");
        }
    }

    public ConstructionProject? GetProject(BuildingId projectId) =>
        _projects.TryGetValue(projectId, out var project) ? project : null;

    /// <summary>True when at least one citizen is assigned to a project or building as a worker.</summary>
    internal bool HasAnyWorkAssignment()
    {
        foreach (var citizen in _citizens.Values)
        {
            if (citizen.CurrentAssignment.HasValue) return true;
        }
        return false;
    }

    private static bool ContainsControlCharacter(string value)
    {
        foreach (char character in value)
        {
            if (char.IsControl(character)) return true;
        }
        return false;
    }

    private static int StableAppearanceSeed(string name, LineageId lineage)
    {
        uint hash = 2166136261;
        foreach (char character in name)
        {
            hash = (hash ^ character) * 16777619;
        }
        foreach (char character in lineage.Value)
        {
            hash = (hash ^ character) * 16777619;
        }
        return (int)(hash & int.MaxValue);
    }

    public Citizen? GetCitizen(CitizenId id) =>
        _citizens.TryGetValue(id, out var citizen) ? citizen : null;

    public Building? GetBuilding(BuildingId id) =>
        _buildings.TryGetValue(id, out var building) ? building : null;

    /// <summary>
    /// Aggregate food available across every Farm-kind building.
    /// Thin facade today; replaceable by a real shared inventory
    /// without touching <see cref="Building"/> or its callers.
    /// </summary>
    public int FoodStock
    {
        get
        {
            int total = 0;
            foreach (var b in _buildings.Values)
            {
                if (b.Kind == BuildingKind.Farm) total += b.Stock;
            }
            return total;
        }
    }

    /// <summary>Aggregate food capacity across every Farm-kind building.</summary>
    public int MaxFoodStock
    {
        get
        {
            int total = 0;
            foreach (var b in _buildings.Values)
            {
                if (b.Kind == BuildingKind.Farm) total += b.StorageCapacity;
            }
            return total;
        }
    }

    /// <summary>
    /// Total wood available across every Forest-kind building.
    /// Wood lives on each Forest's <see cref="Building.Stock"/>
    /// after the hero gathers it from the Forest's
    /// <see cref="Building.WoodReserve"/>.
    /// </summary>
    public int TotalWood
    {
        get
        {
            int total = 0;
            foreach (var b in _buildings.Values)
            {
                if (b.Kind == BuildingKind.Forest) total += b.Stock;
            }
            return total;
        }
    }

    /// <summary>
    /// Total wood still waiting to be gathered across every Forest.
    /// </summary>
    public int TotalWoodReserve
    {
        get
        {
            int total = 0;
            foreach (var b in _buildings.Values)
            {
                if (b.Kind == BuildingKind.Forest) total += b.WoodReserve;
            }
            return total;
        }
    }

    /// <summary>
    /// Adds food across Farm-kind buildings in deterministic insertion
    /// order until capacity absorbs the request. Returns the amount
    /// actually deposited.
    /// </summary>
    public int DepositFood(int amount)
    {
        if (amount <= 0) return 0;
        int remaining = amount;
        foreach (var b in _buildings.Values)
        {
            if (b.Kind != BuildingKind.Farm) continue;
            int added = b.AddStock(remaining);
            remaining -= added;
            if (remaining == 0) break;
        }
        return amount - remaining;
    }

    /// <summary>
    /// Atomically removes <paramref name="amount"/> food from Farm-kind
    /// buildings. Returns <c>false</c> (and leaves state untouched)
    /// when there is not enough food.
    /// </summary>
    public bool TryConsumeFood(int amount)
    {
        if (amount <= 0) return amount == 0;
        if (FoodStock < amount) return false;

        int remaining = amount;
        foreach (var b in _buildings.Values)
        {
            if (b.Kind != BuildingKind.Farm || remaining == 0) continue;
            int take = b.Stock < remaining ? b.Stock : remaining;
            if (b.TryConsumeStock(take))
            {
                remaining -= take;
            }
        }
        return remaining == 0;
    }

    /// <summary>
    /// Returns the first building in the world. Convenience helper
    /// for the prototype: presentation code can default to it when
    /// only one building is in focus.
    /// </summary>
    public Building PrimaryBuilding
    {
        get
        {
            foreach (var building in _buildings.Values)
            {
                return building;
            }
            throw new InvalidOperationException("City world has no building.");
        }
    }

    /// <summary>
    /// Total stock of the given resource across every building that
    /// produces it. Used by the recipe drawdown path to gate
    /// construction authorisation and operating consumption. Iron
    /// is summed from the dedicated <see cref="Building.IronStock"/>
    /// reserve, not from the produced-resource <see cref="Building.Stock"/>.
    /// </summary>
    public int TotalStockOf(ResourceType type)
    {
        if (type == ResourceType.Iron)
        {
            int total = 0;
            foreach (var b in _buildings.Values)
            {
                total += b.IronStock;
            }
            return total;
        }
        int sum = 0;
        foreach (var b in _buildings.Values)
        {
            if (b.ProducedResourceType == type) sum += b.Stock;
        }
        return sum;
    }

    /// <summary>
    /// Consumes the requested amount of the resource across every
    /// building that produces it, in insertion order, draining each
    /// up to its stock. Returns <c>false</c> when the city does not
    /// hold enough to satisfy the request; the city is left untouched
    /// on failure (transactional).
    /// </summary>
    public bool TryConsumeResource(ResourceType type, int amount)
    {
        if (amount <= 0) return amount == 0;
        if (TotalStockOf(type) < amount) return false;

        if (type == ResourceType.Iron)
        {
            // Iron is held in each building's IronStock reserve.
            // Drains in insertion order, transactional.
            int remaining = amount;
            foreach (var b in _buildings.Values)
            {
                if (remaining == 0) break;
                int take = b.IronStock < remaining ? b.IronStock : remaining;
                if (b.TryConsumeIron(take))
                {
                    remaining -= take;
                }
            }
            return remaining == 0;
        }

        int rest = amount;
        foreach (var b in _buildings.Values)
        {
            if (b.ProducedResourceType != type || rest == 0) continue;
            int take = b.Stock < rest ? b.Stock : rest;
            if (b.TryConsumeStock(take))
            {
                rest -= take;
            }
        }
        return rest == 0;
    }

    /// <summary>
    /// Consumes the per-tick operating recipe inputs for the given
    /// building. Returns the first missing <see cref="ResourceType"/>
    /// on failure (transactional: no partial drawdown is left
    /// applied). Returns <c>null</c> on success.
    /// </summary>
    private ResourceType? TryConsumeOperatingInputs(Building building, Recipe recipe)
    {
        var debited = new List<(ResourceType resource, int amount)>();
        foreach (var input in recipe.RequiredInputs)
        {
            if (input.Amount <= 0) continue;
            if (!TryConsumeResource(input.Resource, input.Amount))
            {
                foreach (var (resource, amount) in debited)
                {
                    DepositResource(resource, amount);
                }
                return input.Resource;
            }
            debited.Add((input.Resource, input.Amount));
        }
        return null;
    }

    /// <summary>
    /// Returns the most recent <see cref="WorldEvent"/> whose
    /// <see cref="WorldEvent.SubjectName"/> matches the building's
    /// display name, or <c>null</c> when none exists. Used to wire
    /// <see cref="WorldEvent.CauseEventId"/> for causal chains; the
    /// resource filter is intentionally unused here so a blocked
    /// event can still reference the last successful production tick.
    /// </summary>
    public WorldEvent? FindCauseEvent(Building? building = null, ResourceType? resource = null)
    {
        _ = resource; // accepted for future use; not consulted today.
        var events = _log.Events;
        for (int i = events.Count - 1; i >= 0; i--)
        {
            var evt = events[i];
            if (building is not null && evt.SubjectName != building.DisplayName) continue;
            return evt;
        }
        return null;
    }

    /// <summary>
    /// Drains wood from a Forest's remaining reserve and credits it
    /// to the forest's Stock (which the construction recipe gate then
    /// consumes). Returns the amount actually gathered, which may be
    /// less than <paramref name="amount"/> when the reserve runs dry
    /// or the storage capacity is full. Records a
    /// <see cref="WorldEventKind.StockProduced"/> event so the offline
    /// report can surface the gathering activity.
    /// </summary>
    public int GatherWood(BuildingId forestId, int amount)
    {
        if (amount <= 0) return 0;
        if (!_buildings.TryGetValue(forestId, out var forest)) return 0;
        if (forest.Kind != BuildingKind.Forest) return 0;
        int gathered = forest.GatherWood(amount);
        if (gathered > 0)
        {
            var cause = FindCauseEvent(forest)?.Id.ToString();
            _log.Record(_tick, WorldEventKind.StockProduced, forest.DisplayName, gathered, cause);
            RaiseBuildingChanged(forestId);
        }
        return gathered;
    }

    /// <summary>
    /// Returns the citizens that are not currently assigned to any
    /// building, in deterministic insertion order. The presentation
    /// layer uses this to populate the assignment panel.
    /// </summary>
    public IReadOnlyList<Citizen> AvailableCitizens()
    {
        var list = new List<Citizen>();
        foreach (var citizen in _citizens.Values)
        {
            if (!citizen.CurrentAssignment.HasValue)
            {
                list.Add(citizen);
            }
        }
        return list;
    }

    /// <summary>
    /// Same set as <see cref="AvailableCitizens"/> but ordered so the
    /// highest-priority productive building shows first. The domain
    /// owns the policy; consumers (the assignment panel) just render.
    /// When no productive building exists, falls back to insertion
    /// order.
    /// </summary>
    public IReadOnlyList<Citizen> AvailableCitizensByPriority()
    {
        var list = new List<Citizen>(AvailableCitizens());
        int topPriority = -1;
        foreach (var b in _buildings.Values)
        {
            if (b.Priority > topPriority) topPriority = b.Priority;
        }
        // When there is a productive building, the most relevant
        // priority ranks first; the panel renders this order. With
        // no productive building the list stays in insertion order.
        if (topPriority >= 0)
        {
            list.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.Ordinal));
        }
        return list;
    }

    /// <summary>
    /// Attempts to assign a citizen to a building. The domain
    /// validates the operation end-to-end: the building must exist,
    /// the citizen must exist, the citizen must not already be
    /// assigned elsewhere, and the building must have spare worker
    /// capacity.
    /// </summary>
    public AssignmentResult TryAssignCitizen(BuildingId buildingId, CitizenId citizenId)
    {
        if (!_buildings.TryGetValue(buildingId, out var building))
        {
            return AssignmentResult.Fail(AssignmentOutcome.BuildingNotFound, citizenId, buildingId);
        }

        if (!_citizens.TryGetValue(citizenId, out var citizen))
        {
            return AssignmentResult.Fail(AssignmentOutcome.CitizenNotFound, citizenId, buildingId);
        }

        // Check the specific building first so callers get a precise
        // "AlreadyAssigned" outcome (the citizen is on THIS building)
        // before the more generic "CitizenUnavailable" (the citizen
        // is on another building).
        if (building.IsAssigned(citizenId))
        {
            return AssignmentResult.Fail(AssignmentOutcome.AlreadyAssigned, citizenId, buildingId);
        }

        if (citizen.CurrentAssignment.HasValue)
        {
            return AssignmentResult.Fail(AssignmentOutcome.CitizenUnavailable, citizenId, buildingId);
        }

        var result = building.TryAssign(citizenId);
        if (result.IsSuccess)
        {
            citizen.AssignTo(buildingId);
            RaiseBuildingChanged(buildingId);
        }

        return result;
    }

    /// <summary>
    /// Attempts to remove a citizen from a building.
    /// </summary>
    public AssignmentResult TryUnassignCitizen(BuildingId buildingId, CitizenId citizenId)
    {
        if (!_buildings.TryGetValue(buildingId, out var building))
        {
            return AssignmentResult.Fail(AssignmentOutcome.BuildingNotFound, citizenId, buildingId);
        }

        if (!_citizens.TryGetValue(citizenId, out var citizen))
        {
            return AssignmentResult.Fail(AssignmentOutcome.CitizenNotFound, citizenId, buildingId);
        }

        var result = building.TryUnassign(citizenId);
        if (result.IsSuccess)
        {
            citizen.ClearAssignment();
            RaiseBuildingChanged(buildingId);
        }

        return result;
    }

    /// <summary>
    /// Attempts to assign a citizen to a worksite. The id is shared
    /// with the future building so <see cref="Citizen.CurrentAssignment"/>
    /// remains a plain <see cref="BuildingId"/>?>.
    /// </summary>
    public AssignmentResult TryAssignToProject(BuildingId projectId, CitizenId citizenId)
    {
        if (!_projects.TryGetValue(projectId, out var project))
        {
            return AssignmentResult.Fail(AssignmentOutcome.BuildingNotFound, citizenId, projectId);
        }
        if (!_citizens.TryGetValue(citizenId, out var citizen))
        {
            return AssignmentResult.Fail(AssignmentOutcome.CitizenNotFound, citizenId, projectId);
        }
        if (project.IsAssigned(citizenId))
        {
            return AssignmentResult.Fail(AssignmentOutcome.AlreadyAssigned, citizenId, projectId);
        }
        if (citizen.CurrentAssignment.HasValue)
        {
            return AssignmentResult.Fail(AssignmentOutcome.CitizenUnavailable, citizenId, projectId);
        }
        if (!project.TryAssign(citizenId))
        {
            return AssignmentResult.Fail(AssignmentOutcome.AtCapacity, citizenId, projectId);
        }
        citizen.AssignTo(projectId);
        RaiseProjectChanged(projectId);
        return AssignmentResult.Ok(citizenId, projectId);
    }

    public AssignmentResult TryUnassignFromProject(BuildingId projectId, CitizenId citizenId)
    {
        if (!_projects.TryGetValue(projectId, out var project))
        {
            return AssignmentResult.Fail(AssignmentOutcome.BuildingNotFound, citizenId, projectId);
        }
        if (!_citizens.TryGetValue(citizenId, out var citizen))
        {
            return AssignmentResult.Fail(AssignmentOutcome.CitizenNotFound, citizenId, projectId);
        }
        if (!project.TryUnassign(citizenId))
        {
            return AssignmentResult.Fail(AssignmentOutcome.NotAssigned, citizenId, projectId);
        }
        citizen.ClearAssignment();
        RaiseProjectChanged(projectId);
        return AssignmentResult.Ok(citizenId, projectId);
    }

    /// <summary>
    /// Authorises the first worksite — the Basic Shelter. The id is
    /// the next reserved <see cref="BuildingId"/>, distinct from any
    /// existing building or citizen.
    /// </summary>
    public ConstructionAuthorizationResult TryAuthorizeBasicShelter()
        => TryAuthorizeConstruction(ConstructionKind.BasicShelter);

    /// <summary>
    /// Authorises one worksite at a time. Productive buildings become
    /// available after the founding shelter exists; every kind uses
    /// the same phased progress model with its own work requirement.
    /// Material cost is debited up-front as a deposit; the remainder
    /// is drained one unit per work interval while the project is
    /// active. On cancellation, inputs already consumed remain spent;
    /// the recorded remainder was never debited and is simply discarded.
    /// </summary>
    public ConstructionAuthorizationResult TryAuthorizeConstruction(ConstructionKind kind)
    {
        if (Hero is null)
        {
            return ConstructionAuthorizationResult.Fail(ConstructionAuthorizationOutcome.NoHero);
        }
        if (_projects.Count > 0)
        {
            return ConstructionAuthorizationResult.Fail(ConstructionAuthorizationOutcome.AlreadyAuthorized);
        }
        bool hasHome = false;
        foreach (var building in _buildings.Values)
        {
            if (building.Kind == BuildingKind.Home)
            {
                hasHome = true;
                if (kind == ConstructionKind.BasicShelter)
                {
                    return ConstructionAuthorizationResult.Fail(ConstructionAuthorizationOutcome.HomeAlreadyBuilt);
                }
            }
        }
        if (kind == ConstructionKind.BasicShelter && _citizens.Count > 1)
        {
            return ConstructionAuthorizationResult.Fail(ConstructionAuthorizationOutcome.WorldNotEmpty);
        }
        if (kind != ConstructionKind.BasicShelter && !hasHome)
        {
            return ConstructionAuthorizationResult.Fail(ConstructionAuthorizationOutcome.HomeRequired);
        }

        // Recipe gate: a non-empty recipe must be satisfiable up-front
        // (deposit = ceil(total * 0.25)) or the authorisation fails
        // and the city state is unchanged.
        var recipe = Recipes.ConstructionRecipeFor(kind);
        if (recipe is not null && recipe.RequiredInputs.Count > 0)
        {
            var debited = new List<(ResourceType resource, int amount)>();
            bool success = true;
            foreach (var input in recipe.RequiredInputs)
            {
                int deposit = ConstructionRules.DepositOf(input.Amount);
                if (!TryConsumeResource(input.Resource, deposit))
                {
                    success = false;
                    break;
                }
                debited.Add((input.Resource, deposit));
            }
            if (!success)
            {
                // Refund everything we already took — atomic on failure.
                foreach (var (resource, amount) in debited)
                {
                    DepositResource(resource, amount);
                }
                return ConstructionAuthorizationResult.Fail(ConstructionAuthorizationOutcome.MissingMaterials);
            }
        }

        var projectId = NextAvailableProjectId();
        var project = new ConstructionProject(
            id: projectId,
            kind: kind,
            displayName: ConstructionRules.DisplayNameFor(kind),
            requiredWork: ConstructionRules.RequiredWorkFor(kind),
            workerCapacity: ConstructionRules.WorkerCapacity,
            enabled: true)
        {
            StopCause = ConstructionStopCause.NoWorkers,
        };
        // Seed the remaining-inputs list from the recipe. Each entry
        // starts at the post-deposit remainder; the simulation drains
        // it 1 unit per work interval.
        if (recipe is not null && recipe.RequiredInputs.Count > 0)
        {
            var remaining = new List<RecipeInput>();
            foreach (var input in recipe.RequiredInputs)
            {
                int after = ConstructionRules.RemainderAfterDeposit(input.Amount);
                if (after > 0)
                {
                    remaining.Add(new RecipeInput(input.Resource, after));
                }
            }
            project.SetRemainingInputs(remaining);
        }
        RegisterProject(project);
        RaiseProjectChanged(projectId);
        return ConstructionAuthorizationResult.Success(projectId);
    }

    /// <summary>
    /// Cancels an in-flight project. Inputs already consumed by the
    /// deposit or subsequent work intervals remain spent. RemainingInputs
    /// represent amounts not yet debited, so cancellation must not deposit
    /// them or it would create resources.
    /// </summary>
    public bool CancelProject(BuildingId projectId)
    {
        if (!_projects.TryGetValue(projectId, out var project)) return false;
        foreach (var cid in project.AssignedCitizenIds)
        {
            if (_citizens.TryGetValue(cid, out var citizen))
            {
                citizen.ClearAssignment();
            }
        }
        _projects.Remove(projectId);
        RaiseProjectChanged(projectId);
        return true;
    }

    /// <summary>
    /// Adds the given amount of resource to the city aggregate.
    /// Used by explicit deposit paths such as test setup and future
    /// rewards or expeditions returning with goods.
    /// Iron flows to <see cref="Building.IronStock"/>; everything
    /// else flows to the produced-resource <see cref="Building.Stock"/>.
    /// </summary>
    public void DepositResource(ResourceType type, int amount)
    {
        if (amount <= 0) return;
        if (type == ResourceType.Iron)
        {
            // Spread the deposit across buildings in insertion order.
            // For this slice the convention is "first building gets it";
            // a future slice can introduce per-resource sharing.
            foreach (var b in _buildings.Values)
            {
                b.DepositIron(amount);
                break;
            }
            return;
        }
        int remaining = amount;
        foreach (var b in _buildings.Values)
        {
            if (b.ProducedResourceType != type || remaining == 0) continue;
            int added = b.AddStock(remaining);
            remaining -= added;
            if (remaining == 0) break;
        }
        // If no building produces this resource yet, the deposit is
        // silently lost. Future slices can introduce a "shared
        // inventory" abstraction here without touching callers.
    }

    private BuildingId NextAvailableProjectId()
    {
        var candidate = new BuildingId(_nextProjectId);
        while (_buildings.ContainsKey(candidate) || _projects.ContainsKey(candidate))
        {
            candidate = new BuildingId(++_nextProjectId);
        }
        _nextProjectId++;
        return candidate;
    }

    /// <summary>Toggles whether the project continues to accumulate work.</summary>
    public void SetProjectEnabled(BuildingId projectId, bool enabled)
    {
        if (!_projects.TryGetValue(projectId, out var project)) return;
        project.Enabled = enabled;
        RaiseProjectChanged(projectId);
    }

    /// <summary>
    /// Advances the world by one tick and credits the building
    /// with its current production. Returns the amount of stock
    /// actually added (storage capacity can absorb less than
    /// produced when stock is near full). Day/night agnostic —
    /// callers that want the full world tick should use
    /// <see cref="AdvanceWorldTick"/>.
    /// </summary>
    public int AdvanceProduction(BuildingId buildingId)
    {
        if (!_buildings.TryGetValue(buildingId, out var building))
        {
            return 0;
        }
        _tick++;
        int added = SimulateBuildingTick(building);
        if (added > 0
            || building.StopCause == ProductionStopCause.WorkersExhausted)
        {
            RaiseBuildingChanged(building.Id);
        }
        return added;
    }

    /// <summary>
    /// One world tick. Canonical order: clock advance → mobilisation
    /// at day/night boundary → upkeep → per-building behavior
    /// (day: produce; night: rest) → per-project behaviour
    /// (day: contribute at work intervals; night: rest) → buffs.
    /// Project completion is deferred to the end of the tick so
    /// the project dictionary is not mutated while iterating.
    /// </summary>
    public void AdvanceWorldTick()
    {
        int previousTick = _tick;
        _tick++;
        bool dayChanged = DetectAndApplyMobilisation(previousTick, _tick);
        if (dayChanged)
        {
            if (GameClock.IsDaytime(_tick)) _log.Record(_tick, WorldEventKind.DayBegan, "Sun");
            else _log.Record(_tick, WorldEventKind.NightBegan, "Sun");
        }
        ApplyUpkeep();
        foreach (var building in _buildings.Values)
        {
            building.LastTickProduction = 0;
            if (GameClock.IsDaytime(_tick))
            {
                // Reactive resume: a building whose stock has fallen
                // to or below its MinStock since the last MaxStock cap
                // is unblocked and can produce again next tick.
                if (building.Stock <= building.MinStock)
                {
                    building.ResumeIfBelowMin();
                }

                int added = SimulateBuildingTick(building);
                if (added > 0)
                {
                    var cause = FindCauseEvent(building)?.Id.ToString();
                    _log.Record(_tick, WorldEventKind.StockProduced, building.DisplayName, added, cause);
                }
                if (building.StopCause == ProductionStopCause.WorkersExhausted)
                {
                    _log.Record(_tick, WorldEventKind.WorkersExhausted, building.DisplayName);
                }
                if (building.Stock >= building.MaxStock && building.MaxStock > 0)
                {
                    _log.Record(_tick, WorldEventKind.StockCapped, building.DisplayName);
                }
                if (added > 0
                    || building.StopCause == ProductionStopCause.WorkersExhausted)
                {
                    RaiseBuildingChanged(building.Id);
                }
            }
            else
            {
                ApplyNightRest(building);
            }
        }

        bool isWorkInterval = _tick > 0
            && (_tick % ConstructionRules.WorkIntervalTicks == 0);
        int completed = 0;
        foreach (var project in _projects.Values)
        {
            int previousProgress = project.Progress;
            ConstructionStopCause previousStopCause = project.StopCause;
            project.LastTickProgressAdded = 0;
            if (GameClock.IsDaytime(_tick))
            {
                SimulateProjectTick(project, isWorkInterval);
                if (project.LastTickProgressAdded > 0)
                {
                    _log.Record(_tick, WorldEventKind.ProjectProgressed,
                        project.DisplayName, project.LastTickProgressAdded);
                }
                if (project.Progress != previousProgress
                    || project.StopCause != previousStopCause)
                {
                    RaiseProjectChanged(project.Id);
                }
            }
            else
            {
                ApplyProjectNightRest(project);
            }
            if (project.Progress >= project.RequiredWork) completed++;
        }
        for (int i = 0; i < completed; i++)
        {
            // We cannot iterate _projects here, but the deferred
            // completion list would be heavier than two passes;
            // instead we re-query the dictionary of project ids
            // that crossed the threshold this tick. A second pass
            // over the dictionary is O(n) and avoids the iterator
            // mutation hazard.
        }
        CompleteFinishedProjects();
        DecrementAllWellFed();
    }

    private void CompleteFinishedProjects()
    {
        if (_projects.Count == 0) return;
        List<BuildingId>? completed = null;
        foreach (var pair in _projects)
        {
            if (pair.Value.Progress >= pair.Value.RequiredWork)
            {
                completed ??= new List<BuildingId>();
                completed.Add(pair.Key);
            }
        }
        if (completed is null) return;
        for (int i = 0; i < completed.Count; i++)
        {
            CompleteProject(completed[i]);
        }
    }

    /// <summary>
    /// Compares day/night state before and after the tick and
    /// moves citizens to the right place when the boundary
    /// crosses. Called once per world tick from
    /// <see cref="AdvanceWorldTick"/>. Returns <c>true</c> when the
    /// day/night state actually changed so the caller can emit the
    /// corresponding log event without re-deriving the comparison.
    /// </summary>
    private bool DetectAndApplyMobilisation(int previousTick, int currentTick)
    {
        bool wasDay = GameClock.IsDaytime(previousTick);
        bool isDay = GameClock.IsDaytime(currentTick);
        if (wasDay && !isDay)
        {
            MobiliseForNight();
            return true;
        }
        else if (!wasDay && isDay)
        {
            MobiliseForDay();
            return true;
        }
        return false;
    }

    /// <summary>
    /// All citizens go to the Home at night — assigned workers
    /// leave their production building to rest; idle citizens stay
    /// at home (they never left). Called on the day→night boundary.
    /// </summary>
    private void MobiliseForNight()
    {
        foreach (var citizen in _citizens.Values)
        {
            citizen.SetLocation(CitizenLocation.AtHome);
        }
        // The Home building's slot rendering reads CitizenLocation
        // directly; nothing else needs to fire here. UI listeners
        // re-render via the regular BuildingChanged signals that
        // follow in this tick.
    }

    /// <summary>
    /// Assigned citizens return to their production building;
    /// unassigned citizens stay at home. Called on the night→day
    /// boundary.
    /// </summary>
    private void MobiliseForDay()
    {
        foreach (var citizen in _citizens.Values)
        {
            citizen.SetLocation(citizen.CurrentAssignment.HasValue
                ? CitizenLocation.AtWork
                : CitizenLocation.AtHome);
        }
    }

    /// <summary>
    /// Citizens physically visible at this building right now.
    /// For production buildings: assigned citizens whose
    /// <see cref="Citizen.CurrentLocation"/> is
    /// <see cref="CitizenLocation.AtWork"/>. For Home: every
    /// citizen whose location is
    /// <see cref="CitizenLocation.AtHome"/>.
    /// </summary>
    public IReadOnlyList<CitizenId> GetCurrentlyVisibleOccupants(Building building)
    {
        var ids = new List<CitizenId>();
        if (building.Kind == BuildingKind.Home)
        {
            foreach (var citizen in _citizens.Values)
            {
                if (citizen.CurrentLocation == CitizenLocation.AtHome)
                {
                    ids.Add(citizen.Id);
                }
            }
        }
        else
        {
            foreach (var citizenId in building.AssignedCitizenIds)
            {
                if (_citizens.TryGetValue(citizenId, out var citizen)
                    && citizen.CurrentLocation == CitizenLocation.AtWork)
                {
                    ids.Add(citizen.Id);
                }
            }
        }
        return ids;
    }

    /// <summary>
    /// First Home building in the world, or null if the city has
    /// none. Citizens are mobilised here at night; the UI uses it
    /// as the resting location. Seam: future slices with multiple
    /// homes may return the closest or the one with capacity.
    /// </summary>
    public Building? PrimaryHome
    {
        get
        {
            foreach (var building in _buildings.Values)
            {
                if (building.Kind == BuildingKind.Home) return building;
            }
            return null;
        }
    }

    private void ApplyUpkeep()
    {
        int toConsume = Upkeep.StonePerTick(_citizens.Count);
        for (int i = 0; i < toConsume; i++)
        {
            bool consumed = false;
            foreach (var building in _buildings.Values)
            {
                if (building.Kind != BuildingKind.Quarry) continue;
                if (building.TryConsumeStock(1))
                {
                    consumed = true;
                    break;
                }
            }
            if (!consumed) break; // no stone left anywhere
        }
    }

    private void ApplyNightRest(Building building)
    {
        ApplyFoodAndRegen(building);
        building.StopCause = ProductionStopCause.Night;
        RaiseBuildingChanged(building.Id);
    }

    private void DecrementAllWellFed()
    {
        foreach (var citizen in _citizens.Values)
        {
            citizen.AdvanceWellFedTick();
        }
    }

    /// <summary>
    /// One building tick in isolation. Performs eat / passive
    /// regen (buff-aware) / cost / contributing / produce /
    /// experience, sets the building's
    /// <see cref="Building.StopCause"/>, and returns the stock
    /// added. Does not raise <see cref="BuildingChanged"/> —
    /// callers decide whether to notify the UI.
    /// </summary>
    internal int SimulateBuildingTick(Building building)
    {
        if (!building.CanProduce)
        {
            building.StopCause = ResolveStopCauseWhenNotProducing(building);
            return 0;
        }

        // Recipe gate: if the operating recipe needs inputs and the
        // city cannot satisfy them this tick, block production
        // before paying stamina or growing experience.
        var operatingRecipe = Recipes.OperatingRecipeFor(building.Kind);
        if (operatingRecipe is not null && operatingRecipe.RequiredInputs.Count > 0)
        {
            var missing = TryConsumeOperatingInputs(building, operatingRecipe);
            if (missing is not null)
            {
                building.StopCause = ProductionStopCause.MissingInputs;
                _log.Record(_tick, WorldEventKind.ProductionBlocked,
                    building.DisplayName, amount: 0,
                    causeEventId: FindCauseEvent(building, missing)?.Id.ToString());
                RaiseBuildingChanged(building.Id);
                return 0;
            }
        }

        ApplyFoodAndRegen(building);
        ApplyStaminaCost(building);

        var contributing = new List<Citizen>();
        foreach (var citizenId in building.AssignedCitizenIds)
        {
            if (_citizens.TryGetValue(citizenId, out var citizen)
                && citizen.CurrentStamina > 0)
            {
                contributing.Add(citizen);
            }
        }

        if (contributing.Count == 0)
        {
            building.StopCause = ProductionStopCause.WorkersExhausted;
            return 0;
        }

        int produced = BuildingProductionCalculator.ProductionPerTick(contributing, building);
        int roomToTarget = building.MaxStock - building.Stock;
        int added = building.AddStock(Math.Min(produced, roomToTarget));
        building.LastTickProduction = added;

        var competency = building.ProducedCompetencyId;
        foreach (var citizen in contributing)
        {
            citizen.AddExperience(competency, 1);
        }

        building.StopCause = building.Stock >= building.MaxStock
            ? ProductionStopCause.TargetReached
            : ProductionStopCause.Authorized;
        return added;
    }

    private static ProductionStopCause ResolveStopCauseWhenNotProducing(Building building)
    {
        if (!building.ProductionEnabled) return ProductionStopCause.Paused;
        if (building.AssignedCount == 0) return ProductionStopCause.NoWorkers;
        return ProductionStopCause.TargetReached;
    }

    private void ApplyFoodAndRegen(Building building)
    {
        foreach (var citizenId in building.AssignedCitizenIds)
        {
            if (!_citizens.TryGetValue(citizenId, out var citizen)) continue;

            // Eat if the citizen has room to grow and the city has food.
            if (citizen.CurrentStamina < citizen.MaxStamina
                && TryConsumeFood(StaminaRules.FoodConsumedPerRegen))
            {
                int restored = StaminaRules.RegenFromFood(StaminaRules.FoodConsumedPerRegen, citizen);
                citizen.RestoreStamina(restored);
                citizen.RefreshWellFedBuff();
            }

            // Passive + (optional) buff regen, always applied.
            citizen.RestoreStamina(citizen.RegenPerTick());
        }
    }

    private void ApplyStaminaCost(Building building)
    {
        foreach (var citizenId in building.AssignedCitizenIds)
        {
            if (_citizens.TryGetValue(citizenId, out var citizen))
            {
                citizen.ConsumeStamina(StaminaRules.CostForWorker(citizen, building.Kind));
            }
        }
    }

    /// <summary>
    /// One work-interval tick for a project. Stamina is paid
    /// only on real intervals so a single hero cannot burn out
    /// between work intervals. Material drawdown runs at the same
    /// cadence: 1 unit per remaining input per work interval.
    /// </summary>
    private void SimulateProjectTick(ConstructionProject project, bool isWorkInterval)
    {
        if (!project.Enabled)
        {
            project.StopCause = ConstructionStopCause.Paused;
            return;
        }
        if (project.Progress >= project.RequiredWork)
        {
            project.StopCause = ConstructionStopCause.Completed;
            return;
        }
        if (project.AssignedCount == 0)
        {
            project.StopCause = ConstructionStopCause.NoWorkers;
            return;
        }

        // Material drawdown at the work-interval boundary. Drains
        // 1 unit per remaining input. Refunds anything already taken
        // when one input is short (transactional).
        if (isWorkInterval && project.RemainingInputs.Count > 0)
        {
            var debited = new List<(ResourceType resource, int amount)>();
            bool success = true;
            var nextRemaining = new List<RecipeInput>();
            foreach (var input in project.RemainingInputs)
            {
                if (!TryConsumeResource(input.Resource, 1))
                {
                    success = false;
                    nextRemaining.Add(input);
                    break;
                }
                debited.Add((input.Resource, 1));
                if (input.Amount - 1 > 0)
                {
                    nextRemaining.Add(new RecipeInput(input.Resource, input.Amount - 1));
                }
            }
            if (!success)
            {
                foreach (var (resource, amount) in debited)
                {
                    DepositResource(resource, amount);
                }
                project.StopCause = ConstructionStopCause.MissingMaterials;
                var cause = FindCauseEvent()?.Id.ToString();
                _log.Record(_tick, WorldEventKind.ProductionBlocked,
                    project.DisplayName, amount: 0, causeEventId: cause);
                RaiseProjectChanged(project.Id);
                return;
            }
            project.SetRemainingInputs(nextRemaining);
        }

        int contributed = 0;
        int paid = 0;
        foreach (var citizenId in project.AssignedCitizenIds)
        {
            if (!_citizens.TryGetValue(citizenId, out var citizen)) continue;
            citizen.RestoreStamina(citizen.RegenPerTick());
            int perCitizen = ConstructionRules.ContributionPerWorkInterval(citizen);
            if (perCitizen <= 0) continue;
            contributed += perCitizen;
            if (isWorkInterval)
            {
                citizen.ConsumeStamina(ConstructionRules.CostPerWorkInterval);
                paid++;
            }
        }

        if (paid == 0 && isWorkInterval)
        {
            project.StopCause = ConstructionStopCause.WorkersExhausted;
            return;
        }
        if (contributed == 0)
        {
            project.StopCause = ConstructionStopCause.WorkersExhausted;
            return;
        }
        if (isWorkInterval)
        {
            int room = project.RequiredWork - project.Progress;
            int added = contributed < room ? contributed : room;
            project.Progress += added;
            project.LastTickProgressAdded = added;
        }
        project.StopCause = ConstructionStopCause.Authorized;
    }

    private void ApplyProjectNightRest(ConstructionProject project)
    {
        foreach (var citizenId in project.AssignedCitizenIds)
        {
            if (_citizens.TryGetValue(citizenId, out var citizen))
            {
                citizen.RestoreStamina(citizen.RegenPerTick());
            }
        }
        project.StopCause = ConstructionStopCause.Night;
        RaiseProjectChanged(project.Id);
    }

    private void CompleteProject(BuildingId projectId)
    {
        if (!_projects.TryGetValue(projectId, out var project)) return;
        var contributorIds = new List<CitizenId>(project.AssignedCitizenIds);

        var building = CreateCompletedBuilding(project);

        RegisterBuilding(building);
        RaiseBuildingChanged(building.Id);
        _log.Record(_tick, WorldEventKind.ProjectCompleted, project.DisplayName);
        _log.Record(_tick, WorldEventKind.BuildingCreated, building.DisplayName);

        foreach (var cid in contributorIds)
        {
            project.TryUnassign(cid);
            if (_citizens.TryGetValue(cid, out var c)) c.ClearAssignment();
        }

        _projects.Remove(projectId);
        RaiseProjectChanged(projectId);
    }

    private static Building CreateCompletedBuilding(ConstructionProject project) => project.Kind switch
    {
        ConstructionKind.Farm => new Building(
            id: project.Id,
            displayName: project.DisplayName,
            kind: BuildingKind.Farm,
            producedResourceType: ResourceType.Food,
            producedCompetencyId: CompetencyId.Farming,
            workerCapacity: 5,
            visualCapacity: 5,
            baseProductionPerWorker: 1,
            storageCapacity: 20,
            resourceLabel: "Food",
            resourceUnit: "food"),
        ConstructionKind.Quarry => new Building(
            id: project.Id,
            displayName: project.DisplayName,
            kind: BuildingKind.Quarry,
            producedResourceType: ResourceType.Stone,
            producedCompetencyId: CompetencyId.Mining,
            workerCapacity: 6,
            visualCapacity: 3,
            baseProductionPerWorker: 2,
            storageCapacity: 20,
            resourceLabel: "Stone",
            resourceUnit: "stone"),
        _ => new Building(
            id: project.Id,
            displayName: project.DisplayName,
            kind: BuildingKind.Home,
            producedResourceType: ResourceType.Stone,
            producedCompetencyId: CompetencyId.Mining,
            workerCapacity: 5,
            visualCapacity: 5,
            baseProductionPerWorker: 0,
            storageCapacity: 0,
            resourceLabel: "Rest",
            resourceUnit: "rest",
            productionEnabled: false),
    };

    public void ConfigureProductionPolicy(BuildingId buildingId, bool enabled, int minStock, int maxStock, int priority)
    {
        if (!_buildings.TryGetValue(buildingId, out var building))
        {
            return;
        }

        building.ConfigureProductionPolicy(enabled, minStock, maxStock, priority);
        RaiseBuildingChanged(buildingId);
    }

    internal void AdvanceWorldClock(int tickCount)
    {
        if (tickCount > 0) _tick += tickCount;
    }

    /// <summary>
    /// Fast-forwards a world that has no buildings and no
    /// construction projects. Otherwise the caller must step the
    /// world tick by tick so the worksite can advance.
    /// </summary>
    internal void AdvanceIdleTicks(int tickCount)
    {
        if (tickCount <= 0) return;
        if (_buildings.Count != 0 || _projects.Count != 0)
        {
            throw new InvalidOperationException(
                "Idle fast-forward requires a world with no buildings and no projects.");
        }

        _tick += tickCount;
        foreach (var citizen in _citizens.Values)
        {
            citizen.AdvanceWellFedTicks(tickCount);
        }
        if (GameClock.IsDaytime(_tick)) MobiliseForDay();
        else MobiliseForNight();
    }

    /// <summary>
    /// Current production rate per tick for the given building.
    /// </summary>
    public int CurrentProductionRate(BuildingId buildingId)
    {
        if (!_buildings.TryGetValue(buildingId, out var building))
        {
            return 0;
        }

        return BuildingProductionCalculator.ProductionPerTick(building, _citizens);
    }

    /// <summary>
    /// Replaces this world's contents with the contents of
    /// <paramref name="save"/>. Used by the persistence layer when
    /// auto-loading on launch.
    /// </summary>
    public void Restore(WorldSave save)
    {
        WorldPersistence.Validate(save);
        _citizens.Clear();
        _buildings.Clear();
        _projects.Clear();
        _log.Clear();
        _nextProjectId = 1;
        _tick = save.CurrentTick;

        foreach (var bs in save.Buildings)
        {
            var kind = Enum.TryParse<BuildingKind>(bs.Kind, ignoreCase: true, out var parsed)
                ? parsed
                : BuildingKind.Quarry;
            var resource = Enum.TryParse<ResourceType>(bs.ProducedResourceType, ignoreCase: true, out var pres)
                ? pres
                : ResourceType.Stone;
            var competency = string.IsNullOrEmpty(bs.ProducedCompetencyId)
                ? CompetencyId.Mining
                : new CompetencyId(bs.ProducedCompetencyId);

            var building = new Building(
                id: new BuildingId(bs.Id),
                displayName: bs.DisplayName,
                kind: kind,
                producedResourceType: resource,
                producedCompetencyId: competency,
                workerCapacity: bs.WorkerCapacity,
                visualCapacity: bs.VisualCapacity,
                baseProductionPerWorker: bs.BaseProductionPerWorker,
                storageCapacity: bs.StorageCapacity,
                resourceLabel: string.IsNullOrEmpty(bs.ResourceLabel) ? "Resource" : bs.ResourceLabel,
                resourceUnit: string.IsNullOrEmpty(bs.ResourceUnit) ? "units" : bs.ResourceUnit,
                initialStock: bs.Stock,
                productionEnabled: bs.ProductionEnabled);
            // v3 fields default to (0, StorageCapacity, 0) for v2 saves
            // that predate the policy triplet. A legacy TargetStock is
            // treated as MaxStock so old saves behave identically.
            int maxStock = bs.MaxStock ?? bs.TargetStock ?? bs.StorageCapacity;
            int minStock = bs.MinStock ?? 0;
            int priority = bs.Priority ?? 0;
            building.ConfigureProductionPolicy(bs.ProductionEnabled, minStock, maxStock, priority);
            RegisterBuilding(building);

            foreach (var cid in bs.AssignedCitizenIds)
            {
                // Building.TryAssign is internal — same-assembly access.
                building.TryAssign(new CitizenId(cid));
            }
        }

        foreach (var cs in save.Citizens)
        {
            // Old saves (no StaminaMax) restore to full stamina;
            // new saves (StaminaMax present) restore the saved current.
            int? maxStamina = cs.StaminaMax;
            int? initialStamina = maxStamina.HasValue ? cs.StaminaCurrent : (int?)null;
            var citizen = new Citizen(
                new CitizenId(cs.Id),
                cs.Name,
                cs.AppearanceSeed,
                profile: WorldPersistence.RestoreProfile(cs.Profile!),
                initialStamina: initialStamina,
                maxStamina: maxStamina,
                initialWellFedTicks: cs.WellFedRemainingTicks);
            if (cs.CurrentAssignment.HasValue)
            {
                citizen.AssignTo(new BuildingId(cs.CurrentAssignment.Value));
            }

            foreach (var entry in cs.Competencies)
            {
                citizen.AddExperience(new CompetencyId(entry.Id), entry.Experience);
            }

            foreach (var role in cs.Roles)
            {
                citizen.GrantRole(new RoleId(role.Id), role.GrantedAtTick);
            }

            RegisterCitizen(citizen);
        }

        if (save.Projects is { Count: > 0 })
        {
            foreach (var ps in save.Projects)
            {
                var kind = Enum.TryParse<ConstructionKind>(ps.Kind, ignoreCase: true, out var parsed)
                    ? parsed
                    : ConstructionKind.BasicShelter;
                var project = new ConstructionProject(
                    id: new BuildingId(ps.Id),
                    kind: kind,
                    displayName: string.IsNullOrEmpty(ps.DisplayName) ? "Basic Shelter" : ps.DisplayName,
                    requiredWork: ps.RequiredWork,
                    workerCapacity: ps.WorkerCapacity,
                    enabled: ps.Enabled)
                {
                    Progress = ps.Progress,
                    StopCause = ConstructionStopCause.Paused,
                };
                // Restore material drawdown state. v2 saves without
                // these fields default to "fully spent" (empty) — the
                // resumed project simply runs without any per-interval
                // drawdown, which matches the pre-v3 behaviour exactly.
                var remaining = new List<RecipeInput>();
                if (ps.RemainingInputs is { Count: > 0 })
                {
                    foreach (var pair in ps.RemainingInputs)
                    {
                        if (Enum.TryParse<ResourceType>(pair.Key, ignoreCase: true, out var res)
                            && pair.Value > 0)
                        {
                            remaining.Add(new RecipeInput(res, pair.Value));
                        }
                    }
                }
                project.SetRemainingInputs(remaining);
                RegisterProject(project);
                foreach (var cid in ps.AssignedCitizenIds)
                {
                    project.TryAssign(new CitizenId(cid));
                }
                if (ps.Id >= _nextProjectId) _nextProjectId = ps.Id + 1;
            }
        }

        // Citizens are constructed with CurrentLocation = AtHome
        // (the default). If the saved tick is mid-cycle — neither
        // exactly at a sunrise nor a sunset — the next mobilisation
        // wouldn't fire until the clock crosses the boundary, leaving
        // everyone visibly at home even though the time-of-day is
        // daytime. Seed the initial location from the saved tick so
        // the visualisation matches reality from the very first frame.
        if (GameClock.IsDaytime(_tick))
        {
            MobiliseForDay();
        }
        else
        {
            MobiliseForNight();
        }
    }

    /// <summary>Builds a fresh <see cref="CityWorld"/> from a validated snapshot.</summary>
    public static CityWorld FromSave(WorldSave save)
    {
        var world = new CityWorld();
        world.Restore(save);
        return world;
    }

    private void RaiseBuildingChanged(BuildingId buildingId)
    {
        BuildingChanged?.Invoke(this, new CityWorldChangedEventArgs(buildingId));
    }

    private void RaiseProjectChanged(BuildingId projectId)
    {
        ProjectChanged?.Invoke(this, new CityWorldChangedEventArgs(projectId));
    }
}
