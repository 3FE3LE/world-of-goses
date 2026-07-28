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
}
