namespace WorldofGoses.Domain;

/// <summary>
/// Identifies a kind of worksite a player can authorise. Each
/// <see cref="ConstructionKind"/> maps to a finished
/// <see cref="BuildingKind"/> when the work completes; this enum
/// is separate so future "kinds of construction" do not pollute
/// the semantic of completed buildings.
/// </summary>
public enum ConstructionKind
{
    BasicShelter = 0,
}
