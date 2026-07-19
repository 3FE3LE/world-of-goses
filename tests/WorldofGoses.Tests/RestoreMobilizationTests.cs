using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public class RestoreMobilizationTests
{
    [Fact]
    public void Restore_MidDayTick_AssignedCitizensLandAtWork()
    {
        // Seed advances to a mid-day tick (no transition from the
        // default initial state since seed starts at tick 0 day and
        // never leaves day within a few ticks).
        var world = new CityWorld();
        for (int t = 0; t < 1500; t++) // hour 10 of day 1
        {
            world.AdvanceWorldTick();
        }
        var save = WorldPersistence.Capture(world);

        // Restore: with no transition having fired between saved
        // tick and any new tick, the default CurrentLocation (AtHome)
        // would stick — except Restore now seeds it explicitly.
        var restored = CityWorld.FromSave(save);

        Assert.Equal(CitizenLocation.AtWork, restored.GetCitizen(new CitizenId(1))!.CurrentLocation);
        Assert.Equal(CitizenLocation.AtWork, restored.GetCitizen(new CitizenId(2))!.CurrentLocation);
        Assert.Equal(CitizenLocation.AtWork, restored.GetCitizen(new CitizenId(3))!.CurrentLocation);
        Assert.Equal(CitizenLocation.AtHome, restored.GetCitizen(new CitizenId(4))!.CurrentLocation);
        Assert.Equal(CitizenLocation.AtHome, restored.GetCitizen(new CitizenId(5))!.CurrentLocation);
    }

    [Fact]
    public void Restore_MidNightTick_AllCitizensLandAtHome()
    {
        var world = new CityWorld();
        // Tick 0 is daytime. Skip to mid-night of day 1.
        for (int t = 0; t < GameClock.DayTicks + 600; t++) // hour 4 of night
        {
            world.AdvanceWorldTick();
        }
        var save = WorldPersistence.Capture(world);

        var restored = CityWorld.FromSave(save);

        foreach (var citizen in restored.Citizens.Values)
        {
            Assert.Equal(CitizenLocation.AtHome, citizen.CurrentLocation);
        }
    }

    [Fact]
    public void Restore_Hour10Tick_QuarryShowsAssignedWorkers()
    {
        var world = new CityWorld();
        for (int t = 0; t < 1500; t++) // hour 10 of day 1
        {
            world.AdvanceWorldTick();
        }
        var save = WorldPersistence.Capture(world);
        var restored = CityWorld.FromSave(save);

        var quarry = restored.GetBuilding(new BuildingId(1))!;
        var visible = restored.GetCurrentlyVisibleOccupants(quarry);
        Assert.Equal(2, visible.Count);
        Assert.Contains(new CitizenId(1), visible);
        Assert.Contains(new CitizenId(2), visible);

        var home = restored.PrimaryHome!;
        var homeVisible = restored.GetCurrentlyVisibleOccupants(home);
        Assert.Equal(2, homeVisible.Count);
        Assert.Contains(new CitizenId(4), homeVisible);
        Assert.Contains(new CitizenId(5), homeVisible);
    }
}
