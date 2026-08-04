namespace WorldofGoses.Domain;

/// <summary>
/// Player-selectable frontage window for a construction blueprint. The name
/// remains temporarily for source compatibility with the construction UI.
/// Logical row/start coordinates are authoritative; parcel coordinates only
/// identify the territory containing the window's first column.
/// </summary>
public readonly record struct ConstructionLot(
    ParcelId ParcelId,
    int ParcelColumn,
    int ParcelRow,
    ConstructionRowId RowId,
    int StartColumn,
    int FrontageColumns)
{
    public int LotRow => RowId.Value - ParcelRow * ParcelGrid.ConstructionRowsPerParcel;
    public int LotColumn =>
        (StartColumn - ParcelColumn * ParcelGrid.FrontageColumnsPerParcel)
        / ParcelGrid.TilesPerStandardLot;
}
