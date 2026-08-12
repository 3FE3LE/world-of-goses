using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="ResourceOpportunityKind"/>.
/// Architecture Hardening A7.
/// </summary>
internal static class ResourceOpportunityKindSaveIds
{
    public const string NearbyFoodForageId = "NearbyFoodForage";
    public const string FallenWoodSearchId = "FallenWoodSearch";
    public const string SpiritTrailSearchId = "SpiritTrailSearch";

    public static string ToId(ResourceOpportunityKind value) => value switch
    {
        ResourceOpportunityKind.NearbyFoodForage => NearbyFoodForageId,
        ResourceOpportunityKind.FallenWoodSearch => FallenWoodSearchId,
        ResourceOpportunityKind.SpiritTrailSearch => SpiritTrailSearchId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out ResourceOpportunityKind value)
    {
        switch (id)
        {
            case NearbyFoodForageId: value = ResourceOpportunityKind.NearbyFoodForage; return true;
            case FallenWoodSearchId: value = ResourceOpportunityKind.FallenWoodSearch; return true;
            case SpiritTrailSearchId: value = ResourceOpportunityKind.SpiritTrailSearch; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
