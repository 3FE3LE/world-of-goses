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
    public void CostPerWorkerPerCycle_QuarryAndFarm_AreEqual()
    {
        Assert.Equal(
            StaminaRules.CostPerWorkerPerCycle(BuildingKind.Quarry),
            StaminaRules.CostPerWorkerPerCycle(BuildingKind.Farm));
    }

    [Fact]
    public void CostForWorker_IgnoresCitizenToday()
    {
        var citizen = TestHelpers.NewCitizen(1, miningExperience: 5);
        Assert.Equal(
            StaminaRules.CostPerWorkerPerCycle(BuildingKind.Quarry),
            StaminaRules.CostForWorker(citizen, BuildingKind.Quarry));
    }

    [Fact]
    public void RegenFromFood_OneMealRestoresMeaningfulStamina()
    {
        var citizen = TestHelpers.NewCitizen(1);
        Assert.Equal(StaminaRules.RegenPerFoodUnit, StaminaRules.RegenFromFood(1, citizen));
        Assert.Equal(0, StaminaRules.RegenFromFood(0, citizen));
    }
}
