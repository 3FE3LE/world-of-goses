namespace WorldofGoses.Domain;

/// <summary>
/// Resources that buildings can produce and cities can store. Open-
/// extensible: a future slice that adds "Potions", "Lumber",
/// "Cloth" or whatever does not have to touch any code that
/// doesn't deal with those resources directly. Buildings carry
/// their produced resource type as data, not via a Kind-switch in
/// <see cref="Building"/>.
/// </summary>
public enum ResourceType
{
    Stone = 0,
    Food = 1,
    Iron = 2,
    Potions = 3,
}
