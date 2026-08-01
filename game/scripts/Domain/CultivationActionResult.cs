namespace WorldofGoses.Domain;

public enum CultivationActionOutcome
{
    Success = 0,
    SiteNotFound = 1,
    FounderUnavailable = 2,
    WrongState = 3,
    MissingFood = 4,
}

public readonly record struct CultivationActionResult(
    CultivationActionOutcome Outcome,
    int FoodDelta = 0)
{
    public bool IsSuccess => Outcome == CultivationActionOutcome.Success;

    public static CultivationActionResult Success(int foodDelta) =>
        new(CultivationActionOutcome.Success, foodDelta);

    public static CultivationActionResult Fail(CultivationActionOutcome outcome) =>
        new(outcome);
}
