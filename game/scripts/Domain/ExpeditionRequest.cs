namespace WorldofGoses.Domain;

public enum ExpeditionRewardKind
{
    Supplies = 0,
    Migrant = 1,
}

public readonly record struct ExpeditionRequest(
    CitizenId LeadCitizenId,
    int DurationTicks,
    ResourceType SupplyResource,
    int SupplyAmount,
    ResourceType RewardResource,
    int RewardAmount,
    ExpeditionRewardKind RewardKind,
    string DisplayName)
{
    public static ExpeditionRequest Reconnaissance(CitizenId leadCitizenId) =>
        new(
            leadCitizenId,
            DurationTicks: 4 * GameClock.TicksPerInGameDay,
            SupplyResource: ResourceType.Wood,
            SupplyAmount: 1,
            RewardResource: ResourceType.Stone,
            RewardAmount: 1,
            RewardKind: ExpeditionRewardKind.Supplies,
            DisplayName: "Reconnaissance");

    public static ExpeditionRequest SeekProspect(CitizenId leadCitizenId) =>
        new(
            leadCitizenId,
            DurationTicks: 4 * GameClock.TicksPerInGameDay,
            SupplyResource: ResourceType.Food,
            SupplyAmount: 2,
            RewardResource: ResourceType.Food,
            RewardAmount: 0,
            RewardKind: ExpeditionRewardKind.Migrant,
            DisplayName: "Community contact");
}

public enum ExpeditionStartOutcome
{
    Success = 0,
    NoHero = 1,
    LeadCitizenNotFound = 2,
    LeadUnavailable = 3,
    InvalidRequest = 4,
    MissingSupplies = 5,
    AlreadyActive = 6,
    TownHallUnavailable = 7,
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
