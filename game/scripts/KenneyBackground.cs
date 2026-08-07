#nullable enable
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Orthogonal top-down tiled floor for the detail views.
///
/// <para>
/// This used to be a <see cref="TextureRect"/> in
/// <see cref="TextureRect.StretchModeEnum.Tile"/> mode over a 9-slice panel
/// PNG, tinted by the lineage accent: a repeated wall texture, with no grid
/// and no relationship to the world's terrain. A building interior is a place,
/// so it now draws real terrain tiles from the same Kenney atlas the macro
/// view samples, laid out orthogonally.
/// </para>
///
/// <para>
/// The macro view's <c>DrawTiledFloor</c> is the precedent, but it projects
/// each row into a trapezoid to fake perspective. Nothing here does: a detail
/// view is seen straight down, so tiles are axis-aligned and land on integer
/// pixels. Scale is a whole multiple — 16 px source at ×4 is exactly the
/// project's 64 px <c>BaseUnit</c> — because the bible allows integer scale
/// only.
/// </para>
///
/// <para>
/// The lineage <c>Modulate</c> is deliberately gone. Multiplying real terrain
/// by an accent colour muddies it, which is why the brown-floor iteration was
/// rejected during the original terrain pass.
/// </para>
/// </summary>
public partial class KenneyBackground : Control
{
    /// <summary>Integer magnification: 16 px source → 64 px, the project's terrain unit.</summary>
    public const int TileScale = 4;

    /// <summary>Rendered size of one floor tile.</summary>
    public const int TilePixels = TerrainAtlas.TileSize * TileScale;

    /// <summary>Atlas column of the floor tile. Wood planking by default.</summary>
    [Export] public int AtlasColumn { get; set; } = 5;

    /// <summary>Atlas row of the floor tile.</summary>
    [Export] public int AtlasRow { get; set; } = 4;

    /// <summary>
    /// Every Nth tile is swapped for the variant one row down, so a large
    /// floor does not read as a single stamped texture. Deterministic on the
    /// cell index — a floor must not shimmer between redraws.
    /// </summary>
    [Export] public int VariantEvery { get; set; } = 7;

    private Texture2D? _atlas;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _atlas = ResourceLoader.Load<Texture2D>(TerrainAtlas.AtlasPath);
        if (_atlas is null)
        {
            GD.PushWarning($"KenneyBackground: terrain atlas missing at '{TerrainAtlas.AtlasPath}'.");
        }
        Resized += QueueRedraw;
    }

    public override void _ExitTree()
    {
        Resized -= QueueRedraw;
    }

    public override void _Draw()
    {
        if (_atlas is null) return;

        Rect2 plain = TerrainAtlas.Region(AtlasColumn, AtlasRow);
        Rect2 variant = TerrainAtlas.Region(AtlasColumn, AtlasRow + 1);

        int columns = Mathf.CeilToInt(Size.X / TilePixels);
        int rows = Mathf.CeilToInt(Size.Y / TilePixels);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                bool isVariant = VariantEvery > 0
                    && (column * 3 + row * 5) % VariantEvery == 0;
                DrawTextureRectRegion(
                    _atlas,
                    new Rect2(column * TilePixels, row * TilePixels, TilePixels, TilePixels),
                    isVariant ? variant : plain);
            }
        }
    }
}
