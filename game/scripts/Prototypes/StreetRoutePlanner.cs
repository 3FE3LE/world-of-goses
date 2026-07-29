#nullable enable
using System;
using System.Collections.Generic;
using Godot;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Pure street-route planning for the pseudo-3D macro view (H-32).
///
/// Model: each "calle" (depth row) is the free front band of a lot-row —
/// the corridor the buildings' front setback leaves open (the same reading
/// H-26 gives corridors: streets exist between constructions, where the
/// footprints leave room). Walking ALONG a road is always viable; CROSSING
/// from road <c>b</c> to road <c>b+1</c> walks through lot band <c>b</c>,
/// so that band's occupied lateral intervals decide where the crossing is
/// viable — the hero should thread BETWEEN individual trees/buildings, not
/// walk through their sprites, matching how manual W/S exploration always
/// respected the visible gaps.
///
/// A prior version of this file removed obstacle avoidance entirely after
/// a misread of user feedback; the actual ask was to REFINE it, not drop
/// it. The real bug behind "walks around the whole row instead of between
/// them": the gap-scan step (originally a coarse third of a lot, ~30 px)
/// could jump clean over a narrow viable gap between two adjacent
/// same-row obstacles (with today's spacing, that gap is often only
/// ~18 px wide) without the scan ever landing inside it, forcing the
/// search to keep going until it found a much wider — and much farther —
/// opening. <see cref="ScanStepPx"/> is now a small fraction of that,
/// fine enough that no realistic gap between two adjacent obstacles goes
/// undetected.
///
/// Deliberately Godot-free so xUnit covers it directly.
/// </summary>
public static class StreetRoutePlanner
{
    /// <summary>Occupied lateral span (screen-logical px) within one lot band.</summary>
    public readonly record struct Interval(float Start, float End)
    {
        public bool Contains(float value, float clearance) =>
            value > Start - clearance && value < End + clearance;
    }

    /// <summary>One quantized route target: stand on road <see cref="Street"/> at <see cref="Lateral"/>.</summary>
    public readonly record struct Waypoint(int Street, float Lateral);

    public static bool IsCrossingBlocked(
        IReadOnlyList<Interval> bandIntervals,
        float lateral,
        float clearance)
    {
        for (int i = 0; i < bandIntervals.Count; i++)
        {
            if (bandIntervals[i].Contains(lateral, clearance)) return true;
        }
        return false;
    }

    /// <summary>
    /// Nearest lateral position to <paramref name="preferred"/> where the
    /// band is clear, scanning outward in <paramref name="scanStep"/>
    /// increments within [<paramref name="min"/>, <paramref name="max"/>].
    /// Null when the whole band is blocked at that granularity.
    /// </summary>
    public static float? FindViableCrossing(
        IReadOnlyList<Interval> bandIntervals,
        float preferred,
        float min,
        float max,
        float clearance,
        float scanStep)
    {
        preferred = Math.Clamp(preferred, min, max);
        if (!IsCrossingBlocked(bandIntervals, preferred, clearance)) return preferred;
        int maxSteps = (int)Math.Ceiling((max - min) / scanStep);
        for (int i = 1; i <= maxSteps; i++)
        {
            float right = preferred + i * scanStep;
            if (right <= max && !IsCrossingBlocked(bandIntervals, right, clearance))
            {
                return right;
            }
            float left = preferred - i * scanStep;
            if (left >= min && !IsCrossingBlocked(bandIntervals, left, clearance))
            {
                return left;
            }
        }
        return null;
    }

    private static bool IsCrossingBlockedInAny(
        IReadOnlyList<IReadOnlyList<Interval>> bands, float lateral, float clearance)
    {
        for (int i = 0; i < bands.Count; i++)
        {
            if (IsCrossingBlocked(bands[i], lateral, clearance)) return true;
        }
        return false;
    }

    /// <summary>
    /// Like <see cref="FindViableCrossing"/> but requires the SAME lateral
    /// to be simultaneously clear across every band in
    /// <paramref name="bands"/> — a single corridor that threads straight
    /// through a multi-street crossing without zigzagging.
    /// </summary>
    private static float? FindSharedCorridor(
        IReadOnlyList<IReadOnlyList<Interval>> bands,
        float preferred,
        float min,
        float max,
        float clearance,
        float scanStep)
    {
        preferred = Math.Clamp(preferred, min, max);
        if (!IsCrossingBlockedInAny(bands, preferred, clearance)) return preferred;
        int maxSteps = (int)Math.Ceiling((max - min) / scanStep);
        for (int i = 1; i <= maxSteps; i++)
        {
            float right = preferred + i * scanStep;
            if (right <= max && !IsCrossingBlockedInAny(bands, right, clearance)) return right;
            float left = preferred - i * scanStep;
            if (left >= min && !IsCrossingBlockedInAny(bands, left, clearance)) return left;
        }
        return null;
    }

    /// <summary>
    /// Quantized route from one calle to another. Tries a single lateral
    /// corridor near <paramref name="toLateral"/> that is simultaneously
    /// viable across every intervening band FIRST — when the destination
    /// (or a nearby position) already threads straight through every row
    /// standing in the way, the hero walks there once and crosses all of
    /// them in a straight line, instead of finding an independent "nearest
    /// gap" per row and zigzagging between them. Falls back to a
    /// band-by-band search — which can zigzag — only when no single
    /// lateral clears every band; a real 2D navmesh (TO_DO S-1.2's
    /// <c>NavigationServer2D</c>, already used by the flat view) would
    /// solve that remaining case more robustly than this greedy heuristic
    /// ever can. When a band has no viable crossing at all the current
    /// lateral is used anyway — a best-effort route beats a hero that
    /// refuses to move.
    /// </summary>
    public static List<Waypoint> Plan(
        int fromStreet,
        float fromLateral,
        int toStreet,
        float toLateral,
        Func<int, IReadOnlyList<Interval>> bandOccupancy,
        float min,
        float max,
        float clearance,
        float scanStep)
    {
        var waypoints = new List<Waypoint>();
        if (fromStreet == toStreet)
        {
            waypoints.Add(new Waypoint(toStreet, toLateral));
            return waypoints;
        }

        int direction = Math.Sign(toStreet - fromStreet);
        var bands = new List<IReadOnlyList<Interval>>();
        for (int s = fromStreet; s != toStreet; s += direction)
        {
            bands.Add(bandOccupancy(direction > 0 ? s : s - 1));
        }

        float? corridor = FindSharedCorridor(bands, toLateral, min, max, clearance, scanStep);
        if (corridor.HasValue)
        {
            float sharedLateral = corridor.Value;
            if (Math.Abs(sharedLateral - fromLateral) > 0.01f)
            {
                waypoints.Add(new Waypoint(fromStreet, sharedLateral));
            }
            for (int s = fromStreet + direction; ; s += direction)
            {
                waypoints.Add(new Waypoint(s, sharedLateral));
                if (s == toStreet) break;
            }
            if (Math.Abs(sharedLateral - toLateral) > 0.01f)
            {
                waypoints.Add(new Waypoint(toStreet, toLateral));
            }
            return waypoints;
        }

        int street = fromStreet;
        float lateral = fromLateral;
        while (street != toStreet)
        {
            int band = direction > 0 ? street : street - 1;
            float crossing = FindViableCrossing(
                bandOccupancy(band), toLateral, min, max, clearance, scanStep)
                ?? lateral;
            if (Math.Abs(crossing - lateral) > 0.01f)
            {
                waypoints.Add(new Waypoint(street, crossing));
            }
            street += direction;
            waypoints.Add(new Waypoint(street, crossing));
            lateral = crossing;
        }
        if (waypoints.Count == 0 || Math.Abs(lateral - toLateral) > 0.01f)
        {
            waypoints.Add(new Waypoint(toStreet, toLateral));
        }
        return waypoints;
    }

    /// <summary>
    /// Converts a raw 2D polyline (X = lateral, Y = street depth, as
    /// returned by <c>NavigationServer2D.MapGetPath</c> once rescaled back
    /// to street units) into the same quantized <see cref="Waypoint"/>
    /// shape <see cref="Plan"/> already produces — one waypoint at the
    /// CURRENT street when the lateral needs to change before crossing,
    /// then one waypoint per street crossed. This keeps every consumer
    /// (<c>MacroStreetLiveView.AdvanceRouteTick</c>) unaware of whether a
    /// route came from the greedy band search above or from a real navmesh
    /// query — both ultimately decompose into "walk along this street to
    /// X, then cross" steps, never a diagonal cut across the mesh, so the
    /// hero's motion stays cardinal-ish and quantized either way.
    ///
    /// Godot-free (only <see cref="Vector2"/>/<see cref="Math"/> math, no
    /// engine services) so it can be covered directly by xUnit even though
    /// the caller that produces <paramref name="path"/> cannot be.
    /// </summary>
    public static List<Waypoint> ConvertNavmeshPathToWaypoints(
        IReadOnlyList<Vector2> path,
        int fromStreet,
        float fromLateral,
        int toStreet,
        float toLateral)
    {
        var waypoints = new List<Waypoint>();
        if (fromStreet == toStreet)
        {
            waypoints.Add(new Waypoint(toStreet, toLateral));
            return waypoints;
        }

        int direction = Math.Sign(toStreet - fromStreet);
        float currentLateral = fromLateral;
        int currentStreet = fromStreet;
        for (int targetStreet = fromStreet + direction; ; targetStreet += direction)
        {
            // Sample the FINAL street crossing exactly like every other one
            // instead of jumping straight to toLateral: the real navmesh
            // path may still be dodging an obstacle sitting right at this
            // last band's boundary, and forcing the raw target lateral here
            // threw that avoidance away, drawing a straight cardinal cross
            // through whatever stood between the previous waypoint and the
            // destination — "walks through a tree only on the last stretch"
            // was this exact bug, not the navmesh query itself.
            float crossingLateral = SampleLateralAtStreet(path, targetStreet, toLateral);
            if (Math.Abs(crossingLateral - currentLateral) > 0.01f)
            {
                waypoints.Add(new Waypoint(currentStreet, crossingLateral));
            }
            waypoints.Add(new Waypoint(targetStreet, crossingLateral));
            currentLateral = crossingLateral;
            currentStreet = targetStreet;
            if (targetStreet == toStreet) break;
        }
        // The sampled crossing can land short of the exact destination
        // lateral (interpolation, or the path approaching from an angle);
        // Plan's own greedy planner has the same final adjustment for the
        // same reason — the quantized route must still end exactly on
        // target, not merely "close, on the right street".
        if (Math.Abs(currentLateral - toLateral) > 0.01f)
        {
            waypoints.Add(new Waypoint(toStreet, toLateral));
        }
        return waypoints;
    }

    /// <summary>
    /// Interpolates the path's lateral (X) position at the depth (Y) where
    /// it crosses <paramref name="targetStreet"/>. Falls back to
    /// <paramref name="fallbackLateral"/> when the path has fewer than two
    /// points or never actually reaches that depth (baking/query edge
    /// cases) — a straight best-effort beats throwing away the whole route.
    /// </summary>
    private static float SampleLateralAtStreet(
        IReadOnlyList<Vector2> path, float targetStreet, float fallbackLateral)
    {
        if (path.Count < 2) return fallbackLateral;
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2 a = path[i];
            Vector2 b = path[i + 1];
            float minY = Math.Min(a.Y, b.Y);
            float maxY = Math.Max(a.Y, b.Y);
            if (targetStreet < minY || targetStreet > maxY) continue;
            if (Math.Abs(b.Y - a.Y) < 1e-4f) return a.X;
            float t = (targetStreet - a.Y) / (b.Y - a.Y);
            return a.X + t * (b.X - a.X);
        }
        return fallbackLateral;
    }
}
