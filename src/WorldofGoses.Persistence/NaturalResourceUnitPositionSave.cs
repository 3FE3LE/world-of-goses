using WorldofGoses.Domain;
namespace WorldofGoses.Persistence;

public sealed class NaturalResourceUnitPositionSave
{
    public int RowWithinParcel { get; set; }
    public int FrontageColumnWithinParcel { get; set; }
}
