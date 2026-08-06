#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain.Combat;

/// <summary>Where a technique comes from. A citizen combines one tree of each.</summary>
public enum TechniqueSource
{
    Weapon,
    PhysicalExpression,
    ElementalAffinity,
}

public enum TechniqueKind
{
    Active,
    Passive,
}

/// <summary>Who the technique resolves against. No free movement is modelled.</summary>
public enum TechniqueTargetRule
{
    SingleEnemy,
    AllEnemies,
    LowestHealthEnemy,
    Self,
    LowestHealthAlly,
}

/// <summary>Ordering hint when several techniques are ready on the same step.</summary>
public enum TechniquePriorityRule
{
    Opening,
    Sustained,
    Finisher,
}

/// <summary>
/// The player's automatic-use gate. The player configures intent, never the
/// individual activation.
/// </summary>
public enum TechniqueUseCondition
{
    UseWhenReady,
    UseAgainstTwoOrMoreEnemies,
    UseWhenAllyBelowHalfHealth,
    UseToInterrupt,
    ReserveForPrimaryTarget,
}

/// <summary>Milestone at which a technique may evolve. Identity is preserved.</summary>
public enum TechniqueEvolutionMilestone
{
    /// <summary>Level 5: physical, elemental or hybrid orientation.</summary>
    Orientation = 5,

    /// <summary>Level 10: target shape.</summary>
    TargetShape = 10,

    /// <summary>Level 15: rhythm, cost or cooldown.</summary>
    Rhythm = 15,

    /// <summary>Level 20: mastery transformation.</summary>
    Mastery = 20,
}

/// <summary>
/// One evolution branch. A branch may redistribute the coefficient pair, change
/// the target rule, or change the rhythm — it never simply adds power, because
/// the coefficient budget is validated after the branch is applied.
/// </summary>
public sealed record TechniqueEvolution(
    TechniqueEvolutionMilestone Milestone,
    string Id,
    double? PhysicalCoefficient = null,
    double? ElementalCoefficient = null,
    TechniqueTargetRule? TargetRule = null,
    int? Cooldown = null,
    int? ActivationTime = null);

/// <summary>
/// A technique converts channel power into a concrete action through a physical
/// and an elemental coefficient. It is data: no behaviour, no Godot, no assets.
///
/// <para>
/// The coefficient pair must sum to
/// <see cref="CombatBalanceConfig.TechniqueCoefficientBudget"/>. That single
/// invariant is what makes an evolution a redistribution rather than a free
/// upgrade, and it is why raising one coefficient cannot silently preserve the
/// other.
/// </para>
/// </summary>
public sealed record TechniqueDefinition
{
    public TechniqueDefinition(
        string id,
        TechniqueSource source,
        TechniqueKind kind,
        double physicalCoefficient,
        double elementalCoefficient,
        WeaponFamily? requiredWeaponFamily = null,
        PhysicalExpression? requiredExpression = null,
        ElementalAffinity? requiredAffinity = null,
        int cooldown = 0,
        int activationTime = 0,
        TechniqueTargetRule targetRule = TechniqueTargetRule.SingleEnemy,
        TechniquePriorityRule priorityRule = TechniquePriorityRule.Sustained,
        TechniqueUseCondition useCondition = TechniqueUseCondition.UseWhenReady,
        StatusEffectId? appliesStatus = null,
        string animationTag = "",
        IReadOnlyList<TechniqueEvolution>? evolutions = null,
        CombatBalanceConfig? balance = null)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Technique id is required.", nameof(id));
        if (!Enum.IsDefined(source)) throw new ArgumentOutOfRangeException(nameof(source));
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (!Enum.IsDefined(targetRule)) throw new ArgumentOutOfRangeException(nameof(targetRule));
        if (!Enum.IsDefined(priorityRule)) throw new ArgumentOutOfRangeException(nameof(priorityRule));
        if (!Enum.IsDefined(useCondition)) throw new ArgumentOutOfRangeException(nameof(useCondition));
        if (cooldown < 0) throw new ArgumentOutOfRangeException(nameof(cooldown));
        if (activationTime < 0) throw new ArgumentOutOfRangeException(nameof(activationTime));

        CombatBalanceConfig config = balance ?? CombatBalanceConfig.Default;
        config.Validate();
        ValidateBudget(physicalCoefficient, elementalCoefficient, config, id);

        Id = id;
        Source = source;
        Kind = kind;
        PhysicalCoefficient = physicalCoefficient;
        ElementalCoefficient = elementalCoefficient;
        RequiredWeaponFamily = requiredWeaponFamily;
        RequiredExpression = requiredExpression;
        RequiredAffinity = requiredAffinity;
        Cooldown = cooldown;
        ActivationTime = activationTime;
        TargetRule = targetRule;
        PriorityRule = priorityRule;
        UseCondition = useCondition;
        AppliesStatus = appliesStatus;
        AnimationTag = animationTag;
        Evolutions = evolutions ?? Array.Empty<TechniqueEvolution>();
    }

    public string Id { get; }
    public TechniqueSource Source { get; }
    public TechniqueKind Kind { get; }
    public double PhysicalCoefficient { get; }
    public double ElementalCoefficient { get; }
    public WeaponFamily? RequiredWeaponFamily { get; }
    public PhysicalExpression? RequiredExpression { get; }
    public ElementalAffinity? RequiredAffinity { get; }
    public int Cooldown { get; }
    public int ActivationTime { get; }
    public TechniqueTargetRule TargetRule { get; }
    public TechniquePriorityRule PriorityRule { get; }
    public TechniqueUseCondition UseCondition { get; }
    public StatusEffectId? AppliesStatus { get; }
    public string AnimationTag { get; }
    public IReadOnlyList<TechniqueEvolution> Evolutions { get; }

    /// <summary>
    /// The technique as it exists at a competency level: every evolution whose
    /// milestone the level has reached is applied in milestone order. Identity
    /// (<see cref="Id"/>) is preserved, and the budget is re-validated, so a
    /// branch that raises one coefficient must lower the other.
    /// </summary>
    public TechniqueDefinition AtLevel(int level, CombatBalanceConfig? balance = null)
    {
        CombatBalanceConfig config = balance ?? CombatBalanceConfig.Default;
        double physical = PhysicalCoefficient;
        double elemental = ElementalCoefficient;
        TechniqueTargetRule target = TargetRule;
        int cooldown = Cooldown;
        int activation = ActivationTime;

        foreach (TechniqueEvolution evolution in OrderedEvolutions())
        {
            if (level < (int)evolution.Milestone) continue;
            physical = evolution.PhysicalCoefficient ?? physical;
            elemental = evolution.ElementalCoefficient ?? elemental;
            target = evolution.TargetRule ?? target;
            cooldown = evolution.Cooldown ?? cooldown;
            activation = evolution.ActivationTime ?? activation;
        }

        return new TechniqueDefinition(
            Id,
            Source,
            Kind,
            physical,
            elemental,
            RequiredWeaponFamily,
            RequiredExpression,
            RequiredAffinity,
            cooldown,
            activation,
            target,
            PriorityRule,
            UseCondition,
            AppliesStatus,
            AnimationTag,
            Evolutions,
            config);
    }

    private IEnumerable<TechniqueEvolution> OrderedEvolutions()
    {
        var ordered = new List<TechniqueEvolution>(Evolutions);
        ordered.Sort((left, right) => ((int)left.Milestone).CompareTo((int)right.Milestone));
        return ordered;
    }

    private static void ValidateBudget(
        double physical,
        double elemental,
        CombatBalanceConfig config,
        string id)
    {
        if (!double.IsFinite(physical) || physical < 0)
            throw new ArgumentOutOfRangeException(nameof(physical), physical, "Physical coefficient must be finite and non-negative.");
        if (!double.IsFinite(elemental) || elemental < 0)
            throw new ArgumentOutOfRangeException(nameof(elemental), elemental, "Elemental coefficient must be finite and non-negative.");
        double total = physical + elemental;
        if (Math.Abs(total - config.TechniqueCoefficientBudget) > config.TechniqueBudgetTolerance)
        {
            throw new ArgumentException(
                $"Technique '{id}' coefficients sum to {total} but the budget is "
                + $"{config.TechniqueCoefficientBudget}. An evolution redistributes the pair; "
                + "it never raises one side for free.",
                nameof(physical));
        }
    }
}
