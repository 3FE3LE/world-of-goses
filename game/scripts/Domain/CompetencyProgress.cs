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
        LineageId lineage,
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
            Experience + generatedExperience * LearningEfficiency(lineage, nature, config),
            config);
    }

    /// <summary>
    /// Which of the three learning tiers this family falls in for the citizen.
    /// The lineage is required because the middle tier is defined by what the
    /// lineage's cube vertex can produce, not by the citizen's own expression.
    /// </summary>
    public WeaponLearning LearningAffinity(LineageId lineage, CombatNature nature)
    {
        ArgumentNullException.ThrowIfNull(nature);
        return WeaponLearningAffinity.For(lineage, nature.PhysicalExpression, Family);
    }

    /// <summary>
    /// Fraction of generated experience this family actually absorbs: the full
    /// rate for a family of the citizen's own physical expression, half for one
    /// of the other two expressions their lineage can produce, a tenth for a
    /// family no expression of that lineage reaches.
    /// </summary>
    /// <remarks>
    /// The tier is a learning cost, never a combat one. It scales the
    /// experience absorbed and nothing else — a level reached through a foreign
    /// family is worth exactly as much as any other level at that number.
    /// </remarks>
    public double LearningEfficiency(
        LineageId lineage,
        CombatNature nature,
        StatisticsBalanceConfig? balance = null)
    {
        ArgumentNullException.ThrowIfNull(nature);
        return WeaponLearningAffinity.ExperienceFactor(LearningAffinity(lineage, nature), balance);
    }

    /// <summary>
    /// Grants experience and re-derives the level from the curve, which is the
    /// piece this record previously left pending. The persisted shape is
    /// unchanged — level stays stored — so no save migration is required; the
    /// curve is simply now the authority that produces it.
    /// </summary>
    public CompetencyProgress GrantAndLevel(
        double generatedExperience,
        LineageId lineage,
        CombatNature nature,
        Combat.CompetencyLevelCurve curve,
        int? learningCeiling = null,
        StatisticsBalanceConfig? balance = null)
    {
        ArgumentNullException.ThrowIfNull(curve);
        CompetencyProgress granted = GrantGeneratedExperience(generatedExperience, lineage, nature, balance);
        int level = curve.LevelFor(granted.Experience, learningCeiling);
        return new CompetencyProgress(
            granted.Family,
            level,
            granted.Experience,
            balance ?? StatisticsBalanceConfig.Default);
    }
}
