using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class MacroCitizenActivityTests
{
    [Fact]
    public void PlanCardinalRoute_AvoidsOccupiedBuildingFootprint()
    {
        Vector2 start = new(100, 300);
        Vector2 target = new(100, 50);
        var obstacle = new Rect2(50, 150, 100, 100);

        IReadOnlyList<Vector2> route =
            MacroCitizenActivity.PlanCardinalRoute(start, target, new[] { obstacle });

        Assert.Equal(target, route[^1]);
        Assert.Contains(route, waypoint => waypoint.X < obstacle.Position.X);
        AssertCardinalAndOutside(start, route, obstacle);
    }

    [Fact]
    public void PlanCardinalRoute_UsesIntegerWaypoints()
    {
        Vector2 start = new(10.4f, 20.6f);
        Vector2 target = new(95.7f, 44.2f);

        IReadOnlyList<Vector2> route =
            MacroCitizenActivity.PlanCardinalRoute(start, target, Array.Empty<Rect2>());

        Assert.All(route, waypoint =>
        {
            Assert.Equal(Mathf.Round(waypoint.X), waypoint.X);
            Assert.Equal(Mathf.Round(waypoint.Y), waypoint.Y);
        });
    }

    [Fact]
    public void PixelMotion_StepsEightWholePixelsOnOneAxis()
    {
        Vector2 next = PixelMotion.StepCardinal(
            new Vector2(10.4f, 20.6f),
            new Vector2(40.2f, 50.8f));

        Assert.Equal(new Vector2(18, 21), next);
    }

    [Fact]
    public void ActiveTravel_IsPreservedAcrossMacroRefresh()
    {
        Assert.False(MacroCitizenActivity.ShouldRebuildForRefresh(isTravelling: true));
        Assert.True(MacroCitizenActivity.ShouldRebuildForRefresh(isTravelling: false));
    }

    [Fact]
    public void CanonicalCarrier_IsCompactOnlyInMacroState()
    {
        Assert.Equal(
            new Vector2(0.25f, 0.25f),
            CitizenSpriteCarrier.ScaleForState(
                CitizenSpriteCarrier.VisualState.Macro));
        Assert.Equal(
            Vector2.One,
            CitizenSpriteCarrier.ScaleForState(
                CitizenSpriteCarrier.VisualState.Working));
        Assert.Equal(
            Vector2.One,
            CitizenSpriteCarrier.ScaleForState(
                CitizenSpriteCarrier.VisualState.HeroProfile));
    }

    private static void AssertCardinalAndOutside(
        Vector2 start,
        IReadOnlyList<Vector2> route,
        Rect2 obstacle)
    {
        Vector2 previous = start;
        foreach (Vector2 waypoint in route)
        {
            Assert.True(
                Mathf.IsEqualApprox(previous.X, waypoint.X)
                || Mathf.IsEqualApprox(previous.Y, waypoint.Y));
            Vector2 midpoint = (previous + waypoint) * 0.5f;
            Assert.False(obstacle.HasPoint(midpoint));
            previous = waypoint;
        }
    }
}
