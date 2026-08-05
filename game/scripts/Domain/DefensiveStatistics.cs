namespace WorldofGoses.Domain;

public sealed record DefensiveStatistics(
    CalculatedStatistic MaxHealth,
    CalculatedStatistic PhysicalDefenseScore,
    CalculatedStatistic PhysicalMitigation,
    CalculatedStatistic ElementalDefenseScore,
    CalculatedStatistic ElementalMitigation,
    CalculatedStatistic GeneralDamageReduction);
