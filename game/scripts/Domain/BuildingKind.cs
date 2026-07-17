namespace WorldofGoses.Domain;

/// <summary>
/// Architectural kind of building. Distinct from the
/// <see cref="ResourceType"/> it produces: a building carries both
/// <c>Kind</c> (what type of place it is) and
/// <see cref="Building.ProducedResourceType"/> (what comes out of
/// it) as independent properties. The two are decoupled so future
/// slices can introduce new kinds or new resources without touching
/// this class.
/// </summary>
public enum BuildingKind
{
    Quarry = 0,
    Farm = 1,
    Smithy = 2,
    PotionLab = 3,
}
