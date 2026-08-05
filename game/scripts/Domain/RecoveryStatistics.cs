namespace WorldofGoses.Domain;

public sealed record RecoveryStatistics(
    CalculatedStatistic HealthRegenerationPerMinute,
    CalculatedStatistic HealingAppliedPercent);
