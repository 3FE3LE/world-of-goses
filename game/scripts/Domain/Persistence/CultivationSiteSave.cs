#nullable enable
namespace WorldofGoses.Domain.Persistence;

public sealed class CultivationSiteSave
{
    public int Id { get; set; }
    public string State { get; set; } = "";
    public int? PlantedTick { get; set; }
    public int? ReadyAtTick { get; set; }
}
