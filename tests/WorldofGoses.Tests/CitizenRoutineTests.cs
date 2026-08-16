using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class CitizenRoutineTests
{
    [Fact]
    public void CitizenSave_HasSemanticContextButNoAuthoritativeVisualCoordinates()
    {
        string[] properties = System.Array.ConvertAll(
            typeof(CitizenSave).GetProperties(),
            property => property.Name);

        Assert.Contains(nameof(CitizenSave.CurrentLocation), properties);
        Assert.Contains(nameof(CitizenSave.TransitStartedAtTick), properties);
        Assert.DoesNotContain("GlobalPosition", properties);
        Assert.DoesNotContain("LocalPosition", properties);
        Assert.DoesNotContain("PositionX", properties);
        Assert.DoesNotContain("PositionY", properties);
    }

    [Fact]
    public void AssignedCitizen_InTransit_ExposesSemanticJourneyTiming()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        Citizen worker = world.GetCitizen(new CitizenId(4))!;
        Building quarry = world.GetBuilding(new BuildingId(1))!;

        AssignmentResult assigned = world.TryAssignCitizen(quarry.Id, worker.Id);

        Assert.True(assigned.IsSuccess);
        CitizenRoutineSnapshot routine = world.GetCitizenRoutine(worker.Id)!;
        Assert.Equal(CitizenRoutineActivity.TravellingToWork, routine.Activity);
        Assert.Equal(CitizenContextLocation.InTransit, routine.ContextLocation);
        Assert.Equal(world.PrimaryHome!.Id, routine.TransitOriginId);
        Assert.Equal(quarry.Id, routine.TransitDestinationId);
        Assert.Equal(world.CurrentTick, routine.ActivityStartedAtTick);
        Assert.Equal(world.CurrentTick + world.Hero!.TransitDurationTicks, routine.ExpectedCompletionTick);
    }

    [Fact]
    public void FullWorkplace_PreservesOrderAndExplainsStorageWaitAtHome()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        Building quarry = world.GetBuilding(new BuildingId(1))!;
        Citizen hero = world.Hero!;
        quarry.AddStock(quarry.MaxStock);
        hero.SetLocation(CitizenLocation.AtHome);

        CitizenRoutineSnapshot routine = world.GetCitizenRoutine(hero.Id)!;

        Assert.Equal(quarry.Id, routine.WorkOrder!.Value.TargetId);
        Assert.Equal(CitizenRoutineActivity.WaitingForStorage, routine.Activity);
        Assert.Equal(CitizenRoutineBlockReason.StorageFull, routine.BlockReason);
        Assert.Equal(CitizenContextLocation.AtShelter, routine.ContextLocation);
    }

    [Fact]
    public void UnassignedCitizen_UsesLeisureDuringWorkdayAndRestAtNight()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        Citizen citizen = world.GetCitizen(new CitizenId(4))!;

        Assert.Equal(CitizenRoutineActivity.Leisure, world.GetCitizenRoutine(citizen.Id)!.Activity);

        while (GameClock.IsWorkday(world.CurrentTick)) world.AdvanceWorldTick();

        CitizenRoutineSnapshot night = world.GetCitizenRoutine(citizen.Id)!;
        Assert.Equal(CitizenRoutineActivity.Resting, night.Activity);
        Assert.Equal(GameClock.NextWorkdayStart(world.CurrentTick), night.NextTransitionTick);
    }

    [Fact]
    public void AbsentWorkers_DoNotHideFullStorageStopCause()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        Building quarry = world.GetBuilding(new BuildingId(1))!;
        quarry.AddStock(quarry.MaxStock);
        foreach (CitizenId citizenId in quarry.AssignedCitizenIds)
        {
            world.GetCitizen(citizenId)!.SetLocation(CitizenLocation.AtHome);
        }

        world.SimulateBuildingTick(quarry);

        Assert.Equal(ProductionStopCause.TargetReached, quarry.StopCause);
    }
}
