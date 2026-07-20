using System.Collections.Generic;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class ConstructionRulesTests
{
    [Fact]
    public void Contribution_HeroWithTwoRelevantAptitudes_AddsEight()
    {
        var profile = NewProfile(AptitudeId.Observation, AptitudeId.ManualPrecision, AptitudeId.Empathy);
        var hero = NewHeroCitizen(profile, constructionExperience: 0);

        int contribution = ConstructionRules.ContributionPerWorkInterval(hero);

        Assert.Equal(
            ConstructionRules.BaseContributionPerWorkInterval
            + ConstructionRules.AptitudeBonusPerAptitude * 2,
            contribution);
    }

    [Fact]
    public void Contribution_ProfileWithThreeRelevantAptitudes_AddsTwelve()
    {
        var profile = NewProfile(
            AptitudeId.Strength, AptitudeId.Observation, AptitudeId.ManualPrecision);
        var hero = NewHeroCitizen(profile, constructionExperience: 0);

        int contribution = ConstructionRules.ContributionPerWorkInterval(hero);

        Assert.Equal(
            ConstructionRules.BaseContributionPerWorkInterval
            + ConstructionRules.AptitudeBonusPerAptitude * 3,
            contribution);
    }

    [Fact]
    public void Contribution_StaminaBelowCost_IsZero()
    {
        var profile = NewProfile(AptitudeId.Observation, AptitudeId.ManualPrecision, AptitudeId.Strength);
        var hero = NewHeroCitizen(profile, constructionExperience: 0, maxStamina: 100);
        hero.ConsumeStamina(hero.CurrentStamina - 7);

        Assert.Equal(0, ConstructionRules.ContributionPerWorkInterval(hero));
    }

    [Fact]
    public void Contribution_HighConstructionExperience_RespectsCap()
    {
        var profile = NewProfile(AptitudeId.Strength, AptitudeId.ManualPrecision, AptitudeId.Observation);
        var hero = NewHeroCitizen(profile, constructionExperience: 200);

        int contribution = ConstructionRules.ContributionPerWorkInterval(hero);

        int expected = ConstructionRules.BaseContributionPerWorkInterval
            + ConstructionRules.AptitudeBonusPerAptitude * 3
            + ConstructionRules.CompetencyBonusCap;
        Assert.Equal(expected, contribution);
    }

    [Fact]
    public void PhaseFor_ZeroProgress_IsPlanned()
    {
        Assert.Equal(ConstructionVisualPhase.Planned, ConstructionRules.PhaseFor(0, 100));
        Assert.Equal(ConstructionVisualPhase.Planned, ConstructionRules.PhaseFor(100, 0));
    }

    [Fact]
    public void PhaseFor_Half_IsAdvanced()
    {
        Assert.Equal(ConstructionVisualPhase.Advanced, ConstructionRules.PhaseFor(50, 100));
    }

    [Fact]
    public void PhaseFor_Complete_IsComplete()
    {
        Assert.Equal(ConstructionVisualPhase.Complete, ConstructionRules.PhaseFor(100, 100));
    }

    private static Citizen NewHeroCitizen(CitizenProfile profile, int constructionExperience, int maxStamina = 100)
    {
        var citizen = new Citizen(new CitizenId(1), "Hero", appearanceSeed: 1, profile: profile, maxStamina: maxStamina);
        if (constructionExperience > 0) citizen.AddExperience(CompetencyId.Construction, constructionExperience);
        return citizen;
    }

    private static CitizenProfile NewProfile(params AptitudeId[] aptitudes)
    {
        bool created = CitizenProfile.TryCreate(
            LineageId.Ardhen,
            aptitudes,
            new[] { ProfessionFamilyId.ConstructionInfrastructure, ProfessionFamilyId.MedicineCare, ProfessionFamilyId.Extraction },
            ElementalAffinityId.Water,
            CombatStyleId.DefensiveSupport,
            new[] { WeaponPreferenceId.Polearm },
            new[] { PersonalityTraitId.Patient, PersonalityTraitId.Protective, PersonalityTraitId.Reflective },
            PoliticalOrientationId.Communitarian,
            SpiritualPostureId.Contemplative,
            out CitizenProfile? profile,
            out string error);
        if (!created) throw new System.InvalidOperationException(error);
        return profile!;
    }
}

internal static class CitizenTestExtensions
{
}
