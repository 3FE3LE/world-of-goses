namespace WorldofGoses.Domain;

public sealed record DerivedStatistics(
    OffensiveStatistics Offense,
    DefensiveStatistics Defense,
    RecoveryStatistics Recovery,
    TempoStatistics Tempo);
