using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="ParcelTerritoryState"/>.
/// Architecture Hardening A7. Schema v19 introduced this enum and
/// kept the legacy boolean in the DTO; schema v25+ persists the
/// state name directly.
/// </summary>
internal static class ParcelTerritoryStateSaveIds
{
    public const string LockedId = "Locked";
    public const string ReconnoitredId = "Reconnoitred";
    public const string RouteSecuredId = "RouteSecured";
    public const string AvailableId = "Available";

    public static string ToId(ParcelTerritoryState value) => value switch
    {
        ParcelTerritoryState.Locked => LockedId,
        ParcelTerritoryState.Reconnoitred => ReconnoitredId,
        ParcelTerritoryState.RouteSecured => RouteSecuredId,
        ParcelTerritoryState.Available => AvailableId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out ParcelTerritoryState value)
    {
        switch (id)
        {
            case LockedId: value = ParcelTerritoryState.Locked; return true;
            case ReconnoitredId: value = ParcelTerritoryState.Reconnoitred; return true;
            case RouteSecuredId: value = ParcelTerritoryState.RouteSecured; return true;
            case AvailableId: value = ParcelTerritoryState.Available; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
