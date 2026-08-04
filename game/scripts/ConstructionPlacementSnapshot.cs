#nullable enable
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Read-only placement grid. Domain state decides window validity; the macro
/// view only projects these cells and candidates into perspective.
/// </summary>
public sealed record ConstructionPlacementSnapshot(
    IReadOnlyList<ConstructionPlacementSnapshot.CellItem> Cells,
    IReadOnlyList<ConstructionPlacementSnapshot.WindowItem> Windows)
{
    public sealed record CellItem(
        ConstructionRowId RowId,
        int FrontageColumn,
        FrontageCellState State);

    public sealed record WindowItem(
        ConstructionLot Lot,
        FrontageCellState State)
    {
        public bool IsValid => State == FrontageCellState.Available;
    }

    public static ConstructionPlacementSnapshot From(CityWorld world)
    {
        var cells = new List<CellItem>();
        var windows = new List<WindowItem>();
        foreach (CityParcel parcel in world.Parcels.Values
                     .Where(candidate => candidate.IsUnlocked)
                     .OrderBy(candidate => candidate.LogicalRow)
                     .ThenBy(candidate => candidate.LogicalColumn))
        {
            int parcelStart = checked(
                parcel.LogicalColumn * ParcelGrid.FrontageColumnsPerParcel);
            for (int localRow = 0;
                 localRow < ParcelGrid.ConstructionRowsPerParcel;
                 localRow++)
            {
                ConstructionRowId rowId = ParcelGrid.ConstructionRow(
                    parcel.LogicalRow,
                    localRow);
                for (int localColumn = 0;
                     localColumn < ParcelGrid.FrontageColumnsPerParcel;
                     localColumn++)
                {
                    int frontageColumn = checked(parcelStart + localColumn);
                    cells.Add(new CellItem(
                        rowId,
                        frontageColumn,
                        world.FrontageState(rowId, frontageColumn)));
                    var lot = new ConstructionLot(
                        parcel.Id,
                        parcel.LogicalColumn,
                        parcel.LogicalRow,
                        rowId,
                        frontageColumn,
                        BuildingReservation.MinimumFrontageColumns);
                    windows.Add(new WindowItem(
                        lot,
                        world.ConstructionLotState(lot)));
                }
            }
        }
        return new ConstructionPlacementSnapshot(cells, windows);
    }
}
