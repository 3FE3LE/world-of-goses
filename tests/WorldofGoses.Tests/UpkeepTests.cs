using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class UpkeepTests
{
    [Fact]
    public void StonePerTick_ZeroCitizens_IsZero()
    {
        Assert.Equal(0, Upkeep.StonePerTick(0));
    }

    [Fact]
    public void StonePerTick_NegativeCitizens_IsZero()
    {
        Assert.Equal(0, Upkeep.StonePerTick(-3));
    }

    [Fact]
    public void StonePerTick_SingleCitizen_IsOne()
    {
        Assert.Equal(1, Upkeep.StonePerTick(1));
    }

    [Fact]
    public void StonePerTick_FiveCitizens_IsOne()
    {
        Assert.Equal(1, Upkeep.StonePerTick(5));
    }

    [Fact]
    public void StonePerTick_TenCitizens_IsTwo()
    {
        Assert.Equal(2, Upkeep.StonePerTick(10));
    }

    [Fact]
    public void StonePerTick_FifteenCitizens_IsThree()
    {
        Assert.Equal(3, Upkeep.StonePerTick(15));
    }

    [Fact]
    public void StonePerTick_FourCitizens_IsOne()
    {
        // Ceiling division: 4 citizens rounds up to 1 stone (not 0).
        Assert.Equal(1, Upkeep.StonePerTick(4));
    }

    [Fact]
    public void StonePerTick_SixCitizens_IsTwo()
    {
        Assert.Equal(2, Upkeep.StonePerTick(6));
    }

    [Fact]
    public void FoodPerResidentPerDay_ZeroCitizens_IsZero()
    {
        Assert.Equal(0, Upkeep.FoodPerResidentPerDay(0));
    }

    [Fact]
    public void FoodPerResidentPerDay_NegativeCitizens_IsZero()
    {
        Assert.Equal(0, Upkeep.FoodPerResidentPerDay(-2));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(10, 10)]
    public void FoodPerResidentPerDay_IsExactlyOnePerResident(int citizenCount, int expected)
    {
        // Unlike StonePerTick, this has no floor for an empty city and no
        // rounding curve: the demand exists to make population carry a
        // real cost, not to simulate abstract maintenance.
        Assert.Equal(expected, Upkeep.FoodPerResidentPerDay(citizenCount));
    }
}
