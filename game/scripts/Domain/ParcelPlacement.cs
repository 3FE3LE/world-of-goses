using System;

namespace WorldofGoses.Domain;

/// <summary>Persistent zoning assignment shared by a project and its building.</summary>
public sealed class ParcelPlacement
{
    public BuildingId EntityId { get; }
    public ParcelId ParcelId { get; }
    public int LotColumn { get; }
    public int LotRow { get; }
    public int LotWidth { get; }
    public int LotHeight { get; }
    public string FootprintProfileId { get; }
    public BuildingOrientation Orientation { get; }

    public ParcelPlacement(
        BuildingId entityId,
        ParcelId parcelId,
        int lotColumn,
        int lotRow,
        int lotWidth,
        int lotHeight,
        string footprintProfileId,
        BuildingOrientation orientation)
    {
        if (entityId.Value <= 0) throw new ArgumentOutOfRangeException(nameof(entityId));
        if (parcelId.Value <= 0) throw new ArgumentOutOfRangeException(nameof(parcelId));
        if (lotColumn < 0 || lotColumn >= ParcelGrid.LotsPerAxis)
        {
            throw new ArgumentOutOfRangeException(nameof(lotColumn));
        }
        if (lotRow < 0 || lotRow >= ParcelGrid.LotsPerAxis)
        {
            throw new ArgumentOutOfRangeException(nameof(lotRow));
        }
        if (lotWidth <= 0 || lotColumn + lotWidth > ParcelGrid.LotsPerAxis)
        {
            throw new ArgumentOutOfRangeException(nameof(lotWidth));
        }
        if (lotHeight <= 0 || lotRow + lotHeight > ParcelGrid.LotsPerAxis)
        {
            throw new ArgumentOutOfRangeException(nameof(lotHeight));
        }
        if (string.IsNullOrWhiteSpace(footprintProfileId))
        {
            throw new ArgumentException("Footprint profile is required.", nameof(footprintProfileId));
        }
        EntityId = entityId;
        ParcelId = parcelId;
        LotColumn = lotColumn;
        LotRow = lotRow;
        LotWidth = lotWidth;
        LotHeight = lotHeight;
        FootprintProfileId = footprintProfileId;
        Orientation = orientation;
    }

    public bool Overlaps(ParcelPlacement other) =>
        ParcelId == other.ParcelId
        && LotColumn < other.LotColumn + other.LotWidth
        && LotColumn + LotWidth > other.LotColumn
        && LotRow < other.LotRow + other.LotHeight
        && LotRow + LotHeight > other.LotRow;
}
