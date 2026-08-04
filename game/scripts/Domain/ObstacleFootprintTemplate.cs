using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Asset-independent obstacle geometry. The reserved rectangle owns placement;
/// the solid rectangle blocks movement. Their difference is authored clearance
/// that remains navigable for any resource, construction, or infrastructure.
/// </summary>
public sealed class ObstacleFootprintTemplate
{
    public string Id { get; }
    public HalfTileRect ReservedArea { get; }
    public HalfTileRect SolidArea { get; }
    public int LeftClearance => SolidArea.X - ReservedArea.X;
    public int RightClearance => ReservedArea.Right - SolidArea.Right;
    public int BackClearance => SolidArea.Y - ReservedArea.Y;
    public int FrontClearance => ReservedArea.Bottom - SolidArea.Bottom;

    public ObstacleFootprintTemplate(
        string id,
        HalfTileRect reservedArea,
        HalfTileRect solidArea)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Obstacle footprint id is required.", nameof(id));
        }
        reservedArea.ValidatePositive();
        solidArea.ValidatePositive();
        if (!reservedArea.Contains(solidArea))
        {
            throw new ArgumentException(
                "The solid obstacle area must fit inside its reserved area.",
                nameof(solidArea));
        }
        Id = id;
        ReservedArea = reservedArea;
        SolidArea = solidArea;
    }
}
