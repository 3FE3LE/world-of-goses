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
        double factor = NaturalWeaponFamilies.Contains(nature.PhysicalExpression, Family)
            ? config.NaturalWeaponExperienceFactor
            : config.ForeignWeaponExperienceFactor;
        return new CompetencyProgress(Family, Level, Experience + generatedExperience * factor, config);
    }
}
