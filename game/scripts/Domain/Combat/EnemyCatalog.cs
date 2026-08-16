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
    double MovementSpeed,
    double AttackRange,
    double BodyRadius,
    double Stability,
    double Impulse,
    CombatStature Stature,
    ElementalAffinity Affinity,
    PhysicalExpression Expression,
    double PhysicalEvasion,
    double ElementalEvasion,
    double ControlPower,
    double ControlResistance);

public static class EnemyCatalog
{
    public sealed record EncounterTuning(double HealthFactor, double PowerFactor)
    {
        public static EncounterTuning Standard { get; } = new(1, 1);

        public void Validate()
        {
            if (HealthFactor <= 0) throw new ArgumentOutOfRangeException(nameof(HealthFactor));
            if (PowerFactor <= 0) throw new ArgumentOutOfRangeException(nameof(PowerFactor));
        }
    }

    private static readonly Dictionary<EnemyArchetype, EnemyDefinition> Definitions = new()
    {
        [EnemyArchetype.MeleeEnemy] = new EnemyDefinition(
            EnemyArchetype.MeleeEnemy, "Rooted brute",
            MaxHealth: 90, PhysicalChannelPower: 42, ElementalChannelPower: 10,
            PhysicalMitigation: 0.12, ElementalMitigation: 0.05,
            GeneralDamageReduction: 0.04, CriticalChance: 0.05, AttackSpeed: 1.00,
            MovementSpeed: 1.20, AttackRange: 34, BodyRadius: 14, Stability: 62, Impulse: 66,
            Stature: CombatStature.Tall,
            Affinity: ElementalAffinity.Earth, Expression: PhysicalExpression.Fracture,
            // Plants its feet and swings. Nothing here dodges.
            PhysicalEvasion: 0.04, ElementalEvasion: 0.02,
            ControlPower: 1.10, ControlResistance: 1.15),

        [EnemyArchetype.RangedEnemy] = new EnemyDefinition(
            EnemyArchetype.RangedEnemy, "Thorn slinger",
            MaxHealth: 60, PhysicalChannelPower: 30, ElementalChannelPower: 26,
            PhysicalMitigation: 0.04, ElementalMitigation: 0.08,
            GeneralDamageReduction: 0.02, CriticalChance: 0.10, AttackSpeed: 1.20,
            MovementSpeed: 0.92, AttackRange: 250, BodyRadius: 11, Stability: 42, Impulse: 48,
            Stature: CombatStature.Standard,
            Affinity: ElementalAffinity.Air, Expression: PhysicalExpression.Knockdown,
            // The one that is hard to hit and easy to control once you do.
            PhysicalEvasion: 0.16, ElementalEvasion: 0.10,
            ControlPower: 0.95, ControlResistance: 0.85),

        [EnemyArchetype.ResistantEnemy] = new EnemyDefinition(
            EnemyArchetype.ResistantEnemy, "Stone-shelled warden",
            MaxHealth: 140, PhysicalChannelPower: 34, ElementalChannelPower: 14,
            PhysicalMitigation: 0.35, ElementalMitigation: 0.30,
            GeneralDamageReduction: 0.12, CriticalChance: 0.05, AttackSpeed: 0.85,
            MovementSpeed: 0.72, AttackRange: 38, BodyRadius: 16, Stability: 82, Impulse: 58,
            Stature: CombatStature.Large,
            Affinity: ElementalAffinity.Earth, Expression: PhysicalExpression.Fracture,
            // Sits at the resistance ceiling: the answer to a control build is
            // an enemy control barely moves, not one it cannot touch. The floor
            // in CombatBalanceConfig still lets every expression through.
            PhysicalEvasion: 0.00, ElementalEvasion: 0.00,
            ControlPower: 1.00, ControlResistance: 1.40),

        [EnemyArchetype.SupportEnemy] = new EnemyDefinition(
            EnemyArchetype.SupportEnemy, "Murmuring tender",
            MaxHealth: 70, PhysicalChannelPower: 16, ElementalChannelPower: 34,
            PhysicalMitigation: 0.06, ElementalMitigation: 0.16,
            GeneralDamageReduction: 0.03, CriticalChance: 0.05, AttackSpeed: 1.05,
            MovementSpeed: 0.82, AttackRange: 210, BodyRadius: 12, Stability: 50, Impulse: 44,
            Stature: CombatStature.Small,
            Affinity: ElementalAffinity.Water, Expression: PhysicalExpression.Paralysis,
            // The controller of the four, and the most fragile under control
            // itself. Its Paralysis is the reason a party feels this one.
            PhysicalEvasion: 0.08, ElementalEvasion: 0.14,
            ControlPower: 1.30, ControlResistance: 0.90),
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
    public static CombatantState Create(
        EnemyArchetype archetype,
        string id,
        double positionX = 0,
        EncounterTuning? tuning = null)
    {
        EnemyDefinition definition = Get(archetype);
        tuning ??= EncounterTuning.Standard;
        tuning.Validate();
        // The definition authors both values. Deriving the expression from the
        // affinity instead, as this did, silently discarded a definition whose
        // two fields disagreed: the techniques came from `Expression` while the
        // combatant carried whatever the affinity implied.
        var nature = new CombatNature(definition.Affinity, definition.Expression);
        var techniques = new List<TechniqueDefinition>();
        techniques.AddRange(TechniqueCatalog.ForAffinity(definition.Affinity));
        techniques.AddRange(TechniqueCatalog.ForExpression(definition.Expression));

        return new CombatantState(
            id,
            definition.DisplayName,
            CombatSide.Enemy,
            citizenId: null,
            maxHealth: definition.MaxHealth * tuning.HealthFactor,
            currentHealth: definition.MaxHealth * tuning.HealthFactor,
            physicalChannelPower: definition.PhysicalChannelPower * tuning.PowerFactor,
            elementalChannelPower: definition.ElementalChannelPower * tuning.PowerFactor,
            physicalMitigation: definition.PhysicalMitigation,
            elementalMitigation: definition.ElementalMitigation,
            generalDamageReduction: definition.GeneralDamageReduction,
            criticalChance: definition.CriticalChance,
            attackSpeed: definition.AttackSpeed,
            elementalAffinity: definition.Affinity,
            physicalExpression: nature.PhysicalExpression,
            weaponFamily: null,
            techniques: techniques,
            spatial: new CombatSpatialState(
                positionX,
                definition.MovementSpeed,
                definition.AttackRange,
                definition.BodyRadius,
                definition.Stability,
                definition.Impulse,
                CombatFacing.Left),
            stature: definition.Stature,
            physicalEvasion: definition.PhysicalEvasion,
            elementalEvasion: definition.ElementalEvasion,
            controlPower: definition.ControlPower,
            controlResistance: definition.ControlResistance);
    }
}
