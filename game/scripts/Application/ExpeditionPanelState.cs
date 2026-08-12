#nullable enable
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Bundled read-only projection of the aggregate facts the expedition
/// planning panel reads. Replaces the previous direct
/// <c>_controller.World.X</c> accesses in <c>ExpeditionPanel</c>.
/// </summary>
public sealed record ExpeditionPanelState(
    int CurrentTick,
    bool HasTownHall,
    bool HasPendingProspect,
    ExpeditionPanelState.ActiveExpeditionItem? ActiveExpedition,
    IReadOnlyList<ExpeditionPanelState.TerritoryParcelItem> TerritoryTargets,
    IReadOnlyList<ExpeditionPanelState.ExpeditionMemberItem> Members,
    IReadOnlyDictionary<BuildingId, string> BuildingDisplayNames,
    IReadOnlyDictionary<BuildingId, string> ProjectDisplayNames)
{
    public sealed record ActiveExpeditionItem(
        ExpeditionId Id,
        string DisplayName,
        ExpeditionPhase Phase,
        ExpeditionRetreatPosture RetreatPosture,
        ExpeditionEncounterOutcome? EncounterOutcome,
        int StartTick,
        int EndTick,
        IReadOnlyList<CitizenId> MemberIds,
        IReadOnlyList<string> MemberNames);

    public sealed record TerritoryParcelItem(
        ParcelId ParcelId,
        ParcelTerritoryState TerritoryState);

    public sealed record ExpeditionMemberItem(
        CitizenId Id,
        string Name,
        bool IsHero,
        bool CanJoinExpedition,
        bool IsOnActiveExpedition,
        bool IsWounded,
        WoundSeverity? WoundSeverity,
        int WoundRecoveryTicksRemaining,
        bool IsInRecoveryCommitment,
        CitizenCommitmentKind CommitmentKind,
        CitizenAvailabilityReason AvailabilityReason,
        BuildingId? CommitmentTargetId);

    public static ExpeditionPanelState From(CityWorld world)
    {
        bool hasTownHall = false;
        foreach (var building in world.Buildings.Values)
        {
            if (building.Kind == BuildingKind.TownHall)
            {
                hasTownHall = true;
                break;
            }
        }

        ActiveExpeditionItem? active = null;
        foreach (Expedition expedition in world.Expeditions.Values)
        {
            if (expedition.Status == ExpeditionStatus.Active)
            {
                var memberNames = new List<string>(expedition.MemberIds.Count);
                foreach (CitizenId memberId in expedition.MemberIds)
                {
                    Citizen? member = world.GetCitizen(memberId);
                    memberNames.Add(member?.Name ?? "?");
                }
                active = new ActiveExpeditionItem(
                    expedition.Id,
                    expedition.DisplayName,
                    expedition.Phase,
                    expedition.RetreatPosture,
                    expedition.EncounterOutcome,
                    expedition.StartTick,
                    expedition.EndTick,
                    expedition.MemberIds.ToArray(),
                    memberNames);
                break;
            }
        }

        var territory = new List<TerritoryParcelItem>();
        CityParcel? target = world.NextTerritoryTarget;
        if (target is not null)
        {
            territory.Add(new TerritoryParcelItem(target.Id, target.TerritoryState));
        }

        var members = new List<ExpeditionMemberItem>();
        foreach (Citizen citizen in world.Citizens.Values)
        {
            BuildingId? commitmentTargetId = citizen.Commitment.EntityId is int id
                ? new BuildingId(id)
                : null;
            members.Add(new ExpeditionMemberItem(
                citizen.Id,
                citizen.Name,
                citizen.IsHero,
                citizen.CanJoinExpedition,
                world.IsCitizenOnActiveExpedition(citizen.Id),
                citizen.IsWounded,
                citizen.Wound?.Severity,
                citizen.Wound?.RecoveryTicksRemaining ?? 0,
                citizen.Commitment.Kind == CitizenCommitmentKind.Recovery,
                citizen.Commitment.Kind,
                citizen.AvailabilityReason,
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

        return new ExpeditionPanelState(
            world.CurrentTick,
            hasTownHall,
            world.PendingProspect is not null,
            active,
            territory,
            members,
            buildingDisplayNames,
            projectDisplayNames);
    }
}