#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain.Combat;

namespace WorldofGoses.Domain;

public sealed class Expedition
{
    public Expedition(
        ExpeditionId id,
        string displayName,
        IReadOnlyList<CitizenId> memberIds,
        int startTick,
        int endTick,
        ExpeditionSupplyRequirement supplyRequirement,
        ExpeditionReward reward,
        ResourceReservationId? reservationId,
        ExpeditionStatus status = ExpeditionStatus.Active,
        ExpeditionPhase phase = ExpeditionPhase.Outbound,
        ExpeditionEncounterOutcome? encounterOutcome = null,
        ExpeditionRetreatPosture retreatPosture = ExpeditionRetreatPosture.ContinueAfterSetback,
        WorldEventId? dispatchEventId = null,
        int? returnedAmount = null,
        CitizenId? deliveredMigrantId = null,
        ParcelId? targetParcelId = null,
        ResourceOpportunityId? resourceOpportunityId = null,
        ResourceOpportunityKind? resourceOpportunityKind = null,
        int setbackReturn = 0,
        int partialReturn = 0,
        int carryCapacity = 0,
        int? objectiveReachedAtTick = null,
        int combatRulesVersion = ExpeditionCombatSessionFactory.CurrentRulesVersion)
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
        if (supplyRequirement.IsNone != !reservationId.HasValue)
            throw new ArgumentException("Only a material supply requirement owns a reservation.");
        if (objectiveReachedAtTick is int objectiveTick
            && (objectiveTick < startTick || objectiveTick > endTick))
            throw new ArgumentOutOfRangeException(nameof(objectiveReachedAtTick));
        if (combatRulesVersion < ExpeditionCombatSessionFactory.LegacyRulesVersion
            || combatRulesVersion > ExpeditionCombatSessionFactory.CurrentRulesVersion)
            throw new ArgumentOutOfRangeException(nameof(combatRulesVersion));
        if (targetParcelId is ParcelId target && target.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetParcelId));
        }
        if (resourceOpportunityId.HasValue
            && (!resourceOpportunityKind.HasValue
                || (reward.IsMaterial
                    && (setbackReturn <= 0
                        || partialReturn < setbackReturn
                        || reward.Amount < partialReturn
                        || carryCapacity < setbackReturn
                        || carryCapacity > reward.Amount))
                || (!reward.IsMaterial
                    && (setbackReturn != 0
                        || partialReturn != 0
                        || carryCapacity != 0))))
        {
            throw new ArgumentException("Resource expedition return values are invalid.");
        }
        if (!resourceOpportunityId.HasValue
            && (resourceOpportunityKind.HasValue
                || setbackReturn != 0
                || partialReturn != 0
                || carryCapacity != 0))
        {
            throw new ArgumentException("Legacy expeditions cannot carry resource-opportunity state.");
        }

        Id = id;
        DisplayName = displayName;
        MemberIds = memberIds.ToArray();
        StartTick = startTick;
        EndTick = endTick;
        SupplyRequirement = supplyRequirement;
        Reward = reward;
        ReservationId = reservationId;
        Status = status;
        Phase = phase;
        EncounterOutcome = encounterOutcome;
        RetreatPosture = retreatPosture;
        DispatchEventId = dispatchEventId;
        ReturnedAmount = returnedAmount;
        DeliveredMigrantId = deliveredMigrantId;
        TargetParcelId = targetParcelId;
        ResourceOpportunityId = resourceOpportunityId;
        ResourceOpportunityKind = resourceOpportunityKind;
        SetbackReturn = setbackReturn;
        PartialReturn = partialReturn;
        CarryCapacity = carryCapacity;
        ObjectiveReachedAtTick = objectiveReachedAtTick;
        CombatRulesVersion = combatRulesVersion;
    }

    public ExpeditionId Id { get; }
    public string DisplayName { get; }
    public IReadOnlyList<CitizenId> MemberIds { get; }

    /// <summary>The first member, kept for presentation that only needs one name (e.g. a compact list row).</summary>
    public CitizenId LeadCitizenId => MemberIds[0];
    public int StartTick { get; }
    public int EndTick { get; }
    public ExpeditionSupplyRequirement SupplyRequirement { get; }
    public ResourceType? SupplyResource => SupplyRequirement.Resource;
    public int SupplyAmount => SupplyRequirement.Amount;
    public ExpeditionReward Reward { get; }
    public ResourceType? RewardResource => Reward.Resource;
    public int RewardAmount => Reward.Amount;
    public ExpeditionRewardKind RewardKind => Reward.Kind;
    public ResourceReservationId? ReservationId { get; }
    public ExpeditionStatus Status { get; private set; }
    public int? ReturnedAmount { get; private set; }
    public WorldEventId? DispatchEventId { get; private set; }
    public CitizenId? DeliveredMigrantId { get; private set; }
    public ExpeditionPhase Phase { get; private set; }
    public ExpeditionEncounterOutcome? EncounterOutcome { get; private set; }
    public ExpeditionRetreatPosture RetreatPosture { get; }
    public ParcelId? TargetParcelId { get; }
    public ResourceOpportunityId? ResourceOpportunityId { get; }
    public ResourceOpportunityKind? ResourceOpportunityKind { get; }
    public int SetbackReturn { get; }
    public int PartialReturn { get; }
    public int CarryCapacity { get; }
    public int? ObjectiveReachedAtTick { get; private set; }
    public int CombatRulesVersion { get; }

    public int ReturnFor(ExpeditionEncounterOutcome outcome)
    {
        if (!Reward.IsMaterial) return 0;
        int planned = ResourceOpportunityId.HasValue
            ? outcome switch
            {
                ExpeditionEncounterOutcome.FullSuccess => RewardAmount,
                ExpeditionEncounterOutcome.PartialSuccess => PartialReturn,
                _ => SetbackReturn,
            }
            : outcome switch
            {
                ExpeditionEncounterOutcome.FullSuccess => RewardAmount,
                ExpeditionEncounterOutcome.PartialSuccess => Math.Max(1, RewardAmount / 2),
                _ => 0,
            };
        return ResourceOpportunityId.HasValue
            ? Math.Min(planned, CarryCapacity)
            : planned;
    }
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
    /// One-way, one-time transition into the encounter. The incremental combat
    /// session may then span several world ticks before storing its outcome.
    /// </summary>
    internal bool BeginEncounter()
    {
        if (Phase != ExpeditionPhase.Outbound) return false;
        Phase = ExpeditionPhase.Encounter;
        return true;
    }

    internal bool CompleteEncounter(ExpeditionEncounterOutcome outcome)
    {
        if (Phase != ExpeditionPhase.Encounter || EncounterOutcome.HasValue) return false;
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

    internal bool ReachObjectiveAndBeginReturn(int currentTick)
    {
        if (Phase != ExpeditionPhase.Objective || currentTick < StartTick) return false;
        ObjectiveReachedAtTick = currentTick;
        Phase = ExpeditionPhase.Returning;
        return true;
    }

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

    internal void MarkReturnedDiscovery()
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
