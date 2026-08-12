using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="WeaponFamily"/>.
/// Architecture Hardening A7. Schema v30 persisted weapon-family
/// competencies; v32 reserved the mastery tier naming.
/// </summary>
internal static class WeaponFamilySaveIds
{
    public static string ToId(WeaponFamily value) => value.ToString();

    public static bool TryParse(string id, out WeaponFamily value)
    {
        return Enum.TryParse(id, ignoreCase: true, out value);
    }
}
