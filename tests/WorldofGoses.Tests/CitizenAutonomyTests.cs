using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public class CitizenAutonomyTests
{
    /// <summary>
    /// The inverse of the rule this test used to guard. Until DEC-0023 a live
    /// journey could only end when Godot reported its sprite had arrived, so
    /// elapsed ticks deliberately did nothing. Now world time is the only
    /// authority: the citizen arrives on the arrival tick whether or not
    /// anything is being drawn, and not one tick earlier.
    /// </summary>
    [Fact]
    public void LiveTicks_CompleteAnAssignmentRouteByElapsedTime()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Citizen hero = world.Hero!;
        Building quarry = TestHelpers.NewBuilding(new BuildingId(49));
        quarry.DepositIron(100);
        world.RegisterBuilding(quarry);
        Assert.True(world.TryAssignCitizen(quarry.Id, hero.Id).IsSuccess);

        for (int tick = 0; tick < CityEconomyRules.AbstractTravelTicks - 1; tick++)
        {
            world.AdvanceWorldTick();
        }

        Assert.Equal(CitizenLocation.InTransit, hero.CurrentLocation);
        Assert.Equal(0, quarry.Stock);

        world.AdvanceWorldTick();

        Assert.Equal(CitizenLocation.AtWork, hero.CurrentLocation);
    }

    [Fact]
    public void ExhaustionWithoutFood_BlocksLifeSupportButPreservesPlayerOrder()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Citizen hero = world.Hero!;
        Building farm = world.Buildings.Values.First(building => building.Kind == BuildingKind.Farm);
        farm.ConfigureProductionPolicy(false, 0, farm.StorageCapacity);
        Building quarry = TestHelpers.NewBuilding(new BuildingId(50));
        quarry.DepositIron(100);
        world.RegisterBuilding(quarry);
        Assert.True(world.TryAssignCitizen(quarry.Id, hero.Id).IsSuccess);
        TestHelpers.PlaceAtAssignment(world, hero.Id);
        hero.ConsumeStamina(hero.CurrentStamina - (CitizenNeedsRules.InterruptAtStamina + 2));

        TestHelpers.AdvanceToNextProductionCycle(world);
        for (int tick = 0; tick <= CityEconomyRules.AbstractTravelTicks; tick++)
        {
            world.AdvanceWorldTick();
        }

        Assert.Equal(quarry.Id, hero.CurrentAssignment);
        Assert.Equal(CitizenLocation.AtHome, hero.CurrentLocation);
        Assert.Equal(CitizenVitalStatus.BlockedNoFood, hero.VitalStatus);
        Assert.Equal(ProductionStopCause.WorkersBlockedNoFood, quarry.StopCause);
    }

    [Fact]
    public void FoodAndRest_ResumeTheLatestStandingOrder()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Citizen hero = world.Hero!;
        Building farm = world.Buildings.Values.First(building => building.Kind == BuildingKind.Farm);
        farm.ConfigureProductionPolicy(false, 0, farm.StorageCapacity);
        Building quarry = TestHelpers.NewBuilding(new BuildingId(51));
        quarry.DepositIron(100);
        world.RegisterBuilding(quarry);
        Assert.True(world.TryAssignCitizen(quarry.Id, hero.Id).IsSuccess);
        TestHelpers.PlaceAtAssignment(world, hero.Id);
        hero.ConsumeStamina(hero.CurrentStamina - (CitizenNeedsRules.InterruptAtStamina + 2));
        TestHelpers.AdvanceToNextProductionCycle(world);
        for (int tick = 0; tick <= CityEconomyRules.AbstractTravelTicks; tick++)
        {
            world.AdvanceWorldTick();
        }
        Assert.Equal(CitizenVitalStatus.BlockedNoFood, hero.VitalStatus);

        Assert.Equal(1, world.DepositFood(1));
        for (int tick = 0; tick < CitizenNeedsRules.ResumeAtStamina; tick++)
        {
            world.AdvanceWorldTick();
            if (hero.CurrentLocation == CitizenLocation.InTransit) break;
        }

        Assert.Equal(CitizenVitalStatus.Stable, hero.VitalStatus);
        Assert.Equal(quarry.Id, hero.CurrentAssignment);
        Assert.Equal(CitizenLocation.InTransit, hero.CurrentLocation);

        for (int tick = 0; tick < CityEconomyRules.AbstractTravelTicks; tick++)
        {
            world.AdvanceWorldTick();
        }
        Assert.Equal(CitizenLocation.AtWork, hero.CurrentLocation);
    }

    [Fact]
    public void Expedition_InterruptsAndRoundTripsWithoutDeletingStandingOrder()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Citizen hero = world.Hero!;
        Building quarry = TestHelpers.NewBuilding(new BuildingId(52));
        quarry.DepositIron(100);
        world.RegisterBuilding(quarry);
        Assert.True(world.TryAssignCitizen(quarry.Id, hero.Id).IsSuccess);
        TestHelpers.PlaceAtAssignment(world, hero.Id);
        world.DepositResource(ResourceType.Wood, 2);
        ExpeditionRequest request = ExpeditionRequest.Reconnaissance(hero.Id);

        ExpeditionStartResult started = world.StartExpedition(request);

        Assert.True(started.IsSuccess);
        Assert.Equal(quarry.Id, hero.CurrentAssignment);
        Assert.Equal(CitizenCommitmentKind.Expedition, hero.Commitment.Kind);
        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.Capture(world));
        Assert.Equal(quarry.Id, restored.Hero!.CurrentAssignment);
        Assert.Equal(CitizenCommitmentKind.Expedition, restored.Hero.Commitment.Kind);

        for (int tick = 0; tick < request.DurationTicks; tick++)
        {
            restored.AdvanceWorldTick();
        }

        Assert.Equal(quarry.Id, restored.Hero.CurrentAssignment);
        Assert.Equal(CitizenCommitmentKind.BuildingWork, restored.Hero.Commitment.Kind);
        Assert.Equal(CitizenVitalStatus.Recovering, restored.Hero.VitalStatus);
        Assert.Equal(CitizenLocation.AtHome, restored.Hero.CurrentLocation);
        Assert.True(restored.Hero.ResumeWorkNotBeforeTick > restored.CurrentTick);
    }

    [Fact]
    public void AssignmentTravel_SaveLoadAndOfflineArrivalRemainEquivalent()
    {
        CityWorld live = TestHelpers.WorldWithHome();
        Citizen liveHero = live.Hero!;
        Building liveQuarry = TestHelpers.NewBuilding(new BuildingId(53));
        liveQuarry.DepositIron(100);
        live.RegisterBuilding(liveQuarry);
        Assert.True(live.TryAssignCitizen(liveQuarry.Id, liveHero.Id).IsSuccess);
        for (int tick = 0; tick < 11; tick++) live.AdvanceWorldTick();

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.Capture(live));
        Citizen restoredHero = restored.Hero!;
        Assert.Equal(CitizenLocation.InTransit, restoredHero.CurrentLocation);
        Assert.Equal(liveHero.TransitStartedAtTick, restoredHero.TransitStartedAtTick);

        for (int tick = 0; tick < CityEconomyRules.AbstractTravelTicks; tick++)
        {
            live.AdvanceWorldTick();
            restored.AdvanceWorldTick();
        }

        Assert.Equal(liveHero.CurrentLocation, restoredHero.CurrentLocation);
        Assert.Equal(CitizenLocation.AtWork, restoredHero.CurrentLocation);
        Assert.Equal(liveQuarry.Stock, restored.GetBuilding(liveQuarry.Id)!.Stock);
        Assert.Equal(liveHero.CurrentStamina, restoredHero.CurrentStamina);
    }

    [Fact]
    public void RecoveryTravel_SaveLoadDoesNotFeedCitizenBeforeShelterArrival()
    {
        CityWorld live = TestHelpers.WorldWithHome();
        Citizen hero = live.Hero!;
        Building farm = live.Buildings.Values.First(building => building.Kind == BuildingKind.Farm);
        farm.ConfigureProductionPolicy(false, 0, farm.StorageCapacity);
        Building quarry = TestHelpers.NewBuilding(new BuildingId(54));
        quarry.DepositIron(100);
        live.RegisterBuilding(quarry);
        Assert.True(live.TryAssignCitizen(quarry.Id, hero.Id).IsSuccess);
        TestHelpers.PlaceAtAssignment(live, hero.Id);
        hero.ConsumeStamina(hero.CurrentStamina - (CitizenNeedsRules.InterruptAtStamina + 2));
        live.DepositFood(1);
        TestHelpers.AdvanceToNextProductionCycle(live);
        Assert.True(hero.IsReturningHome);
        int foodDuringTravel = live.FoodStock;

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.Capture(live));
        for (int tick = 0; tick < CityEconomyRules.AbstractTravelTicks - 1; tick++)
        {
            restored.AdvanceWorldTick();
        }

        Assert.Equal(CitizenLocation.InTransit, restored.Hero!.CurrentLocation);
        Assert.Equal(foodDuringTravel, restored.FoodStock);

        restored.AdvanceWorldTick();
        Assert.Equal(CitizenLocation.AtHome, restored.Hero.CurrentLocation);
        Assert.Equal(foodDuringTravel - 1, restored.FoodStock);
    }
}
