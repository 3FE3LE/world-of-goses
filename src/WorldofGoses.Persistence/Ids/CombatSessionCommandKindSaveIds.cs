using System;
using WorldofGoses.Domain.Combat;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="CombatSessionCommandKind"/>.
/// Architecture Hardening A7. Schema v33 introduced persistent
/// combat commands.
/// </summary>
internal static class CombatSessionCommandKindSaveIds
{
    public static string ToId(CombatSessionCommandKind value) => value.ToString();

    public static bool TryParse(string id, out CombatSessionCommandKind value)
    {
        return Enum.TryParse(id, ignoreCase: true, out value);
    }
}
