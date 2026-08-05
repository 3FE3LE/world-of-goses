#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

public enum FounderScoreAxis
{
    Cube,
    Lineage,
    Aptitude,
    Profession,
    Element,
    CombatStyle,
    Weapon,
    Trait,
    RiskProfile,
    LeadershipStyle,
    SpiritualPosture,
    PoliticalOrientation,
    IdentityContinuity,
    TransformationAcceptance,
    Attachment,
    Autonomy,
    Openness,
    Control,
    Impulse,
    Contemplation,
    MortalityResponse,
}

public sealed record ScoreContribution(
    FounderScoreAxis Axis,
    string ValueId,
    int Weight);

public sealed record FounderNarrativeChoice(
    string Id,
    string Text,
    string ImmediateConsequence,
    IReadOnlyList<ScoreContribution> Contributions,
    string PrologueFragment = "");

public sealed record FounderNarrativeQuestion(
    string Id,
    string Title,
    string Text,
    IReadOnlyList<FounderNarrativeChoice> Choices,
    float TerrainReveal);

public sealed record FounderIdentityProfile(
    [property: Obsolete("DEC-0013: risk is learned during the citizen's life, not produced by onboarding.")]
    string RiskProfile,
    [property: Obsolete("DEC-0013: leadership style is learned during the citizen's life, not produced by onboarding.")]
    string LeadershipStyle,
    IReadOnlyDictionary<FounderScoreAxis, int> InternalAxes,
    IReadOnlyList<string> PrologueFragments);

[Obsolete("DEC-0013: use FounderOnboardingResult. Retained for one compatibility version only.")]
public sealed record FounderNarrativeResult(
    CitizenProfile Profile,
    LineageId Lineage,
    [property: Obsolete("DEC-0013: aptitudes are not produced by onboarding.")]
    IReadOnlyList<AptitudeId> Aptitudes,
    [property: Obsolete("DEC-0013: professional affinities are learned during the citizen's life.")]
    IReadOnlyList<ProfessionFamilyId> ProfessionalAffinities,
    ElementalAffinityId Element,
    [property: Obsolete("DEC-0013: combat style is learned during the citizen's life.")]
    CombatStyleId CombatStyle,
    [property: Obsolete("DEC-0013: weapon preferences are learned during the citizen's life.")]
    IReadOnlyList<WeaponPreferenceId> WeaponPreferences,
    [property: Obsolete("DEC-0013: traits are acquired during the citizen's life.")]
    IReadOnlyList<PersonalityTraitId> Traits,
    [property: Obsolete("DEC-0013: spiritual posture is not produced by onboarding.")]
    SpiritualPostureId SpiritualPosture,
    [property: Obsolete("DEC-0013: political orientation is not produced by onboarding.")]
    PoliticalOrientationId PoliticalOrientation,
    FounderIdentityProfile Identity);
