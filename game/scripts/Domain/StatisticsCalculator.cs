#nullable enable
using System;

namespace WorldofGoses.Domain;

/// <summary>Facade over the four v0.1 derived-stat calculator families.</summary>
public sealed class StatisticsCalculator
{
    public StatisticsCalculator(StatisticsBalanceConfig? balance = null)
    {
        Balance = balance ?? StatisticsBalanceConfig.Default;
        Balance.Validate();
        Offense = new OffensiveStatisticsCalculator(Balance);
        Defense = new DefensiveStatisticsCalculator(Balance);
        Recovery = new RecoveryStatisticsCalculator(Balance);
        Tempo = new TempoStatisticsCalculator(Balance);
    }

    public StatisticsBalanceConfig Balance { get; }
    public OffensiveStatisticsCalculator Offense { get; }
    public DefensiveStatisticsCalculator Defense { get; }
    public RecoveryStatisticsCalculator Recovery { get; }
    public TempoStatisticsCalculator Tempo { get; }

    public DerivedStatistics Calculate(
        FounderCubeProfile cube,
        EquipmentLoadout loadout,
        StatCalculationContext context)
    {
        ArgumentNullException.ThrowIfNull(cube);
        ArgumentNullException.ThrowIfNull(loadout);
        ArgumentNullException.ThrowIfNull(context);
        return new DerivedStatistics(
            Offense.Calculate(cube, loadout, context),
            Defense.Calculate(cube, loadout, context),
            Recovery.Calculate(cube, loadout, context),
            Tempo.Calculate(cube, loadout, context));
    }
}
