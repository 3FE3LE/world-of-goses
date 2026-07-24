namespace WorldofGoses.Domain.Persistence;

public sealed class ParcelPlacementSave
{
    public int EntityId { get; set; }
    public int ParcelId { get; set; }
    public int LotColumn { get; set; }
    public int LotRow { get; set; }
    public int LotWidth { get; set; } = 1;
    public int LotHeight { get; set; } = 1;
    public string FootprintProfileId { get; set; } = "standard-side-setbacks";
    public string Orientation { get; set; } = BuildingOrientation.South.ToString();
}
