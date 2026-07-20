#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;

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
    /// Buildings are rendered as finished plots; in-flight projects
    /// (with no corresponding building yet) are rendered as
    /// under-construction plots. Buildings for which
    /// <see cref="BuildingArt.GetTexturePath"/> returns null are
    /// skipped (Smithy and PotionLab today).
    /// </summary>
    public void Render(IReadOnlyList<Building> buildings, IReadOnlyList<ConstructionProject> projects)
    {
        // Map id -> project so we can look up by id when iterating plots.
        var projectsById = projects.ToDictionary(p => p.Id.Value);

        var wanted = new HashSet<int>();
        foreach (var b in buildings)
        {
            if (BuildingArt.GetTexturePath(b.Kind) is not null)
            {
                wanted.Add(b.Id.Value);
            }
        }
        foreach (var p in projects)
        {
            if (BuildingArt.GetTexturePath(p.ResultingKind) is not null)
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

        // Add plots for any new id.
        var existing = new HashSet<int>(_plots.Select(p => p.BuildingIdValue));
        foreach (var building in buildings)
        {
            if (existing.Contains(building.Id.Value)) continue;
            var texturePath = BuildingArt.GetTexturePath(building.Kind);
            if (texturePath is null) continue;
            AddPlot(building.Id.Value, building.Kind, building.DisplayName, texturePath, isUnderConstruction: false);
        }
        foreach (var project in projects)
        {
            if (existing.Contains(project.Id.Value)) continue;
            var texturePath = BuildingArt.GetTexturePath(project.ResultingKind);
            if (texturePath is null) continue;
            AddPlot(project.Id.Value, project.ResultingKind, project.DisplayName, texturePath, isUnderConstruction: true);
        }
    }

    private void AddPlot(int idValue, BuildingKind kind, string displayName, string texturePath, bool isUnderConstruction)
    {
        var plot = new BuildingPlot
        {
            Name = $"Plot_{idValue}",
            BuildingIdValue = idValue,
        };
        // Configure BEFORE AddChild so the overlay/texture/text fields are
        // populated before _Ready runs (mirrors the constraint honored by
        // VisibleWorkerSlot / VisibleWorkerSlots).
        plot.Configure(texturePath, displayName, isUnderConstruction);
        plot.BuildingClicked += emittedId => EmitSignal(SignalName.BuildingClicked, emittedId);
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