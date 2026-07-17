using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Raised when a building's state changes in a way that affects what
/// the presentation layer renders (assignment, unassignment,
/// production tick). The event is intentionally building-scoped so
/// the presentation layer only refreshes what changed.
/// </summary>
public sealed class CityWorldChangedEventArgs : EventArgs
{
    public CityWorldChangedEventArgs(BuildingId buildingId)
    {
        BuildingId = buildingId;
    }

    public BuildingId BuildingId { get; }
}
