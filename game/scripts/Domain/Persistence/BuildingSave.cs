using System.Collections.Generic;

namespace WorldofGoses.Domain.Persistence;

public sealed class BuildingSave
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
    /// <summary>Serialized as a string (e.g. <c>"Quarry"</c>) for forward compatibility with new kinds.</summary>
    public string Kind { get; set; } = BuildingKind.Quarry.ToString();
    /// <summary>Serialized as a string. Old saves default to <see cref="ResourceType.Stone"/>.</summary>
    public string ProducedResourceType { get; set; } = ResourceType.Stone.ToString();
    /// <summary>Serialized as the competency's underlying string. Old saves default to <see cref="CompetencyId.Mining"/>.</summary>
    public string ProducedCompetencyId { get; set; } = CompetencyId.Mining.Value;
    /// <summary>Capitalised display label, e.g. "Stone".</summary>
    public string ResourceLabel { get; set; } = "Resource";
    /// <summary>Singular inline unit, e.g. "stone".</summary>
    public string ResourceUnit { get; set; } = "units";
    public int WorkerCapacity { get; set; }
    public int VisualCapacity { get; set; }
    public int BaseProductionPerWorker { get; set; }
    public int StorageCapacity { get; set; }
    public int Stock { get; set; }
    public int IronStock { get; set; }
    /// <summary>
    /// Wood reserve carried by <see cref="BuildingKind.Forest"/> plots.
    /// Nullable so v2 saves that predate the wood-gathering slice
    /// deserialize cleanly: <see cref="CityWorld.Restore"/> falls
    /// back to <see cref="Building.SeedWoodReserve"/> defaults
    /// when missing.
    /// </summary>
    public int? WoodReserve { get; set; }
    public List<int> WoodUnitReserves { get; set; } = new();
    public bool ProductionEnabled { get; set; } = true;
    /// <summary>Legacy v2 field retained so old saves round-trip cleanly. New v3 saves ignore it.</summary>
    public int? TargetStock { get; set; }
    /// <summary>Nullable for forward-compat reads of v2 saves; defaults to 0.</summary>
    public int? MinStock { get; set; }
    /// <summary>Nullable for forward-compat reads of v2 saves; defaults to <see cref="StorageCapacity"/>.</summary>
    public int? MaxStock { get; set; }
    /// <summary>Nullable for forward-compat reads of v2 saves; defaults to 0.</summary>
    public int? Priority { get; set; }
    public List<int> AssignedCitizenIds { get; set; } = new();
}
