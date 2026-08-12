#nullable enable
using WorldofGoses.Domain;
namespace WorldofGoses.Persistence;

public sealed class ParcelSave
{
    public int Id { get; set; }
    public int LogicalColumn { get; set; }
    public int LogicalRow { get; set; }
    public bool IsUnlocked { get; set; }
    public string? TerritoryState { get; set; }
}
