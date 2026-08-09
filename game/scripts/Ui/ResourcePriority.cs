#nullable enable

using System.Collections.Generic;
using WorldofGoses.Domain;

namespace WorldofGoses.Ui;

/// <summary>
/// Shared priority sequence for resources in HUD surfaces. The top status
/// ticker and the city summary panel both consume this so the player reads
/// the same order in both places instead of a different sequence per surface.
/// </summary>
/// <remarks>
/// <para>
/// The order is <b>survival → construction inputs → remaining</b>. Survival
/// (food-related) precedes the resources the city's construction pipeline
/// actively consumes, with everything else trailing. Within each tier the
/// canonical <see cref="Sequence"/> order is preserved.
/// </para>
/// <para>
/// Reservations are not encoded as a separate tier here: a resource already
/// in the city inventory appears in its tier position regardless of how much
/// of it is reserved, and the chip's tooltip reports the reserved amount.
/// Promoting a reserved resource above an unreserved one of the same tier
/// would be a presentation-side decision the domain does not currently
/// support.
/// </para>
/// </remarks>
public static class ResourcePriority
{
    /// <summary>
    /// Canonical display order. Survival first, then the construction
    /// inputs the current recipes consume (Wood, Stone, Branches,
    /// PlantFiber, SmallStone), then the remaining resources.
    /// </summary>
    public static readonly IReadOnlyList<ResourceType> Sequence = new ResourceType[]
    {
        ResourceType.Food,        // survival
        ResourceType.WildFood,    // survival buffer / seed
        ResourceType.Wood,        // construction input
        ResourceType.Stone,       // construction input
        ResourceType.Branches,    // construction input
        ResourceType.PlantFiber,  // construction input
        ResourceType.SmallStone,  // construction input
        ResourceType.Iron,        // remaining
        ResourceType.Potions,     // remaining
    };

    /// <summary>Resources whose absence threatens the city's survival.</summary>
    public static readonly IReadOnlySet<ResourceType> Survival = new HashSet<ResourceType>
    {
        ResourceType.Food,
        ResourceType.WildFood,
    };

    /// <summary>
    /// Resources consumed by the current construction recipes (Basic Shelter
    /// and its founding-site modules).
    /// </summary>
    public static readonly IReadOnlySet<ResourceType> ConstructionInputs = new HashSet<ResourceType>
    {
        ResourceType.Wood,
        ResourceType.Stone,
        ResourceType.Branches,
        ResourceType.PlantFiber,
        ResourceType.SmallStone,
    };

    /// <summary>
    /// Returns the input resources sequenced by priority. Resources not
    /// present in the snapshot are skipped, so the result length matches the
    /// number of non-empty resource entries.
    /// </summary>
    public static IReadOnlyList<ResourceInventoryItem> Prioritize(
        IReadOnlyList<ResourceInventoryItem> resources)
    {
        var byType = new Dictionary<ResourceType, ResourceInventoryItem>(resources.Count);
        foreach (ResourceInventoryItem item in resources)
        {
            byType[item.Resource] = item;
        }
        var prioritized = new List<ResourceInventoryItem>(resources.Count);
        foreach (ResourceType resource in Sequence)
        {
            if (byType.TryGetValue(resource, out ResourceInventoryItem? item))
            {
                prioritized.Add(item);
            }
        }
        return prioritized;
    }
}
