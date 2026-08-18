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
        int combatRulesVersion = ExpeditionCombatSessionFactory.CurrentRulesVersion,
        int? estimatedEndTick = null,
        IReadOnlyList<ExpeditionTimeEvent>? timeEvents = null)
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
        EstimatedEndTick = estimatedEndTick ?? endTick;
        if (timeEvents is { Count: > 0 }) _timeEvents.AddRange(timeEvents);
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

    private readonly List<ExpeditionTimeEvent> _timeEvents = new();

    public ExpeditionId Id { get; }
    public string DisplayName { get; }
    public IReadOnlyList<CitizenId> MemberIds { get; }

    /// <summary>The first member, kept for presentation that only needs one name (e.g. a compact list row).</summary>
    public CitizenId LeadCitizenId => MemberIds[0];
    public int StartTick { get; }

    /// <summary>
    /// When the expedition is now expected back. It moves: the estimate is a
    /// projection of pure travel, and what happens on the road is added to it.
    /// </summary>
    /// <remarks>
    /// It used to be fixed at dispatch, which made an expedition a timer with a
    /// known end rather than a journey. A fight that dragged could not cost
    /// anything, and an empty road could not save anything — the only thing an
    /// encounter could change was its own outcome.
    /// </remarks>
    public int EndTick { get; private set; }

    /// <summary>
    /// What the journey was projected to take at dispatch, from its distance
    /// and the party's pace alone. Never moves, so the difference against
    /// <see cref="EndTick"/> is exactly what the road cost.
    /// </summary>
    public int EstimatedEndTick { get; }

    /// <summary>Ticks the road added (positive) or saved (negative).</summary>
    public int EstimateDeltaTicks => EndTick - EstimatedEndTick;

    /// <summary>Everything that moved the return, in the order it happened.</summary>
    public IReadOnlyList<ExpeditionTimeEvent> TimeEvents => _timeEvents;

    /// <summary>
    /// Records something that cost or saved time, and moves the return with it.
    /// </summary>
    /// <remarks>
    /// The return can be pushed but never pulled before the tick the event was
    /// recorded on: an expedition cannot arrive in its own past, however
    /// generous the road was.
    /// </remarks>
    public void RecordTimeEvent(ExpeditionTimeEventKind kind, int ticks, int atTick)
    {
        if (ticks == 0) return;
        _timeEvents.Add(new ExpeditionTimeEvent(kind, ticks, atTick));
        EndTick = Math.Max(atTick, EndTick + ticks);
    }
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
    internal bool BeginEncounter(int atTick)
    {
        if (Phase != ExpeditionPhase.Outbound) return false;
        Phase = ExpeditionPhase.Encounter;
        EncounterStartedAtTick = atTick;
        return true;
    }

    /// <summary>The tick the party stopped travelling to fight.</summary>
    public int? EncounterStartedAtTick { get; private set; }

    /// <summary>
    /// Restores an in-flight encounter's start after a load, so a fight that
    /// spans a save still charges the road for its whole length instead of only
    /// the part that happened after reopening the game.
    /// </summary>
    internal void RestoreEncounterStart(int? atTick) => EncounterStartedAtTick = atTick;

    /// <summary>
    /// Closes the encounter and charges the road for it: whatever the fight
    /// took is time the party did not spend walking, so the return moves by
    /// exactly that much. A fight that dragged costs more than a short one,
    /// with nothing rolled and nothing assumed.
    /// </summary>
    internal bool CompleteEncounter(ExpeditionEncounterOutcome outcome, int atTick)
    {
        if (Phase != ExpeditionPhase.Encounter || EncounterOutcome.HasValue) return false;
        EncounterOutcome = outcome;
        if (EncounterStartedAtTick is int startedAt)
        {
            RecordTimeEvent(ExpeditionTimeEventKind.Encounter, atTick - startedAt, atTick);
        }
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
