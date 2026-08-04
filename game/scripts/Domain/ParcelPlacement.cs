using System;

namespace WorldofGoses.Domain;

/// <summary>Persistent zoning assignment shared by a project and its building.</summary>
public sealed class ParcelPlacement
{
    public BuildingId EntityId { get; }
    public ParcelId ParcelId { get; }
    public BuildingReservation Reservation { get; }
    public ConstructionRowId RowId => Reservation.RowId;
    public int StartColumn => Reservation.StartColumn;
    public int FrontageColumns => Reservation.FrontageColumns;
    public int DepthRows => Reservation.DepthRows;
    public int BaseFrontageColumns => Reservation.BaseFrontageColumns;
    public int LeftExpansionColumns => Reservation.LeftExpansionColumns;
    public int RightExpansionColumns => Reservation.RightExpansionColumns;
    public int LotColumn { get; }
    public int LotRow { get; }
    public int LotWidth { get; }
    public int LotHeight { get; }
    public string FootprintProfileId { get; }
    public BuildingOrientation Orientation { get; }

    public ParcelPlacement(
        BuildingId entityId,
        ParcelId parcelId,
        ConstructionRowId rowId,
        int startColumn,
        int frontageColumns,
        int depthRows,
        int baseFrontageColumns,
        int leftExpansionColumns,
        int rightExpansionColumns,
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
        Reservation = new BuildingReservation(
            entityId,
            rowId,
            startColumn,
            frontageColumns,
            depthRows,
            baseFrontageColumns,
            leftExpansionColumns,
            rightExpansionColumns);
        LotColumn = lotColumn;
        LotRow = lotRow;
        LotWidth = lotWidth;
        LotHeight = lotHeight;
        FootprintProfileId = footprintProfileId;
        Orientation = orientation;
    }

    public bool Overlaps(ParcelPlacement other) => Reservation.Overlaps(other.Reservation);
}
