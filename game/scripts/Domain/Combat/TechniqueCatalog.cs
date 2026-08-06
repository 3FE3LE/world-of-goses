#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain.Combat;

/// <summary>
/// Provisional technique content. Three independent module groups that COMBINE —
/// never one ability per weapon×expression×affinity combination:
///
/// <code>
/// weapon family tree   (Spear, Staff, Mace, Orb in this slice)
/// physical expression  (Stunning, Knockdown in this slice)
/// elemental affinity   (all six, in the domain from the start)
/// </code>
///
/// A citizen draws one tree from each group, so four weapons × two expressions ×
/// six affinities is 12 definitions here, not 48.
///
/// <para>
/// PROVISIONAL BALANCE. Every coefficient pair sums to
/// <see cref="CombatBalanceConfig.TechniqueCoefficientBudget"/>; the split
/// expresses each module's character (Mace leans physical, Orb leans elemental)
/// and is not tuned content.
/// </para>
/// </summary>
public static class TechniqueCatalog
{
    /// <summary>The four weapon families this slice gives content to.</summary>
    public static IReadOnlyList<WeaponFamily> SliceWeaponFamilies { get; } = new[]
    {
        WeaponFamily.Spear,
        WeaponFamily.Staff,
        WeaponFamily.Mace,
        WeaponFamily.Orb,
    };

    /// <summary>The two expressions with full behaviour in this slice.</summary>
    public static IReadOnlyList<PhysicalExpression> SliceExpressions { get; } = new[]
    {
        PhysicalExpression.Stunning,
        PhysicalExpression.Knockdown,
    };

    /// <summary>
    /// Weapon trees. The demonstrative evolution lives on Spear: at level 5 it
    /// redistributes the pair toward the physical side, and because the budget is
    /// re-validated the elemental side must give up exactly what physical gains.
    /// </summary>
    public static IReadOnlyList<TechniqueDefinition> ForWeapon(WeaponFamily family) => family switch
    {
        WeaponFamily.Spear => new[]
        {
            new TechniqueDefinition(
                "spear.thrust",
                TechniqueSource.Weapon,
                TechniqueKind.Active,
                physicalCoefficient: 0.70,
                elementalCoefficient: 0.30,
                requiredWeaponFamily: WeaponFamily.Spear,
                cooldown: 2,
                activationTime: 0,
                targetRule: TechniqueTargetRule.SingleEnemy,
                priorityRule: TechniquePriorityRule.Sustained,
                animationTag: "spear_thrust",
                evolutions: new[]
                {
                    // Redistribution, not a bonus: +0.15 physical costs 0.15 elemental.
                    new TechniqueEvolution(
                        TechniqueEvolutionMilestone.Orientation,
                        "spear.thrust.physical",
                        PhysicalCoefficient: 0.85,
                        ElementalCoefficient: 0.15),
                    new TechniqueEvolution(
                        TechniqueEvolutionMilestone.TargetShape,
                        "spear.thrust.reach",
                        TargetRule: TechniqueTargetRule.AllEnemies),
                }),
            Passive("spear.footing", WeaponFamily.Spear, 0.60, 0.40),
        },
        WeaponFamily.Staff => new[]
        {
            new TechniqueDefinition(
                "staff.sweep",
                TechniqueSource.Weapon,
                TechniqueKind.Active,
                physicalCoefficient: 0.50,
                elementalCoefficient: 0.50,
                requiredWeaponFamily: WeaponFamily.Staff,
                cooldown: 3,
                activationTime: 1,
                targetRule: TechniqueTargetRule.AllEnemies,
                priorityRule: TechniquePriorityRule.Opening,
                useCondition: TechniqueUseCondition.UseAgainstTwoOrMoreEnemies,
                animationTag: "staff_sweep"),
            Passive("staff.balance", WeaponFamily.Staff, 0.45, 0.55),
        },
        WeaponFamily.Mace => new[]
        {
            new TechniqueDefinition(
                "mace.crush",
                TechniqueSource.Weapon,
                TechniqueKind.Active,
                physicalCoefficient: 0.85,
                elementalCoefficient: 0.15,
                requiredWeaponFamily: WeaponFamily.Mace,
                cooldown: 3,
                activationTime: 0,
                targetRule: TechniqueTargetRule.SingleEnemy,
                priorityRule: TechniquePriorityRule.Finisher,
                animationTag: "mace_crush"),
            Passive("mace.weight", WeaponFamily.Mace, 0.80, 0.20),
        },
        WeaponFamily.Orb => new[]
        {
            new TechniqueDefinition(
                "orb.channel",
                TechniqueSource.Weapon,
                TechniqueKind.Active,
                physicalCoefficient: 0.15,
                elementalCoefficient: 0.85,
                requiredWeaponFamily: WeaponFamily.Orb,
                cooldown: 2,
                activationTime: 1,
                targetRule: TechniqueTargetRule.LowestHealthEnemy,
                priorityRule: TechniquePriorityRule.Sustained,
                animationTag: "orb_channel"),
            Passive("orb.resonance", WeaponFamily.Orb, 0.10, 0.90),
        },
        _ => Array.Empty<TechniqueDefinition>(),
    };

    /// <summary>
    /// Physical-expression trees. The active technique is the one that applies the
    /// expression's status, which is how an expression becomes a consequence
    /// rather than a damage multiplier.
    /// </summary>
    public static IReadOnlyList<TechniqueDefinition> ForExpression(PhysicalExpression expression)
    {
        StatusEffectId status = StatusFor(expression);
        string prefix = expression.ToString().ToLowerInvariant();
        return new[]
        {
            new TechniqueDefinition(
                $"{prefix}.impose",
                TechniqueSource.PhysicalExpression,
                TechniqueKind.Active,
                physicalCoefficient: 0.75,
                elementalCoefficient: 0.25,
                requiredExpression: expression,
                cooldown: 4,
                activationTime: 0,
                targetRule: TechniqueTargetRule.SingleEnemy,
                priorityRule: TechniquePriorityRule.Opening,
                useCondition: TechniqueUseCondition.UseToInterrupt,
                appliesStatus: status,
                animationTag: $"{prefix}_impose"),
            new TechniqueDefinition(
                $"{prefix}.conditioning",
                TechniqueSource.PhysicalExpression,
                TechniqueKind.Passive,
                physicalCoefficient: 0.70,
                elementalCoefficient: 0.30,
                requiredExpression: expression),
        };
    }

    /// <summary>
    /// Elemental-affinity trees. All six exist. They differ by which affinity
    /// discriminates the elemental contribution — not by six recolours of the same
    /// periodic damage.
    /// </summary>
    public static IReadOnlyList<TechniqueDefinition> ForAffinity(ElementalAffinity affinity)
    {
        string prefix = affinity.ToString().ToLowerInvariant();
        return new[]
        {
            new TechniqueDefinition(
                $"{prefix}.surge",
                TechniqueSource.ElementalAffinity,
                TechniqueKind.Active,
                physicalCoefficient: 0.20,
                elementalCoefficient: 0.80,
                requiredAffinity: affinity,
                cooldown: 3,
                activationTime: 1,
                targetRule: TechniqueTargetRule.SingleEnemy,
                priorityRule: TechniquePriorityRule.Sustained,
                animationTag: $"{prefix}_surge"),
            new TechniqueDefinition(
                $"{prefix}.attunement",
                TechniqueSource.ElementalAffinity,
                TechniqueKind.Passive,
                physicalCoefficient: 0.25,
                elementalCoefficient: 0.75,
                requiredAffinity: affinity),
        };
    }

    /// <summary>
    /// The full technique set a citizen brings: its weapon tree, its expression
    /// tree and its affinity tree, combined.
    /// </summary>
    public static IReadOnlyList<TechniqueDefinition> For(
        WeaponFamily family,
        CombatNature nature)
    {
        ArgumentNullException.ThrowIfNull(nature);
        var all = new List<TechniqueDefinition>();
        all.AddRange(ForWeapon(family));
        all.AddRange(ForExpression(nature.PhysicalExpression));
        all.AddRange(ForAffinity(nature.ElementalAffinity));
        return all;
    }

    /// <summary>The status an expression imposes, mirroring the canonical mapping.</summary>
    public static StatusEffectId StatusFor(PhysicalExpression expression) => expression switch
    {
        PhysicalExpression.Stunning => StatusEffectId.Stunning,
        PhysicalExpression.Knockdown => StatusEffectId.Knockdown,
        PhysicalExpression.Fracture => StatusEffectId.Fracture,
        PhysicalExpression.Bleeding => StatusEffectId.Bleeding,
        PhysicalExpression.Poisoning => StatusEffectId.Poisoning,
        PhysicalExpression.Paralysis => StatusEffectId.Paralysis,
        _ => throw new ArgumentOutOfRangeException(nameof(expression), expression, null),
    };

    private static TechniqueDefinition Passive(
        string id,
        WeaponFamily family,
        double physical,
        double elemental) =>
        new(
            id,
            TechniqueSource.Weapon,
            TechniqueKind.Passive,
            physical,
            elemental,
            requiredWeaponFamily: family);
}
