using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class GameClockTests
{
    // The configured workday is 08:00–16:00 (1200–2400 ticks). Tests
    // pin the boundaries so a future "let's just shift to 09:00"
    // change cannot land without a human signature.

    [Fact]
    public void WorkdayStart_EqualsEightInGameHours()
    {
        // 8 hours * 150 ticks/hour (3600 ticks/day / 24h) = 1200 ticks.
        const int expected = 8 * (GameClock.TicksPerInGameDay / 24);
        Assert.Equal(expected, GameClock.WorkdayStartTick);
    }

    [Fact]
    public void WorkdayEnd_EqualsSixteenInGameHours()
    {
        // 16 hours * 150 ticks/hour = 2400 ticks.
        const int expected = 16 * (GameClock.TicksPerInGameDay / 24);
        Assert.Equal(expected, GameClock.WorkdayEndTick);
    }

    [Fact]
    public void WorkdayDuration_IsEightInGameHours()
    {
        // 8 in-game hours * (3600 / 24) = 1200 ticks = the new workday span.
        int hours = (GameClock.WorkdayEndTick - GameClock.WorkdayStartTick)
            / (GameClock.TicksPerInGameDay / 24);
        Assert.Equal(8, hours);
    }

    [Fact]
    public void IsDaytime_BeforeWorkdayStart_IsFalse()
    {
        // Tick 0 (00:00) and tick 1199 (07:59) are night. Before the
        // human run found this confusing the test assumed 00:00 was
        // already daytime; the human playtest (2026-07-30) signed off
        // on the 08:00 start.
        Assert.False(GameClock.IsDaytime(0));
        Assert.False(GameClock.IsDaytime(GameClock.WorkdayStartTick - 1));
    }

    [Fact]
    public void IsDaytime_AtWorkdayStart_IsTrue()
    {
        Assert.True(GameClock.IsDaytime(GameClock.WorkdayStartTick));
    }

    [Fact]
    public void IsDaytime_AtWorkdayEnd_IsFalse()
    {
        // End is exclusive: tick 2400 (16:00) is night.
        Assert.False(GameClock.IsDaytime(GameClock.WorkdayEndTick));
    }

    [Fact]
    public void IsDaytime_JustBeforeWorkdayEnd_IsTrue()
    {
        Assert.True(GameClock.IsDaytime(GameClock.WorkdayEndTick - 1));
    }

    [Fact]
    public void IsDaytime_MidNight_IsFalse()
    {
        int midNightTick = GameClock.WorkdayEndTick
            + (GameClock.TicksPerInGameDay - GameClock.WorkdayEndTick) / 2;
        Assert.False(GameClock.IsDaytime(midNightTick));
    }

    [Fact]
    public void IsDaytime_NextDayWorkdayStart_IsTrue()
    {
        Assert.True(GameClock.IsDaytime(
            GameClock.TicksPerInGameDay + GameClock.WorkdayStartTick));
    }

    [Fact]
    public void IsDaytime_HandlesNegativeTicksModularly()
    {
        // Defensive: DayFraction / IsDaytime should not throw on
        // negative ticks (e.g., before world starts). What we assert
        // here is just that the call does not throw.
        var ex1 = Record.Exception(() => GameClock.IsDaytime(-1));
        var ex2 = Record.Exception(() => GameClock.IsDaytime(-GameClock.DayTicks));
        Assert.Null(ex1);
        Assert.Null(ex2);
    }

    [Fact]
    public void DayFraction_AtStart_IsZero()
    {
        Assert.Equal(0.0, GameClock.DayFraction(0));
    }

    [Fact]
    public void DayFraction_AtEndOfDay_IsCloseToOne()
    {
        // Just before the day boundary.
        double frac = GameClock.DayFraction(GameClock.TicksPerInGameDay - 1);
        Assert.True(frac > 0.99);
        Assert.True(frac < 1.0);
    }

    [Fact]
    public void DayFraction_WrapsAroundAtNewDay()
    {
        Assert.Equal(0.0, GameClock.DayFraction(GameClock.TicksPerInGameDay));
        Assert.Equal(0.0, GameClock.DayFraction(GameClock.TicksPerInGameDay * 5));
    }

    [Fact]
    public void DayNumber_StartsAtOne()
    {
        Assert.Equal(1, GameClock.DayNumber(0));
        Assert.Equal(1, GameClock.DayNumber(GameClock.TicksPerInGameDay - 1));
    }

    [Fact]
    public void DayNumber_IncrementsAtBoundary()
    {
        Assert.Equal(2, GameClock.DayNumber(GameClock.TicksPerInGameDay));
        Assert.Equal(3, GameClock.DayNumber(GameClock.TicksPerInGameDay * 2));
    }
}
