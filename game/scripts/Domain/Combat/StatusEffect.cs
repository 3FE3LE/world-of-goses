#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain.Combat;

/// <summary>
/// The physical consequences a technique can inflict. These mirror
/// <see cref="PhysicalExpression"/> so the domain can represent all six, while
/// only Stunning and Knockdown carry behaviour in this slice.
/// </summary>
public enum StatusEffectId
{
    Stunning,
    Knockdown,
    Fracture,
    Bleeding,
    Poisoning,
    Paralysis,
}

public enum StatusExpirationRule
{
    /// <summary>Loses one step of duration per encounter step.</summary>
    DecayPerStep,

    /// <summary>Consumed the moment it prevents an action.</summary>
    ConsumedOnTrigger,
}

/// <summary>
/// One applied status. Immutable: the resolver produces a new instance rather
/// than mutating, so a CombatLog entry keeps the value it recorded.
/// </summary>
public sealed record StatusEffect
{
    public StatusEffect(
        StatusEffectId id,
        string sourceId,
        string targetId,
        int stacks,
        int duration,
        int threshold,
        int appliedAtStep,
        StatusExpirationRule expirationRule = StatusExpirationRule.DecayPerStep)
    {
        if (!Enum.IsDefined(id)) throw new ArgumentOutOfRangeException(nameof(id));
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Source is required.", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(targetId)) throw new ArgumentException("Target is required.", nameof(targetId));
        if (stacks <= 0) throw new ArgumentOutOfRangeException(nameof(stacks));
        if (duration <= 0) throw new ArgumentOutOfRangeException(nameof(duration));
        if (threshold <= 0) throw new ArgumentOutOfRangeException(nameof(threshold));
        if (appliedAtStep < 0) throw new ArgumentOutOfRangeException(nameof(appliedAtStep));
        if (!Enum.IsDefined(expirationRule)) throw new ArgumentOutOfRangeException(nameof(expirationRule));

        Id = id;
        SourceId = sourceId;
        TargetId = targetId;
        Stacks = stacks;
        Duration = duration;
        Threshold = threshold;
        AppliedAtStep = appliedAtStep;
        ExpirationRule = expirationRule;
    }

    public StatusEffectId Id { get; }
    public string SourceId { get; }
    public string TargetId { get; }
    public int Stacks { get; }
    public int Duration { get; }
    public int Threshold { get; }
    public int AppliedAtStep { get; }
    public StatusExpirationRule ExpirationRule { get; }

    /// <summary>True once the accumulated stacks reach the threshold.</summary>
    public bool IsActive => Stacks >= Threshold;

    public StatusEffect WithStacks(int stacks) =>
        new(Id, SourceId, TargetId, stacks, Duration, Threshold, AppliedAtStep, ExpirationRule);

    public StatusEffect WithDuration(int duration) =>
        new(Id, SourceId, TargetId, Stacks, duration, Threshold, AppliedAtStep, ExpirationRule);
}

/// <summary>
/// Applies, accumulates, ticks and expires statuses. Pure and deterministic: the
/// same inputs always produce the same collection, and nothing here reads a
/// clock or a frame.
/// </summary>
public sealed class StatusResolver
{
    private readonly CombatBalanceConfig _balance;

    public StatusResolver(CombatBalanceConfig? balance = null)
    {
        _balance = balance ?? CombatBalanceConfig.Default;
        _balance.Validate();
    }

    /// <summary>
    /// Duration and threshold for a status, from central balance.
    /// </summary>
    /// <remarks>
    /// All six are behavioural. They used to fall through to <c>(1, 1)</c> with
    /// only Stunning and Knockdown doing anything, which made the other four
    /// labels: a technique could apply Bleeding and nothing anywhere would read
    /// it. The six now cost different things — see <see cref="PreventsAction"/>,
    /// <see cref="PreventsMovement"/>, <see cref="DamageOverTime"/> and the
    /// end-of-encounter conversion of Fracture.
    /// </remarks>
    public StatusEffect Create(StatusEffectId id, string sourceId, string targetId, int step)
    {
        (int duration, int threshold) = id switch
        {
            StatusEffectId.Stunning =>
                (_balance.StunningDurationSteps, _balance.StunningInterruptThreshold),
            StatusEffectId.Knockdown =>
                (_balance.KnockdownDurationSteps, _balance.KnockdownThreshold),
            StatusEffectId.Paralysis =>
                (_balance.ParalysisDurationSteps, _balance.ParalysisThreshold),
            StatusEffectId.Bleeding =>
                (_balance.BleedingDurationSteps, _balance.BleedingThreshold),
            StatusEffectId.Poisoning =>
                (_balance.PoisoningDurationSteps, _balance.PoisoningThreshold),
            StatusEffectId.Fracture =>
                (_balance.FractureDurationSteps, _balance.FractureThreshold),
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown status effect."),
        };
        return new StatusEffect(id, sourceId, targetId, 1, duration, threshold, step);
    }

    /// <summary>Whether a second application of this status adds a stack.</summary>
    /// <remarks>
    /// Poisoning is the exception: reapplying it refreshes the duration but does
    /// not deepen it. That is the whole shape of the effect — it cannot be piled
    /// on to burst someone down, so its pressure is attrition you have to keep
    /// renewing, which is what distinguishes it from Bleeding.
    /// </remarks>
    public static bool Stacks(StatusEffectId id) => id != StatusEffectId.Poisoning;

    /// <summary>
    /// Adds a status, stacking onto an existing one of the same id and refreshing
    /// its duration rather than creating a duplicate entry.
    /// </summary>
    public IReadOnlyList<StatusEffect> Apply(
        IReadOnlyList<StatusEffect> current,
        StatusEffect incoming)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(incoming);
        var next = new List<StatusEffect>(current.Count + 1);
        bool merged = false;
        foreach (StatusEffect existing in current)
        {
            if (!merged && existing.Id == incoming.Id && existing.TargetId == incoming.TargetId)
            {
                int stacks = Stacks(incoming.Id)
                    ? existing.Stacks + incoming.Stacks
                    : existing.Stacks;
                next.Add(existing
                    .WithStacks(stacks)
                    .WithDuration(Math.Max(existing.Duration, incoming.Duration)));
                merged = true;
                continue;
            }
            next.Add(existing);
        }
        if (!merged) next.Add(incoming);
        return next;
    }

    /// <summary>
    /// Advances every status one step and drops the expired ones. Statuses whose
    /// rule is <see cref="StatusExpirationRule.ConsumedOnTrigger"/> are not decayed
    /// here; the consumer removes them via <see cref="Consume"/>.
    /// </summary>
    public IReadOnlyList<StatusEffect> Tick(IReadOnlyList<StatusEffect> current)
    {
        ArgumentNullException.ThrowIfNull(current);
        var next = new List<StatusEffect>(current.Count);
        foreach (StatusEffect status in current)
        {
            if (status.ExpirationRule == StatusExpirationRule.ConsumedOnTrigger)
            {
                next.Add(status);
                continue;
            }
            int duration = status.Duration - 1;
            if (duration <= 0) continue;
            next.Add(status.WithDuration(duration));
        }
        return next;
    }

    /// <summary>Removes one status by id, used when a status spends itself.</summary>
    public IReadOnlyList<StatusEffect> Consume(
        IReadOnlyList<StatusEffect> current,
        StatusEffectId id)
    {
        ArgumentNullException.ThrowIfNull(current);
        var next = new List<StatusEffect>(current.Count);
        bool removed = false;
        foreach (StatusEffect status in current)
        {
            if (!removed && status.Id == id)
            {
                removed = true;
                continue;
            }
            next.Add(status);
        }
        return next;
    }

    public bool IsActive(IReadOnlyList<StatusEffect> current, StatusEffectId id)
    {
        ArgumentNullException.ThrowIfNull(current);
        foreach (StatusEffect status in current)
        {
            if (status.Id == id && status.IsActive) return true;
        }
        return false;
    }

    /// <summary>
    /// Stunning interrupts: an affected combatant loses this step's action.
    /// Knockdown also costs the turn and additionally exposes the target, which
    /// <see cref="MitigationScale"/> reports.
    /// </summary>
    /// <summary>
    /// Whether the combatant loses its action this step.
    /// </summary>
    /// <remarks>
    /// Stunning and Knockdown both cost the action, and that is deliberate — the
    /// difference between them is what else they cost. Stunning takes the action
    /// and nothing more: a stunned combatant holds its ground. Knockdown takes
    /// the action <em>and</em> the position, because it is the one that throws
    /// the target and leaves it prone.
    /// </remarks>
    public bool PreventsAction(IReadOnlyList<StatusEffect> current) =>
        IsActive(current, StatusEffectId.Stunning) || IsActive(current, StatusEffectId.Knockdown);

    /// <summary>
    /// Whether the combatant is held where it stands.
    /// </summary>
    /// <remarks>
    /// Knockdown only. Paralysis used to root outright here, which made it read
    /// as a third interrupt; it now scales the advance through
    /// <see cref="Modifiers"/> instead — a severe slow rather than a stop — and
    /// pays for its offensive value with a chance to seize the action. Being on
    /// the floor is the one thing that truly stops movement.
    /// </remarks>
    public bool PreventsMovement(IReadOnlyList<StatusEffect> current) =>
        IsActive(current, StatusEffectId.Knockdown);

    /// <summary>
    /// Health the statuses cost this step, split by what can reduce it.
    /// </summary>
    /// <remarks>
    /// The split is the point. Bleeding is a physical wound: it scales with how
    /// deep the cut went — its stacks — and armour still counts, so the caller
    /// mitigates it. Poisoning is elemental corruption: a flat trickle that no
    /// mitigation touches, which is why it is the smaller of the two per step and
    /// the only one that refuses to stack.
    /// </remarks>
    public (double Mitigable, double Unmitigable) DamageOverTime(
        IReadOnlyList<StatusEffect> current)
    {
        ArgumentNullException.ThrowIfNull(current);
        double mitigable = 0;
        double unmitigable = 0;
        foreach (StatusEffect status in current)
        {
            if (!status.IsActive) continue;
            if (status.Id == StatusEffectId.Bleeding)
                mitigable += _balance.BleedingDamagePerStack * status.Stacks;
            else if (status.Id == StatusEffectId.Poisoning)
                unmitigable += _balance.PoisoningDamagePerStep;
        }
        return (mitigable, unmitigable);
    }

    /// <summary>
    /// Everything the active statuses multiply, gathered in one read.
    /// </summary>
    /// <remarks>
    /// This replaced a single <c>MitigationScale</c> that only Knockdown moved,
    /// and that only ever scaled both mitigations together. Once each expression
    /// had to earn its place offensively, one number stopped being enough:
    /// Fracture opens a physical window, Stunning an elemental one, and the two
    /// have to be separable or the expressions collapse back into each other.
    /// </remarks>
    public StatusModifiers Modifiers(IReadOnlyList<StatusEffect> current)
    {
        ArgumentNullException.ThrowIfNull(current);
        double physical = 1.0;
        double elemental = 1.0;
        double damageTaken = 1.0;
        double movement = 1.0;

        if (IsActive(current, StatusEffectId.Knockdown))
        {
            // Prone: exposed to everything, because nothing is being guarded.
            physical *= _balance.KnockdownMitigationScale;
            elemental *= _balance.KnockdownMitigationScale;
        }
        if (IsActive(current, StatusEffectId.Fracture))
        {
            physical *= _balance.FracturePhysicalMitigationScale;
        }
        if (IsActive(current, StatusEffectId.Stunning))
        {
            elemental *= _balance.StunningElementalMitigationScale;
        }
        if (IsActive(current, StatusEffectId.Paralysis))
        {
            movement *= _balance.ParalysisMovementSpeedScale;
        }
        if (IsActive(current, StatusEffectId.Poisoning))
        {
            damageTaken *= _balance.PoisoningDamageTakenScale;
        }

        return new StatusModifiers(physical, elemental, damageTaken, movement);
    }

    /// <summary>
    /// Whether Paralysis seizes the action this step. Rolled, not certain.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="PreventsAction"/> because it needs the
    /// encounter's random source, and because it is a different promise: the two
    /// statuses in <c>PreventsAction</c> always cost the action, this one
    /// sometimes does.
    /// </remarks>
    public bool ParalysisSeizesAction(IReadOnlyList<StatusEffect> current, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return IsActive(current, StatusEffectId.Paralysis)
            && random.NextDouble() < _balance.ParalysisActionLossChance;
    }
}

/// <summary>
/// The multipliers the active statuses impose on a combatant.
/// </summary>
/// <remarks>
/// Every field is a factor around 1.0, so an absent status is the identity and
/// two statuses compose by multiplication rather than by whichever the caller
/// happens to check first.
/// </remarks>
public readonly record struct StatusModifiers(
    double PhysicalMitigationScale,
    double ElementalMitigationScale,
    double DamageTakenScale,
    double MovementSpeedScale)
{
    public static StatusModifiers None { get; } = new(1, 1, 1, 1);
}
