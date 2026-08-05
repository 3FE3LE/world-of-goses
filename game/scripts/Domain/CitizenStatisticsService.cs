#nullable enable
using System;

namespace WorldofGoses.Domain;

/// <summary>
/// On-demand application seam for derived citizen statistics. It performs no
/// caching and therefore cannot drift from equipment, competency or condition.
/// </summary>
public sealed class CitizenStatisticsService
{
    private readonly StatisticsCalculator _calculator;

    public CitizenStatisticsService(StatisticsBalanceConfig? balance = null)
    {
        _calculator = new StatisticsCalculator(balance);
    }

    public DerivedStatistics Calculate(Citizen citizen, double citySupportFactor)
    {
        ArgumentNullException.ThrowIfNull(citizen);
        WeaponChannelProfile weapon = citizen.EquipmentLoadout.Weapon
            ?? throw new InvalidOperationException("Derived offensive statistics require an equipped weapon.");
        StatCalculationContext context = ContextFor(citizen, citySupportFactor, weapon.Family);
        return _calculator.Calculate(citizen.CubeProfile, citizen.EquipmentLoadout, context);
    }

    public DefensiveStatistics CalculateDefense(Citizen citizen, double citySupportFactor) =>
        _calculator.Defense.Calculate(
            citizen.CubeProfile,
            citizen.EquipmentLoadout,
            ContextFor(citizen, citySupportFactor, EquippedFamily(citizen)));

    public RecoveryStatistics CalculateRecovery(Citizen citizen, double citySupportFactor) =>
        _calculator.Recovery.Calculate(
            citizen.CubeProfile,
            citizen.EquipmentLoadout,
            ContextFor(citizen, citySupportFactor, EquippedFamily(citizen)));

    public TempoStatistics CalculateTempo(Citizen citizen, double citySupportFactor) =>
        _calculator.Tempo.Calculate(
            citizen.CubeProfile,
            citizen.EquipmentLoadout,
            ContextFor(citizen, citySupportFactor, EquippedFamily(citizen)));

    private StatCalculationContext ContextFor(
        Citizen citizen,
        double citySupportFactor,
        WeaponFamily? applicableFamily)
    {
        ArgumentNullException.ThrowIfNull(citizen);
        int level = applicableFamily.HasValue
            ? citizen.WeaponSkillLevel(applicableFamily.Value)
            : _calculator.Balance.MinimumSkillLevel;
        return new StatCalculationContext(
            level,
            citizen.CurrentHealthAndCondition.RequireConditionFactor(),
            citySupportFactor,
            _calculator.Balance);
    }

    private static WeaponFamily? EquippedFamily(Citizen citizen) =>
        citizen.EquipmentLoadout.Weapon?.Family;
}
