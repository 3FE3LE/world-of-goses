using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Owns the consistency rules for moving citizens into and out of buildings
/// and construction projects. <see cref="CityWorld"/> remains the public
/// aggregate facade; this collaborator mutates only the collections owned by
/// that aggregate and reports the affected subject through narrow callbacks.
/// </summary>
internal sealed class CitizenAssignmentService
{
    private readonly IDictionary<CitizenId, Citizen> _citizens;
    private readonly IDictionary<BuildingId, Building> _buildings;
    private readonly IDictionary<BuildingId, ConstructionProject> _projects;
    private readonly Action<BuildingId> _buildingChanged;
    private readonly Action<BuildingId> _projectChanged;

    public CitizenAssignmentService(
        IDictionary<CitizenId, Citizen> citizens,
        IDictionary<BuildingId, Building> buildings,
        IDictionary<BuildingId, ConstructionProject> projects,
        Action<BuildingId> buildingChanged,
        Action<BuildingId> projectChanged)
    {
        _citizens = citizens;
        _buildings = buildings;
        _projects = projects;
        _buildingChanged = buildingChanged;
        _projectChanged = projectChanged;
    }

    public AssignmentResult AssignToBuilding(
        BuildingId buildingId,
        CitizenId citizenId,
        int currentTick)
    {
        if (!_buildings.TryGetValue(buildingId, out var building))
        {
            return AssignmentResult.Fail(AssignmentOutcome.BuildingNotFound, citizenId, buildingId);
        }
        if (!_citizens.TryGetValue(citizenId, out var citizen))
        {
            return AssignmentResult.Fail(AssignmentOutcome.CitizenNotFound, citizenId, buildingId);
        }
        if (building.IsAssigned(citizenId))
        {
            return AssignmentResult.Fail(AssignmentOutcome.AlreadyAssigned, citizenId, buildingId);
        }
        if (!citizen.IsAvailable)
        {
            return AssignmentResult.Fail(
                AssignmentOutcome.CitizenUnavailable,
                citizenId,
                buildingId,
                citizen.AvailabilityReason);
        }

        var result = building.TryAssign(citizenId);
        if (!result.IsSuccess) return result;

        if (!citizen.TryCommitToBuilding(buildingId))
        {
            building.TryUnassign(citizenId);
            return AssignmentResult.Fail(
                AssignmentOutcome.CitizenUnavailable,
                citizenId,
                buildingId,
                citizen.AvailabilityReason);
        }
        MobiliseCitizen(citizen, building, currentTick);
        _buildingChanged(buildingId);
        return result;
    }

    public AssignmentResult UnassignFromBuilding(BuildingId buildingId, CitizenId citizenId)
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
        if (!result.IsSuccess) return result;

        citizen.ReleaseCommitment(CitizenCommitmentKind.BuildingWork, buildingId.Value);
        MobiliseHome(citizen);
        _buildingChanged(buildingId);
        return result;
    }

    public AssignmentResult AssignToProject(
        BuildingId projectId,
        CitizenId citizenId,
        int currentTick)
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
        if (!citizen.IsAvailable)
        {
            return AssignmentResult.Fail(
                AssignmentOutcome.CitizenUnavailable,
                citizenId,
                projectId,
                citizen.AvailabilityReason);
        }
        if (!project.TryAssign(citizenId))
        {
            return AssignmentResult.Fail(AssignmentOutcome.AtCapacity, citizenId, projectId);
        }

        if (!citizen.TryCommitToConstruction(projectId))
        {
            project.TryUnassign(citizenId);
            return AssignmentResult.Fail(
                AssignmentOutcome.CitizenUnavailable,
                citizenId,
                projectId,
                citizen.AvailabilityReason);
        }
        MobiliseCitizen(citizen, currentTick);
        _projectChanged(projectId);
        return AssignmentResult.Ok(citizenId, projectId);
    }

    public AssignmentResult UnassignFromProject(BuildingId projectId, CitizenId citizenId)
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

        citizen.ReleaseCommitment(CitizenCommitmentKind.Construction, projectId.Value);
        MobiliseHome(citizen);
        _projectChanged(projectId);
        return AssignmentResult.Ok(citizenId, projectId);
    }

    public void PauseArrivedWorkers(Building building, int currentTick)
    {
        var assignedIds = new List<CitizenId>(building.AssignedCitizenIds);
        foreach (var citizenId in assignedIds)
        {
            if (!_citizens.TryGetValue(citizenId, out var citizen)
                || citizen.CurrentLocation != CitizenLocation.AtWork)
            {
                continue;
            }
            citizen.BeginTravelHome(currentTick);
        }
    }

    private static void MobiliseCitizen(Citizen citizen, int currentTick)
    {
        if (GameClock.IsDaytime(currentTick)) citizen.BeginTravelToAssignment(currentTick);
        else citizen.SetLocation(CitizenLocation.AtHome);
    }

    private static void MobiliseCitizen(Citizen citizen, Building building, int currentTick)
    {
        bool workIsNeeded = building.ProductionEnabled && building.Stock < building.MaxStock;
        if (GameClock.IsDaytime(currentTick) && workIsNeeded)
        {
            citizen.BeginTravelToAssignment(currentTick);
        }
        else
        {
            // Preserve the player's standing order without sending someone to
            // a workplace that cannot currently accept productive work. The
            // world scheduler will re-evaluate it once stock drops below the
            // configured production ceiling.
            citizen.SetLocation(CitizenLocation.AtHome);
        }
    }

    private static void MobiliseHome(Citizen citizen)
    {
        citizen.SetLocation(CitizenLocation.AtHome);
    }
}
