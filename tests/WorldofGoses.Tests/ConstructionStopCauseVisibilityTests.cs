using System.Linq;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// A worksite can sit at 0/180 for entirely legitimate reasons — the founding
/// site finished its Campfire and is waiting for the player to choose the next
/// module, the contributor is exhausted, or they are still walking. The status
/// strip used to report progress with no reason at all, which reads as a broken
/// game rather than a blocked one. These assertions keep the reason on the
/// snapshot the strip renders from.
/// </summary>
public sealed class ConstructionStopCauseVisibilityTests
{
    [Fact]
    public void StatusSnapshot_CarriesTheProjectStopCause()
    {
        CityWorld world = TestHelpers.NewConstructionWorld();

        CityStatusSnapshot snapshot = CityStatusSnapshot.From(world);
        CityStatusSnapshot.ProjectItem project = snapshot.Projects.Single();

        Assert.Equal(world.Projects.Values.Single().StopCause, project.StopCause);
    }

    [Fact]
    public void AFoundingSiteBetweenModules_ReportsAwaitingModule()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        foreach (RecipeInput input in FoundingSiteRules.InputsFor(FoundingSiteModule.Campfire))
        {
            world.Resources.DepositToCityInventory(input.Resource, input.Amount);
        }
        ConstructionAuthorizationResult authorization =
            world.TryAuthorizeConstruction(ConstructionKind.FoundingSite);
        Assert.True(authorization.IsSuccess, authorization.Outcome.ToString());
        ConstructionProject project = world.Projects.Values.Single();

        // Finish the Campfire the way the simulation would.
        project.Progress = project.RequiredWork;
        world.AdvanceWorldTick();

        Assert.Contains(FoundingSiteModule.Campfire, project.CompletedFoundingModules);
        Assert.Equal(0, project.Progress);
        Assert.Equal(ConstructionStopCause.AwaitingModule, project.StopCause);

        // This is the state the player saw as "Obra 0/180" with no explanation.
        CityStatusSnapshot.ProjectItem item = CityStatusSnapshot.From(world).Projects.Single();
        Assert.Equal(ConstructionStopCause.AwaitingModule, item.StopCause);
        Assert.Equal(0, item.Progress);
    }

    [Fact]
    public void ModuleOptions_CarryRequiredAndAvailableAmounts()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        foreach (RecipeInput input in FoundingSiteRules.InputsFor(FoundingSiteModule.Campfire))
        {
            world.Resources.DepositToCityInventory(input.Resource, input.Amount);
        }
        Assert.True(world.TryAuthorizeConstruction(ConstructionKind.FoundingSite).IsSuccess);
        ConstructionProject project = world.Projects.Values.Single();
        project.Progress = project.RequiredWork;
        world.AdvanceWorldTick();

        ConstructionSnapshot.FoundingModuleOptionItem bedroll =
            ConstructionSnapshot.From(world).FoundingOptionFor(FoundingSiteModule.Bedroll)!;

        // The panel renders "N resource (available: M)" per material from these
        // two numbers, so a disabled module button can explain itself.
        Assert.NotEmpty(bedroll.Materials);
        Assert.All(bedroll.Materials, material => Assert.True(material.Required > 0));
        Assert.False(bedroll.CanAuthorize);
        Assert.Contains(bedroll.Materials, material => material.Available < material.Required);
        foreach (ConstructionSnapshot.MaterialItem material in bedroll.Materials)
        {
            Assert.Equal(world.Resources.Available(material.Resource), material.Available);
        }
    }

    [Fact]
    public void AnExhaustedContributor_ReportsWorkersExhausted()
    {
        CityWorld world = TestHelpers.NewConstructionWorld();
        ConstructionProject project = world.Projects.Values.Single();
        Citizen founder = world.Hero!;
        world.ConfirmCitizenArrivedAtAssignment(founder.Id, project.Id);
        Assert.Equal(CitizenLocation.AtWork, founder.CurrentLocation);

        // Drain the contributor below the per-interval stamina cost.
        founder.ConsumeStamina(founder.CurrentStamina);
        Assert.True(founder.CurrentStamina < ConstructionRules.CostPerWorkInterval);

        int safety = ConstructionRules.WorkIntervalTicks * 3;
        while (project.StopCause != ConstructionStopCause.WorkersExhausted && safety-- > 0)
        {
            world.AdvanceWorldTick();
        }

        Assert.Equal(ConstructionStopCause.WorkersExhausted, project.StopCause);
        Assert.Equal(
            ConstructionStopCause.WorkersExhausted,
            CityStatusSnapshot.From(world).Projects.Single().StopCause);
    }
}
