using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using Xunit;

namespace WorldofGoses.Tests.Combat;

/// <summary>
/// Roadmap Fase 1: experience, learning efficiency and the level ceiling. The
/// natural/foreign split is a LEARNING penalty — these tests pin that it never
/// touches a technique's result.
/// </summary>
public sealed class CompetencyProgressionTests
{
    // Stunning's natural families are Mace and Orb; Spear and Staff are foreign to it.
    private static readonly CombatNature Stunner = new(ElementalAffinity.Fire);

    [Fact]
    public void NaturalFamily_AbsorbsAllGeneratedExperience()
    {
        var progress = new CompetencyProgress(WeaponFamily.Mace, 0, 0);

        Assert.Equal(1.00, progress.LearningEfficiency(Stunner));
        Assert.Equal(500, progress.GrantGeneratedExperience(500, Stunner).Experience);
    }

    [Fact]
    public void ForeignFamily_AbsorbsATenthOfGeneratedExperience()
    {
        var progress = new CompetencyProgress(WeaponFamily.Spear, 0, 0);

        Assert.Equal(0.10, progress.LearningEfficiency(Stunner));
        Assert.Equal(50, progress.GrantGeneratedExperience(500, Stunner).Experience);
    }

    [Fact]
    public void LevelIsCappedAtTwenty_NoMatterHowMuchExperience()
    {
        var curve = new CompetencyLevelCurve();

        Assert.Equal(20, curve.MaximumLevel);
        Assert.Equal(20, curve.LevelFor(double.MaxValue / 4));
        Assert.Equal(20, curve.LevelFor(curve.ExperienceRequiredFor(20) * 100));

        var progress = new CompetencyProgress(WeaponFamily.Mace, 0, 0)
            .GrantAndLevel(1_000_000, Stunner, curve);
        Assert.Equal(20, progress.Level);
    }

    [Fact]
    public void TheCurveIsMonotonicAndStartsAtZero()
    {
        var curve = new CompetencyLevelCurve();

        Assert.Equal(0, curve.LevelFor(0));
        Assert.Equal(0, curve.ExperienceRequiredFor(0));
        double previous = -1;
        for (int level = 0; level <= curve.MaximumLevel; level++)
        {
            double required = curve.ExperienceRequiredFor(level);
            Assert.True(required > previous, $"Level {level} must cost more than {level - 1}.");
            Assert.Equal(level, curve.LevelFor(required));
            previous = required;
        }
    }

    [Fact]
    public void LearningCeiling_StopsProgressBelowTheGlobalMaximum()
    {
        var curve = new CompetencyLevelCurve();
        double plenty = curve.ExperienceRequiredFor(20);

        Assert.Equal(20, curve.LevelFor(plenty));
        Assert.Equal(6, curve.LevelFor(plenty, learningCeiling: 6));
        Assert.Null(curve.ExperienceToNextLevel(plenty, learningCeiling: 6));
    }

    [Fact]
    public void ExperienceToNextLevel_ReportsTheRemainingGap()
    {
        var curve = new CompetencyLevelCurve();
        double atLevelTwo = curve.ExperienceRequiredFor(2);

        double? remaining = curve.ExperienceToNextLevel(atLevelTwo);

        Assert.NotNull(remaining);
        Assert.Equal(curve.ExperienceRequiredFor(3) - atLevelTwo, remaining!.Value, 6);
    }

    [Fact]
    public void SurvivalIsAProfessionCompetency_DistinctFromWeaponFamilies()
    {
        // Guardrail: no separate levels for affinity or expression, and the
        // profession competency is not a weapon family.
        Assert.Equal("survival", CompetencyId.Survival.Value);
        Citizen citizen = TestHelpers.NewCitizen(4242);

        citizen.AddExperience(CompetencyId.Survival, 30);

        Assert.Equal(30, citizen.GetExperience(CompetencyId.Survival));
        Assert.Empty(citizen.WeaponCompetencies);
    }
}
