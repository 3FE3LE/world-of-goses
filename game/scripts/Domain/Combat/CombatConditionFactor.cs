#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain.Combat;

/// <summary>
/// The individual causes behind a citizen's ConditionFactor. The roadmap requires
/// the UI to show the causes, not just the product, so the derivation returns them
/// instead of a bare number.
/// </summary>
public sealed record ConditionFactorBreakdown(
    double HealthComponent,
    double FatigueComponent,
    double InjuryComponent,
    double Raw,
    double Value,
    bool WasClamped,
    IReadOnlyList<string> Causes);

/// <summary>
/// Derives ConditionFactor from persistent causes — current health, accumulated
/// fatigue and carried injuries — instead of letting a caller assign an arbitrary
/// number. The result is clamped into the range the statistics system accepts.
///
/// <para>PROVISIONAL BALANCE: weights and injury penalties live here and in
/// <see cref="CombatBalanceConfig"/>.</para>
/// </summary>
public static class CombatConditionFactor
{
    /// <summary>Penalty per injury kind. Incapacitation dominates.</summary>
    public static double PenaltyFor(InjuryKind injury) => injury switch
    {
        InjuryKind.Contusion => 0.04,
        InjuryKind.OpenWound => 0.12,
        // Between an open wound and incapacitation: it does not take a citizen
        // out of the roster, but it is the one injury that follows them home.
        InjuryKind.Fracture => 0.20,
        InjuryKind.TemporaryIncapacitation => 0.30,
        _ => 0.0,
    };

    public static ConditionFactorBreakdown Derive(
        double currentHealth,
        double maxHealth,
        double fatigue,
        IReadOnlyList<InjuryKind> injuries,
        StatisticsBalanceConfig? stats = null,
        CombatBalanceConfig? combat = null)
    {
        ArgumentNullException.ThrowIfNull(injuries);
        StatisticsBalanceConfig statsConfig = stats ?? StatisticsBalanceConfig.Default;
        CombatBalanceConfig combatConfig = combat ?? CombatBalanceConfig.Default;
        statsConfig.Validate();
        combatConfig.Validate();

        var causes = new List<string>();

        double healthRatio = maxHealth <= 0 ? 0 : Math.Clamp(currentHealth / maxHealth, 0, 1);
        // Full health is neutral; being at half health costs a quarter of condition.
        double healthComponent = 0.5 + 0.5 * healthRatio;
        if (healthRatio < 1.0) causes.Add($"health {healthRatio:P0}");

        double fatigueRatio = Math.Clamp(fatigue / combatConfig.FatigueForMinimumCondition, 0, 1);
        double fatigueComponent = 1.0 - 0.4 * fatigueRatio;
        if (fatigue > 0) causes.Add($"fatigue {fatigue:0.#}");

        double injuryPenalty = 0;
        foreach (InjuryKind injury in injuries)
        {
            injuryPenalty += PenaltyFor(injury);
            causes.Add($"injury {injury}");
        }
        double injuryComponent = Math.Max(0, 1.0 - injuryPenalty);

        double raw = healthComponent * fatigueComponent * injuryComponent;
        double value = Math.Clamp(
            raw,
            statsConfig.MinimumConditionFactor,
            statsConfig.MaximumConditionFactor);
        if (causes.Count == 0) causes.Add("rested and unhurt");

        return new ConditionFactorBreakdown(
            healthComponent,
            fatigueComponent,
            injuryComponent,
            raw,
            value,
            WasClamped: Math.Abs(raw - value) > 1e-9,
            causes);
    }
}
