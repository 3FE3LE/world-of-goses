using WorldofGoses.Domain;
using WorldofGoses.Prototypes;
using WorldofGoses.Ui;
using Xunit;
using System.Collections.Generic;

namespace WorldofGoses.Tests;

public class MacroStreetLiveViewTests
{
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
            Assert.True(world.ConfirmCitizenArrivedHome(hero.Id));
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
