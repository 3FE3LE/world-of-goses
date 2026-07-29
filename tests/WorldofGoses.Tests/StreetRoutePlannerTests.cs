using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Prototypes;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Pure coverage for the pseudo-3D macro view's street routing (H-32):
/// roads are the free front band of each lot-row and crossings are only
/// viable through the gaps constructions leave in the band between two
/// roads — the hero should thread BETWEEN obstacles, never through them,
/// matching how manual W/S exploration always respected visible gaps.
/// </summary>
public class StreetRoutePlannerTests
{
    private const float Min = -540f;
    private const float Max = 540f;
    private const float Clearance = 14f;
    // Matches MacroStreetLiveView.CrossingScanStepPx: fine enough to land
    // inside a narrow gap between two adjacent obstacles.
    private const float ScanStep = 6f;

    private static readonly IReadOnlyList<StreetRoutePlanner.Interval> Empty =
        Array.Empty<StreetRoutePlanner.Interval>();

    private static Func<int, IReadOnlyList<StreetRoutePlanner.Interval>> Bands(
        Dictionary<int, IReadOnlyList<StreetRoutePlanner.Interval>> bands) =>
        band => bands.TryGetValue(band, out var intervals) ? intervals : Empty;

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    public void WorkplaceEntranceStreet_IsThePlotFrontBand(int buildingStreet, int expected)
    {
        Assert.Equal(expected, MacroStreetLiveView.WorkplaceEntranceStreet(buildingStreet));
    }

    [Fact]
    public void SameStreet_PlansSingleLateralWaypoint()
    {
        List<StreetRoutePlanner.Waypoint> route = StreetRoutePlanner.Plan(
            2, 0f, 2, 180f, _ => Empty, Min, Max, Clearance, ScanStep);

        StreetRoutePlanner.Waypoint waypoint = Assert.Single(route);
        Assert.Equal(2, waypoint.Street);
        Assert.Equal(180f, waypoint.Lateral);
    }

    [Fact]
    public void OpenBands_PlanStepsOneStreetAtATime()
    {
        List<StreetRoutePlanner.Waypoint> route = StreetRoutePlanner.Plan(
            0, 90f, 2, 90f, _ => Empty, Min, Max, Clearance, ScanStep);

        Assert.Equal(new[] { 1, 2 }, route.ConvertAll(w => w.Street).ToArray());
        Assert.All(route, w => Assert.Equal(90f, w.Lateral));
    }

    /// <summary>
    /// Regression for the real bug behind "walks around the whole row
    /// instead of between two adjacent trees": with a dense row of trees
    /// 90 px apart (LotUnitPx) each blocking a 44 px span
    /// (TreeBlockHalfWidthPx = 22), the viable gap between any two
    /// adjacent trees is only ~18 px wide (e.g. [36,54] between trees at
    /// 0 and 90). A coarse scan step divides evenly into that 90 px
    /// spacing (30 px, the original step, is exactly 90/3) and so lands
    /// on the SAME relative offset for every tree in the row — if it
    /// misses the gap next to one tree, it misses it next to all of
    /// them, forcing the search past the entire row before finding open
    /// space beyond its far end (here, that would be lateral 240 — over
    /// 4x farther than the real gap at ~36-54). The fine
    /// <see cref="ScanStep"/> must land inside the near gap instead.
    /// </summary>
    [Fact]
    public void NarrowGapBetweenAdjacentTreesInADenseRow_IsFound_NotSkippedOver()
    {
        var intervals = new List<StreetRoutePlanner.Interval>();
        foreach (float center in new[] { -180f, -90f, 0f, 90f, 180f })
        {
            intervals.Add(new StreetRoutePlanner.Interval(center - 22f, center + 22f));
        }
        var bands = new Dictionary<int, IReadOnlyList<StreetRoutePlanner.Interval>>
        {
            [0] = intervals,
        };

        // Preferred (destination) lateral sits on the row's own center
        // tree, forcing the planner to actually scan for a nearby gap
        // instead of getting one for free.
        List<StreetRoutePlanner.Waypoint> route = StreetRoutePlanner.Plan(
            0, 0f, 1, 0f, Bands(bands), Min, Max, Clearance, ScanStep);

        float crossingLateral = Math.Abs(route[0].Lateral);
        Assert.True(
            crossingLateral < 100f,
            $"crossing at {route[0].Lateral} skipped over the near inter-tree gap and detoured around the whole row instead");
    }

    [Fact]
    public void BlockedCrossing_DetoursThroughNearestGap()
    {
        var bands = new Dictionary<int, IReadOnlyList<StreetRoutePlanner.Interval>>
        {
            // Band 0 (between roads 0 and 1) has a building covering the
            // hero's lateral position; the nearest gap is to the right.
            [0] = new[] { new StreetRoutePlanner.Interval(-60f, 60f) },
        };

        List<StreetRoutePlanner.Waypoint> route = StreetRoutePlanner.Plan(
            0, 0f, 1, 0f, Bands(bands), Min, Max, Clearance, ScanStep);

        // Walk along road 0 to the gap, cross, then walk back to the target.
        Assert.Equal(0, route[0].Street);
        Assert.True(Math.Abs(route[0].Lateral) > 60f, "crossing must be outside the blocked span");
        Assert.Equal(1, route[1].Street);
        Assert.Equal(route[0].Lateral, route[1].Lateral);
        Assert.Equal(1, route[^1].Street);
        Assert.Equal(0f, route[^1].Lateral);
    }

    [Fact]
    public void MovingCloser_ChecksTheBandBelowTheCurrentStreet()
    {
        var bands = new Dictionary<int, IReadOnlyList<StreetRoutePlanner.Interval>>
        {
            // Moving from road 1 down to road 0 crosses band 0.
            [0] = new[] { new StreetRoutePlanner.Interval(-30f, 30f) },
        };

        List<StreetRoutePlanner.Waypoint> route = StreetRoutePlanner.Plan(
            1, 0f, 0, 0f, Bands(bands), Min, Max, Clearance, ScanStep);

        Assert.True(Math.Abs(route[0].Lateral) > 30f);
        Assert.Equal(0, route[^1].Street);
        Assert.Equal(0f, route[^1].Lateral);
    }

    [Fact]
    public void FullyBlockedBand_FallsBackToDirectCrossing()
    {
        var bands = new Dictionary<int, IReadOnlyList<StreetRoutePlanner.Interval>>
        {
            [0] = new[] { new StreetRoutePlanner.Interval(Min, Max) },
        };

        List<StreetRoutePlanner.Waypoint> route = StreetRoutePlanner.Plan(
            0, 45f, 1, 45f, Bands(bands), Min, Max, Clearance, ScanStep);

        // Best-effort route instead of a stuck hero (documented fallback).
        Assert.Equal(1, route[^1].Street);
        Assert.Equal(45f, route[^1].Lateral);
    }

    [Fact]
    public void IsCrossingBlocked_RespectsClearanceMargin()
    {
        var band = new[] { new StreetRoutePlanner.Interval(0f, 90f) };

        Assert.True(StreetRoutePlanner.IsCrossingBlocked(band, 100f, Clearance));
        Assert.False(StreetRoutePlanner.IsCrossingBlocked(band, 120f, Clearance));
    }

    [Fact]
    public void FindViableCrossing_ReturnsNullWhenEverythingIsBlocked()
    {
        var band = new[] { new StreetRoutePlanner.Interval(Min - Clearance, Max + Clearance) };

        Assert.Null(StreetRoutePlanner.FindViableCrossing(
            band, 0f, Min, Max, Clearance, ScanStep));
    }

    [Fact]
    public void Plan_PrefersCrossingNearDestination_NotNearOrigin()
    {
        // Two narrow gaps far apart: one right where the hero starts
        // (lateral -450), one right where the hero is headed (lateral
        // 436). Everything else on the crossing band is blocked.
        var bands = new Dictionary<int, IReadOnlyList<StreetRoutePlanner.Interval>>
        {
            [0] = new[]
            {
                new StreetRoutePlanner.Interval(Min, -480f),
                new StreetRoutePlanner.Interval(-420f, 420f),
                new StreetRoutePlanner.Interval(480f, Max),
            },
        };

        List<StreetRoutePlanner.Waypoint> route = StreetRoutePlanner.Plan(
            0, -450f, 1, 436f, Bands(bands), Min, Max, Clearance, ScanStep);

        // A planner that scans from the ORIGIN lateral would cross right
        // next to where the hero already stands, then have to walk the
        // entire remaining ~900px along the destination street with no
        // further obstacle avoidance — reading as if it skirted the whole
        // width of the row instead of heading toward the target. Preferring
        // the destination lateral means the crossing itself already lands
        // at (or very near) 436, so no such detour waypoint exists.
        Assert.DoesNotContain(route, w => w.Street == 1 && Math.Abs(w.Lateral - -450f) < 1f);
        Assert.Equal(436f, route[^1].Lateral);
        Assert.Equal(1, route[^1].Street);
    }

    [Fact]
    public void Plan_PrefersASingleCorridorAcrossMultipleBands_NoZigzag()
    {
        var bands = new Dictionary<int, IReadOnlyList<StreetRoutePlanner.Interval>>
        {
            [0] = new[] { new StreetRoutePlanner.Interval(-540f, 100f) }, // open right of ~114
            [1] = new[] { new StreetRoutePlanner.Interval(300f, 540f) },  // open left of ~286
        };

        // 200 clears both bands (114 < 200 < 286): a single corridor exists.
        List<StreetRoutePlanner.Waypoint> route = StreetRoutePlanner.Plan(
            0, -450f, 2, 200f, Bands(bands), Min, Max, Clearance, ScanStep);

        // Every waypoint should sit at the same lateral — the hero walks
        // straight to the shared corridor once and crosses both rows there,
        // never detouring to a different lateral for the second crossing
        // (which would read as a zigzag through the rows instead of a
        // direct approach).
        Assert.All(route, w => Assert.Equal(200f, w.Lateral));
    }

    [Fact]
    public void FindViableCrossing_PrefersTheNearestGap()
    {
        var band = new[]
        {
            new StreetRoutePlanner.Interval(-200f, -80f),
            new StreetRoutePlanner.Interval(-40f, 100f),
        };

        float? crossing = StreetRoutePlanner.FindViableCrossing(
            band, 0f, Min, Max, Clearance, ScanStep);

        Assert.NotNull(crossing);
        // The free strip between the two intervals (-80..-40) leaves room
        // for the clearance (its viable center range is -66..-54) and is
        // nearer to the preferred 0 than anything past +100.
        Assert.InRange(crossing.Value, -66f, -54f);
        Assert.False(StreetRoutePlanner.IsCrossingBlocked(band, crossing.Value, Clearance));
    }

    // ConvertNavmeshPathToWaypoints: the Godot-free half of the
    // NavigationServer2D-backed street router (StreetNavigationServerPlanner
    // itself needs a live engine and so is not unit-testable here — this
    // covers the pure "raw polyline -> quantized Waypoint list" conversion
    // it depends on, using synthetic paths in street-space (X = lateral,
    // Y = street depth) instead of a real baked navmesh query result.

    [Fact]
    public void ConvertNavmeshPath_SameStreet_PlansSingleLateralWaypoint()
    {
        List<StreetRoutePlanner.Waypoint> route = StreetRoutePlanner.ConvertNavmeshPathToWaypoints(
            Array.Empty<Vector2>(), 2, 0f, 2, 180f);

        StreetRoutePlanner.Waypoint waypoint = Assert.Single(route);
        Assert.Equal(2, waypoint.Street);
        Assert.Equal(180f, waypoint.Lateral);
    }

    [Fact]
    public void ConvertNavmeshPath_StraightSingleCrossing_NoLateralAdjustmentWaypoint()
    {
        var path = new[] { new Vector2(90f, 0f), new Vector2(90f, 1f) };

        List<StreetRoutePlanner.Waypoint> route = StreetRoutePlanner.ConvertNavmeshPathToWaypoints(
            path, 0, 90f, 1, 90f);

        // Already at the target lateral before crossing, so no pre-crossing
        // adjustment waypoint is needed — just the single crossing step.
        StreetRoutePlanner.Waypoint waypoint = Assert.Single(route);
        Assert.Equal(1, waypoint.Street);
        Assert.Equal(90f, waypoint.Lateral);
    }

    [Fact]
    public void ConvertNavmeshPath_SingleCrossingNeedingLateralMove_WalksThenCrosses()
    {
        var path = new[] { new Vector2(0f, 0f), new Vector2(50f, 1f) };

        List<StreetRoutePlanner.Waypoint> route = StreetRoutePlanner.ConvertNavmeshPathToWaypoints(
            path, 0, 0f, 1, 50f);

        Assert.Equal(2, route.Count);
        Assert.Equal(0, route[0].Street);
        Assert.Equal(50f, route[0].Lateral);
        Assert.Equal(1, route[1].Street);
        Assert.Equal(50f, route[1].Lateral);
    }

    /// <summary>
    /// The real payoff of routing through a genuine navmesh: a multi-band
    /// crossing whose real shortest path zigzags between two different
    /// laterals (unlike the greedy planner's own "single shared corridor or
    /// bust" heuristic) still decomposes into the same "walk to X on this
    /// street, then cross" waypoint shape at each intermediate street.
    /// </summary>
    [Fact]
    public void ConvertNavmeshPath_MultiStreetPath_SamplesLateralAtEachStreetBoundary()
    {
        var path = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(100f, 1f),
            new Vector2(200f, 2f),
            new Vector2(300f, 3f),
        };

        List<StreetRoutePlanner.Waypoint> route = StreetRoutePlanner.ConvertNavmeshPathToWaypoints(
            path, 0, 0f, 3, 300f);

        Assert.Equal(
            new (int Street, float Lateral)[]
            {
                (0, 100f), (1, 100f),
                (1, 200f), (2, 200f),
                (2, 300f), (3, 300f),
            },
            route.ConvertAll(w => (w.Street, w.Lateral)).ToArray());
    }

    /// <summary>
    /// The bug this test pins down: the final crossing (into
    /// <paramref name="toStreet"/>) used to be hardcoded to the raw
    /// destination lateral instead of sampled from the real path like every
    /// other crossing — discarding whatever obstacle-avoiding detour the
    /// navmesh had actually planned for that last band and drawing a
    /// straight cardinal cross through it instead. Reported live as "the
    /// citizen walks around the whole row of trees, then crosses straight
    /// through the last one right before arriving".
    /// </summary>
    [Fact]
    public void ConvertNavmeshPath_FinalCrossingNeedsItsOwnDetour_SamplesItThenAdjuststoExactTarget()
    {
        // The real navmesh path dodges an obstacle right at the street-2
        // boundary (crossing at lateral 200) before the destination itself
        // (lateral 300) requires one more lateral walk within street 2.
        var path = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(100f, 1f),
            new Vector2(200f, 2f),
            new Vector2(300f, 2.5f),
        };

        List<StreetRoutePlanner.Waypoint> route = StreetRoutePlanner.ConvertNavmeshPathToWaypoints(
            path, 0, 0f, 2, 300f);

        Assert.Equal(
            new (int Street, float Lateral)[]
            {
                (0, 100f), (1, 100f),
                (1, 200f), (2, 200f),
                (2, 300f),
            },
            route.ConvertAll(w => (w.Street, w.Lateral)).ToArray());
    }

    [Fact]
    public void ConvertNavmeshPath_MissingSamplePoint_FallsBackToRequestedLateral()
    {
        // A degenerate path that never actually reaches the intermediate
        // street's depth (street 1, crossing 0 -> 2) — the sampler must
        // not throw, and should fall back to a sane value rather than
        // propagate a NaN/garbage lateral.
        var path = new[] { new Vector2(0f, 5f), new Vector2(10f, 6f) };

        List<StreetRoutePlanner.Waypoint> route = StreetRoutePlanner.ConvertNavmeshPathToWaypoints(
            path, 0, 0f, 2, 42f);

        Assert.Equal(2, route[^1].Street);
        Assert.Equal(42f, route[^1].Lateral);
    }
}
