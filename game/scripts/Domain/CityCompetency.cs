#nullable enable
using System;

namespace WorldofGoses.Domain;

/// <summary>
/// The level curve for a city competency — the professions a citizen practises
/// by working, as opposed to the weapon families
/// <see cref="Combat.CompetencyLevelCurve"/> governs.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="CompetencyEntry"/> stores raw experience and nothing else, so
/// until now a city skill had no level at all: every consumer invented its own
/// thresholds inline. <c>ConstructionRules.CompetencyBonusAt</c> was the only
/// one, and its <c>(min(exp, 24) / 8) * 4</c> is a level curve written as
/// arithmetic. This is that curve, named, extended past its cap, and available
/// to the other five competencies.
/// </para>
/// <para>
/// The levels below reproduce the construction thresholds exactly for a citizen
/// with no matching aptitude, so nothing about existing balance moves.
/// </para>
/// </remarks>
public static class CityCompetency
{
    /// <summary>Experience between one level and the next.</summary>
    public const int ExperiencePerLevel = 8;

    /// <summary>Levels a city competency can reach. PROVISIONAL BALANCE.</summary>
    public const int MaximumLevel = 10;

    /// <summary>Cumulative experience an unaided citizen needs to reach a level.</summary>
    public static int ExperienceForLevel(int level) =>
        level <= 0 ? 0 : Math.Min(level, MaximumLevel) * ExperiencePerLevel;

    /// <summary>
    /// Cumulative experience <paramref name="learningFactor"/> actually has to
    /// pay for <paramref name="level"/>.
    /// </summary>
    /// <remarks>
    /// The aptitude divides the requirement instead of multiplying the grant.
    /// Both express "learns faster", but production credits one point per tick,
    /// and a multiplier on a one-point grant either rounds away to nothing or
    /// rounds up to double. Dividing the requirement is exact in integers and
    /// needs no fractional experience in the save.
    /// </remarks>
    public static int ExperienceForLevel(int level, double learningFactor)
    {
        int baseline = ExperienceForLevel(level);
        if (baseline <= 0) return 0;
        if (!double.IsFinite(learningFactor) || learningFactor <= AptitudeLearning.BaseLearningFactor)
        {
            return baseline;
        }
        // Ceiling, so a factor can never make a level free, and one experience
        // point is always still one point of progress.
        return Math.Max(1, (int)Math.Ceiling(baseline / learningFactor));
    }

    /// <summary>The level <paramref name="experience"/> buys at base learning speed.</summary>
    public static int LevelFor(int experience) =>
        LevelFor(experience, AptitudeLearning.BaseLearningFactor);

    /// <summary>The level <paramref name="experience"/> buys at this learning speed.</summary>
    public static int LevelFor(int experience, double learningFactor)
    {
        if (experience <= 0) return 0;
        int level = 0;
        while (level < MaximumLevel && experience >= ExperienceForLevel(level + 1, learningFactor))
        {
            level++;
        }
        return level;
    }

    /// <summary>
    /// The level this citizen has in <paramref name="competency"/>, including
    /// whatever their aptitudes did to how fast they got there.
    /// </summary>
    public static int LevelOf(Citizen? citizen, CompetencyId competency)
    {
        if (citizen is null) return 0;
        return LevelFor(
            citizen.GetExperience(competency),
            AptitudeLearning.LearningFactor(citizen.Profile, competency));
    }
}
