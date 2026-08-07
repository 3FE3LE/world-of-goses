#nullable enable
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Ui;

/// <summary>
/// Single source of truth for the Kenney roguelike terrain atlas and the tile
/// coordinates the world draws from it.
///
/// <para>
/// The coordinates used to live in three places — <c>ResourceTree</c> (with
/// the row and column re-typed as literals directly under the named constants
/// that already held them), <c>MacroStreetLiveView.DrawNaturalResourceUnit</c>
/// and <c>MacroStreetLiveView.SelectTree</c> — each re-deriving the same
/// parity hash. `docs/VISUAL_REGRESSION.md` records that two coordinates once
/// slipped and rendered water tiles instead of trees; three copies of the
/// arithmetic is how that happens.
/// </para>
///
/// <para>
/// Every coordinate below was read off a labelled contact sheet of the atlas,
/// not inferred. Changing one requires a fresh capture: the sheet has water,
/// furniture and grave markers within a few tiles of the nature block.
/// </para>
/// </summary>
public static class TerrainAtlas
{
    public const string AtlasPath =
        "res://assets/terrain/kenney/roguelike-rpg/roguelike_sheet_transparent.png";

    /// <summary>Source tile size. The sheet packs 16 px tiles with a 1 px gutter.</summary>
    public const int TileSize = 16;

    /// <summary>Tile pitch: <see cref="TileSize"/> plus the 1 px gutter.</summary>
    public const int Stride = 17;

    // Broadleaf trees, complete in one tile: green / autumn.
    public const int TreeColumnA = 13;
    public const int TreeColumnB = 14;
    public const int TreeRow = 9;

    /// <summary>Region of one tile, in atlas pixels.</summary>
    public static Rect2 Region(int column, int row) =>
        new(column * Stride, row * Stride, TileSize, TileSize);

    // The same trees in their two-tile form: canopy on row 10, trunk on row
    // 11. A tree drawn at the same 16×16 footprint as a berry bush reads as
    // the same plant at the same size, which is what the single-tile pair
    // above produced in the macro view.
    public const int TallTreeCanopyRow = 10;
    public const int TallTreeTrunkRow = 11;

    /// <summary>
    /// Deterministic tree variant. Keyed on the unit's identity rather than a
    /// random draw so a tree keeps its silhouette across saves and redraws.
    /// </summary>
    public static Rect2 TreeRegion(int forestId, int unitId) =>
        Region(TreeColumn(forestId, unitId), TreeRow);

    /// <summary>
    /// Which tree this unit is. Keyed on the unit's identity, so a given tree
    /// keeps its species and colour across saves and redraws; keyed on the
    /// biome, so a cactus grows in the sand and a fruiting broadleaf does not.
    /// </summary>
    public static TreeVariant TreeFor(GroundBiome biome, int forestId, int unitId) =>
        biome.Trees[VariantIndex(forestId, unitId, biome.Trees.Length)];

    private static int TreeColumn(int forestId, int unitId) =>
        (forestId + unitId) % 2 == 0 ? TreeColumnA : TreeColumnB;

    /// <summary>
    /// The sprite for a gatherable ground resource. These used to be flat
    /// <c>DrawRect</c> markers in four hard-coded colours — the reason a
    /// branch pile, a fibre tuft and a stone all read as the same coloured
    /// square at macro distance.
    /// </summary>
    /// <summary>
    /// Sheet width in tiles. The pack numbers its tiles row-major from zero,
    /// so a linear id maps to <c>(id % Columns, id / Columns)</c>. Tiled's own
    /// GIDs are that index plus one — the two readings sit a column apart, so
    /// any id taken from a map editor has to be checked against the sprite it
    /// actually resolves to before it is trusted.
    /// </summary>
    public const int Columns = 57;

    /// <summary>Region of the tile with the pack's linear id.</summary>
    public static Rect2 RegionOfId(int tileId) => Region(tileId % Columns, tileId / Columns);

    /// <summary>
    /// The ground palette of a city site: a handful of interchangeable fill
    /// tiles plus the tile a trodden path wears down to.
    ///
    /// <para>
    /// Every id here is a **seam-free fill**, verified by rendering it. Most of
    /// the sheet's coloured bands are autotile sets whose tiles carry a corner
    /// or edge of the neighbouring material — including the flower-strewn
    /// grass — so repeating one across a band shows the cut. That is why this
    /// list is shorter than the palette looks.
    /// </para>
    /// </summary>
    /// <summary>
    /// One kind of tree. Most are two tiles tall — canopy above trunk, drawn as
    /// separate regions because the sheet keeps a transparent gutter between
    /// rows and spanning it would cut a line across the trunk. A cactus is one
    /// tile, so <paramref name="CanopyId"/> is <c>-1</c> for it.
    /// </summary>
    public sealed record TreeVariant(int TrunkId, int CanopyId = -1)
    {
        public bool IsTall => CanopyId >= 0;
    }

    // Two-tile trees: canopy on row 10, trunk on row 11.
    private static TreeVariant Tall(int column) =>
        new(TrunkId: 11 * Columns + column, CanopyId: 10 * Columns + column);

    // Verified against a rendered sheet: cols 13/14/15 broadleaf in green,
    // autumn and teal; 16/17/18 the same three as conifers; 23 a fruiting
    // broadleaf; 27 a bare dead tree; and (22,9) a single-tile cactus.
    private static readonly TreeVariant BroadleafGreen = Tall(13);
    private static readonly TreeVariant BroadleafAutumn = Tall(14);
    private static readonly TreeVariant BroadleafTeal = Tall(15);
    private static readonly TreeVariant ConiferGreen = Tall(16);
    private static readonly TreeVariant ConiferAutumn = Tall(17);
    private static readonly TreeVariant ConiferTeal = Tall(18);
    private static readonly TreeVariant Fruiting = Tall(23);
    private static readonly TreeVariant DeadTree = new(TrunkId: 597, CanopyId: 540);
    private static readonly TreeVariant Cactus = new(TrunkId: 535);

    public sealed record GroundBiome(int[] Fill, int Path, TreeVariant[] Trees);

    /// <summary>
    /// Which fill variant a ground tile uses. A spatial hash with large
    /// coprime multipliers, so the choice stays deterministic — the ground
    /// must not shimmer between redraws and carries no save state — without
    /// degenerating for any particular variant count.
    /// </summary>
    public static int VariantIndex(int column, int row, int variantCount)
    {
        if (variantCount <= 1) return 0;
        return SpatialHash(column, row) % variantCount;
    }

    /// <summary>
    /// Which fill a ground tile uses. Unlike <see cref="VariantIndex"/> this is
    /// deliberately lopsided: the first fill is the biome's material and the
    /// rest are occasional patches in it.
    ///
    /// <para>
    /// An even mix reads as a checkerboard rather than as ground — a city whose
    /// palette held two strongly different hues came out looking like tiling
    /// noise. Terrain wants one material with variation, not a fair draw.
    /// </para>
    /// </summary>
    public static int GroundVariantIndex(int column, int row, int variantCount)
    {
        if (variantCount <= 1) return 0;
        int hash = SpatialHash(column, row);
        if (hash % PatchRarity != 0) return 0;
        return 1 + (hash / PatchRarity) % (variantCount - 1);
    }

    /// <summary>One tile in this many carries a patch of a secondary fill.</summary>
    private const int PatchRarity = 7;

    private static int SpatialHash(int column, int row) =>
        ((column * 73856093) ^ (row * 19349663)) & int.MaxValue;

    // Verified seam-free fills, by family:
    //   green 5, 62, 66 · brown 6, 63 · grey 7, 64, 9 · cream 8, 65
    //   orange 1086
    //
    // Two different things in this sheet look like a fill and are not. The
    // solid swatch block (cols 5-9, rows 0-1) really is flat. The coloured
    // *patch* blocks are 5×3 autotiles: columns 0-1 hold the four inner
    // corners, each carrying a curved notch of the material underneath, and
    // columns 2-4 hold a 3×3 rounded blob. Only the blob's **centre** tiles —
    // (3, r+1) of each block — are seam-free. Orange 1026 was an inner corner
    // and its notch showed as a bite out of every tile; its real fill is 1086.
    // The same trap took magenta 1197 and teal 466 out of these lists.
    private static readonly GroundBiome Meadow = new(
        new[] { 5, 62, 66 }, 6,
        new[] { BroadleafGreen, ConiferGreen, Fruiting });
    private static readonly GroundBiome Stone = new(
        new[] { 7, 64, 9 }, 6,
        new[] { ConiferTeal, DeadTree, BroadleafTeal });
    private static readonly GroundBiome Arid = new(
        new[] { 1086, 6, 63 }, 63,
        new[] { Cactus, DeadTree, BroadleafAutumn });
    private static readonly GroundBiome Sand = new(
        new[] { 8, 65 }, 6,
        new[] { Cactus, DeadTree, ConiferAutumn });
    private static readonly GroundBiome Loam = new(
        new[] { 6, 63, 5 }, 63,
        new[] { BroadleafAutumn, ConiferAutumn, Fruiting });
    private static readonly GroundBiome Ashen = new(
        new[] { 64, 9, 8 }, 6,
        new[] { ConiferTeal, DeadTree, ConiferGreen });
    // Riverside meadow and mossy plateau. Both are green-dominant with a
    // different accent, because the magenta and teal fills they used before
    // are decorative tiles, not credible ground: a city floored in teal read
    // as built on open water, and magenta as nowhere at all. Distinctness
    // comes from the accent and the tree set instead of an exotic hue.
    private static readonly GroundBiome Shallows = new(
        new[] { 62, 5, 8 }, 6,
        new[] { BroadleafTeal, ConiferTeal, BroadleafGreen });
    private static readonly GroundBiome Bloom = new(
        new[] { 66, 5, 7 }, 6,
        new[] { BroadleafTeal, Fruiting, ConiferGreen });

    /// <summary>
    /// The site the founder's fall deposited the city on, keyed by lineage.
    ///
    /// <para>
    /// This is <b>presentation only</b>: no resource, yield or rule differs by
    /// biome, so a lineage still confers no advantage — see <c>DEC-0002</c> and
    /// the standing rule in the macro view that terrain art must never become
    /// simulation state.
    /// </para>
    /// </summary>
    public static GroundBiome BiomeFor(LineageId lineage) => lineage.Value.ToLowerInvariant() switch
    {
        "ardhen" => Stone,
        "eirune" => Meadow,
        "kovari" => Arid,
        "myrven" => Bloom,
        "vaelun" => Sand,
        "orveth" => Loam,
        "caelith" => Ashen,
        "theryn" => Shallows,
        _ => Meadow,
    };

    // Ids verified against the rendered sprite, not inferred: 654 is the bare
    // branching deadwood, 537 the low berry bush, and 1251-1253 three loose
    // rocks. The earlier picks were wrong in both directions — a brush clump
    // that read as a flower, and rubble that read as broken masonry.
    private const int BranchesTileId = 654;
    private const int WildFoodTileId = 537;
    // Six loose rocks: 1251-1253 bare, 1308-1310 the same three with moss.
    private static readonly int[] SmallStoneTileIds = { 1251, 1252, 1253, 1308, 1309, 1310 };

    public static Rect2 ResourceRegion(ResourceType resource, int forestId = 0, int unitId = 0) =>
        resource switch
        {
            ResourceType.Branches => RegionOfId(BranchesTileId),
            // Low green sprout; the pack has no dedicated fibre tile.
            ResourceType.PlantFiber => Region(22, 10),
            // Three rocks, chosen from the unit's identity so a given stone
            // keeps its shape across saves and redraws.
            ResourceType.SmallStone => RegionOfId(
                SmallStoneTileIds[Mathf.Abs(forestId + unitId) % SmallStoneTileIds.Length]),
            ResourceType.WildFood => RegionOfId(WildFoodTileId),
            // Falls back to the tree so an unmapped resource is visibly wrong
            // rather than invisible.
            _ => Region(TreeColumnA, TreeRow),
        };
}
