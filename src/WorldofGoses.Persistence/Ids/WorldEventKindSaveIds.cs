using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="WorldEventKind"/>.
/// Architecture Hardening A7. WorldEventKind is the causal log's
/// discriminator and is consumed by retention, chronicle and metric
/// capture. Renaming in C# must NOT silently change the persisted
/// string.
/// </summary>
internal static class WorldEventKindSaveIds
{
    public const string ExpeditionDispatchedId = "ExpeditionDispatched";

    public static string ToId(WorldEventKind value) => value.ToString();

    public static bool TryParse(string id, out WorldEventKind value)
    {
        return Enum.TryParse(id, ignoreCase: true, out value);
    }
}
