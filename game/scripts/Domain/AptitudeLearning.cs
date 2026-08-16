#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// What an aptitude actually does: it changes how fast a citizen learns a
/// competency, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// An aptitude is not output. Before this, the single mechanical use of the
/// whole aptitude list was <c>ConstructionRules.AptitudeBonus</c>, a flat
/// addition to how much work a citizen completed per tick — which is an
/// automatic production advantage handed out by identity, exactly what the
/// lineage pillar forbids. A learning multiplier is the other thing: two
/// citizens with the same competency level do the same work, and the one with
/// the aptitude simply got there sooner.
/// </para>
/// <para>
/// It is also why aptitudes are individual and never derived from lineage. A
/// Vaelun with Empathy learns care work faster than a Vaelun without it; the
/// lineage says nothing about which of the two you got.
/// </para>
/// <para>
/// Mechanically the factor divides the experience a level costs, in
/// <see cref="CityCompetency"/>, rather than multiplying the experience earned.
/// Both say "learns faster"; only one is exact in integers when production
/// credits a single point per tick.
/// </para>
/// <para>
/// PROVISIONAL BALANCE. The bonus per aptitude and the mapping below are first
/// proposals. What is not provisional is the shape: it changes how fast a
/// citizen learns, never how much work they produce.
/// </para>
/// </remarks>
public static class AptitudeLearning
{
    /// <summary>
    /// Multiplier added per matching aptitude. Three matching aptitudes on one
    /// competency is the ceiling a citizen can reach, because a profile holds
    /// three.
    /// </summary>
    public const double BonusPerMatchingAptitude = 0.15;

    /// <summary>Learning speed with no matching aptitude at all.</summary>
    public const double BaseLearningFactor = 1.0;

    /// <summary>
    /// Which competencies each aptitude accelerates. An aptitude that matched
    /// everything would be a strictly better aptitude, so each covers two or
    /// three and none covers all six.
    /// </summary>
    private static readonly Dictionary<AptitudeId, CompetencyId[]> Accelerates = new()
    {
        // Reading ground, weather and spoil: where to dig and when to harvest.
        [AptitudeId.Observation] = new[] { CompetencyId.Mining, CompetencyId.Foraging },

        // Care work and the coordination a shared worksite runs on.
        [AptitudeId.Empathy] = new[] { CompetencyId.Farming, CompetencyId.Construction },

        // The hands: anything whose quality depends on the millimetre.
        [AptitudeId.ManualPrecision] = new[] { CompetencyId.Smithing, CompetencyId.Construction },

        // Sustained physical load, which is most of extraction and building.
        [AptitudeId.Strength] = new[] { CompetencyId.Mining, CompetencyId.Construction },

        // Knowing where you are and how to get back.
        [AptitudeId.Orientation] = new[] { CompetencyId.Foraging, CompetencyId.Survival },

        // Recipes, seasons, routes — the professions that are mostly recall.
        [AptitudeId.Memory] = new[] { CompetencyId.Smithing, CompetencyId.Farming },

        // Making something that was not in the instructions.
        [AptitudeId.Creativity] = new[] { CompetencyId.Smithing, CompetencyId.Foraging },

        // Not panicking, and not stopping when it stops being interesting.
        [AptitudeId.SelfControl] = new[] { CompetencyId.Survival, CompetencyId.Farming },

        // Being willing to be where the work is dangerous.
        [AptitudeId.RiskTolerance] = new[] { CompetencyId.Survival, CompetencyId.Mining },

        // Learns the unfamiliar job faster than the specialist does.
        [AptitudeId.Adaptability] = new[]
        {
            CompetencyId.Foraging,
            CompetencyId.Construction,
            CompetencyId.Survival,
        },
    };

    /// <summary>
    /// How fast <paramref name="profile"/> learns <paramref name="competency"/>,
    /// as a multiplier on experience gained.
    /// </summary>
    public static double LearningFactor(CitizenProfile? profile, CompetencyId competency)
    {
        if (profile is null) return BaseLearningFactor;

        int matches = 0;
        foreach (AptitudeId aptitude in profile.Aptitudes)
        {
            if (!Accelerates.TryGetValue(aptitude, out CompetencyId[]? accelerated)) continue;
            foreach (CompetencyId accelerates in accelerated)
            {
                if (accelerates != competency) continue;
                matches++;
                break;
            }
        }

        return BaseLearningFactor + matches * BonusPerMatchingAptitude;
    }

    /// <summary>The competencies an aptitude accelerates, for UI and tests.</summary>
    public static IReadOnlyList<CompetencyId> AcceleratedBy(AptitudeId aptitude) =>
        Accelerates.TryGetValue(aptitude, out CompetencyId[]? accelerated)
            ? accelerated
            : Array.Empty<CompetencyId>();
}
