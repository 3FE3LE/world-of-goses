using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

public sealed class TempoStatisticsCalculator
{
    private readonly StatisticsBalanceConfig _balance;

    public TempoStatisticsCalculator(StatisticsBalanceConfig balance)
    {
        ArgumentNullException.ThrowIfNull(balance);
        balance.Validate();
        _balance = balance;
    }

    public TempoStatistics Calculate(
        FounderCubeProfile cube,
        EquipmentLoadout loadout,
        StatCalculationContext context)
    {
        ArgumentNullException.ThrowIfNull(cube);
        ArgumentNullException.ThrowIfNull(loadout);
        ArgumentNullException.ThrowIfNull(context);
        StatisticsCalculation.ValidateInputs(loadout, context, _balance);
        GearSupportProfile gear = loadout.TotalGearSupport;
        EffectiveCubeProfile effective = EffectiveCubeProfile.From(cube, gear);
        double skill = StatisticsCalculation.SkillFactor(context.ApplicableSkillLevel, _balance);

        return new TempoStatistics(
            Curved("AttackSpeed", cube, gear, effective, context, skill, new[] { CubeFace.Impulse }, _balance.AttackSpeedMinimum, _balance.AttackSpeedMaximum),
            Curved("CastSpeed", cube, gear, effective, context, skill, new[] { CubeFace.Impulse, CubeFace.Bond }, _balance.CastSpeedMinimum, _balance.CastSpeedMaximum),
            Curved("CooldownReduction", cube, gear, effective, context, skill, new[] { CubeFace.Impulse, CubeFace.Domain }, _balance.CooldownReductionMinimum, _balance.CooldownReductionMaximum),
            Curved("CriticalChance", cube, gear, effective, context, skill, new[] { CubeFace.Domain }, _balance.CriticalChanceMinimum, _balance.CriticalChanceMaximum),
            Curved("PhysicalEvasion", cube, gear, effective, context, skill, new[] { CubeFace.Impulse, CubeFace.Reach }, _balance.PhysicalEvasionMinimum, _balance.PhysicalEvasionMaximum),
            Curved("ElementalEvasion", cube, gear, effective, context, skill, new[] { CubeFace.Bond, CubeFace.Reach }, _balance.ElementalEvasionMinimum, _balance.ElementalEvasionMaximum),
            Curved("MovementSpeed", cube, gear, effective, context, skill, new[] { CubeFace.Reach }, _balance.MovementSpeedMinimum, _balance.MovementSpeedMaximum));
    }

    private CalculatedStatistic Curved(
        string id,
        FounderCubeProfile cube,
        GearSupportProfile gear,
        EffectiveCubeProfile effective,
        StatCalculationContext context,
        double skill,
        IReadOnlyList<CubeFace> faceIds,
        double minimum,
        double maximum)
    {
        var faces = new List<CubeFaceCalculation>(faceIds.Count);
        double baseScore = 0;
        foreach (CubeFace face in faceIds)
        {
            faces.Add(StatisticsCalculation.Face(face, cube, gear, effective));
            baseScore += effective.For(face);
        }
        baseScore /= faceIds.Count;
        double score = baseScore * skill * context.ConditionFactor * context.CitySupportFactor;
        double value = StatisticsCalculation.Smoothstep(score, minimum, maximum, _balance);
        bool cappedAtMinimum = score <= _balance.SmoothstepScoreMinimum;
        bool cappedAtMaximum = score >= _balance.SmoothstepScoreMinimum + _balance.SmoothstepScoreRange;
        var intermediate = new Dictionary<string, double>
        {
            ["BaseScore"] = baseScore,
            ["AdjustedScore"] = score,
            ["Minimum"] = minimum,
            ["Maximum"] = maximum,
        };
        StatisticsBreakdown breakdown = StatisticsCalculation.Breakdown(
            id,
            faces,
            null,
            skill,
            context,
            intermediate,
            value,
            cappedAtMinimum ? minimum : cappedAtMaximum ? maximum : null,
            cappedAtMinimum || cappedAtMaximum);
        return new CalculatedStatistic(value, breakdown);
    }
}
