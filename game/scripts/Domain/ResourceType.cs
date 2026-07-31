namespace WorldofGoses.Domain;

/// <summary>
/// Resources that buildings can produce, citizens can gather from the
/// ground, and cities can store. Open-extensible: a future slice that adds
/// "Potions", "Lumber", "Cloth" or whatever does not have to touch any code
/// that doesn't deal with those resources directly. Buildings carry their
/// produced resource type as data, not via a Kind-switch in
/// <see cref="Building"/>.
/// </summary>
public enum ResourceType
{
    Stone = 0,
    Food = 1,
    Iron = 2,
    Potions = 3,
    /// <summary>
    /// Wood gathered from <see cref="BuildingKind.Forest"/> plots.
    /// Required as input by the Basic Shelter recipe; future
    /// carpenter / forestry buildings will consume it too.
    /// </summary>
    Wood = 4,
    /// <summary>
    /// Branches collected from the ground during the founding camp
    /// phase (EG-A0). Currency for the Campfire, Bedroll, Cache and
    /// Canopy modules that compose into the Basic Shelter.
    /// </summary>
    Branches = 5,
    /// <summary>
    /// Plant fiber bundled from the ground (EG-A0). Consumed by
    /// Bedroll, Cache and Canopy; visually a leaf variant of the same
    /// pickup sprite.
    /// </summary>
    PlantFiber = 6,
    /// <summary>
    /// Small stones piled on the ground (EG-A0). Used by the
    /// Campfire ring and the plot preparation step.
    /// </summary>
    SmallStone = 7,
    /// <summary>
    /// Wild food harvested from the ground (EG-A0). Buffer against
    /// the daily ration, seed for the first crop, or supply for the
    /// first Food sortie.
    /// </summary>
    WildFood = 8,
}
