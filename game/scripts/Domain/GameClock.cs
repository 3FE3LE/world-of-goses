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

    /// <summary>Ticks during which workers are considered "working" (day).</summary>
    public const int DayTicks = 2400;

    /// <summary>Ticks during which workers are considered "resting" (night).</summary>
    public const int NightTicks = TicksPerInGameDay - DayTicks;

    /// <summary>True when the given world tick falls inside the day portion.</summary>
    public static bool IsDaytime(int tick) =>
        ((tick % TicksPerInGameDay) + TicksPerInGameDay) % TicksPerInGameDay < DayTicks;

    /// <summary>
    /// Current position through the in-game day, in [0.0, 1.0).
    /// Useful for UI indicators ("Day 1 · 12:30") and for
    /// proportional calculations.
    /// </summary>
    public static double DayFraction(int tick)
    {
        int mod = ((tick % TicksPerInGameDay) + TicksPerInGameDay) % TicksPerInGameDay;
        return (double)mod / TicksPerInGameDay;
    }

    /// <summary>
    /// Which in-game day has just completed at the given tick.
    /// Day numbers start at 1 (tick 0 is day 1, tick 3600 is day 2).
    /// </summary>
    public static int DayNumber(int tick) =>
        (tick / TicksPerInGameDay) + 1;
}
