using System.Linq;
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
            world.AdvanceOfflineWorldTick();
        }

        // After sunset, all citizens are at home regardless of assignment.
        foreach (var citizen in world.Citizens.Values)
        {
            Assert.Equal(CitizenLocation.AtHome, citizen.CurrentLocation);
        }
    }

    [Fact]
    public void AdvanceWorldTick_AtSunrise_ReevaluatesEachStandingOrder()
    {
        var world = TestHelpers.NewProductionWorld();
        // First go to night.
        for (int t = 0; t < GameClock.DayTicks; t++)
        {
            world.AdvanceOfflineWorldTick();
        }
        // Then to next sunrise.
        for (int t = 0; t < GameClock.NightTicks; t++)
        {
            world.AdvanceOfflineWorldTick();
        }

        Assert.Equal(CitizenLocation.AtHome, world.GetCitizen(new CitizenId(1))!.CurrentLocation);
        Assert.Equal(CitizenLocation.AtHome, world.GetCitizen(new CitizenId(2))!.CurrentLocation);
        Assert.Equal(CitizenLocation.InTransit, world.GetCitizen(new CitizenId(3))!.CurrentLocation);
        Assert.Equal(CitizenLocation.AtHome, world.GetCitizen(new CitizenId(4))!.CurrentLocation);
        Assert.Equal(CitizenLocation.AtHome, world.GetCitizen(new CitizenId(5))!.CurrentLocation);
    }

    [Fact]
    public void Mobilization_DoesNotChangeCurrentAssignment()
    {
        // Stock saturation pauses execution but preserves the standing
        // work order across day/night mobilisation.
        var world = TestHelpers.NewProductionWorld();
        var bran = world.GetCitizen(new CitizenId(1))!;
        var initialAssignment = bran.CurrentAssignment;

        for (int t = 0; t < GameClock.DayTicks + GameClock.NightTicks + 5; t++)
        {
            world.AdvanceOfflineWorldTick();
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
    public void AssignDuringDay_WaitsForArrivalBeforeProductionBegins()
    {
        var world = TestHelpers.WorldWithHome();
        var farm = world.Buildings.Values.First(building => building.Kind == BuildingKind.Farm);
        var citizen = world.Hero!;

        int stockBefore = farm.Stock;
        int rateBefore = world.CurrentProductionRate(farm.Id);
        Assert.True(world.TryAssignCitizen(farm.Id, citizen.Id).IsSuccess);

        Assert.Equal(CitizenLocation.InTransit, citizen.CurrentLocation);
        Assert.Equal(rateBefore, world.CurrentProductionRate(farm.Id));
        Assert.DoesNotContain(citizen.Id, world.GetCurrentlyVisibleOccupants(farm));
        world.AdvanceWorldTick();
        Assert.Equal(stockBefore, farm.Stock);
        Assert.Equal(ProductionStopCause.WorkersInTransit, farm.StopCause);

        Assert.True(world.ConfirmCitizenArrivedAtAssignment(citizen.Id, farm.Id));
        Assert.Equal(CitizenLocation.AtWork, citizen.CurrentLocation);
        Assert.True(world.CurrentProductionRate(farm.Id) > rateBefore);
        TestHelpers.AdvanceToNextProductionCycle(world);
        Assert.True(farm.Stock > stockBefore);
    }

    [Fact]
    public void ArrivalAtEightAm_ActivatesAssignedWorker()
    {
        var world = TestHelpers.WorldWithHome();
        Building quarry = TestHelpers.NewBuilding(new BuildingId(9100));
        world.RegisterBuilding(quarry);
        Citizen founder = world.Hero!;
        int eightAmTick = GameClock.TicksPerInGameDay / 3;
        while (world.CurrentTick < eightAmTick) world.AdvanceWorldTick();

        Assert.True(world.TryAssignCitizen(quarry.Id, founder.Id).IsSuccess);
        Assert.Equal(CitizenLocation.InTransit, founder.CurrentLocation);
        Assert.True(world.ConfirmCitizenArrivedAtAssignment(founder.Id, quarry.Id));

        Assert.Equal(CitizenLocation.AtWork, founder.CurrentLocation);
        Assert.Contains(founder.Id, world.GetCurrentlyVisibleOccupants(quarry));
    }

    [Fact]
    public void ArrivalAfterWorkday_ReversesJourneyWithoutDroppingStandingOrder()
    {
        var world = TestHelpers.WorldWithHome();
        Building quarry = TestHelpers.NewBuilding(new BuildingId(9101));
        world.RegisterBuilding(quarry);
        Citizen founder = world.Hero!;

        Assert.True(world.TryAssignCitizen(quarry.Id, founder.Id).IsSuccess);
        // Advance past the workday end (16:00) so the arrival is
        // tested against the off-hours boundary. The post-2026-07-30
        // workday runs 08:00–16:00 (ticks 1200–2400), so this loop
        // walks to just past tick 2400.
        while (world.CurrentTick < GameClock.WorkdayEndTick) world.AdvanceWorldTick();

        Assert.False(world.ConfirmCitizenArrivedAtAssignment(founder.Id, quarry.Id));
        Assert.Equal(quarry.Id, founder.CurrentAssignment);
        Assert.Equal(CitizenLocation.InTransit, founder.CurrentLocation);
        Assert.True(founder.IsReturningHome);
        Assert.DoesNotContain(founder.Id, world.GetCurrentlyVisibleOccupants(quarry));
    }

    [Fact]
    public void FullStorage_PreservesStandingOrderWithoutUnnecessaryTravel()
    {
        var world = new CityWorld();
        // Stand-alone worlds start at tick 0 (night, post-2026-07-30).
        // Advance to the workday so the standing-order mobilisation
        // fires; the test's arrival step depends on that.
        TestHelpers.AdvanceToWorkday(world);
        Citizen citizen = TestHelpers.NewCitizen(42);
        world.RegisterCitizen(citizen);
        var farm = new Building(
            new BuildingId(42),
            "Arrival test farm",
            BuildingKind.Farm,
            ResourceType.Food,
            CompetencyId.Farming,
            workerCapacity: 1,
            visualCapacity: 1,
            baseProductionPerWorker: 1,
            storageCapacity: 20,
            resourceLabel: "Food",
            resourceUnit: "food");
        farm.AddStock(farm.MaxStock);
        world.RegisterBuilding(farm);

        Assert.True(world.TryAssignCitizen(farm.Id, citizen.Id).IsSuccess);
        Assert.Equal(farm.Id, citizen.CurrentAssignment);
        Assert.True(farm.IsAssigned(citizen.Id));
        Assert.Equal(CitizenLocation.AtHome, citizen.CurrentLocation);

        Assert.True(farm.TryConsumeStock(farm.Stock));
        world.AdvanceWorldTick();

        Assert.Equal(CitizenLocation.InTransit, citizen.CurrentLocation);
    }

    [Fact]
    public void FullStorage_ReleasesArrivedWorkerButPreservesTravellingWorkerCommitment()
    {
        var world = new CityWorld();
        // Advance to the configured workday so the arrival check in
        // ConfirmCitizenArrivedAtAssignment does not reverse the
        // journey. Tests that build worlds ad-hoc instead of going
        // through TestHelpers.NewProductionWorld must do this too
        // since the 2026-07-30 workday change moved the dawn to
        // 08:00 (tick 1200).
        TestHelpers.AdvanceToWorkday(world);
        Citizen arrived = TestHelpers.NewCitizen(42);
        Citizen travelling = TestHelpers.NewCitizen(43);
        world.RegisterCitizen(arrived);
        world.RegisterCitizen(travelling);
        var farm = new Building(
            new BuildingId(42),
            "Mixed arrival farm",
            BuildingKind.Farm,
            ResourceType.Food,
            CompetencyId.Farming,
            workerCapacity: 2,
            visualCapacity: 2,
            baseProductionPerWorker: 1,
            storageCapacity: 20,
            resourceLabel: "Food",
            resourceUnit: "food");
        world.RegisterBuilding(farm);

        Assert.True(world.TryAssignCitizen(farm.Id, arrived.Id).IsSuccess);
        Assert.True(world.ConfirmCitizenArrivedAtAssignment(arrived.Id, farm.Id));
        farm.AddStock(farm.MaxStock);
        Assert.True(world.TryAssignCitizen(farm.Id, travelling.Id).IsSuccess);

        for (int tick = 0; tick < Building.MaxStockReleaseCooldown + 2; tick++)
        {
            world.AdvanceWorldTick();
        }

        Assert.Equal(farm.Id, arrived.CurrentAssignment);
        Assert.Equal(CitizenLocation.InTransit, arrived.CurrentLocation);
        Assert.True(arrived.IsReturningHome);
        Assert.Equal(farm.Id, travelling.CurrentAssignment);
        Assert.True(farm.IsAssigned(travelling.Id));
        Assert.Equal(CitizenLocation.AtHome, travelling.CurrentLocation);
        Assert.False(travelling.IsReturningHome);
    }

    [Fact]
    public void GetCurrentlyVisibleOccupants_Quarry_DuringNight_ReturnsEmpty()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;

        for (int t = 0; t < GameClock.DayTicks; t++)
        {
            world.AdvanceOfflineWorldTick();
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
            world.AdvanceOfflineWorldTick();
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
        // Upkeep is dormant: no building drains stone on its own. This
        // test pins that behaviour by disabling Quarry production and
        // confirming its stock survives a tick unchanged.
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        quarry.ConfigureProductionPolicy(enabled: false, minStock: 0, maxStock: quarry.StorageCapacity, priority: 0);
        quarry.AddStock(50);

        int before = quarry.Stock;
        world.AdvanceWorldTick();
        int drained = before - quarry.Stock;

        Assert.Equal(0, drained);
    }
}
