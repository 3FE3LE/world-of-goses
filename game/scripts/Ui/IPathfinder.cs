using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

#nullable enable

namespace WorldofGoses.Ui;

/// <summary>
/// Pathfinding abstraction for the macro city view. Today a single
/// implementation (<see cref="CardinalPathfinder"/>) handles routes
/// between the hero carrier and resource trees / construction lots
/// using a cardinal-axis search with a small obstacle-avoidance set.
///
/// <para>
/// The seam exists so that when the transitable mesh slice
/// (<c>H-26</c>) lands, a new <c>NavigationServerPathfinder</c>
/// implementation can be swapped in without touching
/// <c>MacroCitizenActivity</c>. The interface is intentionally
/// narrow: route planning only, no per-tick movement (that lives in
/// <c>PixelMotion</c>). Consumers that need richer queries (e.g. "is
/// position X reachable from Y in N steps?") can extend the interface
/// in a follow-up slice; for now we keep the surface minimal.
/// </para>
/// </summary>
public interface IPathfinder
{
    /// <summary>
    /// Plans a route from <paramref name="start"/> to <paramref name="target"/>
    /// that avoids the given <paramref name="obstacles"/>. Implementations
    /// are expected to:
    ///   - return at least the target as the last waypoint;
    ///   - use integer-pixel waypoints (snap) so the
    ///     <c>PixelMotion.StepCardinal</c> cadence stays aligned;
    ///   - return an empty list (or single-element list) when no
    ///     obstacle is in the way (no detour needed).
    /// </summary>
    IReadOnlyList<Vector2> PlanRoute(
        Vector2 start,
        Vector2 target,
        IReadOnlyCollection<Rect2> obstacles);
}

/// <summary>
/// Cardinal-axis pathfinder with single-detour obstacle avoidance.
/// Origin: the only pathfinder that exists today. Production-safe
/// and deterministic, which is why the macro tests rely on it for
/// fixture stability. Will remain the fallback pathfinder even after
/// <c>NavigationServerPathfinder</c> lands, so the test suite keeps a
/// pure-CPU reference.
/// </summary>
public sealed class CardinalPathfinder : IPathfinder
{
    public IReadOnlyList<Vector2> PlanRoute(
        Vector2 start,
        Vector2 target,
        IReadOnlyCollection<Rect2> obstacles)
    {
        start = PixelMotion.Snap(start);
        target = PixelMotion.Snap(target);
        var candidates = new List<List<Vector2>>
        {
            new() { new Vector2(target.X, start.Y), target },
            new() { new Vector2(start.X, target.Y), target },
        };

        foreach (Rect2 obstacle in obstacles)
        {
            float above = Mathf.Floor(obstacle.Position.Y - 1f);
            float below = Mathf.Ceil(obstacle.End.Y + 1f);
            float left = Mathf.Floor(obstacle.Position.X - 1f);
            float right = Mathf.Ceil(obstacle.End.X + 1f);
            candidates.Add(new List<Vector2>
            {
                new(start.X, above), new(target.X, above), target,
            });
            candidates.Add(new List<Vector2>
            {
                new(start.X, below), new(target.X, below), target,
            });
            candidates.Add(new List<Vector2>
            {
                new(left, start.Y), new(left, target.Y), target,
            });
            candidates.Add(new List<Vector2>
            {
                new(right, start.Y), new(right, target.Y), target,
            });
        }

        List<Vector2>? best = null;
        float bestDistance = float.MaxValue;
        foreach (List<Vector2> candidate in candidates)
        {
            Vector2 from = start;
            float distance = 0f;
            bool blocked = false;
            foreach (Vector2 waypoint in candidate)
            {
                if (SegmentCrossesAny(from, waypoint, obstacles))
                {
                    blocked = true;
                    break;
                }
                distance += from.DistanceTo(waypoint);
                from = waypoint;
            }
            if (blocked) continue;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best ?? new List<Vector2> { target };
    }

    private static bool SegmentCrossesAny(
        Vector2 from,
        Vector2 to,
        IReadOnlyCollection<Rect2> obstacles)
    {
        foreach (Rect2 obstacle in obstacles)
        {
            // The pathfinder generates axis-aligned segments only
            // (cardinal routes). The check is therefore simplified to
            // "is this horizontal/vertical line overlapping the rect".
            // NavigationServerPathfinder handles arbitrary angles; this
            // implementation stays cheap and correct for cardinal routes.
            bool horizontal = Mathf.IsEqualApprox(from.Y, to.Y)
                && from.Y > obstacle.Position.Y
                && from.Y < obstacle.End.Y
                && Mathf.Max(from.X, to.X) > obstacle.Position.X
                && Mathf.Min(from.X, to.X) < obstacle.End.X;
            bool vertical = Mathf.IsEqualApprox(from.X, to.X)
                && from.X > obstacle.Position.X
                && from.X < obstacle.End.X
                && Mathf.Max(from.Y, to.Y) > obstacle.Position.Y
                && Mathf.Min(from.Y, to.Y) < obstacle.End.Y;
            if (horizontal || vertical) return true;
        }
        return false;
    }
}

/// <summary>
/// <see cref="NavigationServer2D"/>-backed pathfinder (S-1.2). Owns a
/// private navigation map + region (not tied to any <see cref="World2D"/>
/// or scene node), rebaking the traversable polygon from the caller's
/// obstacle rects on every <see cref="PlanRoute"/> call. Call frequency is
/// low — once per hero travel command, not per tick — so a full rebake per
/// call is cheap enough and avoids the complexity of diffing obstacle sets.
///
/// <para>
/// <see cref="NavigationServer2D"/> only exists while a live Godot engine
/// is running, so this implementation is wired into <c>MacroCitizenActivity</c>
/// for real gameplay. Tests keep using <see cref="CardinalPathfinder"/>
/// directly — it requires no engine loop and stays the deterministic
/// reference the fixtures already rely on.
/// </para>
/// </summary>
public sealed class NavigationServerPathfinder : IPathfinder, IDisposable
{
    private const float BoundsPadding = 256f;

    private readonly Rid _map;
    private readonly Rid _region;
    private readonly NavigationPolygon _polygon = new();
    private bool _disposed;

    public NavigationServerPathfinder()
    {
        _map = NavigationServer2D.MapCreate();
        NavigationServer2D.MapSetActive(_map, true);
        _region = NavigationServer2D.RegionCreate();
        NavigationServer2D.RegionSetMap(_region, _map);
        NavigationServer2D.RegionSetEnabled(_region, true);
    }

    public IReadOnlyList<Vector2> PlanRoute(
        Vector2 start,
        Vector2 target,
        IReadOnlyCollection<Rect2> obstacles)
    {
        start = PixelMotion.Snap(start);
        target = PixelMotion.Snap(target);
        Bake(start, target, obstacles);

        Vector2[] path = NavigationServer2D.MapGetPath(_map, start, target, true);
        var waypoints = new List<Vector2>();
        // The server includes the start point; PlanRoute's contract is
        // "waypoints after start", matching CardinalPathfinder.
        for (int i = 1; i < path.Length; i++)
        {
            waypoints.Add(PixelMotion.Snap(path[i]));
        }
        if (waypoints.Count == 0) waypoints.Add(target);
        return waypoints;
    }

    private void Bake(Vector2 start, Vector2 target, IReadOnlyCollection<Rect2> obstacles)
    {
        Rect2 bounds = new(start, Vector2.Zero);
        bounds = bounds.Expand(target);
        foreach (Rect2 obstacle in obstacles) bounds = bounds.Merge(obstacle);
        bounds = bounds.Grow(BoundsPadding);

        var source = new NavigationMeshSourceGeometryData2D();
        source.AddTraversableOutline(RectOutline(bounds));
        foreach (Rect2 obstacle in obstacles)
        {
            source.AddObstructionOutline(RectOutline(obstacle));
        }

        // The non-"Async" overload still bakes synchronously; it just
        // also accepts a completion Callable, which a no-op satisfies.
        NavigationServer2D.BakeFromSourceGeometryData(
            _polygon, source, Callable.From(() => { }));
        NavigationServer2D.RegionSetNavigationPolygon(_region, _polygon);
#pragma warning disable CS0618 // single-threaded context, exactly what this pathfinder is
        NavigationServer2D.MapForceUpdate(_map);
#pragma warning restore CS0618
    }

    private static Vector2[] RectOutline(Rect2 rect) => new[]
    {
        rect.Position,
        new Vector2(rect.End.X, rect.Position.Y),
        rect.End,
        new Vector2(rect.Position.X, rect.End.Y),
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        NavigationServer2D.FreeRid(_region);
        NavigationServer2D.FreeRid(_map);
    }
}
