#nullable enable
using System;
using System.Collections.Generic;
using WorldofGoses.Domain.Combat;

namespace WorldofGoses.Ui;

/// <summary>
/// The two things about a striker a hit reaction needs: how hard they hit and
/// which side of the target they were standing on.
/// </summary>
public readonly record struct Striker(double Impulse, double ScreenX);

/// <summary>
/// How far a struck combatant is shoved on screen, and for how long.
/// </summary>
/// <remarks>
/// <para>
/// Only a blow that lands Knockdown moves anyone in the domain — displacement
/// is a consequence of a physical expression, not of any hit that connects.
/// That is correct, and on its own it made combat look inert: a spear thrust to
/// the chest left the target standing exactly where it was.
/// </para>
/// <para>
/// This is the missing half. It is a <b>hit reaction</b>: transient, entirely
/// presentational, and it always decays back to where the domain says the figure
/// is. Nothing here is read by the simulation, an unobserved encounter resolves
/// identically, and a save reloaded mid-fight puts everyone back on their
/// authoritative position with no reaction in flight.
/// </para>
/// <para>
/// It is a plain static class rather than logic inside <c>CombatantView</c>
/// because the view is a Godot <c>Control</c> and cannot be run in a test. The
/// magnitudes are the part worth asserting.
/// </para>
/// </remarks>
public static class HitReaction
{
    /// <summary>Screen pixels a fully physical blow shoves at parity.</summary>
    /// <remarks>
    /// PROVISIONAL. Small on purpose: this reads as a flinch, not as the
    /// knockback Knockdown produces, and the two must stay visibly different or
    /// the expression loses the thing that makes it worth building around.
    /// </remarks>
    public const double MaximumShovePixels = 7.0;

    /// <summary>Seconds the shove takes to decay back to the domain position.</summary>
    /// <remarks>
    /// Deliberately shorter than <c>CombatantView.InterpolationSeconds</c>, so a
    /// reaction resolves inside the step that caused it and never leaks into the
    /// next one — a flinch that outlives its blow reads as drift.
    /// </remarks>
    public const double DecaySeconds = 0.12;

    /// <summary>
    /// Shove in screen pixels for one blow, unsigned.
    /// </summary>
    /// <param name="physicalShare">
    /// How much of the blow came from the body, in <c>[0, 1]</c>. A purely
    /// elemental blast transfers no momentum and so does not shove at all — the
    /// same rule the domain applies to a real knockback.
    /// </param>
    /// <param name="attackerImpulse">The striker's Impulse.</param>
    /// <param name="targetStability">The struck combatant's Stability.</param>
    public static double ShovePixels(
        double physicalShare,
        double attackerImpulse,
        double targetStability)
    {
        if (!double.IsFinite(physicalShare) || physicalShare <= 0) return 0;

        double impulse = Math.Max(0, attackerImpulse);
        double stability = Math.Max(0, targetStability);
        double resistance = impulse + stability;
        // Both unmeasured is the assembled-from-bare-parts case: no opinion
        // about the exchange, so no opinion about how it looked.
        if (resistance <= 0) return 0;

        return MaximumShovePixels
            * Math.Clamp(physicalShare, 0, 1)
            * (impulse / resistance);
    }

    /// <summary>
    /// The shove as a signed screen offset, away from the attacker.
    /// </summary>
    /// <param name="attackerScreenX">Attacker's screen X.</param>
    /// <param name="targetScreenX">Struck combatant's screen X.</param>
    /// <remarks>
    /// Direction comes from where the two are drawn rather than from facing: a
    /// combatant struck from behind is shoved forward, which is what a blow
    /// does, and reading facing here would have shoved them into it.
    /// </remarks>
    public static double SignedShovePixels(
        double physicalShare,
        double attackerImpulse,
        double targetStability,
        double attackerScreenX,
        double targetScreenX)
    {
        double magnitude = ShovePixels(physicalShare, attackerImpulse, targetStability);
        if (magnitude <= 0) return 0;
        // Exactly co-located is a real state — melee bodies overlap — and has no
        // away direction. Falling back to +1 keeps it deterministic rather than
        // letting a floating-point tie decide.
        double direction = targetScreenX < attackerScreenX ? -1 : 1;
        return magnitude * direction;
    }

    /// <summary>
    /// Total shove one combatant takes from everything that hit it this step.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Blows are summed rather than the largest taken, so two attackers on
    /// opposite sides of one target cancel — which is what being caught between
    /// two people looks like, and reads as pinned rather than as the target
    /// arbitrarily favouring whoever the loop saw last.
    /// </para>
    /// <para>
    /// An evaded blow shoves nothing: it did not connect. Neither does a blow
    /// that got through and dealt nothing, which is a hit fully absorbed.
    /// </para>
    /// </remarks>
    public static double ForEvents(
        string targetId,
        double targetStability,
        double targetScreenX,
        IReadOnlyList<CombatLogEntry> events,
        IReadOnlyDictionary<string, Striker> strikers)
    {
        ArgumentNullException.ThrowIfNull(targetId);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(strikers);

        double total = 0;
        foreach (CombatLogEntry entry in events)
        {
            if (entry.TargetId != targetId) continue;
            if (entry.Resolution is not { } resolution) continue;
            if (resolution.Evaded || resolution.FinalResult <= 0) continue;
            if (!strikers.TryGetValue(entry.ActorId, out Striker striker)) continue;

            total += SignedShovePixels(
                resolution.PhysicalShare,
                striker.Impulse,
                targetStability,
                striker.ScreenX,
                targetScreenX);
        }
        return total;
    }

    /// <summary>
    /// How much of the shove is still present after <paramref name="elapsedSeconds"/>.
    /// </summary>
    /// <remarks>
    /// Quadratic ease-out: the figure is thrown and settles, rather than sliding
    /// back linearly like something being dragged. Reaches exactly zero at
    /// <see cref="DecaySeconds"/>, so the figure lands on the domain position
    /// and not near it.
    /// </remarks>
    public static double Remaining(double shovePixels, double elapsedSeconds)
    {
        if (elapsedSeconds <= 0) return shovePixels;
        if (elapsedSeconds >= DecaySeconds) return 0;
        double remaining = 1 - (elapsedSeconds / DecaySeconds);
        return shovePixels * remaining * remaining;
    }
}
