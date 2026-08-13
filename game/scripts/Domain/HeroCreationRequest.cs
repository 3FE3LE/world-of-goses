#nullable enable
namespace WorldofGoses.Domain;

/// <summary>Input required to establish the principal hero.</summary>
public sealed record HeroCreationRequest(
    string Name,
    CitizenProfile Profile,
    GenderId Gender,
    FounderOnboardingResult? OnboardingResult = null,
    /// <summary>One of the two weapon families returned by
    /// <see cref="NaturalWeaponFamilies.For"/> for the founder's
    /// <see cref="FounderCubeProfile.PhysicalExpression"/>. The
    /// application re-validates this against the cube before
    /// creating the founder.</summary>
    WeaponFamily? MaterializedWeaponFamily = null);
