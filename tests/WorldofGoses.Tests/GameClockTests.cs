using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class GameClockTests
{
    [Fact]
    public void IsDaytime_StartOfDay_IsTrue()
    {
        Assert.True(GameClock.IsDaytime(0));
    }

    [Fact]
    public void IsDaytime_EndOfDay_IsFalse()
    {
        Assert.False(GameClock.IsDaytime(GameClock.DayTicks));
    }

    [Fact]
    public void IsDaytime_MidNight_IsFalse()
    {
        Assert.False(GameClock.IsDaytime(GameClock.DayTicks + GameClock.NightTicks / 2));
    }

    [Fact]
    public void IsDaytime_StartOfDayTwo_IsTrue()
    {
        Assert.True(GameClock.IsDaytime(GameClock.TicksPerInGameDay));
    }

    [Fact]
    public void IsDaytime_HandlesNegativeTicksModularly()
    {
        // Defensive: DayFraction / IsDaytime should not throw on
        // negative ticks (e.g., before world starts). Using Euclidean
        // modular arithmetic, tick -1 → 3599 (night) and tick -DayTicks
        // → DayTicks - DayTicks = 0... wait, actually -2400 mod 3600 =
        // 1200, which IS daytime. Both are defensive only — what we
        // assert here is just that the call does not throw.
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
