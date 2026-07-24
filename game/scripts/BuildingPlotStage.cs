#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Dynamic stage that renders one <see cref="MacroBuildingView"/> per
/// completed building and one per in-flight construction project that
/// does not yet have a building. Mirrors the pattern of
/// <see cref="VisibleWorkerSlots"/>: diffs the wanted set against
/// already-instantiated plots, adds and removes on each
/// <see cref="Render"/>.
///
/// Forwarding rule: a click on any plot emits <see cref="BuildingClicked"/>
/// with the configured <c>BuildingId.Value</c>; the controller routes
/// that to the matching detail view.
///
/// Composition: each plot is a self-contained <see cref="BuildingPlot"/>
/// control, anchored inside an <see cref="HBoxContainer"/> centred by a
/// <see cref="CenterContainer"/>. The whole stage is anchored to the
/// macro view with anchor preset 15 (full rect) so the inner layout
/// stays responsive to window resizes.
/// </summary>
public partial class BuildingPlotStage : Control
{
    [Signal] public delegate void BuildingClickedEventHandler(int buildingId);
    [Signal] public delegate void ProjectClickedEventHandler(int projectId);

    private readonly List<MacroBuildingView> _plots = new();
    private readonly Dictionary<int, CityMacroSnapshot.PlotItem> _items = new();

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = Control.MouseFilterEnum.Ignore;
        Resized += RepositionPlots;
    }

    public override void _ExitTree()
    {
        Resized -= RepositionPlots;
    }

    /// <summary>
    /// Reconciles the stage against the current buildings and projects.
    /// Buildings with art are rendered as finished plots; in-flight
    /// projects (with no corresponding building yet) are rendered as
    /// under-construction plots. Buildings for which
    /// <see cref="BuildingArt.GetTexturePath"/> returns null fall back
    /// to a placeholder plot so the player can still reach them — see
    /// <see cref="BuildingPlot.Configure"/> for the placeholder style.
    /// The placeholder list below names every kind we explicitly want
    /// to keep visible while art is missing; kinds not in this list
    /// (Smithy, PotionLab) remain hidden so the city stays accurate
    /// when a future slice wires their art.
    /// </summary>
    private static readonly HashSet<BuildingKind> KindsWithoutArtStillShown = new();

    public void Render(
        IReadOnlyList<CityMacroSnapshot.PlotItem> buildings,
        IReadOnlyList<CityMacroSnapshot.PlotItem> projects)
    {
        // Sort: in-progress first, then completed. Stable within each
        // group so the layout stays deterministic across loads.
        var sortedBuildings = SortPlots(buildings);
        var sortedProjects = SortPlots(projects);
        _items.Clear();
        foreach (CityMacroSnapshot.PlotItem item in sortedBuildings)
        {
            _items[item.Id.Value] = item;
        }
        foreach (CityMacroSnapshot.PlotItem item in sortedProjects)
        {
            _items[item.Id.Value] = item;
        }
        var wanted = new HashSet<int>();
        foreach (var b in sortedBuildings)
        {
            if (IsPlottable(b.Kind))
            {
                wanted.Add(b.Id.Value);
            }
        }
        foreach (var p in sortedProjects)
        {
            if (IsPlottable(p.Kind))
            {
                wanted.Add(p.Id.Value);
            }
        }

        // Remove plots whose ids no longer exist.
        for (int i = _plots.Count - 1; i >= 0; i--)
        {
            var plot = _plots[i];
            if (!wanted.Contains(plot.EntityId))
            {
                RemoveChild(plot);
                plot.QueueFree();
                _plots.RemoveAt(i);
            }
        }

        foreach (var building in sortedBuildings) UpdatePlot(building);
        foreach (var project in sortedProjects) UpdatePlot(project);

        // Add plots for any new id in the desired order.
        var existing = new HashSet<int>(_plots.Select(p => p.EntityId));
        foreach (var building in sortedBuildings)
        {
            if (existing.Contains(building.Id.Value)) continue;
            if (!IsPlottable(building.Kind)) continue;
            var texturePath = BuildingArt.GetTexturePath(building.Kind);
            AddPlot(building, texturePath);
        }
        foreach (var project in sortedProjects)
        {
            if (existing.Contains(project.Id.Value)) continue;
            if (!IsPlottable(project.Kind)) continue;
            var texturePath = BuildingArt.GetTexturePath(project.Kind);
            AddPlot(project, texturePath);
        }
        RepositionPlots();
    }

    /// <summary>
    /// Sorts plots so under-construction items lead the row. The
    /// relative order within each group is preserved (stable) so the
    /// city keeps a deterministic layout across loads.
    /// </summary>
    private static List<CityMacroSnapshot.PlotItem> SortPlots(
        IReadOnlyList<CityMacroSnapshot.PlotItem> source)
    {
        var sorted = new List<CityMacroSnapshot.PlotItem>(source.Count);
        foreach (var item in source)
        {
            if (item.IsUnderConstruction) sorted.Add(item);
        }
        foreach (var item in source)
        {
            if (!item.IsUnderConstruction) sorted.Add(item);
        }
        return sorted;
    }

    private void UpdatePlot(CityMacroSnapshot.PlotItem item)
    {
        var plot = _plots.Find(candidate => candidate.EntityId == item.Id.Value);
        if (plot is null) return;
        plot.Configure(item);
    }

    /// <summary>
    /// True when a <see cref="BuildingKind"/> should produce a
    /// <see cref="BuildingPlot"/>. Real-art kinds are always shown;
    /// kinds in <see cref="KindsWithoutArtStillShown"/> are shown via
    /// placeholder; everything else (Smithy, PotionLab today) is hidden
    /// so the city never references an unrepresented building.
    /// </summary>
    private static bool IsPlottable(BuildingKind kind)
    {
        // Natural Forest resources are represented by interactive trees in
        // OrthogonalParcelTerrain, not by building cards or detail-view plots.
        if (kind == BuildingKind.Forest) return false;
        if (BuildingArt.GetTexturePath(kind) is not null) return true;
        return KindsWithoutArtStillShown.Contains(kind);
    }

    private void AddPlot(CityMacroSnapshot.PlotItem item, string? texturePath)
    {
        var plot = new MacroBuildingView
        {
            Name = $"Plot_{item.Id.Value}",
        };
        _ = texturePath;
        plot.Configure(item);
        plot.Activated += emittedId =>
        {
            if (plot.IsUnderConstruction)
            {
                EmitSignal(SignalName.ProjectClicked, emittedId);
            }
            else
            {
                EmitSignal(SignalName.BuildingClicked, emittedId);
            }
        };
        plot.AddToGroup(PresentationConstants.GroupBuildingPlot);
        AddChild(plot);
        _plots.Add(plot);
    }

    private void RepositionPlots()
    {
        int fallbackIndex = 0;
        foreach (MacroBuildingView plot in _plots)
        {
            if (!_items.TryGetValue(
                plot.EntityId,
                out CityMacroSnapshot.PlotItem? item)
                || item.ParcelId is null)
            {
                var fallback = new Rect2(
                    new Vector2(
                    fallbackIndex++ * (
                        PresentationConstants.MacroPlotSize
                        + PresentationConstants.MacroPlotSpacing),
                    0),
                    new Vector2(
                        PresentationConstants.MacroPlotSize,
                        PresentationConstants.MacroPlotSize));
                plot.SetPlacement(fallback, new Rect2(Vector2.Zero, fallback.Size));
                continue;
            }
            Rect2 placementRect = CalculatePlacementRect(Size, item);
            Rect2 solidLocalRect = CalculateSolidLocalRect(placementRect.Size, item);
            plot.SetPlacement(placementRect, solidLocalRect);
        }
        _plots.Sort((left, right) =>
        {
            CityMacroSnapshot.PlotItem leftItem = _items[left.EntityId];
            CityMacroSnapshot.PlotItem rightItem = _items[right.EntityId];
            int row = leftItem.ParcelRow.CompareTo(rightItem.ParcelRow);
            if (row != 0) return row;
            row = leftItem.LotRow.CompareTo(rightItem.LotRow);
            if (row != 0) return row;
            return leftItem.LotColumn.CompareTo(rightItem.LotColumn);
        });
        for (int index = 0; index < _plots.Count; index++)
        {
            MoveChild(_plots[index], index);
        }
    }

    internal static Rect2 CalculatePlacementRect(
        Vector2 viewportSize,
        CityMacroSnapshot.PlotItem item)
    {
        Rect2 parcel = OrthogonalParcelTerrain.CalculateParcelRect(
            viewportSize,
            item.ParcelColumn,
            item.ParcelRow);
        Vector2 lotSize = parcel.Size / ParcelGrid.LotsPerAxis;
        return new Rect2(
            parcel.Position + new Vector2(
                item.LotColumn * lotSize.X,
                item.LotRow * lotSize.Y),
            new Vector2(
                item.LotWidth * lotSize.X,
                item.LotHeight * lotSize.Y));
    }

    internal static Rect2 CalculateSolidLocalRect(
        Vector2 reservedPixelSize,
        CityMacroSnapshot.PlotItem item)
    {
        BuildingFootprintTemplate template =
            BuildingFootprintCatalog.Get(item.FootprintProfileId);
        HalfTileRect solid = RotateSolid(
            template.SolidArea,
            template.ReservedArea.Width,
            template.ReservedArea.Height,
            item.Orientation);
        float unitX = reservedPixelSize.X / template.ReservedArea.Width;
        float unitY = reservedPixelSize.Y / template.ReservedArea.Height;
        return new Rect2(
            new Vector2(solid.X * unitX, solid.Y * unitY),
            new Vector2(solid.Width * unitX, solid.Height * unitY));
    }

    internal static HalfTileRect RotateSolid(
        HalfTileRect solid,
        int reservedWidth,
        int reservedHeight,
        BuildingOrientation orientation) => orientation switch
    {
        BuildingOrientation.North => new HalfTileRect(
            reservedWidth - solid.Right,
            reservedHeight - solid.Bottom,
            solid.Width,
            solid.Height),
        BuildingOrientation.West => new HalfTileRect(
            solid.Y,
            reservedWidth - solid.Right,
            solid.Height,
            solid.Width),
        BuildingOrientation.East => new HalfTileRect(
            reservedHeight - solid.Bottom,
            solid.X,
            solid.Height,
            solid.Width),
        _ => solid,
    };

    /// <summary>
    /// Pure helper exposed for unit tests: given an existing set of
    /// plot ids and a wanted set, returns the ids to add and the ids
    /// to remove. The runtime <see cref="Render"/> method does this
    /// in-place; tests cover the algorithm without needing a
    /// <see cref="SceneTree"/>.
    /// </summary>
    internal static void DiffEntries(
        IReadOnlyList<int> existing,
        IReadOnlyList<int> wanted,
        out List<int> toAdd,
        out List<int> toRemove)
    {
        var wantedSet = new HashSet<int>(wanted);
        var existingSet = new HashSet<int>(existing);
        toAdd = wanted.Where(id => !existingSet.Contains(id)).ToList();
        toRemove = existing.Where(id => !wantedSet.Contains(id)).ToList();
    }

    public IReadOnlyList<Rect2> GetOccupiedGlobalRects()
    {
        var occupied = new List<Rect2>(_plots.Count);
        foreach (MacroBuildingView plot in _plots)
        {
            if (!plot.Visible || !plot.IsVisibleInTree()) continue;
            occupied.Add(plot.GetSolidGlobalRect());
        }
        return occupied;
    }

    public bool TryGetEntityGlobalPosition(
        BuildingId entityId,
        out Vector2 globalPosition)
    {
        MacroBuildingView? plot = _plots.Find(
            candidate => candidate.EntityId == entityId.Value);
        if (plot is null)
        {
            globalPosition = Vector2.Zero;
            return false;
        }
        globalPosition = plot.GetGlobalRect().GetCenter();
        return true;
    }
}
