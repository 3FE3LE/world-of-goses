using System;
using WorldofGoses.Domain;
using Xunit;
using static WorldofGoses.Tests.TestHelpers;

namespace WorldofGoses.Tests;

public class CitizenStaminaTests
{
    [Fact]
    public void Constructor_DefaultsCurrentToMax()
    {
        var c = NewCitizen(1);
        Assert.Equal(StaminaRules.MaxStamina, c.MaxStamina);
        Assert.Equal(c.MaxStamina, c.CurrentStamina);
    }

    [Fact]
    public void Constructor_CustomMax_Respected()
    {
        var c = new Citizen(new CitizenId(1), "X", appearanceSeed: 1, maxStamina: 50);
        Assert.Equal(50, c.MaxStamina);
        Assert.Equal(50, c.CurrentStamina);
    }

    [Fact]
    public void Constructor_NegativeStamina_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Citizen(new CitizenId(1), "X", appearanceSeed: 1, initialStamina: -1));
    }

    [Fact]
    public void Constructor_StaminaExceedingMax_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Citizen(new CitizenId(1), "X", appearanceSeed: 1, initialStamina: 200, maxStamina: 100));
    }

    [Fact]
    public void Constructor_NonPositiveMax_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Citizen(new CitizenId(1), "X", appearanceSeed: 1, maxStamina: 0));
    }

    [Fact]
    public void ConsumeStamina_ClampsAtZero()
    {
        var c = NewCitizen(1);
        c.ConsumeStamina(10);
        Assert.Equal(StaminaRules.MaxStamina - 10, c.CurrentStamina);
        c.ConsumeStamina(9999);
        Assert.Equal(0, c.CurrentStamina);
    }

    [Fact]
    public void RestoreStamina_ClampsAtMax()
    {
        var c = NewCitizen(1);
        c.ConsumeStamina(30);
        c.RestoreStamina(10);
        Assert.Equal(StaminaRules.MaxStamina - 20, c.CurrentStamina);
        c.RestoreStamina(9999);
        Assert.Equal(c.MaxStamina, c.CurrentStamina);
    }

    [Fact]
    public void ConsumeAndRestore_NonPositiveAmounts_AreNoOps()
    {
        var c = NewCitizen(1);
        int before = c.CurrentStamina;
        c.ConsumeStamina(0);
        c.ConsumeStamina(-5);
        c.RestoreStamina(0);
        c.RestoreStamina(-5);
        Assert.Equal(before, c.CurrentStamina);
    }
}
