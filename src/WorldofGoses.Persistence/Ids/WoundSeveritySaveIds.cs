using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="WoundSeverity"/>.
/// Architecture Hardening A7. Schema v19 introduced durable wounds.
/// </summary>
internal static class WoundSeveritySaveIds
{
    public const string ModerateId = "Moderate";
    public const string SevereId = "Severe";

    public static string ToId(WoundSeverity value) => value switch
    {
        WoundSeverity.Moderate => ModerateId,
        WoundSeverity.Severe => SevereId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out WoundSeverity value)
    {
        switch (id)
        {
            case ModerateId: value = WoundSeverity.Moderate; return true;
            case SevereId: value = WoundSeverity.Severe; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
