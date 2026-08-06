using System;
using System.Collections.Generic;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using Xunit;

namespace WorldofGoses.Tests.Combat;

/// <summary>
/// Roadmap Fase 2: a technique turns channel power into a result through its two
/// coefficients, and its evolution redistributes that pair instead of inflating it.
/// </summary>
public sealed class TechniqueResolutionTests
{
    [Fact]
    public void HybridTechnique_SumsBothContributions()
    {
        TechniqueResolution resolution = Resolve(
            physicalCoefficient: 0.60,
            elementalCoefficient: 0.40,
            physicalPower: 100,
            elementalPower: 50);

        Assert.Equal(60, resolution.PhysicalContribution, 6);
        Assert.Equal(20, resolution.ElementalContribution, 6);
        Assert.Equal(80, resolution.RawTechniqueResult, 6);
    }

    [Fact]
    public void PurelyPhysicalTechnique_IgnoresElementalPower()
    {
        TechniqueResolution resolution = Resolve(1.00, 0.00, physicalPower: 90, elementalPower: 900);

        Assert.Equal(90, resolution.PhysicalContribution, 6);
        Assert.Equal(0, resolution.ElementalContribution, 6);
        Assert.Equal(90, resolution.RawTechniqueResult, 6);
        Assert.Equal(1.0, resolution.PhysicalShare, 6);
    }

    [Fact]
    public void PurelyElementalTechnique_IgnoresPhysicalPower()
    {
        TechniqueResolution resolution = Resolve(0.00, 1.00, physicalPower: 900, elementalPower: 70);

        Assert.Equal(0, resolution.PhysicalContribution, 6);
        Assert.Equal(70, resolution.ElementalContribution, 6);
        Assert.Equal(70, resolution.RawTechniqueResult, 6);
        Assert.Equal(0.0, resolution.PhysicalShare, 6);
    }

    [Fact]
    public void CoefficientPair_MustRespectTheBudget()
    {
        // A technique cannot simply be strong on both channels.
        ArgumentException error = Assert.Throws<ArgumentException>(() => new TechniqueDefinition(
            "invalid.greedy",
            TechniqueSource.Weapon,
            TechniqueKind.Active,
            physicalCoefficient: 1.00,
            elementalCoefficient: 1.00));

        Assert.Contains("budget", error.Message);
    }

    [Fact]
    public void Evolution_RedistributesTheCoefficientsWithoutRaisingTheTotal()
    {
        TechniqueDefinition spear = Find("spear.thrust");
        double budget = CombatBalanceConfig.Default.TechniqueCoefficientBudget;

        TechniqueDefinition before = spear.AtLevel(4);
        TechniqueDefinition after = spear.AtLevel(5);

        // The milestone shifts weight toward the physical channel...
        Assert.True(after.PhysicalCoefficient > before.PhysicalCoefficient);
        // ...and the elemental channel pays for exactly that shift.
        Assert.True(after.ElementalCoefficient < before.ElementalCoefficient);
        // What physical gains is exactly what elemental gives up.
        Assert.Equal(
            after.PhysicalCoefficient - before.PhysicalCoefficient,
            before.ElementalCoefficient - after.ElementalCoefficient,
            6);
        Assert.Equal(budget, before.PhysicalCoefficient + before.ElementalCoefficient, 6);
        Assert.Equal(budget, after.PhysicalCoefficient + after.ElementalCoefficient, 6);
        // Identity survives the evolution: it is the same technique, not a clone.
        Assert.Equal(spear.Id, after.Id);
    }

    [Fact]
    public void Evolution_AtLaterMilestone_ChangesTargetShape()
    {
        TechniqueDefinition spear = Find("spear.thrust");

        Assert.Equal(TechniqueTargetRule.SingleEnemy, spear.AtLevel(9).TargetRule);
        Assert.Equal(TechniqueTargetRule.AllEnemies, spear.AtLevel(10).TargetRule);
    }

    [Fact]
    public void Catalog_CombinesThreeModulesInsteadOfOnePerCombination()
    {
        var nature = new CombatNature(ElementalAffinity.Fire);

        IReadOnlyList<TechniqueDefinition> all = TechniqueCatalog.For(WeaponFamily.Mace, nature);

        // One weapon tree + one expression tree + one affinity tree, each with an
        // active and a passive: six, never one per weapon×expression×affinity.
        Assert.Equal(6, all.Count);
        Assert.Contains(all, technique => technique.Source == TechniqueSource.Weapon);
        Assert.Contains(all, technique => technique.Source == TechniqueSource.PhysicalExpression);
        Assert.Contains(all, technique => technique.Source == TechniqueSource.ElementalAffinity);
    }

    [Fact]
    public void EverySupportedAffinityHasContent()
    {
        foreach (ElementalAffinity affinity in Enum.GetValues<ElementalAffinity>())
        {
            IReadOnlyList<TechniqueDefinition> techniques = TechniqueCatalog.ForAffinity(affinity);
            Assert.Equal(2, techniques.Count);
            Assert.Contains(techniques, technique => technique.Kind == TechniqueKind.Active);
            Assert.Contains(techniques, technique => technique.Kind == TechniqueKind.Passive);
        }
    }

    [Fact]
    public void EverySliceWeaponFamilyHasAnActiveAndAPassive()
    {
        foreach (WeaponFamily family in TechniqueCatalog.SliceWeaponFamilies)
        {
            IReadOnlyList<TechniqueDefinition> techniques = TechniqueCatalog.ForWeapon(family);
            Assert.Equal(2, techniques.Count);
            Assert.Contains(techniques, technique => technique.Kind == TechniqueKind.Active);
            Assert.Contains(techniques, technique => technique.Kind == TechniqueKind.Passive);
        }
    }

    [Fact]
    public void Mitigation_ReducesTheFinalResultWithoutChangingTheRawResult()
    {
        TechniqueResolution soft = Resolve(0.50, 0.50, 100, 100, mitigation: 0.0);
        TechniqueResolution armoured = Resolve(0.50, 0.50, 100, 100, mitigation: 0.5);

        Assert.Equal(soft.RawTechniqueResult, armoured.RawTechniqueResult, 6);
        Assert.True(armoured.FinalResult < soft.FinalResult);
        // Blended by share: a 50/50 technique against 50 % on both channels.
        Assert.Equal(soft.RawTechniqueResult * 0.5, armoured.FinalResult, 6);
    }

    internal static TechniqueDefinition Find(string id)
    {
        foreach (WeaponFamily family in TechniqueCatalog.SliceWeaponFamilies)
        {
            foreach (TechniqueDefinition technique in TechniqueCatalog.ForWeapon(family))
            {
                if (technique.Id == id) return technique;
            }
        }
        throw new InvalidOperationException($"Technique '{id}' is not in the catalog.");
    }

    private static TechniqueResolution Resolve(
        double physicalCoefficient,
        double elementalCoefficient,
        double physicalPower,
        double elementalPower,
        double mitigation = 0.0)
    {
        var technique = new TechniqueDefinition(
            "test.technique",
            TechniqueSource.Weapon,
            TechniqueKind.Active,
            physicalCoefficient,
            elementalCoefficient);
        CombatantState source = CombatTestFactory.Combatant(
            "source", CombatSide.Party,
            physicalPower: physicalPower,
            elementalPower: elementalPower,
            criticalChance: 0.0);
        CombatantState target = CombatTestFactory.Combatant(
            "target", CombatSide.Enemy,
            physicalMitigation: mitigation,
            elementalMitigation: mitigation);

        return CombatTestFactory.Resolver().Resolve(
            step: 1, technique, source, target, new DeterministicRandom(1));
    }
}
