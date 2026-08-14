namespace WorldofGoses.Domain;

public enum ResourceOpportunityKind
{
    NearbyFoodForage = 0,
    FallenWoodSearch = 1,

    /// <summary>
    /// The trail the fire spirit left when it departed at dawn
    /// (<c>docs/systems/first-night.md</c> §11–12). Carries
    /// the same supply cost as <see cref="FallenWoodSearch"/> but
    /// rewards <see cref="ResourceType.Wood"/> in the form of
    /// fire-blackened remnants. Only appears in the expedition panel
    /// after the night has concluded — the trail cannot be read while
    /// the spirit is still present. Persisted as a string
    /// (<see cref="System.Enum.TryParse{TEnum}(string, bool, out TEnum})"/>
    /// tolerates the new value in legacy saves without a schema bump.
    /// </summary>
    SpiritTrailSearch = 2,
}
