#nullable enable
using Godot;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Prototype-only elevation test: two ground levels connected by a single
/// ramp gap, built procedurally (same pattern as
/// <c>OrthogonalParcelTerrain.BuildGroundTileSet</c>) so no art assets are
/// required to validate the mechanic. Attached to a <c>YSortEnabled</c>
/// <see cref="Node2D"/> whose sibling children — the raised layer and the
/// avatar authored in the scene — are what actually gets sorted. See
/// docs/world-of-goses-design-bible/08_VISUAL_UI_AND_ASSET_GUIDELINES.md,
/// "Profundidad y desniveles".
/// </summary>
public partial class ElevationTestLayout : Node2D
{
    private const int SourceTileSize = 16;
    private const int DisplayTileSize = 32;
    private const int WorldTileColumns = 20;
    private const int WorldTileRows = 14;
    private const int ElevationOffsetPx = 16;
    private const float WallThicknessPx = 4f;

    private const int RaisedMinCol = 8;
    private const int RaisedMaxColExclusive = 14;
    private const int RaisedMinRow = 3;
    private const int RaisedMaxRowExclusive = 9;
    private const int RampColumn = 10;

    private static readonly Vector2I BaseTile = new(0, 0);
    private static readonly Vector2I RaisedTile = new(1, 0);
    private static readonly Vector2I RampTile = new(2, 0);

    public override void _Ready()
    {
        TileSet tileSet = BuildTileSet();
        AddChild(BuildBaseLayer(tileSet));
        AddChild(BuildRaisedLayer(tileSet));
        BuildWalls();
    }

    private static TileSet BuildTileSet()
    {
        Image image = Image.CreateEmpty(
            SourceTileSize * 3,
            SourceTileSize,
            useMipmaps: false,
            Image.Format.Rgba8);
        image.FillRect(new Rect2I(0, 0, SourceTileSize, SourceTileSize), new Color("#385a3d"));
        image.FillRect(new Rect2I(SourceTileSize, 0, SourceTileSize, SourceTileSize), new Color("#7a6a4f"));
        image.FillRect(new Rect2I(SourceTileSize * 2, 0, SourceTileSize, SourceTileSize), new Color("#c9b27a"));
        var atlas = new TileSetAtlasSource
        {
            Texture = ImageTexture.CreateFromImage(image),
            TextureRegionSize = new Vector2I(SourceTileSize, SourceTileSize),
        };
        atlas.CreateTile(BaseTile);
        atlas.CreateTile(RaisedTile);
        atlas.CreateTile(RampTile);
        var tileSet = new TileSet { TileSize = new Vector2I(SourceTileSize, SourceTileSize) };
        tileSet.AddSource(atlas, 0);
        return tileSet;
    }

    private static TileMapLayer BuildBaseLayer(TileSet tileSet)
    {
        var layer = new TileMapLayer { Name = "BaseGroundLayer", TileSet = tileSet };
        float scale = DisplayTileSize / (float)SourceTileSize;
        layer.Scale = new Vector2(scale, scale);
        for (int row = 0; row < WorldTileRows; row++)
        {
            for (int column = 0; column < WorldTileColumns; column++)
            {
                layer.SetCell(new Vector2I(column, row), 0, BaseTile);
            }
        }
        return layer;
    }

    /// <summary>
    /// The raised platform's cells share the base grid, offset visually by
    /// <see cref="ElevationOffsetPx"/> — the "desnivel" is a rendering
    /// illusion, not a change in the 2D collision world. <c>YSortEnabled</c>
    /// on this layer lets its individual tiles sort against the avatar
    /// (its parent's y-sort would otherwise treat the whole layer as one
    /// atomic unit).
    /// </summary>
    private static TileMapLayer BuildRaisedLayer(TileSet tileSet)
    {
        int width = RaisedMaxColExclusive - RaisedMinCol;
        int height = RaisedMaxRowExclusive - RaisedMinRow;
        var layer = new TileMapLayer
        {
            Name = "RaisedGroundLayer",
            TileSet = tileSet,
            YSortEnabled = true,
        };
        float scale = DisplayTileSize / (float)SourceTileSize;
        layer.Scale = new Vector2(scale, scale);
        layer.Position = new Vector2(
            RaisedMinCol * DisplayTileSize,
            RaisedMinRow * DisplayTileSize - ElevationOffsetPx);

        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                bool isRampCell = row == height - 1 && RaisedMinCol + column == RampColumn;
                layer.SetCell(new Vector2I(column, row), 0, isRampCell ? RampTile : RaisedTile);
            }
        }
        return layer;
    }

    /// <summary>
    /// Thin static colliders around the raised platform's perimeter, with a
    /// one-tile gap at <see cref="RampColumn"/> on the south edge — the
    /// only way to cross between levels, proving desniveles block movement
    /// except through an authored ramp.
    /// </summary>
    private void BuildWalls()
    {
        AddPerimeterEdge(isHorizontal: true, coord: RaisedMinRow, start: RaisedMinCol, end: RaisedMaxColExclusive, skip: null);
        AddPerimeterEdge(isHorizontal: true, coord: RaisedMaxRowExclusive, start: RaisedMinCol, end: RaisedMaxColExclusive, skip: RampColumn);
        AddPerimeterEdge(isHorizontal: false, coord: RaisedMinCol, start: RaisedMinRow, end: RaisedMaxRowExclusive, skip: null);
        AddPerimeterEdge(isHorizontal: false, coord: RaisedMaxColExclusive, start: RaisedMinRow, end: RaisedMaxRowExclusive, skip: null);
    }

    private void AddPerimeterEdge(bool isHorizontal, int coord, int start, int end, int? skip)
    {
        for (int i = start; i < end; i++)
        {
            if (skip.HasValue && i == skip.Value) continue;
            var body = new StaticBody2D
            {
                Position = isHorizontal
                    ? new Vector2((i + 0.5f) * DisplayTileSize, coord * DisplayTileSize)
                    : new Vector2(coord * DisplayTileSize, (i + 0.5f) * DisplayTileSize),
            };
            body.AddChild(new CollisionShape2D
            {
                Shape = new RectangleShape2D
                {
                    Size = isHorizontal
                        ? new Vector2(DisplayTileSize, WallThicknessPx)
                        : new Vector2(WallThicknessPx, DisplayTileSize),
                },
            });
            AddChild(body);
        }
    }
}
