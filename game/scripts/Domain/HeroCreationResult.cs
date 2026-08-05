#nullable enable
namespace WorldofGoses.Domain;

/// <summary>Result of a hero-creation command.</summary>
public readonly record struct HeroCreationResult(
    HeroCreationOutcome Outcome,
    CitizenId? CitizenId = null,
    FounderOnboardingResult? OnboardingResult = null)
{
    public bool IsSuccess => Outcome == HeroCreationOutcome.Success;

    public static HeroCreationResult Success(
        CitizenId citizenId,
        FounderOnboardingResult? onboardingResult = null) =>
        new(HeroCreationOutcome.Success, citizenId, onboardingResult);

    public static HeroCreationResult Fail(HeroCreationOutcome outcome) => new(outcome);
}
