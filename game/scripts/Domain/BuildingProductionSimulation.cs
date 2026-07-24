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
    private readonly Func<int, bool> _tryConsumeFood;
    private readonly Func<Building, Recipe, ResourceType?> _tryConsumeInputs;
    private readonly Func<Building?, ResourceType?, WorldEvent?> _findCauseEvent;
    private readonly Action<BuildingId> _buildingChanged;

    public BuildingProductionSimulation(
        IReadOnlyDictionary<CitizenId, Citizen> citizens,
        WorldEventLog log,
        Func<int> currentTick,
        Func<int, bool> tryConsumeFood,
        Func<Building, Recipe, ResourceType?> tryConsumeInputs,
        Func<Building?, ResourceType?, WorldEvent?> findCauseEvent,
        Action<BuildingId> buildingChanged)
    {
        _citizens = citizens;
        _log = log;
        _currentTick = currentTick;
        _tryConsumeFood = tryConsumeFood;
        _tryConsumeInputs = tryConsumeInputs;
        _findCauseEvent = findCauseEvent;
        _buildingChanged = buildingChanged;
    }

    public int SimulateTick(Building building)
    {
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

    public void ApplyFoodAndRegen(Building building)
    {
        foreach (var citizenId in building.AssignedCitizenIds)
        {
            if (!_citizens.TryGetValue(citizenId, out var citizen)) continue;
            if (citizen.CurrentStamina < citizen.MaxStamina
                && _tryConsumeFood(StaminaRules.FoodConsumedPerRegen))
            {
                citizen.RestoreStamina(StaminaRules.RegenFromFood(
                    StaminaRules.FoodConsumedPerRegen,
                    citizen));
                citizen.RefreshWellFedBuff();
            }
            citizen.RestoreStamina(citizen.RegenPerTick());
        }
    }

    public static ProductionStopCause ResolveStopCauseWhenNotProducing(Building building)
    {
        if (!building.ProductionEnabled) return ProductionStopCause.Paused;
        if (building.AssignedCount == 0) return ProductionStopCause.NoWorkers;
        return ProductionStopCause.TargetReached;
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
}
