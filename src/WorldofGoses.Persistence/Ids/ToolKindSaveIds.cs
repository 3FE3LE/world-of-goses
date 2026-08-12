using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="ToolKind"/>.
/// Architecture Hardening A7. The single durable city tool today is
/// the Primitive Axe (schema v28). Future tools slot in here.
/// </summary>
internal static class ToolKindSaveIds
{
    public const string PrimitiveAxeId = "PrimitiveAxe";

    public static string ToId(ToolKind value) => value switch
    {
        ToolKind.PrimitiveAxe => PrimitiveAxeId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out ToolKind value)
    {
        switch (id)
        {
            case PrimitiveAxeId: value = ToolKind.PrimitiveAxe; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
