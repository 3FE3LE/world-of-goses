namespace WorldofGoses.Domain.Persistence;

public sealed class ParcelPlacementSave
{
    public int EntityId { get; set; }
    public int ParcelId { get; set; }
    public int LotColumn { get; set; }
    public int LotRow { get; set; }
    public int LotWidth { get; set; } = 1;
    public int LotHeight { get; set; } = 1;
    public int RowId { get; set; }
    public int StartColumn { get; set; }
    public int FrontageColumns { get; set; } = BuildingReservation.MinimumFrontageColumns;
    public int DepthRows { get; set; } = BuildingReservation.RequiredDepthRows;
    public int BaseFrontageColumns { get; set; } = BuildingReservation.MinimumFrontageColumns;
    public int LeftExpansionColumns { get; set; }
    public int RightExpansionColumns { get; set; }
    public string FootprintProfileId { get; set; } = "standard-side-setbacks";
    public string Orientation { get; set; } = BuildingOrientation.South.ToString();
}
