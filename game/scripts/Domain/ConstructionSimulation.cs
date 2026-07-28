#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Executes construction-project work and rest ticks. The world aggregate owns
/// resource storage, event history, authorisation, and project completion; this
/// collaborator owns the repeatable project simulation rules only.
/// </summary>
internal sealed class ConstructionSimulation
{
    private readonly IReadOnlyDictionary<CitizenId, Citizen> _citizens;
    private readonly WorldEventLog _log;
    private readonly Func<int> _currentTick;
    private readonly Func<IReadOnlyList<RecipeInput>, ResourceType?> _tryConsumeResources;
    private readonly Func<WorldEvent?> _findLatestCause;
    private readonly Action<BuildingId> _projectChanged;

    public ConstructionSimulation(
        IReadOnlyDictionary<CitizenId, Citizen> citizens,
        WorldEventLog log,
        Func<int> currentTick,
        Func<IReadOnlyList<RecipeInput>, ResourceType?> tryConsumeResources,
        Func<WorldEvent?> findLatestCause,
        Action<BuildingId> projectChanged)
    {
        _citizens = citizens;
        _log = log;
        _currentTick = currentTick;
        _tryConsumeResources = tryConsumeResources;
        _findLatestCause = findLatestCause;
        _projectChanged = projectChanged;
    }

    public void SimulateTick(ConstructionProject project, bool isWorkInterval)
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

        var presentWorkers = new List<Citizen>();
        foreach (CitizenId citizenId in project.AssignedCitizenIds)
        {
            if (_citizens.TryGetValue(citizenId, out Citizen? citizen)
                && citizen.CurrentLocation == CitizenLocation.AtWork)
            {
                presentWorkers.Add(citizen);
            }
        }
        if (presentWorkers.Count == 0)
        {
            project.LastTickProgressAdded = 0;
            project.StopCause = ConstructionStopCause.WorkersInTransit;
            return;
        }

        if (isWorkInterval && project.RemainingInputs.Count > 0
            && !TryDrawInputs(project))
        {
            return;
        }

        int contributed = 0;
        int paid = 0;
        foreach (Citizen citizen in presentWorkers)
        {
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

        if ((paid == 0 && isWorkInterval) || contributed == 0)
        {
            project.StopCause = ConstructionStopCause.WorkersExhausted;
            return;
        }
        if (isWorkInterval)
        {
            int added = Math.Min(contributed, project.RequiredWork - project.Progress);
            project.Progress += added;
            project.LastTickProgressAdded = added;
        }
        project.StopCause = ConstructionStopCause.Authorized;
    }

    public void ApplyNightRest(ConstructionProject project)
    {
        foreach (var citizenId in project.AssignedCitizenIds)
        {
            if (_citizens.TryGetValue(citizenId, out var citizen))
            {
                citizen.RestoreStamina(citizen.RegenPerTick());
            }
        }
        project.StopCause = ConstructionStopCause.Night;
        _projectChanged(project.Id);
    }

    private bool TryDrawInputs(ConstructionProject project)
    {
        var draw = new List<RecipeInput>();
        var nextRemaining = new List<RecipeInput>();
        foreach (var input in project.RemainingInputs)
        {
            draw.Add(new RecipeInput(input.Resource, 1));
            if (input.Amount > 1)
            {
                nextRemaining.Add(new RecipeInput(input.Resource, input.Amount - 1));
            }
        }
        if (_tryConsumeResources(draw) is not null)
        {
            project.StopCause = ConstructionStopCause.MissingMaterials;
            _log.Record(
                _currentTick(),
                WorldEventKind.ProductionBlocked,
                WorldEventSubject.ConstructionProject(project.Id, project.DisplayName),
                causeEventId: _findLatestCause()?.Id);
            _projectChanged(project.Id);
            return false;
        }
        project.SetRemainingInputs(nextRemaining);
        return true;
    }
}
