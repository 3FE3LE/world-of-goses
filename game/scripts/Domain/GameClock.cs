namespace WorldofGoses.Domain;

/// <summary>
/// World time-of-day parameters. The simulation has one shared
/// "world tick" counter that monotonically increases; the time of
/// day is derived from it via <see cref="IsDaytime"/> and
/// <see cref="DayFraction"/>.
///
/// <para>
/// At 1 Hz (the default tick rate), one in-game day lasts
/// <see cref="TicksPerInGameDay"/> real seconds, so
/// <see cref="TicksPerInGameDay"/> = 3600 means "1 real hour per
/// in-game day". All values are provisional tuning — see
/// <c>docs/PRODUCT_DIRECTION.md §5</c>.
/// </para>
/// </summary>
public static class GameClock
{
    /// <summary>Total ticks in one in-game day. 3600 = 1 hour at 1 Hz.</summary>
    public const int TicksPerInGameDay = 3600;

    /// <summary>
    /// Provisional start of the configured workday, expressed as a tick within
    /// the in-game day. Kept explicit so presentation and routine scheduling do
    /// not infer labour policy from a lighting constant. 08:00 = 8 hours ×
    /// (3600 ticks / 24 h) = 1200 ticks; the visual day/night cycle remains
    /// the whole 24 h so the city still ticks overnight, only the labour
    /// window has been pushed to a sensible morning-to-afternoon band.
    /// </summary>
    public const int WorkdayStartTick = 1200;

    /// <summary>Exclusive end of the provisional workday (16:00).</summary>
    public const int WorkdayEndTick = 2400;

    /// <summary>Ticks during which workers are considered "working" (day).</summary>
    public const int DayTicks = WorkdayEndTick - WorkdayStartTick;

    /// <summary>Ticks during which workers are considered "resting" (night).</summary>
    public const int NightTicks = TicksPerInGameDay - DayTicks;

    /// <summary>True when the given world tick falls inside the day portion.</summary>
    public static bool IsDaytime(int tick) => IsWorkday(tick);

    /// <summary>True while the centralized work schedule is active.</summary>
    public static bool IsWorkday(int tick)
    {
        int dayTick = TickWithinDay(tick);
        return dayTick >= WorkdayStartTick && dayTick < WorkdayEndTick;
    }

    /// <summary>Next absolute tick at which the workday starts.</summary>
    public static int NextWorkdayStart(int tick)
    {
        int dayStart = tick - TickWithinDay(tick);
        int candidate = dayStart + WorkdayStartTick;
        return candidate > tick ? candidate : candidate + TicksPerInGameDay;
    }

    /// <summary>Next absolute tick at which the current workday ends.</summary>
    public static int NextWorkdayEnd(int tick)
    {
        int dayStart = tick - TickWithinDay(tick);
        int candidate = dayStart + WorkdayEndTick;
        return candidate > tick ? candidate : candidate + TicksPerInGameDay;
    }

    public static int TickWithinDay(int tick) =>
        ((tick % TicksPerInGameDay) + TicksPerInGameDay) % TicksPerInGameDay;

    /// <summary>
    /// Current position through the in-game day, in [0.0, 1.0).
    /// Useful for UI indicators ("Day 1 · 12:30") and for
    /// proportional calculations.
    /// </summary>
    public static double DayFraction(int tick)
    {
        int mod = TickWithinDay(tick);
        return (double)mod / TicksPerInGameDay;
    }

    /// <summary>
    /// Which in-game day has just completed at the given tick.
    /// Day numbers start at 1 (tick 0 is day 1, tick 3600 is day 2).
    /// </summary>
    public static int DayNumber(int tick) =>
        (tick / TicksPerInGameDay) + 1;
}
