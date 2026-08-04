#nullable enable
namespace WorldofGoses.Domain;

/// <summary>
/// Provisional zoning profiles. Final solid bounds will come from authored
/// building metadata without changing parcel or navigation semantics.
/// </summary>
public static class BuildingFootprintCatalog
{
    public const string StandardWithSideSetbacksId = "standard-side-setbacks";
    public const string StandardFullWidthId = "standard-full-width";
    private static readonly HalfTileRect StandardReservedArea =
        new(0, 0, ParcelGrid.HalfTilesPerStandardLot, ParcelGrid.HalfTilesPerStandardLot);

    public static ObstacleFootprintTemplate StandardWithSideSetbacks { get; } =
        new(
            StandardWithSideSetbacksId,
            StandardReservedArea,
            // 0.5 tile on both sides, 1 tile of frontal access.
            new HalfTileRect(1, 0, 4, 4));

    public static ObstacleFootprintTemplate StandardFullWidth { get; } =
        new(
            StandardFullWidthId,
            StandardReservedArea,
            // Full width still preserves a 1-tile frontal access strip.
            new HalfTileRect(0, 0, 6, 4));

    public static string ProfileIdFor(ConstructionKind kind) =>
        kind is ConstructionKind.Quarry or ConstructionKind.FoundingSite
            ? StandardFullWidthId
            : StandardWithSideSetbacksId;

    public static string ProfileIdFor(BuildingKind kind) =>
        kind == BuildingKind.Quarry
            ? StandardFullWidthId
            : StandardWithSideSetbacksId;

    public static ObstacleFootprintTemplate Get(string? profileId) =>
        profileId == StandardFullWidthId
            ? StandardFullWidth
            : StandardWithSideSetbacks;
}
