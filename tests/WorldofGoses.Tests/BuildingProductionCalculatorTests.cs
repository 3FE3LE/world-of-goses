using System.Collections.Generic;
using WorldofGoses.Domain;
using Xunit;
using static WorldofGoses.Tests.TestHelpers;

namespace WorldofGoses.Tests;

public class BuildingProductionCalculatorTests
{
    [Fact]
    public void ProductionPerTick_NoWorkers_IsZero()
    {
        var b = NewBuilding(baseProductionPerWorker: 5);
        var citizens = new Dictionary<CitizenId, Citizen>();
        Assert.Equal(0, BuildingProductionCalculator.ProductionPerTick(b, citizens));
    }

    [Fact]
    public void ProductionPerTick_OneWorkerNoExperience_IsBase()
    {
        var b = NewBuilding(baseProductionPerWorker: 10);
        var c = NewCitizen(1);
        b.TryAssign(c.Id);
        var citizens = new Dictionary<CitizenId, Citizen> { [c.Id] = c };
        Assert.Equal(10, BuildingProductionCalculator.ProductionPerTick(b, citizens));
    }

    [Fact]
    public void ProductionPerTick_MiningExperienceYieldsLinearBonus()
    {
        var b = NewBuilding(baseProductionPerWorker: 10);
        var c = NewCitizen(1, miningExperience: 4);
        b.TryAssign(c.Id);
        var citizens = new Dictionary<CitizenId, Citizen> { [c.Id] = c };
        Assert.Equal(12, BuildingProductionCalculator.ProductionPerTick(b, citizens));
    }

    [Fact]
    public void ProductionPerTick_FarmWithFarmingExperience_BonusesApply()
    {
        var b = NewBuilding(
            kind: BuildingKind.Farm,
            producedCompetencyId: CompetencyId.Farming,
            producedResourceType: ResourceType.Food,
            baseProductionPerWorker: 10);
        var c = NewCitizen(1, CompetencyId.Farming, experience: 4);
        b.TryAssign(c.Id);
        var citizens = new Dictionary<CitizenId, Citizen> { [c.Id] = c };
        Assert.Equal(12, BuildingProductionCalculator.ProductionPerTick(b, citizens));
    }

    [Fact]
    public void ProductionPerTick_WrongCompetencyGivesNoBonus()
    {
        var b = NewBuilding(
            kind: BuildingKind.Farm,
            producedCompetencyId: CompetencyId.Farming,
            producedResourceType: ResourceType.Food,
            baseProductionPerWorker: 10);
        var c = NewCitizen(1, miningExperience: 4); // mining, not farming
        b.TryAssign(c.Id);
        var citizens = new Dictionary<CitizenId, Citizen> { [c.Id] = c };
        Assert.Equal(10, BuildingProductionCalculator.ProductionPerTick(b, citizens));
    }

    [Fact]
    public void ProductionPerTick_MultipleWorkers_SumsContributions()
    {
        var b = NewBuilding(baseProductionPerWorker: 10);
        var c1 = NewCitizen(1, miningExperience: 4); // 12
        var c2 = NewCitizen(2, miningExperience: 8); // 14
        var c3 = NewCitizen(3);                       // 10
        b.TryAssign(c1.Id);
        b.TryAssign(c2.Id);
        b.TryAssign(c3.Id);
        var citizens = new Dictionary<CitizenId, Citizen>
        {
            [c1.Id] = c1,
            [c2.Id] = c2,
            [c3.Id] = c3,
        };
        Assert.Equal(36, BuildingProductionCalculator.ProductionPerTick(b, citizens));
    }

    [Fact]
    public void ProductionPerTick_UnknownCitizen_ContributesZeroDefensively()
    {
        var b = NewBuilding(baseProductionPerWorker: 5);
        var c1 = NewCitizen(1);
        b.TryAssign(c1.Id);
        var c2Unknown = NewCitizen(2);
        b.TryAssign(c2Unknown.Id);
        var citizens = new Dictionary<CitizenId, Citizen> { [c1.Id] = c1 };
        Assert.Equal(5, BuildingProductionCalculator.ProductionPerTick(b, citizens));
    }

    [Fact]
    public void WorkerContribution_DirectCall_MatchesFormula()
    {
        var c = NewCitizen(1, miningExperience: 20);
        Assert.Equal(10, BuildingProductionCalculator.WorkerContribution(c, CompetencyId.Mining, baseProductionPerWorker: 5));
    }
}
