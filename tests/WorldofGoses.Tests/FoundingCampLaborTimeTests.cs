using System.Linq;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The founding camp has no Home yet, so every tick is a labour tick and the
/// configured workday (08:00–16:00) must not apply. That rule used to live only
/// in the per-tick simulation while mobilisation and arrival kept their own
/// GameClock checks. Authorising the founding site outside work hours therefore
/// assigned the founder and parked them at home: the worksite reported
/// WorkersInTransit and never progressed, with no error anywhere.
/// </summary>
public sealed class FoundingCampLaborTimeTests
{
    [Fact]
    public void FoundingSiteAuthorizedAtNight_PutsTheFounderToWorkAndProgresses()
    {
        CityWorld world = NightFoundingWorld();
        Assert.False(GameClock.IsWorkday(world.CurrentTick));

        ConstructionProject project = AuthorizeFoundingSite(world);
        Citizen founder = world.Hero!;

        // The founder must be travelling to the worksite, not parked at home.
        // This is the assertion that failed before the fix: MobiliseCitizen read
        // the raw workday and left them AtHome, so they never set out at all.
        Assert.Equal(CitizenLocation.InTransit, founder.CurrentLocation);

        Arrive(world, founder, project);
        AdvanceUntilProgress(world, project);

        Assert.True(
            project.Progress > 0,
            $"Founding site made no progress at night. Stop cause: {project.StopCause}.");
        Assert.Equal(CitizenLocation.AtWork, founder.CurrentLocation);
    }

    [Fact]
    public void ArrivalAtTheWorksite_IsNotReversedDuringTheFoundingCamp()
    {
        CityWorld world = NightFoundingWorld();
        ConstructionProject project = AuthorizeFoundingSite(world);
        Citizen founder = world.Hero!;

        // Drive the visible-route path the macro view uses.
        Assert.Equal(CitizenLocation.InTransit, founder.CurrentLocation);
        Assert.True(world.ConfirmCitizenArrivedAtAssignment(founder.Id, project.Id));
        Assert.Equal(CitizenLocation.AtWork, founder.CurrentLocation);
    }

    [Fact]
    public void CrossingIntoNight_DoesNotPullTheFounderOffTheWorksite()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        // Start close enough to 16:00 that the Campfire cannot finish before the
        // boundary. A completed module now releases its contributors, so
        // authorising at 08:00 and running the full workday would send the
        // founder home for a legitimate reason and stop testing the bypass.
        TestHelpers.SetTick(world, GameClock.WorkdayEndTick - 25);

        ConstructionProject project = AuthorizeFoundingSite(world);
        Citizen founder = world.Hero!;
        Arrive(world, founder, project);
        AdvanceUntilProgress(world, project);
        Assert.Equal(CitizenLocation.AtWork, founder.CurrentLocation);

        // Step through the 16:00 boundary. With no Home built, the founder holds
        // the worksite instead of being mobilised home.
        int safety = GameClock.TicksPerInGameDay;
        while (GameClock.IsWorkday(world.CurrentTick) && safety-- > 0)
        {
            world.AdvanceWorldTick();
        }

        Assert.False(GameClock.IsWorkday(world.CurrentTick));
        Assert.True(
            project.HasActiveWork,
            "The module must still be under way, or the founder would be free for an unrelated reason.");
        Assert.Equal(CitizenLocation.AtWork, founder.CurrentLocation);
        Assert.NotEqual(ConstructionStopCause.Night, project.StopCause);
    }

    [Fact]
    public void OnceTheShelterExists_TheWorkdayGovernsAgain()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Assert.True(world.HasCompletedFirstShelter());

        int safety = GameClock.TicksPerInGameDay;
        while (GameClock.IsWorkday(world.CurrentTick) && safety-- > 0)
        {
            world.AdvanceWorldTick();
        }

        Assert.False(GameClock.IsWorkday(world.CurrentTick));
        // The founding-camp exemption is over: night mobilisation applies.
        Assert.All(
            world.Citizens.Values.Where(
                citizen => citizen.Commitment.Kind != CitizenCommitmentKind.Expedition),
            citizen => Assert.NotEqual(CitizenLocation.AtWork, citizen.CurrentLocation));
    }

    /// <summary>
    /// The status strip must not announce "work paused" while the bypass has the
    /// founder building. The chip used to test GameClock.IsDaytime directly, so
    /// it showed the off-hours cue for the whole opening — the founding camp is
    /// night-time by definition, since a fresh world starts at 00:00.
    /// </summary>
    [Fact]
    public void OffHoursSignal_StaysSilentWhileTheFoundingCampBypassApplies()
    {
        CityWorld world = NightFoundingWorld();
        Assert.False(GameClock.IsWorkday(world.CurrentTick));
        Assert.False(world.HasCompletedFirstShelter());

        Assert.True(
            CityStatusSnapshot.From(world).IsLaborTime,
            "The founding camp must not read as off-hours.");

        CityWorld withHome = TestHelpers.WorldWithHome();
        TestHelpers.SetTick(withHome, GameClock.WorkdayEndTick);

        Assert.True(withHome.HasCompletedFirstShelter());
        Assert.False(
            CityStatusSnapshot.From(withHome).IsLaborTime,
            "Once a Home exists the configured workday governs again.");
    }

    /// <summary>
    /// The batched quiescent path used to relabel a Founding Site waiting
    /// between modules as NoWorkers, or as Night during the bypass, because it
    /// read the raw clock and ignored ConstructionSimulation's precedence. Both
    /// are causes the per-tick path would never produce for that project.
    /// </summary>
    [Fact]
    public void QuiescentBatch_KeepsAwaitingModuleAndNeverReportsNightDuringTheCamp()
    {
        CityWorld world = NightFoundingWorld();
        ConstructionProject project = AuthorizeFoundingSite(world);
        Citizen founder = world.Hero!;
        Arrive(world, founder, project);

        int safety = GameClock.TicksPerInGameDay;
        while (project.ActiveFoundingModule is not null && safety-- > 0)
        {
            world.AdvanceWorldTick();
        }

        Assert.True(project.HasCompletedFoundingModule(FoundingSiteModule.Campfire));
        Assert.Equal(ConstructionStopCause.AwaitingModule, project.StopCause);

        // No assignment and no active work is exactly what lets the batch path
        // run at all, so this is the reachable case.
        Assert.Equal(0, project.AssignedCount);
        WorldTimeAdvance.Advance(world, GameClock.TicksPerInGameDay);

        Assert.Equal(ConstructionStopCause.AwaitingModule, project.StopCause);
        Assert.NotEqual(ConstructionStopCause.Night, project.StopCause);
    }

    /// <summary>A founding-camp world whose clock sits outside work hours.</summary>
    private static CityWorld NightFoundingWorld()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        Assert.False(world.HasCompletedFirstShelter());
        int safety = GameClock.TicksPerInGameDay;
        while (GameClock.IsWorkday(world.CurrentTick) && safety-- > 0)
        {
            world.AdvanceWorldTick();
        }
        Assert.False(GameClock.IsWorkday(world.CurrentTick));
        return world;
    }

    private static ConstructionProject AuthorizeFoundingSite(CityWorld world)
    {
        foreach (RecipeInput input in FoundingSiteRules.InputsFor(FoundingSiteModule.Campfire))
        {
            world.Resources.DepositToCityInventory(input.Resource, input.Amount);
        }
        ConstructionAuthorizationResult result =
            world.TryAuthorizeConstruction(ConstructionKind.FoundingSite);
        Assert.True(result.IsSuccess, result.Outcome.ToString());
        return world.Projects.Values.Single();
    }

    /// <summary>
    /// Completes the visible route the way MacroStreetLiveView does when the
    /// walking citizen reaches the worksite. The stepped world tick deliberately
    /// does not finish visible travel on its own.
    /// </summary>
    private static void Arrive(CityWorld world, Citizen citizen, ConstructionProject project)
    {
        Assert.True(
            world.ConfirmCitizenArrivedAtAssignment(citizen.Id, project.Id),
            "Arrival at the worksite was refused.");
    }

    private static void AdvanceUntilProgress(CityWorld world, ConstructionProject project)
    {
        int safety = ConstructionRules.WorkIntervalTicks * 4;
        while (project.Progress == 0 && safety-- > 0)
        {
            world.AdvanceWorldTick();
        }
    }
}
