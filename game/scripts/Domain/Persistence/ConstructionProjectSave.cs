#nullable enable
using System.Collections.Generic;

namespace WorldofGoses.Domain.Persistence;

/// <summary>Serializable form of a <see cref="ConstructionProject"/>.</summary>
public sealed class ConstructionProjectSave
{
    public int Id { get; set; }
    public string Kind { get; set; } = "BasicShelter";
    public string DisplayName { get; set; } = "Basic Shelter";
    public int Progress { get; set; }
    public int RequiredWork { get; set; }
    public int WorkerCapacity { get; set; }
    public bool Enabled { get; set; } = true;
    public List<int> AssignedCitizenIds { get; set; } = new();
}
