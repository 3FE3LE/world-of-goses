using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class StaminaRulesTests
{
    [Fact]
    public void MaxStamina_IsPositiveConstant()
    {
        Assert.True(StaminaRules.MaxStamina > 0);
    }

    [Fact]
    public void CostPerWorkerPerTick_QuarryAndFarm_AreEqual()
    {
        Assert.Equal(
            StaminaRules.CostPerWorkerPerTick(BuildingKind.Quarry),
            StaminaRules.CostPerWorkerPerTick(BuildingKind.Farm));
    }

    [Fact]
    public void CostForWorker_IgnoresCitizenToday()
    {
        var citizen = TestHelpers.NewCitizen(1, miningExperience: 5);
        Assert.Equal(
            StaminaRules.CostPerWorkerPerTick(BuildingKind.Quarry),
            StaminaRules.CostForWorker(citizen, BuildingKind.Quarry));
    }

    [Fact]
    public void RegenFromFood_OneForOne()
    {
        var citizen = TestHelpers.NewCitizen(1);
        Assert.Equal(1, StaminaRules.RegenFromFood(1, citizen));
        Assert.Equal(0, StaminaRules.RegenFromFood(0, citizen));
    }
}
