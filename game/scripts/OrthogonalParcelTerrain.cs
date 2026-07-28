#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Presentation-only orthogonal ground for the macro city. It establishes the
/// parcel grid and a deterministic scattering of provisional trees without
/// turning terrain art into simulation state.
///
/// The ground itself is a single <see cref="TileMapLayer"/> (S-1.3): one
/// draw call regardless of parcel count, instead of a
/// <c>DrawTextureRectRegion</c> call per tile.
///
/// <para>
/// Parcel geometry is now fixed-scale, not resize-driven: a parcel is
/// always exactly <see cref="ParcelGrid.LotsPerAxis"/> ×
/// <see cref="ParcelGrid.TilesPerStandardLot"/> tiles (9×9 at the current
/// domain constants), matching <c>ParcelGrid</c>. <see cref="CalculateTerrainRect"/>
/// centers that fixed-size world within the available display area when it
/// fits, and otherwise positions it via the shared <see cref="PanOffset"/>
/// (a lightweight virtual-camera scroll — this codebase is Control-first,
/// so a real <c>Camera2D</c> would also pan the HUD unless it moved into
/// its own <c>CanvasLayer</c>; the offset keeps the fix scoped to this
/// class and its two static-method consumers). The layer's cells are
/// rebuilt and its <see cref="Node2D.Position"/>/<see cref="Node2D.Scale"/>
/// re-applied on every resize or pan (never per frame). Grid border/lines
/// stay hand-drawn in <see cref="_Draw"/> since <see cref="TileMapLayer"/>
/// has nothing to offer there. The layer uses
/// <see cref="CanvasItem.ShowBehindParent"/> at the parent's z-index so it
/// stays behind the grid lines without falling behind the macro background.
/// </para>
/// </summary>
public partial class OrthogonalParcelTerrain : Control, ITerrainRenderer
{
    private const int SourceTileSize = 16;
    internal const int DisplayTileSize = 32;
    internal const int ParcelColumns = 4;
    internal const int ParcelRows = 2;
    private const float TopHudReservedHeight = 96f;
    private const float BottomHudReservedHeight = 72f;
    private static readonly Vector2I GroundTileA = new(0, 0);
    private static readonly Vector2I GroundTileB = new(1, 0);

    /// <summary>Fixed tiles-per-parcel edge (9 with the current domain constants).</summary>
    internal const int ParcelTileSpan =
        WorldofGoses.Domain.ParcelGrid.LotsPerAxis
            * WorldofGoses.Domain.ParcelGrid.TilesPerStandardLot;

    internal const float ParcelPixelSize = DisplayTileSize * ParcelTileSpan;

    /// <summary>
    /// Shared virtual-camera scroll, in world pixels, applied by every
    /// consumer of <see cref="CalculateTerrainRect"/>/<see cref="CalculateParcelRect"/>.
    /// A plain static rather than an instance/DI value: there is exactly one
    /// macro view live at a time (no split-screen), matching this codebase's
    /// existing singleton-registry precedent (<c>CitizenSpriteBank.Instance</c>,
    /// <c>LineageThemeRegistry</c>). Clamped by <see cref="ClampPan"/> so it
    /// never scrolls the world past its own edge.
    /// </summary>
    internal static Vector2 PanOffset { get; private set; } = Vector2.Zero;

    /// <summary>Test seam: <see cref="PanOffset"/> is shared static state, so
    /// xUnit tests that call <see cref="CalculateTerrainRect"/> directly must
    /// reset it first to avoid order-dependence between test cases.</summary>
    internal static void ResetPanForTests() => PanOffset = Vector2.Zero;

    /// <summary>Raised whenever <see cref="PanOffset"/> changes. Consumers that cache
    /// positions computed from <see cref="CalculateParcelRect"/> (plots, lot-selection
    /// overlays, the hero anchor) must reposition on this — panning doesn't touch any
    /// Control's own <c>Size</c>, so their own <c>Resized</c> signal never fires for it.</summary>
    [Signal] public delegate void PanChangedEventHandler();

    private bool _panDragging;
    private Vector2 _panDragStartMouse;
    private Vector2 _panDragStartOffset;
    private const float PanDragThresholdPx = 4f;

    /// <inheritdoc />
    public int VisibleParcelCount => ParcelColumns * ParcelRows;

    /// <inheritdoc />
    public int VisibleTreeCount => _trees.Count;

    /// <inheritdoc />
    public void SetParcelHighlight(ParcelId? parcelId)
    {
        // No-op for the current sprite-based implementation. The
        // future TileMapTerrainRenderer paints a highlight tile on
        // the active parcel; this stub is the seam.
        _ = parcelId;
    }

    private TileMapLayer _groundLayer = null!;
    private readonly List<ResourceTree> _trees = new();
    private readonly Dictionary<(int ForestId, int UnitId), Vector2> _resolvedTreePositions =
        new();
    private IReadOnlyList<CityMacroSnapshot.PlotItem> _forests =
        Array.Empty<CityMacroSnapshot.PlotItem>();
    private ResourceActionMenu _actionMenu = null!;
    private bool _canGather = true;
    private string _gatherUnavailableReason = string.Empty;

    [Export] public PackedScene TreeScene { get; set; } = null!;

    [Signal]
    public delegate void GatherRequestedEventHandler(
        int forestId,
        int unitId,
        Vector2 targetPosition);

    public override void _Ready()
    {
        // Stop (not Ignore): empty terrain background now needs to catch
        // drag input to pan. Children (trees, lot buttons) still get first
        // refusal at their own pixels regardless of the parent's filter, so
        // this does not change click routing to them.
        MouseFilter = MouseFilterEnum.Stop;
        // Panning can position content (and this control's own _Draw()
        // border) outside this control's rect when the fixed-size world is
        // larger than the visible display area; without clipping that would
        // bleed into whatever sits beyond this control's own bounds.
        ClipContents = true;
        _groundLayer = new TileMapLayer
        {
            Name = "GroundLayer",
            TileSet = BuildGroundTileSet(),
            // Draw before the parent's border/grid, but keep the same global
            // z-index. A negative z-index would place this below the sibling
            // macro Background and make the parcel surface appear transparent.
            ShowBehindParent = true,
        };
        AddChild(_groundLayer);
        _actionMenu = GetNode<ResourceActionMenu>("ResourceActionMenu");
        _actionMenu.GatherRequested += OnGatherRequested;
        Resized += OnResized;
        ClampPan(Size);
        RebuildGround(CalculateTerrainRect(Size));
        QueueRedraw();
    }

    private TileSet BuildGroundTileSet()
    {
        Image groundImage = Image.CreateEmpty(
            SourceTileSize * 2,
            SourceTileSize,
            useMipmaps: false,
            Image.Format.Rgba8);
        groundImage.FillRect(
            new Rect2I(0, 0, SourceTileSize, SourceTileSize),
            new Color("#385a3d"));
        groundImage.FillRect(
            new Rect2I(SourceTileSize, 0, SourceTileSize, SourceTileSize),
            new Color("#3f6343"));
        ImageTexture groundTexture = ImageTexture.CreateFromImage(groundImage);
        var atlasSource = new TileSetAtlasSource
        {
            Texture = groundTexture,
            TextureRegionSize = new Vector2I(SourceTileSize, SourceTileSize),
        };
        atlasSource.CreateTile(GroundTileA);
        atlasSource.CreateTile(GroundTileB);

        var tileSet = new TileSet { TileSize = new Vector2I(SourceTileSize, SourceTileSize) };
        tileSet.AddSource(atlasSource, 0);
        return tileSet;
    }

    /// <summary>
    /// Clears and repopulates the ground layer's cells for the current
    /// terrain rect, then repositions/rescales the layer so its cells
    /// (authored at <see cref="SourceTileSize"/>) land at
    /// <see cref="DisplayTileSize"/> starting from the rect's origin.
    /// Called once per resize (via <see cref="OnResized"/>) and once at
    /// <see cref="_Ready"/> — never per frame.
    /// </summary>
    private void RebuildGround(Rect2 terrain)
    {
        _groundLayer.Clear();
        int columns = Mathf.FloorToInt(terrain.Size.X / DisplayTileSize);
        int rows = Mathf.FloorToInt(terrain.Size.Y / DisplayTileSize);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                bool alternate = (column * 3 + row * 5) % 11 == 0;
                _groundLayer.SetCell(
                    new Vector2I(column, row),
                    0,
                    alternate ? GroundTileB : GroundTileA);
            }
        }
        _groundLayer.Position = terrain.Position;
        float scale = DisplayTileSize / (float)SourceTileSize;
        _groundLayer.Scale = new Vector2(scale, scale);
    }

    public override void _ExitTree()
    {
        Resized -= OnResized;
        if (_actionMenu is not null) _actionMenu.GatherRequested -= OnGatherRequested;
    }

    public override void _Draw()
    {
        Rect2 terrain = CalculateTerrainRect(Size);
        // An unfilled outline, not a solid fill: this call and the grid
        // lines below run at the Control's own (default) z-index, above
        // _groundLayer's -1. A filled rect here would paint over the
        // whole terrain and hide the tile layer sitting behind it.
        DrawRect(terrain.Grow(6), new Color("#14221f"), filled: false, width: 6);
        DrawParcelGrid(terrain);
    }

    public void RenderResources(
        IReadOnlyList<CityMacroSnapshot.PlotItem> buildings,
        IReadOnlyList<Rect2>? occupiedGlobalRects = null,
        bool canGather = true,
        string gatherUnavailableReason = "")
    {
        _ = occupiedGlobalRects;
        _canGather = canGather;
        _gatherUnavailableReason = gatherUnavailableReason;
        _forests = buildings
            // Keep depleted patches in the spatial index while their domain
            // entity still exists. Live units render below, but removing the
            // patch here would shift every later unit and invalidate a
            // citizen's persisted forestId + unitId visit.
            .Where(item => item.Kind == BuildingKind.Forest)
            .ToArray();
        RebuildTrees();
    }

    /// <summary>
    /// Returns the world's fixed-size rect (<see cref="ParcelColumns"/> ×
    /// <see cref="ParcelRows"/> parcels of exactly <see cref="ParcelTileSpan"/>
    /// tiles each) positioned within <paramref name="viewportSize"/>: centered
    /// on an axis where the world already fits the display, or offset by
    /// <see cref="PanOffset"/> (scrolled, clamped to the world's own edges) on
    /// an axis where it doesn't. <see cref="Rect2.Size"/> is always the full
    /// world size — callers that need "what's actually visible" should
    /// intersect with the display area themselves; rendering is clipped via
    /// <see cref="Control.ClipContents"/> instead.
    /// </summary>
    internal static Rect2 CalculateTerrainRect(Vector2 viewportSize)
    {
        float displayWidth = Mathf.Max(0, viewportSize.X - 64);
        float displayHeight = Mathf.Max(
            0,
            viewportSize.Y - TopHudReservedHeight - BottomHudReservedHeight);
        float worldWidth = ParcelColumns * ParcelPixelSize;
        float worldHeight = ParcelRows * ParcelPixelSize;

        float extraX = Mathf.Max(0, displayWidth - worldWidth);
        float extraY = Mathf.Max(0, displayHeight - worldHeight);
        float marginX = 32f + extraX * 0.5f;
        float marginY = TopHudReservedHeight + extraY * 0.5f;

        return new Rect2(
            new Vector2(
                Mathf.Floor(marginX - PanOffset.X),
                Mathf.Floor(marginY - PanOffset.Y)),
            new Vector2(worldWidth, worldHeight));
    }

    internal static Rect2 CalculateParcelRect(
        Vector2 viewportSize,
        int parcelColumn,
        int parcelRow)
    {
        Rect2 terrain = CalculateTerrainRect(viewportSize);
        float parcelWidth = terrain.Size.X / ParcelColumns;
        float parcelHeight = terrain.Size.Y / ParcelRows;
        return new Rect2(
            terrain.Position + new Vector2(
                parcelColumn * parcelWidth,
                parcelRow * parcelHeight),
            new Vector2(parcelWidth, parcelHeight));
    }

    private void DrawParcelGrid(Rect2 terrain)
    {
        Color border = new("#395547");
        Color parcelLine = new("#6b7654");
        Color lotLine = new(0.31f, 0.38f, 0.29f, 0.78f);
        Color tileLine = new(0.20f, 0.27f, 0.22f, 0.34f);
        DrawRect(terrain, border, filled: false, width: 4);

        int worldTileColumns = ParcelColumns * ParcelTileSpan;
        int worldTileRows = ParcelRows * ParcelTileSpan;
        for (int column = 1; column < worldTileColumns; column++)
        {
            if (column % ParcelTileSpan == 0) continue;
            float x = terrain.Position.X + column * DisplayTileSize;
            bool lotBoundary = column % ParcelGrid.TilesPerStandardLot == 0;
            DrawLine(
                new Vector2(x, terrain.Position.Y),
                new Vector2(x, terrain.End.Y),
                lotBoundary ? lotLine : tileLine,
                width: lotBoundary ? 2f : 1f);
        }
        for (int row = 1; row < worldTileRows; row++)
        {
            if (row % ParcelTileSpan == 0) continue;
            float y = terrain.Position.Y + row * DisplayTileSize;
            bool lotBoundary = row % ParcelGrid.TilesPerStandardLot == 0;
            DrawLine(
                new Vector2(terrain.Position.X, y),
                new Vector2(terrain.End.X, y),
                lotBoundary ? lotLine : tileLine,
                width: lotBoundary ? 2f : 1f);
        }

        float parcelWidth = terrain.Size.X / ParcelColumns;
        float parcelHeight = terrain.Size.Y / ParcelRows;
        for (int column = 1; column < ParcelColumns; column++)
        {
            float x = terrain.Position.X + parcelWidth * column;
            DrawLine(
                new Vector2(x, terrain.Position.Y),
                new Vector2(x, terrain.End.Y),
                parcelLine,
                width: 4);
        }
        for (int row = 1; row < ParcelRows; row++)
        {
            float y = terrain.Position.Y + parcelHeight * row;
            DrawLine(
                new Vector2(terrain.Position.X, y),
                new Vector2(terrain.End.X, y),
                parcelLine,
                width: 4);
        }
    }

    private void OnResized()
    {
        ClampPan(Size);
        RebuildGround(CalculateTerrainRect(Size));
        QueueRedraw();
        RebuildTrees();
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton:
                if (mouseButton.Pressed)
                {
                    _panDragging = true;
                    _panDragStartMouse = mouseButton.Position;
                    _panDragStartOffset = PanOffset;
                }
                else
                {
                    _panDragging = false;
                }
                break;
            case InputEventMouseMotion mouseMotion when _panDragging:
                Vector2 dragDelta = mouseMotion.Position - _panDragStartMouse;
                if (dragDelta.Length() < PanDragThresholdPx) break;
                ApplyPan(_panDragStartOffset - dragDelta);
                break;
        }
    }

    /// <summary>
    /// Sets <see cref="PanOffset"/> (clamped to the current display size),
    /// then rebuilds this control's own presentation and notifies
    /// <see cref="PanChanged"/> subscribers so siblings whose positions were
    /// computed from the old offset (plots, lot-selection overlays, the hero
    /// anchor) reposition too.
    /// </summary>
    private void ApplyPan(Vector2 desired)
    {
        PanOffset = desired;
        ClampPan(Size);
        RebuildGround(CalculateTerrainRect(Size));
        QueueRedraw();
        RebuildTrees();
        EmitSignal(SignalName.PanChanged);
    }

    /// <summary>
    /// Clamps <see cref="PanOffset"/> to <c>[0, worldSize - displaySize]</c>
    /// per axis (0 when the fixed-size world already fits the display, since
    /// <see cref="CalculateTerrainRect"/> centers it in that case instead).
    /// </summary>
    private static void ClampPan(Vector2 controlSize)
    {
        float displayWidth = Mathf.Max(0, controlSize.X - 64);
        float displayHeight = Mathf.Max(
            0,
            controlSize.Y - TopHudReservedHeight - BottomHudReservedHeight);
        float worldWidth = ParcelColumns * ParcelPixelSize;
        float worldHeight = ParcelRows * ParcelPixelSize;
        float maxPanX = Mathf.Max(0, worldWidth - displayWidth);
        float maxPanY = Mathf.Max(0, worldHeight - displayHeight);
        PanOffset = new Vector2(
            Mathf.Clamp(PanOffset.X, 0, maxPanX),
            Mathf.Clamp(PanOffset.Y, 0, maxPanY));
    }

    public bool TryGetResourceGlobalPosition(
        int forestId,
        int unitId,
        out Vector2 globalPosition)
    {
        if (_resolvedTreePositions.TryGetValue(
            (forestId, unitId),
            out Vector2 localPosition))
        {
            globalPosition = GlobalPosition + localPosition;
            return true;
        }
        foreach (CityMacroSnapshot.PlotItem forest in _forests)
        {
            if (forest.Id.Value != forestId
                || unitId < 0
                || unitId >= forest.WoodUnitReserves.Count) continue;
            globalPosition = GlobalPosition + ResourceUnitCenter(forest, unitId);
            return true;
        }
        globalPosition = Vector2.Zero;
        return false;
    }

    public bool TryGetLogicalSlotGlobalPosition(
        int positionIndex,
        out Vector2 globalPosition)
    {
        if (positionIndex < 0)
        {
            globalPosition = Vector2.Zero;
            return false;
        }
        int current = 0;
        foreach (CityMacroSnapshot.PlotItem forest in _forests)
        {
            for (int unitId = 0; unitId < forest.WoodUnitReserves.Count; unitId++)
            {
                if (current++ != positionIndex) continue;
                globalPosition = GlobalPosition + ResourceUnitCenter(forest, unitId);
                return true;
            }
        }
        globalPosition = Vector2.Zero;
        return false;
    }

    public Vector2 GetLotGlobalCenter(ConstructionLot lot)
    {
        Rect2 parcel = CalculateParcelRect(
            Size,
            lot.ParcelColumn,
            lot.ParcelRow);
        Vector2 lotSize = parcel.Size / ParcelGrid.LotsPerAxis;
        return GlobalPosition
            + parcel.Position
            + new Vector2(
                (lot.LotColumn + 0.5f) * lotSize.X,
                (lot.LotRow + 0.5f) * lotSize.Y);
    }

    internal static int? FindPositionIndex(
        IReadOnlyList<CityMacroSnapshot.PlotItem> forests,
        int forestId,
        int unitId)
    {
        int positionIndex = 0;
        foreach (CityMacroSnapshot.PlotItem forest in forests)
        {
            for (int index = 0; index < forest.WoodUnitReserves.Count; index++)
            {
                if (forest.Id.Value == forestId && index == unitId)
                {
                    return positionIndex;
                }
                positionIndex++;
            }
        }
        return null;
    }

    private void RebuildTrees()
    {
        foreach (ResourceTree tree in _trees)
        {
            tree.ResourcePressed -= OnTreePressed;
            tree.QueueFree();
        }
        _trees.Clear();
        _resolvedTreePositions.Clear();
        if (TreeScene is null || _forests.Count == 0 || Size == Vector2.Zero) return;

        int positionIndex = 0;
        foreach (CityMacroSnapshot.PlotItem forest in _forests)
        {
            for (int unit = 0; unit < forest.WoodUnitReserves.Count; unit++, positionIndex++)
            {
                if (forest.WoodUnitReserves[unit] <= 0) continue;
                Vector2 center = ResourceUnitCenter(forest, unit);
                _resolvedTreePositions[(forest.Id.Value, unit)] = center;
                var tree = TreeScene.Instantiate<ResourceTree>();
                tree.Name = $"Tree_{forest.Id.Value}_{unit}";
                tree.Configure(forest.Id.Value, unit, positionIndex);
                tree.Position = center - new Vector2(24, 48);
                tree.ResourcePressed += OnTreePressed;
                AddChild(tree);
                MoveChild(tree, GetChildCount() - 2);
                _trees.Add(tree);
            }
        }
    }

    private Vector2 ResourceUnitCenter(
        CityMacroSnapshot.PlotItem forest,
        int unitId)
    {
        Rect2 parcel = CalculateParcelRect(
            Size,
            forest.ParcelColumn,
            forest.ParcelRow);
        (int column, int row) = ParcelGrid.NaturalResourceLot(unitId);
        Vector2 lotSize = parcel.Size / ParcelGrid.LotsPerAxis;
        return parcel.Position
            + new Vector2(
                (column + 0.5f) * lotSize.X,
                (row + 0.5f) * lotSize.Y);
    }

    private void OnTreePressed(int forestId, int unitId, Vector2 targetPosition)
    {
        CityMacroSnapshot.PlotItem? forest =
            _forests.FirstOrDefault(item => item.Id.Value == forestId);
        if (forest is null) return;
        _actionMenu.Open(
            forestId,
            unitId,
            targetPosition,
            targetPosition - GlobalPosition,
            _canGather,
            _gatherUnavailableReason);
    }

    private void OnGatherRequested(int forestId, int unitId, Vector2 targetPosition) =>
        EmitSignal(SignalName.GatherRequested, forestId, unitId, targetPosition);

    internal void ShowResourceMenuForVisualRegression()
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1"
            || _trees.Count == 0)
        {
            return;
        }
        ResourceTree tree = _trees[0];
        OnTreePressed(tree.ForestId, tree.UnitId, tree.GlobalPosition + tree.Size * 0.5f);
    }

    internal void StartGatherForVisualRegression()
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1"
            || _trees.Count == 0)
        {
            return;
        }
        ResourceTree tree = _trees[0];
        OnGatherRequested(
            tree.ForestId,
            tree.UnitId,
            tree.GlobalPosition + tree.Size * 0.5f);
    }
}
