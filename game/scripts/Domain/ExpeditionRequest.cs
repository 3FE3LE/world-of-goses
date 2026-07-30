using System.Collections.Generic;

namespace WorldofGoses.Domain;

public enum ExpeditionRewardKind
{
    Supplies = 0,
    Migrant = 1,
}

public readonly record struct ExpeditionRequest(
    IReadOnlyList<CitizenId> MemberIds,
    int DurationTicks,
    ResourceType SupplyResource,
    int SupplyAmount,
    ResourceType RewardResource,
    int RewardAmount,
    ExpeditionRewardKind RewardKind,
    string DisplayName,
    ExpeditionRetreatPosture RetreatPosture = ExpeditionRetreatPosture.ContinueAfterSetback)
{
    /// <summary>
    /// docs/FIRST_PLAYABLE_LOOP_AUDIT.md §G3: "select 1-2 real citizens".
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
    public const int FirstLoopDurationTicks = GameClock.TicksPerInGameDay / 6;

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
            SupplyResource: ResourceType.Wood,
            SupplyAmount: 1,
            RewardResource: ResourceType.Stone,
            RewardAmount: 1,
            RewardKind: ExpeditionRewardKind.Supplies,
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
            SupplyResource: ResourceType.Food,
            SupplyAmount: 2,
            RewardResource: ResourceType.Food,
            RewardAmount: 0,
            RewardKind: ExpeditionRewardKind.Migrant,
            DisplayName: "Community contact",
            RetreatPosture: retreatPosture);
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
