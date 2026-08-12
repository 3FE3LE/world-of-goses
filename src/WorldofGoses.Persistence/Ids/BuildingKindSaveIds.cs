using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="BuildingKind"/>.
/// Architecture Hardening A7: persisting via <c>Enum.ToString()</c>
/// would silently change every save if a future slice renamed
/// <c>BuildingKind.Quarry</c> to <c>BuildingKind.QuarriedStone</c>.
/// Every Capture / Parse site for the persisted kind goes through
/// this mapper.
///
/// <para>Wire IDs match the historical <c>Enum.ToString()</c>
/// output. Schema v3 introduced Farm, Quarry, Smithy, PotionLab,
/// Home, Forest; v14 retired the Forest building entity; v22 added
/// CultivationSite and TownHall.</para>
/// </summary>
internal static class BuildingKindSaveIds
{
    public const string QuarryId = "Quarry";
    public const string FarmId = "Farm";
    public const string SmithyId = "Smithy";
    public const string PotionLabId = "PotionLab";
    public const string HomeId = "Home";
    public const string ForestId = "Forest";
    public const string TownHallId = "TownHall";
    public const string CultivationSiteId = "CultivationSite";

    public static string ToId(BuildingKind value) => value switch
    {
        BuildingKind.Quarry => QuarryId,
        BuildingKind.Farm => FarmId,
        BuildingKind.Smithy => SmithyId,
        BuildingKind.PotionLab => PotionLabId,
        BuildingKind.Home => HomeId,
        BuildingKind.Forest => ForestId,
        BuildingKind.TownHall => TownHallId,
        BuildingKind.CultivationSite => CultivationSiteId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out BuildingKind value)
    {
        switch (id)
        {
            case QuarryId: value = BuildingKind.Quarry; return true;
            case FarmId: value = BuildingKind.Farm; return true;
            case SmithyId: value = BuildingKind.Smithy; return true;
            case PotionLabId: value = BuildingKind.PotionLab; return true;
            case HomeId: value = BuildingKind.Home; return true;
            case ForestId: value = BuildingKind.Forest; return true;
            case TownHallId: value = BuildingKind.TownHall; return true;
            case CultivationSiteId: value = BuildingKind.CultivationSite; return true;
            default:
                // Fallback for unknown / legacy IDs: try a tolerant parse
                // so pre-v8 saves that used the raw enum name still load.
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
