using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class Eg4ResourceExpeditionTests
{
    [Fact]
    public void StartingOpportunities_AreFiniteAndRequireCampfireAndCache()
    {
        CityWorld world = NewOpportunityWorld(unlockResourceSorties: false);
        ResourceOpportunity food = Opportunity(world, ResourceOpportunityKind.NearbyFoodForage);

        ExpeditionStartResult result = world.StartResourceExpedition(
            food.Id,
            new[] { world.Hero!.Id },
            ExpeditionRetreatPosture.ContinueAfterSetback);

        Assert.Equal(2, world.ResourceOpportunities.Count);
        Assert.Equal(ResourceOpportunityState.Available, food.State);
        Assert.Equal(ExpeditionStartOutcome.ResourceSortiesUnavailable, result.Outcome);
    }

    [Fact]
    public void Dispatch_ReservesSupplyOpportunityAndReturnCapacity()
    {
        CityWorld world = NewOpportunityWorld();
        world.Resources.DepositToCityInventory(ResourceType.Branches, 1);
        ResourceOpportunity food = Opportunity(world, ResourceOpportunityKind.NearbyFoodForage);

        ExpeditionStartResult result = world.StartResourceExpedition(
            food.Id,
            new[] { world.Hero!.Id },
            ExpeditionRetreatPosture.ContinueAfterSetback);

        Assert.True(result.IsSuccess, result.Outcome.ToString());
        Expedition expedition = world.Expeditions[result.ExpeditionId!.Value];
        Assert.Equal(ResourceOpportunityState.Reserved, food.State);
        Assert.Equal(expedition.Id, food.ReservedByExpeditionId);
        Assert.Equal(7, expedition.CarryCapacity);
        Assert.Equal(CitizenCommitmentKind.Expedition, world.Hero.Commitment.Kind);
        Assert.Single(world.Resources.Reservations);
    }

    [Fact]
    public void Cancel_ReleasesFiniteOpportunityAndReservedSupply()
    {
        CityWorld world = NewOpportunityWorld();
        world.Resources.DepositToCityInventory(ResourceType.Food, 1);
        ResourceOpportunity wood = Opportunity(world, ResourceOpportunityKind.FallenWoodSearch);
        ExpeditionStartResult result = world.StartResourceExpedition(
            wood.Id,
            new[] { world.Hero!.Id },
            ExpeditionRetreatPosture.ContinueAfterSetback);

        Assert.True(world.CancelExpedition(result.ExpeditionId!.Value));

        Assert.Equal(ResourceOpportunityState.Available, wood.State);
        Assert.Null(wood.ReservedByExpeditionId);
        Assert.Empty(world.Resources.Reservations);
        Assert.Equal(1, world.Resources.Total(ResourceType.Food));
    }

    [Fact]
    public void Completion_ReturnsExactTierAndDepletesOpportunityOnce()
    {
        CityWorld world = NewOpportunityWorld();
        world.Resources.DepositToCityInventory(ResourceType.Branches, 1);
        ResourceOpportunity food = Opportunity(world, ResourceOpportunityKind.NearbyFoodForage);
        ExpeditionStartResult result = world.StartResourceExpedition(
            food.Id,
            new[] { world.Hero!.Id },
            ExpeditionRetreatPosture.ContinueAfterSetback);
        Expedition expedition = world.Expeditions[result.ExpeditionId!.Value];

        for (int tick = 0; tick < ResourceExpeditionRules.Definition(food.Kind).DurationTicks; tick++)
        {
            world.AdvanceWorldTick();
        }

        Assert.Equal(ExpeditionStatus.Returned, expedition.Status);
        Assert.Equal(expedition.ReturnFor(expedition.EncounterOutcome!.Value), expedition.ReturnedAmount);
        Assert.Equal(expedition.ReturnedAmount, world.Resources.Total(ResourceType.Food));
        Assert.Equal(ResourceOpportunityState.Depleted, food.State);
        Assert.False(food.TryReserve(new ExpeditionId(999)));
    }

    [Fact]
    public void Dispatch_RejectsWhenMinimumReturnCannotFit()
    {
        CityWorld world = NewOpportunityWorld();
        world.Resources.DepositToCityInventory(ResourceType.Food, 10);
        world.Resources.DepositToCityInventory(ResourceType.Branches, 1);
        ResourceOpportunity food = Opportunity(world, ResourceOpportunityKind.NearbyFoodForage);

        ExpeditionStartResult result = world.StartResourceExpedition(
            food.Id,
            new[] { world.Hero!.Id },
            ExpeditionRetreatPosture.ContinueAfterSetback);

        Assert.Equal(ExpeditionStartOutcome.InsufficientReturnCapacity, result.Outcome);
        Assert.Equal(ResourceOpportunityState.Available, food.State);
        Assert.Empty(world.Resources.Reservations);
    }

    [Fact]
    public void ReservedOpportunity_RoundTripsAndCompletesIdenticallyOffline()
    {
        CityWorld live = NewOpportunityWorld();
        live.Resources.DepositToCityInventory(ResourceType.Food, 1);
        ResourceOpportunity wood = Opportunity(live, ResourceOpportunityKind.FallenWoodSearch);
        ExpeditionStartResult started = live.StartResourceExpedition(
            wood.Id,
            new[] { live.Hero!.Id },
            ExpeditionRetreatPosture.ContinueAfterSetback);
        CityWorld offline = CityWorld.FromSave(WorldPersistence.DeserializeFromJson(
            WorldPersistence.SerializeToJson(WorldPersistence.Capture(live))));

        ResourceOpportunity restored = offline.ResourceOpportunities[wood.Id];
        Assert.Equal(ResourceOpportunityState.Reserved, restored.State);
        Assert.Equal(started.ExpeditionId, restored.ReservedByExpeditionId);

        int duration = ResourceExpeditionRules.Definition(wood.Kind).DurationTicks;
        for (int tick = 0; tick < duration; tick++) live.AdvanceWorldTick();
        WorldTimeAdvance.Advance(offline, duration);

        Expedition liveExpedition = live.Expeditions[started.ExpeditionId!.Value];
        Expedition offlineExpedition = offline.Expeditions[started.ExpeditionId.Value];
        Assert.Equal(liveExpedition.EncounterOutcome, offlineExpedition.EncounterOutcome);
        Assert.Equal(liveExpedition.ReturnedAmount, offlineExpedition.ReturnedAmount);
        Assert.Equal(ResourceOpportunityState.Depleted, restored.State);
        Assert.Equal(
            live.Resources.Total(ResourceType.Wood),
            offline.Resources.Total(ResourceType.Wood));
    }

    private static CityWorld NewOpportunityWorld(bool unlockResourceSorties = true)
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingOpportunities();
        if (!unlockResourceSorties) return world;

        DepositCost(world, FoundingSiteModule.Campfire);
        ConstructionAuthorizationResult authorization =
            world.TryAuthorizeConstruction(ConstructionKind.FoundingSite);
        Assert.True(authorization.IsSuccess, authorization.Outcome.ToString());
        ConstructionProject project = world.Projects[authorization.ProjectId!.Value];
        CompleteActiveModule(world, project);
        CompleteModule(world, project, FoundingSiteModule.Cache);
        return world;
    }

    private static ResourceOpportunity Opportunity(
        CityWorld world,
        ResourceOpportunityKind kind) =>
        world.ResourceOpportunities.Values.Single(item => item.Kind == kind);

    private static void CompleteModule(
        CityWorld world,
        ConstructionProject project,
        FoundingSiteModule module)
    {
        DepositCost(world, module);
        ConstructionAuthorizationResult result =
            world.TryAuthorizeFoundingSiteModule(project.Id, module);
        Assert.True(result.IsSuccess, result.Outcome.ToString());
        CompleteActiveModule(world, project);
    }

    private static void CompleteActiveModule(CityWorld world, ConstructionProject project)
    {
        project.Progress = project.RequiredWork;
        world.AdvanceWorldTick();
    }

    private static void DepositCost(CityWorld world, FoundingSiteModule module)
    {
        foreach (RecipeInput input in FoundingSiteRules.InputsFor(module))
        {
            world.Resources.DepositToCityInventory(input.Resource, input.Amount);
        }
    }
}
