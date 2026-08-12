using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using WorldofGoses.Prototypes;
using WorldofGoses.Ui;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace WorldofGoses.Tests;

public class MacroStreetLiveViewTests
{
    [Fact]
    public void ResourceGainPopup_FollowsMovingCarrierWhileKeepingQuantizedRise()
    {
        Vector2 first = ResourceGainPopup.FollowedPosition(
            new Vector2(100f, 200f),
            Vector2.Up * 72f,
            motionStep: 2);
        Vector2 moved = ResourceGainPopup.FollowedPosition(
            new Vector2(124f, 208f),
            Vector2.Up * 72f,
            motionStep: 2);

        Assert.Equal(new Vector2(24f, 8f), moved - first);
        Assert.Equal(new Vector2(100f, 124f), first);
    }

    [Fact]
    public void Snapshot_ContainsOnlyRealTerritoryParcels_NoPhantomRightColumnCell()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingOpportunities();

        CityMacroSnapshot snapshot = CityMacroSnapshot.From(world);

        Assert.Equal(3, snapshot.Parcels.Count);
        Assert.Equal(new[] { 0, 1, 2 }, snapshot.Parcels
            .OrderBy(item => item.LogicalColumn)
            .Select(item => item.LogicalColumn));
        Assert.All(snapshot.Parcels, item =>
        {
            Assert.Equal(0, item.LogicalRow);
            Assert.Equal(ParcelTerritoryState.Available, item.TerritoryState);
        });
    }

    [Theory]
    [InlineData(15, 16, 48)]
    [InlineData(20, 21, 63)]
    public void LongTerrariumFixture_AddsRequestedRowsAtTheFoundingWidth(
        int additionalRows,
        int expectedRows,
        int expectedParcels)
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        WorldSave save = WorldPersistence.Capture(world);

        CityPrototype.AddTerrariumRowsForVisualRegression(save, additionalRows);

        Assert.Equal(expectedParcels, save.Parcels.Count);
        Assert.Equal(Enumerable.Range(0, expectedRows), save.Parcels
            .Select(parcel => parcel.LogicalRow)
            .Distinct()
            .OrderBy(row => row));
        Assert.All(save.Parcels.GroupBy(parcel => parcel.LogicalRow),
            row => Assert.Equal(3, row.Count()));
        Assert.All(save.Parcels,
            parcel => Assert.Equal(
                ParcelTerritoryState.Available.ToString(),
                parcel.TerritoryState));
    }

    [Fact]
    public void TerrariumWindowFixture_BuildsEightByNinePresentationEnvelope()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        WorldSave save = WorldPersistence.Capture(world);
        save.ParcelPlacements.Add(new ParcelPlacementSave
        {
            EntityId = 99,
            ParcelId = 2,
            LotColumn = 1,
            StartColumn = 2,
        });
        save.CorridorReservations.Add(new CorridorReservationSave
        {
            Id = 99,
            RowId = 0,
            StartColumn = 2,
            FrontageColumns = 1,
        });

        CityPrototype.ResizeTerrariumForVisualRegression(save, rows: 8, columns: 9);

        Assert.Equal(72, save.Parcels.Count);
        Assert.Equal(Enumerable.Range(0, 8), save.Parcels
            .Select(parcel => parcel.LogicalRow)
            .Distinct()
            .OrderBy(row => row));
        Assert.All(save.Parcels.GroupBy(parcel => parcel.LogicalRow),
            row => Assert.Equal(9, row.Count()));
        Assert.Equal(new[] { 3, 4, 5 }, save.Parcels
            .Where(parcel => parcel.Id <= 3)
            .OrderBy(parcel => parcel.Id)
            .Select(parcel => parcel.LogicalColumn));
        Assert.Equal(10, save.ParcelPlacements.Single(item => item.EntityId == 99).LotColumn);
        Assert.Equal(29, save.ParcelPlacements.Single(item => item.EntityId == 99).StartColumn);
        Assert.Equal(29, save.CorridorReservations.Single(item => item.Id == 99).StartColumn);
    }

    [Fact]
    public void GatherRequest_MicroDoubleClickDoesNotRestartPendingRoute()
    {
        Assert.True(MacroStreetLiveView.IsDuplicateGatherRequest((100, 2), 100, 2));
        Assert.False(MacroStreetLiveView.IsDuplicateGatherRequest(null, 100, 2));
        Assert.False(MacroStreetLiveView.IsDuplicateGatherRequest((100, 1), 100, 2));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public void WheelZoom_IsReservedForScrollableUiUnderThePointer(
        bool isWheelEvent,
        bool pointerIsOverScrollableUi,
        bool expected)
    {
        Assert.Equal(
            expected,
            UiInputBoundary.ShouldWorldCameraHandleWheel(
                isWheelEvent,
                pointerIsOverScrollableUi));
    }

    [Fact]
    public void ActiveReturnRoute_IsNotRestartedOnEverySnapshot()
    {
        Assert.True(MacroStreetLiveView.ShouldBeginReturnHomeRoute(
            CitizenLocation.AtWork,
            hasRoute: false,
            pendingReturnHome: false));

        Assert.False(MacroStreetLiveView.ShouldBeginReturnHomeRoute(
            CitizenLocation.InTransit,
            hasRoute: true,
            pendingReturnHome: true));

        Assert.True(MacroStreetLiveView.ShouldBeginReturnHomeRoute(
            CitizenLocation.InTransit,
            hasRoute: false,
            pendingReturnHome: false));
    }

    [Fact]
    public void UnassignedCitizenAtHome_IsHiddenOnlyAfterShelterArrivalSettles()
    {
        Assert.True(MacroStreetLiveView.ShouldHideHeroInsideShelter(
            currentAssignment: null,
            location: CitizenLocation.AtHome,
            hasShelter: true,
            hasRoute: false,
            pendingReturnHome: false));

        Assert.False(MacroStreetLiveView.ShouldHideHeroInsideShelter(
            currentAssignment: null,
            location: CitizenLocation.InTransit,
            hasShelter: true,
            hasRoute: true,
            pendingReturnHome: true));

        Assert.False(MacroStreetLiveView.ShouldHideHeroInsideShelter(
            currentAssignment: null,
            location: CitizenLocation.AtHome,
            hasShelter: false,
            hasRoute: false,
            pendingReturnHome: false));
    }

    [Fact]
    public void FounderOnGatherRoute_IsNeverHiddenByTheShelterAutoHide()
    {
        // Gathering is the one founder route that does not change domain
        // location, so its explicit latch keeps the carrier visible.
        Assert.False(MacroStreetLiveView.ShouldHideHeroInsideShelter(
            currentAssignment: null,
            location: CitizenLocation.AtHome,
            hasShelter: true,
            hasRoute: false,
            pendingReturnHome: false,
            isGatheringOutsideHome: true));

        Assert.True(MacroStreetLiveView.ShouldHideHeroInsideShelter(
            currentAssignment: null,
            location: CitizenLocation.AtHome,
            hasShelter: true,
            hasRoute: false,
            pendingReturnHome: false,
            isGatheringOutsideHome: false));
    }

    [Fact]
    public void TravelDestination_AssignedCitizenRoutesToWorkplace()
    {
        var workplace = new BuildingId(7);

        BuildingId? plot = MacroStreetLiveView.ResolveTravelDestination(
            location: CitizenLocation.InTransit,
            isReturningHome: false,
            currentAssignment: workplace,
            homeBuildingId: new BuildingId(1));

        Assert.Equal(workplace, plot);
    }

    [Fact]
    public void TravelDestination_ReturningCitizenRoutesToShelter()
    {
        var home = new BuildingId(1);

        BuildingId? plot = MacroStreetLiveView.ResolveTravelDestination(
            location: CitizenLocation.InTransit,
            isReturningHome: true,
            currentAssignment: new BuildingId(7),
            homeBuildingId: home);

        Assert.Equal(home, plot);
    }

    [Fact]
    public void TravelDestination_CitizenNotInTransitHasNoRoute()
    {
        BuildingId? plot = MacroStreetLiveView.ResolveTravelDestination(
            location: CitizenLocation.AtHome,
            isReturningHome: false,
            currentAssignment: null,
            homeBuildingId: null);

        Assert.Null(plot);
    }

    [Fact]
    public void TravelDestination_CitizenAtWorkHasNoRoute()
    {
        BuildingId? plot = MacroStreetLiveView.ResolveTravelDestination(
            location: CitizenLocation.AtWork,
            isReturningHome: false,
            currentAssignment: new BuildingId(7),
            homeBuildingId: new BuildingId(1));

        Assert.Null(plot);
    }

    [Fact]
    public void CameraStartsFreeAndRequiresExplicitFollowToggle()
    {
        Assert.False(MacroStreetLiveView.FollowsFounderByDefault);
    }

    [Fact]
    public void MinimumZoom_FramesThirteenStreetWindowWithoutChangingProjection()
    {
        float zoom = MacroStreetLiveView.MinimumZoomForTests;
        float pivotY = MacroStreetLiveView.CameraZoomPivotYForTests;
        float nearY = pivotY + zoom
            * (StreetDepthProjection.RowScreenY(-2f, 580f) - pivotY);
        float farY = pivotY + zoom
            * (StreetDepthProjection.RowScreenY(11f, 580f) - pivotY);

        Assert.InRange(nearY, 704f, 716f);
        Assert.InRange(farY, 72f, 88f);
    }

    [Fact]
    public void MaximumZoom_AllowsACloserViewThanThePreviousLimit()
    {
        Assert.Equal(3.0f, MacroStreetLiveView.MaximumZoomForTests);
        Assert.True(MacroStreetLiveView.MaximumZoomForTests > 1.75f);
    }

    [Fact]
    public void HeldVerticalPan_AcceleratesGraduallyAndRemainsBounded()
    {
        float initialRepeat = MacroStreetLiveView.VerticalPanRepeatSeconds(0f);
        float middleRepeat = MacroStreetLiveView.VerticalPanRepeatSeconds(1.5f);
        float finalRepeat = MacroStreetLiveView.VerticalPanRepeatSeconds(3f);
        float afterCurveRepeat = MacroStreetLiveView.VerticalPanRepeatSeconds(30f);

        Assert.Equal(0.48f, initialRepeat, precision: 3);
        Assert.InRange(middleRepeat, finalRepeat, initialRepeat);
        Assert.Equal(0.26f, finalRepeat, precision: 3);
        Assert.Equal(finalRepeat, afterCurveRepeat);

        float initialSpeed = MacroStreetLiveView.VerticalPanTransitionMultiplier(0f);
        float middleSpeed = MacroStreetLiveView.VerticalPanTransitionMultiplier(1.5f);
        float finalSpeed = MacroStreetLiveView.VerticalPanTransitionMultiplier(3f);

        Assert.Equal(1f, initialSpeed);
        Assert.InRange(middleSpeed, initialSpeed, finalSpeed);
        Assert.Equal(1.55f, finalSpeed, precision: 3);
    }

    [Fact]
    public void StandingOrderAtHome_DoesNotCreateAVisualWorkRoute()
    {
        Assert.False(MacroStreetLiveView.ShouldBeginWorkRoute(
            new BuildingId(7),
            CitizenLocation.AtHome,
            isReturningHome: false,
            hasRoute: false));

        Assert.True(MacroStreetLiveView.ShouldBeginWorkRoute(
            new BuildingId(7),
            CitizenLocation.InTransit,
            isReturningHome: false,
            hasRoute: false));
    }

    [Fact]
    public void RestoredTransit_ReconstructsElapsedRouteInsteadOfRestartingAtOrigin()
    {
        var route = new List<StreetRoutePlanner.Waypoint>
        {
            new(0, 32f),
            new(2, 32f),
        };

        MacroStreetLiveView.ReconstructedRoutePosition position =
            MacroStreetLiveView.ReconstructRouteProgress(
                route,
                startStreet: 0,
                startLateral: 0f,
                elapsedTicks: 15,
                expectedDurationTicks: 30);

        Assert.True(position.Lateral > 0f || position.Street > 0);
        Assert.False(position.Street == 0 && position.Lateral == 0f);
    }

    /// <summary>
    /// The pacing rule that replaced "one step per render frame" (DEC-0023).
    /// A route is spread across the domain's own journey window, which is what
    /// makes the drawn arrival land on the tick the domain already chose.
    /// </summary>
    [Fact]
    public void PacedRoute_SpendsItsStepsAcrossTheJourneyWindow()
    {
        const int totalSteps = 40;
        const int duration = 30;

        // Nothing walked before the journey starts, and the route is never
        // finished early — that was the failure mode of letting the render
        // cadence decide, on a route short enough to run out before the tick.
        Assert.Equal(0, MacroStreetLiveView.PacedRouteSteps(totalSteps, 0, 0d, duration));
        Assert.True(MacroStreetLiveView.PacedRouteSteps(totalSteps, duration - 1, 0d, duration) < totalSteps);

        // Halfway through the window, halfway along the route.
        Assert.Equal(
            totalSteps / 2,
            MacroStreetLiveView.PacedRouteSteps(totalSteps, duration / 2, 0d, duration));

        // The last step is spent exactly when the domain completes the journey,
        // and no later: overshooting the window cannot walk past the end.
        Assert.Equal(totalSteps, MacroStreetLiveView.PacedRouteSteps(totalSteps, duration, 0d, duration));
        Assert.Equal(totalSteps, MacroStreetLiveView.PacedRouteSteps(totalSteps, duration * 5, 0d, duration));
    }

    /// <summary>
    /// The sub-tick phase only smooths motion between one-second world ticks;
    /// it can never move the citizen beyond the next whole tick's position.
    /// </summary>
    [Fact]
    public void PacedRoute_TickPhaseOnlySmoothsWithinOneTick()
    {
        const int totalSteps = 60;
        const int duration = 30;

        int atTick = MacroStreetLiveView.PacedRouteSteps(totalSteps, 10, 0d, duration);
        int midTick = MacroStreetLiveView.PacedRouteSteps(totalSteps, 10, 0.5d, duration);
        int nextTick = MacroStreetLiveView.PacedRouteSteps(totalSteps, 11, 0d, duration);

        Assert.True(midTick >= atTick);
        Assert.True(midTick <= nextTick);
        // An out-of-range phase cannot be used to run ahead of the domain.
        Assert.Equal(nextTick, MacroStreetLiveView.PacedRouteSteps(totalSteps, 10, 99d, duration));
    }

    /// <summary>
    /// A journey reversed at the workday boundary used to reach the view as a
    /// refused arrival. With arrivals no longer the view's to claim, the route
    /// pointing the wrong way is the only signal left that it must be re-planned.
    /// </summary>
    [Fact]
    public void RouteContradictsDomain_DetectsAJourneyReversedUnderneathIt()
    {
        // Domain turned the citizen around; the drawn route still aims at work.
        Assert.True(MacroStreetLiveView.RouteContradictsDomain(
            isReturningHome: true, routeTargetsAssignment: true, routeTargetsHome: false));
        // Domain sent them back out; the drawn route still aims home.
        Assert.True(MacroStreetLiveView.RouteContradictsDomain(
            isReturningHome: false, routeTargetsAssignment: false, routeTargetsHome: true));

        // Agreement in both directions must not throw away a valid route.
        Assert.False(MacroStreetLiveView.RouteContradictsDomain(
            isReturningHome: true, routeTargetsAssignment: false, routeTargetsHome: true));
        Assert.False(MacroStreetLiveView.RouteContradictsDomain(
            isReturningHome: false, routeTargetsAssignment: true, routeTargetsHome: false));
    }

    [Fact]
    public void FreshTransit_StillStartsAtSemanticOrigin()
    {
        var route = new List<StreetRoutePlanner.Waypoint> { new(1, 24f) };

        MacroStreetLiveView.ReconstructedRoutePosition position =
            MacroStreetLiveView.ReconstructRouteProgress(route, 0, 0f, 0, 30);

        Assert.Equal(0, position.Street);
        Assert.Equal(0f, position.Lateral);
        Assert.Equal(0, position.RouteIndex);
    }

    [Fact]
    public void BuildingAnchors_AreDerivedFromCurrentPlacementAndStayBounded()
    {
        BuildingVisualAnchors anchors = BuildingVisualAnchors.FromPlacement(
            frontStreet: 3,
            lateral: 96f,
            streetCount: 5,
            lateralHalfWidth: 100f,
            stepPixels: 8f);

        Assert.Equal(new StreetVisualAnchor(3, 96f), anchors.Entrance);
        Assert.Equal(100f, anchors.Waiting.Lateral);
        Assert.InRange(anchors.LeisureLeft.Lateral, -100f, 100f);
        Assert.InRange(anchors.LeisureRight.Lateral, -100f, 100f);
    }

    [Theory]
    [InlineData(CitizenRoutineActivity.Leisure, true)]
    [InlineData(CitizenRoutineActivity.WaitingForStorage, true)]
    [InlineData(CitizenRoutineActivity.Working, false)]
    [InlineData(CitizenRoutineActivity.Resting, false)]
    public void OnlyInterruptibleIdleActivitiesUseAmbientWandering(
        CitizenRoutineActivity activity,
        bool expected)
    {
        Assert.Equal(expected, MacroStreetLiveView.CanWander(activity));
    }

    [Theory]
    [InlineData(CitizenLocation.AtHome, CitizenRoutineActivity.Resting, true)]
    [InlineData(CitizenLocation.AtHome, CitizenRoutineActivity.OffDuty, true)]
    [InlineData(CitizenLocation.AtHome, CitizenRoutineActivity.Leisure, false)]
    [InlineData(CitizenLocation.AtHome, CitizenRoutineActivity.Recovering, false)]
    [InlineData(CitizenLocation.AtHome, CitizenRoutineActivity.WaitingForStorage, false)]
    [InlineData(CitizenLocation.AtWork, CitizenRoutineActivity.Resting, false)]
    [InlineData(CitizenLocation.InTransit, CitizenRoutineActivity.Resting, false)]
    public void NonFounderCitizenAtHome_IsHiddenUnlessWanderingOrRecovering(
        CitizenLocation location,
        CitizenRoutineActivity activity,
        bool expected)
    {
        // Resting/OffDuty at home would otherwise be parked at the
        // home's entrance anchor (anchors.Entrance), so closing the
        // shelter detail view used to leave every sleeping citizen
        // visibly outside the building.
        Assert.Equal(
            expected,
            MacroStreetLiveView.ShouldHideCitizenAtHome(location, activity));
    }

    [Fact]
    public void CitizenSelectionDetail_IdleCitizen_ReportsActivityOnly()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        CityMacroSnapshot.CitizenItem citizen = Assert.Single(CityMacroSnapshot.From(world).Citizens);

        var lines = MacroStreetLiveView.BuildCitizenSelectionKeys(citizen);

        // Single line for an uninjured citizen resting at home; never a
        // wound/expedition/no-food line.
        MacroStreetLiveView.SelectionLine only = Assert.Single(lines);
        Assert.DoesNotContain("wound", only.TextKey);
        Assert.DoesNotContain("expedition", only.TextKey);
        Assert.DoesNotContain("no_food", only.TextKey);
    }

    [Fact]
    public void CitizenSelectionDetail_WoundedCitizenInTreatment_ListsWoundAndTreatment()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Citizen hero = world.Hero!;
        WorldEvent origin = world.Log.Record(
            world.CurrentTick,
            WorldEventKind.WoundSustained,
            WorldEventSubject.Citizen(hero.Id, hero.Name),
            (int)WoundSeverity.Moderate);
        hero.SustainWound(WoundSeverity.Moderate, origin.Id);
        world.DepositFood(WoundRules.ModerateFoodCost);
        Assert.True(world.TryBeginWoundRecovery(hero.Id).IsSuccess);

        CityMacroSnapshot.CitizenItem treating = Assert.Single(CityMacroSnapshot.From(world).Citizens);

        var lines = MacroStreetLiveView.BuildCitizenSelectionKeys(treating);

        // The wound line is always present; the treatment line is appended
        // only while the recovery countdown is being paid out.
        Assert.Contains(lines, line => line.TextKey == "ui.world_status.wound");
        Assert.Contains(lines, line => line.TextKey == "ui.world_status.treatment");
    }

    [Fact]
    public void CitizenSelectionDetail_ExpeditionCitizen_ReportsExpeditionLine()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        CityMacroSnapshot.CitizenItem idle = Assert.Single(CityMacroSnapshot.From(world).Citizens);
        // Project the snapshot into an on-expedition citizen. Going through a
        // full StartExpedition requires a free hero and stockpile; the BODY
        // logic under test only reads the resulting IsOnExpedition flag, so
        // projecting the snapshot is the most local fixture.
        CityMacroSnapshot.CitizenItem traveller = idle with { IsOnExpedition = true };

        var lines = MacroStreetLiveView.BuildCitizenSelectionKeys(traveller);

        MacroStreetLiveView.SelectionLine only = Assert.Single(lines);
        Assert.Equal("ui.world_status.expedition", only.TextKey);
        Assert.Equal(IconPaths.Shield, only.IconPath);
    }

    [Fact]
    public void CitizenSelectionDetail_FoodBlockedRecovery_ReportsNoFoodOverActivity()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Citizen hero = world.Hero!;
        if (hero.CurrentLocation == CitizenLocation.InTransit && hero.IsReturningHome)
        {
            TestHelpers.SettleTravel(world);
        }
        hero.MarkFoodBlocked();

        CityMacroSnapshot.CitizenItem blocked = Assert.Single(CityMacroSnapshot.From(world).Citizens);

        var lines = MacroStreetLiveView.BuildCitizenSelectionKeys(blocked);

        // The "no food" line is the causal explanation; the activity line is
        // suppressed so the player sees the WHY first instead of the routine.
        MacroStreetLiveView.SelectionLine only = Assert.Single(lines);
        Assert.Equal("ui.world_status.no_food", only.TextKey);
    }
}
