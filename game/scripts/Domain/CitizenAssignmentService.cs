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
    private readonly CityWorld _cityWorld;

    public CitizenAssignmentService(
        IDictionary<CitizenId, Citizen> citizens,
        IDictionary<BuildingId, Building> buildings,
        IDictionary<BuildingId, ConstructionProject> projects,
        Action<BuildingId> buildingChanged,
        Action<BuildingId> projectChanged,
        CityWorld cityWorld)
    {
        _citizens = citizens;
        _buildings = buildings;
        _projects = projects;
        _buildingChanged = buildingChanged;
        _projectChanged = projectChanged;
        _cityWorld = cityWorld;
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
        if (citizen.CurrentAssignment.HasValue)
        {
            return AssignmentResult.Fail(AssignmentOutcome.CitizenUnavailable, citizenId, buildingId);
        }
        if (_cityWorld.IsCitizenOnActiveExpedition(citizenId))
        {
            return AssignmentResult.Fail(AssignmentOutcome.CitizenUnavailable, citizenId, buildingId);
        }

        var result = building.TryAssign(citizenId);
        if (!result.IsSuccess) return result;

        AttachCitizen(citizen, buildingId, currentTick);
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

        DetachCitizen(citizen);
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
        if (citizen.CurrentAssignment.HasValue)
        {
            return AssignmentResult.Fail(AssignmentOutcome.CitizenUnavailable, citizenId, projectId);
        }
        if (_cityWorld.IsCitizenOnActiveExpedition(citizenId))
        {
            return AssignmentResult.Fail(AssignmentOutcome.CitizenUnavailable, citizenId, projectId);
        }
        if (!project.TryAssign(citizenId))
        {
            return AssignmentResult.Fail(AssignmentOutcome.AtCapacity, citizenId, projectId);
        }

        AttachCitizen(citizen, projectId, currentTick);
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

        DetachCitizen(citizen);
        _projectChanged(projectId);
        return AssignmentResult.Ok(citizenId, projectId);
    }

    public void ReleaseBuilding(Building building)
    {
        var assignedIds = new List<CitizenId>(building.AssignedCitizenIds);
        foreach (var citizenId in assignedIds)
        {
            UnassignFromBuilding(building.Id, citizenId);
        }
    }

    private static void AttachCitizen(Citizen citizen, BuildingId assignmentId, int currentTick)
    {
        citizen.AssignTo(assignmentId);
        citizen.SetLocation(GameClock.IsDaytime(currentTick)
            ? CitizenLocation.AtWork
            : CitizenLocation.AtHome);
    }

    private static void DetachCitizen(Citizen citizen)
    {
        citizen.ClearAssignment();
        citizen.SetLocation(CitizenLocation.AtHome);
    }
}
