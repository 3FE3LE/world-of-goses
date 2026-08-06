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
    /// Duration and threshold for a status, from central balance. Only Stunning
    /// and Knockdown are behavioural in this slice; the rest are representable so
    /// content can reference them without inventing four more systems.
    /// </summary>
    public StatusEffect Create(StatusEffectId id, string sourceId, string targetId, int step)
    {
        (int duration, int threshold) = id switch
        {
            StatusEffectId.Stunning =>
                (_balance.StunningDurationSteps, _balance.StunningInterruptThreshold),
            StatusEffectId.Knockdown =>
                (_balance.KnockdownDurationSteps, _balance.KnockdownThreshold),
            _ => (1, 1),
        };
        return new StatusEffect(id, sourceId, targetId, 1, duration, threshold, step);
    }

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
                next.Add(existing
                    .WithStacks(existing.Stacks + incoming.Stacks)
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
    public bool PreventsAction(IReadOnlyList<StatusEffect> current) =>
        IsActive(current, StatusEffectId.Stunning) || IsActive(current, StatusEffectId.Knockdown);

    /// <summary>
    /// Factor applied to the target's mitigation while it is knocked down. This is
    /// how Knockdown alters exposure without introducing free movement into a
    /// combat model that has no positions to move between.
    /// </summary>
    public double MitigationScale(IReadOnlyList<StatusEffect> current) =>
        IsActive(current, StatusEffectId.Knockdown) ? _balance.KnockdownMitigationScale : 1.0;
}
