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
    /// produced when stock is near full).
    /// </summary>
    public int AdvanceProduction(BuildingId buildingId)
    {
        if (!_buildings.TryGetValue(buildingId, out var building))
        {
            return 0;
        }
        _tick++;
        return ProduceBuildingTick(building);
    }

    public void AdvanceWorldProductionTick()
    {
        _tick++;
        foreach (var building in _buildings.Values)
        {
            ProduceBuildingTick(building);
        }
    }

    private int ProduceBuildingTick(Building building)
    {
        if (!building.CanProduce) return 0;

        int produced = BuildingProductionCalculator.ProductionPerTick(building, _citizens);
        int roomToTarget = building.TargetStock - building.Stock;
        int added = building.AddStock(Math.Min(produced, roomToTarget));

        // Award a small, deterministic experience bump to every
        // assigned worker, in the building's own competency.
        var competency = building.ProducedCompetencyId;
        foreach (var citizenId in building.AssignedCitizenIds)
        {
            if (_citizens.TryGetValue(citizenId, out var citizen))
            {
                citizen.AddExperience(competency, 1);
            }
        }

        RaiseBuildingChanged(building.Id);
        return added;
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

    /// <summary>
    /// Batched version of <see cref="AdvanceProduction"/> for
    /// offline catch-up: the per-tick production rate is constant
    /// (worker assignment, competencies, base rate all fixed
    /// during a single offline tick batch), so we just multiply
    /// the rate by <paramref name="tickCount"/> and grant
    /// experience once per tick.
    /// </summary>
    public void AdvanceTicks(BuildingId buildingId, int tickCount)
    {
        if (tickCount <= 0) return;
        _tick += tickCount;
        AdvanceBuildingTicks(buildingId, tickCount);
    }

    internal void AdvanceBuildingTicks(BuildingId buildingId, int tickCount)
    {
        if (tickCount <= 0) return;
        if (!_buildings.TryGetValue(buildingId, out var building)) return;

        var competency = building.ProducedCompetencyId;
        foreach (var citizenId in building.AssignedCitizenIds)
        {
            if (_citizens.TryGetValue(citizenId, out var citizen))
            {
                citizen.AddExperience(competency, tickCount);
            }
        }
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
            var citizen = new Citizen(new CitizenId(cs.Id), cs.Name, cs.AppearanceSeed);
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
