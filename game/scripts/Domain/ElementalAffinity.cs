namespace WorldofGoses.Domain;

/// <summary>
/// The six canonical elemental affinities. Cultural names are presentation
/// aliases only and never change the persisted identity.
/// </summary>
public enum ElementalAffinity
{
    Earth,
    Water,
    Fire,
    Air,
    Aether,
    Silence,
}

internal static class ElementalAffinityDisplay
{
    public static string DisplayName(ElementalAffinity affinity) => affinity switch
    {
        ElementalAffinity.Earth => "Earth",
        ElementalAffinity.Aether => "Aether",
        ElementalAffinity.Water => "Water",
        ElementalAffinity.Fire => "Fire",
        ElementalAffinity.Silence => "Silence",
        ElementalAffinity.Air => "Air",
        _ => affinity.ToString(),
    };

}
