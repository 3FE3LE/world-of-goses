#nullable enable
using System;
using System.Collections.Generic;
using WorldofGoses.Domain.Persistence;

namespace WorldofGoses.Domain;

/// <summary>
/// Deterministic, in-memory world state for the vertical slice. The
/// prototype seeds an initial population and two buildings — a
/// Quarry and a Farm — so the rest of the prototype can react to
/// assignment changes across heterogeneous buildings without
/// dealing with generation, persistence, or migration.
///
/// The world exposes events instead of being polled by the
/// presentation layer. The presentation layer never reaches into
/// a building or citizen to mutate state directly.
/// </summary>
public sealed class CityWorld
{
    private readonly Dictionary<CitizenId, Citizen> _citizens = new();
    private readonly Dictionary<BuildingId, Building> _buildings = new();
    private int _tick;

    public CityWorld() : this(seed: true) { }

    /// <summary>
    /// Internal entry point used by <see cref="FromSave"/>: skip
    /// the seed so the world starts empty and the restore step
    /// doesn't allocate 2 buildings + 5 citizens just to throw them
    /// away. Production callers should keep using the public
    /// parameterless constructor.
    /// </summary>
    private CityWorld(bool seed)
    {
        if (seed) Seed();
    }

    /// <summary>
    /// Monotonically increasing world tick. The prototype uses it
    /// only to time-stamp role grants and to drive manual
    /// production ticks.
    /// </summary>
    public int CurrentTick => _tick;

    public IReadOnlyDictionary<CitizenId, Citizen> Citizens => _citizens;
    public IReadOnlyDictionary<BuildingId, Building> Buildings => _buildings;

    public event EventHandler<CityWorldChangedEventArgs>? BuildingChanged;

    private void Seed()
    {
        // Quarry: produces stone, requires mining experience.
        // Pre-assigned: Bran (mining exp 3) + Erin (mining exp 1).
        var quarryId = new BuildingId(1);
        var quarry = new Building(
            id: quarryId,
            displayName: "Quarry",
            kind: BuildingKind.Quarry,
            producedResourceType: ResourceType.Stone,
            producedCompetencyId: CompetencyId.Mining,
            workerCapacity: 6,
            visualCapacity: 3,
            baseProductionPerWorker: 1,
            storageCapacity: 20,
            resourceLabel: "Stone",
            resourceUnit: "stone");
        _buildings[quarryId] = quarry;

        // Farm: produces food, requires farming experience.
        // Pre-assigned: Lior (farming exp 3).
        var farmId = new BuildingId(2);
        var farm = new Building(
            id: farmId,
            displayName: "Farm",
            kind: BuildingKind.Farm,
            producedResourceType: ResourceType.Food,
            producedCompetencyId: CompetencyId.Farming,
            workerCapacity: 4,
            visualCapacity: 2,
            baseProductionPerWorker: 1,
            storageCapacity: 30,
            resourceLabel: "Food",
            resourceUnit: "food");
        _buildings[farmId] = farm;

        // Home: where citizens go at night to rest. Non-producing,
        // non-upkeep-consuming. Capacity matches population so every
        // citizen has a place to rest.
        var homeId = new BuildingId(3);
        var home = new Building(
            id: homeId,
            displayName: "Home",
            kind: BuildingKind.Home,
            producedResourceType: ResourceType.Stone, // placeholder, ignored
            producedCompetencyId: CompetencyId.Mining, // placeholder, ignored
            workerCapacity: 5,
            visualCapacity: 5,
            baseProductionPerWorker: 0,
            storageCapacity: 0,
            resourceLabel: "Rest",
            resourceUnit: "rest",
            productionEnabled: false);
        _buildings[homeId] = home;

        var minerA = new Citizen(new CitizenId(1), "Bran", appearanceSeed: 11);
        minerA.AddExperience(CompetencyId.Mining, 3);
        _citizens[minerA.Id] = minerA;

        var minerB = new Citizen(new CitizenId(2), "Erin", appearanceSeed: 22);
        minerB.AddExperience(CompetencyId.Mining, 1);
        _citizens[minerB.Id] = minerB;

        var farmerA = new Citizen(new CitizenId(3), "Lior", appearanceSeed: 33);
        farmerA.AddExperience(CompetencyId.Farming, 3);
        _citizens[farmerA.Id] = farmerA;

        var availableA = new Citizen(new CitizenId(4), "Mira", appearanceSeed: 44);
        _citizens[availableA.Id] = availableA;

        var availableB = new Citizen(new CitizenId(5), "Toma", appearanceSeed: 55);
        _citizens[availableB.Id] = availableB;

        PreAssign(quarry, quarryId, minerA);
        PreAssign(quarry, quarryId, minerB);
        PreAssign(farm, farmId, farmerA);

        // Game starts at tick 0 (daytime). Assigned citizens are
        // physically at their workplace; unassigned are at home.
        minerA.SetLocation(CitizenLocation.AtWork);
        minerB.SetLocation(CitizenLocation.AtWork);
        farmerA.SetLocation(CitizenLocation.AtWork);
        availableA.SetLocation(CitizenLocation.AtHome);
        availableB.SetLocation(CitizenLocation.AtHome);
    }

    private void PreAssign(Building building, BuildingId buildingId, Citizen citizen)
    {
        var assignment = building.TryAssign(citizen.Id);
        if (!assignment.IsSuccess)
        {
            // The seed's pre-assignments are part of the canonical
            // fixture; a failure here is a developer bug, not a
            // runtime condition (typically: WorkerCapacity too small
            // for the pre-assigned count).
            throw new InvalidOperationException(
                $"Seed pre-assignment failed for {citizen.Name} (id={citizen.Id.Value}) " +
                $"on building {buildingId.Value}: {assignment.Outcome}. " +
                "Adjust the seed so the building's WorkerCapacity covers the pre-assigned citizens.");
        }
        citizen.AssignTo(buildingId);
        citizen.GrantRole(RoleId.Miner, _tick);
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
            citizen.GrantRole(RoleId.Miner, _tick);
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
    /// (day: produce; night: rest). Buffs decrement at the end so a
    /// citizen who eats this tick gets the bonus applied to this
    /// same tick.
    /// </summary>
    public void AdvanceWorldTick()
    {
        int previousTick = _tick;
        _tick++;
        DetectAndApplyMobilisation(previousTick, _tick);
        ApplyUpkeep();
        foreach (var building in _buildings.Values)
        {
            building.LastTickProduction = 0;
            if (GameClock.IsDaytime(_tick))
            {
                int added = SimulateBuildingTick(building);
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
        DecrementAllWellFed();
    }

    /// <summary>
    /// Compares day/night state before and after the tick and
    /// moves citizens to the right place when the boundary
    /// crosses. Called once per world tick from
    /// <see cref="AdvanceWorldTick"/>.
    /// </summary>
    private void DetectAndApplyMobilisation(int previousTick, int currentTick)
    {
        bool wasDay = GameClock.IsDaytime(previousTick);
        bool isDay = GameClock.IsDaytime(currentTick);
        if (wasDay && !isDay)
        {
            MobiliseForNight();
        }
        else if (!wasDay && isDay)
        {
            MobiliseForDay();
        }
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
        int roomToTarget = building.TargetStock - building.Stock;
        int added = building.AddStock(Math.Min(produced, roomToTarget));
        building.LastTickProduction = added;

        var competency = building.ProducedCompetencyId;
        foreach (var citizen in contributing)
        {
            citizen.AddExperience(competency, 1);
        }

        building.StopCause = building.Stock >= building.TargetStock
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

    public void ConfigureProductionPolicy(BuildingId buildingId, bool enabled, int targetStock)
    {
        if (!_buildings.TryGetValue(buildingId, out var building))
        {
            return;
        }

        building.ConfigureProductionPolicy(enabled, targetStock);
        RaiseBuildingChanged(buildingId);
    }

    internal void AdvanceWorldClock(int tickCount)
    {
        if (tickCount > 0) _tick += tickCount;
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
                productionEnabled: bs.ProductionEnabled,
                targetStock: bs.TargetStock ?? bs.StorageCapacity);
            _buildings[building.Id] = building;

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

            _citizens[citizen.Id] = citizen;
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

    /// <summary>
    /// Builds a fresh <see cref="CityWorld"/> from a save
    /// snapshot. Skips the seed step so the restore doesn't
    /// allocate 2 buildings + 5 citizens just to throw them away.
    /// </summary>
    public static CityWorld FromSave(WorldSave save)
    {
        var world = new CityWorld(seed: false);
        world.Restore(save);
        return world;
    }

    private void RaiseBuildingChanged(BuildingId buildingId)
    {
        BuildingChanged?.Invoke(this, new CityWorldChangedEventArgs(buildingId));
    }
}
