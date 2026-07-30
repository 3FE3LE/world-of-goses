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
}
