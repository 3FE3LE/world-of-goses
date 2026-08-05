using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

public sealed class OffensiveStatisticsCalculator
{
    private readonly StatisticsBalanceConfig _balance;

    public OffensiveStatisticsCalculator(StatisticsBalanceConfig balance)
    {
        ArgumentNullException.ThrowIfNull(balance);
        balance.Validate();
        _balance = balance;
    }

    public OffensiveStatistics Calculate(
        FounderCubeProfile cube,
        EquipmentLoadout loadout,
        StatCalculationContext context)
    {
        ArgumentNullException.ThrowIfNull(cube);
        ArgumentNullException.ThrowIfNull(loadout);
        ArgumentNullException.ThrowIfNull(context);
        StatisticsCalculation.ValidateInputs(loadout, context, _balance);
        WeaponChannelProfile weapon = loadout.Weapon
            ?? throw new InvalidOperationException("Offensive channel power requires an equipped weapon.");
        GearSupportProfile gear = loadout.TotalGearSupport;
        EffectiveCubeProfile effective = EffectiveCubeProfile.From(cube, gear);
        double skillFactor = StatisticsCalculation.SkillFactor(context.ApplicableSkillLevel, _balance);

        CalculatedStatistic physical = Channel(
            "PhysicalChannelPower",
            CubeFace.Body,
            cube,
            gear,
            effective,
            weapon.PhysicalTransfer,
            skillFactor,
            context);
        CalculatedStatistic elemental = Channel(
            "ElementalChannelPower",
            CubeFace.Bond,
            cube,
            gear,
            effective,
            weapon.ElementalResonance,
            skillFactor,
            context);
        return new OffensiveStatistics(physical, elemental);
    }

    private CalculatedStatistic Channel(
        string id,
        CubeFace face,
        FounderCubeProfile cube,
        GearSupportProfile gear,
        EffectiveCubeProfile effective,
        double weaponCoefficient,
        double skillFactor,
        StatCalculationContext context)
    {
        double raw = effective.For(face)
            * weaponCoefficient
            * skillFactor
            * context.ConditionFactor
            * context.CitySupportFactor;
        double value = StatisticsCalculation.Clamp(raw, _balance.MinimumChannelPower, _balance.MaximumChannelPower);
        bool wasCapped = value != raw;
        double? appliedCap = !wasCapped
            ? null
            : raw < _balance.MinimumChannelPower
                ? _balance.MinimumChannelPower
                : _balance.MaximumChannelPower;
        CubeFaceCalculation faceCalculation = StatisticsCalculation.Face(face, cube, gear, effective);
        var intermediate = new Dictionary<string, double>
        {
            ["UncappedChannelPower"] = raw,
        };
        StatisticsBreakdown breakdown = StatisticsCalculation.Breakdown(
            id,
            new[] { faceCalculation },
            weaponCoefficient,
            skillFactor,
            context,
            intermediate,
            value,
            appliedCap,
            wasCapped);
        return new CalculatedStatistic(value, breakdown);
    }
}
