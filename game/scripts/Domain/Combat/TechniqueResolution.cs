#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain.Combat;

/// <summary>
/// The complete audit trail of one resolved technique. Every value the roadmap's
/// mandatory telemetry asks for is a field here, produced by the domain — the UI
/// formats this record and never recomputes any part of it.
///
/// <para>
/// Note the vocabulary: channel POWER is a capacity, contributions and
/// <see cref="RawTechniqueResult"/> are the technique's output, and only
/// <see cref="FinalResult"/> is what the target actually loses. No field here is
/// a persistent "physical damage" statistic.
/// </para>
/// </summary>
public sealed record TechniqueResolution(
    int Step,
    string TechniqueId,
    string SourceId,
    string TargetId,
    double PhysicalChannelPower,
    double ElementalChannelPower,
    double PhysicalCoefficient,
    double ElementalCoefficient,
    double PhysicalContribution,
    double ElementalContribution,
    double RawTechniqueResult,
    double PhysicalMitigation,
    double ElementalMitigation,
    double GeneralDamageReduction,
    bool CriticalResult,
    double FinalResult,
    ElementalAffinity ElementalNature,
    IReadOnlyList<StatusEffectId> AppliedStatuses)
{
    /// <summary>Share of the raw result that came from the physical channel.</summary>
    public double PhysicalShare => RawTechniqueResult <= 0
        ? 0
        : PhysicalContribution / RawTechniqueResult;
}

/// <summary>
/// Converts channel power into a concrete technique outcome. This is the only
/// place the roadmap's resolution formula exists.
///
/// <code>
/// PhysicalContribution = PhysicalChannelPower × PhysicalCoefficient
/// ElementalContribution = ElementalChannelPower × ElementalCoefficient
/// RawTechniqueResult   = PhysicalContribution + ElementalContribution
/// </code>
///
/// Critical, mitigation and general reduction are then applied through the
/// existing calculators rather than reimplemented.
/// </summary>
public sealed class TechniqueResolver
{
    private readonly DefensiveStatisticsCalculator _defense;
    private readonly StatusResolver _statuses;
    private readonly CombatBalanceConfig _balance;

    public TechniqueResolver(
        DefensiveStatisticsCalculator defense,
        StatusResolver statuses,
        CombatBalanceConfig? balance = null)
    {
        _defense = defense ?? throw new ArgumentNullException(nameof(defense));
        _statuses = statuses ?? throw new ArgumentNullException(nameof(statuses));
        _balance = balance ?? CombatBalanceConfig.Default;
        _balance.Validate();
    }

    /// <summary>
    /// Resolves <paramref name="technique"/> from <paramref name="source"/> against
    /// <paramref name="target"/>. The technique's split decides how much of each
    /// channel is used; the target's own mitigations decide how much survives.
    /// </summary>
    public TechniqueResolution Resolve(
        int step,
        TechniqueDefinition technique,
        CombatantState source,
        CombatantState target,
        IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(technique);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(random);

        double physicalPower = source.PhysicalChannelPower;
        double elementalPower = source.ElementalChannelPower;
        double physicalContribution = physicalPower * technique.PhysicalCoefficient;
        double elementalContribution = elementalPower * technique.ElementalCoefficient;
        double raw = physicalContribution + elementalContribution;

        // Critical is a roll against the source's own derived chance.
        bool critical = random.NextDouble() < source.CriticalChance;
        double afterCritical = critical ? raw * _balance.CriticalMultiplier : raw;

        // A technique whose output is mostly physical is resisted by physical
        // mitigation, and vice versa. Blending by share keeps a hybrid technique
        // from picking whichever mitigation happens to be lower.
        double physicalShare = raw <= 0 ? 0 : physicalContribution / raw;
        double exposure = _statuses.MitigationScale(target.Statuses);
        double physicalMitigation = target.PhysicalMitigation * exposure;
        double elementalMitigation = target.ElementalMitigation * exposure;
        double blendedMitigation =
            physicalMitigation * physicalShare + elementalMitigation * (1 - physicalShare);

        double final = afterCritical
            * (1 - target.GeneralDamageReduction)
            * (1 - blendedMitigation);
        final = Math.Max(0, final);

        var applied = new List<StatusEffectId>();
        if (technique.AppliesStatus is StatusEffectId status) applied.Add(status);

        return new TechniqueResolution(
            step,
            technique.Id,
            source.Id,
            target.Id,
            physicalPower,
            elementalPower,
            technique.PhysicalCoefficient,
            technique.ElementalCoefficient,
            physicalContribution,
            elementalContribution,
            raw,
            physicalMitigation,
            elementalMitigation,
            target.GeneralDamageReduction,
            critical,
            final,
            source.ElementalAffinity,
            applied);
    }

    /// <summary>
    /// Exposes the existing damage-taken calculator so a caller that holds full
    /// <see cref="CalculatedStatistic"/> instances can obtain the same auditable
    /// breakdown the statistics system produces elsewhere.
    /// </summary>
    public CalculatedStatistic ExplainDamageTaken(
        double rawResult,
        CalculatedStatistic generalDamageReduction,
        CalculatedStatistic specificMitigation) =>
        _defense.CalculateDamageTaken(rawResult, generalDamageReduction, specificMitigation);
}
