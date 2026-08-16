#nullable enable

using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Everything the macro floor needs to know about one biome's ground sheet:
/// the image, the grid it is cut on, and which tiles play which role.
/// </summary>
/// <remarks>
/// <para>
/// This exists so that changing a biome's tileset is editing a resource rather
/// than editing drawing code. Before it, <c>TerrainAtlas</c> held a single
/// <c>const string AtlasPath</c>, a single <c>TileSize</c>/<c>Stride</c>/
/// <c>Columns</c> triple, and raw integer ids per biome — so a second sheet,
/// cut on a different grid, could not exist at all.
/// </para>
/// <para>
/// The atlas is an exported <see cref="Texture2D"/> and not a path string on
/// purpose: Godot tracks the dependency by uid, so renaming or moving the PNG
/// keeps working. A path in a constant silently resolves to nothing.
/// </para>
/// <para>
/// <b>Ground only.</b> Trees and loose props still come from the shared Kenney
/// placeholder sheet through <c>TerrainAtlas</c>. They are a separate authoring
/// job on a separate sheet, and folding them in here before they are drawn
/// would invent a contract for art that does not exist yet.
/// </para>
/// </remarks>
[GlobalClass]
public partial class GroundAtlasProfile : Resource
{
    /// <summary>The sheet these tile ids index into.</summary>
    [Export] public Texture2D? Atlas { get; set; }

    /// <summary>Edge of one tile, in atlas pixels.</summary>
    [Export] public int TileSize { get; set; } = 32;

    /// <summary>
    /// Transparent gutter between tiles, in pixels. Zero for anything authored
    /// for this project; the Kenney placeholder sheet packs a 1 px gutter, which
    /// is the only reason this field exists.
    /// </summary>
    [Export] public int Separation { get; set; }

    /// <summary>
    /// Tiles per row. <b>Part of the sheet's identity.</b> Ids are linear, so
    /// changing this renumbers every tile in the sheet at once — which is why
    /// it is declared per sheet instead of being a shared constant.
    /// </summary>
    [Export] public int Columns { get; set; } = 10;

    /// <summary>
    /// Seam-free fill tiles. The first is the biome's material and the rest are
    /// occasional patches in it; see <c>TerrainAtlas.GroundVariantIndex</c> for
    /// why the draw is deliberately lopsided rather than an even mix.
    /// </summary>
    [Export] public int[] Fill { get; set; } = System.Array.Empty<int>();

    /// <summary>The tile a trodden path wears down to.</summary>
    [Export] public int Path { get; set; }

    /// <summary>Tile pitch: <see cref="TileSize"/> plus the gutter.</summary>
    public int Stride => TileSize + Separation;

    /// <summary>Region of the tile with this linear id, in atlas pixels.</summary>
    public Rect2 RegionOfId(int tileId) => new(
        (tileId % Columns) * Stride,
        (tileId / Columns) * Stride,
        TileSize,
        TileSize);
}
