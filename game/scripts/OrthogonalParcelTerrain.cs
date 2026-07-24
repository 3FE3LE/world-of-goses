#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Presentation-only orthogonal ground for the macro city. It establishes the
/// parcel grid and a deterministic scattering of provisional trees without
/// turning terrain art into simulation state.
/// </summary>
public partial class OrthogonalParcelTerrain : Control
{
    private const string TerrainAtlasPath =
        "res://assets/terrain/kenney/roguelike-rpg/roguelike_sheet_transparent.png";
    private const int SourceTileSize = 16;
    private const int DisplayTileSize = 32;
    private const int ParcelColumns = 4;
    private const int ParcelRows = 2;

    private Texture2D _atlas = null!;
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
        MouseFilter = MouseFilterEnum.Ignore;
        _atlas = ResourceLoader.Load<Texture2D>(TerrainAtlasPath)
            ?? throw new InvalidOperationException(
                $"Could not load orthogonal terrain atlas at {TerrainAtlasPath}.");
        _actionMenu = GetNode<ResourceActionMenu>("ResourceActionMenu");
        _actionMenu.GatherRequested += OnGatherRequested;
        Resized += OnResized;
        QueueRedraw();
    }

    public override void _ExitTree()
    {
        Resized -= OnResized;
        if (_actionMenu is not null) _actionMenu.GatherRequested -= OnGatherRequested;
    }

    public override void _Draw()
    {
        Rect2 terrain = CalculateTerrainRect(Size);
        DrawRect(terrain.Grow(6), new Color("#14221f"));

        int columns = Mathf.FloorToInt(terrain.Size.X / DisplayTileSize);
        int rows = Mathf.FloorToInt(terrain.Size.Y / DisplayTileSize);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                bool alternate = (column * 3 + row * 5) % 11 == 0;
                Rect2 source = AtlasTile(alternate ? 10 : 9, 25);
                Rect2 destination = new(
                    terrain.Position + new Vector2(
                        column * DisplayTileSize,
                        row * DisplayTileSize),
                    new Vector2(DisplayTileSize, DisplayTileSize));
                DrawTextureRectRegion(
                    _atlas,
                    destination,
                    source,
                    modulate: new Color(0.72f, 0.72f, 0.58f));
            }
        }

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

    internal static Rect2 CalculateTerrainRect(Vector2 viewportSize)
    {
        float width = Mathf.Max(0, viewportSize.X - 64);
        float height = Mathf.Max(0, viewportSize.Y - 72);
        width = Mathf.Floor(width / (DisplayTileSize * ParcelColumns))
            * DisplayTileSize * ParcelColumns;
        height = Mathf.Floor(height / (DisplayTileSize * ParcelRows))
            * DisplayTileSize * ParcelRows;
        return new Rect2(
            new Vector2(
                Mathf.Floor((viewportSize.X - width) * 0.5f),
                Mathf.Floor((viewportSize.Y - height) * 0.5f) + 12),
            new Vector2(width, height));
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

    private static Rect2 AtlasTile(int column, int row) =>
        new(
            column * (SourceTileSize + 1),
            row * (SourceTileSize + 1),
            SourceTileSize,
            SourceTileSize);

    private void DrawParcelGrid(Rect2 terrain)
    {
        Color border = new("#395547");
        Color inner = new("#6b7654");
        DrawRect(terrain, border, filled: false, width: 4);

        float parcelWidth = terrain.Size.X / ParcelColumns;
        float parcelHeight = terrain.Size.Y / ParcelRows;
        for (int column = 1; column < ParcelColumns; column++)
        {
            float x = terrain.Position.X + parcelWidth * column;
            DrawLine(
                new Vector2(x, terrain.Position.Y),
                new Vector2(x, terrain.End.Y),
                inner,
                width: 2);
        }
        for (int row = 1; row < ParcelRows; row++)
        {
            float y = terrain.Position.Y + parcelHeight * row;
            DrawLine(
                new Vector2(terrain.Position.X, y),
                new Vector2(terrain.End.X, y),
                inner,
                width: 2);
        }
    }

    private void OnResized()
    {
        QueueRedraw();
        RebuildTrees();
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
            forest.WoodUnitReserves[unitId],
            forest.TicksUntilRegeneration,
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
