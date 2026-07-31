#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WorldofGoses.Domain;

/// <summary>
/// Renders an <see cref="EarlyGameMetrics"/> as plain text so a play session
/// yields EG-A0 validation data instead of only a signature.
///
/// <para>Deliberately not localized and not a UI surface. This is a diagnostic
/// artifact read by whoever is calibrating the early game, in the same spirit
/// as the visual-regression captures: it exists to be read once against
/// <c>docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md</c> and thrown away.
/// Putting it in the player-facing UI would mean translating and maintaining a
/// screen full of numbers that mean nothing to a player.</para>
/// </summary>
public static class EarlyGameMetricsReport
{
    /// <summary>
    /// Formats the measurement. Every derived figure states the raw numbers it
    /// came from, so a surprising ratio can be checked rather than trusted.
    /// </summary>
    /// <param name="currentTick">The world's tick, used only to tell a run
    /// that has not reached its first dawn apart from one that was never
    /// instrumented. Pass -1 when no world is available.</param>
    public static string Format(EarlyGameMetrics metrics, int currentTick = -1)
    {
        var text = new StringBuilder();
        text.AppendLine("EG-0 opening measurement");
        text.AppendLine("========================");

        if (metrics.DawnSamples == 0)
        {
            // Two very different situations produce zero samples, and reading
            // one as the other wastes a measurement run. A young city simply
            // has not crossed 08:00 yet; a migrated city never will, because
            // its opening happened before the instrumentation existed.
            text.AppendLine("No dawn has been sampled yet, which means either:");
            text.AppendLine();
            if (currentTick >= 0 && currentTick < GameClock.WorkdayStartTick)
            {
                int remaining = GameClock.WorkdayStartTick - currentTick;
                text.AppendLine(
                    $"  This run has not reached its first dawn. Tick is"
                    + $" {currentTick}; the first sample lands at tick"
                    + $" {GameClock.WorkdayStartTick} (08:00 in-game), in"
                    + $" {remaining} ticks — about {remaining / 60} real"
                    + " minutes at 1x. Keep playing.");
            }
            else
            {
                text.AppendLine(
                    $"  This run is past its first dawn (tick {currentTick},"
                    + $" dawn at {GameClock.WorkdayStartTick}) yet nothing was"
                    + " sampled, so this city was migrated from a save written"
                    + " before the instrumentation existed. It will stay empty."
                    + " Measure a run started from a clean slot instead.");
            }
            return text.ToString();
        }

        text.AppendLine(Line("In-game days observed", metrics.DawnSamples));
        text.AppendLine(Line(
            "Time to first shelter",
            metrics.FirstShelterCompletedAtTick is int shelterTick
                ? $"{shelterTick} ticks ({Days(shelterTick)} in-game days)"
                : "not built yet"));

        text.AppendLine();
        text.AppendLine("Food horizon (days of ration the stock covers)");
        text.AppendLine(Line(
            "  Tightest moment",
            Tenths(metrics.MinFoodHorizonTenths)));
        text.AppendLine(Line(
            "  At the first shelter",
            Tenths(metrics.FoodHorizonTenthsAtFirstShelter)));

        text.AppendLine();
        text.AppendLine("Idle time (sampled at dawn, in citizen-days)");
        text.AppendLine(Line("  Idle", metrics.IdleCitizenDays));
        text.AppendLine(Line("  Observed", metrics.ObservedCitizenDays));
        text.AppendLine(Line("  Share idle", Share(
            metrics.IdleCitizenDays,
            metrics.ObservedCitizenDays)));

        text.AppendLine();
        text.AppendLine("Expeditions");
        text.AppendLine(Line("  Dispatched", metrics.ExpeditionsDispatched));
        text.AppendLine(Line(
            "  First dispatched at",
            metrics.FirstExpeditionDispatchedAtTick is int expeditionTick
                ? $"{expeditionTick} ticks ({Days(expeditionTick)} in-game days)"
                : "never"));
        text.AppendLine(Line(
            "  Absence (opportunity cost)",
            $"{metrics.ExpeditionAbsenceTicks} citizen-ticks"));

        text.AppendLine();
        AppendFlow(text, "Resources gathered", metrics.Gathered);
        text.AppendLine();
        AppendFlow(text, "Resources spent", metrics.Consumed);
        return text.ToString();
    }

    private static void AppendFlow(
        StringBuilder text,
        string title,
        IReadOnlyDictionary<ResourceType, int> flow)
    {
        text.AppendLine(title);
        if (flow.Count == 0)
        {
            text.AppendLine("  (none)");
            return;
        }
        foreach (KeyValuePair<ResourceType, int> entry in flow)
        {
            text.AppendLine(Line($"  {entry.Key}", entry.Value));
        }
    }

    private static string Line(string label, int value) =>
        Line(label, value.ToString(CultureInfo.InvariantCulture));

    private static string Line(string label, string value) =>
        $"{label.PadRight(30)} {value}";

    private static string Days(int ticks) =>
        ((double)ticks / GameClock.TicksPerInGameDay)
            .ToString("0.0", CultureInfo.InvariantCulture);

    private static string Tenths(int? tenths) =>
        tenths is int value
            ? (value / 10.0).ToString("0.0", CultureInfo.InvariantCulture)
            : "not sampled";

    private static string Share(int part, int whole) =>
        whole <= 0
            ? "n/a"
            : (100.0 * part / whole).ToString("0.0", CultureInfo.InvariantCulture) + "%";
}
