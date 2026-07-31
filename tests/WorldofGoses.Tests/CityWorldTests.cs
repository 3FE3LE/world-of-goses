using System.Linq;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// End-to-end domain tests. Production scenarios are built explicitly by
/// TestHelpers; a fresh CityWorld remains empty until hero onboarding.
/// </summary>
public class CityWorldTests
{
    [Fact]
    public void AdvanceWorldTick_AdvancesEveryAuthorizedBuildingOnce()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var farm = world.GetBuilding(new BuildingId(2))!;

        int tickBefore = world.CurrentTick;
        TestHelpers.AdvanceToNextProductionCycle(world);

        // The world starts at the configured workday tick (08:00
        // since the 2026-07-30 workday shift), so the absolute tick
        // post-advance is relative to that start.
        Assert.Equal(tickBefore + CityEconomyRules.ProductionCycleTicks, world.CurrentTick);
        Assert.True(quarry.Stock > 0);
        Assert.True(farm.Stock > 0);
    }

    [Fact]
    public void AdvanceWorldTick_RespectsIndependentBuildingPolicies()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var farm = world.GetBuilding(new BuildingId(2))!;
        quarry.ConfigureProductionPolicy(enabled: false, minStock: 0, maxStock: quarry.StorageCapacity, priority: 0);

        TestHelpers.AdvanceToNextProductionCycle(world);

        Assert.Equal(0, quarry.Stock);
        Assert.True(farm.Stock > 0);
    }

    [Fact]
    public void ProductionScenario_HasPrimaryBuildingAndAvailableCitizens()
    {
        var world = TestHelpers.NewProductionWorld();
        Assert.NotNull(world.PrimaryBuilding);
        Assert.Equal(2, world.AvailableCitizens().Count);
    }

    [Fact]
    public void ProductionScenario_HasQuarryFarmAndHome()
    {
        var world = TestHelpers.NewProductionWorld();
        Assert.Equal(3, world.Buildings.Count);

        var kinds = world.Buildings.Values.Select(b => b.Kind).ToHashSet();
        Assert.Contains(BuildingKind.Quarry, kinds);
        Assert.Contains(BuildingKind.Farm, kinds);
        Assert.Contains(BuildingKind.Home, kinds);
    }

    [Fact]
    public void ProductionScenario_CitizenIsPreAssignedToFarm()
    {
        var world = TestHelpers.NewProductionWorld();
        var lior = world.GetCitizen(new CitizenId(3))!;
        Assert.NotNull(lior.CurrentAssignment);
        var building = world.GetBuilding(lior.CurrentAssignment!.Value)!;
        Assert.Equal(BuildingKind.Farm, building.Kind);
    }

    [Fact]
    public void AvailableCitizens_OnlyReturnsUnassigned()
    {
        var world = TestHelpers.NewProductionWorld();
        foreach (var c in world.AvailableCitizens())
        {
            Assert.Null(c.CurrentAssignment);
        }
    }

    [Fact]
    public void Assign_FirstFree_SucceedsAndRaisesBuildingChanged()
    {
        var world = TestHelpers.NewProductionWorld();
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
        var world = TestHelpers.NewProductionWorld();
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
        var world = TestHelpers.NewProductionWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var target = world.AvailableCitizens()[0];

        // Simulate the citizen being attached to a different building.
        world.GetCitizen(target.Id)!.TryCommitToBuilding(new BuildingId(99));

        var result = world.TryAssignCitizen(buildingId, target.Id);
        Assert.False(result.IsSuccess);
        Assert.Equal(AssignmentOutcome.CitizenUnavailable, result.Outcome);
        Assert.Equal(CitizenAvailabilityReason.AssignedToBuilding, result.UnavailableReason);
    }

    [Fact]
    public void Assign_UnknownBuilding_RejectsWithBuildingNotFound()
    {
        var world = TestHelpers.NewProductionWorld();
        var anyAvailable = world.AvailableCitizens()[0];
        var result = world.TryAssignCitizen(new BuildingId(99), anyAvailable.Id);
        Assert.False(result.IsSuccess);
        Assert.Equal(AssignmentOutcome.BuildingNotFound, result.Outcome);
    }

    [Fact]
    public void Assign_UnknownCitizen_RejectsWithCitizenNotFound()
    {
        var world = TestHelpers.NewProductionWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var result = world.TryAssignCitizen(buildingId, new CitizenId(999));
        Assert.False(result.IsSuccess);
        Assert.Equal(AssignmentOutcome.CitizenNotFound, result.Outcome);
    }

    [Fact]
    public void Unassign_NotCurrentlyAssigned_RejectsWithNotAssigned()
    {
        var world = TestHelpers.NewProductionWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var unassigned = world.AvailableCitizens()[0];
        var result = world.TryUnassignCitizen(buildingId, unassigned.Id);
        Assert.False(result.IsSuccess);
        Assert.Equal(AssignmentOutcome.NotAssigned, result.Outcome);
    }

    [Fact]
    public void AssignAndUnassign_TogglesAvailability()
    {
        var world = TestHelpers.NewProductionWorld();
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
        var world = TestHelpers.NewProductionWorld();
        var buildingId = world.PrimaryBuilding.Id;
        int before = world.PrimaryBuilding.Stock;
        int added = AdvanceBuildingToNextCycle(world, buildingId);
        Assert.True(added > 0);
        Assert.Equal(before + added, world.PrimaryBuilding.Stock);
    }

    [Fact]
    public void AdvanceProduction_ClampsAtStorageCapacity()
    {
        var world = TestHelpers.NewProductionWorld();
        var buildingId = world.PrimaryBuilding.Id;

        while (world.PrimaryBuilding.Stock < world.PrimaryBuilding.StorageCapacity)
        {
            Assert.True(AdvanceBuildingToNextCycle(world, buildingId) > 0);
        }

        Assert.Equal(world.PrimaryBuilding.StorageCapacity, world.PrimaryBuilding.Stock);
        Assert.Equal(0, world.AdvanceProduction(buildingId));
    }

    [Fact]
    public void AdvanceProduction_GrantsExperienceToAssignedCitizens()
    {
        var world = TestHelpers.NewProductionWorld();
        var buildingId = world.PrimaryBuilding.Id;
        var assignedIds = world.PrimaryBuilding.AssignedCitizenIds.ToList();
        Assert.NotEmpty(assignedIds);

        var first = world.GetCitizen(assignedIds[0])!;
        var competency = world.PrimaryBuilding.ProducedCompetencyId;
        int before = first.GetExperience(competency);

        AdvanceBuildingToNextCycle(world, buildingId);

        Assert.Equal(before + 1, first.GetExperience(competency));
    }

    [Fact]
    public void FreshWorld_StartsEmptyUntilHeroOnboarding()
    {
        var world = new CityWorld();

        Assert.True(world.NeedsOnboarding);
        Assert.Empty(world.Citizens);
        Assert.Empty(world.Buildings);
    }

    private static int AdvanceBuildingToNextCycle(CityWorld world, BuildingId buildingId)
    {
        int added = 0;
        do
        {
            added = world.AdvanceProduction(buildingId);
        }
        while (!CityEconomyRules.IsProductionCycle(world.CurrentTick));
        return added;
    }

    [Fact]
    public void FreshWorld_OnboardingCreatesThePrincipalHero()
    {
        var world = new CityWorld();
        var result = world.TryCreateHero(
            new HeroCreationRequest("Founder", TestHelpers.NewProfile(LineageId.Caelith), GenderId.Feminine));

        Assert.True(result.IsSuccess);
        Assert.Equal("Founder", world.Hero!.Name);
        Assert.True(world.Hero.IsHero);
        Assert.Single(world.Citizens);
        Assert.Empty(world.Buildings);
    }
}
