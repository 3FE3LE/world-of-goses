#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Executes one productive-building tick. Resource ownership and causal-log
/// storage remain with <see cref="CityWorld"/>; narrow delegates cross that
/// boundary until the resource ledger and typed event slices replace them.
/// </summary>
internal sealed class BuildingProductionSimulation
{
    private readonly IReadOnlyDictionary<CitizenId, Citizen> _citizens;
    private readonly WorldEventLog _log;
    private readonly Func<int> _currentTick;
    private readonly Func<Building, Recipe, ResourceType?> _tryConsumeInputs;
    private readonly Func<Building?, ResourceType?, WorldEvent?> _findCauseEvent;
    private readonly Action<BuildingId> _buildingChanged;

    public BuildingProductionSimulation(
        IReadOnlyDictionary<CitizenId, Citizen> citizens,
        WorldEventLog log,
        Func<int> currentTick,
        Func<Building, Recipe, ResourceType?> tryConsumeInputs,
        Func<Building?, ResourceType?, WorldEvent?> findCauseEvent,
        Action<BuildingId> buildingChanged)
    {
        _citizens = citizens;
        _log = log;
        _currentTick = currentTick;
        _tryConsumeInputs = tryConsumeInputs;
        _findCauseEvent = findCauseEvent;
        _buildingChanged = buildingChanged;
    }

    public int SimulateTick(Building building)
    {
        if (!building.ProductionEnabled || building.AssignedCount == 0)
        {
            building.StopCause = ResolveStopCauseWhenNotProducing(building);
            return 0;
        }

        var presentWorkers = new List<Citizen>();
        foreach (CitizenId citizenId in building.AssignedCitizenIds)
        {
            if (_citizens.TryGetValue(citizenId, out Citizen? citizen)
                && citizen.CurrentLocation == CitizenLocation.AtWork)
            {
                presentWorkers.Add(citizen);
            }
        }
        if (presentWorkers.Count == 0)
        {
            building.LastTickProduction = 0;
            building.StopCause = ResolveAbsentWorkerCause(building);
            return 0;
        }
        if (!building.CanProduce)
        {
            building.StopCause = ResolveStopCauseWhenNotProducing(building);
            return 0;
        }
        if (building.Kind == BuildingKind.Forest && building.WoodReserve <= 0)
        {
            building.LastTickProduction = 0;
            building.StopCause = ProductionStopCause.MissingInputs;
            return 0;
        }
        if (!CityEconomyRules.IsProductionCycle(_currentTick()))
        {
            building.LastTickProduction = 0;
            building.StopCause = ProductionStopCause.Authorized;
            return 0;
        }

        var operatingRecipe = Recipes.OperatingRecipeFor(building.Kind);
        if (operatingRecipe is not null && operatingRecipe.RequiredInputs.Count > 0)
        {
            var missing = _tryConsumeInputs(building, operatingRecipe);
            if (missing is not null)
            {
                building.StopCause = ProductionStopCause.MissingInputs;
                _log.Record(
                    _currentTick(),
                    WorldEventKind.ProductionBlocked,
                    WorldEventSubject.Building(building.Id, building.DisplayName),
                    causeEventId: _findCauseEvent(building, missing)?.Id);
                _buildingChanged(building.Id);
                return 0;
            }
        }

        ApplyStaminaCost(presentWorkers, building.Kind);

        var contributing = new List<Citizen>();
        foreach (Citizen citizen in presentWorkers)
        {
            if (citizen.CurrentStamina > 0)
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
        if (building.Kind == BuildingKind.Forest)
        {
            int fromReserve = Math.Min(contributing.Count, building.WoodReserve);
            building.DecrementWoodReserve(fromReserve);
            produced = fromReserve;
        }

        int added = building.AddStock(Math.Min(produced, building.MaxStock - building.Stock));
        building.LastTickProduction = added;
        foreach (var citizen in contributing)
        {
            citizen.AddExperience(building.ProducedCompetencyId, 1);
        }

        if (building.Kind == BuildingKind.Forest && building.WoodReserve == 0)
        {
            building.StopCause = ProductionStopCause.MissingInputs;
            return added;
        }

        building.StopCause = building.Stock >= building.MaxStock
            ? ProductionStopCause.TargetReached
            : ProductionStopCause.Authorized;
        return added;
    }

    public static ProductionStopCause ResolveStopCauseWhenNotProducing(Building building)
    {
        if (!building.ProductionEnabled) return ProductionStopCause.Paused;
        if (building.AssignedCount == 0) return ProductionStopCause.NoWorkers;
        return ProductionStopCause.TargetReached;
    }

    private ProductionStopCause ResolveAbsentWorkerCause(Building building)
    {
        // A temporary lack of present workers must not overwrite the causal
        // production block. In particular, workers released home after the
        // max-stock cooldown are waiting for storage demand, not travelling.
        if (!building.ProductionEnabled) return ProductionStopCause.Paused;
        if (building.Stock >= building.MaxStock) return ProductionStopCause.TargetReached;

        bool hasRecovering = false;
        foreach (CitizenId citizenId in building.AssignedCitizenIds)
        {
            if (!_citizens.TryGetValue(citizenId, out Citizen? citizen)) continue;
            if (citizen.VitalStatus == CitizenVitalStatus.BlockedNoFood)
            {
                return ProductionStopCause.WorkersBlockedNoFood;
            }
            if (citizen.VitalStatus == CitizenVitalStatus.Recovering)
            {
                hasRecovering = true;
            }
        }
        return hasRecovering
            ? ProductionStopCause.WorkersRecovering
            : ProductionStopCause.WorkersInTransit;
    }

    private static void ApplyStaminaCost(IReadOnlyList<Citizen> citizens, BuildingKind kind)
    {
        foreach (Citizen citizen in citizens)
        {
            citizen.ConsumeStamina(StaminaRules.CostForWorker(citizen, kind));
        }
    }
}
