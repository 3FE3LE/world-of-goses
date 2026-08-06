#nullable enable
using System;

namespace WorldofGoses.Domain.Combat;

/// <summary>
/// Injectable tuning for the combat and competency slice. Every number the
/// combat domain uses lives here, mirroring how
/// <see cref="StatisticsBalanceConfig"/> keeps the derived-stat calculators free
/// of literals. Nothing in this file is final balance.
///
/// <para>
/// PROVISIONAL BALANCE. The roadmap fixes no numeric values for the experience
/// curve, technique budgets, status durations or enemy tuning, so these are
/// deliberate first proposals: moderate, centralised and covered by tests.
/// Changing a value here must not require touching a resolver.
/// </para>
/// </summary>
public sealed record CombatBalanceConfig
{
    public static CombatBalanceConfig Default { get; } = new();

    // ---- Experience curve -------------------------------------------------

    /// <summary>Cumulative experience required to reach level 1.</summary>
    public double BaseExperiencePerLevel { get; init; } = 100.0;

    /// <summary>
    /// Super-linear growth so early levels arrive quickly and level 20 is a long
    /// commitment. Cumulative requirement is Base * level^Exponent.
    /// </summary>
    public double ExperienceGrowthExponent { get; init; } = 1.55;

    /// <summary>Experience a participating combatant generates per resolved technique.</summary>
    public double ExperiencePerResolvedTechnique { get; init; } = 12.0;

    /// <summary>Experience granted to every survivor for completing an encounter.</summary>
    public double ExperiencePerEncounterCleared { get; init; } = 40.0;

    /// <summary>Survival experience granted per expedition segment travelled.</summary>
    public double SurvivalExperiencePerSegment { get; init; } = 15.0;

    // ---- Technique budget -------------------------------------------------

    /// <summary>
    /// A technique's physical and elemental coefficients must sum to this budget.
    /// It is what makes an evolution a redistribution rather than a free upgrade.
    /// </summary>
    public double TechniqueCoefficientBudget { get; init; } = 1.00;

    /// <summary>Tolerance when validating a coefficient pair against the budget.</summary>
    public double TechniqueBudgetTolerance { get; init; } = 1e-9;

    /// <summary>Multiplier applied to a critical technique result.</summary>
    public double CriticalMultiplier { get; init; } = 1.50;

    // ---- Status effects ---------------------------------------------------

    /// <summary>Steps a Stunning application lasts before expiring.</summary>
    public int StunningDurationSteps { get; init; } = 2;

    /// <summary>Stacks of Stunning required before an action is actually interrupted.</summary>
    public int StunningInterruptThreshold { get; init; } = 1;

    /// <summary>Steps a Knockdown application lasts before expiring.</summary>
    public int KnockdownDurationSteps { get; init; } = 2;

    /// <summary>Stacks of Knockdown required before the target loses its turn.</summary>
    public int KnockdownThreshold { get; init; } = 1;

    /// <summary>
    /// While knocked down a combatant is easier to hit: its mitigation is scaled
    /// by this factor. Knockdown alters availability and exposure, not position,
    /// because this combat model has no free movement.
    /// </summary>
    public double KnockdownMitigationScale { get; init; } = 0.5;

    // ---- Encounter --------------------------------------------------------

    /// <summary>Hard stop so a mutually unkillable board cannot loop forever.</summary>
    public int MaximumEncounterSteps { get; init; } = 200;

    /// <summary>Fatigue a combatant accumulates per step it acts.</summary>
    public double FatiguePerAction { get; init; } = 1.5;

    /// <summary>Fatigue accumulated by every member per expedition segment.</summary>
    public double FatiguePerSegment { get; init; } = 8.0;

    /// <summary>Fatigue at which ConditionFactor reaches its floor.</summary>
    public double FatigueForMinimumCondition { get; init; } = 100.0;

    /// <summary>Health ratio at or below which the retreat rule may trigger.</summary>
    public double RetreatHealthRatio { get; init; } = 0.30;

    public void Validate()
    {
        if (BaseExperiencePerLevel <= 0)
            throw new InvalidOperationException("Base experience per level must be positive.");
        if (ExperienceGrowthExponent <= 0)
            throw new InvalidOperationException("Experience growth exponent must be positive.");
        if (TechniqueCoefficientBudget <= 0)
            throw new InvalidOperationException("Technique coefficient budget must be positive.");
        if (CriticalMultiplier < 1)
            throw new InvalidOperationException("Critical multiplier must be at least 1.");
        if (StunningDurationSteps <= 0 || KnockdownDurationSteps <= 0)
            throw new InvalidOperationException("Status durations must be positive.");
        if (StunningInterruptThreshold <= 0 || KnockdownThreshold <= 0)
            throw new InvalidOperationException("Status thresholds must be positive.");
        if (KnockdownMitigationScale is < 0 or > 1)
            throw new InvalidOperationException("Knockdown mitigation scale must be within [0, 1].");
        if (MaximumEncounterSteps <= 0)
            throw new InvalidOperationException("Maximum encounter steps must be positive.");
        if (FatigueForMinimumCondition <= 0)
            throw new InvalidOperationException("Fatigue for minimum condition must be positive.");
        if (RetreatHealthRatio is < 0 or > 1)
            throw new InvalidOperationException("Retreat health ratio must be within [0, 1].");
    }
}
