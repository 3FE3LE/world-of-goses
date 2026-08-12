using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="GenderId"/>.
/// Architecture Hardening A7. Schema v4 introduced explicit gender.
/// </summary>
internal static class GenderIdSaveIds
{
    public const string FeminineId = "Feminine";
    public const string MasculineId = "Masculine";

    public static string ToId(GenderId value) => value switch
    {
        GenderId.Feminine => FeminineId,
        GenderId.Masculine => MasculineId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out GenderId value)
    {
        switch (id)
        {
            case FeminineId: value = GenderId.Feminine; return true;
            case MasculineId: value = GenderId.Masculine; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
