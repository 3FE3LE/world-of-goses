using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>Shared player-facing formatter for the simulation clock.</summary>
public static class SimulationTimeText
{
    private const int MinutesPerHour = 60;
    private const int HoursPerDay = 24;
    private const int MinutesPerDay = MinutesPerHour * HoursPerDay;

    public static string Format(int tick)
    {
        int day = GameClock.DayNumber(tick);
        double totalHours = GameClock.DayFraction(tick) * 24d;
        int hour = (int)totalHours;
        int minute = (int)((totalHours - hour) * 60d);
        return $"Day {day} · {hour:00}:{minute:00}";
    }

    public static string FormatLocalized(int tick)
    {
        int day = GameClock.DayNumber(tick);
        double totalHours = GameClock.DayFraction(tick) * 24d;
        int hour = (int)totalHours;
        int minute = (int)((totalHours - hour) * 60d);
        return Ui.UiText.Format("ui.time.day", day, hour, minute);
    }

    /// <summary>
    /// Converts an internal tick count into player-facing world time. UI copy
    /// must use this helper instead of exposing simulation ticks or treating
    /// them as real-world seconds.
    /// </summary>
    public static string FormatDuration(int tickCount) =>
        FormatDurationParts(tickCount, LocalizeEnglishUnit, " ");

    public static string FormatDurationLocalized(int tickCount) =>
        FormatDurationParts(
            tickCount,
            FormatLocalizedUnit,
            Ui.UiText.Get("ui.duration.separator"));

    private static string FormatDurationParts(
        int tickCount,
        System.Func<string, int, string> formatUnit,
        string separator)
    {
        long safeTicks = System.Math.Max(0, tickCount);
        int totalMinutes = safeTicks == 0
            ? 0
            : (int)System.Math.Max(
                1,
                (safeTicks * MinutesPerDay + GameClock.TicksPerInGameDay - 1)
                    / GameClock.TicksPerInGameDay);
        int days = totalMinutes / MinutesPerDay;
        int remainingMinutes = totalMinutes % MinutesPerDay;
        int hours = remainingMinutes / MinutesPerHour;
        int minutes = remainingMinutes % MinutesPerHour;

        if (days > 0)
        {
            string dayText = formatUnit("day", days);
            return hours > 0
                ? dayText + separator + formatUnit("hour", hours)
                : dayText;
        }
        if (hours > 0)
        {
            string hourText = formatUnit("hour", hours);
            return minutes > 0
                ? hourText + separator + formatUnit("minute", minutes)
                : hourText;
        }
        return formatUnit("minute", minutes);
    }

    private static string LocalizeEnglishUnit(string unit, int value) =>
        $"{value} {unit}{(value == 1 ? string.Empty : "s")}";

    private static string FormatLocalizedUnit(string unit, int value)
    {
        return (unit, value == 1) switch
        {
            ("day", true) => Ui.UiText.Format("ui.duration.day.one", value),
            ("day", false) => Ui.UiText.Format("ui.duration.day.many", value),
            ("hour", true) => Ui.UiText.Format("ui.duration.hour.one", value),
            ("hour", false) => Ui.UiText.Format("ui.duration.hour.many", value),
            ("minute", true) => Ui.UiText.Format("ui.duration.minute.one", value),
            _ => Ui.UiText.Format("ui.duration.minute.many", value),
        };
    }
}
