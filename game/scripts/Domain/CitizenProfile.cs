#nullable enable
#pragma warning disable CS0618 // This type owns the one-version DEC-0013 compatibility fields.
using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldofGoses.Domain;

/// <summary>
/// Immutable individual identity profile. Lineage and personal choices are
/// deliberately separate: professional affinities may coincide with or
/// contradict common lineage tendencies.
/// </summary>
public sealed class CitizenProfile
{
    private CitizenProfile(
        LineageId lineage,
        GenderId gender,
        AptitudeId[] aptitudes,
        ProfessionFamilyId[] professionalAffinities,
        ElementalAffinityId elementalAffinity,
        CombatStyleId combatStyle,
        WeaponPreferenceId[] weaponPreferences,
        PersonalityTraitId[] personalityTraits,
        PoliticalOrientationId politicalOrientation,
        SpiritualPostureId spiritualPosture,
        FounderCubeProfile cubeProfile,
        FounderOnboardingResult? founderOnboardingResult)
    {
        ArgumentNullException.ThrowIfNull(cubeProfile);
        Lineage = lineage;
        Gender = gender;
        Aptitudes = Array.AsReadOnly(aptitudes);
        ProfessionalAffinities = Array.AsReadOnly(professionalAffinities);
        ElementalAffinity = elementalAffinity;
        CombatStyle = combatStyle;
        WeaponPreferences = Array.AsReadOnly(weaponPreferences);
        PersonalityTraits = Array.AsReadOnly(personalityTraits);
        PoliticalOrientation = politicalOrientation;
        SpiritualPosture = spiritualPosture;
        CubeProfile = cubeProfile;
        FounderOnboardingResult = founderOnboardingResult;
        CombatNature = CombatNature.FromCube(
            founderOnboardingResult?.ElementalAffinity ?? ToCanonicalAffinity(elementalAffinity),
            cubeProfile);
    }

    public LineageId Lineage { get; }
    public GenderId Gender { get; }
    public IReadOnlyList<AptitudeId> Aptitudes { get; }
    [Obsolete("DEC-0013: professional affinities are learned during the citizen's life, not produced by onboarding.")]
    public IReadOnlyList<ProfessionFamilyId> ProfessionalAffinities { get; }
    public ElementalAffinityId ElementalAffinity { get; }
    [Obsolete("DEC-0013: combat style is learned during the citizen's life, not produced by onboarding.")]
    public CombatStyleId CombatStyle { get; }
    [Obsolete("DEC-0013: weapon preferences are learned during the citizen's life, not produced by onboarding.")]
    public IReadOnlyList<WeaponPreferenceId> WeaponPreferences { get; }
    [Obsolete("DEC-0013: traits are acquired during the citizen's life, not produced by onboarding.")]
    public IReadOnlyList<PersonalityTraitId> PersonalityTraits { get; }
    [Obsolete("DEC-0013: political orientation is not produced by onboarding.")]
    public PoliticalOrientationId PoliticalOrientation { get; }
    [Obsolete("DEC-0013: spiritual posture is not produced by onboarding.")]
    public SpiritualPostureId SpiritualPosture { get; }
    public FounderOnboardingResult? FounderOnboardingResult { get; }
    public FounderCubeProfile CubeProfile { get; }
    public CombatNature CombatNature { get; }
    public global::WorldofGoses.Domain.ElementalAffinity CanonicalElementalAffinity =>
        FounderOnboardingResult?.ElementalAffinity ?? ToCanonicalAffinity(ElementalAffinity);

    public static CitizenProfile CreateFounder(
        FounderOnboardingResult onboardingResult,
        GenderId gender)
    {
        ArgumentNullException.ThrowIfNull(onboardingResult);
        if (!ProfileCatalog.Contains(onboardingResult.Lineage))
        {
            throw new ArgumentOutOfRangeException(nameof(onboardingResult), "Founder lineage is unknown.");
        }
        if (!Enum.IsDefined(typeof(GenderId), gender))
        {
            throw new ArgumentOutOfRangeException(nameof(gender));
        }

        return new CitizenProfile(
            onboardingResult.Lineage,
            gender,
            Array.Empty<AptitudeId>(),
            Array.Empty<ProfessionFamilyId>(),
            ToLegacyAffinity(onboardingResult.ElementalAffinity),
            new CombatStyleId(string.Empty),
            Array.Empty<WeaponPreferenceId>(),
            Array.Empty<PersonalityTraitId>(),
            new PoliticalOrientationId(string.Empty),
            new SpiritualPostureId(string.Empty),
            onboardingResult.CubeProfile,
            onboardingResult);
    }

    internal CitizenProfile WithFounderOnboardingResult(FounderOnboardingResult onboardingResult)
    {
        ArgumentNullException.ThrowIfNull(onboardingResult);
        if (onboardingResult.Lineage != Lineage)
        {
            throw new ArgumentException("Founder onboarding lineage must match the citizen profile.", nameof(onboardingResult));
        }
        return new CitizenProfile(
            Lineage,
            Gender,
            Aptitudes.ToArray(),
            ProfessionalAffinities.ToArray(),
            ToLegacyAffinity(onboardingResult.ElementalAffinity),
            CombatStyle,
            WeaponPreferences.ToArray(),
            PersonalityTraits.ToArray(),
            PoliticalOrientation,
            SpiritualPosture,
            onboardingResult.CubeProfile,
            onboardingResult);
    }

    internal CitizenProfile WithFounderFallback() =>
        WithFounderOnboardingResult(new FounderOnboardingResult(
            Lineage,
            ToCanonicalAffinity(ElementalAffinity),
            CubeScoring.ComputeCubeVertex(Lineage),
            FounderNarrativeMemory.Empty));

    public static bool TryCreate(
        LineageId lineage,
        GenderId gender,
        IEnumerable<AptitudeId> aptitudes,
        IEnumerable<ProfessionFamilyId> professionalAffinities,
        ElementalAffinityId elementalAffinity,
        CombatStyleId combatStyle,
        IEnumerable<WeaponPreferenceId> weaponPreferences,
        IEnumerable<PersonalityTraitId> personalityTraits,
        PoliticalOrientationId politicalOrientation,
        SpiritualPostureId spiritualPosture,
        out CitizenProfile? profile,
        out string error) =>
        TryCreate(
            lineage,
            gender,
            aptitudes,
            professionalAffinities,
            elementalAffinity,
            combatStyle,
            weaponPreferences,
            personalityTraits,
            politicalOrientation,
            spiritualPosture,
            cubeProfile: null,
            out profile,
            out error);

    /// <summary>
    /// Overload that lets the caller pass a precomputed
    /// <see cref="FounderCubeProfile"/> instead of falling back to the
    /// lineage vertex. Used by <see cref="CityWorld.CreateMigrantProfile"/>
    /// to give ordinary citizens a deterministic ±8 shift around their
    /// lineage's vertex; the canonical path (no cube) stays unchanged so
    /// every existing test fixture keeps its vertex-shaped cube.
    /// </summary>
    public static bool TryCreate(
        LineageId lineage,
        GenderId gender,
        IEnumerable<AptitudeId> aptitudes,
        IEnumerable<ProfessionFamilyId> professionalAffinities,
        ElementalAffinityId elementalAffinity,
        CombatStyleId combatStyle,
        IEnumerable<WeaponPreferenceId> weaponPreferences,
        IEnumerable<PersonalityTraitId> personalityTraits,
        PoliticalOrientationId politicalOrientation,
        SpiritualPostureId spiritualPosture,
        FounderCubeProfile? cubeProfile,
        out CitizenProfile? profile,
        out string error)
    {
        profile = null;
        error = string.Empty;

        if (!ProfileCatalog.Contains(lineage))
        {
            error = "Choose a known lineage.";
            return false;
        }
        if (!Enum.IsDefined(typeof(GenderId), gender))
        {
            error = "Choose a known gender.";
            return false;
        }
        if (!ProfileCatalog.Contains(elementalAffinity))
        {
            error = "Choose a known elemental affinity.";
            return false;
        }
        if (!ProfileCatalog.Contains(combatStyle))
        {
            error = "Choose a known combat style.";
            return false;
        }
        if (!ProfileCatalog.Contains(politicalOrientation))
        {
            error = "Choose a known political orientation.";
            return false;
        }
        if (!ProfileCatalog.Contains(spiritualPosture))
        {
            error = "Choose a known spiritual posture.";
            return false;
        }

        if (!TryValidateSelection(aptitudes, 3, 3, ProfileCatalog.Contains, "personal aptitudes", out AptitudeId[] aptitudeValues, out error)
            || !TryValidateSelection(professionalAffinities, 3, 3, ProfileCatalog.Contains, "professional affinities", out ProfessionFamilyId[] professionValues, out error)
            || !TryValidateSelection(weaponPreferences, 1, 2, ProfileCatalog.Contains, "weapon preferences", out WeaponPreferenceId[] weaponValues, out error)
            || !TryValidateSelection(personalityTraits, 3, 3, ProfileCatalog.Contains, "personality traits", out PersonalityTraitId[] traitValues, out error))
        {
            return false;
        }

        profile = new CitizenProfile(
            lineage,
            gender,
            aptitudeValues,
            professionValues,
            elementalAffinity,
            combatStyle,
            weaponValues,
            traitValues,
            politicalOrientation,
            spiritualPosture,
            cubeProfile ?? CubeScoring.ComputeCubeVertex(lineage),
            null);
        return true;
    }

    internal static CitizenProfile Restore(
        LineageId lineage,
        GenderId gender,
        IEnumerable<AptitudeId> aptitudes,
        IEnumerable<ProfessionFamilyId> professionalAffinities,
        ElementalAffinityId elementalAffinity,
        CombatStyleId combatStyle,
        IEnumerable<WeaponPreferenceId> weaponPreferences,
        IEnumerable<PersonalityTraitId> personalityTraits,
        PoliticalOrientationId politicalOrientation,
        SpiritualPostureId spiritualPosture,
        FounderCubeProfile cubeProfile,
        FounderOnboardingResult? founderOnboardingResult)
    {
        ArgumentNullException.ThrowIfNull(cubeProfile);
        if (founderOnboardingResult is not null)
        {
            return new CitizenProfile(
                lineage,
                gender,
                aptitudes.ToArray(),
                professionalAffinities.ToArray(),
                NormalizeLegacyAffinity(elementalAffinity),
                combatStyle,
                weaponPreferences.ToArray(),
                personalityTraits.ToArray(),
                politicalOrientation,
                spiritualPosture,
                cubeProfile,
                founderOnboardingResult);
        }

        if (!TryCreate(
                lineage,
                gender,
                aptitudes,
                professionalAffinities,
                NormalizeLegacyAffinity(elementalAffinity),
                combatStyle,
                weaponPreferences,
                personalityTraits,
                politicalOrientation,
                spiritualPosture,
                out CitizenProfile? legacy,
                out string error))
        {
            throw new InvalidOperationException($"Invalid citizen profile: {error}");
        }

        return new CitizenProfile(
            legacy!.Lineage,
            legacy.Gender,
            legacy.Aptitudes.ToArray(),
            legacy.ProfessionalAffinities.ToArray(),
            legacy.ElementalAffinity,
            legacy.CombatStyle,
            legacy.WeaponPreferences.ToArray(),
            legacy.PersonalityTraits.ToArray(),
            legacy.PoliticalOrientation,
            legacy.SpiritualPosture,
            cubeProfile,
            null);
    }

    internal static global::WorldofGoses.Domain.ElementalAffinity ToCanonicalAffinity(ElementalAffinityId affinity) =>
        affinity.Value.ToLowerInvariant() switch
        {
            "earth" => global::WorldofGoses.Domain.ElementalAffinity.Earth,
            "aether" => global::WorldofGoses.Domain.ElementalAffinity.Aether,
            "water" => global::WorldofGoses.Domain.ElementalAffinity.Water,
            "fire" => global::WorldofGoses.Domain.ElementalAffinity.Fire,
            "air" => global::WorldofGoses.Domain.ElementalAffinity.Air,
            "silence" or "neutral" or "none" or "" => global::WorldofGoses.Domain.ElementalAffinity.Silence,
            _ => throw new ArgumentOutOfRangeException(nameof(affinity), affinity, "Unknown elemental affinity."),
        };

    internal static ElementalAffinityId ToLegacyAffinity(global::WorldofGoses.Domain.ElementalAffinity affinity) => affinity switch
    {
        global::WorldofGoses.Domain.ElementalAffinity.Earth => ElementalAffinityId.Earth,
        global::WorldofGoses.Domain.ElementalAffinity.Aether => ElementalAffinityId.Aether,
        global::WorldofGoses.Domain.ElementalAffinity.Water => ElementalAffinityId.Water,
        global::WorldofGoses.Domain.ElementalAffinity.Fire => ElementalAffinityId.Fire,
        global::WorldofGoses.Domain.ElementalAffinity.Silence => ElementalAffinityId.Silence,
        global::WorldofGoses.Domain.ElementalAffinity.Air => ElementalAffinityId.Air,
        _ => throw new ArgumentOutOfRangeException(nameof(affinity), affinity, null),
    };

    private static ElementalAffinityId NormalizeLegacyAffinity(ElementalAffinityId affinity) =>
        ToLegacyAffinity(ToCanonicalAffinity(affinity));

    private static bool TryValidateSelection<TId>(
        IEnumerable<TId>? selected,
        int minimum,
        int maximum,
        Func<TId, bool> isKnown,
        string label,
        out TId[] values,
        out string error)
        where TId : struct
    {
        values = selected?.ToArray() ?? Array.Empty<TId>();
        error = string.Empty;

        if (values.Length < minimum || values.Length > maximum)
        {
            error = minimum == maximum
                ? $"Choose exactly {minimum} {label}."
                : $"Choose between {minimum} and {maximum} {label}.";
            return false;
        }
        if (values.Distinct().Count() != values.Length)
        {
            error = $"The {label} selection contains duplicates.";
            return false;
        }
        if (values.Any(value => !isKnown(value)))
        {
            error = $"The {label} selection contains an unknown value.";
            return false;
        }
        return true;
    }
}
#pragma warning restore CS0618
