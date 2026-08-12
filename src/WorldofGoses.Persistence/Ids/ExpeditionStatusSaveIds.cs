using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="ExpeditionStatus"/>.
/// Architecture Hardening A7. Retreated was added in schema v18.
/// </summary>
internal static class ExpeditionStatusSaveIds
{
    public const string ActiveId = "Active";
    public const string ReturnedId = "Returned";
    public const string FailedId = "Failed";
    public const string CancelledId = "Cancelled";
    public const string RetreatedId = "Retreated";

    public static string ToId(ExpeditionStatus value) => value switch
    {
        ExpeditionStatus.Active => ActiveId,
        ExpeditionStatus.Returned => ReturnedId,
        ExpeditionStatus.Failed => FailedId,
        ExpeditionStatus.Cancelled => CancelledId,
        ExpeditionStatus.Retreated => RetreatedId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out ExpeditionStatus value)
    {
        switch (id)
        {
            case ActiveId: value = ExpeditionStatus.Active; return true;
            case ReturnedId: value = ExpeditionStatus.Returned; return true;
            case FailedId: value = ExpeditionStatus.Failed; return true;
            case CancelledId: value = ExpeditionStatus.Cancelled; return true;
            case RetreatedId: value = ExpeditionStatus.Retreated; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
