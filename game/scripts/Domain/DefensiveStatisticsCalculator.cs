using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

public sealed class DefensiveStatisticsCalculator
{
    private readonly StatisticsBalanceConfig _balance;

    public DefensiveStatisticsCalculator(StatisticsBalanceConfig balance)
    {
        ArgumentNullException.ThrowIfNull(balance);
        balance.Validate();
        _balance = balance;
    }

    public DefensiveStatistics Calculate(
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
        CubeFaceCalculation body = StatisticsCalculation.Face(CubeFace.Body, cube, gear, effective);
        CubeFaceCalculation bond = StatisticsCalculation.Face(CubeFace.Bond, cube, gear, effective);
        CubeFaceCalculation stability = StatisticsCalculation.Face(CubeFace.Stability, cube, gear, effective);

        double maxHealthValue = _balance.BaseMaxHealth
            + _balance.BodyHealthCoefficient * effective.Body
            + _balance.StabilityHealthCoefficient * effective.Stability;
        var healthIntermediate = new Dictionary<string, double>
        {
            ["BaseHealth"] = _balance.BaseMaxHealth,
            ["BodyContribution"] = _balance.BodyHealthCoefficient * effective.Body,
            ["StabilityContribution"] = _balance.StabilityHealthCoefficient * effective.Stability,
        };
        CalculatedStatistic maxHealth = Stat(
            "MaxHealth",
            new[] { body, stability },
            skill,
            context,
            healthIntermediate,
            maxHealthValue,
            usesContextFactors: false);

        double physicalBase = _balance.DefenseStabilityWeight * effective.Stability
            + _balance.DefenseSecondaryFaceWeight * effective.Body;
        CalculatedStatistic physicalDefense = DefenseScore(
            "PhysicalDefenseScore",
            new[] { stability, body },
            physicalBase,
            skill,
            context);
        CalculatedStatistic physicalMitigation = Mitigation(
            "PhysicalMitigation",
            new[] { stability, body },
            physicalDefense.Value,
            skill,
            context);

        double elementalBase = _balance.DefenseStabilityWeight * effective.Stability
            + _balance.DefenseSecondaryFaceWeight * effective.Bond;
        CalculatedStatistic elementalDefense = DefenseScore(
            "ElementalDefenseScore",
            new[] { stability, bond },
            elementalBase,
            skill,
            context);
        CalculatedStatistic elementalMitigation = Mitigation(
            "ElementalMitigation",
            new[] { stability, bond },
            elementalDefense.Value,
            skill,
            context);

        double uncappedReduction = _balance.GeneralReductionCoefficient
            * effective.Stability
            / (effective.Stability + _balance.GeneralReductionDenominator);
        double reduction = Math.Min(_balance.MaximumGeneralDamageReduction, uncappedReduction);
        var reductionIntermediate = new Dictionary<string, double>
        {
            ["UncappedGeneralReduction"] = uncappedReduction,
        };
        CalculatedStatistic generalReduction = Stat(
            "GeneralDamageReduction",
            new[] { stability },
            skill,
            context,
            reductionIntermediate,
            reduction,
            reduction != uncappedReduction ? _balance.MaximumGeneralDamageReduction : null,
            reduction != uncappedReduction,
            usesContextFactors: false);

        return new DefensiveStatistics(
            maxHealth,
            physicalDefense,
            physicalMitigation,
            elementalDefense,
            elementalMitigation,
            generalReduction);
    }

    public CalculatedStatistic CalculateDamageTaken(
        double rawDamage,
        CalculatedStatistic generalDamageReduction,
        CalculatedStatistic specificMitigation)
    {
        if (!double.IsFinite(rawDamage) || rawDamage < 0)
            throw new ArgumentOutOfRangeException(nameof(rawDamage));
        ArgumentNullException.ThrowIfNull(generalDamageReduction);
        ArgumentNullException.ThrowIfNull(specificMitigation);
        double value = rawDamage
            * (1 - generalDamageReduction.Value)
            * (1 - specificMitigation.Value);
        var intermediate = new Dictionary<string, double>
        {
            ["RawDamage"] = rawDamage,
            ["GeneralDamageReduction"] = generalDamageReduction.Value,
            ["SpecificMitigation"] = specificMitigation.Value,
        };
        var context = new StatCalculationContext(
            _balance.MinimumSkillLevel,
            _balance.NeutralConditionFactor,
            _balance.NeutralCitySupportFactor,
            _balance);
        StatisticsBreakdown breakdown = StatisticsCalculation.Breakdown(
            "DamageTaken",
            Array.Empty<CubeFaceCalculation>(),
            null,
            1,
            context,
            intermediate,
            value,
            usesSkillFactor: false,
            usesConditionFactor: false,
            usesCitySupportFactor: false);
        return new CalculatedStatistic(value, breakdown);
    }

    private CalculatedStatistic DefenseScore(
        string id,
        IReadOnlyList<CubeFaceCalculation> faces,
        double baseScore,
        double skill,
        StatCalculationContext context)
    {
        double value = baseScore * skill * context.ConditionFactor * context.CitySupportFactor;
        var intermediate = new Dictionary<string, double>
        {
            ["WeightedBaseScore"] = baseScore,
        };
        return Stat(id, faces, skill, context, intermediate, value);
    }

    private CalculatedStatistic Mitigation(
        string id,
        IReadOnlyList<CubeFaceCalculation> faces,
        double defenseScore,
        double skill,
        StatCalculationContext context)
    {
        double uncapped = defenseScore / (defenseScore + _balance.SpecificMitigationDenominator);
        double value = Math.Min(_balance.MaximumSpecificMitigation, uncapped);
        var intermediate = new Dictionary<string, double>
        {
            ["DefenseScore"] = defenseScore,
            ["UncappedMitigation"] = uncapped,
        };
        return Stat(
            id,
            faces,
            skill,
            context,
            intermediate,
            value,
            value != uncapped ? _balance.MaximumSpecificMitigation : null,
            value != uncapped);
    }

    private static CalculatedStatistic Stat(
        string id,
        IReadOnlyList<CubeFaceCalculation> faces,
        double skill,
        StatCalculationContext context,
        IReadOnlyDictionary<string, double> intermediate,
        double value,
        double? cap = null,
        bool wasCapped = false,
        bool usesContextFactors = true)
    {
        StatisticsBreakdown breakdown = StatisticsCalculation.Breakdown(
            id,
            faces,
            null,
            skill,
            context,
            intermediate,
            value,
            cap,
            wasCapped,
            usesSkillFactor: usesContextFactors,
            usesConditionFactor: usesContextFactors,
            usesCitySupportFactor: usesContextFactors);
        return new CalculatedStatistic(value, breakdown);
    }
}
