using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Every in-city journey used to take thirty ticks whatever the distance, so
/// the same citizen looked slow walking somewhere near and fast walking
/// somewhere far. Duration was the constant and distance was the variable.
/// </summary>
public sealed class CityTravelTests
{
    private static ParcelPlacement At(int entityId, int row, int startColumn) =>
        new(
            new BuildingId(entityId),
            new ParcelId(1),
            new ConstructionRowId(row),
            startColumn,
            BuildingReservation.MinimumFrontageColumns,
            BuildingReservation.RequiredDepthRows,
            BuildingReservation.MinimumFrontageColumns,
            leftExpansionColumns: 0,
            rightExpansionColumns: 0,
            lotColumn: 0,
            lotRow: 0,
            lotWidth: 1,
            lotHeight: 1,
            footprintProfileId: "standard-side-setbacks",
            orientation: BuildingOrientation.South);

    /// <summary>The whole point of the change, stated as one assertion.</summary>
    [Fact]
    public void FartherIsLonger()
    {
        ParcelPlacement home = At(1, row: 0, startColumn: 0);

        int near = CityTravel.TravelTicks(home, At(2, row: 0, startColumn: 3));
        int far = CityTravel.TravelTicks(home, At(3, row: 0, startColumn: 24));

        Assert.True(far > near, $"near {near}, far {far}");
    }

    /// <summary>
    /// A street change costs more than a step along a frontage: it is a depth
    /// move across a carriageway. Held at the same column so the row is the
    /// only difference — comparing six columns against two rows would compare
    /// two quantities and tell us nothing about either.
    /// </summary>
    [Fact]
    public void CrossingAStreetCostsMoreThanTheSameStepAlongOne()
    {
        ParcelPlacement home = At(1, row: 0, startColumn: 0);

        int oneColumnOver = CityTravel.TravelTicks(home, At(2, row: 0, startColumn: 1));
        int oneStreetOver = CityTravel.TravelTicks(home, At(3, row: 1, startColumn: 0));

        Assert.True(
            oneStreetOver > oneColumnOver,
            $"one column {oneColumnOver}, one street {oneStreetOver}");
    }

    [Fact]
    public void TheSameDistanceCostsTheSameWhicheverWayItIsWalked()
    {
        // The symptom that opened the issue: two journeys of equal length must
        // not differ because one of them happens to be a gathering trip and the
        // other a walk to a worksite. Nothing here knows what the trip is for.
        ParcelPlacement a = At(1, row: 1, startColumn: 3);
        ParcelPlacement b = At(2, row: 1, startColumn: 12);

        Assert.Equal(CityTravel.TravelTicks(a, b), CityTravel.TravelTicks(b, a));
    }

    [Fact]
    public void AFasterCitizenArrivesSooner()
    {
        ParcelPlacement home = At(1, row: 0, startColumn: 0);
        ParcelPlacement work = At(2, row: 1, startColumn: 18);

        int brisk = CityTravel.TravelTicks(home, work, movementSpeed: 1.3);
        int ordinary = CityTravel.TravelTicks(home, work, movementSpeed: 1.0);
        int slow = CityTravel.TravelTicks(home, work, movementSpeed: 0.8);

        Assert.True(brisk < ordinary, $"brisk {brisk}, ordinary {ordinary}");
        Assert.True(ordinary < slow, $"ordinary {ordinary}, slow {slow}");
    }

    [Fact]
    public void NoJourneyIsInstantAndNoneIsEndless()
    {
        ParcelPlacement here = At(1, row: 0, startColumn: 0);

        Assert.Equal(CityTravel.MinimumTravelTicks, CityTravel.TravelTicks(here, here));
        Assert.InRange(
            CityTravel.TravelTicks(here, At(2, row: 99, startColumn: 9999)),
            CityTravel.MinimumTravelTicks,
            CityTravel.MaximumTravelTicks);
        // A speed of zero or nonsense must not divide the world by zero.
        Assert.InRange(
            CityTravel.TravelTicks(here, At(3, row: 1, startColumn: 9), movementSpeed: 0),
            CityTravel.MinimumTravelTicks,
            CityTravel.MaximumTravelTicks);
    }

    /// <summary>
    /// An endpoint the world cannot place keeps the old flat duration.
    /// </summary>
    /// <remarks>
    /// Measuring nothing and calling it "next door" would make those journeys
    /// nearly instant, which is a bigger lie than the constant was.
    /// </remarks>
    [Fact]
    public void AnUnplacedEndpointFallsBackToTheOldConstant()
    {
        ParcelPlacement placed = At(1, row: 0, startColumn: 0);

        Assert.Equal(
            CityEconomyRules.AbstractTravelTicks,
            CityTravel.TravelTicks(placed, null));
        Assert.Equal(
            CityEconomyRules.AbstractTravelTicks,
            CityTravel.TravelTicks(null, placed));
    }

    /// <summary>
    /// The calibration claim, checked rather than asserted in a comment: a
    /// typical trip still lands near the thirty ticks every trip used to cost,
    /// so this changes the shape of the economy and not its scale.
    /// </summary>
    [Fact]
    public void ATypicalTripStaysNearTheDurationItReplaced()
    {
        int typical = CityTravel.TravelTicks(
            At(1, row: 0, startColumn: 0),
            At(2, row: 1, startColumn: 6));

        Assert.InRange(
            typical,
            CityEconomyRules.AbstractTravelTicks / 2,
            CityEconomyRules.AbstractTravelTicks * 2);
    }

    // ---- Through the world -------------------------------------------------

    /// <summary>
    /// The wire: a real citizen in a real world gets a distance-derived
    /// duration, not the constant. Every test above measures the function in
    /// isolation and would stay green if nothing ever called it.
    /// </summary>
    [Fact]
    public void AWorldJourneyIsMeasuredAndNotAssumed()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Citizen hero = world.Hero!;
        Building quarry = TestHelpers.NewBuilding(new BuildingId(61));
        world.RegisterBuilding(quarry);
        Assert.True(world.TryAssignCitizen(quarry.Id, hero.Id).IsSuccess);
        Assert.Equal(CitizenLocation.InTransit, hero.CurrentLocation);

        // The two endpoints the world actually placed, measured directly.
        ParcelPlacement home = world.ParcelPlacements[world.PrimaryHome!.Id];
        ParcelPlacement work = world.ParcelPlacements[quarry.Id];
        double impliedSpeed =
            CityTravel.Distance(home, work) / hero.TransitDurationTicks;

        // The duration is the distance divided by a plausible walking pace, not
        // a constant that happens to be in range. MovementSpeed is capped to
        // [0.8, 1.3] by the statistics balance, so the implied speed has to sit
        // there — and a flat thirty would not, for this distance.
        Assert.InRange(impliedSpeed, 0.8, 1.3);
        Assert.Equal(
            hero.TransitStartedAtTick!.Value + hero.TransitDurationTicks,
            hero.TravelArrivalTick);
    }

    /// <summary>
    /// Two destinations at different distances give the same citizen different
    /// journeys. This is the user-visible symptom, checked through the world.
    /// </summary>
    [Fact]
    public void TwoDestinationsAtDifferentDistancesTakeDifferentTimes()
    {
        // Register a spread of workplaces and let the world place them where it
        // likes; which id lands where is the placer's business, not this test's.
        CityWorld world = TestHelpers.WorldWithHome();
        for (int id = 71; id <= 84; id++)
        {
            world.RegisterBuilding(TestHelpers.NewBuilding(new BuildingId(id)));
        }

        ParcelPlacement home = world.ParcelPlacements[world.PrimaryHome!.Id];
        var byDistance = Enumerable.Range(71, 14)
            .Select(id => new BuildingId(id))
            .Where(world.ParcelPlacements.ContainsKey)
            .OrderBy(id => CityTravel.Distance(home, world.ParcelPlacements[id]))
            .ToList();

        BuildingId nearest = byDistance.First();
        BuildingId farthest = byDistance.Last();
        Assert.True(
            CityTravel.Distance(home, world.ParcelPlacements[farthest])
            > CityTravel.Distance(home, world.ParcelPlacements[nearest]),
            "The fixture must place at least two workplaces at different distances.");

        Assert.True(
            JourneyTicksTo(farthest) > JourneyTicksTo(nearest),
            "The farther workplace must take longer to reach.");
    }

    private static int JourneyTicksTo(BuildingId destination)
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Citizen hero = world.Hero!;
        for (int id = 71; id <= 84; id++)
        {
            world.RegisterBuilding(TestHelpers.NewBuilding(new BuildingId(id)));
        }
        Assert.True(world.TryAssignCitizen(destination, hero.Id).IsSuccess);
        Assert.Equal(CitizenLocation.InTransit, hero.CurrentLocation);
        return hero.TransitDurationTicks;
    }

    /// <summary>
    /// A journey survives a save reloaded halfway with the same deadline. The
    /// duration is recomputed rather than stored, so this is what proves the
    /// recomputation happens at all.
    /// </summary>
    [Fact]
    public void AJourneyKeepsItsDeadlineAcrossASave()
    {
        CityWorld live = TestHelpers.WorldWithHome();
        Citizen hero = live.Hero!;
        Building quarry = TestHelpers.NewBuilding(new BuildingId(62));
        live.RegisterBuilding(quarry);
        Assert.True(live.TryAssignCitizen(quarry.Id, hero.Id).IsSuccess);

        int arrivesAt = hero.TravelArrivalTick!.Value;
        for (int tick = 0; tick < 2; tick++) live.AdvanceWorldTick();

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.Capture(live));

        Assert.Equal(CitizenLocation.InTransit, restored.Hero!.CurrentLocation);
        Assert.Equal(arrivesAt, restored.Hero.TravelArrivalTick);
        Assert.Equal(hero.TransitDurationTicks, restored.Hero.TransitDurationTicks);
    }
}
