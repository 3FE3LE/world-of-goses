#nullable enable
using System;

namespace WorldofGoses.Domain.Combat;

/// <summary>
/// The single owner of the experience-to-level relationship. Levels feed
/// SkillFactor through the existing statistics system; this type never
/// reimplements that curve, it only decides which level an amount of
/// accumulated experience has earned.
///
/// <para>
/// Level bounds come from <see cref="StatisticsBalanceConfig"/> (0..20) so the
/// competency model and the statistics model cannot disagree about the ceiling.
/// The shape of the curve is provisional balance from
/// <see cref="CombatBalanceConfig"/>.
/// </para>
/// </summary>
public sealed class CompetencyLevelCurve
{
    private readonly StatisticsBalanceConfig _stats;
    private readonly CombatBalanceConfig _combat;

    public CompetencyLevelCurve(
        StatisticsBalanceConfig? stats = null,
        CombatBalanceConfig? combat = null)
    {
        _stats = stats ?? StatisticsBalanceConfig.Default;
        _combat = combat ?? CombatBalanceConfig.Default;
        _stats.Validate();
        _combat.Validate();
    }

    public int MinimumLevel => _stats.MinimumSkillLevel;
    public int MaximumLevel => _stats.MaximumSkillLevel;

    /// <summary>
    /// Cumulative experience needed to have reached <paramref name="level"/>.
    /// Level <see cref="MinimumLevel"/> always costs nothing.
    /// </summary>
    public double ExperienceRequiredFor(int level)
    {
        if (level <= MinimumLevel) return 0;
        int clamped = Math.Min(level, MaximumLevel);
        return _combat.BaseExperiencePerLevel
            * Math.Pow(clamped - MinimumLevel, _combat.ExperienceGrowthExponent);
    }

    /// <summary>
    /// The level a total experience amount has earned, never above
    /// <paramref name="learningCeiling"/> and never above <see cref="MaximumLevel"/>.
    /// The ceiling models the roadmap's rule that routine practice cannot raise a
    /// competency indefinitely past what the activity can teach.
    /// </summary>
    public int LevelFor(double accumulatedExperience, int? learningCeiling = null)
    {
        if (!double.IsFinite(accumulatedExperience) || accumulatedExperience < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(accumulatedExperience),
                accumulatedExperience,
                "Accumulated experience must be finite and non-negative.");
        }
        int ceiling = Math.Clamp(learningCeiling ?? MaximumLevel, MinimumLevel, MaximumLevel);
        int level = MinimumLevel;
        for (int candidate = MinimumLevel + 1; candidate <= ceiling; candidate++)
        {
            if (accumulatedExperience + 1e-9 < ExperienceRequiredFor(candidate)) break;
            level = candidate;
        }
        return level;
    }

    /// <summary>
    /// Experience still needed for the next level, or null at the ceiling. Exposed
    /// so telemetry can explain progress instead of showing an opaque total.
    /// </summary>
    public double? ExperienceToNextLevel(double accumulatedExperience, int? learningCeiling = null)
    {
        int ceiling = Math.Clamp(learningCeiling ?? MaximumLevel, MinimumLevel, MaximumLevel);
        int level = LevelFor(accumulatedExperience, ceiling);
        if (level >= ceiling) return null;
        return Math.Max(0, ExperienceRequiredFor(level + 1) - accumulatedExperience);
    }
}
