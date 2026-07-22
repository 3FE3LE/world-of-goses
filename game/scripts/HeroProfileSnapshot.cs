#nullable enable
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
    IReadOnlyList<string> Aptitudes,
    IReadOnlyList<string> ProfessionalAffinities,
    IReadOnlyList<string> MarkedAffinities,
    string ElementalAffinity,
    string CombatStyle,
    IReadOnlyList<string> WeaponPreferences,
    IReadOnlyList<string> PersonalityTraits,
    string PoliticalOrientation,
    string SpiritualPosture,
    int CurrentStamina,
    int MaxStamina,
    bool IsAtHome)
{
    public static HeroProfileSnapshot? From(CityWorld world)
    {
        Citizen? hero = world.Hero;
        if (hero is null) return null;

        CitizenProfile profile = hero.Profile;
        LineageDefinition lineage = ProfileCatalog.Get(profile.Lineage);
        return new HeroProfileSnapshot(
            hero.Id,
            hero.Name,
            lineage.DisplayName,
            lineage.Summary,
            lineage.LearningApproach,
            profile.Lineage,
            profile.Gender,
            profile.Aptitudes.Select(ProfileCatalog.DisplayName).ToArray(),
            profile.ProfessionalAffinities.Select(ProfileCatalog.DisplayName).ToArray(),
            lineage.MarkedAffinities.Select(ProfileCatalog.DisplayName).ToArray(),
            ProfileCatalog.DisplayName(profile.ElementalAffinity),
            ProfileCatalog.DisplayName(profile.CombatStyle),
            profile.WeaponPreferences.Select(ProfileCatalog.DisplayName).ToArray(),
            profile.PersonalityTraits.Select(ProfileCatalog.DisplayName).ToArray(),
            ProfileCatalog.DisplayName(profile.PoliticalOrientation),
            ProfileCatalog.DisplayName(profile.SpiritualPosture),
            hero.CurrentStamina,
            hero.MaxStamina,
            hero.CurrentLocation == CitizenLocation.AtHome);
    }
}
