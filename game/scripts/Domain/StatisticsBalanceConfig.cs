using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Injectable v0.1 balance coefficients for every derived-stat formula.
/// Calculators contain no tuning literals outside this configuration.
/// </summary>
public sealed record StatisticsBalanceConfig
{
    public static StatisticsBalanceConfig Default { get; } = new();

    public int MinimumSkillLevel { get; init; } = 0;
    public int MaximumSkillLevel { get; init; } = 20;
    public double BaseSkillFactor { get; init; } = 1.0;
    public double SkillFactorPerLevel { get; init; } = 0.025;
    public double NaturalWeaponExperienceFactor { get; init; } = 1.00;
    public double ForeignWeaponExperienceFactor { get; init; } = 0.10;

    public double MinimumWeaponChannel { get; init; } = 0.75;
    public double MaximumWeaponChannel { get; init; } = 1.20;
    public double MaximumGearSupportPerFace { get; init; } = 12.0;
    public double MinimumConditionFactor { get; init; } = 0.50;
    public double MaximumConditionFactor { get; init; } = 1.05;
    public double NeutralConditionFactor { get; init; } = 1.00;
    public double MinimumCitySupportFactor { get; init; } = 0.90;
    public double MaximumCitySupportFactor { get; init; } = 1.10;
    public double NeutralCitySupportFactor { get; init; } = 1.00;

    public double MinimumChannelPower { get; init; } = 0.0;
    public double MaximumChannelPower { get; init; } = 160.0;

    public double BaseMaxHealth { get; init; } = 100.0;
    public double BodyHealthCoefficient { get; init; } = 1.5;
    public double StabilityHealthCoefficient { get; init; } = 1.0;
    public double DefenseStabilityWeight { get; init; } = 0.55;
    public double DefenseSecondaryFaceWeight { get; init; } = 0.45;
    public double SpecificMitigationDenominator { get; init; } = 60.0;
    public double MaximumSpecificMitigation { get; init; } = 0.70;
    public double GeneralReductionCoefficient { get; init; } = 0.20;
    public double GeneralReductionDenominator { get; init; } = 100.0;
    public double MaximumGeneralDamageReduction { get; init; } = 0.20;

    public double RegenerationCoefficient { get; init; } = 0.12;
    public double HealingCoefficient { get; init; } = 0.50;
    public double BaseHealingAppliedPercent { get; init; } = 100.0;

    public double SmoothstepScoreMinimum { get; init; } = 30.0;
    public double SmoothstepScoreRange { get; init; } = 60.0;
    public double SmoothstepQuadraticCoefficient { get; init; } = 3.0;
    public double SmoothstepCubicCoefficient { get; init; } = 2.0;
    public double AttackSpeedMinimum { get; init; } = 0.80;
    public double AttackSpeedMaximum { get; init; } = 1.40;
    public double CastSpeedMinimum { get; init; } = 0.80;
    public double CastSpeedMaximum { get; init; } = 1.40;
    public double CooldownReductionMinimum { get; init; } = 0.00;
    public double CooldownReductionMaximum { get; init; } = 0.40;
    public double CriticalChanceMinimum { get; init; } = 0.05;
    public double CriticalChanceMaximum { get; init; } = 0.35;
    public double PhysicalEvasionMinimum { get; init; } = 0.00;
    public double PhysicalEvasionMaximum { get; init; } = 0.30;
    public double ElementalEvasionMinimum { get; init; } = 0.00;
    public double ElementalEvasionMaximum { get; init; } = 0.30;
    public double MovementSpeedMinimum { get; init; } = 0.80;
    public double MovementSpeedMaximum { get; init; } = 1.30;

    public void Validate()
    {
        if (MinimumSkillLevel < 0 || MaximumSkillLevel < MinimumSkillLevel)
            throw new InvalidOperationException("Skill level bounds are invalid.");
        ValidatePositive(SkillFactorPerLevel, nameof(SkillFactorPerLevel), allowZero: true);
        ValidatePositive(BaseSkillFactor, nameof(BaseSkillFactor));
        ValidateFactor(NaturalWeaponExperienceFactor, nameof(NaturalWeaponExperienceFactor));
        ValidateFactor(ForeignWeaponExperienceFactor, nameof(ForeignWeaponExperienceFactor));
        ValidateOrdered(MinimumWeaponChannel, MaximumWeaponChannel, nameof(MinimumWeaponChannel));
        ValidatePositive(MinimumWeaponChannel, nameof(MinimumWeaponChannel));
        ValidatePositive(MaximumGearSupportPerFace, nameof(MaximumGearSupportPerFace), allowZero: true);
        ValidateOrdered(MinimumConditionFactor, MaximumConditionFactor, nameof(MinimumConditionFactor));
        ValidateWithin(NeutralConditionFactor, MinimumConditionFactor, MaximumConditionFactor, nameof(NeutralConditionFactor));
        ValidateOrdered(MinimumCitySupportFactor, MaximumCitySupportFactor, nameof(MinimumCitySupportFactor));
        ValidateWithin(NeutralCitySupportFactor, MinimumCitySupportFactor, MaximumCitySupportFactor, nameof(NeutralCitySupportFactor));
        ValidateOrdered(MinimumChannelPower, MaximumChannelPower, nameof(MinimumChannelPower));
        ValidatePositive(SpecificMitigationDenominator, nameof(SpecificMitigationDenominator));
        ValidatePositive(GeneralReductionDenominator, nameof(GeneralReductionDenominator));
        ValidatePositive(SmoothstepScoreRange, nameof(SmoothstepScoreRange));
        ValidatePositive(SmoothstepQuadraticCoefficient, nameof(SmoothstepQuadraticCoefficient));
        ValidatePositive(SmoothstepCubicCoefficient, nameof(SmoothstepCubicCoefficient));
        ValidateProbability(MaximumSpecificMitigation, nameof(MaximumSpecificMitigation));
        ValidateProbability(MaximumGeneralDamageReduction, nameof(MaximumGeneralDamageReduction));
        ValidatePositive(BaseMaxHealth, nameof(BaseMaxHealth));
        ValidatePositive(BodyHealthCoefficient, nameof(BodyHealthCoefficient), allowZero: true);
        ValidatePositive(StabilityHealthCoefficient, nameof(StabilityHealthCoefficient), allowZero: true);
        ValidatePositive(DefenseStabilityWeight, nameof(DefenseStabilityWeight), allowZero: true);
        ValidatePositive(DefenseSecondaryFaceWeight, nameof(DefenseSecondaryFaceWeight), allowZero: true);
        if (Math.Abs(DefenseStabilityWeight + DefenseSecondaryFaceWeight - 1) > 0.0000001)
            throw new InvalidOperationException("Defense face weights must sum to 1.");
        ValidatePositive(GeneralReductionCoefficient, nameof(GeneralReductionCoefficient), allowZero: true);
        ValidatePositive(RegenerationCoefficient, nameof(RegenerationCoefficient), allowZero: true);
        ValidatePositive(HealingCoefficient, nameof(HealingCoefficient), allowZero: true);
        ValidatePositive(BaseHealingAppliedPercent, nameof(BaseHealingAppliedPercent), allowZero: true);
        if (!double.IsFinite(SmoothstepScoreMinimum))
            throw new InvalidOperationException("SmoothstepScoreMinimum must be finite.");
        ValidateOrdered(AttackSpeedMinimum, AttackSpeedMaximum, nameof(AttackSpeedMinimum));
        ValidateOrdered(CastSpeedMinimum, CastSpeedMaximum, nameof(CastSpeedMinimum));
        ValidateOrdered(CooldownReductionMinimum, CooldownReductionMaximum, nameof(CooldownReductionMinimum));
        ValidateOrdered(CriticalChanceMinimum, CriticalChanceMaximum, nameof(CriticalChanceMinimum));
        ValidateOrdered(PhysicalEvasionMinimum, PhysicalEvasionMaximum, nameof(PhysicalEvasionMinimum));
        ValidateOrdered(ElementalEvasionMinimum, ElementalEvasionMaximum, nameof(ElementalEvasionMinimum));
        ValidateOrdered(MovementSpeedMinimum, MovementSpeedMaximum, nameof(MovementSpeedMinimum));
        ValidateProbability(CooldownReductionMaximum, nameof(CooldownReductionMaximum));
        ValidateProbability(CriticalChanceMaximum, nameof(CriticalChanceMaximum));
        ValidateProbability(PhysicalEvasionMaximum, nameof(PhysicalEvasionMaximum));
        ValidateProbability(ElementalEvasionMaximum, nameof(ElementalEvasionMaximum));
    }

    public double SkillFactor(int level)
    {
        if (level < MinimumSkillLevel || level > MaximumSkillLevel)
            throw new ArgumentOutOfRangeException(nameof(level), level, $"Skill level must be in [{MinimumSkillLevel}, {MaximumSkillLevel}].");
        return BaseSkillFactor + SkillFactorPerLevel * level;
    }

    public void ValidateWeaponChannel(double value, string parameterName)
    {
        if (!double.IsFinite(value)
            || value < MinimumWeaponChannel
            || value > MaximumWeaponChannel)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Weapon channels must be in [{MinimumWeaponChannel}, {MaximumWeaponChannel}].");
        }
    }

    private static void ValidateFactor(double value, string name) => ValidatePositive(value, name, allowZero: true);

    private static void ValidateProbability(double value, string name)
    {
        if (!double.IsFinite(value) || value is < 0 or > 1)
            throw new InvalidOperationException($"{name} must be in [0, 1].");
    }

    private static void ValidatePositive(double value, string name, bool allowZero = false)
    {
        if (!double.IsFinite(value) || (allowZero ? value < 0 : value <= 0))
            throw new InvalidOperationException($"{name} must be {(allowZero ? "non-negative" : "positive")}.");
    }

    private static void ValidateOrdered(double minimum, double maximum, string name)
    {
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || maximum < minimum)
            throw new InvalidOperationException($"The range beginning at {name} is invalid.");
    }

    private static void ValidateWithin(double value, double minimum, double maximum, string name)
    {
        if (!double.IsFinite(value) || value < minimum || value > maximum)
            throw new InvalidOperationException($"{name} must be within its configured range.");
    }
}
