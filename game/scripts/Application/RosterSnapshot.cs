#nullable enable
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Read-only projection of the city roster. Replaces the previous
/// <c>_controller.World.Citizens.Values</c> iteration in
/// <c>MigrantPanel</c> with a snapshot that carries every field the
/// panel renders.
/// </summary>
public sealed record RosterSnapshot(
    int CitizenCount,
    int HousingCapacity,
    int AvailableHousing,
    CitizenId? HeroId,
    bool IsHousingFull,
    IReadOnlyList<RosterSnapshot.RosterEntry> Entries,
    IReadOnlyDictionary<BuildingId, string> BuildingDisplayNames,
    IReadOnlyDictionary<BuildingId, string> ProjectDisplayNames)
{
    public sealed record RosterEntry(
        CitizenId Id,
        string Name,
        bool IsHero,
        LineageId Lineage,
        GenderId Gender,
        AppearanceVariantId Appearance,
        FounderCubeProfile CubeProfile,
        CombatNature CombatNature,
        bool IsAvailable,
        bool IsOnActiveExpedition,
        bool IsWounded,
        WoundSeverity? WoundSeverity,
        CitizenCommitmentKind CommitmentKind,
        bool IsInRecoveryCommitment,
        CitizenAvailabilityReason AvailabilityReason,
        BuildingId? CurrentAssignment,
        CitizenLocation CurrentLocation,
        int CurrentStamina,
        int MaxStamina,
        BuildingId? CommitmentTargetId);

    public static RosterSnapshot From(CityWorld world)
    {
        var entries = new List<RosterEntry>();
        foreach (Citizen citizen in world.Citizens.Values)
        {
            BuildingId? commitmentTargetId = citizen.Commitment.EntityId is int id
                ? new BuildingId(id)
                : null;
            entries.Add(new RosterEntry(
                citizen.Id,
                citizen.Name,
                citizen.IsHero,
                citizen.Profile.Lineage,
                citizen.Profile.Gender,
                citizen.AppearanceVariant,
                citizen.Profile.CubeProfile,
                citizen.Profile.CombatNature,
                citizen.IsAvailable,
                world.IsCitizenOnActiveExpedition(citizen.Id),
                citizen.IsWounded,
                citizen.Wound?.Severity,
                citizen.Commitment.Kind,
                citizen.Commitment.Kind == CitizenCommitmentKind.Recovery,
                citizen.AvailabilityReason,
                citizen.CurrentAssignment,
                citizen.CurrentLocation,
                citizen.CurrentStamina,
                citizen.MaxStamina,
                commitmentTargetId));
        }

        var buildingDisplayNames = new Dictionary<BuildingId, string>();
        foreach (var building in world.Buildings.Values)
        {
            buildingDisplayNames[building.Id] = building.DisplayName;
        }
        var projectDisplayNames = new Dictionary<BuildingId, string>();
        foreach (var project in world.Projects.Values)
        {
            projectDisplayNames[project.Id] = project.DisplayName;
        }

        return new RosterSnapshot(
            world.Citizens.Count,
            world.HousingCapacity,
            world.AvailableHousing,
            world.Hero?.Id,
            world.AvailableHousing == 0,
            entries,
            buildingDisplayNames,
            projectDisplayNames);
    }
}