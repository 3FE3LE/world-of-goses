#nullable enable
using System;

namespace WorldofGoses.Domain;

public sealed record CurrentHealthAndCondition
{
    private CurrentHealthAndCondition()
    {
    }

    /// <summary>
    /// Legacy saves with wounds cannot infer condition without inventing the
    /// still-pending wound-to-condition rule. Application code must resolve it.
    /// </summary>
    public static CurrentHealthAndCondition Unresolved { get; } = new();

    public CurrentHealthAndCondition(
        double currentHealth,
        double conditionFactor,
        StatisticsBalanceConfig? balance = null)
    {
        StatisticsBalanceConfig config = balance ?? StatisticsBalanceConfig.Default;
        config.Validate();
        if (!double.IsFinite(currentHealth) || currentHealth < 0)
            throw new ArgumentOutOfRangeException(nameof(currentHealth));
        if (!double.IsFinite(conditionFactor)
            || conditionFactor < config.MinimumConditionFactor
            || conditionFactor > config.MaximumConditionFactor)
        {
            throw new ArgumentOutOfRangeException(nameof(conditionFactor));
        }
        CurrentHealth = currentHealth;
        ConditionFactor = conditionFactor;
    }

    public double? CurrentHealth { get; }
    public double? ConditionFactor { get; }
    public bool IsResolved => CurrentHealth.HasValue && ConditionFactor.HasValue;

    public double RequireConditionFactor() => ConditionFactor
        ?? throw new InvalidOperationException(
            "Health and condition must be resolved by the application before calculating statistics.");
}
