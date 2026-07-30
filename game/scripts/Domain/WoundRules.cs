using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Provisional tuning for the first persistent-wound slice. Wounds remain a
/// separate condition while severity limits the stamina a citizen can use.
/// </summary>
public static class WoundRules
{
    public const int ModerateEffectiveStaminaPercent = 75;
    public const int SevereEffectiveStaminaPercent = 50;
    public const int ModerateRecoveryTicks = GameClock.TicksPerInGameDay;
    public const int SevereRecoveryTicks = GameClock.TicksPerInGameDay * 2;
    public const int ModerateFoodCost = 1;
    public const int SevereFoodCost = 2;

    public static int EffectiveStaminaCap(int maximumStamina, WoundSeverity severity)
    {
        int percent = severity switch
        {
            WoundSeverity.Moderate => ModerateEffectiveStaminaPercent,
            WoundSeverity.Severe => SevereEffectiveStaminaPercent,
            _ => throw new ArgumentOutOfRangeException(nameof(severity)),
        };
        return Math.Max(1, maximumStamina * percent / 100);
    }

    public static int RecoveryTicksFor(WoundSeverity severity) => severity switch
    {
        WoundSeverity.Moderate => ModerateRecoveryTicks,
        WoundSeverity.Severe => SevereRecoveryTicks,
        _ => throw new ArgumentOutOfRangeException(nameof(severity)),
    };

    public static int FoodCostFor(WoundSeverity severity) => severity switch
    {
        WoundSeverity.Moderate => ModerateFoodCost,
        WoundSeverity.Severe => SevereFoodCost,
        _ => throw new ArgumentOutOfRangeException(nameof(severity)),
    };

    public static WoundSeverity SeverityFor(Citizen citizen) =>
        citizen.CurrentStamina * 100 / citizen.MaxStamina <= 25
            ? WoundSeverity.Severe
            : WoundSeverity.Moderate;
}
