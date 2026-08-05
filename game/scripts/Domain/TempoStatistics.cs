namespace WorldofGoses.Domain;

public sealed record TempoStatistics(
    CalculatedStatistic AttackSpeed,
    CalculatedStatistic CastSpeed,
    CalculatedStatistic CooldownReduction,
    CalculatedStatistic CriticalChance,
    CalculatedStatistic PhysicalEvasion,
    CalculatedStatistic ElementalEvasion,
    CalculatedStatistic MovementSpeed);
