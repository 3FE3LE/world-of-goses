using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

internal static class StatisticsCalculation
{
    public static void ValidateInputs(
        EquipmentLoadout loadout,
        StatCalculationContext context,
        StatisticsBalanceConfig config)
    {
        config.Validate();
        if (context.ApplicableSkillLevel < config.MinimumSkillLevel
            || context.ApplicableSkillLevel > config.MaximumSkillLevel)
            throw new ArgumentOutOfRangeException(nameof(context), "Skill level is outside the calculator configuration.");
        if (context.ConditionFactor < config.MinimumConditionFactor
            || context.ConditionFactor > config.MaximumConditionFactor)
            throw new ArgumentOutOfRangeException(nameof(context), "Condition factor is outside the calculator configuration.");
        if (context.CitySupportFactor < config.MinimumCitySupportFactor
            || context.CitySupportFactor > config.MaximumCitySupportFactor)
            throw new ArgumentOutOfRangeException(nameof(context), "City support factor is outside the calculator configuration.");
        foreach (CubeFace face in Enum.GetValues<CubeFace>())
        {
            if (loadout.TotalGearSupport.For(face) > config.MaximumGearSupportPerFace)
                throw new ArgumentOutOfRangeException(nameof(loadout), $"{face} support exceeds the calculator configuration.");
        }
        if (loadout.Weapon is { } weapon)
        {
            config.ValidateWeaponChannel(weapon.PhysicalTransfer, nameof(weapon.PhysicalTransfer));
            config.ValidateWeaponChannel(weapon.ElementalResonance, nameof(weapon.ElementalResonance));
        }
    }

    public static double SkillFactor(int level, StatisticsBalanceConfig config) =>
        config.SkillFactor(level);

    public static double Clamp(double value, double minimum, double maximum) =>
        Math.Clamp(value, minimum, maximum);

    public static double Smoothstep(double score, double minimum, double maximum, StatisticsBalanceConfig config)
    {
        double t = Clamp(
            (score - config.SmoothstepScoreMinimum) / config.SmoothstepScoreRange,
            0,
            1);
        double curve = config.SmoothstepQuadraticCoefficient * t * t
            - config.SmoothstepCubicCoefficient * t * t * t;
        return Clamp(minimum + (maximum - minimum) * curve, minimum, maximum);
    }

    public static CubeFaceCalculation Face(
        CubeFace face,
        FounderCubeProfile cube,
        GearSupportProfile gear,
        EffectiveCubeProfile effective)
    {
        double baseValue = face switch
        {
            CubeFace.Body => cube.Body,
            CubeFace.Bond => cube.Bond,
            CubeFace.Stability => cube.Stability,
            CubeFace.Impulse => cube.Impulse,
            CubeFace.Domain => cube.Domain,
            CubeFace.Reach => cube.Reach,
            _ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
        };
        return new CubeFaceCalculation(face, baseValue, gear.For(face), effective.For(face));
    }

    public static StatisticsBreakdown Breakdown(
        string id,
        IReadOnlyList<CubeFaceCalculation> faces,
        double? weaponCoefficient,
        double skillFactor,
        StatCalculationContext context,
        IReadOnlyDictionary<string, double> intermediate,
        double finalValue,
        double? cap = null,
        bool wasCapped = false,
        bool usesSkillFactor = true,
        bool usesConditionFactor = true,
        bool usesCitySupportFactor = true)
    {
        double baseValue = 0;
        double gear = 0;
        double effective = 0;
        foreach (CubeFaceCalculation face in faces)
        {
            baseValue += face.BaseCubeValue;
            gear += face.GearSupport;
            effective += face.EffectiveCubeValue;
        }
        if (faces.Count > 1)
        {
            baseValue /= faces.Count;
            gear /= faces.Count;
            effective /= faces.Count;
        }
        return new StatisticsBreakdown(
            id,
            baseValue,
            gear,
            effective,
            weaponCoefficient,
            usesSkillFactor ? skillFactor : null,
            usesConditionFactor ? context.ConditionFactor : null,
            usesCitySupportFactor ? context.CitySupportFactor : null,
            faces,
            intermediate,
            finalValue,
            cap,
            wasCapped);
    }
}
