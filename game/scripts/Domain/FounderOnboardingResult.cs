namespace WorldofGoses.Domain;

/// <summary>The complete and exclusive mechanical output of founder onboarding.</summary>
public sealed record FounderOnboardingResult(
    LineageId Lineage,
    ElementalAffinity ElementalAffinity,
    FounderCubeProfile CubeProfile,
    FounderNarrativeMemory NarrativeMemory);
