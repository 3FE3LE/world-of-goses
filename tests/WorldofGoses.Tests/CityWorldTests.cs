using System.Linq;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// End-to-end domain tests. Use the seeded <see cref="CityWorld"/>
/// as a realistic fixture: 1 Quarry + 1 Farm, 5 citizens, 3
/// pre-assigned (Bran + Erin on Quarry, Lior on Farm), 2 free.
/// </summary>
public class CityWorldTests
{
    [Fact]
    public void AdvanceWorldTick_AdvancesEveryAuthorizedBuildingOnce()
    {
        var world = new CityWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var farm = world.GetBuilding(new BuildingId(2))!;

        world.AdvanceWorldTick();

        Assert.Equal(1, world.CurrentTick);
        Assert.True(quarry.Stock > 0);
        Assert.True(farm.Stock > 0);
    }

    [Fact]
    public void AdvanceWorldTick_RespectsIndependentBuildingPolicies()
    {
        var world = new CityWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var farm = world.GetBuilding(new BuildingId(2))!;
        quarry.ConfigureProductionPolicy(enabled: false, targetStock: quarry.StorageCapacity);

        world.AdvanceWorldTick();

        Assert.Equal(0, quarry.Stock);
        Assert.True(farm.Stock > 0);
    }

    [Fact]
    public void FreshWorld_HasPrimaryBuildingAndAvailableCitizens()
    {
        var world = new CityWorld();
        Assert.NotNull(world.PrimaryBuilding);
        Assert.Equal(2, world.AvailableCitizens().Count);
    }

    [Fact]
    public void Seed_HasQuarryAndFarmAndHome()
    {
        var world = new CityWorld();
        Assert.Equal(3, world.Buildings.Count);

        var kinds = world.Buildings.Values.Select(b => b.Kind).ToHashSet();
        Assert.Contains(BuildingKind.Quarry, kinds);
        Assert.Contains(BuildingKind.Farm, kinds);
        Assert.Contains(BuildingKind.Home, kinds);
    }

    [Fact]
    public void Seed_LiorPreAssignedToFarm()
    {
        var world = new CityWorld();
        var lior = world.GetCitizen(new CitizenId(3))!;
        Assert.NotNull(lior.CurrentAssignment);
        var building = world.GetBuilding(lior.CurrentAssignment!.Value)!;
        Assert.Equal(BuildingKind.Farm, building.Kind);
    }

    [Fact]
    public void AvailableCitizens_OnlyReturnsUnassigned()
    {
        var world = new CityWorld();
        foreach (var c in world.AvailableCitizens())
        {
            Assert.Null(c.CurrentAssignment);
        }
    }

    [Fact]
    public void Assign_FirstFree_SucceedsAndRaisesBuildingChanged()
    {
        var world = new CityWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var firedBuildingId = -1;
        world.BuildingChanged += (_, e) => firedBuildingId = e.BuildingId.Value;

        var target = world.AvailableCitizens()[0];
        var result = world.TryAssignCitizen(buildingId, target.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(buildingId.Value, firedBuildingId);
        Assert.Equal<BuildingId?>(buildingId, world.GetCitizen(target.Id)!.CurrentAssignment);
    }

    [Fact]
    public void Assign_AlreadyOnBuilding_RejectsWithAlreadyAssigned()
    {
        var world = new CityWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var available = world.AvailableCitizens()[0];
        world.TryAssignCitizen(buildingId, available.Id);

        var second = world.TryAssignCitizen(buildingId, available.Id);
        Assert.False(second.IsSuccess);
        Assert.Equal(AssignmentOutcome.AlreadyAssigned, second.Outcome);
    }

    [Fact]
    public void Assign_CitizenAlreadyOnAnotherBuilding_RejectsWithCitizenUnavailable()
    {
        var world = new CityWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var target = world.AvailableCitizens()[0];

        // Simulate the citizen being attached to a different building.
        world.GetCitizen(target.Id)!.AssignTo(new BuildingId(99));

        var result = world.TryAssignCitizen(buildingId, target.Id);
        Assert.False(result.IsSuccess);
        Assert.Equal(AssignmentOutcome.CitizenUnavailable, result.Outcome);
    }

    [Fact]
    public void Assign_UnknownBuilding_RejectsWithBuildingNotFound()
    {
        var world = new CityWorld();
        var anyAvailable = world.AvailableCitizens()[0];
        var result = world.TryAssignCitizen(new BuildingId(99), anyAvailable.Id);
        Assert.False(result.IsSuccess);
        Assert.Equal(AssignmentOutcome.BuildingNotFound, result.Outcome);
    }

    [Fact]
    public void Assign_UnknownCitizen_RejectsWithCitizenNotFound()
    {
        var world = new CityWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var result = world.TryAssignCitizen(buildingId, new CitizenId(999));
        Assert.False(result.IsSuccess);
        Assert.Equal(AssignmentOutcome.CitizenNotFound, result.Outcome);
    }

    [Fact]
    public void Unassign_NotCurrentlyAssigned_RejectsWithNotAssigned()
    {
        var world = new CityWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var unassigned = world.AvailableCitizens()[0];
        var result = world.TryUnassignCitizen(buildingId, unassigned.Id);
        Assert.False(result.IsSuccess);
        Assert.Equal(AssignmentOutcome.NotAssigned, result.Outcome);
    }

    [Fact]
    public void AssignAndUnassign_TogglesAvailability()
    {
        var world = new CityWorld();
        var buildingId = world.PrimaryBuilding.Id;

        var target = world.AvailableCitizens()[0];
        Assert.True(world.TryAssignCitizen(buildingId, target.Id).IsSuccess);
        Assert.Single(world.AvailableCitizens());

        Assert.True(world.TryUnassignCitizen(buildingId, target.Id).IsSuccess);
        Assert.Equal(2, world.AvailableCitizens().Count);
    }

    [Fact]
    public void AdvanceProduction_CreditsStockByCurrentRate()
    {
        var world = new CityWorld();
        var buildingId = world.PrimaryBuilding.Id;
        int before = world.PrimaryBuilding.Stock;
        int added = world.AdvanceProduction(buildingId);
        Assert.True(added > 0);
        Assert.Equal(before + added, world.PrimaryBuilding.Stock);
    }

    [Fact]
    public void AdvanceProduction_ClampsAtStorageCapacity()
    {
        var world = new CityWorld();
        var buildingId = world.PrimaryBuilding.Id;

        while (world.PrimaryBuilding.Stock < world.PrimaryBuilding.StorageCapacity
               && world.AdvanceProduction(buildingId) > 0)
        {
            // fill until full
        }

        Assert.Equal(world.PrimaryBuilding.StorageCapacity, world.PrimaryBuilding.Stock);
        Assert.Equal(0, world.AdvanceProduction(buildingId));
    }

    [Fact]
    public void AdvanceProduction_GrantsExperienceToAssignedCitizens()
    {
        var world = new CityWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var assignedIds = world.PrimaryBuilding.AssignedCitizenIds.ToList();
        Assert.NotEmpty(assignedIds);

        var first = world.GetCitizen(assignedIds[0])!;
        var competency = world.PrimaryBuilding.ProducedCompetencyId;
        int before = first.GetExperience(competency);

        world.AdvanceProduction(buildingId);

        Assert.Equal(before + 1, first.GetExperience(competency));
    }
}
