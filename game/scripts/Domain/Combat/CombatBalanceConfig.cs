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

    /// <summary>
    /// What a stunned target's elemental mitigation is multiplied by. A rattled
    /// combatant cannot hold its resonance, so the interrupt also opens an
    /// elemental window — which is what gives Stunning an offensive reason to
    /// exist beyond denying one action.
    /// </summary>
    public double StunningElementalMitigationScale { get; init; } = 0.6;

    /// <summary>Steps a Knockdown application lasts before expiring.</summary>
    public int KnockdownDurationSteps { get; init; } = 2;

    /// <summary>Stacks of Knockdown required before the target loses its turn.</summary>
    public int KnockdownThreshold { get; init; } = 1;

    /// <summary>
    /// Steps a Paralysis application lasts. The longest of the three control
    /// effects, because its pressure is probabilistic rather than certain.
    /// </summary>
    public int ParalysisDurationSteps { get; init; } = 5;

    /// <summary>Stacks of Paralysis required before it bites.</summary>
    public int ParalysisThreshold { get; init; } = 1;

    /// <summary>
    /// What a paralysed combatant's movement is multiplied by. Severe, because
    /// this is the effect's main body: a slow, not a root.
    /// </summary>
    public double ParalysisMovementSpeedScale { get; init; } = 0.35;

    /// <summary>
    /// Per-step chance that Paralysis also costs the action outright.
    /// </summary>
    /// <remarks>
    /// This is what stops Paralysis from being free against an enemy that never
    /// wanted to move. A ranged attacker ignores a movement debuff entirely, so
    /// without this the effect would read as "anchor them while they keep firing".
    /// It is deliberately a <em>chance</em> over a long duration, against
    /// Stunning's certainty over a short one — same family, opposite textures.
    /// </remarks>
    public double ParalysisActionLossChance { get; init; } = 0.25;

    /// <summary>
    /// Steps a Bleeding application lasts. Physical attrition: it is mitigated
    /// like any physical hit, which is what separates it from Poisoning.
    /// </summary>
    public int BleedingDurationSteps { get; init; } = 4;

    /// <summary>Stacks of Bleeding required before it starts costing health.</summary>
    public int BleedingThreshold { get; init; } = 1;

    /// <summary>Health lost per step, per stack of Bleeding, before mitigation.</summary>
    public double BleedingDamagePerStack { get; init; } = 6;

    /// <summary>
    /// Steps a Poisoning application lasts. The longest of the six, because it
    /// does not stack: refreshing it is the only way to keep it up, so its
    /// pressure comes from duration rather than from accumulation.
    /// </summary>
    public int PoisoningDurationSteps { get; init; } = 7;

    /// <summary>Stacks of Poisoning required before it starts costing health.</summary>
    public int PoisoningThreshold { get; init; } = 1;

    /// <summary>
    /// Health lost per step of Poisoning, ignoring mitigation. Lower than a
    /// stack of Bleeding precisely because nothing reduces it.
    /// </summary>
    public double PoisoningDamagePerStep { get; init; } = 4;

    /// <summary>
    /// What all damage taken is multiplied by while poisoned.
    /// </summary>
    /// <remarks>
    /// Poisoning cannot be stacked, so it needed a second way to scale or it
    /// would be strictly worse than a second application of Bleeding. It does
    /// not deepen — it makes everything else land harder, which is a different
    /// kind of pressure and rewards applying it early rather than repeatedly.
    /// </remarks>
    public double PoisoningDamageTakenScale { get; init; } = 1.15;

    /// <summary>Steps a Fracture application lasts inside the encounter.</summary>
    public int FractureDurationSteps { get; init; } = 10;

    /// <summary>
    /// What a fractured target's physical mitigation is multiplied by. Fracture
    /// is the physical counterpart of Stunning's elemental window.
    /// </summary>
    public double FracturePhysicalMitigationScale { get; init; } = 0.6;

    /// <summary>
    /// Health a fractured combatant loses on a fully physical blow of its own,
    /// scaled down by however much of the blow was elemental instead.
    /// </summary>
    /// <remarks>
    /// Using a broken body hurts, and only the bodily part of a technique does.
    /// This is Fracture's second and last effect: it briefly also slowed the
    /// attack clock and charged a flat cost every step the target acted at all,
    /// which stacked three penalties onto one expression and made it the obvious
    /// pick over the other five.
    /// </remarks>
    public double FractureExertionDamage { get; init; } = 3;

    /// <summary>
    /// Stacks of Fracture required before the bone actually gives. Two, so a
    /// single graze cannot cripple someone for days after the encounter.
    /// </summary>
    public int FractureThreshold { get; init; } = 2;

    /// <summary>
    /// While knocked down a combatant is easier to hit: its mitigation is scaled
    /// by this factor, on both windows at once, because prone guards nothing.
    /// Knockdown is also the only effect that takes ground — see
    /// <see cref="KnockbackBaseDistance"/>.
    /// </summary>
    public double KnockdownMitigationScale { get; init; } = 0.5;

    // ---- Control ----------------------------------------------------------

    /// <summary>
    /// Chance an expression sticks when attacker and target are evenly matched.
    /// </summary>
    /// <remarks>
    /// Deliberately high. Control is the offensive point of all six expressions,
    /// so the default outcome of throwing one is that it works; Control
    /// Resistance carves into that rather than being a wall. The alternative —
    /// an even split between equals — would have halved the whole expression
    /// system's output and forced every duration in this file to be retuned.
    /// </remarks>
    public double BaseControlLandChance { get; init; } = 0.75;

    /// <summary>
    /// Floor on the land chance, so no amount of Stability makes a combatant
    /// immune to an expression. A wall the player cannot ever get through is
    /// not difficulty, it is a locked door.
    /// </summary>
    public double MinimumControlLandChance { get; init; } = 0.30;

    /// <summary>
    /// Ceiling on the land chance, so control is never a certainty either.
    /// </summary>
    public double MaximumControlLandChance { get; init; } = 0.95;

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

    // ---- Lateral space ----------------------------------------------------

    /// <summary>Authoritative one-dimensional combat envelope.</summary>
    public double BattlefieldMinimumX { get; init; } = 0;
    public double BattlefieldMaximumX { get; init; } = 1000;

    /// <summary>Distance covered per logical step for each derived speed point.</summary>
    public double MovementDistancePerSpeedPoint { get; init; } = 48;

    /// <summary>
    /// Base displacement before attacker Impulse, defender Stability and the
    /// physical share of the blow.
    /// </summary>
    /// <remarks>
    /// Paid only by a blow that lands <see cref="StatusEffectId.Knockdown"/>.
    /// Every damaging technique used to move its target, so a fight drifted
    /// across the battlefield on ordinary attrition and engagement range became
    /// something neither side chose. The small shove a solid hit looks like it
    /// should produce is a hit reaction and belongs to presentation, which is
    /// free to draw it as long as the figure ends where the domain says it is.
    /// </remarks>
    public double KnockbackBaseDistance { get; init; } = 40;

    public double CitizenShortAttackRange { get; init; } = 42;
    public double CitizenSpearAttackRange { get; init; } = 68;
    public double CitizenRangedAttackRange { get; init; } = 230;
    public double CitizenBodyRadius { get; init; } = 12;

    public double PartyStartingX { get; init; } = 140;
    public double PartyStartingSpacing { get; init; } = 18;
    public double EnemyMeleeStartingX { get; init; } = 820;
    public double EnemyRangedStartingX { get; init; } = 850;

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
        if (StunningDurationSteps <= 0 || KnockdownDurationSteps <= 0
            || ParalysisDurationSteps <= 0 || BleedingDurationSteps <= 0
            || PoisoningDurationSteps <= 0 || FractureDurationSteps <= 0)
            throw new InvalidOperationException("Status durations must be positive.");
        if (StunningInterruptThreshold <= 0 || KnockdownThreshold <= 0
            || ParalysisThreshold <= 0 || BleedingThreshold <= 0
            || PoisoningThreshold <= 0 || FractureThreshold <= 0)
            throw new InvalidOperationException("Status thresholds must be positive.");
        if (BleedingDamagePerStack <= 0 || PoisoningDamagePerStep <= 0)
            throw new InvalidOperationException("Damage-over-time amounts must be positive.");
        if (KnockdownMitigationScale is < 0 or > 1)
            throw new InvalidOperationException("Knockdown mitigation scale must be within [0, 1].");
        if (MinimumControlLandChance is < 0 or > 1
            || MaximumControlLandChance is < 0 or > 1
            || MinimumControlLandChance > MaximumControlLandChance)
        {
            throw new InvalidOperationException(
                "Control land chance bounds must be an ordered pair within [0, 1].");
        }
        if (BaseControlLandChance <= 0)
            throw new InvalidOperationException("Base control land chance must be positive.");
        if (MaximumEncounterSteps <= 0)
            throw new InvalidOperationException("Maximum encounter steps must be positive.");
        if (FatigueForMinimumCondition <= 0)
            throw new InvalidOperationException("Fatigue for minimum condition must be positive.");
        if (RetreatHealthRatio is < 0 or > 1)
            throw new InvalidOperationException("Retreat health ratio must be within [0, 1].");
        if (BattlefieldMaximumX <= BattlefieldMinimumX)
            throw new InvalidOperationException("Battlefield maximum must exceed its minimum.");
        if (MovementDistancePerSpeedPoint <= 0)
            throw new InvalidOperationException("Movement distance per speed point must be positive.");
        if (KnockbackBaseDistance < 0)
            throw new InvalidOperationException("Knockback base distance cannot be negative.");
        if (CitizenShortAttackRange <= 0
            || CitizenSpearAttackRange <= 0
            || CitizenRangedAttackRange <= 0
            || CitizenBodyRadius <= 0)
        {
            throw new InvalidOperationException("Citizen spatial dimensions must be positive.");
        }
        if (PartyStartingSpacing < 0
            || PartyStartingX < BattlefieldMinimumX
            || PartyStartingX + 3 * PartyStartingSpacing > BattlefieldMaximumX
            || EnemyMeleeStartingX < BattlefieldMinimumX
            || EnemyMeleeStartingX > BattlefieldMaximumX
            || EnemyRangedStartingX < BattlefieldMinimumX
            || EnemyRangedStartingX > BattlefieldMaximumX)
        {
            throw new InvalidOperationException("Combat starting positions must fit the battlefield.");
        }
    }
}
