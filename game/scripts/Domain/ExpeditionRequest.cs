using System.Collections.Generic;

namespace WorldofGoses.Domain;

public enum ExpeditionRewardKind
{
    Supplies = 0,
    Migrant = 1,
    Discovery = 2,
}

public readonly record struct ExpeditionRequest(
    IReadOnlyList<CitizenId> MemberIds,
    int DurationTicks,
    ExpeditionSupplyRequirement SupplyRequirement,
    ExpeditionReward Reward,
    string DisplayName,
    ExpeditionRetreatPosture RetreatPosture = ExpeditionRetreatPosture.ContinueAfterSetback,
    ResourceOpportunityId? ResourceOpportunityId = null,
    ResourceOpportunityKind? ResourceOpportunityKind = null,
    int SetbackReturn = 0,
    int PartialReturn = 0)
{
    /// <summary>
    /// docs/systems/expeditions.md: an expedition carries real citizen ids, today 1-2.
    /// One authoritative team-size ceiling so validation, UI, and tests
    /// never restate the number separately.
    /// </summary>
    public const int MaxTeamSize = 2;

    /// <summary>
    /// Duration of the two expedition templates used by the first playable
    /// loop. Four in-game hours remain long enough to expose every persisted
    /// phase and a mid-expedition relaunch, while keeping a normal-UI run
    /// practical: ten real minutes at 1x or two and a half at 4x.
    /// Later expeditions may define longer durations from route distance.
    /// </summary>
    public const int FirstLoopDurationTicks = ExpeditionTiming.SpiritTrailDurationTicks;

    public ResourceType? SupplyResource => SupplyRequirement.Resource;
    public int SupplyAmount => SupplyRequirement.Amount;
    public ResourceType? RewardResource => Reward.Resource;
    public int RewardAmount => Reward.Amount;
    public ExpeditionRewardKind RewardKind => Reward.Kind;

    public static ExpeditionRequest Reconnaissance(
        CitizenId soleMemberId,
        ExpeditionRetreatPosture retreatPosture = ExpeditionRetreatPosture.ContinueAfterSetback) =>
        Reconnaissance(new[] { soleMemberId }, retreatPosture);

    public static ExpeditionRequest Reconnaissance(
        IReadOnlyList<CitizenId> memberIds,
        ExpeditionRetreatPosture retreatPosture = ExpeditionRetreatPosture.ContinueAfterSetback) =>
        new(
            memberIds,
            DurationTicks: FirstLoopDurationTicks,
            SupplyRequirement: ExpeditionSupplyRequirement.Required(ResourceType.Wood, 1),
            Reward: ExpeditionReward.Supplies(ResourceType.Stone, 1),
            DisplayName: "Reconnaissance",
            RetreatPosture: retreatPosture);

    public static ExpeditionRequest SeekProspect(
        CitizenId soleMemberId,
        ExpeditionRetreatPosture retreatPosture = ExpeditionRetreatPosture.ContinueAfterSetback) =>
        SeekProspect(new[] { soleMemberId }, retreatPosture);

    public static ExpeditionRequest SeekProspect(
        IReadOnlyList<CitizenId> memberIds,
        ExpeditionRetreatPosture retreatPosture = ExpeditionRetreatPosture.ContinueAfterSetback) =>
        new(
            memberIds,
            DurationTicks: FirstLoopDurationTicks,
            SupplyRequirement: ExpeditionSupplyRequirement.Required(ResourceType.Food, 2),
            Reward: ExpeditionReward.Migrant,
            DisplayName: "Community contact",
            RetreatPosture: retreatPosture);

    public static ExpeditionRequest ResourceSortie(
        ResourceOpportunity opportunity,
        IReadOnlyList<CitizenId> memberIds,
        ExpeditionRetreatPosture retreatPosture = ExpeditionRetreatPosture.ContinueAfterSetback)
    {
        ResourceExpeditionDefinition definition =
            ResourceExpeditionRules.Definition(opportunity.Kind);
        return new ExpeditionRequest(
            memberIds,
            definition.DurationTicks,
            definition.SupplyRequirement,
            definition.Reward,
            definition.DisplayName,
            retreatPosture,
            opportunity.Id,
            opportunity.Kind,
            definition.SetbackReturn,
            definition.PartialReturn);
    }
}

public enum ExpeditionStartOutcome
{
    Success = 0,
    NoHero = 1,
    MemberNotFound = 2,
    MemberUnavailable = 3,
    InvalidRequest = 4,
    MissingSupplies = 5,
    AlreadyActive = 6,
    TownHallUnavailable = 7,
    DuplicateMember = 8,
    MemberNotHero = 9,
    ResourceSortiesUnavailable = 10,
    OpportunityNotFound = 11,
    OpportunityUnavailable = 12,
    InsufficientReturnCapacity = 13,
}

public readonly record struct ExpeditionStartResult(
    ExpeditionStartOutcome Outcome,
    ExpeditionId? ExpeditionId,
    CitizenAvailabilityReason? UnavailableReason = null)
{
    public bool IsSuccess => Outcome == ExpeditionStartOutcome.Success;

    public static ExpeditionStartResult Success(ExpeditionId id) =>
        new(ExpeditionStartOutcome.Success, id);

    public static ExpeditionStartResult Fail(
        ExpeditionStartOutcome outcome,
        CitizenAvailabilityReason? unavailableReason = null) =>
        new(outcome, null, unavailableReason);
}
