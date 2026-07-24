#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly Dictionary<ParcelId, CityParcel> _parcels = new();
    private readonly Dictionary<int, NaturalResourcePatch> _naturalResourcePatches = new();
    private readonly Dictionary<BuildingId, ParcelPlacement> _parcelPlacements = new();
    private readonly WorldEventLog _log = new();
    private readonly CityInventory _inventory = new();
    private readonly CityResourceLedger _resources;
    private readonly CitizenAssignmentService _assignments;
    private readonly BuildingProductionSimulation _production;
    private readonly ConstructionSimulation _construction;
    private int _tick;
    private int _nextProjectId = 1;

    private static readonly CitizenId PrincipalHeroId = new(1);

    /// <summary>A new world is intentionally empty until onboarding creates its hero.</summary>
    public CityWorld()
    {
        _resources = new CityResourceLedger(_buildings, _inventory);
        _assignments = new CitizenAssignmentService(
            _citizens,
            _buildings,
            _projects,
            RaiseBuildingChanged,
            RaiseProjectChanged);
        _production = new BuildingProductionSimulation(
            _citizens,
            _log,
            () => _tick,
            TryConsumeFood,
            TryConsumeOperatingInputs,
            FindCauseEvent,
            RaiseBuildingChanged);
        _construction = new ConstructionSimulation(
            _citizens,
            _log,
            () => _tick,
            TryConsumeResources,
            () => FindCauseEvent(),
            RaiseProjectChanged);
    }

    public int CurrentTick => _tick;
    public IReadOnlyDictionary<CitizenId, Citizen> Citizens => _citizens;
    public IReadOnlyDictionary<BuildingId, Building> Buildings => _buildings;
    public IReadOnlyDictionary<BuildingId, ConstructionProject> Projects => _projects;
    public IReadOnlyDictionary<ParcelId, CityParcel> Parcels => _parcels;
    public IReadOnlyDictionary<int, NaturalResourcePatch> NaturalResourcePatches =>
        _naturalResourcePatches;
    public IReadOnlyDictionary<BuildingId, ParcelPlacement> ParcelPlacements =>
        _parcelPlacements;

    /// <summary>Read-only view of the chronological event log.</summary>
    public WorldEventLog Log => _log;
    public CityResourceLedger Resources => _resources;

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

    public CityWorld CreateRestartedCityKeepingHero()
    {
        Citizen? hero = Hero;
        if (hero is null)
        {
            throw new InvalidOperationException(
                "A city without a founder cannot be soft-reset.");
        }

        var restarted = new CityWorld();
        HeroCreationResult result = restarted.TryCreateHero(
            new HeroCreationRequest(
                hero.Name,
                hero.Profile,
                hero.Profile.Gender));
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Could not preserve the founder during soft reset: {result.Outcome}.");
        }
        restarted.SeedStartingForests();
        return restarted;
    }

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
    /// Number of individually visible trees in each founding resource patch.
    /// </summary>
    public const int StartingForestUnitCount = 8;

    /// <summary>Wood held by each tree in a founding resource patch.</summary>
    public const int StartingTreeWoodReserve = 40;

    /// <summary>Total reserve across one founding resource patch.</summary>
    public const int StartingForestWoodReserve =
        StartingForestUnitCount * StartingTreeWoodReserve;

    /// <summary>Per-forest compatibility-storage capacity for gathered wood.</summary>
    public const int StartingForestStorageCapacity = StartingForestWoodReserve * 2;

    /// <summary>
    /// Drops two Forests into the world so the hero has a wood source
    /// to gather from. Each Forest starts with
    /// <see cref="StartingForestWoodReserve"/> wood still in it.
    /// IDs are reserved (100, 101) so they never collide with future
    /// player-authorised buildings. Safe to call from any path:
    /// - no-op when no hero exists (no point seeding before founding),
    /// - no-op when the world already has a Forest (idempotent),
    /// - otherwise seeds two Forests. This is intentionally permissive
    ///   about pre-existing non-Forest buildings so a hero who already
    ///   finished the founding step still receives forests when their
    ///   save predated the wood-gathering slice.
    /// </summary>
    public void SeedStartingForests()
    {
        if (_citizens.Count == 0) return;
        foreach (NaturalResourcePatch patch in _naturalResourcePatches.Values)
        {
            if (patch.ResourceType == ResourceType.Wood) return;
        }
        foreach (var b in _buildings.Values)
        {
            if (b.Kind == BuildingKind.Forest) return;
        }

        var forest1 = new Building(
            id: new BuildingId(100),
            displayName: "Forest",
            kind: BuildingKind.Forest,
            producedResourceType: ResourceType.Wood,
            producedCompetencyId: CompetencyId.Foraging,
            workerCapacity: 2,
            visualCapacity: 2,
            baseProductionPerWorker: 1,
            storageCapacity: StartingForestStorageCapacity,
            resourceLabel: "Wood",
            resourceUnit: "wood");
        forest1.RestoreWoodUnits(
            Enumerable.Repeat(StartingTreeWoodReserve, StartingForestUnitCount));

        var forest2 = new Building(
            id: new BuildingId(101),
            displayName: "Forest",
            kind: BuildingKind.Forest,
            producedResourceType: ResourceType.Wood,
            producedCompetencyId: CompetencyId.Foraging,
            workerCapacity: 2,
            visualCapacity: 2,
            baseProductionPerWorker: 1,
            storageCapacity: StartingForestStorageCapacity,
            resourceLabel: "Wood",
            resourceUnit: "wood");
        forest2.RestoreWoodUnits(
            Enumerable.Repeat(StartingTreeWoodReserve, StartingForestUnitCount));

        RegisterBuilding(forest1);
        RegisterBuilding(forest2);
        EnsureFoundingParcels();
        RegisterNaturalResourcePatch(new NaturalResourcePatch(
            forest1.Id.Value, new ParcelId(1), ResourceType.Wood,
            forest1.WoodUnitReserves, forest1.Id));
        RegisterNaturalResourcePatch(new NaturalResourcePatch(
            forest2.Id.Value, new ParcelId(2), ResourceType.Wood,
            forest2.WoodUnitReserves, forest2.Id));
    }

    private void EnsureFoundingParcels()
    {
        const int columnCount = 4;
        const int rowCount = 2;
        for (int row = 0; row < rowCount; row++)
        {
            for (int column = 0; column < columnCount; column++)
            {
                var parcelId = new ParcelId(row * columnCount + column + 1);
                _parcels.TryAdd(
                    parcelId,
                    new CityParcel(parcelId, column, row, isUnlocked: true));
            }
        }
    }

    private void RegisterNaturalResourcePatch(NaturalResourcePatch patch)
    {
        if (!_parcels.ContainsKey(patch.ParcelId))
        {
            throw new InvalidOperationException(
                $"Natural resource patch {patch.Id} references unknown parcel {patch.ParcelId.Value}.");
        }
        if (!_naturalResourcePatches.TryAdd(patch.Id, patch))
        {
            throw new InvalidOperationException($"Natural resource patch id {patch.Id} already exists.");
        }
    }

    private void RegisterParcelPlacement(ParcelPlacement placement)
    {
        if (!_parcels.TryGetValue(placement.ParcelId, out CityParcel? parcel)
            || !parcel.IsUnlocked)
        {
            throw new InvalidOperationException(
                $"Placement {placement.EntityId.Value} requires an unlocked parcel.");
        }
        if (NaturalResourceOccupiesLot(
            placement.ParcelId,
            placement.LotColumn,
            placement.LotRow))
        {
            throw new InvalidOperationException(
                $"Placement {placement.EntityId.Value} overlaps a natural resource.");
        }
        foreach (ParcelPlacement existing in _parcelPlacements.Values)
        {
            if (placement.Overlaps(existing))
            {
                throw new InvalidOperationException(
                    $"Placement {placement.EntityId.Value} overlaps {existing.EntityId.Value}.");
            }
        }
        if (!_parcelPlacements.TryAdd(placement.EntityId, placement))
        {
            throw new InvalidOperationException(
                $"Placement for entity {placement.EntityId.Value} already exists.");
        }
    }

    private ParcelPlacement? FindFirstAvailablePlacement(
        BuildingId entityId,
        string footprintProfileId)
    {
        IReadOnlyList<ConstructionLot> lots = AvailableConstructionLots();
        if (lots.Count == 0) return null;
        ConstructionLot lot = lots[0];
        return CreatePlacement(entityId, lot, footprintProfileId);
    }

    public IReadOnlyList<ConstructionLot> AvailableConstructionLots()
    {
        var lots = new List<ConstructionLot>();
        var candidateId = new BuildingId(int.MaxValue);
        foreach (CityParcel parcel in _parcels.Values)
        {
            if (!parcel.IsUnlocked) continue;
            for (int row = 0; row < ParcelGrid.LotsPerAxis; row++)
            {
                for (int column = 0; column < ParcelGrid.LotsPerAxis; column++)
                {
                    if (NaturalResourceOccupiesLot(parcel.Id, column, row)) continue;
                    var lot = new ConstructionLot(
                        parcel.Id,
                        parcel.LogicalColumn,
                        parcel.LogicalRow,
                        column,
                        row);
                    ParcelPlacement candidate = CreatePlacement(
                        candidateId,
                        lot,
                        BuildingFootprintCatalog.StandardWithSideSetbacksId);
                    bool occupied = false;
                    foreach (ParcelPlacement existing in _parcelPlacements.Values)
                    {
                        if (candidate.Overlaps(existing))
                        {
                            occupied = true;
                            break;
                        }
                    }
                    if (!occupied) lots.Add(lot);
                }
            }
        }
        return lots;
    }

    private static ParcelPlacement CreatePlacement(
        BuildingId entityId,
        ConstructionLot lot,
        string footprintProfileId) =>
        new(
            entityId,
            lot.ParcelId,
            lot.LotColumn,
            lot.LotRow,
            lotWidth: 1,
            lotHeight: 1,
            footprintProfileId,
            BuildingOrientation.South);

    private bool NaturalResourceOccupiesLot(
        ParcelId parcelId,
        int lotColumn,
        int lotRow)
    {
        foreach (NaturalResourcePatch patch in _naturalResourcePatches.Values)
        {
            if (patch.ParcelId != parcelId) continue;
            for (int unitId = 0; unitId < patch.UnitReserves.Count; unitId++)
            {
                if (patch.UnitReserves[unitId] <= 0) continue;
                (int column, int row) = ParcelGrid.NaturalResourceLot(unitId);
                if (column == lotColumn && row == lotRow) return true;
            }
        }
        return false;
    }

    internal void RegisterCitizen(Citizen citizen)
    {
        ArgumentNullException.ThrowIfNull(citizen);
        if (!_citizens.TryAdd(citizen.Id, citizen))
        {
            throw new InvalidOperationException($"Citizen id {citizen.Id.Value} already exists.");
        }
    }

    internal void RegisterBuilding(Building building, bool placeIfMissing = true)
    {
        ArgumentNullException.ThrowIfNull(building);
        if (!_buildings.TryAdd(building.Id, building))
        {
            throw new InvalidOperationException($"Building id {building.Id.Value} already exists.");
        }
        if (placeIfMissing
            && building.Kind != BuildingKind.Forest
            && !_parcelPlacements.ContainsKey(building.Id))
        {
            if (_parcels.Count == 0)
            {
                var parcelId = new ParcelId(1);
                _parcels.Add(parcelId, new CityParcel(parcelId, 0, 0, true));
            }
            ParcelPlacement? placement = FindFirstAvailablePlacement(
                building.Id,
                BuildingFootprintCatalog.ProfileIdFor(building.Kind));
            if (placement is null)
            {
                _buildings.Remove(building.Id);
                throw new InvalidOperationException(
                    $"No available parcel lot for building {building.Id.Value}.");
            }
            RegisterParcelPlacement(placement);
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
            return _resources.Total(ResourceType.Food);
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
            return _resources.Total(ResourceType.Wood);
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
            foreach (NaturalResourcePatch patch in _naturalResourcePatches.Values)
            {
                if (patch.ResourceType == ResourceType.Wood) total += patch.TotalReserve;
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
        return _resources.Deposit(ResourceType.Food, amount);
    }

    /// <summary>
    /// Atomically removes <paramref name="amount"/> food from Farm-kind
    /// buildings. Returns <c>false</c> (and leaves state untouched)
    /// when there is not enough food.
    /// </summary>
    public bool TryConsumeFood(int amount)
    {
        return _resources.TryConsume(ResourceType.Food, amount);
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
        return _resources.Total(type);
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
        return _resources.TryConsume(type, amount);
    }

    /// <summary>
    /// Consumes the per-tick operating recipe inputs for the given
    /// building. Returns the first missing <see cref="ResourceType"/>
    /// on failure (transactional: no partial drawdown is left
    /// applied). Returns <c>null</c> on success.
    /// </summary>
    private ResourceType? TryConsumeOperatingInputs(Building building, Recipe recipe)
    {
        return _resources.TryConsume(recipe.RequiredInputs, out ResourceType? missing)
            ? null
            : missing;
    }

    /// <summary>
    /// Returns the most recent <see cref="WorldEvent"/> whose
    /// typed subject matches the building identity, or <c>null</c> when none
    /// exists. Used to wire
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
            if (building is not null
                && (evt.Subject.Kind != WorldEventSubjectKind.Building
                    || evt.Subject.EntityId != building.Id.Value)) continue;
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
        return GatherWood(forestId, unitId: null, amount: amount);
    }

    public int GatherWood(BuildingId forestId, int? unitId, int amount)
    {
        if (amount <= 0) return 0;
        Citizen? hero = Hero;
        if (hero is null || hero.CurrentAssignment.HasValue) return 0;
        if (!_buildings.TryGetValue(forestId, out var forest)) return 0;
        if (forest.Kind != BuildingKind.Forest) return 0;
        int gathered;
        if (_naturalResourcePatches.TryGetValue(
            forestId.Value,
            out NaturalResourcePatch? patch))
        {
            int drained = unitId.HasValue
                ? patch.GatherUnit(unitId.Value, amount)
                : patch.Gather(amount);
            forest.RestoreWoodUnits(patch.UnitReserves);
            gathered = _resources.DepositToCityInventory(
                ResourceType.Wood,
                drained);
        }
        else
        {
            gathered = unitId.HasValue
                ? forest.GatherWoodUnit(unitId.Value, amount)
                : forest.GatherWood(amount);
        }
        if (gathered > 0)
        {
            if (unitId.HasValue)
            {
                hero.VisitResource(
                    forestId,
                    unitId.Value,
                    ResourcePositionIndex(forestId, unitId.Value));
            }
            WorldEventId? cause = FindCauseEvent(forest)?.Id;
            _log.Record(_tick, WorldEventKind.StockProduced,
                WorldEventSubject.Building(forest.Id, forest.DisplayName), gathered, cause);
            RaiseBuildingChanged(forestId);
        }
        return gathered;
    }

    private int ResourcePositionIndex(BuildingId forestId, int unitId)
    {
        int positionIndex = 0;
        foreach (Building building in _buildings.Values)
        {
            if (building.Kind != BuildingKind.Forest) continue;
            if (building.Id == forestId)
            {
                return positionIndex + unitId;
            }
            positionIndex += building.WoodUnitReserves.Count;
        }
        return Math.Max(0, unitId);
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
        => _assignments.AssignToBuilding(buildingId, citizenId, _tick);

    /// <summary>
    /// Attempts to remove a citizen from a building.
    /// </summary>
    public AssignmentResult TryUnassignCitizen(BuildingId buildingId, CitizenId citizenId)
        => _assignments.UnassignFromBuilding(buildingId, citizenId);

    /// <summary>
    /// Unassigns every assigned citizen from the given building. Used
    /// by the auto-release watch when a building has been at max stock
    /// long enough to rule out a brief production peak and the workers
    /// should be re-deployed elsewhere.
    /// </summary>
    private void ReleaseAssignedWorkers(Building building) =>
        _assignments.ReleaseBuilding(building);

    /// <summary>
    /// Attempts to assign a citizen to a worksite. The id is shared
    /// with the future building so <see cref="Citizen.CurrentAssignment"/>
    /// remains a plain <see cref="BuildingId"/>?>.
    /// </summary>
    public AssignmentResult TryAssignToProject(BuildingId projectId, CitizenId citizenId)
        => _assignments.AssignToProject(projectId, citizenId, _tick);

    public AssignmentResult TryUnassignFromProject(BuildingId projectId, CitizenId citizenId)
        => _assignments.UnassignFromProject(projectId, citizenId);

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
    public ConstructionAuthorizationResult TryAuthorizeConstruction(
        ConstructionKind kind,
        ConstructionLot? selectedLot = null)
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

        var projectId = NextAvailableProjectId();
        string footprintProfileId = BuildingFootprintCatalog.ProfileIdFor(kind);
        ParcelPlacement? placement = selectedLot.HasValue
            && AvailableConstructionLots().Contains(selectedLot.Value)
                ? CreatePlacement(projectId, selectedLot.Value, footprintProfileId)
                : selectedLot.HasValue
                    ? null
                    : FindFirstAvailablePlacement(projectId, footprintProfileId);
        if (placement is null)
        {
            return ConstructionAuthorizationResult.Fail(
                ConstructionAuthorizationOutcome.NoAvailableLot);
        }

        // Recipe gate: a non-empty recipe must be satisfiable up-front
        // (deposit = ceil(total * 0.25)) or the authorisation fails
        // and the city state is unchanged.
        var recipe = Recipes.ConstructionRecipeFor(kind);
        if (recipe is not null && recipe.RequiredInputs.Count > 0)
        {
            var deposits = new List<RecipeInput>();
            foreach (var input in recipe.RequiredInputs)
            {
                int deposit = ConstructionRules.DepositOf(input.Amount);
                if (deposit > 0) deposits.Add(new RecipeInput(input.Resource, deposit));
            }
            if (TryConsumeResources(deposits) is not null)
            {
                return ConstructionAuthorizationResult.Fail(ConstructionAuthorizationOutcome.MissingMaterials);
            }
        }

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
        RegisterParcelPlacement(placement);
        if (kind == ConstructionKind.BasicShelter)
        {
            EnsureFoundingShelterContributor();
        }
        RaiseProjectChanged(projectId);
        return ConstructionAuthorizationResult.Success(projectId);
    }

    /// <summary>
    /// Assigns the lone available founder to an in-flight Basic Shelter.
    /// Used on authorisation and once after loading older stalled saves.
    /// It never overrides an existing assignment or a deliberate contributor.
    /// </summary>
    public bool EnsureFoundingShelterContributor()
    {
        Citizen? hero = Hero;
        if (hero is null || hero.CurrentAssignment.HasValue) return false;
        foreach (ConstructionProject project in _projects.Values)
        {
            if (project.Kind != ConstructionKind.BasicShelter
                || project.AssignedCount > 0
                || project.Progress >= project.RequiredWork)
            {
                continue;
            }
            return _assignments.AssignToProject(project.Id, hero.Id, _tick).IsSuccess;
        }
        return false;
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
        _parcelPlacements.Remove(projectId);
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
        _resources.Deposit(type, amount);
    }

    private ResourceType? TryConsumeResources(IReadOnlyList<RecipeInput> inputs) =>
        _resources.TryConsume(inputs, out ResourceType? missing) ? null : missing;

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
            if (GameClock.IsDaytime(_tick))
            {
                _log.Record(_tick, WorldEventKind.DayBegan, WorldEventSubject.World("Sun"));
                RegenerateNaturalResources();
            }
            else _log.Record(_tick, WorldEventKind.NightBegan, WorldEventSubject.World("Sun"));
        }
        ApplyUpkeep();
        foreach (var building in _buildings.Values)
        {
            building.LastTickProduction = 0;
            if (GameClock.IsDaytime(_tick))
            {
                ProductionStopCause previousStopCause = building.StopCause;
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
                    WorldEventId? cause = FindCauseEvent(building)?.Id;
                    _log.Record(_tick, WorldEventKind.StockProduced,
                        WorldEventSubject.Building(building.Id, building.DisplayName), added, cause);
                }
                if (building.StopCause == ProductionStopCause.WorkersExhausted)
                {
                    _log.Record(_tick, WorldEventKind.WorkersExhausted,
                        WorldEventSubject.Building(building.Id, building.DisplayName));
                }
                if (building.StopCause == ProductionStopCause.TargetReached
                    && previousStopCause != ProductionStopCause.TargetReached)
                {
                    _log.Record(_tick, WorldEventKind.StockCapped,
                        WorldEventSubject.Building(building.Id, building.DisplayName));
                }
                if (added > 0
                    || building.StopCause == ProductionStopCause.WorkersExhausted)
                {
                    RaiseBuildingChanged(building.Id);
                }

                // Auto-release workers after the building has been at
                // max stock long enough to rule out a brief production
                // peak. Any consumption that drops the stock below the
                // cap resets the watch.
                if (building.AssignedCount > 0 && building.TickMaxStockWatch())
                {
                    ReleaseAssignedWorkers(building);
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
                _construction.SimulateTick(project, isWorkInterval);
                if (project.LastTickProgressAdded > 0)
                {
                    _log.Record(_tick, WorldEventKind.ProjectProgressed,
                        WorldEventSubject.ConstructionProject(project.Id, project.DisplayName),
                        project.LastTickProgressAdded);
                }
                if (project.Progress != previousProgress
                    || project.StopCause != previousStopCause)
                {
                    RaiseProjectChanged(project.Id);
                }
            }
            else
            {
                _construction.ApplyNightRest(project);
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
        DemolishDepletedForests();
        DecrementAllWellFed();
    }

    /// <summary>
    /// Removes Forests only after both their natural reserve and their
    /// gathered stock are empty. The Forest remains as the owning
    /// storage location while gathered wood is waiting to be consumed;
    /// deleting it when only the reserve reaches zero would destroy
    /// player-owned stock. Other building kinds never trigger this path.
    /// </summary>
    private void DemolishDepletedForests()
    {
        List<BuildingId>? depleted = null;
        foreach (var pair in _buildings)
        {
            if (pair.Value.Kind != BuildingKind.Forest) continue;
            if (_naturalResourcePatches.ContainsKey(pair.Key.Value)) continue;
            if (pair.Value.WoodReserve > 0) continue;
            if (pair.Value.Stock > 0) continue;
            depleted ??= new List<BuildingId>();
            depleted.Add(pair.Key);
        }
        if (depleted is null) return;

        foreach (var id in depleted)
        {
            Building building = _buildings[id];
            RemoveBuildingInternal(id);
            _log.Record(_tick, WorldEventKind.ForestDemolished,
                WorldEventSubject.Building(id, building.DisplayName));
        }
        if (depleted.Count > 0)
        {
            RaiseBuildingChanged(depleted[0]);
        }
    }

    private void RegenerateNaturalResources()
    {
        foreach (NaturalResourcePatch patch in _naturalResourcePatches.Values)
        {
            if (patch.ResourceType != ResourceType.Wood) continue;
            int added = patch.Regenerate(
                amountPerUnit: 1,
                unitCapacity: StartingTreeWoodReserve,
                canGrowAtUnit: unitId =>
                {
                    (int column, int row) = ParcelGrid.NaturalResourceLot(unitId);
                    return !ConstructionOccupiesLot(patch.ParcelId, column, row);
                });
            if (added <= 0) continue;
            if (patch.LegacyStorageBuildingId is not BuildingId storageId
                || !_buildings.TryGetValue(storageId, out Building? storage))
            {
                continue;
            }
            storage.RestoreWoodUnits(patch.UnitReserves);
            RaiseBuildingChanged(storageId);
        }
    }

    private bool ConstructionOccupiesLot(
        ParcelId parcelId,
        int lotColumn,
        int lotRow)
    {
        foreach (ParcelPlacement placement in _parcelPlacements.Values)
        {
            if (placement.ParcelId != parcelId) continue;
            if (lotColumn >= placement.LotColumn
                && lotColumn < placement.LotColumn + placement.LotWidth
                && lotRow >= placement.LotRow
                && lotRow < placement.LotRow + placement.LotHeight)
            {
                return true;
            }
        }
        return false;
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
        _production.ApplyFoodAndRegen(building);
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
        => _production.SimulateTick(building);

    private void CompleteProject(BuildingId projectId)
    {
        if (!_projects.TryGetValue(projectId, out var project)) return;
        var contributorIds = new List<CitizenId>(project.AssignedCitizenIds);

        var building = CreateCompletedBuilding(project);

        RegisterBuilding(building);
        RaiseBuildingChanged(building.Id);
        _log.Record(_tick, WorldEventKind.ProjectCompleted,
            WorldEventSubject.ConstructionProject(project.Id, project.DisplayName));
        _log.Record(_tick, WorldEventKind.BuildingCreated,
            WorldEventSubject.Building(building.Id, building.DisplayName));

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

    /// <summary>
    /// Flips a building's <see cref="Building.ProductionEnabled"/>
    /// flag without touching its reactive <c>MinStock</c>/<c>MaxStock</c>/
    /// <c>Priority</c> triplet. The presentation layer uses this when the
    /// player toggles the simple on/off button. Future slices that
    /// expose the triplet as a UI again will revert to
    /// <see cref="ConfigureProductionPolicy"/>.
    /// </summary>
    public void SetProductionEnabled(BuildingId buildingId, bool enabled)
    {
        if (!_buildings.TryGetValue(buildingId, out var building))
        {
            return;
        }

        building.ConfigureProductionPolicy(
            enabled,
            building.MinStock,
            building.MaxStock,
            building.Priority);
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
    /// Advances a same-phase range for a city that has structures but no work
    /// assignments. Returns the number of ticks consumed, or zero when the
    /// canonical per-tick path is required. Day/night boundaries are excluded
    /// so mobilisation and its causal event remain canonical stepped ticks.
    /// </summary>
    internal int TryAdvanceQuiescentTicks(int maxTickCount)
    {
        if (maxTickCount <= 0 || HasAnyWorkAssignment()) return 0;

        foreach (var building in _buildings.Values)
        {
            if (building.Kind == BuildingKind.Forest
                && building.WoodReserve <= 0
                && building.Stock <= 0)
            {
                return 0;
            }
        }
        foreach (var project in _projects.Values)
        {
            if (project.AssignedCount > 0 || project.Progress >= project.RequiredWork)
            {
                return 0;
            }
        }

        int dayTick = ((_tick % GameClock.TicksPerInGameDay)
            + GameClock.TicksPerInGameDay) % GameClock.TicksPerInGameDay;
        int lastTickInPhase = GameClock.IsDaytime(_tick)
            ? GameClock.DayTicks - 1
            : GameClock.TicksPerInGameDay - 1;
        int ticksBeforeBoundary = lastTickInPhase - dayTick;
        int tickCount = Math.Min(maxTickCount, ticksBeforeBoundary);
        if (tickCount <= 0) return 0;

        ApplyUpkeepBatch(tickCount);
        bool isDaytime = GameClock.IsDaytime(_tick);
        foreach (var building in _buildings.Values)
        {
            building.LastTickProduction = 0;
            building.StopCause = isDaytime
                ? BuildingProductionSimulation.ResolveStopCauseWhenNotProducing(building)
                : ProductionStopCause.Night;
        }
        foreach (var project in _projects.Values)
        {
            project.LastTickProgressAdded = 0;
            project.StopCause = isDaytime
                ? project.Enabled
                    ? ConstructionStopCause.NoWorkers
                    : ConstructionStopCause.Paused
                : ConstructionStopCause.Night;
        }
        foreach (var citizen in _citizens.Values)
        {
            citizen.AdvanceWellFedTicks(tickCount);
        }
        _tick += tickCount;
        return tickCount;
    }

    private void ApplyUpkeepBatch(int tickCount)
    {
        long remaining = (long)Upkeep.StonePerTick(_citizens.Count) * tickCount;
        if (remaining <= 0) return;

        foreach (var building in _buildings.Values)
        {
            if (building.Kind != BuildingKind.Quarry || building.Stock <= 0) continue;
            int consumed = (int)Math.Min(remaining, building.Stock);
            building.TryConsumeStock(consumed);
            remaining -= consumed;
            if (remaining <= 0) return;
        }
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
        _parcels.Clear();
        _naturalResourcePatches.Clear();
        _parcelPlacements.Clear();
        _log.Clear();
        _resources.ClearReservations();
        _nextProjectId = 1;
        _tick = save.CurrentTick;

        foreach (ParcelSave parcel in save.Parcels)
        {
            var restoredParcel = new CityParcel(
                new ParcelId(parcel.Id),
                parcel.LogicalColumn,
                parcel.LogicalRow,
                parcel.IsUnlocked);
            _parcels.Add(restoredParcel.Id, restoredParcel);
        }

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

            // Old saves predate the wood-gathering slice and have no
            // WoodReserve field; for Forest plots, seed them with
            // the starting reserve so the saving doesn't auto-demolish
            // them on the first tick. Fresh worlds (already carrying
            // a WoodReserve) preserve their state.
            if (kind == BuildingKind.Forest && bs.WoodUnitReserves is { Count: > 0 })
            {
                building.RestoreWoodUnits(bs.WoodUnitReserves);
            }
            else if (kind == BuildingKind.Forest && bs.WoodReserve is null)
            {
                building.SeedWoodReserve(StartingForestWoodReserve);
                if (bs.WorkerCapacity == 0)
                {
                    // Old saves serialised Forest with capacity 0 (a
                    // marker for "non-productive in v2"). Re-apply the
                    // v4 defaults so the player can assign workers.
                    building.ReplaceForestCapacity(
                        workerCapacity: 2,
                        visualCapacity: 2,
                        baseProductionPerWorker: 1);
                }
            }
            else
            {
                building.SeedWoodReserve(bs.WoodReserve ?? 0);
            }
            building.DepositIron(bs.IronStock);

            RegisterBuilding(building, placeIfMissing: false);

            foreach (var cid in bs.AssignedCitizenIds)
            {
                // Building.TryAssign is internal — same-assembly access.
                building.TryAssign(new CitizenId(cid));
            }
        }

        foreach (NaturalResourcePatchSave patch in save.NaturalResourcePatches)
        {
            ResourceType type = Enum.TryParse(
                patch.ResourceType,
                ignoreCase: true,
                out ResourceType parsedType)
                ? parsedType
                : ResourceType.Wood;
            RegisterNaturalResourcePatch(new NaturalResourcePatch(
                patch.Id,
                new ParcelId(patch.ParcelId),
                type,
                patch.UnitReserves,
                patch.LegacyStorageBuildingId.HasValue
                    ? new BuildingId(patch.LegacyStorageBuildingId.Value)
                    : null));
        }
        EnsureFoundingParcels();

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
                initialWellFedTicks: cs.WellFedRemainingTicks,
                appearanceVariant: string.IsNullOrEmpty(cs.AppearanceVariant)
                    ? (AppearanceVariantId?)null
                    : new AppearanceVariantId(cs.AppearanceVariant));
            if (cs.CurrentAssignment.HasValue)
            {
                citizen.AssignTo(new BuildingId(cs.CurrentAssignment.Value));
            }
            if (cs.LastVisitedResourceBuildingId.HasValue
                && cs.LastVisitedResourceUnitId.HasValue)
            {
                citizen.VisitResource(
                    new BuildingId(cs.LastVisitedResourceBuildingId.Value),
                    cs.LastVisitedResourceUnitId.Value,
                    cs.LastVisitedResourcePositionIndex
                        ?? ResourcePositionIndex(
                            new BuildingId(cs.LastVisitedResourceBuildingId.Value),
                            cs.LastVisitedResourceUnitId.Value));
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

        foreach (ParcelPlacementSave placement in save.ParcelPlacements)
        {
            BuildingOrientation orientation = Enum.TryParse(
                placement.Orientation,
                ignoreCase: true,
                out BuildingOrientation parsedOrientation)
                ? parsedOrientation
                : BuildingOrientation.South;
            var restoredPlacement = new ParcelPlacement(
                new BuildingId(placement.EntityId),
                new ParcelId(placement.ParcelId),
                placement.LotColumn,
                placement.LotRow,
                placement.LotWidth,
                placement.LotHeight,
                placement.FootprintProfileId,
                orientation);
            if (NaturalResourceOccupiesLot(
                restoredPlacement.ParcelId,
                restoredPlacement.LotColumn,
                restoredPlacement.LotRow))
            {
                restoredPlacement = FindFirstAvailablePlacement(
                    restoredPlacement.EntityId,
                    restoredPlacement.FootprintProfileId)
                    ?? throw new InvalidOperationException(
                        $"No resource-free parcel lot is available for entity "
                        + $"{restoredPlacement.EntityId.Value}.");
            }
            RegisterParcelPlacement(restoredPlacement);
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

        var restoredEvents = new List<WorldEvent>(save.Events.Count);
        foreach (var evt in save.Events)
        {
            _ = Enum.TryParse(evt.Kind, ignoreCase: true, out WorldEventKind kind);
            _ = Enum.TryParse(evt.SubjectKind, ignoreCase: true, out WorldEventSubjectKind subjectKind);
            restoredEvents.Add(new WorldEvent(
                new WorldEventId(evt.Id),
                evt.Tick,
                kind,
                new WorldEventSubject(subjectKind, evt.SubjectEntityId, evt.SubjectDisplayName),
                evt.Amount,
                evt.CauseEventId is int causeId ? new WorldEventId(causeId) : null));
        }
        _log.Restore(restoredEvents);

        var restoredReservations = new List<ResourceReservation>(save.ResourceReservations.Count);
        foreach (var reservation in save.ResourceReservations)
        {
            _ = Enum.TryParse(reservation.Resource, ignoreCase: true, out ResourceType resource);
            _ = Enum.TryParse(reservation.OwnerKind, ignoreCase: true,
                out ResourceReservationOwnerKind ownerKind);
            restoredReservations.Add(new ResourceReservation(
                new ResourceReservationId(reservation.Id),
                resource,
                reservation.Amount,
                new ResourceReservationOwner(ownerKind, reservation.OwnerEntityId)));
        }
        _resources.RestoreReservations(restoredReservations);
        var restoredInventory = new Dictionary<ResourceType, int>();
        foreach ((string key, int amount) in save.CityInventory)
        {
            _ = Enum.TryParse(key, ignoreCase: true, out ResourceType resource);
            restoredInventory[resource] = amount;
        }
        _inventory.Restore(restoredInventory);
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

    /// <summary>
    /// Removes a building from the world without raising an event.
    /// Used by the per-tick depletion sweep that might demolish
    /// several forests in one pass; the caller emits one
    /// <see cref="RaiseBuildingChanged"/> for the batch when needed.
    /// Free any assigned citizens via
    /// <see cref="TryUnassignCitizen"/> first so the world state
    /// stays consistent.
    /// </summary>
    private void RemoveBuildingInternal(BuildingId buildingId)
    {
        if (!_buildings.TryGetValue(buildingId, out var building)) return;

        // Free assigned citizens so they can be re-assigned elsewhere.
        var assignedCopy = new List<CitizenId>(building.AssignedCitizenIds);
        foreach (var citizenId in assignedCopy)
        {
            TryUnassignCitizen(buildingId, citizenId);
        }
        _buildings.Remove(buildingId);
        _parcelPlacements.Remove(buildingId);
    }

    /// <summary>
    /// Public demolition path: removes a building immediately and
    /// notifies subscribers. Used when the player explicitly tears
    /// down a building (future slice). Today's <see cref="DemolishDepletedForests"/>
    /// sweep batches internally to avoid per-building event spam.
    /// </summary>
    public bool RemoveBuilding(BuildingId buildingId)
    {
        if (!_buildings.ContainsKey(buildingId)) return false;
        RemoveBuildingInternal(buildingId);
        RaiseBuildingChanged(buildingId);
        return true;
    }

    private void RaiseProjectChanged(BuildingId projectId)
    {
        ProjectChanged?.Invoke(this, new CityWorldChangedEventArgs(projectId));
    }
}
