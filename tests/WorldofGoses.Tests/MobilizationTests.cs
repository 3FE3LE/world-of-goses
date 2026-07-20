using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public class MobilizationTests
{
    [Fact]
    public void Scenario_HomeBuildingExistsWithPopulationCapacity()
    {
        var world = TestHelpers.NewProductionWorld();
        var home = world.PrimaryHome;

        Assert.NotNull(home);
        Assert.Equal(BuildingKind.Home, home!.Kind);
        Assert.Equal(5, home.WorkerCapacity);
    }

    [Fact]
    public void Scenario_AssignedCitizensStartAtWork()
    {
        var world = TestHelpers.NewProductionWorld();
        Assert.Equal(CitizenLocation.AtWork, world.GetCitizen(new CitizenId(1))!.CurrentLocation);
        Assert.Equal(CitizenLocation.AtWork, world.GetCitizen(new CitizenId(2))!.CurrentLocation);
        Assert.Equal(CitizenLocation.AtWork, world.GetCitizen(new CitizenId(3))!.CurrentLocation);
    }

    [Fact]
    public void Scenario_UnassignedCitizensStartAtHome()
    {
        var world = TestHelpers.NewProductionWorld();
        Assert.Equal(CitizenLocation.AtHome, world.GetCitizen(new CitizenId(4))!.CurrentLocation);
        Assert.Equal(CitizenLocation.AtHome, world.GetCitizen(new CitizenId(5))!.CurrentLocation);
    }

    [Fact]
    public void AdvanceWorldTick_AtSunset_MovesEveryoneToHome()
    {
        var world = TestHelpers.NewProductionWorld();
        // Skip to sunset (tick DayTicks = first night tick).
        for (int t = 0; t < GameClock.DayTicks; t++)
        {
            world.AdvanceWorldTick();
        }

        // After sunset, all citizens are at home regardless of assignment.
        foreach (var citizen in world.Citizens.Values)
        {
            Assert.Equal(CitizenLocation.AtHome, citizen.CurrentLocation);
        }
    }

    [Fact]
    public void AdvanceWorldTick_AtSunrise_AssignedReturnToWorkUnassignedStayHome()
    {
        var world = TestHelpers.NewProductionWorld();
        // First go to night.
        for (int t = 0; t < GameClock.DayTicks; t++)
        {
            world.AdvanceWorldTick();
        }
        // Then to next sunrise.
        for (int t = 0; t < GameClock.NightTicks; t++)
        {
            world.AdvanceWorldTick();
        }

        Assert.Equal(CitizenLocation.AtWork, world.GetCitizen(new CitizenId(1))!.CurrentLocation);
        Assert.Equal(CitizenLocation.AtWork, world.GetCitizen(new CitizenId(2))!.CurrentLocation);
        Assert.Equal(CitizenLocation.AtWork, world.GetCitizen(new CitizenId(3))!.CurrentLocation);
        Assert.Equal(CitizenLocation.AtHome, world.GetCitizen(new CitizenId(4))!.CurrentLocation);
        Assert.Equal(CitizenLocation.AtHome, world.GetCitizen(new CitizenId(5))!.CurrentLocation);
    }

    [Fact]
    public void Mobilization_DoesNotChangeCurrentAssignment()
    {
        var world = TestHelpers.NewProductionWorld();
        var bran = world.GetCitizen(new CitizenId(1))!;
        var initialAssignment = bran.CurrentAssignment;

        for (int t = 0; t < GameClock.DayTicks + GameClock.NightTicks + 5; t++)
        {
            world.AdvanceWorldTick();
        }

        Assert.Equal(initialAssignment, bran.CurrentAssignment);
    }

    [Fact]
    public void GetCurrentlyVisibleOccupants_Quarry_DuringDay_ReturnsAssigned()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        var visible = world.GetCurrentlyVisibleOccupants(quarry);

        Assert.Equal(2, visible.Count);
        Assert.Contains(new CitizenId(1), visible);
        Assert.Contains(new CitizenId(2), visible);
    }

    [Fact]
    public void GetCurrentlyVisibleOccupants_Quarry_DuringNight_ReturnsEmpty()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;

        for (int t = 0; t < GameClock.DayTicks; t++)
        {
            world.AdvanceWorldTick();
        }

        var visible = world.GetCurrentlyVisibleOccupants(quarry);
        Assert.Empty(visible);
    }

    [Fact]
    public void GetCurrentlyVisibleOccupants_Home_DuringDay_ReturnsOnlyUnassigned()
    {
        var world = TestHelpers.NewProductionWorld();
        var home = world.PrimaryHome!;
        var visible = world.GetCurrentlyVisibleOccupants(home);

        // Two unassigned citizens live at home.
        Assert.Equal(2, visible.Count);
        Assert.Contains(new CitizenId(4), visible);
        Assert.Contains(new CitizenId(5), visible);
    }

    [Fact]
    public void GetCurrentlyVisibleOccupants_Home_DuringNight_ReturnsEveryone()
    {
        var world = TestHelpers.NewProductionWorld();
        var home = world.PrimaryHome!;

        for (int t = 0; t < GameClock.DayTicks; t++)
        {
            world.AdvanceWorldTick();
        }

        var visible = world.GetCurrentlyVisibleOccupants(home);
        Assert.Equal(5, visible.Count);
    }

    [Fact]
    public void Home_DoesNotProduceStock()
    {
        var world = TestHelpers.NewProductionWorld();
        var home = world.PrimaryHome!;

        for (int t = 0; t < 100; t++)
        {
            world.AdvanceWorldTick();
        }

        Assert.Equal(0, home.Stock);
        Assert.Equal(0, home.LastTickProduction);
    }

    [Fact]
    public void Home_DoesNotConsumeUpkeep()
    {
        // Disable Quarry production, fill it with stone, and confirm
        // upkeep drains exactly citizens/5 stone/tick (not more).
        // If Home were consuming upkeep, the drain would be higher.
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        quarry.ConfigureProductionPolicy(enabled: false, targetStock: quarry.StorageCapacity);
        quarry.AddStock(50);

        int before = quarry.Stock;
        world.AdvanceWorldTick();
        int drained = before - quarry.Stock;

        Assert.Equal(1, drained); // 5 citizens → 1 stone/tick, not 2.
    }
}
