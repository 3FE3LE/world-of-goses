#nullable enable
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>Immutable presentation row for a resource inventory surface.</summary>
public sealed record ResourceInventoryItem(
    ResourceType Resource,
    int TotalAmount,
    int AvailableAmount);
