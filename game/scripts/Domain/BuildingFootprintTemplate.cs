using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Asset-independent zoning template. The reserved rectangle owns placement;
/// the solid rectangle blocks movement. Their difference remains available as
/// setbacks which adjacent buildings can combine into passages.
/// </summary>
public sealed class BuildingFootprintTemplate
{
    public string Id { get; }
    public HalfTileRect ReservedArea { get; }
    public HalfTileRect SolidArea { get; }
    public int LeftSetback => SolidArea.X - ReservedArea.X;
    public int RightSetback => ReservedArea.Right - SolidArea.Right;
    public int BackSetback => SolidArea.Y - ReservedArea.Y;
    public int FrontSetback => ReservedArea.Bottom - SolidArea.Bottom;

    public BuildingFootprintTemplate(
        string id,
        HalfTileRect reservedArea,
        HalfTileRect solidArea)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Footprint id is required.", nameof(id));
        }
        reservedArea.ValidatePositive();
        solidArea.ValidatePositive();
        if (!reservedArea.Contains(solidArea))
        {
            throw new ArgumentException(
                "The solid building area must fit inside its reserved area.",
                nameof(solidArea));
        }
        Id = id;
        ReservedArea = reservedArea;
        SolidArea = solidArea;
    }
}
