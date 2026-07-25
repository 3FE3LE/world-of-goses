#nullable enable
using System.Collections.Generic;

namespace WorldofGoses.Domain;

public enum FounderScoreAxis
{
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
    string RiskProfile,
    string LeadershipStyle,
    IReadOnlyDictionary<FounderScoreAxis, int> InternalAxes,
    IReadOnlyList<string> PrologueFragments);

public sealed record FounderNarrativeResult(
    CitizenProfile Profile,
    LineageId Lineage,
    IReadOnlyList<AptitudeId> Aptitudes,
    IReadOnlyList<ProfessionFamilyId> ProfessionalAffinities,
    ElementalAffinityId Element,
    CombatStyleId CombatStyle,
    IReadOnlyList<WeaponPreferenceId> WeaponPreferences,
    IReadOnlyList<PersonalityTraitId> Traits,
    SpiritualPostureId SpiritualPosture,
    PoliticalOrientationId PoliticalOrientation,
    FounderIdentityProfile Identity);
