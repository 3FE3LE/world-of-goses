namespace WorldofGoses.Domain;

/// <summary>
/// Functional phases of the single persistent Founding Site. Campfire is
/// always first, Bedroll and Cache may be completed in either order, and
/// Canopy consolidates the site into the Basic Shelter.
/// </summary>
public enum FoundingSiteModule
{
    Campfire = 0,
    Bedroll = 1,
    Cache = 2,
    Canopy = 3,
}
