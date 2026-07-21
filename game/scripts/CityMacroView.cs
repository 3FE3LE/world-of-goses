using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Macro city view. A newly founded world legitimately contains one hero and
/// no buildings, so this view renders that empty state without manufacturing
/// production plots or treating the save as broken.
///
/// When a building exists it is rendered through a <see cref="BuildingPlotStage"/>
/// positioned under the centred panels. In-flight construction projects are
/// rendered alongside completed buildings so the player can see the city
/// already-built plots plus the worksite currently under construction.
/// </summary>
public partial class CityMacroView : Control
{
    /// <summary>
    /// Resolved by <see cref="Refresh"/> from the world state and used
    /// to drive both the panel visibility and the plot stage render.
    /// The four cases are exhaustive for the current slice; expand
    /// here if a new state (e.g. expedition-only view) is introduced.
    /// </summary>
    public enum MacroMode
    {
        Empty,
        Construction,
        Plots,
        PlotsAndConstruction,
    }

    [Export] public NodePath ActivityPath { get; set; } = "MacroCitizenActivity";
    [Export] public NodePath StatusPanelPath { get; set; } = "../CityStatusPanel";
    [Export] public NodePath OfflineReportPath { get; set; } = "OfflineReportPanel";
    [Export] public NodePath ConstructionPanelPath { get; set; } = "Center/ConstructionPanel";
    [Export] public NodePath EmptyPanelPath { get; set; } = "Center/EmptyPanel";
    [Export] public NodePath HeroProfileButtonPath { get; set; } =
        "Center/EmptyPanel/Margin/Content/HeroProfileButton";
    [Export] public NodePath PlotStagePath { get; set; } = "BuildingPlotStage";
    [Export] public NodePath ConstructionMenuButtonPath { get; set; } = "../ConstructionMenuButton";
    [Export] public NodePath ConstructionScrimPath { get; set; } = "ConstructionModalScrim";

    private CityWorldController _controller = null!;
    private MacroCitizenActivity _activity = null!;
    private CityStatusPanel _statusPanel = null!;
    private OfflineReportPanel _offlineReport = null!;
    private ConstructionPanel _constructionPanel = null!;
    private PanelContainer _emptyPanel = null!;
    private Button _heroProfileButton = null!;
    private BuildingPlotStage _plotStage = null!;
    private bool _offlineReportShown;
    private IconButton _constructionMenuButton = null!;
    private bool _constructionMenuOpen;
    private ColorRect _constructionScrim = null!;

    public override void _Ready()
    {
        _controller = GetParent().GetNode<CityWorldController>("CityWorldController");
        _activity = GetNode<MacroCitizenActivity>(ActivityPath);
        _statusPanel = GetNode<CityStatusPanel>(StatusPanelPath);
        _offlineReport = GetNode<OfflineReportPanel>(OfflineReportPath);
        _constructionPanel = GetNode<ConstructionPanel>(ConstructionPanelPath);
        _emptyPanel = GetNode<PanelContainer>(EmptyPanelPath);
        _heroProfileButton = GetNode<Button>(HeroProfileButtonPath);
        _plotStage = GetNode<BuildingPlotStage>(PlotStagePath);
        _constructionMenuButton = GetNode<IconButton>(ConstructionMenuButtonPath);
        _constructionScrim = GetNode<ColorRect>(ConstructionScrimPath);

        _controller.BuildingStateChanged += OnAnyBuildingStateChanged;
        _controller.ProjectStateChanged += OnAnyProjectStateChanged;
        _controller.WorldTickAdvanced += OnWorldTickAdvanced;
        _controller.SelectionChanged += OnSelectionChanged;
        _controller.HeroCreated += OnHeroCreated;
        _plotStage.BuildingClicked += OnPlotBuildingClicked;
        _heroProfileButton.Pressed += OnHeroProfilePressed;
        _constructionMenuButton.Pressed += OnConstructionMenuPressed;

        Visible = !_controller.NeedsOnboarding();
        if (Visible) Refresh();
    }

    public override void _ExitTree()
    {
        if (_controller is not null)
        {
            _controller.BuildingStateChanged -= OnAnyBuildingStateChanged;
            _controller.ProjectStateChanged -= OnAnyProjectStateChanged;
            _controller.WorldTickAdvanced -= OnWorldTickAdvanced;
            _controller.SelectionChanged -= OnSelectionChanged;
            _controller.HeroCreated -= OnHeroCreated;
        }
        if (_plotStage is not null)
        {
            _plotStage.BuildingClicked -= OnPlotBuildingClicked;
        }
        if (_heroProfileButton is not null)
        {
            _heroProfileButton.Pressed -= OnHeroProfilePressed;
        }
        if (_constructionMenuButton is not null)
        {
            _constructionMenuButton.Pressed -= OnConstructionMenuPressed;
        }
    }

    public void OnReturnedToCity()
    {
        if (_controller.NeedsOnboarding()) return;
        Show();
        Refresh();
    }

    private void Refresh()
    {
        _statusPanel.Refresh(_controller);
        _activity.Hero = _controller.HeroOrNull();
        _activity.Populate(
            _controller.Citizens().Count,
            _controller.World.Buildings.Count,
            _controller.World.Projects.Count);
        _constructionPanel.Refresh();

        var mode = DetermineMacroMode(
            _controller.World.Buildings.Count,
            _controller.World.Projects.Count);

        _emptyPanel.Visible = mode == MacroMode.Empty;
        bool constructionPanelVisible = mode == MacroMode.Construction || _constructionMenuOpen;
        _constructionPanel.Visible = constructionPanelVisible;
        _constructionScrim.Visible = constructionPanelVisible;
        _plotStage.Visible = !constructionPanelVisible
            && (mode is MacroMode.Plots or MacroMode.PlotsAndConstruction);
        _constructionMenuButton.Visible = mode is MacroMode.Plots
            or MacroMode.PlotsAndConstruction
            or MacroMode.Empty;
        UpdateConstructionMenuButton(mode);

        if (mode is MacroMode.Plots or MacroMode.PlotsAndConstruction)
        {
            _plotStage.Render(
                _controller.World.Buildings.Values.ToList(),
                _controller.World.Projects.Values.ToList());
        }
        else
        {
            _plotStage.Render(Array.Empty<Building>(), Array.Empty<ConstructionProject>());
        }

        if (!_offlineReportShown
            && _controller.LastOfflineReport is { HadProgression: true } report)
        {
            _offlineReport.ShowReport(report);
            _offlineReportShown = true;
        }
        else
        {
            _offlineReport.ShowLog(_controller.World.Log.Events);
        }
    }

    /// <summary>
    /// Pure decision function exposed for unit tests. Decides which
    /// combination of panels and plot stage should be visible given
    /// the current world state.
    /// </summary>
    public static MacroMode DetermineMacroMode(int buildingCount, int projectCount)
    {
        bool hasBuildings = buildingCount > 0;
        bool hasProjects = projectCount > 0;

        if (!hasBuildings && !hasProjects) return MacroMode.Empty;
        if (!hasBuildings && hasProjects) return MacroMode.Construction;
        if (hasBuildings && !hasProjects) return MacroMode.Plots;
        return MacroMode.PlotsAndConstruction;
    }

    private void OnHeroCreated(int citizenId)
    {
        Show();
        Refresh();
    }

    private void OnHeroProfilePressed() => _controller.SelectHero();

    private void OnConstructionMenuPressed()
    {
        _constructionMenuOpen = !_constructionMenuOpen;
        _constructionMenuButton.SetIconAndLabel(
            _constructionMenuOpen ? IconPaths.Close : IconPaths.Building,
            _constructionMenuOpen ? "Close construction" : "Construction");
        Refresh();
    }

    private void UpdateConstructionMenuButton(MacroMode mode)
    {
        if (_constructionMenuOpen)
        {
            _constructionMenuButton.SetIconAndLabel(IconPaths.Close, "Close construction");
            return;
        }

        (string icon, string label) = mode switch
        {
            MacroMode.Empty => (IconPaths.House, "Build shelter"),
            MacroMode.Construction => (IconPaths.Building, "Construction progress"),
            MacroMode.PlotsAndConstruction => (IconPaths.Building, "Construction progress"),
            _ => (IconPaths.Plus, "Construction"),
        };
        _constructionMenuButton.SetIconAndLabel(icon, label);
    }

    private void OnPlotBuildingClicked(int buildingId) =>
        _controller.SelectBuilding(new BuildingId(buildingId));

    private void OnAnyBuildingStateChanged(int buildingId) => Refresh();

    private void OnAnyProjectStateChanged(int projectId) => Refresh();

    private void OnWorldTickAdvanced(int tick)
    {
        _statusPanel.Refresh(_controller);
        _offlineReport.ShowLog(_controller.World.Log.Events);
    }

    private void OnSelectionChanged(int selectionState)
    {
        if ((CityWorldController.Selection)selectionState == CityWorldController.Selection.MacroView
            && !_controller.NeedsOnboarding())
        {
            Show();
            Refresh();
        }
        else
        {
            Hide();
        }
    }
}
