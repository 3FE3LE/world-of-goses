using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="CitizenVitalStatus"/>.
/// Architecture Hardening A7.
/// </summary>
internal static class CitizenVitalStatusSaveIds
{
    public const string StableId = "Stable";
    public const string RecoveringId = "Recovering";
    public const string BlockedNoFoodId = "BlockedNoFood";

    public static string ToId(CitizenVitalStatus value) => value switch
    {
        CitizenVitalStatus.Stable => StableId,
        CitizenVitalStatus.Recovering => RecoveringId,
        CitizenVitalStatus.BlockedNoFood => BlockedNoFoodId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out CitizenVitalStatus value)
    {
        switch (id)
        {
            case StableId: value = CitizenVitalStatus.Stable; return true;
            case RecoveringId: value = CitizenVitalStatus.Recovering; return true;
            case BlockedNoFoodId: value = CitizenVitalStatus.BlockedNoFood; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
