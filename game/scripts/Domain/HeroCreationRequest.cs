#nullable enable
namespace WorldofGoses.Domain;

/// <summary>Input required to establish the principal hero.</summary>
public sealed record HeroCreationRequest(
    string Name,
    CitizenProfile Profile,
    GenderId Gender,
    FounderOnboardingResult? OnboardingResult = null);
