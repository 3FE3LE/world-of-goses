#nullable enable
using System;
using System.Collections.Generic;
using Godot;

namespace WorldofGoses.Prototypes;

/// <summary>
/// <see cref="NavigationServer2D"/>-backed street router (S-1.2, redirected
/// at the perspective view) — the real fix for the multi-band zigzag
/// <see cref="StreetRoutePlanner.Plan"/>'s greedy shared-corridor/nearest-gap
/// heuristic cannot solve in general: a genuine A* over the actual gaps
/// between obstacles across every intervening band, instead of guessing one
/// shared lateral or falling back to a per-band nearest-gap search.
///
/// <para>
/// Mirrors <c>NavigationServerPathfinder</c>'s own precedent exactly: owns
/// a private map/region (not tied to any scene node), rebakes on every
/// <see cref="Plan"/> call (once per travel command — a gather click or a
/// new work assignment — never per tick, so a full rebake per call is
/// cheap), and only exists while a live Godot engine is running, which is
/// why <see cref="StreetRoutePlanner"/> stays the Godot-free reference
/// <c>StreetRoutePlannerTests.cs</c> covers directly and the fallback this
/// class defers to when the navmesh genuinely finds no path at all.
/// </para>
///
/// <para>
/// The mesh's own coordinate space is (lateral, street × <see cref="DepthUnitPx"/>)
/// — streets are spaced by a real pixel-ish unit (not bare integers) so
/// the bake's default cell size behaves the same as it does for the
/// pixel-scale lateral axis, then <see cref="StreetRoutePlanner.ConvertNavmeshPathToWaypoints"/>
/// (Godot-free, separately unit-tested) turns the raw polyline back into
/// the same quantized <see cref="StreetRoutePlanner.Waypoint"/> shape the
/// greedy planner already produces, so no consumer
/// (<c>MacroStreetLiveView.AdvanceRouteTick</c>) needs to know which
/// planner produced its route.
/// </para>
/// </summary>
public sealed class StreetNavigationServerPlanner : IDisposable
{
    private const float DepthUnitPx = 200f;
    private const float DepthPadding = 0.5f * DepthUnitPx;

    private readonly Rid _map;
    private readonly Rid _region;
    private readonly NavigationPolygon _polygon = new();
    private bool _disposed;

    public StreetNavigationServerPlanner()
    {
        _map = NavigationServer2D.MapCreate();
        NavigationServer2D.MapSetActive(_map, true);
        _region = NavigationServer2D.RegionCreate();
        NavigationServer2D.RegionSetMap(_region, _map);
        NavigationServer2D.RegionSetEnabled(_region, true);
        // Bake already erodes each obstruction outline by the caller's own
        // `clearance` (see Bake below) — the one Godot exposes as "how much
        // space a citizen needs around an obstacle" for this planner.
        // NavigationPolygon.AgentRadius is Godot's OWN separate erosion
        // pass applied on top during baking, defaulting to a nonzero value
        // meant for a mesh with no manual clearance of its own; left at
        // that default here, it double-counts the margin already applied,
        // shrinking a real ~18px gap between adjacent trees (see
        // StreetRoutePlanner's own doc comment on ScanStepPx for that
        // figure) below zero and sealing it — the citizen then has no
        // choice but to detour around the whole row. Zero it out so this
        // planner's only clearance is the one it already applies itself.
        _polygon.AgentRadius = 0f;
    }

    /// <summary>
    /// Plans a route through the real navmesh; returns null when the query
    /// genuinely finds no path (fully disconnected geometry) so the caller
    /// can fall back to <see cref="StreetRoutePlanner.Plan"/>'s best-effort
    /// behavior instead of stranding the hero.
    /// </summary>
    public List<StreetRoutePlanner.Waypoint>? Plan(
        int fromStreet,
        float fromLateral,
        int toStreet,
        float toLateral,
        Func<int, IReadOnlyList<StreetRoutePlanner.Interval>> bandOccupancy,
        int streetCount,
        float min,
        float max,
        float clearance)
    {
        if (fromStreet == toStreet)
        {
            return new List<StreetRoutePlanner.Waypoint> { new(toStreet, toLateral) };
        }

        int lowStreet = Math.Clamp(Math.Min(fromStreet, toStreet) - 1, 0, streetCount - 1);
        int highStreet = Math.Clamp(Math.Max(fromStreet, toStreet) + 1, 0, streetCount - 1);
        Bake(lowStreet, highStreet, bandOccupancy, min, max, clearance);

        var start = new Vector2(fromLateral, fromStreet * DepthUnitPx);
        var target = new Vector2(toLateral, toStreet * DepthUnitPx);
        Vector2[] rawPath = NavigationServer2D.MapGetPath(_map, start, target, true);
        if (rawPath.Length == 0) return null;

        // Rescale the path's depth axis back to plain street units before
        // handing it to the Godot-free converter.
        var streetSpacePath = new Vector2[rawPath.Length];
        for (int i = 0; i < rawPath.Length; i++)
        {
            streetSpacePath[i] = new Vector2(rawPath[i].X, rawPath[i].Y / DepthUnitPx);
        }

        return StreetRoutePlanner.ConvertNavmeshPathToWaypoints(
            streetSpacePath, fromStreet, fromLateral, toStreet, toLateral);
    }

    private void Bake(
        int lowStreet,
        int highStreet,
        Func<int, IReadOnlyList<StreetRoutePlanner.Interval>> bandOccupancy,
        float min,
        float max,
        float clearance)
    {
        float top = lowStreet * DepthUnitPx - DepthPadding;
        float bottom = highStreet * DepthUnitPx + DepthPadding;

        var source = new NavigationMeshSourceGeometryData2D();
        source.AddTraversableOutline(new[]
        {
            new Vector2(min, top),
            new Vector2(max, top),
            new Vector2(max, bottom),
            new Vector2(min, bottom),
        });

        for (int band = lowStreet; band < highStreet; band++)
        {
            float bandTop = band * DepthUnitPx;
            float bandBottom = (band + 1) * DepthUnitPx;
            foreach (StreetRoutePlanner.Interval interval in bandOccupancy(band))
            {
                float left = Math.Clamp(interval.Start - clearance, min, max);
                float right = Math.Clamp(interval.End + clearance, min, max);
                if (right <= left) continue;
                source.AddObstructionOutline(new[]
                {
                    new Vector2(left, bandTop),
                    new Vector2(right, bandTop),
                    new Vector2(right, bandBottom),
                    new Vector2(left, bandBottom),
                });
            }
        }

        NavigationServer2D.BakeFromSourceGeometryData(
            _polygon, source, Callable.From(() => { }));
        NavigationServer2D.RegionSetNavigationPolygon(_region, _polygon);
#pragma warning disable CS0618 // single-threaded context, exactly what this planner is
        NavigationServer2D.MapForceUpdate(_map);
#pragma warning restore CS0618
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        NavigationServer2D.FreeRid(_region);
        NavigationServer2D.FreeRid(_map);
    }
}
