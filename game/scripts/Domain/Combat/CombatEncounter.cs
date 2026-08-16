#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain.Combat;

public enum CombatOutcome
{
    InProgress,
    PartyVictory,
    PartyDefeated,
    PartyRetreated,
    Exhausted,
}

public enum CombatLogKind
{
    EncounterBegan,
    CombatantMoved,
    BasicAttackResolved,
    TechniqueResolved,
    KnockbackApplied,
    StatusApplied,

    /// <summary>
    /// A status took health: a Bleeding or Poisoning tick, or the exertion a
    /// Fracture charges its bearer. Distinct from <see cref="StatusApplied"/>,
    /// which used to carry both — "you were poisoned" and "the poison bit"
    /// were the same kind, told apart only by the actor and target happening
    /// to be the same combatant.
    /// </summary>
    StatusDamage,

    /// <summary>
    /// A status was thrown and did not stick, because the target's Control
    /// Resistance beat the attacker's Control Power.
    /// </summary>
    StatusResisted,

    /// <summary>A technique was avoided outright and did nothing.</summary>
    Evaded,

    ActionPrevented,
    CombatantDefeated,
    Retreated,
    EncounterEnded,
}

/// <summary>
/// The measurable consequence of one log entry, in domain units.
/// </summary>
/// <remarks>
/// <para>
/// Every number here used to be formatted into <see cref="CombatLogEntry.Detail"/>
/// and, if anyone wanted it back, parsed out of a string whose format was
/// nobody's contract. Presentation cannot draw a knockback arc off
/// <c>"12.4"</c> without agreeing on a culture, a precision and a sign
/// convention that no test pinned.
/// </para>
/// <para>
/// <see cref="Detail"/> stays: it is the line a human reads in the chronicle or
/// the debug log. This is the half a machine reads. Fields not relevant to a
/// given <see cref="CombatLogKind"/> are zero.
/// </para>
/// </remarks>
/// <param name="Displacement">
/// Signed movement along the battlefield axis, in the same units as
/// <see cref="CombatSpatialState.PositionX"/>. Positive is toward the maximum.
/// </param>
/// <param name="HealthDelta">
/// Signed change to the subject's health. Negative is damage, so presentation
/// never has to know which kinds happen to be harmful.
/// </param>
/// <param name="PhysicalShare">
/// How much of the causing blow came from the body, in <c>[0, 1]</c>. This is
/// what lets presentation dramatise a shove by the weapon's bluntness without
/// the domain having to decide how the hit looked.
/// </param>
public readonly record struct CombatImpact(
    double Displacement = 0,
    double HealthDelta = 0,
    double PhysicalShare = 0);

/// <summary>One auditable line of an encounter. Presentation reacts to these.</summary>
public sealed record CombatLogEntry(
    int Step,
    CombatLogKind Kind,
    string ActorId,
    string? TargetId,
    string Detail,
    TechniqueResolution? Resolution = null,
    CombatImpact? Impact = null);

/// <summary>Per-member automatic configuration. The player configures intent only.</summary>
public sealed record CombatantPlan(
    int Position,
    IReadOnlyList<string> TechniquePriority,
    string? PreferredTargetId,
    bool RetreatWhenBelowThreshold)
{
    public static CombatantPlan Default { get; } =
        new(0, Array.Empty<string>(), null, RetreatWhenBelowThreshold: false);
}

/// <summary>
/// Chooses which enemy a technique resolves against, honouring the technique's
/// target rule and the player's preferred target.
/// </summary>
public sealed class TargetResolver
{
    public CombatantState? Resolve(
        TechniqueDefinition technique,
        CombatantState actor,
        CombatantPlan plan,
        IReadOnlyList<CombatantState> allies,
        IReadOnlyList<CombatantState> enemies,
        IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(technique);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(allies);
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentNullException.ThrowIfNull(random);

        return technique.TargetRule switch
        {
            TechniqueTargetRule.Self => actor,
            TechniqueTargetRule.LowestHealthAlly => LowestHealth(allies),
            TechniqueTargetRule.LowestHealthEnemy => LowestHealth(enemies),
            // AllEnemies still reports a representative target for the log; the
            // encounter applies the result to every living enemy.
            //
            // With no preferred target the choice is a seeded draw among the living
            // rather than "whoever is first". Always taking the first opponent made
            // every attack pile onto one member, which is both unconvincing and hid
            // whether health really carried between encounters.
            _ => Preferred(plan, enemies) ?? RandomAlive(enemies, random),
        };
    }

    private static CombatantState? RandomAlive(
        IReadOnlyList<CombatantState> candidates,
        IRandomSource random)
    {
        var alive = new List<CombatantState>(candidates.Count);
        foreach (CombatantState candidate in candidates)
        {
            if (candidate.IsAlive) alive.Add(candidate);
        }
        if (alive.Count == 0) return null;
        return alive[random.NextInt(alive.Count)];
    }

    private static CombatantState? Preferred(
        CombatantPlan plan,
        IReadOnlyList<CombatantState> enemies)
    {
        if (plan.PreferredTargetId is null) return null;
        foreach (CombatantState enemy in enemies)
        {
            if (enemy.IsAlive && enemy.Id == plan.PreferredTargetId) return enemy;
        }
        return null;
    }

    private static CombatantState? FirstAlive(IReadOnlyList<CombatantState> candidates)
    {
        foreach (CombatantState candidate in candidates)
        {
            if (candidate.IsAlive) return candidate;
        }
        return null;
    }

    private static CombatantState? LowestHealth(IReadOnlyList<CombatantState> candidates)
    {
        CombatantState? best = null;
        foreach (CombatantState candidate in candidates)
        {
            if (!candidate.IsAlive) continue;
            if (best is null || candidate.HealthRatio < best.HealthRatio) best = candidate;
        }
        return best;
    }
}

/// <summary>
/// Picks which ready technique a combatant uses this step. Order is: the player's
/// explicit priority list first, then the technique's own priority rule. A
/// technique whose use condition is not satisfied is skipped rather than delayed.
/// </summary>
public sealed class AutoCastController
{
    public TechniqueDefinition? Choose(
        CombatantState actor,
        CombatantPlan plan,
        IReadOnlyList<CombatantState> allies,
        IReadOnlyList<CombatantState> enemies)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(plan);

        var candidates = new List<TechniqueDefinition>();
        foreach (TechniqueDefinition technique in actor.ActiveTechniques)
        {
            if (!actor.IsReady(technique.Id)) continue;
            if (!ConditionSatisfied(technique, actor, plan, allies, enemies)) continue;
            candidates.Add(technique);
        }
        if (candidates.Count == 0) return null;

        candidates.Sort((left, right) =>
        {
            int byPlan = PlanRank(plan, left).CompareTo(PlanRank(plan, right));
            if (byPlan != 0) return byPlan;
            int byRule = RuleRank(left).CompareTo(RuleRank(right));
            if (byRule != 0) return byRule;
            // Stable final tiebreak so a replay from a seed is reproducible.
            return string.CompareOrdinal(left.Id, right.Id);
        });
        return candidates[0];
    }

    private static bool ConditionSatisfied(
        TechniqueDefinition technique,
        CombatantState actor,
        CombatantPlan plan,
        IReadOnlyList<CombatantState> allies,
        IReadOnlyList<CombatantState> enemies) => technique.UseCondition switch
        {
            TechniqueUseCondition.UseWhenReady => true,
            TechniqueUseCondition.UseAgainstTwoOrMoreEnemies => CountAlive(enemies) >= 2,
            TechniqueUseCondition.UseWhenAllyBelowHalfHealth => AnyBelowHalf(allies),
            // Reserved for interrupting: only worth spending while an enemy still
            // has an action to lose.
            TechniqueUseCondition.UseToInterrupt => CountAlive(enemies) >= 1,
            TechniqueUseCondition.ReserveForPrimaryTarget =>
                plan.PreferredTargetId is not null && AliveWithId(enemies, plan.PreferredTargetId),
            _ => true,
        };

    private static int PlanRank(CombatantPlan plan, TechniqueDefinition technique)
    {
        for (int index = 0; index < plan.TechniquePriority.Count; index++)
        {
            if (plan.TechniquePriority[index] == technique.Id) return index;
        }
        return plan.TechniquePriority.Count;
    }

    private static int RuleRank(TechniqueDefinition technique) => technique.PriorityRule switch
    {
        TechniquePriorityRule.Opening => 0,
        TechniquePriorityRule.Finisher => 1,
        _ => 2,
    };

    private static int CountAlive(IReadOnlyList<CombatantState> candidates)
    {
        int alive = 0;
        foreach (CombatantState candidate in candidates)
        {
            if (candidate.IsAlive) alive++;
        }
        return alive;
    }

    private static bool AnyBelowHalf(IReadOnlyList<CombatantState> candidates)
    {
        foreach (CombatantState candidate in candidates)
        {
            if (candidate.IsAlive && candidate.HealthRatio < 0.5) return true;
        }
        return false;
    }

    private static bool AliveWithId(IReadOnlyList<CombatantState> candidates, string id)
    {
        foreach (CombatantState candidate in candidates)
        {
            if (candidate.IsAlive && candidate.Id == id) return true;
        }
        return false;
    }
}

/// <summary>
/// Resolves one encounter in discrete logical steps. No Node, no _Process, no
/// animation: application code may advance it incrementally, while tests and
/// debug tools may consume the same engine through <see cref="ResolveToEnd"/>.
/// Presentation observes session snapshots and the resulting
/// <see cref="CombatLogEntry"/> stream.
///
/// <para>
/// Determinism: given the same combatants, plans, balance and seed, the produced
/// log is identical. Ordering is by attack speed then id, never by dictionary
/// enumeration.
/// </para>
/// </summary>
public sealed class CombatEncounter
{
    private readonly List<CombatantState> _party;
    private readonly List<CombatantState> _enemies;
    private readonly Dictionary<string, CombatantPlan> _plans;
    private readonly TechniqueResolver _techniques;
    private readonly StatusResolver _statuses;
    private readonly TargetResolver _targets;
    private readonly AutoCastController _autoCast;
    private readonly IRandomSource _random;
    private readonly CombatBalanceConfig _balance;
    private readonly List<CombatLogEntry> _log = new();
    private bool _began;
    private bool _ended;

    public CombatEncounter(
        string encounterId,
        IReadOnlyList<CombatantState> party,
        IReadOnlyList<CombatantState> enemies,
        IReadOnlyDictionary<string, CombatantPlan> plans,
        TechniqueResolver techniques,
        StatusResolver statuses,
        IRandomSource random,
        CombatBalanceConfig? balance = null)
    {
        if (string.IsNullOrWhiteSpace(encounterId))
            throw new ArgumentException("Encounter id is required.", nameof(encounterId));
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentNullException.ThrowIfNull(plans);

        EncounterId = encounterId;
        _party = new List<CombatantState>(party);
        _enemies = new List<CombatantState>(enemies);
        _plans = new Dictionary<string, CombatantPlan>(plans);
        _techniques = techniques ?? throw new ArgumentNullException(nameof(techniques));
        _statuses = statuses ?? throw new ArgumentNullException(nameof(statuses));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _balance = balance ?? CombatBalanceConfig.Default;
        _balance.Validate();
        _targets = new TargetResolver();
        _autoCast = new AutoCastController();
    }

    public string EncounterId { get; }
    public int Step { get; private set; }
    public CombatOutcome Outcome { get; private set; } = CombatOutcome.InProgress;
    public IReadOnlyList<CombatLogEntry> Log => _log;
    public IReadOnlyList<CombatantState> Party => _party;
    public IReadOnlyList<CombatantState> Enemies => _enemies;
    public double BattlefieldMinimumX => _balance.BattlefieldMinimumX;
    public double BattlefieldMaximumX => _balance.BattlefieldMaximumX;

    /// <summary>
    /// Advances a bounded number of logical combat steps. World/application code
    /// decides when those steps happen; the encounter owns no clock or speed.
    /// </summary>
    public CombatOutcome Advance(
        int steps = 1,
        bool autoPartySkills = true,
        IReadOnlySet<string>? manuallyActivatedPartyMembers = null)
    {
        if (steps < 0) throw new ArgumentOutOfRangeException(nameof(steps));
        BeginIfNeeded();

        for (int index = 0; index < steps && Outcome == CombatOutcome.InProgress; index++)
        {
            if (Step >= _balance.MaximumEncounterSteps)
            {
                Outcome = CombatOutcome.Exhausted;
                break;
            }
            Step++;
            AdvanceOneStep(
                autoPartySkills,
                index == 0 ? manuallyActivatedPartyMembers : null);
            EvaluateOutcome();
        }

        EndIfNeeded();
        return Outcome;
    }

    /// <summary>Runs the same incremental engine until it reaches a terminal outcome.</summary>
    public CombatOutcome ResolveToEnd()
    {
        while (Outcome == CombatOutcome.InProgress) Advance();
        return Outcome;
    }

    /// <summary>Compatibility alias retained for tests and the debug expedition.</summary>
    public CombatOutcome Resolve() => ResolveToEnd();

    private void AdvanceOneStep(
        bool autoPartySkills,
        IReadOnlySet<string>? manuallyActivatedPartyMembers)
    {
        foreach (CombatantState combatant in AllCombatants())
            combatant.Spatial.BeginStep(combatant.IsDefeated);

        ApplyDamageOverTime();

        foreach (CombatantState actor in TurnOrder())
        {
            if (Outcome != CombatOutcome.InProgress) return;
            if (actor.IsDefeated) continue;

            if (_statuses.PreventsAction(actor.Statuses))
            {
                Record(CombatLogKind.ActionPrevented, actor.Id, null,
                    _statuses.IsActive(actor.Statuses, StatusEffectId.Stunning)
                        ? "Stunning interrupted the action"
                        : "Knockdown cost the action");
                continue;
            }

            // Paralysis is the probabilistic member of the control family: a
            // long slow that sometimes seizes outright, against Stunning's brief
            // certainty. Rolled from the encounter's own seeded source, so an
            // unobserved resolution and a watched one produce the same fight.
            if (_statuses.ParalysisSeizesAction(actor.Statuses, _random))
            {
                Record(CombatLogKind.ActionPrevented, actor.Id, null,
                    "Paralysis seized the action");
                continue;
            }

            CombatantPlan plan = PlanFor(actor);
            if (ShouldRetreat(actor, plan))
            {
                Outcome = CombatOutcome.PartyRetreated;
                Record(CombatLogKind.Retreated, actor.Id, null,
                    $"Health {actor.HealthRatio:P0} at or below the retreat rule");
                return;
            }

            (IReadOnlyList<CombatantState> allies, IReadOnlyList<CombatantState> foes) = Sides(actor);
            CombatantState? approachTarget = ApproachTarget(actor, plan, foes);
            if (approachTarget is null) continue;

            // Knockdown roots outright — you are on the floor. Paralysis does
            // not: it scales the advance down hard, so a melee actor still
            // closes, just far too slowly to reach anyone this step.
            StatusModifiers modifiers = _statuses.Modifiers(actor.Statuses);
            bool rooted = _statuses.PreventsMovement(actor.Statuses);
            double moved = rooted
                ? 0
                : actor.Spatial.Approach(
                    approachTarget.Spatial,
                    actor.Spatial.MovementSpeed
                        * _balance.MovementDistancePerSpeedPoint
                        * modifiers.MovementSpeedScale,
                    _balance.BattlefieldMinimumX,
                    _balance.BattlefieldMaximumX);
            if (rooted)
            {
                Record(CombatLogKind.ActionPrevented, actor.Id, approachTarget.Id,
                    "Knockdown held the advance");
            }
            else if (Math.Abs(moved) > double.Epsilon)
            {
                Record(
                    CombatLogKind.CombatantMoved,
                    actor.Id,
                    approachTarget.Id,
                    moved.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture),
                    new CombatImpact(Displacement: moved));
            }

            IReadOnlyList<CombatantState> actionRangeFoes = FoesWithinRange(actor, foes);
            ApplyBasicAttack(actor, plan, allies, actionRangeFoes);
            if (CountAlive(foes) == 0) continue;

            bool manuallyActivated = actor.Side == CombatSide.Party
                && manuallyActivatedPartyMembers?.Contains(actor.Id) == true;
            TechniqueDefinition? technique = manuallyActivated
                ? FirstReadyActive(actor)
                : actor.Side == CombatSide.Enemy || autoPartySkills
                    ? _autoCast.Choose(actor, plan, allies, actionRangeFoes)
                    : null;
            if (technique is null) continue;

            CombatantState? target = technique.TargetRule is TechniqueTargetRule.Self
                or TechniqueTargetRule.LowestHealthAlly
                ? _targets.Resolve(technique, actor, plan, allies, foes, _random)
                : _targets.Resolve(technique, actor, plan, allies, actionRangeFoes, _random);
            if (target is null) continue;

            actor.StartCooldown(technique);
            actor.AddFatigue(_balance.FatiguePerAction);

            if (technique.TargetRule == TechniqueTargetRule.AllEnemies)
            {
                foreach (CombatantState enemy in new List<CombatantState>(actionRangeFoes))
                {
                    if (enemy.IsAlive) ApplyTechnique(technique, actor, enemy);
                }
            }
            else
            {
                ApplyTechnique(technique, actor, target);
            }
        }

        foreach (CombatantState combatant in AllCombatants())
        {
            combatant.TickCooldowns();
            combatant.ReplaceStatuses(_statuses.Tick(combatant.Statuses));
        }
    }

    /// <summary>
    /// Charges a fractured attacker for the bodily half of the blow it just threw.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Scaled by the resolution's own physical share, so the cost tracks how much
    /// of the technique was body: a fractured combatant swinging a weapon pays in
    /// full, and the same combatant channelling something purely elemental pays
    /// nothing. Bone does not object to resonance.
    /// </para>
    /// <para>
    /// This replaced a flat charge levied once per step on anyone who acted at
    /// all, which — together with an attack-speed penalty and a physical window —
    /// made Fracture three effects in one and plainly the strongest of the six.
    /// Fracture now does two things: it opens the physical window, and it makes
    /// the target's own physical attacks cost it.
    /// </para>
    /// </remarks>
    private void ChargeFractureExertion(CombatantState actor, TechniqueResolution resolution)
    {
        if (!_statuses.IsActive(actor.Statuses, StatusEffectId.Fracture)) return;

        double cost = _balance.FractureExertionDamage * resolution.PhysicalShare;
        if (cost <= 0) return;

        double paid = actor.ApplyResult(cost);
        if (paid <= 0) return;

        Record(
            CombatLogKind.StatusDamage,
            actor.Id,
            actor.Id,
            $"Fracture cost {paid:0.####} on a physical blow",
            new CombatImpact(HealthDelta: -paid, PhysicalShare: resolution.PhysicalShare));
        if (actor.IsDefeated)
        {
            Record(CombatLogKind.CombatantDefeated, actor.Id, null, "The fracture gave");
        }
    }

    /// <summary>
    /// Charges the two damage-over-time statuses before anyone acts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// At the top of the step, not inside the turn order, so attrition does not
    /// depend on where a combatant sits in the initiative: bleeding out is
    /// something the encounter does to you, not something your turn does.
    /// </para>
    /// <para>
    /// Bleeding passes through the target's physical mitigation and Poisoning
    /// does not. That single asymmetry is what makes armour a real answer to one
    /// and no answer at all to the other.
    /// </para>
    /// </remarks>
    private void ApplyDamageOverTime()
    {
        foreach (CombatantState combatant in AllCombatants())
        {
            if (combatant.IsDefeated) continue;

            (double mitigable, double unmitigable) = _statuses.DamageOverTime(combatant.Statuses);
            if (mitigable <= 0 && unmitigable <= 0) continue;

            double total = (mitigable * (1 - Math.Clamp(combatant.PhysicalMitigation, 0, 1)))
                + unmitigable;
            double applied = combatant.ApplyResult(total);
            if (applied <= 0) continue;

            Record(
                CombatLogKind.StatusDamage,
                combatant.Id,
                combatant.Id,
                applied.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture),
                // Attrition has no blow behind it, so no physical share: the
                // share describes a strike, and nothing struck here.
                new CombatImpact(HealthDelta: -applied));

            if (combatant.IsDefeated)
            {
                Record(CombatLogKind.CombatantDefeated, combatant.Id, null, "Bled out");
            }
        }
    }

    private void ApplyBasicAttack(
        CombatantState actor,
        CombatantPlan plan,
        IReadOnlyList<CombatantState> allies,
        IReadOnlyList<CombatantState> foesInRange)
    {
        // Inert test targets carry no combat techniques. Production combatants
        // always carry at least their active tree and therefore always pulse a
        // Basic Attack, independently from the player's AUTO preference.
        if (actor.Techniques.Count == 0) return;
        TechniqueDefinition basic = TechniqueCatalog.BasicAttack;
        CombatantState? target = _targets.Resolve(
            basic,
            actor,
            plan,
            allies,
            foesInRange,
            _random);
        if (target is null) return;

        actor.AddFatigue(_balance.FatiguePerAction);
        ApplyTechnique(basic, actor, target, CombatLogKind.BasicAttackResolved);
    }

    private static TechniqueDefinition? FirstReadyActive(CombatantState actor)
    {
        foreach (TechniqueDefinition technique in actor.ActiveTechniques)
        {
            if (actor.IsReady(technique.Id)) return technique;
        }
        return null;
    }

    private void ApplyTechnique(
        TechniqueDefinition technique,
        CombatantState actor,
        CombatantState target,
        CombatLogKind logKind = CombatLogKind.TechniqueResolved)
    {
        TechniqueResolution resolution =
            _techniques.Resolve(Step, technique, actor, target, _random);

        actor.Spatial.MarkActivity(logKind == CombatLogKind.BasicAttackResolved
            ? CombatSpatialActivity.BasicAttack
            : CombatSpatialActivity.ActiveSkill);

        if (resolution.Evaded)
        {
            // The attacker still swung, so its own activity stands and its
            // cooldown was already paid. The target simply was not there: no
            // hit animation, no fracture exertion, no expression, no shove.
            _log.Add(new CombatLogEntry(
                Step, CombatLogKind.Evaded, actor.Id, target.Id, technique.Id, resolution));
            return;
        }

        target.ApplyResult(resolution.FinalResult);
        target.Spatial.MarkActivity(target.IsDefeated
            ? CombatSpatialActivity.Defeated
            : CombatSpatialActivity.Hit);
        _log.Add(new CombatLogEntry(
            Step,
            logKind,
            actor.Id,
            target.Id,
            technique.Id,
            resolution));

        ChargeFractureExertion(actor, resolution);

        // Statuses are resolved before the knockback, not after, because the
        // knockback now depends on which of them actually stuck. Reading
        // `resolution.AppliedStatuses` here would read what the technique
        // *tried* to apply, and a resisted Knockdown would still shove.
        bool knocksDown = false;
        foreach (StatusEffectId status in resolution.AppliedStatuses)
        {
            if (!TryApplyStatus(status, actor, target)) continue;
            if (status == StatusEffectId.Knockdown) knocksDown = true;
        }

        // Only a blow that lands Knockdown moves anyone.
        //
        // Every damaging technique used to write PositionX, so a fight drifted
        // across the battlefield on ordinary attrition and range became noise:
        // two combatants trading jabs slid apart without either deciding to.
        // Displacement is now a consequence of the expression, and Knockdown's
        // natural weapons are the polearms — leverage, not concussion.
        //
        // The small shove every solid hit looks like it should produce is real,
        // but it is a hit reaction and lives in presentation: transient, ending
        // where the domain says the target still stands. Presentation can size
        // it from the CombatImpact this records.
        if (knocksDown && !target.IsDefeated && resolution.FinalResult > 0)
        {
            // Momentum is transferred by the bodily part of a blow and by
            // nothing else. Without the physical share here a purely elemental
            // blast displaced exactly as far as a spear thrust, because the
            // formula only ever read Impulse against Stability.
            double resistance = actor.Spatial.Impulse + target.Spatial.Stability;
            double impulseShare = resistance <= 0 ? 0 : actor.Spatial.Impulse / resistance;
            double direction = target.Spatial.DirectionAwayFrom(actor.Spatial, target.Side);
            double displacement = target.Spatial.ApplyKnockback(
                direction
                    * _balance.KnockbackBaseDistance
                    * impulseShare
                    * resolution.PhysicalShare,
                _balance.BattlefieldMinimumX,
                _balance.BattlefieldMaximumX);
            if (Math.Abs(displacement) > double.Epsilon)
            {
                Record(
                    CombatLogKind.KnockbackApplied,
                    actor.Id,
                    target.Id,
                    displacement.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture),
                    new CombatImpact(
                        Displacement: displacement,
                        PhysicalShare: resolution.PhysicalShare));
            }
        }

        if (target.IsDefeated)
        {
            Record(CombatLogKind.CombatantDefeated, target.Id, null,
                target.Side == CombatSide.Party ? "Incapacitated" : "Defeated");
        }
    }

    /// <summary>
    /// Throws one physical expression at a target and reports whether it stuck.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Expressions used to land every single time. That made the six of them
    /// unconditional: there was no build that shrugged off a Knockdown, no
    /// reason for a heavy, planted combatant to feel any different from a
    /// fragile one under control pressure, and Control Power was a stat nobody
    /// could spend anything on because nothing read it.
    /// </para>
    /// <para>
    /// The roll is an opposed ratio of the two derived multipliers around a base
    /// chance, then clamped: control is never certain and never impossible. See
    /// <see cref="CombatBalanceConfig.BaseControlLandChance"/>.
    /// </para>
    /// <para>
    /// A target with no Control Resistance at all is not rolled against. That is
    /// not a special case for its own sake — it keeps a combatant nobody has
    /// given control statistics to behaving exactly as before, so an encounter
    /// built from bare parts stays reproducible against its old seed.
    /// </para>
    /// </remarks>
    private bool TryApplyStatus(StatusEffectId status, CombatantState actor, CombatantState target)
    {
        if (target.ControlResistance > 0)
        {
            double power = Math.Max(actor.ControlPower, 0);
            double chance = Math.Clamp(
                _balance.BaseControlLandChance * power / target.ControlResistance,
                _balance.MinimumControlLandChance,
                _balance.MaximumControlLandChance);
            if (_random.NextDouble() >= chance)
            {
                Record(CombatLogKind.StatusResisted, actor.Id, target.Id, status.ToString());
                return false;
            }
        }

        StatusEffect effect = _statuses.Create(status, actor.Id, target.Id, Step);
        target.ReplaceStatuses(_statuses.Apply(target.Statuses, effect));
        Record(CombatLogKind.StatusApplied, actor.Id, target.Id, status.ToString());
        return true;
    }

    /// <summary>
    /// Faster combatants act first; ties break on id so enumeration order can never
    /// influence the result.
    /// </summary>
    private IEnumerable<CombatantState> TurnOrder()
    {
        var ordered = new List<CombatantState>(AllCombatants());
        ordered.Sort((left, right) =>
        {
            int bySpeed = right.AttackSpeed.CompareTo(left.AttackSpeed);
            return bySpeed != 0 ? bySpeed : string.CompareOrdinal(left.Id, right.Id);
        });
        return ordered;
    }

    private IEnumerable<CombatantState> AllCombatants()
    {
        foreach (CombatantState member in _party) yield return member;
        foreach (CombatantState enemy in _enemies) yield return enemy;
    }

    private (IReadOnlyList<CombatantState> Allies, IReadOnlyList<CombatantState> Foes) Sides(
        CombatantState actor) =>
        actor.Side == CombatSide.Party ? (_party, _enemies) : (_enemies, _party);

    private CombatantPlan PlanFor(CombatantState actor) =>
        _plans.TryGetValue(actor.Id, out CombatantPlan? plan) ? plan : CombatantPlan.Default;

    private bool ShouldRetreat(CombatantState actor, CombatantPlan plan) =>
        actor.Side == CombatSide.Party
        && plan.RetreatWhenBelowThreshold
        && actor.HealthRatio <= _balance.RetreatHealthRatio;

    private CombatantState? ApproachTarget(
        CombatantState actor,
        CombatantPlan plan,
        IReadOnlyList<CombatantState> foes)
    {
        if (plan.PreferredTargetId is not null)
        {
            foreach (CombatantState foe in foes)
            {
                if (foe.IsAlive && foe.Id == plan.PreferredTargetId) return foe;
            }
        }

        CombatantState? nearest = null;
        double nearestDistance = double.MaxValue;
        foreach (CombatantState foe in foes)
        {
            if (!foe.IsAlive) continue;
            double distance = actor.Spatial.EdgeDistanceTo(foe.Spatial);
            if (distance < nearestDistance
                || (Math.Abs(distance - nearestDistance) <= double.Epsilon
                    && nearest is not null
                    && string.CompareOrdinal(foe.Id, nearest.Id) < 0))
            {
                nearest = foe;
                nearestDistance = distance;
            }
        }
        return nearest;
    }

    private static IReadOnlyList<CombatantState> FoesWithinRange(
        CombatantState actor,
        IReadOnlyList<CombatantState> foes)
    {
        var eligible = new List<CombatantState>();
        foreach (CombatantState foe in foes)
        {
            if (foe.IsAlive && actor.Spatial.IsWithinAttackRange(foe.Spatial))
                eligible.Add(foe);
        }
        return eligible;
    }

    private void EvaluateOutcome()
    {
        if (Outcome != CombatOutcome.InProgress) return;
        if (CountAlive(_enemies) == 0) Outcome = CombatOutcome.PartyVictory;
        else if (CountAlive(_party) == 0) Outcome = CombatOutcome.PartyDefeated;
    }

    private void Record(CombatLogKind kind, string actorId, string? targetId, string detail) =>
        _log.Add(new CombatLogEntry(Step, kind, actorId, targetId, detail));

    private void Record(
        CombatLogKind kind,
        string actorId,
        string? targetId,
        string detail,
        CombatImpact impact) =>
        _log.Add(new CombatLogEntry(Step, kind, actorId, targetId, detail, Impact: impact));

    private void BeginIfNeeded()
    {
        if (_began) return;
        _began = true;
        Record(CombatLogKind.EncounterBegan, EncounterId, null,
            $"{CountAlive(_party)} vs {CountAlive(_enemies)}");
        EvaluateOutcome();
    }

    private void EndIfNeeded()
    {
        if (_ended || Outcome == CombatOutcome.InProgress) return;
        _ended = true;
        Record(CombatLogKind.EncounterEnded, EncounterId, null, Outcome.ToString());
    }

    private static int CountAlive(IReadOnlyList<CombatantState> combatants)
    {
        int alive = 0;
        foreach (CombatantState combatant in combatants)
        {
            if (combatant.IsAlive) alive++;
        }
        return alive;
    }
}
