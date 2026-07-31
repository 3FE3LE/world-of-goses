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

/// <summary>
/// Raised when a <see cref="NaturalResourcePatch"/>'s reserves change
/// so the presentation layer can refresh the ground-resource overlay
/// (Branches, Plant Fiber, Small Stone, Wild Food, Wood post-EG-1).
/// The event is patch-scoped to keep refresh scope tight.
/// </summary>
public sealed class PatchChangedEventArgs : EventArgs
{
    public PatchChangedEventArgs(int patchId)
    {
        PatchId = patchId;
    }

    public int PatchId { get; }
}
