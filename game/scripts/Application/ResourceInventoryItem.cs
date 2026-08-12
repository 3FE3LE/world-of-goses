#nullable enable
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>Immutable read-model row for a resource inventory surface. Shared
/// by the building-detail, city-status and construction snapshots, which is
/// why it lives in the application layer rather than beside the panels that
/// render it.</summary>
public sealed record ResourceInventoryItem(
    ResourceType Resource,
    int TotalAmount,
    int AvailableAmount);
