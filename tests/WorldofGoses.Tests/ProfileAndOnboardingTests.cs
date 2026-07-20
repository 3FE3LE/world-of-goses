using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public class ProfileCatalogTests
{
    [Fact]
    public void Catalog_ContainsEightLineagesAndTwelveProfessionFamilies()
    {
        Assert.Equal(8, ProfileCatalog.Lineages.Count);
        Assert.Equal(12, ProfileCatalog.ProfessionFamilies.Count);
        Assert.Equal(8, ProfileCatalog.Lineages.Select(lineage => lineage.Id).Distinct().Count());
    }

    [Fact]
    public void Profile_RequiresTheAgreedCardinalities()
    {
        bool valid = CitizenProfile.TryCreate(
            LineageId.Caelith,
            new[] { AptitudeId.Observation, AptitudeId.Memory, AptitudeId.Creativity },
            new[] { ProfessionFamilyId.MedicineCare, ProfessionFamilyId.ResearchEducation, ProfessionFamilyId.Extraction },
            ElementalAffinityId.Water,
            CombatStyleId.Precision,
            new[] { WeaponPreferenceId.Ranged },
            new[] { PersonalityTraitId.Curious, PersonalityTraitId.Reflective, PersonalityTraitId.Patient },
            PoliticalOrientationId.Autonomist,
            SpiritualPostureId.Skeptical,
            out CitizenProfile? profile,
            out string error);

        Assert.True(valid, error);
        Assert.NotNull(profile);
    }

    [Fact]
    public void Profile_AllowsPersonalAffinitiesToContradictLineage()
    {
        bool valid = CitizenProfile.TryCreate(
            LineageId.Eirune,
            new[] { AptitudeId.Strength, AptitudeId.Orientation, AptitudeId.RiskTolerance },
            new[] { ProfessionFamilyId.Extraction, ProfessionFamilyId.SecurityCombat, ProfessionFamilyId.EngineeringManufacturing },
            ElementalAffinityId.Fire,
            CombatStyleId.DirectAssault,
            new[] { WeaponPreferenceId.Heavy, WeaponPreferenceId.Polearm },
            new[] { PersonalityTraitId.Bold, PersonalityTraitId.Tenacious, PersonalityTraitId.Restless },
            PoliticalOrientationId.SecurityOriented,
            SpiritualPostureId.Secular,
            out CitizenProfile? profile,
            out string error);

        Assert.True(valid, error);
        Assert.Equal(LineageId.Eirune, profile!.Lineage);
        Assert.Contains(ProfessionFamilyId.Extraction, profile.ProfessionalAffinities);
    }

    [Fact]
    public void Profile_RejectsDuplicateSelections()
    {
        bool valid = CitizenProfile.TryCreate(
            LineageId.Ardhen,
            new[] { AptitudeId.Observation, AptitudeId.Observation, AptitudeId.Empathy },
            new[] { ProfessionFamilyId.Extraction, ProfessionFamilyId.MedicineCare, ProfessionFamilyId.Logistics },
            ElementalAffinityId.Water,
            CombatStyleId.DefensiveSupport,
            new[] { WeaponPreferenceId.Shield },
            new[] { PersonalityTraitId.Patient, PersonalityTraitId.Protective, PersonalityTraitId.Reflective },
            PoliticalOrientationId.Communitarian,
            SpiritualPostureId.Contemplative,
            out _,
            out string error);

        Assert.False(valid);
        Assert.Contains("duplicates", error);
    }
}
