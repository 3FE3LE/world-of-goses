using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="WorldEventSubjectKind"/>.
/// Architecture Hardening A7.
/// </summary>
internal static class WorldEventSubjectKindSaveIds
{
    public static string ToId(WorldEventSubjectKind value) => value.ToString();

    public static bool TryParse(string id, out WorldEventSubjectKind value)
    {
        return Enum.TryParse(id, ignoreCase: true, out value);
    }
}
