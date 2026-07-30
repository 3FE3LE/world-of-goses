namespace WorldofGoses.Domain;

public readonly record struct HeroIncorporationResult(
    HeroIncorporationOutcome Outcome,
    CitizenId? CitizenId)
{
    public bool IsSuccess => Outcome == HeroIncorporationOutcome.Success;

    public static HeroIncorporationResult Success(CitizenId citizenId) =>
        new(HeroIncorporationOutcome.Success, citizenId);

    public static HeroIncorporationResult Fail(HeroIncorporationOutcome outcome) =>
        new(outcome, null);
}
