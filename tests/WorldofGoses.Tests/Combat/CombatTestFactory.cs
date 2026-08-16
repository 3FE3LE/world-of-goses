using System;
using System.Collections.Generic;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;

namespace WorldofGoses.Tests.Combat;

/// <summary>
/// Small, readable fixtures for the combat slice. Deliberately hand-built rather
/// than captured snapshots: a failing assertion should point at one number.
/// </summary>
internal static class CombatTestFactory
{
    public static TechniqueResolver Resolver(CombatBalanceConfig? balance = null)
    {
        CombatBalanceConfig config = balance ?? CombatBalanceConfig.Default;
        return new TechniqueResolver(
            new DefensiveStatisticsCalculator(StatisticsBalanceConfig.Default),
            new StatusResolver(config),
            config);
    }

    public static CombatantState Combatant(
        string id,
        CombatSide side,
        double maxHealth = 100,
        double currentHealth = 100,
        double physicalPower = 50,
        double elementalPower = 50,
        double physicalMitigation = 0,
        double elementalMitigation = 0,
        double generalReduction = 0,
        double criticalChance = 0,
        double attackSpeed = 1.0,
        ElementalAffinity affinity = ElementalAffinity.Fire,
        PhysicalExpression expression = PhysicalExpression.Stunning,
        WeaponFamily? weapon = null,
        IReadOnlyList<TechniqueDefinition>? techniques = null,
        CitizenId? citizenId = null,
        CombatSpatialState? spatial = null,
        double physicalEvasion = 0,
        double elementalEvasion = 0,
        double controlPower = 0,
        // Zero by default, which disables the control roll entirely. Every
        // fixture that does not care about control therefore keeps landing its
        // expressions exactly as it did before the roll existed.
        double controlResistance = 0) =>
        new(
            id,
            id,
            side,
            citizenId,
            maxHealth,
            currentHealth,
            physicalPower,
            elementalPower,
            physicalMitigation,
            elementalMitigation,
            generalReduction,
            criticalChance,
            attackSpeed,
            affinity,
            expression,
            weapon,
            techniques ?? Array.Empty<TechniqueDefinition>(),
            spatial: spatial,
            physicalEvasion: physicalEvasion,
            elementalEvasion: elementalEvasion,
            controlPower: controlPower,
            controlResistance: controlResistance);

    /// <summary>A single-technique attacker, so a test controls exactly what fires.</summary>
    public static CombatantState AttackerWith(
        string id,
        CombatSide side,
        TechniqueDefinition technique,
        double physicalPower = 60,
        double elementalPower = 20,
        double attackSpeed = 1.0,
        double maxHealth = 100,
        double currentHealth = 100) =>
        Combatant(
            id,
            side,
            maxHealth: maxHealth,
            currentHealth: currentHealth,
            physicalPower: physicalPower,
            elementalPower: elementalPower,
            attackSpeed: attackSpeed,
            techniques: new[] { technique });

    /// <summary>An inert target that never acts, isolating the attacker's behaviour.</summary>
    public static CombatantState Dummy(
        string id,
        CombatSide side,
        double maxHealth = 1000,
        double physicalMitigation = 0,
        double elementalMitigation = 0) =>
        Combatant(
            id,
            side,
            maxHealth: maxHealth,
            currentHealth: maxHealth,
            physicalMitigation: physicalMitigation,
            elementalMitigation: elementalMitigation,
            techniques: Array.Empty<TechniqueDefinition>());

    public static TechniqueDefinition Technique(
        string id,
        double physical = 0.5,
        double elemental = 0.5,
        int cooldown = 0,
        int activationTime = 0,
        TechniqueTargetRule target = TechniqueTargetRule.SingleEnemy,
        TechniquePriorityRule priority = TechniquePriorityRule.Sustained,
        TechniqueUseCondition condition = TechniqueUseCondition.UseWhenReady,
        StatusEffectId? appliesStatus = null) =>
        new(
            id,
            TechniqueSource.Weapon,
            TechniqueKind.Active,
            physical,
            elemental,
            cooldown: cooldown,
            activationTime: activationTime,
            targetRule: target,
            priorityRule: priority,
            useCondition: condition,
            appliesStatus: appliesStatus);

    public static CombatEncounter Encounter(
        IReadOnlyList<CombatantState> party,
        IReadOnlyList<CombatantState> enemies,
        ulong seed = 7,
        IReadOnlyDictionary<string, CombatantPlan>? plans = null,
        CombatBalanceConfig? balance = null)
    {
        CombatBalanceConfig config = balance ?? CombatBalanceConfig.Default;
        return new CombatEncounter(
            "test.encounter",
            party,
            enemies,
            plans ?? new Dictionary<string, CombatantPlan>(),
            Resolver(config),
            new StatusResolver(config),
            new DeterministicRandom(seed),
            config);
    }
}
