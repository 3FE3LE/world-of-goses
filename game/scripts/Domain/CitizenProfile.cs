#nullable enable
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
        SpiritualPostureId spiritualPosture)
    {
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
    }

    public LineageId Lineage { get; }
    public GenderId Gender { get; }
    public IReadOnlyList<AptitudeId> Aptitudes { get; }
    public IReadOnlyList<ProfessionFamilyId> ProfessionalAffinities { get; }
    public ElementalAffinityId ElementalAffinity { get; }
    public CombatStyleId CombatStyle { get; }
    public IReadOnlyList<WeaponPreferenceId> WeaponPreferences { get; }
    public IReadOnlyList<PersonalityTraitId> PersonalityTraits { get; }
    public PoliticalOrientationId PoliticalOrientation { get; }
    public SpiritualPostureId SpiritualPosture { get; }

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
            spiritualPosture);
        return true;
    }

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