using System;

namespace WorldofGoses.Domain;

/// <summary>
/// One durable personal injury. It is deliberately independent from stamina:
/// stamina can recover while this condition remains, and only shelter
/// treatment advances <see cref="RecoveryTicksRemaining"/>.
/// </summary>
public sealed class CitizenWound
{
    public WoundSeverity Severity { get; private set; }
    public WorldEventId OriginatingEventId { get; }
    public int RecoveryTicksRemaining { get; private set; }

    public CitizenWound(
        WoundSeverity severity,
        WorldEventId originatingEventId,
        int recoveryTicksRemaining)
    {
        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity));
        }
        if (originatingEventId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(originatingEventId));
        }
        if (recoveryTicksRemaining <= 0
            || recoveryTicksRemaining > WoundRules.RecoveryTicksFor(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(recoveryTicksRemaining));
        }

        Severity = severity;
        OriginatingEventId = originatingEventId;
        RecoveryTicksRemaining = recoveryTicksRemaining;
    }

    internal void WorsenTo(WoundSeverity severity)
    {
        if (severity <= Severity) return;
        Severity = severity;
        RecoveryTicksRemaining = WoundRules.RecoveryTicksFor(severity);
    }

    internal bool AdvanceRecoveryTick()
        => AdvanceRecoveryTicks(1);

    internal bool AdvanceRecoveryTicks(int tickCount)
    {
        if (tickCount <= 0) return false;
        RecoveryTicksRemaining = Math.Max(0, RecoveryTicksRemaining - tickCount);
        return RecoveryTicksRemaining == 0;
    }
}
