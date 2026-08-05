using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

public sealed class RecoveryStatisticsCalculator
{
    private readonly StatisticsBalanceConfig _balance;

    public RecoveryStatisticsCalculator(StatisticsBalanceConfig balance)
    {
        ArgumentNullException.ThrowIfNull(balance);
        balance.Validate();
        _balance = balance;
    }

    public RecoveryStatistics Calculate(
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
        double factors = skill * context.ConditionFactor * context.CitySupportFactor;

        CubeFaceCalculation body = StatisticsCalculation.Face(CubeFace.Body, cube, gear, effective);
        CubeFaceCalculation stability = StatisticsCalculation.Face(CubeFace.Stability, cube, gear, effective);
        double regenerationGeometricMean = Math.Sqrt(effective.Body * effective.Stability);
        double regenerationValue = _balance.RegenerationCoefficient * regenerationGeometricMean * factors;
        CalculatedStatistic regeneration = Stat(
            "HealthRegenerationPerMinute",
            new[] { body, stability },
            skill,
            context,
            new Dictionary<string, double> { ["GeometricMean"] = regenerationGeometricMean },
            regenerationValue);

        CubeFaceCalculation bond = StatisticsCalculation.Face(CubeFace.Bond, cube, gear, effective);
        CubeFaceCalculation domain = StatisticsCalculation.Face(CubeFace.Domain, cube, gear, effective);
        double healingGeometricMean = Math.Sqrt(effective.Bond * effective.Domain);
        double healingBonus = _balance.HealingCoefficient * healingGeometricMean * factors;
        double healingApplied = _balance.BaseHealingAppliedPercent + healingBonus;
        CalculatedStatistic healing = Stat(
            "HealingAppliedPercent",
            new[] { bond, domain },
            skill,
            context,
            new Dictionary<string, double>
            {
                ["GeometricMean"] = healingGeometricMean,
                ["HealingBonusPercent"] = healingBonus,
                ["BaseHealingAppliedPercent"] = _balance.BaseHealingAppliedPercent,
            },
            healingApplied);

        return new RecoveryStatistics(regeneration, healing);
    }

    private static CalculatedStatistic Stat(
        string id,
        IReadOnlyList<CubeFaceCalculation> faces,
        double skill,
        StatCalculationContext context,
        IReadOnlyDictionary<string, double> intermediate,
        double value)
    {
        StatisticsBreakdown breakdown = StatisticsCalculation.Breakdown(
            id,
            faces,
            null,
            skill,
            context,
            intermediate,
            value);
        return new CalculatedStatistic(value, breakdown);
    }
}
