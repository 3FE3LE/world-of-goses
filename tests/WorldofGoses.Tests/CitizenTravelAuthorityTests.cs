using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The domain is the only authority that ends a citizen's journey (DEC-0023).
///
/// <para>
/// Before A2 a live journey could only end when <c>MacroStreetLiveView</c>
/// reported its sprite had reached an anchor, while offline catch-up ended the
/// same journey on elapsed ticks. Two authorities for one fact meant a stalled
/// animation could hold a citizen in transit indefinitely and keep their
/// workplace on <c>WorkersInTransit</c> forever.
/// </para>
///
/// <para>
/// Every test here asserts the same property from a different angle: given the
/// same world and the same number of ticks, stepping and catching up land on
/// the same state, and nothing outside the domain has a say in when.
/// </para>
/// </summary>
public sealed class CitizenTravelAuthorityTests
{
    /// <summary>
    /// Steps a world one tick at a time — the live path.
    /// </summary>
    private static void AdvanceLive(CityWorld world, int ticks)
    {
        for (int tick = 0; tick < ticks; tick++) world.AdvanceWorldTick();
    }

    /// <summary>
    /// Advances a world through the offline seam, which batches whole quiescent
    /// ranges instead of stepping. Both must produce the same world.
    /// </summary>
    private static void AdvanceOffline(CityWorld world, int ticks) =>
        WorldTimeAdvance.Advance(world, ticks);

    /// <summary>
    /// The comparison the whole slice exists to make true. Serialising both
    /// worlds catches divergence in any persisted field, not only the ones a
    /// given test happened to think of.
    /// </summary>
    private static void AssertSameWorld(CityWorld live, CityWorld offline)
    {
        Assert.Equal(live.CurrentTick, offline.CurrentTick);
        // A fixed capture stamp: the snapshot records when it was taken, and
        // comparing two wall clocks would fail on timing rather than on state.
        Assert.Equal(
            WorldPersistence.SerializeToJson(
                WorldPersistence.Capture(live, System.DateTimeOffset.UnixEpoch.AddDays(2))),
            WorldPersistence.SerializeToJson(
                WorldPersistence.Capture(offline, System.DateTimeOffset.UnixEpoch.AddDays(2))));
    }

    private static (CityWorld World, Citizen Hero, Building Quarry) AssignedWorld(int quarryId)
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Citizen hero = world.Hero!;
        Building quarry = TestHelpers.NewBuilding(new BuildingId(quarryId));
        quarry.DepositIron(100);
        world.RegisterBuilding(quarry);
        Assert.True(world.TryAssignCitizen(quarry.Id, hero.Id).IsSuccess);
        Assert.Equal(CitizenLocation.InTransit, hero.CurrentLocation);
        return (world, hero, quarry);
    }

    [Fact]
    public void TravelToWork_EndsOnTheArrivalTickAndNotBefore()
    {
        (CityWorld world, Citizen hero, Building quarry) = AssignedWorld(8100);
        int arrivesAt = hero.TravelArrivalTick!.Value;
        Assert.Equal(hero.TransitStartedAtTick!.Value + CityEconomyRules.AbstractTravelTicks, arrivesAt);

        while (world.CurrentTick < arrivesAt - 1) world.AdvanceWorldTick();
        Assert.Equal(CitizenLocation.InTransit, hero.CurrentLocation);
        Assert.Equal(ProductionStopCause.WorkersInTransit, quarry.StopCause);

        world.AdvanceWorldTick();

        Assert.Equal(arrivesAt, world.CurrentTick);
        Assert.Equal(CitizenLocation.AtWork, hero.CurrentLocation);
    }

    [Fact]
    public void TravelToWork_LiveAndOfflineReachTheSameState()
    {
        (CityWorld live, _, _) = AssignedWorld(8101);
        CityWorld offline = WorldPersistence.FromSave(WorldPersistence.Capture(live));

        AdvanceLive(live, CityEconomyRules.AbstractTravelTicks * 2);
        AdvanceOffline(offline, CityEconomyRules.AbstractTravelTicks * 2);

        Assert.Equal(CitizenLocation.AtWork, live.Hero!.CurrentLocation);
        AssertSameWorld(live, offline);
    }

    [Fact]
    public void TravelHome_LiveAndOfflineReachTheSameState()
    {
        // No standing order, so the return is the whole story: a citizen with a
        // work order is sent straight back out on the tick they get home, which
        // the standing-order test below covers on its own.
        CityWorld live = TestHelpers.WorldWithHome();
        Citizen hero = live.Hero!;
        Assert.Null(hero.CurrentAssignment);

        hero.BeginTravelHome(live.CurrentTick);
        CityWorld offline = WorldPersistence.FromSave(WorldPersistence.Capture(live));

        AdvanceLive(live, CityEconomyRules.AbstractTravelTicks);
        AdvanceOffline(offline, CityEconomyRules.AbstractTravelTicks);

        Assert.Equal(CitizenLocation.AtHome, live.Hero!.CurrentLocation);
        AssertSameWorld(live, offline);
    }

    [Fact]
    public void JourneyAcrossTheDayNightBoundary_LiveAndOfflineAgree()
    {
        CityWorld live = TestHelpers.WorldWithHome();
        Building quarry = TestHelpers.NewBuilding(new BuildingId(8103));
        quarry.DepositIron(100);
        live.RegisterBuilding(quarry);

        // Leave less of the workday than the journey needs, so the trip is in
        // flight when the world crosses into night.
        while (live.CurrentTick < GameClock.WorkdayEndTick - CityEconomyRules.AbstractTravelTicks / 2)
        {
            live.AdvanceWorldTick();
        }
        Assert.True(live.TryAssignCitizen(quarry.Id, live.Hero!.Id).IsSuccess);
        CityWorld offline = WorldPersistence.FromSave(WorldPersistence.Capture(live));

        AdvanceLive(live, CityEconomyRules.AbstractTravelTicks * 3);
        AdvanceOffline(offline, CityEconomyRules.AbstractTravelTicks * 3);

        // The boundary turns the journey around rather than parking anyone at a
        // closed worksite, and the player's standing order survives it.
        Assert.Equal(quarry.Id, live.Hero!.CurrentAssignment);
        Assert.Equal(CitizenLocation.AtHome, live.Hero!.CurrentLocation);
        AssertSameWorld(live, offline);
    }

    [Fact]
    public void SaveLoadMidTransit_ResumesTheSameJourneyAndArrivesOnTime()
    {
        (CityWorld live, Citizen hero, _) = AssignedWorld(8104);
        int arrivesAt = hero.TravelArrivalTick!.Value;
        AdvanceLive(live, CityEconomyRules.AbstractTravelTicks / 3);

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.Capture(live));
        Assert.Equal(CitizenLocation.InTransit, restored.Hero!.CurrentLocation);
        Assert.Equal(hero.TransitStartedAtTick, restored.Hero.TransitStartedAtTick);
        // The journey keeps its original deadline: a save does not restart it,
        // and reloading does not shorten it either.
        Assert.Equal(arrivesAt, restored.Hero.TravelArrivalTick);

        AdvanceLive(live, CityEconomyRules.AbstractTravelTicks);
        AdvanceOffline(restored, CityEconomyRules.AbstractTravelTicks);

        Assert.Equal(CitizenLocation.AtWork, live.Hero!.CurrentLocation);
        AssertSameWorld(live, restored);
    }

    /// <summary>
    /// Speed is a rendering cadence, not a rule. 1x / 2x / 4x change how often
    /// the controller asks for a tick; they cannot change what a tick does, so
    /// the same tick count must produce the same world at every speed.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void SimulationSpeed_DoesNotChangeWhatATickDoes(int speedMultiplier)
    {
        (CityWorld reference, _, _) = AssignedWorld(8105);
        CityWorld atSpeed = WorldPersistence.FromSave(WorldPersistence.Capture(reference));

        int ticks = CityEconomyRules.AbstractTravelTicks * 2;
        AdvanceLive(reference, ticks);
        // Whatever the multiplier, the world advances one tick per tick; the
        // multiplier only decides how much wall-clock time that took.
        for (int batch = 0; batch < ticks / speedMultiplier; batch++)
        {
            AdvanceLive(atSpeed, speedMultiplier);
        }

        Assert.Equal(CitizenLocation.AtWork, atSpeed.Hero!.CurrentLocation);
        AssertSameWorld(reference, atSpeed);
    }

    [Fact]
    public void RecoveryTravel_LiveAndOfflineFeedTheCitizenAtTheSameTick()
    {
        (CityWorld live, Citizen hero, _) = AssignedWorld(8106);
        Building farm = live.Buildings.Values.First(building => building.Kind == BuildingKind.Farm);
        farm.ConfigureProductionPolicy(false, 0, farm.StorageCapacity);
        AdvanceLive(live, CityEconomyRules.AbstractTravelTicks);
        Assert.Equal(CitizenLocation.AtWork, hero.CurrentLocation);

        hero.ConsumeStamina(hero.CurrentStamina - (CitizenNeedsRules.InterruptAtStamina + 2));
        live.DepositFood(1);
        TestHelpers.AdvanceToNextProductionCycle(live);
        Assert.True(hero.IsReturningHome);

        CityWorld offline = WorldPersistence.FromSave(WorldPersistence.Capture(live));
        int foodWhileWalking = live.FoodStock;

        AdvanceLive(live, CityEconomyRules.AbstractTravelTicks);
        AdvanceOffline(offline, CityEconomyRules.AbstractTravelTicks);

        // The meal belongs to the arrival, not to the departure: the walk home
        // costs nothing, and reaching the shelter costs exactly one Food.
        Assert.Equal(CitizenLocation.AtHome, live.Hero!.CurrentLocation);
        Assert.Equal(foodWhileWalking - 1, live.FoodStock);
        AssertSameWorld(live, offline);
    }

    [Fact]
    public void StandingOrder_IsReDispatchedAfterTheReturnJourneyEnds()
    {
        (CityWorld world, Citizen hero, Building quarry) = AssignedWorld(8107);
        AdvanceLive(world, CityEconomyRules.AbstractTravelTicks);
        Assert.Equal(CitizenLocation.AtWork, hero.CurrentLocation);

        hero.BeginTravelHome(world.CurrentTick);
        AdvanceLive(world, CityEconomyRules.AbstractTravelTicks);

        // The order was never cancelled, so arriving home and being sent back
        // out happen on the same tick: the citizen is already walking to the
        // quarry again rather than resting there for one tick first.
        Assert.Equal(quarry.Id, hero.CurrentAssignment);
        Assert.Equal(CitizenLocation.InTransit, hero.CurrentLocation);
        Assert.False(hero.IsReturningHome);
        // The second journey is timed exactly like the first.
        Assert.Equal(
            world.CurrentTick + CityEconomyRules.AbstractTravelTicks,
            hero.TravelArrivalTick);

        AdvanceLive(world, CityEconomyRules.AbstractTravelTicks);
        Assert.Equal(CitizenLocation.AtWork, hero.CurrentLocation);
    }

    /// <summary>
    /// Regression for the commitment whitelist the old offline-only completion
    /// carried. It listed BuildingWork, Construction and Recovery, so a citizen
    /// walking home with no commitment left — exactly what releasing a post
    /// produces — had no rule that could ever end their journey.
    /// </summary>
    [Fact]
    public void CitizenWithNoCommitment_StillCompletesTheirJourneyHome()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Citizen hero = world.Hero!;
        Assert.Equal(CitizenCommitmentKind.None, hero.Commitment.Kind);

        hero.BeginTravelHome(world.CurrentTick);
        AdvanceLive(world, CityEconomyRules.AbstractTravelTicks);

        Assert.Equal(CitizenLocation.AtHome, hero.CurrentLocation);
        Assert.Null(hero.TransitStartedAtTick);
    }

    /// <summary>
    /// The offline seam batches whole quiescent ranges to avoid stepping days
    /// one tick at a time. An arrival is a scheduled state change, so the batch
    /// has to stop at it — otherwise catch-up would jump clean over the journey
    /// and leave the citizen walking forever.
    /// </summary>
    [Fact]
    public void QuiescentBatching_NeverSkipsAnArrivalTick()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Citizen hero = world.Hero!;
        hero.BeginTravelHome(world.CurrentTick);
        int arrivesAt = hero.TravelArrivalTick!.Value;

        // Ask for far more time than the journey needs, through the path that
        // would batch it away if it were allowed to.
        AdvanceOffline(world, CityEconomyRules.AbstractTravelTicks * 10);

        Assert.Equal(CitizenLocation.AtHome, hero.CurrentLocation);
        Assert.True(world.CurrentTick >= arrivesAt);
    }
}
