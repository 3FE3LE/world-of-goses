using System.Collections.Generic;

namespace WorldofGoses.Domain.Persistence;

public sealed class NaturalResourcePatchSave
{
    public int Id { get; set; }
    public int ParcelId { get; set; }
    public string ResourceType { get; set; } = Domain.ResourceType.Wood.ToString();
    public int? LegacyStorageBuildingId { get; set; }
    public List<int> UnitReserves { get; set; } = new();
}
