#nullable enable
namespace WorldofGoses.Domain;

/// <summary>
/// Authored navigation geometry for one natural-resource unit. Resource art
/// may fill its visual canvas, but only the solid area blocks traversal.
/// </summary>
public static class NaturalResourceFootprintCatalog
{
    public const string StandardGroundResourceId = "standard-ground-resource";

    public static ObstacleFootprintTemplate StandardGroundResource { get; } =
        new(
            StandardGroundResourceId,
            new HalfTileRect(
                0,
                0,
                ParcelGrid.HalfTilesPerTile,
                ParcelGrid.HalfTilesPerStandardLot),
            // The asset reserves one frontage cell but only its left half is a
            // solid obstacle. Adjacent resources therefore leave a real
            // half-tile passage, while the frontal clearance keeps the street
            // side open so citizens render and travel in front of the asset.
            // This is the generic obstacle contract, not a tree exception.
            new HalfTileRect(0, 0, 1, 4));

    public static ObstacleFootprintTemplate Get(ResourceType resourceType) =>
        StandardGroundResource;
}
