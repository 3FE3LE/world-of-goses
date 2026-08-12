using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="FoundingSiteModule"/>.
/// Architecture Hardening A7.
/// </summary>
internal static class FoundingSiteModuleSaveIds
{
    public const string CampfireId = "Campfire";
    public const string BedrollId = "Bedroll";
    public const string CacheId = "Cache";
    public const string CanopyId = "Canopy";

    public static string ToId(FoundingSiteModule value) => value switch
    {
        FoundingSiteModule.Campfire => CampfireId,
        FoundingSiteModule.Bedroll => BedrollId,
        FoundingSiteModule.Cache => CacheId,
        FoundingSiteModule.Canopy => CanopyId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out FoundingSiteModule value)
    {
        switch (id)
        {
            case CampfireId: value = FoundingSiteModule.Campfire; return true;
            case BedrollId: value = FoundingSiteModule.Bedroll; return true;
            case CacheId: value = FoundingSiteModule.Cache; return true;
            case CanopyId: value = FoundingSiteModule.Canopy; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
