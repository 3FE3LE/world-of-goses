namespace WorldofGoses.Domain;

public sealed record TempoStatistics(
    CalculatedStatistic AttackSpeed,
    CalculatedStatistic CastSpeed,
    CalculatedStatistic CooldownReduction,
    CalculatedStatistic CriticalChance,
    CalculatedStatistic PhysicalEvasion,
    CalculatedStatistic ElementalEvasion,
    CalculatedStatistic MovementSpeed,

    /// <summary>How well this citizen makes a physical expression stick.</summary>
    CalculatedStatistic ControlPower,

    /// <summary>How well this citizen shrugs one off.</summary>
    CalculatedStatistic ControlResistance);
