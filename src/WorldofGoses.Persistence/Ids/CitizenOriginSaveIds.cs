using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="CitizenOrigin"/>.
/// Architecture Hardening A7. Mortal and AstralFounder are the two
/// origins introduced in schema v15.
/// </summary>
internal static class CitizenOriginSaveIds
{
    public const string MortalId = "Mortal";
    public const string AstralFounderId = "AstralFounder";

    public static string ToId(CitizenOrigin value) => value switch
    {
        CitizenOrigin.Mortal => MortalId,
        CitizenOrigin.AstralFounder => AstralFounderId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out CitizenOrigin value)
    {
        switch (id)
        {
            case MortalId: value = CitizenOrigin.Mortal; return true;
            case AstralFounderId: value = CitizenOrigin.AstralFounder; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
