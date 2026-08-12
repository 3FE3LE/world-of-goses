using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="ResourceOpportunityState"/>.
/// Architecture Hardening A7.
/// </summary>
internal static class ResourceOpportunityStateSaveIds
{
    public const string AvailableId = "Available";
    public const string ReservedId = "Reserved";
    public const string DepletedId = "Depleted";

    public static string ToId(ResourceOpportunityState value) => value switch
    {
        ResourceOpportunityState.Available => AvailableId,
        ResourceOpportunityState.Reserved => ReservedId,
        ResourceOpportunityState.Depleted => DepletedId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out ResourceOpportunityState value)
    {
        switch (id)
        {
            case AvailableId: value = ResourceOpportunityState.Available; return true;
            case ReservedId: value = ResourceOpportunityState.Reserved; return true;
            case DepletedId: value = ResourceOpportunityState.Depleted; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
