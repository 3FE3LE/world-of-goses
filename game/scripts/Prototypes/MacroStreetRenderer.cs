#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Owns the per-frame draw of the macro street city (A4). Holds the
/// projected <see cref="PlotBox"/> and <see cref="TreeBox"/> collections,
/// the building texture cache, the ground biome, the per-street obstacle
/// band layers, and the terrain wear grid. The view's <c>_Draw</c> body
/// delegates to <see cref="Draw"/>; the interaction controller reads
/// <see cref="Hits"/> in the next <c>_Process</c>.
///
/// Step 5 ships the records and the draw methods. The view keeps the
/// renderer state on its own (the renderer is a helper that takes the
/// view as a parameter) so the existing in-class usage of
/// <c>_plots</c>/<c>_trees</c> by other collaborators keeps compiling
/// until Steps 6–9 rotate them over.
/// </summary>
internal sealed class MacroStreetRenderer
{
    /// <summary>Plot box: a single building's projected screen rectangle plus
    /// the metadata the interaction controller needs to render selection,
    /// hover, and storage-full badges.</summary>
    public readonly record struct PlotBox(
        int Street,
        float LateralOffset,
        float Width,
        float Height,
        int BuildingId,
        string DisplayName,
        BuildingKind Kind,
        bool IsUnderConstruction,
        bool IsClickable,
        int Stock,
        int StorageCapacity,
        CultivationPlotState? CultivationState,
        int? ReadyAtTick)
    {
        public bool IsStorageFull => StorageCapacity > 0 && Stock >= StorageCapacity;
    }

    /// <summary>Tree box: a single natural-resource unit's projected position
    /// plus the metadata the interaction controller needs to render hover
    /// the gather menu.</summary>
    public readonly record struct TreeBox(
        int Street,
        float LateralOffset,
        int ForestId,
        int UnitId,
        ResourceType ResourceType,
        int Reserve);

    private readonly Dictionary<BuildingKind, Texture2D?> _buildingTextureCache = new();
    private readonly List<PlotBox> _plots = new();
    private readonly List<TreeBox> _trees = new();
    private readonly List<StreetBandLayer> _bandLayers = new();
    private readonly TerrainWearGrid _terrainWear = new();
    private readonly Dictionary<int, List<StreetRoutePlanner.Interval>> _bandOccupancy = new();
    private static readonly List<StreetRoutePlanner.Interval> EmptyBand = new();
    private readonly Dictionary<int, CityMacroSnapshot.CitizenItem> _citizenStates = new();
    private readonly Dictionary<(int Row, int Column), ParcelTerritoryState> _parcelTerritory = new();
    private TerrainAtlas.GroundBiome _groundBiome = TerrainAtlas.BiomeFor(LineageId.Ardhen);
    private GroundAtlasProfile? _groundProfile;
    private Texture2D? _terrainAtlas;
    private int _streetCount = 1;
    private float _lateralHalfWidthPx = MacroViewConstants.LotUnitPx;
    private int _worldParcelColumns = MacroViewConstants.DefaultWorldParcelColumns;
    private int _worldParcelRows = MacroViewConstants.DefaultWorldParcelRows;
    private Node2D? _host;

    /// <summary>Painter the renderer installs on every per-street
    /// <see cref="StreetBandLayer"/> it creates. The view's own
    /// <c>DrawStreetObstacles</c> reads the presenter's state and pushes
    /// obstacles into each layer's canvas.</summary>
    public Action<CanvasItem, int>? Painter { get; set; }

    /// <summary>Bound to the view's own host so the renderer can add/replace
    /// child nodes (the <see cref="StreetBandLayer"/> layers).</summary>
    public void Attach(Node2D host) => _host = host;

    /// <summary>Loads the shared terrain atlas once and caches it for the
    /// floor draw. Caller (the view's <c>_Ready</c>) supplies the path
    /// so the renderer never reaches into resource paths.</summary>
    public void LoadTerrainAtlas(string path) =>
        _terrainAtlas = GD.Load<Texture2D>(path);

    /// <summary>Brings the layer count in line with the requested street
    /// count. Schedules the rebuild for off-frame because the scene tree
    /// cannot be edited while drawing.</summary>
    public void RequestBandLayerRebuild(int streetCount)
    {
        if (_bandLayers.Count != streetCount)
        {
            Node2D host = _host ?? throw new InvalidOperationException(
                "Renderer not attached before band layer rebuild.");
            Callable.From(() => RebuildBandLayers(streetCount)).CallDeferred();
        }
    }

    /// <summary>Brings the layer count in line with the street count, off-frame.</summary>
    private void RebuildBandLayers(int streetCount)
    {
        Node2D host = _host ?? throw new InvalidOperationException(
            "Renderer not attached before band layer rebuild.");
        Action<CanvasItem, int>? painter = Painter;
        while (_bandLayers.Count < streetCount)
        {
            var layer = new StreetBandLayer
            {
                Name = $"StreetBand{_bandLayers.Count}",
                Painter = painter,
            };
            host.AddChild(layer);
            _bandLayers.Add(layer);
        }
        while (_bandLayers.Count > streetCount)
        {
            StreetBandLayer extra = _bandLayers[^1];
            _bandLayers.RemoveAt(_bandLayers.Count - 1);
            extra.QueueFree();
        }
    }

    /// <summary>Syncs the z-index and visibility of every existing layer
    /// against the current camera depth anchor. Layers that are already
    /// visible ask for a redraw; the layer count is left to
    /// <see cref="RequestBandLayerRebuild"/>.</summary>
    public void SyncBandLayers(int streetCount, float cameraDepthAnchor)
    {
        RequestBandLayerRebuild(streetCount);
        for (int street = 0; street < _bandLayers.Count; street++)
        {
            StreetBandLayer layer = _bandLayers[street];
            layer.Street = street;
            layer.Visible = MacroProjectionHelpers.IsProjectedDepthVisible(street - cameraDepthAnchor);
            layer.ZIndex = MacroProjectionHelpers.DepthToZ(street - cameraDepthAnchor, streetCount, cameraDepthAnchor);
            if (layer.Visible) layer.QueueRedraw();
        }
    }

    /// <summary>Disposes the current band stack. Called from the view's
    /// <c>_ExitTree</c> pass.</summary>
    public void Dispose()
    {
        foreach (StreetBandLayer layer in _bandLayers)
        {
            layer.QueueFree();
        }
        _bandLayers.Clear();
        _buildingTextureCache.Clear();
    }

    /// <summary>Cached building texture path. Loads once per <see cref="BuildingKind"/>
    /// and reuses the same instance across all renders. Smithy/PotionLab
    /// have no connected playable slice today, so <see cref="BuildingArt.GetTexturePath"/>
    /// returns null and the cache stores an explicit null to avoid
    /// re-querying the path on every redraw.</summary>
    public Texture2D? GetBuildingTexture(BuildingKind kind)
    {
        if (_buildingTextureCache.TryGetValue(kind, out Texture2D? cached)) return cached;
        string? path = BuildingArt.GetTexturePath(kind);
        Texture2D? texture = path is null ? null : GD.Load<Texture2D>(path);
        _buildingTextureCache[kind] = texture;
        return texture;
    }

    /// <summary>Session-scoped terrain wear grid (S-1.3 phase 2). The
    /// grid deliberately stays out of WorldSave — the founder's footprint
    /// is a presentation effect, never simulation state. Trampled tiles
    /// reach the renderer through <see cref="TrampleHeroTile"/>; the
    /// <see cref="DrawTiledFloor"/> consumer reads via
    /// <see cref="WearAt"/>.</summary>
    public float WearAt(int street, int tileIndex) => _terrainWear.WearAt(street, tileIndex);

    /// <summary>Marks the tile under the hero's current feet as trampled.
    /// The view passes the hero's authoritative street and the tile index
    /// (computed from the hero's <c>_heroLateral</c>) so the renderer
    /// never has to know about the journey presenter.</summary>
    public void TrampleHeroTile(int heroStreet, int tileIndex) =>
        _terrainWear.Trample(heroStreet, tileIndex);

    /// <summary>Active ground biome. The view sets this once when the hero
    /// is mounted; the renderer reads it for floor tile sampling and
    /// tree-trunk selection. Biomes drive presentation only — the founder's
    /// mechanical identity (lineage, lineage culture) does not change.</summary>
    public TerrainAtlas.GroundBiome GroundBiome
    {
        get => _groundBiome;
        set => _groundBiome = value;
    }

    /// <summary>
    /// The sheet, grid and tile roles the floor draws from. Null until a
    /// lineage has been set; the floor draw skips rather than guessing, because
    /// a floor drawn from the wrong sheet is worse than no floor.
    /// </summary>
    public GroundAtlasProfile? GroundProfile => _groundProfile;

    /// <summary>Convenience setter keyed by the founder's lineage. Used by
    /// the view's <c>EnsureHeroCarrier</c> hook.</summary>
    public void SetGroundBiomeForLineage(LineageId lineage)
    {
        _groundBiome = TerrainAtlas.BiomeFor(lineage);
        string path = TerrainAtlas.GroundProfilePathFor(lineage);
        _groundProfile = GD.Load<GroundAtlasProfile>(path);
        if (_groundProfile is null)
        {
            GD.PushError($"No ground profile at '{path}' for lineage '{lineage.Value}'.");
        }
    }

    // ---------- World envelope ----------

    /// <summary>Number of construction streets in the active city. Drives
    /// the band stack and the projection window.</summary>
    public int StreetCount
    {
        get => _streetCount;
        set => _streetCount = value;
    }

    /// <summary>Half the lateral width of the city in pixels. The vanishing
    /// point's lateral offset is relative to this center.</summary>
    public float LateralHalfWidthPx
    {
        get => _lateralHalfWidthPx;
        set => _lateralHalfWidthPx = value;
    }

    /// <summary>Number of parcel columns (and rows) the proto-city exposes.
    /// Set during the founding pass; lets the renderer project its
    /// band stack and the camera clamp queries.</summary>
    public int WorldParcelColumns
    {
        get => _worldParcelColumns;
        set => _worldParcelColumns = value;
    }

    /// <summary>Number of parcel rows (and columns) the proto-city exposes.
    /// Inverse of <see cref="WorldParcelColumns"/> on the side axis.</summary>
    public int WorldParcelRows
    {
        get => _worldParcelRows;
        set => _worldParcelRows = value;
    }

    /// <summary>Read-only view of the projected <see cref="PlotBox"/> list.
    /// Populated by <see cref="RefreshPlots"/>; consumed by the renderer
    /// (for the floor/obstacle draw) and by the interaction controller
    /// (for click routing via the plot-lookup bag).</summary>
    public IReadOnlyList<PlotBox> Plots => _plots;

    /// <summary>Read-only view of the projected <see cref="TreeBox"/> list.</summary>
    public IReadOnlyList<TreeBox> Trees => _trees;

    /// <summary>Read-only view of the citizen-id → <see cref="CityMacroSnapshot.CitizenItem"/>
    /// map. The view uses this to format selection text and the
    /// interaction controller uses it to render hover bubbles.</summary>
    public IReadOnlyDictionary<int, CityMacroSnapshot.CitizenItem> CitizenStates => _citizenStates;

    /// <summary>Read-only view of the parcel-territory tint map keyed by
    /// (row, column). The renderer reads it to draw the territory tints
    /// in <c>DrawStreetGround</c>.</summary>
    public IReadOnlyDictionary<(int Row, int Column), ParcelTerritoryState> ParcelTerritory =>
        _parcelTerritory;

    /// <summary>Read-only view of the per-street obstacle band occupancy for
    /// <see cref="StreetRoutePlanner"/>. The view writes via
    /// <see cref="AddBandInterval"/>; the journey presenter reads via
    /// <see cref="GetBandOccupancy"/>.</summary>
    public IReadOnlyDictionary<int, List<StreetRoutePlanner.Interval>> BandOccupancy => _bandOccupancy;

    /// <summary>Append a per-band obstacle interval. Coalescing happens
    /// naturally because the caller (the plot/tree adders) appends in
    /// document order; the planner reads through <see cref="GetBandOccupancy"/>
    /// which returns the same list.</summary>
    public void AddBandInterval(int band, float start, float end)
    {
        if (!_bandOccupancy.TryGetValue(band, out List<StreetRoutePlanner.Interval>? intervals))
        {
            intervals = new List<StreetRoutePlanner.Interval>();
            _bandOccupancy[band] = intervals;
        }
        intervals.Add(new StreetRoutePlanner.Interval(start, end));
    }

    /// <summary>Read the per-band obstacle intervals. Returns the read-only
    /// empty list when no obstacles have been registered for the band.</summary>
    public IReadOnlyList<StreetRoutePlanner.Interval> GetBandOccupancy(int band) =>
        _bandOccupancy.TryGetValue(band, out List<StreetRoutePlanner.Interval>? intervals)
            ? intervals
            : EmptyBand;

    /// <summary>Clears every per-band obstacle interval. Called by the
    /// view's <c>RefreshPlots</c> pass at the start of every snapshot
    /// rebuild.</summary>
    public void ClearBandOccupancy() => _bandOccupancy.Clear();

    /// <summary>Sets the citizen state for a single id. Called by the
    /// view's <see cref="RefreshPlots"/> pass after the snapshot is read.</summary>
    public void SetCitizenState(int id, CityMacroSnapshot.CitizenItem citizen) =>
        _citizenStates[id] = citizen;

    /// <summary>Clears the citizen state map. Called by the view's
    /// <c>RefreshPlots</c> at the start of every snapshot rebuild.</summary>
    public void ClearCitizenStates() => _citizenStates.Clear();

    /// <summary>Sets the parcel-territory tint for one (row, column).
    /// The renderer reads it during <c>DrawStreetGround</c>.</summary>
    public void SetParcelTerritory(int row, int column, ParcelTerritoryState state) =>
        _parcelTerritory[(row, column)] = state;

    /// <summary>Clears the parcel-territory tint map. Called by the view's
    /// <c>RefreshParcelEnvelope</c> at the start of every rebuild.</summary>
    public void ClearParcelTerritory() => _parcelTerritory.Clear();

    /// <summary>Clears the cached plot and tree lists. Called by the view's
    /// <c>RefreshPlots</c> at the start of every snapshot rebuild.</summary>
    public void ClearPlotsAndTrees()
    {
        _plots.Clear();
        _trees.Clear();
    }

    /// <summary>Adds a projected natural-resource unit to the renderer's
    /// tree list. The renderer's <see cref="AddBandInterval"/> already
    /// registers the obstacle used for navigation.</summary>
    public void AddTree(
        CityMacroSnapshot.PlotItem forest,
        int unitId,
        int totalLotColumns)
    {
        NaturalResourceUnitPosition position = forest.ResourceUnitPositions[unitId];
        int street = forest.ParcelRow * ParcelGrid.ConstructionRowsPerParcel
            + position.RowWithinParcel;
        int globalFrontageColumn = forest.ParcelColumn * ParcelGrid.FrontageColumnsPerParcel
            + position.FrontageColumnWithinParcel;
        // GitHub #30: the lateral projection used to be a hand-rolled
        // formula that was also computed independently by the placement
        // underlay. Both sites passed `worldParcelColumns` semantically
        // (one called it `totalLotColumns`), but the field's name on the
        // snapshot side is parcel columns — that mismatch is exactly
        // the kind of off-by-parcel that hid the bug. Route through
        // the shared helper so the resource anchor and the
        // corresponding placement cell can no longer drift.
        int worldParcelColumns = totalLotColumns / ParcelGrid.LotsPerAxis;
        float lateralOffset = MacroGroundProjection.ResourceAnchor(
            globalFrontageColumn,
            worldParcelColumns);
        _trees.Add(new TreeBox(
            street,
            lateralOffset,
            forest.Id.Value,
            unitId,
            forest.GroundResourceType ?? ResourceType.Wood,
            forest.WoodUnitReserves[unitId]));
        ObstacleFootprintTemplate footprint = NaturalResourceFootprintCatalog.Get(
            forest.GroundResourceType ?? ResourceType.Wood);
        float halfTileUnitPx = MacroViewConstants.TileUnitPx * 0.5f;
        float reservedWidth = footprint.ReservedArea.Width * halfTileUnitPx;
        StreetRoutePlanner.Interval obstacle = MacroObstacleGeometry.ObstacleIntervalFromClearances(
            lateralOffset - reservedWidth * 0.5f,
            reservedWidth,
            footprint.LeftClearance * halfTileUnitPx,
            footprint.RightClearance * halfTileUnitPx);
        AddBandInterval(street, obstacle.Start, obstacle.End);
    }

    /// <summary>Adds a projected building lot to the renderer's plot list
    /// and registers the building obstacle for navigation.</summary>
    public void AddPlot(
        CityMacroSnapshot.PlotItem item,
        bool clickable,
        int totalParcelColumns)
    {
        int street = item.RowId;
        // GitHub #30: keep the lateral projection on the same shared
        // helper the resource and the placement underlay read.
        float lateralOffset = MacroGroundProjection.LateralOffsetForWindow(
            item.StartColumn,
            item.FrontageColumns,
            totalParcelColumns);
        float width = item.FrontageColumns * MacroViewConstants.TileUnitPx;
        _plots.Add(new PlotBox(
            street,
            lateralOffset,
            width,
            item.DepthRows * MacroViewConstants.TileUnitPx,
            item.Id.Value,
            item.DisplayName,
            item.Kind,
            item.IsUnderConstruction,
            clickable,
            item.Stock,
            item.StorageCapacity,
            item.CultivationState,
            item.ReadyAtTick));
        StreetRoutePlanner.Interval obstacle = MacroObstacleGeometry.BuildingObstacleInterval(
            item,
            MacroGroundProjection.TotalFrontageColumns(totalParcelColumns),
            MacroViewConstants.TileUnitPx);
        AddBandInterval(street, obstacle.Start, obstacle.End);
    }

    /// <summary>Draws the per-street obstacles (buildings + trees) onto
    /// the given canvas. The renderer owns the plot/tree lists and the
    /// ground biome; the view passes its camera-relative depth and
    /// lateral anchors so the renderer can stay a plain C# class.</summary>
    public void DrawStreetObstacles(
        MacroStreetLiveView view,
        CanvasItem canvas,
        int street,
        float cameraDepthAnchor,
        float cameraLateral,
        MacroHitRects hitRects)
    {
        float depth = street - cameraDepthAnchor;
        float anchorDepth = MacroProjectionHelpers.AnchorDepth(depth);

        foreach (PlotBox plot in _plots)
        {
            if (plot.Street != street) continue;
            float relativeOffset = plot.LateralOffset - cameraLateral;
            (Vector2 position, Vector2 scale) = MacroProjectionHelpers.Project(
                anchorDepth, relativeOffset, MacroViewConstants.CenterX, MacroViewConstants.BaseY);
            var size = new Vector2(plot.Width * scale.X, plot.Height * scale.Y);
            var rect = new Rect2(
                new Vector2(position.X - size.X * 0.5f, position.Y - size.Y),
                size);
            if (plot.CultivationState is CultivationPlotState cultivationState)
            {
                view.DrawCultivationSite(canvas, rect, cultivationState);
                if (plot.IsClickable) hitRects.BuildingClickableRects.Add((rect, plot.BuildingId));
                continue;
            }
            Texture2D? texture = GetBuildingTexture(plot.Kind);
            if (texture is not null)
            {
                canvas.DrawTextureRect(
                    texture,
                    rect,
                    tile: false,
                    modulate: plot.IsUnderConstruction ? MacroViewConstants.UnderConstructionModulate : Colors.White);
            }
            else
            {
                canvas.DrawRect(rect, MacroViewConstants.BuildingColor);
            }
            if (plot.IsClickable) hitRects.BuildingClickableRects.Add((rect, plot.BuildingId));
            if (plot.IsStorageFull)
            {
                view.DrawStorageFullBadge(canvas, rect, plot);
            }
        }

        foreach (TreeBox tree in _trees)
        {
            if (tree.Street != street) continue;
            float treeRelativeOffset = tree.LateralOffset - cameraLateral;
            (Vector2 treePosition, Vector2 treeScale) = MacroProjectionHelpers.Project(
                anchorDepth, treeRelativeOffset, MacroViewConstants.CenterX, MacroViewConstants.BaseY);
            var treeSize = new Vector2(
                MacroViewConstants.ResourceUnitBaseSizePx * treeScale.X,
                MacroViewConstants.ResourceUnitBaseSizePx * treeScale.Y);
            var treeRect = new Rect2(
                new Vector2(treePosition.X - treeSize.X * 0.5f, treePosition.Y - treeSize.Y),
                treeSize);
            view.DrawNaturalResourceUnit(canvas, tree, treeRect);
            hitRects.TreeClickableRects.Add((treeRect, tree));
        }
    }

    /// <summary>Top-level draw orchestrator. The view's <c>_Draw</c> body
    /// delegates here. The renderer clears the hit-rect bag, iterates
    /// the visible streets in back-to-front order, and asks the view
    /// to draw each street (the per-street draw methods still live on
    /// the view; the next extraction pass rotates them over).</summary>
    public void Draw(MacroStreetLiveView view, MacroHitRects hitRects)
    {
        hitRects.Clear();
        int streetCount = _streetCount;
        float cameraDepthAnchor = view.CameraDepthAnchor;
        for (int street = streetCount - 1; street >= 0; street--)
        {
            if (!MacroProjectionHelpers.IsProjectedDepthVisible(street - cameraDepthAnchor)) continue;
            view.DrawStreetGround(street);
        }
    }

    /// <summary>Renders one street's tiled floor. The renderer owns the
    /// terrain atlas, the ground biome, the parcel-territory map, and
    /// the terrain wear grid; the view passes its camera-relative depth
    /// and lateral anchor so the renderer can stay a plain C# class.</summary>
    public void DrawTiledFloor(
        CanvasItem canvas,
        int street,
        float depth,
        float cameraLateral)
    {
        // The ground comes from the biome's own profile — its own sheet, its own
        // grid — and no longer from the shared Kenney atlas the trees still use.
        if (_groundProfile?.Atlas is not { } groundAtlas) return;
        if (_groundProfile.Fill.Length == 0) return;
        int totalTiles = Mathf.RoundToInt(2f * _lateralHalfWidthPx / MacroViewConstants.TileUnitPx);
        int parcelRow = street / ParcelGrid.ConstructionRowsPerParcel;
        for (int tileRow = 0; tileRow < ParcelGrid.TilesPerStandardLot; tileRow++)
        {
            float depthNear = depth + tileRow / (float)ParcelGrid.TilesPerStandardLot;
            float depthFar = depth + (tileRow + 1) / (float)ParcelGrid.TilesPerStandardLot;
            float yNear = MacroProjectionHelpers.ProjectedRowScreenY(depthNear, MacroViewConstants.BaseY);
            float yFar = MacroProjectionHelpers.ProjectedRowScreenY(depthFar, MacroViewConstants.BaseY);
            float scaleNear = MacroProjectionHelpers.HorizontalScale(depthNear);
            float scaleFar = MacroProjectionHelpers.HorizontalScale(depthFar);
            int globalTileRow = street * ParcelGrid.TilesPerStandardLot + tileRow;

            for (int tileIndex = 0; tileIndex < totalTiles; tileIndex++)
            {
                int parcelColumn = tileIndex / ParcelGrid.FrontageColumnsPerParcel;
                if (!_parcelTerritory.ContainsKey((parcelRow, parcelColumn))) continue;
                float tileCenterGlobal = (tileIndex + 0.5f) * MacroViewConstants.TileUnitPx - _lateralHalfWidthPx;
                float leftGlobal = tileCenterGlobal - MacroViewConstants.TileUnitPx * 0.5f - cameraLateral;
                float rightGlobal = tileCenterGlobal + MacroViewConstants.TileUnitPx * 0.5f - cameraLateral;
                int variant = TerrainAtlas.GroundVariantIndex(
                    tileIndex, globalTileRow, _groundProfile.Fill.Length);
                DrawPixelStaircaseTrapezoid(
                    canvas,
                    yNear, yFar,
                    MacroViewConstants.CenterX + leftGlobal * scaleNear,
                    MacroViewConstants.CenterX + rightGlobal * scaleNear,
                    MacroViewConstants.CenterX + leftGlobal * scaleFar,
                    MacroViewConstants.CenterX + rightGlobal * scaleFar,
                    groundAtlas, _groundProfile.RegionOfId(_groundProfile.Fill[variant]));

                float wear = tileRow == 0 ? WearAt(street, tileIndex) : 0f;
                if (wear <= 0f) continue;
                float dirtWidthFactor = Mathf.Clamp(0.04f + wear * 0.96f, 0f, 1f);
                float halfDirtWidth = MacroViewConstants.TileUnitPx * dirtWidthFactor * 0.5f;
                float dirtLeft = tileCenterGlobal - halfDirtWidth - cameraLateral;
                float dirtRight = tileCenterGlobal + halfDirtWidth - cameraLateral;
                DrawPixelStaircaseTrapezoid(
                    canvas,
                    yNear, yFar,
                    MacroViewConstants.CenterX + dirtLeft * scaleNear,
                    MacroViewConstants.CenterX + dirtRight * scaleNear,
                    MacroViewConstants.CenterX + dirtLeft * scaleFar,
                    MacroViewConstants.CenterX + dirtRight * scaleFar,
                    groundAtlas, _groundProfile.RegionOfId(_groundProfile.Path));
            }
        }
    }

    /// <summary>Approximates a perspective trapezoid as a "staircase" of
    /// small, axis-aligned, pixel-snapped rectangles (see the moved
    /// view doc for the full rationale). Forwards to
    /// <see cref="SharedDepthBands.DrawStaircaseTrapezoid"/> so the
    /// expedition path renderer can consume the same primitive in
    /// #21 without copy/paste.</summary>
    public void DrawPixelStaircaseTrapezoid(
        CanvasItem canvas,
        float yNear, float yFar,
        float xLeftNear, float xRightNear,
        float xLeftFar, float xRightFar,
        Texture2D atlas, Rect2 sourceRegion) =>
        SharedDepthBands.DrawStaircaseTrapezoid(
            canvas, yNear, yFar,
            xLeftNear, xRightNear, xLeftFar, xRightFar,
            atlas, sourceRegion, MacroViewConstants.PixelStepPx);

    /// <summary>Render the placement footprint for a single street.
    /// The view supplies the placement cells / lots / hover / selected
    /// state so the renderer stays a pure draw helper. The hit-rect
    /// bag is filled here.</summary>
    public void DrawPlacementLots(
        CanvasItem canvas,
        int street,
        float streetDepth,
        float cameraLateral,
        IReadOnlyList<PlacementPresenter.PlacementCellBox> placementCells,
        IReadOnlyList<PlacementPresenter.PlacementLotBox> placementLots,
        PlacementPresenter.PlacementLotBox? hoveredPlacementLot,
        WorldofGoses.Domain.ConstructionLot? selectedPlacementLot,
        MacroHitRects hitRects)
    {
        foreach (PlacementPresenter.PlacementCellBox cell in placementCells)
        {
            if (cell.Street != street) continue;
            ProjectPlacementFootprint(
                cell.LateralOffset, cell.Width, streetDepth,
                cameraLateral,
                out Vector2 nearLeft, out Vector2 nearRight,
                out Vector2 farRight, out Vector2 farLeft);
            bool blocked = cell.Cell.State != WorldofGoses.Domain.FrontageCellState.Available;
            // GitHub #30: the availability underlay represents one
            // frontage cell, not three. A 1×3 strip with internal
            // depth sub-divisions made the grid look like it tracked
            // three independent states when the domain only knows
            // about one. The 3×3 building preview below keeps
            // `depthDivisions: 3` because the shelter footprint
            // actually spans three depth tiles.
            DrawSteppedPlacementFootprint(
                canvas,
                nearLeft, nearRight, farRight, farLeft,
                blocked ? MacroViewConstants.PlacementBlockedCellColor : MacroViewConstants.PlacementAvailableColor,
                MacroViewConstants.PlacementGridColor,
                frontageDivisions: 1,
                depthDivisions: 1,
                drawInvalidMarker: blocked);
        }

        foreach (PlacementPresenter.PlacementLotBox lot in placementLots)
        {
            if (lot.Street != street) continue;
            ProjectPlacementFootprint(
                lot.LateralOffset, lot.Width, streetDepth,
                cameraLateral,
                out Vector2 nearLeft, out Vector2 nearRight,
                out Vector2 farRight, out Vector2 farLeft);
            Vector2 boundsMin = new(
                Mathf.Min(Mathf.Min(nearLeft.X, nearRight.X), Mathf.Min(farLeft.X, farRight.X)),
                Mathf.Min(nearLeft.Y, farLeft.Y));
            Vector2 boundsMax = new(
                Mathf.Max(Mathf.Max(nearLeft.X, nearRight.X), Mathf.Max(farLeft.X, farRight.X)),
                Mathf.Max(nearLeft.Y, farLeft.Y));
            hitRects.PlacementRects.Add((new Rect2(boundsMin, boundsMax - boundsMin), lot));
        }

        PlacementPresenter.PlacementLotBox? preview = hoveredPlacementLot is PlacementPresenter.PlacementLotBox hovered
            && hovered.Street == street
                ? hovered
                : null;
        if (preview is null && selectedPlacementLot is WorldofGoses.Domain.ConstructionLot selected)
        {
            foreach (PlacementPresenter.PlacementLotBox candidate in placementLots)
            {
                if (candidate.Street != street || candidate.Window.Lot != selected) continue;
                preview = candidate;
                break;
            }
        }
        if (preview is not PlacementPresenter.PlacementLotBox highlighted) return;
        ProjectPlacementFootprint(
            highlighted.LateralOffset, highlighted.Width, streetDepth,
            cameraLateral,
            out Vector2 previewNearLeft, out Vector2 previewNearRight,
            out Vector2 previewFarRight, out Vector2 previewFarLeft);
        bool isSelected = selectedPlacementLot is WorldofGoses.Domain.ConstructionLot selectedLot
            && selectedLot == highlighted.Window.Lot
            && hoveredPlacementLot is null;
        Color previewFill = !highlighted.Window.IsValid
            ? MacroViewConstants.PlacementHoveredInvalidColor
            : isSelected
                ? MacroViewConstants.PlacementSelectedColor
                : MacroViewConstants.PlacementHoveredValidColor;
        Color previewOutline = !highlighted.Window.IsValid
            ? new Color("#ff7777")
            : isSelected
                ? new Color("#ffe08a")
                : new Color("#8dffad");
        DrawSteppedPlacementFootprint(
            canvas,
            previewNearLeft, previewNearRight, previewFarRight, previewFarLeft,
            previewFill, previewOutline,
            frontageDivisions: highlighted.Window.Lot.FrontageColumns,
            depthDivisions: 3,
            drawInvalidMarker: !highlighted.Window.IsValid);
    }

    /// <summary>Project a rectangular footprint to its four corners in
    /// screen space. Pure helper used by both the placement footprint
    /// and the territory-tint draw.</summary>
    public void ProjectPlacementFootprint(
        float lateralOffset,
        float width,
        float streetDepth,
        float cameraLateral,
        out Vector2 nearLeft,
        out Vector2 nearRight,
        out Vector2 farRight,
        out Vector2 farLeft)
    {
        float lotLeft = lateralOffset - width * 0.5f - cameraLateral;
        float lotRight = lotLeft + width;
        float depthNear = streetDepth;
        float depthFar = streetDepth + 1f;
        float yNear = MacroProjectionHelpers.ProjectedRowScreenY(depthNear, MacroViewConstants.BaseY);
        float yFar = MacroProjectionHelpers.ProjectedRowScreenY(depthFar, MacroViewConstants.BaseY);
        float scaleNear = MacroProjectionHelpers.HorizontalScale(depthNear);
        float scaleFar = MacroProjectionHelpers.HorizontalScale(depthFar);
        nearLeft = new Vector2(MacroViewConstants.CenterX + lotLeft * scaleNear, yNear);
        nearRight = new Vector2(MacroViewConstants.CenterX + lotRight * scaleNear, yNear);
        farRight = new Vector2(MacroViewConstants.CenterX + lotRight * scaleFar, yFar);
        farLeft = new Vector2(MacroViewConstants.CenterX + lotLeft * scaleFar, yFar);
    }

    /// <summary>Per-parcel-column tint driven by the territory state.
    /// Available parcels get no overlay; locked gets an opaque dark band;
    /// intermediate states get a translucent hue.</summary>
    public void DrawParcelTerritoryTints(
        CanvasItem canvas,
        int street,
        float streetDepth,
        float cameraLateral,
        int worldParcelColumns)
    {
        int parcelRow = street / ParcelGrid.ConstructionRowsPerParcel;
        float depthNear = streetDepth;
        float depthFar = streetDepth + 1f;
        float yNear = MacroProjectionHelpers.ProjectedRowScreenY(depthNear, MacroViewConstants.BaseY);
        float yFar = MacroProjectionHelpers.ProjectedRowScreenY(depthFar, MacroViewConstants.BaseY);
        float scaleNear = MacroProjectionHelpers.HorizontalScale(depthNear);
        float scaleFar = MacroProjectionHelpers.HorizontalScale(depthFar);
        float totalLotColumns = worldParcelColumns * ParcelGrid.LotsPerAxis;
        foreach (((int Row, int Column) coordinate, ParcelTerritoryState territoryState) in _parcelTerritory)
        {
            if (coordinate.Row != parcelRow) continue;
            Color fill = territoryState switch
            {
                ParcelTerritoryState.Locked => MacroViewConstants.LockedParcelColor,
                ParcelTerritoryState.Reconnoitred => MacroViewConstants.ReconnoitredParcelColor,
                ParcelTerritoryState.RouteSecured => MacroViewConstants.RouteSecuredParcelColor,
                _ => Colors.Transparent,
            };
            if (fill.A == 0) continue;
            float parcelLeftColumn = coordinate.Column * ParcelGrid.LotsPerAxis;
            float parcelLeft = (parcelLeftColumn - totalLotColumns * 0.5f) * MacroViewConstants.LotUnitPx
                - cameraLateral;
            float parcelRight = parcelLeft + ParcelGrid.LotsPerAxis * MacroViewConstants.LotUnitPx;
            var nearLeft = new Vector2(MacroViewConstants.CenterX + parcelLeft * scaleNear, yNear);
            var nearRight = new Vector2(MacroViewConstants.CenterX + parcelRight * scaleNear, yNear);
            var farRight = new Vector2(MacroViewConstants.CenterX + parcelRight * scaleFar, yFar);
            var farLeft = new Vector2(MacroViewConstants.CenterX + parcelLeft * scaleFar, yFar);
            DrawSteppedTintTrapezoid(canvas, nearLeft, nearRight, farRight, farLeft, fill);
        }
    }

    /// <summary>Approximates a perspective trapezoid as a "staircase" of
    /// rectangles (territory-tint variant).</summary>
    public void DrawSteppedTintTrapezoid(
        CanvasItem canvas,
        Vector2 nearLeft,
        Vector2 nearRight,
        Vector2 farRight,
        Vector2 farLeft,
        Color fill)
    {
        float height = Mathf.Abs(farLeft.Y - nearLeft.Y);
        int stripes = Mathf.Max(1, Mathf.CeilToInt(height / MacroViewConstants.PixelStepPx));
        for (int index = 0; index < stripes; index++)
        {
            float t0 = index / (float)stripes;
            float t1 = (index + 1) / (float)stripes;
            Vector2 left0 = PixelMotion.Snap(nearLeft.Lerp(farLeft, t0));
            Vector2 right0 = PixelMotion.Snap(nearRight.Lerp(farRight, t0));
            Vector2 left1 = PixelMotion.Snap(nearLeft.Lerp(farLeft, t1));
            Vector2 right1 = PixelMotion.Snap(nearRight.Lerp(farRight, t1));
            float top = Mathf.Min(left0.Y, left1.Y);
            float bottom = Mathf.Max(left0.Y, left1.Y);
            float left = Mathf.Min(left0.X, left1.X);
            float right = Mathf.Max(right0.X, right1.X);
            canvas.DrawRect(new Rect2(
                new Vector2(left, top),
                new Vector2(Mathf.Max(1f, right - left), Mathf.Max(1f, bottom - top))),
                fill);
        }
    }

    /// <summary>Draws a stepped placement footprint outline + fill.
    /// Mirrors the moved view doc — the stepped trapezoid is the
    /// pixel-art way to draw a trapezoid on a chunky grid.</summary>
    public void DrawSteppedPlacementFootprint(
        CanvasItem canvas,
        Vector2 nearLeft,
        Vector2 nearRight,
        Vector2 farRight,
        Vector2 farLeft,
        Color fill,
        Color outline,
        int frontageDivisions,
        int depthDivisions,
        bool drawInvalidMarker)
    {
        const float stripeHeight = 2f;
        float height = Mathf.Abs(farLeft.Y - nearLeft.Y);
        int stripes = Mathf.Max(1, Mathf.CeilToInt(height / stripeHeight));
        Vector2 previousLeft = PixelMotion.Snap(nearLeft);
        Vector2 previousRight = PixelMotion.Snap(nearRight);
        canvas.DrawLine(previousLeft, previousRight, outline, 2f, antialiased: false);

        for (int index = 0; index < stripes; index++)
        {
            float t0 = index / (float)stripes;
            float t1 = (index + 1) / (float)stripes;
            Vector2 left0 = PixelMotion.Snap(nearLeft.Lerp(farLeft, t0));
            Vector2 right0 = PixelMotion.Snap(nearRight.Lerp(farRight, t0));
            Vector2 left1 = PixelMotion.Snap(nearLeft.Lerp(farLeft, t1));
            Vector2 right1 = PixelMotion.Snap(nearRight.Lerp(farRight, t1));
            float top = Mathf.Min(left0.Y, left1.Y);
            float bottom = Mathf.Max(left0.Y, left1.Y);
            canvas.DrawRect(new Rect2(
                new Vector2(Mathf.Min(left0.X, left1.X), top),
                new Vector2(
                    Mathf.Max(right0.X, right1.X) - Mathf.Min(left0.X, left1.X),
                    Mathf.Max(1f, bottom - top))), fill);

            canvas.DrawLine(previousLeft, new Vector2(left1.X, previousLeft.Y), outline, 2f, false);
            canvas.DrawLine(new Vector2(left1.X, previousLeft.Y), left1, outline, 2f, false);
            canvas.DrawLine(previousRight, new Vector2(right1.X, previousRight.Y), outline, 2f, false);
            canvas.DrawLine(new Vector2(right1.X, previousRight.Y), right1, outline, 2f, false);
            previousLeft = left1;
            previousRight = right1;
        }
        canvas.DrawLine(previousLeft, previousRight, outline, 2f, antialiased: false);

        for (int column = 1; column < frontageDivisions; column++)
        {
            float t = column / (float)frontageDivisions;
            Vector2 near = PixelMotion.Snap(nearLeft.Lerp(nearRight, t));
            Vector2 far = PixelMotion.Snap(farLeft.Lerp(farRight, t));
            DrawSteppedPlacementEdge(canvas, near, far, stripes, outline);
        }
        for (int row = 1; row < depthDivisions; row++)
        {
            float t = row / (float)depthDivisions;
            Vector2 left = PixelMotion.Snap(nearLeft.Lerp(farLeft, t));
            Vector2 right = PixelMotion.Snap(nearRight.Lerp(farRight, t));
            canvas.DrawLine(left, right, outline, 2f, antialiased: false);
        }
        if (drawInvalidMarker)
        {
            Vector2 topLeft = PixelMotion.Snap(nearLeft.Lerp(farLeft, 0.75f));
            Vector2 topRight = PixelMotion.Snap(nearRight.Lerp(farRight, 0.75f));
            Vector2 bottomLeft = PixelMotion.Snap(nearLeft.Lerp(farLeft, 0.25f));
            Vector2 bottomRight = PixelMotion.Snap(nearRight.Lerp(farRight, 0.25f));
            canvas.DrawLine(topLeft.Lerp(topRight, 0.2f), bottomLeft.Lerp(bottomRight, 0.8f), outline, 3f, false);
            canvas.DrawLine(topRight.Lerp(topLeft, 0.2f), bottomRight.Lerp(bottomLeft, 0.8f), outline, 3f, false);
        }
    }

    /// <summary>Draws one stepped placement edge — the stair-stepped
    /// diagonal between two anchors.</summary>
    public void DrawSteppedPlacementEdge(
        CanvasItem canvas,
        Vector2 from,
        Vector2 to,
        int steps,
        Color color)
    {
        Vector2 previous = from;
        for (int index = 1; index <= steps; index++)
        {
            Vector2 next = PixelMotion.Snap(from.Lerp(to, index / (float)steps));
            Vector2 corner = new(next.X, previous.Y);
            canvas.DrawLine(previous, corner, color, 2f, false);
            canvas.DrawLine(corner, next, color, 2f, false);
            previous = next;
        }
    }

    /// <summary>Draws one cultivation site: the soil bed, its furrows, the
    /// per-state plant markers, and the harvest-ready badge. Pure drawing
    /// against the passed rect — the band layer calls it through the view's
    /// painter seam.</summary>
    public void DrawCultivationSite(CanvasItem canvas, Rect2 rect, CultivationPlotState state)
    {
        Color soil = state == CultivationPlotState.Prepared
            ? new Color("#71513a")
            : new Color("#59412f");
        canvas.DrawRect(rect, soil);
        float lineWidth = Mathf.Max(2f, rect.Size.X * 0.035f);
        for (int row = 1; row <= 3; row++)
        {
            float y = Mathf.Round(rect.Position.Y + rect.Size.Y * row / 4f);
            canvas.DrawLine(
                new Vector2(rect.Position.X + rect.Size.X * 0.12f, y),
                new Vector2(rect.End.X - rect.Size.X * 0.12f, y),
                new Color("#3d2b22"),
                lineWidth,
                antialiased: false);
        }
        if (state == CultivationPlotState.Prepared) return;

        Color plant = state switch
        {
            CultivationPlotState.Sown => new Color("#b89a52"),
            CultivationPlotState.Growing => new Color("#6f9f48"),
            CultivationPlotState.Ready => new Color("#d2b24c"),
            CultivationPlotState.Spent => new Color("#8a7457"),
            _ => Colors.White,
        };
        int markerCount = state == CultivationPlotState.Sown ? 3 : 5;
        float markerSize = Mathf.Max(3f, rect.Size.X * 0.075f);
        for (int marker = 0; marker < markerCount; marker++)
        {
            float x = rect.Position.X
                + rect.Size.X * (marker + 1f) / (markerCount + 1f);
            float height = state switch
            {
                CultivationPlotState.Sown => markerSize,
                CultivationPlotState.Growing => markerSize * 2f,
                CultivationPlotState.Ready => markerSize * 3f,
                _ => markerSize * 1.4f,
            };
            canvas.DrawRect(
                new Rect2(
                    new Vector2(
                        Mathf.Round(x - markerSize * 0.5f),
                        Mathf.Round(rect.End.Y - rect.Size.Y * 0.22f - height)),
                    new Vector2(markerSize, height)),
                plant);
        }
        if (state == CultivationPlotState.Ready)
        {
            float badge = Mathf.Max(5f, rect.Size.X * 0.12f);
            canvas.DrawCircle(
                new Vector2(rect.End.X - badge, rect.Position.Y + badge),
                badge * 0.55f,
                new Color("#f2d35f"),
                filled: true,
                width: -1f,
                antialiased: false);
        }
    }

    /// <summary>Draws one gatherable ground unit from the shared terrain atlas.
    /// The renderer owns the ground biome and the atlas reference, so
    /// this is the canonical home for the per-tree draw.</summary>
    public void DrawNaturalResourceUnit(
        CanvasItem canvas,
        TreeBox unit,
        Rect2 rect,
        Texture2D terrainAtlas)
    {
        if (unit.ResourceType != ResourceType.Wood)
        {
            canvas.DrawTextureRectRegion(
                terrainAtlas,
                rect,
                TerrainAtlas.ResourceRegion(unit.ResourceType, unit.ForestId, unit.UnitId));
            return;
        }
        TerrainAtlas.TreeVariant variant =
            TerrainAtlas.TreeFor(_groundBiome, unit.ForestId, unit.UnitId);
        if (variant.IsTall)
        {
            var canopy = new Rect2(
                new Vector2(rect.Position.X, rect.Position.Y - rect.Size.Y),
                rect.Size);
            canvas.DrawTextureRectRegion(
                terrainAtlas, canopy, TerrainAtlas.RegionOfId(variant.CanopyId));
        }
        canvas.DrawTextureRectRegion(
            terrainAtlas,
            new Rect2(rect.Position, rect.Size),
            TerrainAtlas.RegionOfId(variant.TrunkId));
    }

    /// <summary>Draws the storage-full badge in the top-right corner of a
    /// building rect and publishes the hit-rect for the badge.</summary>
    public void DrawStorageFullBadge(
        CanvasItem canvas,
        Rect2 buildingRect,
        PlotBox plot,
        Texture2D storageFullIcon,
        MacroHitRects hitRects)
    {
        var badgeRect = new Rect2(
            new Vector2(
                buildingRect.End.X - MacroViewConstants.StatusBadgeSize,
                buildingRect.Position.Y - MacroViewConstants.StatusBadgeSize * 0.5f),
            new Vector2(MacroViewConstants.StatusBadgeSize, MacroViewConstants.StatusBadgeSize));
        var borderRect = badgeRect.Grow(MacroViewConstants.StatusBadgeBorder);
        canvas.DrawRect(borderRect, LineageThemeRegistry.IconAccent);
        canvas.DrawRect(badgeRect, new Color(0.06f, 0.05f, 0.04f, 0.94f));
        canvas.DrawTextureRect(storageFullIcon, badgeRect, tile: false);
        hitRects.StorageBadgeRects.Add((borderRect, plot));
    }
}
