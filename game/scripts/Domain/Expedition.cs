#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldofGoses.Domain;

public sealed class Expedition
{
    public Expedition(
        ExpeditionId id,
        string displayName,
        IReadOnlyList<CitizenId> memberIds,
        int startTick,
        int endTick,
        ResourceType supplyResource,
        int supplyAmount,
        ResourceType rewardResource,
        int rewardAmount,
        ExpeditionRewardKind rewardKind,
        ResourceReservationId reservationId,
        ExpeditionStatus status = ExpeditionStatus.Active,
        ExpeditionPhase phase = ExpeditionPhase.Outbound,
        ExpeditionEncounterOutcome? encounterOutcome = null,
        ExpeditionRetreatPosture retreatPosture = ExpeditionRetreatPosture.ContinueAfterSetback,
        WorldEventId? dispatchEventId = null,
        int? returnedAmount = null,
        CitizenId? deliveredMigrantId = null,
        ParcelId? targetParcelId = null)
    {
        if (id.Value <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
        if (memberIds is null || memberIds.Count == 0 || memberIds.Count > ExpeditionRequest.MaxTeamSize)
        {
            throw new ArgumentOutOfRangeException(nameof(memberIds), "An expedition needs 1-2 members.");
        }
        if (memberIds.Any(member => member.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(memberIds), "Every member id must be positive.");
        }
        if (memberIds.Distinct().Count() != memberIds.Count)
        {
            throw new ArgumentException("An expedition cannot list the same citizen twice.", nameof(memberIds));
        }
        if (startTick < 0 || endTick < startTick) throw new ArgumentOutOfRangeException(nameof(endTick));
        if (supplyAmount <= 0) throw new ArgumentOutOfRangeException(nameof(supplyAmount));
        if (rewardKind == ExpeditionRewardKind.Supplies && rewardAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rewardAmount));
        }
        if (targetParcelId is ParcelId target && target.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetParcelId));
        }

        Id = id;
        DisplayName = displayName;
        MemberIds = memberIds.ToArray();
        StartTick = startTick;
        EndTick = endTick;
        SupplyResource = supplyResource;
        SupplyAmount = supplyAmount;
        RewardResource = rewardResource;
        RewardAmount = rewardAmount;
        RewardKind = rewardKind;
        ReservationId = reservationId;
        Status = status;
        Phase = phase;
        EncounterOutcome = encounterOutcome;
        RetreatPosture = retreatPosture;
        DispatchEventId = dispatchEventId;
        ReturnedAmount = returnedAmount;
        DeliveredMigrantId = deliveredMigrantId;
        TargetParcelId = targetParcelId;
    }

    public ExpeditionId Id { get; }
    public string DisplayName { get; }
    public IReadOnlyList<CitizenId> MemberIds { get; }

    /// <summary>The first member, kept for presentation that only needs one name (e.g. a compact list row).</summary>
    public CitizenId LeadCitizenId => MemberIds[0];
    public int StartTick { get; }
    public int EndTick { get; }
    public ResourceType SupplyResource { get; }
    public int SupplyAmount { get; }
    public ResourceType RewardResource { get; }
    public int RewardAmount { get; }
    public ExpeditionRewardKind RewardKind { get; }
    public ResourceReservationId ReservationId { get; }
    public ExpeditionStatus Status { get; private set; }
    public int? ReturnedAmount { get; private set; }
    public WorldEventId? DispatchEventId { get; private set; }
    public CitizenId? DeliveredMigrantId { get; private set; }
    public ExpeditionPhase Phase { get; private set; }
    public ExpeditionEncounterOutcome? EncounterOutcome { get; private set; }
    public ExpeditionRetreatPosture RetreatPosture { get; }
    public ParcelId? TargetParcelId { get; }
    public bool RetreatTriggered =>
        RetreatPosture == ExpeditionRetreatPosture.RetreatAfterSetback
        && EncounterOutcome == ExpeditionEncounterOutcome.Setback;

    public bool HasMember(CitizenId citizenId)
    {
        for (int i = 0; i < MemberIds.Count; i++)
        {
            if (MemberIds[i] == citizenId) return true;
        }
        return false;
    }

    public bool IsComplete(int currentTick) =>
        Status == ExpeditionStatus.Active && currentTick >= EndTick;

    internal void SetDispatchEventId(WorldEventId id) => DispatchEventId = id;

    /// <summary>
    /// One-way, one-time transition out of <see cref="ExpeditionPhase.Outbound"/>.
    /// Stores the outcome exactly once — the result of the encounter, not an
    /// animation — so re-evaluating it on a later tick (e.g. after save/load)
    /// can never re-roll it.
    /// </summary>
    internal bool ResolveEncounter(ExpeditionEncounterOutcome outcome)
    {
        if (Phase != ExpeditionPhase.Outbound) return false;
        Phase = ExpeditionPhase.Encounter;
        EncounterOutcome = outcome;
        return true;
    }

    internal bool TryAdvancePhase(ExpeditionPhase next)
    {
        bool legal = (Phase, next) switch
        {
            (ExpeditionPhase.Encounter, ExpeditionPhase.Objective) => true,
            (ExpeditionPhase.Encounter, ExpeditionPhase.Retreating) => true,
            (ExpeditionPhase.Retreating, ExpeditionPhase.Returning) => true,
            (ExpeditionPhase.Objective, ExpeditionPhase.Returning) => true,
            _ => false,
        };
        if (!legal) return false;
        Phase = next;
        return true;
    }

    internal bool BeginRetreat() => TryAdvancePhase(ExpeditionPhase.Retreating);

    internal void MarkReturnedSupplies(int amount)
    {
        ReturnedAmount = amount;
        Status = ExpeditionStatus.Returned;
        Phase = ExpeditionPhase.Resolved;
    }

    internal void MarkReturnedMigrant(CitizenId migrantId, int carried)
    {
        DeliveredMigrantId = migrantId;
        ReturnedAmount = carried;
        Status = ExpeditionStatus.Returned;
        Phase = ExpeditionPhase.Resolved;
    }

    internal void MarkReturnedProspect()
    {
        ReturnedAmount = 0;
        Status = ExpeditionStatus.Returned;
        Phase = ExpeditionPhase.Resolved;
    }

    internal void MarkFailed()
    {
        ReturnedAmount = 0;
        Status = ExpeditionStatus.Failed;
        Phase = ExpeditionPhase.Resolved;
    }

    internal void MarkRetreated()
    {
        ReturnedAmount = 0;
        Status = ExpeditionStatus.Retreated;
        Phase = ExpeditionPhase.Resolved;
    }

    internal void MarkCancelled()
    {
        ReturnedAmount = 0;
        Status = ExpeditionStatus.Cancelled;
        Phase = ExpeditionPhase.Resolved;
    }
}

public sealed class ExpeditionChangedEventArgs : EventArgs
{
    public ExpeditionChangedEventArgs(ExpeditionId expeditionId, ExpeditionStatus status)
    {
        ExpeditionId = expeditionId;
        Status = status;
    }

    public ExpeditionId ExpeditionId { get; }
    public ExpeditionStatus Status { get; }
}
