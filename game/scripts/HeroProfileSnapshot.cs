#nullable enable
#pragma warning disable CS0618 // Snapshot retains legacy fields for existing consumers through v29.
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;

namespace WorldofGoses;

public sealed record HeroProfileSnapshot(
    CitizenId Id,
    string Name,
    string LineageName,
    string LineageSummary,
    string LearningApproach,
    LineageId Lineage,
    GenderId Gender,
    AppearanceVariantId Appearance,
    IReadOnlyList<string> Aptitudes,
    IReadOnlyList<string> ProfessionalAffinities,
    IReadOnlyList<string> MarkedAffinities,
    string ElementalAffinity,
    FounderCubeProfile CubeProfile,
    string LineageSignature,
    FounderNarrativeMemory NarrativeMemory,
    string CombatStyle,
    IReadOnlyList<string> WeaponPreferences,
    IReadOnlyList<string> PersonalityTraits,
    string PoliticalOrientation,
    string SpiritualPosture,
    int CurrentStamina,
    int MaxStamina,
    int EffectiveMaxStamina,
    WoundSeverity? WoundSeverity,
    int WoundRecoveryTicksRemaining,
    bool IsReceivingWoundTreatment,
    bool IsAtHome)
{
    public static HeroProfileSnapshot? From(CityWorld world)
    {
        Citizen? hero = world.Hero;
        if (hero is null) return null;

        CitizenProfile profile = hero.Profile;
        LineageDefinition lineage = ProfileCatalog.Get(profile.Lineage);
        // Raw English catalog text on purpose — this record is exercised
        // directly by Godot-free xUnit tests (UiSnapshotTests), so it must
        // not depend on WorldofGoses.Ui.UiText (which calls into Godot's
        // TranslationServer and crashes outside the engine). Translation
        // happens at display time in HeroProfileView, the same layer that
        // owns every other UiText.Get/Format call in this project.
        return new HeroProfileSnapshot(
            hero.Id,
            hero.Name,
            lineage.DisplayName,
            lineage.Summary,
            lineage.LearningApproach,
            profile.Lineage,
            profile.Gender,
            hero.AppearanceVariant,
            profile.Aptitudes.Select(ProfileCatalog.DisplayName).ToArray(),
            profile.ProfessionalAffinities.Select(ProfileCatalog.DisplayName).ToArray(),
            lineage.MarkedAffinities.Select(ProfileCatalog.DisplayName).ToArray(),
            ProfileCatalog.DisplayName(profile.CanonicalElementalAffinity),
            profile.CubeProfile,
            CubeScoring.Signature(profile.Lineage),
            profile.FounderOnboardingResult?.NarrativeMemory ?? FounderNarrativeMemory.Empty,
            string.IsNullOrWhiteSpace(profile.CombatStyle.Value)
                ? string.Empty
                : ProfileCatalog.DisplayName(profile.CombatStyle),
            profile.WeaponPreferences.Select(ProfileCatalog.DisplayName).ToArray(),
            profile.PersonalityTraits.Select(ProfileCatalog.DisplayName).ToArray(),
            string.IsNullOrWhiteSpace(profile.PoliticalOrientation.Value)
                ? string.Empty
                : ProfileCatalog.DisplayName(profile.PoliticalOrientation),
            string.IsNullOrWhiteSpace(profile.SpiritualPosture.Value)
                ? string.Empty
                : ProfileCatalog.DisplayName(profile.SpiritualPosture),
            hero.CurrentStamina,
            hero.MaxStamina,
            hero.EffectiveMaxStamina,
            hero.Wound?.Severity,
            hero.Wound?.RecoveryTicksRemaining ?? 0,
            hero.Commitment.Kind == CitizenCommitmentKind.Recovery,
            hero.CurrentLocation == CitizenLocation.AtHome);
    }
}
