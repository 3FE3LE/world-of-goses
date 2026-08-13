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
    /// <summary>
    /// The consequence this citizen expresses when a technique permits it,
    /// derived canonically from the elemental affinity. Onboarding produces it,
    /// so it belongs beside the affinity everywhere the affinity is shown.
    /// </summary>
    string PhysicalExpression,
    /// <summary>
    /// The two weapon families the physical expression makes natural. These are
    /// learning affinities, not equipment: a natural family accrues competency at
    /// full rate and a foreign one at a tenth. They stay on the profile because
    /// they are still how the player reads the founder's aptitude, even after
    /// <see cref="EquippedWeaponName"/> lands.
    /// </summary>
    IReadOnlyList<string> NaturalWeaponFamilies,
    /// <summary>
    /// The display name of the weapon the founder currently has equipped, or
    /// <c>null</c> when the founder is unarmed. Issue #26 landed the founder's
    /// starter weapon as a real <see cref="WeaponItemInstance"/>; this field
    /// carries the family of that instance to the view layer. A legacy save
    /// migrated from v34 with no item — see
    /// <see cref="MigrateV34ToV35Tests"/> — stays unarmed and surfaces here
    /// as <c>null</c>; the view renders an "unarmed" placeholder rather than
    /// hiding the line.
    /// </summary>
    string? EquippedWeaponName,
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
        // Qualified: the record's own NaturalWeaponFamilies property shadows the
        // domain helper of the same name inside this method.
        (WeaponFamily first, WeaponFamily second) =
            Domain.NaturalWeaponFamilies.For(hero.CombatNature.PhysicalExpression);
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
            ProfileCatalog.DisplayName(hero.CombatNature.PhysicalExpression),
            new[]
            {
                ProfileCatalog.DisplayName(first),
                ProfileCatalog.DisplayName(second),
            },
            // #28: the equipped weapon is read from the personal-equipment
            // registry so the projection and the registry cannot disagree.
            // The legacy v34 unarmed path returns null here, which the view
            // renders as a placeholder rather than an empty row.
            hero.PersonalEquipment?.EquippedWeapon is { } equippedWeapon
                ? ProfileCatalog.DisplayName(equippedWeapon.Family)
                : null,
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
