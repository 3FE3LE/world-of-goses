#nullable enable
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Read-only projection of one <see cref="CultivationSite"/>'s lifecycle
/// state. Closes the A0 boundary by replacing the previous direct
/// <c>_controller.World.GetCultivationSite(...)</c> calls in Presentation,
/// which handed the live mutable entity to the view layer.
/// </summary>
public sealed record CultivationSiteSnapshot(
    BuildingId Id,
    CultivationPlotState State,
    int? PlantedTick,
    int? ReadyAtTick)
{
    public static CultivationSiteSnapshot? From(CityWorld world, BuildingId siteId)
    {
        CultivationSite? site = world.GetCultivationSite(siteId);
        return site is null
            ? null
            : new CultivationSiteSnapshot(
                site.Id,
                site.State,
                site.PlantedTick,
                site.ReadyAtTick);
    }
}