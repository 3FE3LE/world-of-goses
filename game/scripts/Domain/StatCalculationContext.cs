#nullable enable
using System;

namespace WorldofGoses.Domain;

public sealed record StatCalculationContext
{
    public StatCalculationContext(
        int applicableSkillLevel,
        double conditionFactor,
        double citySupportFactor,
        StatisticsBalanceConfig? balance = null)
    {
        StatisticsBalanceConfig config = balance ?? StatisticsBalanceConfig.Default;
        config.Validate();
        if (applicableSkillLevel < config.MinimumSkillLevel || applicableSkillLevel > config.MaximumSkillLevel)
            throw new ArgumentOutOfRangeException(nameof(applicableSkillLevel));
        if (!double.IsFinite(conditionFactor)
            || conditionFactor < config.MinimumConditionFactor
            || conditionFactor > config.MaximumConditionFactor)
            throw new ArgumentOutOfRangeException(nameof(conditionFactor));
        if (!double.IsFinite(citySupportFactor)
            || citySupportFactor < config.MinimumCitySupportFactor
            || citySupportFactor > config.MaximumCitySupportFactor)
            throw new ArgumentOutOfRangeException(nameof(citySupportFactor));
        ApplicableSkillLevel = applicableSkillLevel;
        ConditionFactor = conditionFactor;
        CitySupportFactor = citySupportFactor;
    }

    public int ApplicableSkillLevel { get; }
    public double ConditionFactor { get; }
    public double CitySupportFactor { get; }
}
