using System;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Scene-owned reconstruction anchors for a placed building. Save data keeps
/// only the building ID; the current map geometry derives these points, so a
/// visual layout change does not invalidate citizen persistence.
/// </summary>
public readonly record struct StreetVisualAnchor(int Street, float Lateral);

public readonly record struct BuildingVisualAnchors(
    StreetVisualAnchor Entrance,
    StreetVisualAnchor Exit,
    StreetVisualAnchor Work,
    StreetVisualAnchor Waiting,
    StreetVisualAnchor LeisureLeft,
    StreetVisualAnchor LeisureRight)
{
    public static BuildingVisualAnchors FromPlacement(
        int frontStreet,
        float lateral,
        int streetCount,
        float lateralHalfWidth,
        float stepPixels)
    {
        int entranceStreet = Math.Clamp(frontStreet, 0, Math.Max(0, streetCount - 1));
        float wait = Math.Clamp(lateral + stepPixels, -lateralHalfWidth, lateralHalfWidth);
        float leisureLeft = Math.Clamp(lateral - stepPixels * 3f, -lateralHalfWidth, lateralHalfWidth);
        float leisureRight = Math.Clamp(lateral + stepPixels * 3f, -lateralHalfWidth, lateralHalfWidth);
        var entrance = new StreetVisualAnchor(entranceStreet, lateral);
        return new BuildingVisualAnchors(
            entrance,
            entrance,
            new StreetVisualAnchor(entranceStreet, lateral),
            new StreetVisualAnchor(entranceStreet, wait),
            new StreetVisualAnchor(entranceStreet, leisureLeft),
            new StreetVisualAnchor(entranceStreet, leisureRight));
    }
}
