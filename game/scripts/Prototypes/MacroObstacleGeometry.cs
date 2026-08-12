#nullable enable
using System;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Pure obstacle geometry math for the macro street view (A4). Both helpers
/// feed <see cref="StreetRoutePlanner"/>'s band map; the planner itself
/// stays Godot-free, so this layer does too.
/// <see cref="MacroStreetLiveView"/> keeps one-line forwarders so the
/// existing in-class usage and the <c>DynamicFrontageTests</c> surface keep
/// compiling unchanged.
/// </summary>
internal static class MacroObstacleGeometry
{
    /// <summary>Solid footprint of a placed building, derived from the
    /// persisted <c>StructuralStartHalfColumn</c> /
    /// <c>StructuralFrontageHalfColumns</c> properties. The reserved
    /// frontage interval is the wider parent; the navigation obstacle is
    /// the smaller solid interval inside it.</summary>
    public static StreetRoutePlanner.Interval BuildingObstacleInterval(
        CityMacroSnapshot.PlotItem item,
        float totalFrontageColumns,
        float tileUnitPx)
    {
        float reservedLeft = (item.StartColumn
            - totalFrontageColumns * 0.5f) * tileUnitPx;
        float reservedWidth = item.FrontageColumns * tileUnitPx;
        float solidLeft = (item.StructuralStartHalfColumn * 0.5f
            - totalFrontageColumns * 0.5f) * tileUnitPx;
        float solidWidth = item.StructuralFrontageHalfColumns * 0.5f * tileUnitPx;
        return ObstacleIntervalFromClearances(
            reservedLeft,
            reservedWidth,
            solidLeft - reservedLeft,
            reservedLeft + reservedWidth - solidLeft - solidWidth);
    }

    /// <summary>Build an obstacle interval from a reserved interval and the
    /// two clearances inside it. Both clearances must be non-negative and
    /// must leave a positive solid interval; otherwise the call throws
    /// because the planner would later have nowhere to plan a crossing.</summary>
    public static StreetRoutePlanner.Interval ObstacleIntervalFromClearances(
        float reservedStart,
        float reservedWidth,
        float leftClearance,
        float rightClearance)
    {
        if (reservedWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(reservedWidth));
        if (leftClearance < 0f) throw new ArgumentOutOfRangeException(nameof(leftClearance));
        if (rightClearance < 0f) throw new ArgumentOutOfRangeException(nameof(rightClearance));
        if (leftClearance + rightClearance >= reservedWidth)
        {
            throw new ArgumentException(
                "Obstacle clearances must leave a positive solid interval.");
        }
        return new StreetRoutePlanner.Interval(
            reservedStart + leftClearance,
            reservedStart + reservedWidth - rightClearance);
    }
}
