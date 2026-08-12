using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="ExpeditionRetreatPosture"/>.
/// Architecture Hardening A7.
/// </summary>
internal static class ExpeditionRetreatPostureSaveIds
{
    public const string ContinueAfterSetbackId = "ContinueAfterSetback";
    public const string RetreatAfterSetbackId = "RetreatAfterSetback";

    public static string ToId(ExpeditionRetreatPosture value) => value switch
    {
        ExpeditionRetreatPosture.ContinueAfterSetback => ContinueAfterSetbackId,
        ExpeditionRetreatPosture.RetreatAfterSetback => RetreatAfterSetbackId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out ExpeditionRetreatPosture value)
    {
        switch (id)
        {
            case ContinueAfterSetbackId: value = ExpeditionRetreatPosture.ContinueAfterSetback; return true;
            case RetreatAfterSetbackId: value = ExpeditionRetreatPosture.RetreatAfterSetback; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
