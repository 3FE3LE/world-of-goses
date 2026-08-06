#nullable enable
using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Weapon-family progression. Experience is retained independently from the
/// current level because the level threshold curve is intentionally pending.
/// </summary>
public sealed record CompetencyProgress
{
    public CompetencyProgress(
        WeaponFamily family,
        int level,
        double experience,
        StatisticsBalanceConfig? balance = null)
    {
        StatisticsBalanceConfig config = balance ?? StatisticsBalanceConfig.Default;
        config.Validate();
        if (!Enum.IsDefined(family)) throw new ArgumentOutOfRangeException(nameof(family));
        if (level < config.MinimumSkillLevel || level > config.MaximumSkillLevel)
            throw new ArgumentOutOfRangeException(nameof(level), level, $"Skill level must be in [{config.MinimumSkillLevel}, {config.MaximumSkillLevel}].");
        if (!double.IsFinite(experience) || experience < 0)
            throw new ArgumentOutOfRangeException(nameof(experience), experience, "Experience must be finite and non-negative.");
        Family = family;
        Level = level;
        Experience = experience;
    }

    public WeaponFamily Family { get; }
    public int Level { get; }
    public double Experience { get; }

    public CompetencyProgress GrantGeneratedExperience(
        double generatedExperience,
        CombatNature nature,
        StatisticsBalanceConfig? balance = null)
    {
        ArgumentNullException.ThrowIfNull(nature);
        if (!double.IsFinite(generatedExperience) || generatedExperience < 0)
            throw new ArgumentOutOfRangeException(nameof(generatedExperience));
        StatisticsBalanceConfig config = balance ?? StatisticsBalanceConfig.Default;
        return new CompetencyProgress(
            Family,
            Level,
            Experience + generatedExperience * LearningEfficiency(nature, config),
            config);
    }

    /// <summary>
    /// Fraction of generated experience this family actually absorbs: full rate
    /// for a family the citizen's physical expression makes natural, a tenth for a
    /// foreign one. The penalty is on learning, never on the technique's result.
    /// </summary>
    public double LearningEfficiency(
        CombatNature nature,
        StatisticsBalanceConfig? balance = null)
    {
        ArgumentNullException.ThrowIfNull(nature);
        StatisticsBalanceConfig config = balance ?? StatisticsBalanceConfig.Default;
        return NaturalWeaponFamilies.Contains(nature.PhysicalExpression, Family)
            ? config.NaturalWeaponExperienceFactor
            : config.ForeignWeaponExperienceFactor;
    }

    /// <summary>
    /// Grants experience and re-derives the level from the curve, which is the
    /// piece this record previously left pending. The persisted shape is
    /// unchanged — level stays stored — so no save migration is required; the
    /// curve is simply now the authority that produces it.
    /// </summary>
    public CompetencyProgress GrantAndLevel(
        double generatedExperience,
        CombatNature nature,
        Combat.CompetencyLevelCurve curve,
        int? learningCeiling = null,
        StatisticsBalanceConfig? balance = null)
    {
        ArgumentNullException.ThrowIfNull(curve);
        CompetencyProgress granted = GrantGeneratedExperience(generatedExperience, nature, balance);
        int level = curve.LevelFor(granted.Experience, learningCeiling);
        return new CompetencyProgress(
            granted.Family,
            level,
            granted.Experience,
            balance ?? StatisticsBalanceConfig.Default);
    }
}
