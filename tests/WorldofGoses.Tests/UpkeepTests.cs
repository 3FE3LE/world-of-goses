using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class UpkeepTests
{
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
        // The retired StonePerTick placeholder had an artificial floor
        // for an empty city and a rounding curve that simulated
        // abstract maintenance. The remaining formula has neither: the
        // demand exists to make population carry a real cost.
        Assert.Equal(expected, Upkeep.FoodPerResidentPerDay(citizenCount));
    }
}
