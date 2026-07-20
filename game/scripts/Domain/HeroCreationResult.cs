namespace WorldofGoses.Domain;

/// <summary>Result of a hero-creation command.</summary>
public readonly record struct HeroCreationResult(
    HeroCreationOutcome Outcome,
    CitizenId? CitizenId = null)
{
    public bool IsSuccess => Outcome == HeroCreationOutcome.Success;

    public static HeroCreationResult Success(CitizenId citizenId) =>
        new(HeroCreationOutcome.Success, citizenId);

    public static HeroCreationResult Fail(HeroCreationOutcome outcome) => new(outcome);
}
