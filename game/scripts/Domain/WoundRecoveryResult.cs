namespace WorldofGoses.Domain;

public enum WoundRecoveryOutcome
{
    Success = 0,
    CitizenNotFound = 1,
    NotWounded = 2,
    ShelterUnavailable = 3,
    AlreadyRecovering = 4,
    OnExpedition = 5,
    MissingFood = 6,
}

public readonly record struct WoundRecoveryResult(
    WoundRecoveryOutcome Outcome,
    CitizenId? CitizenId = null,
    int FoodConsumed = 0)
{
    public bool IsSuccess => Outcome == WoundRecoveryOutcome.Success;

    public static WoundRecoveryResult Success(CitizenId citizenId, int foodConsumed) =>
        new(WoundRecoveryOutcome.Success, citizenId, foodConsumed);

    public static WoundRecoveryResult Fail(WoundRecoveryOutcome outcome) => new(outcome);
}
