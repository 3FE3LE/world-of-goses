using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>Shared player-facing formatter for the simulation clock.</summary>
public static class SimulationTimeText
{
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
}
