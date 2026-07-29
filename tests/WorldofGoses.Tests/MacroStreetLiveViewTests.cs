using WorldofGoses.Domain;
using WorldofGoses.Prototypes;
using Xunit;

namespace WorldofGoses.Tests;

public class MacroStreetLiveViewTests
{
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
    public void ManuallyWanderingHero_IsNeverHiddenByTheShelterAutoHide()
    {
        // Domain CurrentLocation never leaves AtHome for a manual W/S/arrow
        // wander (no domain travel action is involved), so without the
        // wander latch this would otherwise re-hide the founder on the very
        // next world tick even while they are standing outside.
        Assert.False(MacroStreetLiveView.ShouldHideHeroInsideShelter(
            currentAssignment: null,
            location: CitizenLocation.AtHome,
            hasShelter: true,
            hasRoute: false,
            pendingReturnHome: false,
            hasWanderedManually: true));

        Assert.True(MacroStreetLiveView.ShouldHideHeroInsideShelter(
            currentAssignment: null,
            location: CitizenLocation.AtHome,
            hasShelter: true,
            hasRoute: false,
            pendingReturnHome: false,
            hasWanderedManually: false));
    }

    [Fact]
    public void ResolveAmbientPlotKey_AssignedCitizenStandsAtWorkplace()
    {
        var workplace = new BuildingId(7);

        BuildingId? plot = MacroStreetLiveView.ResolveAmbientPlotKey(
            isHero: false,
            isOnExpedition: false,
            location: CitizenLocation.InTransit,
            currentAssignment: workplace,
            homeBuildingId: new BuildingId(1));

        Assert.Equal(workplace, plot);
    }

    [Fact]
    public void ResolveAmbientPlotKey_IdleCitizenAtHomeStandsAtShelter()
    {
        var home = new BuildingId(1);

        BuildingId? plot = MacroStreetLiveView.ResolveAmbientPlotKey(
            isHero: false,
            isOnExpedition: false,
            location: CitizenLocation.AtHome,
            currentAssignment: null,
            homeBuildingId: home);

        Assert.Equal(home, plot);
    }

    [Fact]
    public void ResolveAmbientPlotKey_IdleCitizenWithNoShelterYetIsInvisible()
    {
        BuildingId? plot = MacroStreetLiveView.ResolveAmbientPlotKey(
            isHero: false,
            isOnExpedition: false,
            location: CitizenLocation.AtHome,
            currentAssignment: null,
            homeBuildingId: null);

        Assert.Null(plot);
    }

    [Fact]
    public void ResolveAmbientPlotKey_HeroNeverUsesTheAmbientWorkerPath()
    {
        BuildingId? plot = MacroStreetLiveView.ResolveAmbientPlotKey(
            isHero: true,
            isOnExpedition: false,
            location: CitizenLocation.AtHome,
            currentAssignment: null,
            homeBuildingId: new BuildingId(1));

        Assert.Null(plot);
    }

    [Fact]
    public void ResolveAmbientPlotKey_ExpeditionMemberIsInvisible()
    {
        BuildingId? plot = MacroStreetLiveView.ResolveAmbientPlotKey(
            isHero: false,
            isOnExpedition: true,
            location: CitizenLocation.AtHome,
            currentAssignment: null,
            homeBuildingId: new BuildingId(1));

        Assert.Null(plot);
    }

    [Fact]
    public void ResolveAmbientPlotKey_CitizenPhysicallyAtWorkUsesTheBuildingsOwnSlotsInstead()
    {
        BuildingId? plot = MacroStreetLiveView.ResolveAmbientPlotKey(
            isHero: false,
            isOnExpedition: false,
            location: CitizenLocation.AtWork,
            currentAssignment: new BuildingId(7),
            homeBuildingId: new BuildingId(1));

        Assert.Null(plot);
    }
}
