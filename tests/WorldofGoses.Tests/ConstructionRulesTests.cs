using System.Collections.Generic;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class ConstructionRulesTests
{
    /// <summary>
    /// An aptitude is not output. Two citizens who have never built anything
    /// contribute the same amount, however gifted one of them is.
    /// </summary>
    /// <remarks>
    /// These two tests used to assert the opposite — that three matching
    /// aptitudes added twelve work per interval at zero experience. That is an
    /// automatic production advantage granted by identity, which the lineage
    /// pillar forbids. What an aptitude buys now is the road to a level.
    /// </remarks>
    [Fact]
    public void Contribution_AptitudesAlone_ChangeNothing()
    {
        var gifted = NewHeroCitizen(
            NewProfile(AptitudeId.Strength, AptitudeId.Observation, AptitudeId.ManualPrecision),
            constructionExperience: 0);
        var plain = NewHeroCitizen(
            NewProfile(AptitudeId.Creativity, AptitudeId.RiskTolerance, AptitudeId.Adaptability),
            constructionExperience: 0);

        Assert.Equal(
            ConstructionRules.BaseContributionPerWorkInterval,
            ConstructionRules.ContributionPerWorkInterval(gifted));
        Assert.Equal(
            ConstructionRules.ContributionPerWorkInterval(plain),
            ConstructionRules.ContributionPerWorkInterval(gifted));
    }

    /// <summary>
    /// The same experience buys a higher construction level for a citizen whose
    /// aptitudes suit the work — and so, at that moment, more contribution.
    /// </summary>
    [Fact]
    public void Contribution_MatchingAptitudes_ReachTheNextLevelSooner()
    {
        // Strength and ManualPrecision both accelerate Construction; neither
        // Creativity nor RiskTolerance does.
        var gifted = NewHeroCitizen(
            NewProfile(AptitudeId.Strength, AptitudeId.ManualPrecision, AptitudeId.Empathy),
            constructionExperience: 7);
        var plain = NewHeroCitizen(
            NewProfile(AptitudeId.Creativity, AptitudeId.RiskTolerance, AptitudeId.Memory),
            constructionExperience: 7);

        // Seven experience is one short of the unaided first level.
        Assert.Equal(0, CityCompetency.LevelOf(plain, CompetencyId.Construction));
        Assert.Equal(1, CityCompetency.LevelOf(gifted, CompetencyId.Construction));
        Assert.True(
            ConstructionRules.ContributionPerWorkInterval(gifted)
            > ConstructionRules.ContributionPerWorkInterval(plain));
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
            GenderId.Masculine,
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
