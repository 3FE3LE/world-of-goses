#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain.Combat;

/// <summary>
/// Provisional enemy archetypes, enough to exercise the engine and no more. This
/// is deliberately NOT a bestiary: coefficients are still unstable, so designing
/// creatures against them would be designing against noise.
///
/// <para>
/// Values live here as data, never in a scene. Enemies reuse the same technique
/// contract and the same mitigation vocabulary as a citizen, but do not carry a
/// citizen's persistent identity, competencies or equipment.
/// </para>
/// </summary>
public enum EnemyArchetype
{
    MeleeEnemy,
    RangedEnemy,
    ResistantEnemy,
    SupportEnemy,
}

/// <summary>PROVISIONAL BALANCE. One tuning row per archetype.</summary>
public sealed record EnemyDefinition(
    EnemyArchetype Archetype,
    string DisplayName,
    double MaxHealth,
    double PhysicalChannelPower,
    double ElementalChannelPower,
    double PhysicalMitigation,
    double ElementalMitigation,
    double GeneralDamageReduction,
    double CriticalChance,
    double AttackSpeed,
    ElementalAffinity Affinity,
    PhysicalExpression Expression);

public static class EnemyCatalog
{
    private static readonly Dictionary<EnemyArchetype, EnemyDefinition> Definitions = new()
    {
        [EnemyArchetype.MeleeEnemy] = new EnemyDefinition(
            EnemyArchetype.MeleeEnemy, "Rooted brute",
            MaxHealth: 90, PhysicalChannelPower: 42, ElementalChannelPower: 10,
            PhysicalMitigation: 0.12, ElementalMitigation: 0.05,
            GeneralDamageReduction: 0.04, CriticalChance: 0.05, AttackSpeed: 1.00,
            Affinity: ElementalAffinity.Earth, Expression: PhysicalExpression.Fracture),

        [EnemyArchetype.RangedEnemy] = new EnemyDefinition(
            EnemyArchetype.RangedEnemy, "Thorn slinger",
            MaxHealth: 60, PhysicalChannelPower: 30, ElementalChannelPower: 26,
            PhysicalMitigation: 0.04, ElementalMitigation: 0.08,
            GeneralDamageReduction: 0.02, CriticalChance: 0.10, AttackSpeed: 1.20,
            Affinity: ElementalAffinity.Air, Expression: PhysicalExpression.Knockdown),

        [EnemyArchetype.ResistantEnemy] = new EnemyDefinition(
            EnemyArchetype.ResistantEnemy, "Stone-shelled warden",
            MaxHealth: 140, PhysicalChannelPower: 34, ElementalChannelPower: 14,
            PhysicalMitigation: 0.35, ElementalMitigation: 0.30,
            GeneralDamageReduction: 0.12, CriticalChance: 0.05, AttackSpeed: 0.85,
            Affinity: ElementalAffinity.Earth, Expression: PhysicalExpression.Fracture),

        [EnemyArchetype.SupportEnemy] = new EnemyDefinition(
            EnemyArchetype.SupportEnemy, "Murmuring tender",
            MaxHealth: 70, PhysicalChannelPower: 16, ElementalChannelPower: 34,
            PhysicalMitigation: 0.06, ElementalMitigation: 0.16,
            GeneralDamageReduction: 0.03, CriticalChance: 0.05, AttackSpeed: 1.05,
            Affinity: ElementalAffinity.Water, Expression: PhysicalExpression.Paralysis),
    };

    public static EnemyDefinition Get(EnemyArchetype archetype) =>
        Definitions.TryGetValue(archetype, out EnemyDefinition? definition)
            ? definition
            : throw new ArgumentOutOfRangeException(nameof(archetype), archetype, "Unknown archetype.");

    /// <summary>
    /// Builds an encounter-ready combatant. Enemies draw their techniques from the
    /// same affinity and expression trees citizens use, so the engine exercises one
    /// resolution path rather than two.
    /// </summary>
    public static CombatantState Create(EnemyArchetype archetype, string id)
    {
        EnemyDefinition definition = Get(archetype);
        var nature = new CombatNature(definition.Affinity);
        var techniques = new List<TechniqueDefinition>();
        techniques.AddRange(TechniqueCatalog.ForAffinity(definition.Affinity));
        techniques.AddRange(TechniqueCatalog.ForExpression(definition.Expression));

        return new CombatantState(
            id,
            definition.DisplayName,
            CombatSide.Enemy,
            citizenId: null,
            maxHealth: definition.MaxHealth,
            currentHealth: definition.MaxHealth,
            physicalChannelPower: definition.PhysicalChannelPower,
            elementalChannelPower: definition.ElementalChannelPower,
            physicalMitigation: definition.PhysicalMitigation,
            elementalMitigation: definition.ElementalMitigation,
            generalDamageReduction: definition.GeneralDamageReduction,
            criticalChance: definition.CriticalChance,
            attackSpeed: definition.AttackSpeed,
            elementalAffinity: definition.Affinity,
            physicalExpression: nature.PhysicalExpression,
            weaponFamily: null,
            techniques: techniques);
    }
}
