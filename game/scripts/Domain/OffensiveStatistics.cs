namespace WorldofGoses.Domain;

public sealed record OffensiveStatistics(
    CalculatedStatistic PhysicalChannelPower,
    CalculatedStatistic ElementalChannelPower);
