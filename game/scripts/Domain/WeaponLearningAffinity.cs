#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>How readily a citizen accumulates experience in a weapon family.</summary>
public enum WeaponLearning
{
    /// <summary>A family of the citizen's own physical expression.</summary>
    Natural,

    /// <summary>
    /// A family of one of the other two expressions their lineage's cube vertex
    /// can produce. Not their own nature, but not alien to their people either.
    /// </summary>
    LineageFamiliar,

    /// <summary>A family belonging to an expression the lineage cannot produce.</summary>
    Foreign,
}

/// <summary>
/// Classifies a weapon family for one citizen, from three pieces that already
/// exist: their physical expression, their lineage's cube vertex, and the
/// canonical expression → weapon-family table.
/// </summary>
/// <remarks>
/// <para>
/// There is deliberately no per-lineage weapon table here. An Ardhen learns
/// Whip at half rate because Paralysis is reachable from the Ardhen vertex, not
/// because a list says so — change the vertex and the classification follows.
/// </para>
/// <para>
/// The tier scales experience <em>acquisition</em> only. It never touches
/// damage, accuracy, channel power, cooldown, defence or technique
/// coefficients. A citizen who reaches Sword level 20 through a foreign family
/// has genuinely reached level 20 and fights exactly like anyone else at that
/// level; the cost was in getting there, not in what they do afterwards.
/// </para>
/// </remarks>
public static class WeaponLearningAffinity
{
    public static WeaponLearning For(
        LineageId lineage,
        PhysicalExpression expression,
        WeaponFamily family)
    {
        if (NaturalWeaponFamilies.Contains(expression, family))
        {
            return WeaponLearning.Natural;
        }

        foreach (PhysicalExpression available in CubeExpression.NaturallyAvailableTo(lineage))
        {
            if (NaturalWeaponFamilies.Contains(available, family))
            {
                return WeaponLearning.LineageFamiliar;
            }
        }

        return WeaponLearning.Foreign;
    }

    /// <summary>Fraction of generated experience the family absorbs.</summary>
    public static double ExperienceFactor(WeaponLearning learning, StatisticsBalanceConfig? balance = null)
    {
        StatisticsBalanceConfig config = balance ?? StatisticsBalanceConfig.Default;
        return learning switch
        {
            WeaponLearning.Natural => config.NaturalWeaponExperienceFactor,
            WeaponLearning.LineageFamiliar => config.LineageFamiliarWeaponExperienceFactor,
            WeaponLearning.Foreign => config.ForeignWeaponExperienceFactor,
            _ => throw new ArgumentOutOfRangeException(nameof(learning), learning, "Unknown weapon learning tier."),
        };
    }

    /// <summary>Every family of the given tier for this lineage and expression.</summary>
    public static IReadOnlyList<WeaponFamily> FamiliesOf(
        LineageId lineage,
        PhysicalExpression expression,
        WeaponLearning learning)
    {
        var families = new List<WeaponFamily>();
        foreach (WeaponFamily family in Enum.GetValues<WeaponFamily>())
        {
            if (For(lineage, expression, family) == learning)
            {
                families.Add(family);
            }
        }

        return families;
    }
}
