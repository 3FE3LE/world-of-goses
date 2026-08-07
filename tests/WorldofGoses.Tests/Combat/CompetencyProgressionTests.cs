using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using Xunit;

namespace WorldofGoses.Tests.Combat;

/// <summary>
/// Roadmap Fase 1: experience, learning efficiency and the level ceiling. The
/// three-tier split is a LEARNING cost — these tests pin that it never touches
/// a technique's result.
/// </summary>
public sealed class CompetencyProgressionTests
{
    // A Kovari whose cube leans Impulse: Stunning, natural families Mace and Orb.
    // The Kovari vertex is Body/Impulse/Domain, so it also reaches Fracture
    // (Hammer, Axe) and Bleeding (Sword, Daggers) — those are lineage-familiar.
    // Spear and Staff belong to Knockdown, which no Kovari cube can produce.
    private static readonly LineageId Kovari = LineageId.Kovari;
    private static readonly CombatNature Stunner =
        new(ElementalAffinity.Fire, PhysicalExpression.Stunning);

    [Fact]
    public void NaturalFamily_AbsorbsAllGeneratedExperience()
    {
        var progress = new CompetencyProgress(WeaponFamily.Mace, 0, 0);

        Assert.Equal(WeaponLearning.Natural, progress.LearningAffinity(Kovari, Stunner));
        Assert.Equal(1.00, progress.LearningEfficiency(Kovari, Stunner));
        Assert.Equal(500, progress.GrantGeneratedExperience(500, Kovari, Stunner).Experience);
    }

    [Fact]
    public void LineageFamiliarFamily_AbsorbsHalfOfGeneratedExperience()
    {
        // Sword is Bleeding's, not Stunning's — but Bleeding sits on the Kovari
        // vertex, so this citizen grew up around it.
        var progress = new CompetencyProgress(WeaponFamily.Sword, 0, 0);

        Assert.Equal(WeaponLearning.LineageFamiliar, progress.LearningAffinity(Kovari, Stunner));
        Assert.Equal(0.50, progress.LearningEfficiency(Kovari, Stunner));
        Assert.Equal(250, progress.GrantGeneratedExperience(500, Kovari, Stunner).Experience);
    }

    [Fact]
    public void ForeignFamily_AbsorbsATenthOfGeneratedExperience()
    {
        var progress = new CompetencyProgress(WeaponFamily.Spear, 0, 0);

        Assert.Equal(WeaponLearning.Foreign, progress.LearningAffinity(Kovari, Stunner));
        Assert.Equal(0.10, progress.LearningEfficiency(Kovari, Stunner));
        Assert.Equal(50, progress.GrantGeneratedExperience(500, Kovari, Stunner).Experience);
    }

    [Fact]
    public void TheSameWeaponIsFamiliarToOneLineageAndForeignToAnother()
    {
        // The tier is not a property of the weapon. Spear is Knockdown's, which
        // Vaelun reaches and Kovari does not — same citizen nature, same family,
        // different people.
        var spear = new CompetencyProgress(WeaponFamily.Spear, 0, 0);

        Assert.Equal(WeaponLearning.Foreign, spear.LearningAffinity(LineageId.Kovari, Stunner));
        Assert.Equal(WeaponLearning.LineageFamiliar, spear.LearningAffinity(LineageId.Vaelun, Stunner));
    }

    [Fact]
    public void EveryLineageSeesTwoNaturalFourFamiliarAndSixForeignFamilies()
    {
        foreach (LineageId lineage in ProfileCatalog.Lineages.Select(entry => entry.Id))
        {
            foreach (PhysicalExpression expression in CubeExpression.NaturallyAvailableTo(lineage))
            {
                Assert.Equal(2, WeaponLearningAffinity.FamiliesOf(lineage, expression, WeaponLearning.Natural).Count);
                Assert.Equal(4, WeaponLearningAffinity.FamiliesOf(lineage, expression, WeaponLearning.LineageFamiliar).Count);
                Assert.Equal(6, WeaponLearningAffinity.FamiliesOf(lineage, expression, WeaponLearning.Foreign).Count);
            }
        }
    }

    [Fact]
    public void TrainingAForeignOrFamiliarWeapon_NeverChangesThePhysicalExpression()
    {
        var whip = new CompetencyProgress(WeaponFamily.Whip, 0, 0);
        var spear = new CompetencyProgress(WeaponFamily.Spear, 0, 0);

        CompetencyProgress trainedWhip = whip.GrantGeneratedExperience(10_000, Kovari, Stunner);
        CompetencyProgress trainedSpear = spear.GrantGeneratedExperience(10_000, Kovari, Stunner);

        Assert.True(trainedWhip.Experience > 0);
        Assert.True(trainedSpear.Experience > 0);
        Assert.Equal(PhysicalExpression.Stunning, Stunner.PhysicalExpression);
        Assert.Equal(ElementalAffinity.Fire, Stunner.ElementalAffinity);
    }

    [Fact]
    public void TheTierScalesExperienceOnly_NotWhatALevelIsWorth()
    {
        // Reaching level 12 through a foreign family costs ten times the
        // experience, and buys exactly the same skill factor. The difficulty is
        // in getting there, never in using it afterwards.
        var config = StatisticsBalanceConfig.Default;
        var natural = new CompetencyProgress(WeaponFamily.Mace, 12, 0, config);
        var foreign = new CompetencyProgress(WeaponFamily.Spear, 12, 0, config);

        Assert.Equal(config.SkillFactor(12), config.SkillFactor(foreign.Level));
        Assert.Equal(config.SkillFactor(natural.Level), config.SkillFactor(foreign.Level));
        Assert.Equal(
            10.0,
            natural.LearningEfficiency(Kovari, Stunner) / foreign.LearningEfficiency(Kovari, Stunner),
            precision: 10);
    }

    [Fact]
    public void LevelIsCappedAtTwenty_NoMatterHowMuchExperience()
    {
        var curve = new CompetencyLevelCurve();

        Assert.Equal(20, curve.MaximumLevel);
        Assert.Equal(20, curve.LevelFor(double.MaxValue / 4));
        Assert.Equal(20, curve.LevelFor(curve.ExperienceRequiredFor(20) * 100));

        var progress = new CompetencyProgress(WeaponFamily.Mace, 0, 0)
            .GrantAndLevel(1_000_000, Kovari, Stunner, curve);
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
