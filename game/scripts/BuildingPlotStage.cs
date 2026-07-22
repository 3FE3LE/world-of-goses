#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Dynamic stage that renders one <see cref="BuildingPlot"/> per
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

    private readonly List<BuildingPlot> _plots = new();
    private CenterContainer _center = null!;
    private HBoxContainer _row = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = Control.MouseFilterEnum.Ignore;

        _center = new CenterContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_center);

        _row = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _row.AddThemeConstantOverride("separation", PresentationConstants.MacroPlotSpacing);
        _center.AddChild(_row);
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
    private static readonly HashSet<BuildingKind> KindsWithoutArtStillShown =
        new() { BuildingKind.Forest };

    public void Render(
        IReadOnlyList<CityMacroSnapshot.PlotItem> buildings,
        IReadOnlyList<CityMacroSnapshot.PlotItem> projects)
    {
        // Sort: in-progress first, then completed. Stable within each
        // group so the layout stays deterministic across loads.
        var sortedBuildings = SortPlots(buildings);
        var sortedProjects = SortPlots(projects);
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
            if (!wanted.Contains(plot.BuildingIdValue))
            {
                _row.RemoveChild(plot);
                plot.QueueFree();
                _plots.RemoveAt(i);
            }
        }

        foreach (var building in sortedBuildings) UpdatePlot(building);
        foreach (var project in sortedProjects) UpdatePlot(project);

        // Add plots for any new id in the desired order.
        var existing = new HashSet<int>(_plots.Select(p => p.BuildingIdValue));
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
        var plot = _plots.Find(candidate => candidate.BuildingIdValue == item.Id.Value);
        if (plot is null) return;
        plot.Configure(
            BuildingArt.GetTexturePath(item.Kind),
            item.DisplayName,
            item.IsUnderConstruction,
            item.Progress,
            item.RequiredWork,
            enabled: item.Enabled);
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
        if (BuildingArt.GetTexturePath(kind) is not null) return true;
        return KindsWithoutArtStillShown.Contains(kind);
    }

    private void AddPlot(CityMacroSnapshot.PlotItem item, string? texturePath)
    {
        var plot = new BuildingPlot
        {
            Name = $"Plot_{item.Id.Value}",
            BuildingIdValue = item.Id.Value,
        };
        // Configure BEFORE AddChild so the overlay/texture/text fields are
        // populated before _Ready runs (mirrors the constraint honored by
        // VisibleWorkerSlot / VisibleWorkerSlots).
        plot.Configure(
            texturePath,
            item.DisplayName,
            item.IsUnderConstruction,
            item.Progress,
            item.RequiredWork,
            enabled: item.Enabled);
        plot.BuildingClicked += emittedId =>
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
        _row.AddChild(plot);
        _plots.Add(plot);
    }

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
}
