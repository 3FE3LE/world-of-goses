using WorldofGoses.Domain;
using Xunit;
using static WorldofGoses.Tests.TestHelpers;

namespace WorldofGoses.Tests;

public class WellFedBuffTests
{
    [Fact]
    public void Constructor_BuffStartsAtZero()
    {
        var c = NewCitizen(1);
        Assert.Equal(0, c.WellFedRemainingTicks);
    }

    [Fact]
    public void RefreshWellFedBuff_ResetsToDuration()
    {
        var c = NewCitizen(1);
        c.RefreshWellFedBuff();
        Assert.Equal(StaminaRules.WellFedBuffDuration, c.WellFedRemainingTicks);
    }

    [Fact]
    public void AdvanceWellFedTick_DecrementsByOne()
    {
        var c = NewCitizen(1);
        c.RefreshWellFedBuff();
        c.AdvanceWellFedTick();
        Assert.Equal(StaminaRules.WellFedBuffDuration - 1, c.WellFedRemainingTicks);
    }

    [Fact]
    public void AdvanceWellFedTick_FloorsAtZero()
    {
        var c = NewCitizen(1);
        for (int i = 0; i < StaminaRules.WellFedBuffDuration + 5; i++)
        {
            c.AdvanceWellFedTick();
        }
        Assert.Equal(0, c.WellFedRemainingTicks);
    }

    [Fact]
    public void RegenPerTick_NoBuff_IsBaseOnly()
    {
        var c = NewCitizen(1);
        Assert.Equal(StaminaRules.BaseRegenPerTick, c.RegenPerTick());
    }

    [Fact]
    public void RegenPerTick_BuffActive_IsBasePlusBonus()
    {
        var c = NewCitizen(1);
        c.RefreshWellFedBuff();
        Assert.Equal(
            StaminaRules.BaseRegenPerTick + StaminaRules.WellFedRegenBonus,
            c.RegenPerTick());
    }

    [Fact]
    public void RegenPerTick_AfterBuffExpires_IsBaseOnly()
    {
        var c = NewCitizen(1);
        c.RefreshWellFedBuff();
        for (int i = 0; i < StaminaRules.WellFedBuffDuration; i++)
        {
            c.AdvanceWellFedTick();
        }
        Assert.Equal(StaminaRules.BaseRegenPerTick, c.RegenPerTick());
    }

    [Fact]
    public void Constructor_CustomBuff_Respected()
    {
        var c = new Citizen(new CitizenId(1), "X", appearanceSeed: 1, initialWellFedTicks: 50);
        Assert.Equal(50, c.WellFedRemainingTicks);
        Assert.Equal(
            StaminaRules.BaseRegenPerTick + StaminaRules.WellFedRegenBonus,
            c.RegenPerTick());
    }

    [Fact]
    public void Constructor_BeyondMaxBuff_Throws()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => new Citizen(new CitizenId(1), "X", appearanceSeed: 1,
                initialWellFedTicks: StaminaRules.WellFedBuffDuration + 1));
    }
}
