using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="ConstructionKind"/>.
/// Architecture Hardening A7. FoundingSite was introduced in v22,
/// CultivationSite in v24.
/// </summary>
internal static class ConstructionKindSaveIds
{
    public const string BasicShelterId = "BasicShelter";
    public const string FarmId = "Farm";
    public const string QuarryId = "Quarry";
    public const string TownHallId = "TownHall";
    public const string FoundingSiteId = "FoundingSite";
    public const string CultivationSiteId = "CultivationSite";

    public static string ToId(ConstructionKind value) => value switch
    {
        ConstructionKind.BasicShelter => BasicShelterId,
        ConstructionKind.Farm => FarmId,
        ConstructionKind.Quarry => QuarryId,
        ConstructionKind.TownHall => TownHallId,
        ConstructionKind.FoundingSite => FoundingSiteId,
        ConstructionKind.CultivationSite => CultivationSiteId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out ConstructionKind value)
    {
        switch (id)
        {
            case BasicShelterId: value = ConstructionKind.BasicShelter; return true;
            case FarmId: value = ConstructionKind.Farm; return true;
            case QuarryId: value = ConstructionKind.Quarry; return true;
            case TownHallId: value = ConstructionKind.TownHall; return true;
            case FoundingSiteId: value = ConstructionKind.FoundingSite; return true;
            case CultivationSiteId: value = ConstructionKind.CultivationSite; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
