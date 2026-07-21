using System;
using WorldofGoses.Domain;
using Xunit;
using static WorldofGoses.Tests.TestHelpers;

namespace WorldofGoses.Tests;

public class BuildingTests
{
    [Fact]
    public void Constructor_AssignsAllProperties()
    {
        var b = NewBuilding(workerCapacity: 8, visualCapacity: 4, storageCapacity: 30);
        Assert.Equal(8, b.WorkerCapacity);
        Assert.Equal(4, b.VisualCapacity);
        Assert.Equal(30, b.StorageCapacity);
        Assert.Equal(0, b.Stock);
        Assert.Equal(0, b.AssignedCount);
    }

    [Fact]
    public void Constructor_DefaultsToQuarryMiningStone()
    {
        var b = NewBuilding();
        Assert.Equal(BuildingKind.Quarry, b.Kind);
        Assert.Equal(ResourceType.Stone, b.ProducedResourceType);
        Assert.Equal(CompetencyId.Mining, b.ProducedCompetencyId);
        Assert.Equal("Stone", b.ResourceLabel);
        Assert.Equal("stone", b.ResourceUnit);
    }

    [Fact]
    public void Constructor_FarmWithFarmingCompetency_ProducesFood()
    {
        var b = NewBuilding(
            kind: BuildingKind.Farm,
            producedCompetencyId: CompetencyId.Farming,
            producedResourceType: ResourceType.Food,
            displayName: "Farm",
            resourceLabel: "Food",
            resourceUnit: "food");
        Assert.Equal(BuildingKind.Farm, b.Kind);
        Assert.Equal(ResourceType.Food, b.ProducedResourceType);
        Assert.Equal(CompetencyId.Farming, b.ProducedCompetencyId);
        Assert.Equal("Food", b.ResourceLabel);
        Assert.Equal("food", b.ResourceUnit);
    }

    [Fact]
    public void FullDisplayLabel_CombinesDisplayNameAndResourceLabel()
    {
        var b = NewBuilding(displayName: "Quarry", resourceLabel: "Stone");
        Assert.Equal("Quarry (Stone)", b.FullDisplayLabel);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_NegativeWorkerCapacity_Throws(int bad)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => NewBuilding(workerCapacity: bad));
    }

    [Fact]
    public void Constructor_NegativeVisualCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => NewBuilding(visualCapacity: -1));
    }

    [Fact]
    public void Constructor_NegativeStorageCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => NewBuilding(storageCapacity: -1));
    }

    [Fact]
    public void TryAssign_BelowCapacity_Succeeds()
    {
        var b = NewBuilding(workerCapacity: 3);
        var result = b.TryAssign(new CitizenId(1));
        Assert.True(result.IsSuccess);
        Assert.Equal(1, b.AssignedCount);
        Assert.True(b.IsAssigned(new CitizenId(1)));
    }

    [Fact]
    public void TryAssign_AlreadyAssigned_RejectsWithAlreadyAssigned()
    {
        var b = NewBuilding(workerCapacity: 3);
        b.TryAssign(new CitizenId(1));
        var second = b.TryAssign(new CitizenId(1));
        Assert.False(second.IsSuccess);
        Assert.Equal(AssignmentOutcome.AlreadyAssigned, second.Outcome);
    }

    [Fact]
    public void TryAssign_AtCapacity_Rejects()
    {
        var b = NewBuilding(workerCapacity: 2);
        b.TryAssign(new CitizenId(1));
        b.TryAssign(new CitizenId(2));
        var third = b.TryAssign(new CitizenId(3));
        Assert.False(third.IsSuccess);
        Assert.Equal(AssignmentOutcome.AtCapacity, third.Outcome);
    }

    [Fact]
    public void TryUnassign_Assigned_Succeeds()
    {
        var b = NewBuilding();
        b.TryAssign(new CitizenId(1));
        var result = b.TryUnassign(new CitizenId(1));
        Assert.True(result.IsSuccess);
        Assert.Equal(0, b.AssignedCount);
    }

    [Fact]
    public void TryUnassign_NotAssigned_Rejects()
    {
        var b = NewBuilding();
        var result = b.TryUnassign(new CitizenId(7));
        Assert.False(result.IsSuccess);
        Assert.Equal(AssignmentOutcome.NotAssigned, result.Outcome);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 1, 0)]
    [InlineData(2, 2, 0)]
    [InlineData(3, 3, 0)]
    [InlineData(4, 3, 1)]
    [InlineData(6, 3, 3)]
    public void VisibleAndHiddenCount_RespectVisualCapacity(
        int assigned, int expectedVisible, int expectedHidden)
    {
        var b = NewBuilding(workerCapacity: 6, visualCapacity: 3);
        for (int i = 0; i < assigned; i++)
        {
            b.TryAssign(new CitizenId(i + 1));
        }
        Assert.Equal(expectedVisible, b.VisibleWorkerCount);
        Assert.Equal(expectedHidden, b.HiddenWorkerCount);
    }

    [Fact]
    public void AddStock_ClampsToCapacity()
    {
        var b = NewBuilding(storageCapacity: 10);
        Assert.Equal(7, b.AddStock(7));
        Assert.Equal(7, b.Stock);

        Assert.Equal(3, b.AddStock(100));
        Assert.Equal(10, b.Stock);
    }

    [Fact]
    public void AddStock_NonPositive_IsZero()
    {
        var b = NewBuilding();
        Assert.Equal(0, b.AddStock(0));
        Assert.Equal(0, b.AddStock(-5));
        Assert.Equal(0, b.Stock);
    }

    [Fact]
    public void TryConsumeStock_EnoughStock_ReturnsTrue()
    {
        var b = NewBuilding();
        b.AddStock(5);
        Assert.True(b.TryConsumeStock(3));
        Assert.Equal(2, b.Stock);
    }

    [Fact]
    public void TryConsumeStock_NotEnoughStock_ReturnsFalse()
    {
        var b = NewBuilding();
        b.AddStock(2);
        Assert.False(b.TryConsumeStock(5));
        Assert.Equal(2, b.Stock);
    }

    [Fact]
    public void TryConsumeStock_NegativeAmount_ReturnsFalse()
    {
        var b = NewBuilding();
        Assert.False(b.TryConsumeStock(-1));
    }

    [Fact]
    public void ConfigureProductionPolicy_UpdatesAuthorizationAndRange()
    {
        var building = NewBuilding(storageCapacity: 20);

        building.ConfigureProductionPolicy(enabled: false, minStock: 2, maxStock: 7, priority: 3);

        Assert.False(building.ProductionEnabled);
        Assert.Equal(2, building.MinStock);
        Assert.Equal(7, building.MaxStock);
        Assert.Equal(3, building.Priority);
        Assert.False(building.CanProduce);
    }

    [Fact]
    public void ConfigureProductionPolicy_MaxOutsideStorage_Throws()
    {
        var building = NewBuilding(storageCapacity: 20);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => building.ConfigureProductionPolicy(enabled: true, minStock: 0, maxStock: 21, priority: 0));
    }

    [Fact]
    public void ConfigureProductionPolicy_MinGreaterThanMax_Throws()
    {
        var building = NewBuilding(storageCapacity: 20);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => building.ConfigureProductionPolicy(enabled: true, minStock: 10, maxStock: 5, priority: 0));
    }

    [Fact]
    public void ConfigureProductionPolicy_NegativePriority_Throws()
    {
        var building = NewBuilding(storageCapacity: 20);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => building.ConfigureProductionPolicy(enabled: true, minStock: 0, maxStock: 20, priority: -1));
    }
}
