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

    /// <summary>
    /// Progress-liveness rule. <c>DEC-0011</c> defines treatment as Basic
    /// Shelter + time + an explicit resource cost, so a city that has neither
    /// the shelter nor the edible stock the cost names has, by construction,
    /// no route out of a durable injury: the wound itself makes its carrier
    /// unavailable for the gathering, building and expedition work that would
    /// otherwise produce both. The proposal's §8.2 "the first guided sortie
    /// cannot silently kill or irreversibly trap the city" and §16 "prevent
    /// soft lock" therefore forbid creating the wound in the first place.
    ///
    /// <para>
    /// This is a gate on <em>inflicting</em> a wound, never on treating one.
    /// A city that already has a shelter and the food keeps the ordinary
    /// rules unchanged: the setback wounds, and treatment still costs
    /// <see cref="FoodCostFor"/>.
    /// </para>
    /// </summary>
    /// <param name="severity">Severity the caller is about to inflict.</param>
    /// <param name="hasTreatmentShelter">Whether a completed Basic Shelter exists.</param>
    /// <param name="edibleStock">Unreserved Food + Wild Food the city holds.</param>
    public static bool CanCityCarryWound(
        WoundSeverity severity,
        bool hasTreatmentShelter,
        int edibleStock) =>
        hasTreatmentShelter && edibleStock >= FoodCostFor(severity);

    public static WoundSeverity SeverityFor(Citizen citizen) =>
        citizen.CurrentStamina * 100 / citizen.MaxStamina <= 25
            ? WoundSeverity.Severe
            : WoundSeverity.Moderate;
}
