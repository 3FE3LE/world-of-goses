using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="ResourceReservationOwnerKind"/>.
/// Architecture Hardening A7.
/// </summary>
internal static class ResourceReservationOwnerKindSaveIds
{
    public static string ToId(ResourceReservationOwnerKind value) => value.ToString();

    public static bool TryParse(string id, out ResourceReservationOwnerKind value)
    {
        return Enum.TryParse(id, ignoreCase: true, out value);
    }
}
