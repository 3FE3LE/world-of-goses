#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

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
    [Export] public NodePath ModalHostPath { get; set; } = "ModalHost";
    [Export] public NodePath EmptyPanelPath { get; set; } = "Center/EmptyPanel";
    [Export] public NodePath HeroProfileButtonPath { get; set; } =
        "Center/EmptyPanel/Margin/Content/HeroProfileButton";
    [Export] public NodePath PlotStagePath { get; set; } = "BuildingPlotStage";
    [Export] public NodePath ConstructionMenuButtonPath { get; set; } = "../ConstructionMenuButton";

    private CityWorldController _controller = null!;
    private MacroCitizenActivity _activity = null!;
    private CityStatusPanel _statusPanel = null!;
    private OfflineReportPanel _offlineReport = null!;
    private ModalHost _modalHost = null!;
    private ConstructionPanel _constructionPanel = null!;
    private PanelContainer _emptyPanel = null!;
    private Button _heroProfileButton = null!;
    private BuildingPlotStage _plotStage = null!;
    private bool _offlineReportShown;
    private IconButton _constructionMenuButton = null!;
    private bool _modalWantsOpen;
    private MacroMode? _lastMacroMode;

    public override void _Ready()
    {
        _controller = GetParent().GetNode<CityWorldController>("CityWorldController");
        _activity = GetNode<MacroCitizenActivity>(ActivityPath);
        _statusPanel = GetNode<CityStatusPanel>(StatusPanelPath);
        _offlineReport = GetNode<OfflineReportPanel>(OfflineReportPath);
        _modalHost = GetNode<ModalHost>(ModalHostPath);
        _constructionPanel = GetNode<ConstructionPanel>("Center/ConstructionPanel");
        _emptyPanel = GetNode<PanelContainer>(EmptyPanelPath);
        _heroProfileButton = GetNode<Button>(HeroProfileButtonPath);
        _plotStage = GetNode<BuildingPlotStage>(PlotStagePath);
        _constructionMenuButton = GetNode<IconButton>(ConstructionMenuButtonPath);

        _controller.BuildingStateChanged += OnAnyBuildingStateChanged;
        _controller.ProjectStateChanged += OnAnyProjectStateChanged;
        _controller.WorldTickAdvanced += OnWorldTickAdvanced;
        _controller.SelectionChanged += OnSelectionChanged;
        _controller.HeroCreated += OnHeroCreated;
        _plotStage.BuildingClicked += OnPlotBuildingClicked;
        _plotStage.ProjectClicked += OnPlotProjectClicked;
        _heroProfileButton.Pressed += OnHeroProfilePressed;
        _constructionMenuButton.Pressed += OnConstructionMenuPressed;
        _constructionPanel.CloseRequested += OnConstructionPanelCloseRequested;
        _modalHost.Closed += OnModalHostClosed;

        _heroProfileButton.FocusNeighborRight = _constructionMenuButton.GetPath();
        _constructionMenuButton.FocusNeighborLeft = _heroProfileButton.GetPath();

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
            _plotStage.ProjectClicked -= OnPlotProjectClicked;
        }
        if (_heroProfileButton is not null)
        {
            _heroProfileButton.Pressed -= OnHeroProfilePressed;
        }
        if (_constructionMenuButton is not null)
        {
            _constructionMenuButton.Pressed -= OnConstructionMenuPressed;
        }
        if (_constructionPanel is not null)
        {
            _constructionPanel.CloseRequested -= OnConstructionPanelCloseRequested;
        }
        if (_modalHost is not null)
        {
            _modalHost.Closed -= OnModalHostClosed;
        }
    }

    /// <summary>
    /// Resets the desired state when the host emits Closed, so the
    /// next refresh does not auto-reopen. Auto-open policy still
    /// applies when <see cref="Refresh"/> runs again with a new mode.
    /// </summary>
    private void OnModalHostClosed()
    {
        _modalWantsOpen = false;
        var snapshot = _controller.GetCityMacroSnapshot();
        UpdateConstructionMenuButton(DetermineMacroMode(
            snapshot.Buildings.Count,
            snapshot.Projects.Count));
    }

    public void OnReturnedToCity()
    {
        if (_controller.NeedsOnboarding()) return;
        Show();
        Refresh();
    }

    private void Refresh()
    {
        var snapshot = _controller.GetCityMacroSnapshot();
        _statusPanel.Refresh(_controller);
        _activity.Hero = snapshot.Hero;
        _activity.Populate(
            snapshot.Citizens,
            snapshot.Buildings.Count,
            snapshot.Projects.Count);
        _constructionPanel.Refresh();

        var mode = DetermineMacroMode(
            snapshot.Buildings.Count,
            snapshot.Projects.Count);

        _emptyPanel.Visible = mode == MacroMode.Empty;
        // Preserve the player's explicit open/closed choice across world
        // ticks. Only a real macro-mode transition may change it
        // automatically; production updates in the same mode must not
        // close an open construction menu.
        _modalWantsOpen = ResolveModalIntent(_lastMacroMode, mode, _modalWantsOpen);
        _lastMacroMode = mode;
        SyncModalHost();
        // The worksite is part of the city state, not modal content. Keep it
        // rendered while Construction progress is open; ModalHost still owns
        // input through its scrim, but assignment refreshes no longer make the
        // Quarry/Farm visually disappear.
        _plotStage.Visible = mode is MacroMode.Plots or MacroMode.PlotsAndConstruction;
        _constructionMenuButton.Visible = mode is MacroMode.Plots
            or MacroMode.PlotsAndConstruction
            or MacroMode.Empty
            or MacroMode.Construction;
        UpdateConstructionMenuButton(mode);
        EnsureDefaultFocus(mode);

        if (mode is MacroMode.Plots or MacroMode.PlotsAndConstruction)
        {
            _plotStage.Render(snapshot.Buildings, snapshot.Projects);
        }
        else
        {
            _plotStage.Render(
                Array.Empty<CityMacroSnapshot.PlotItem>(),
                Array.Empty<CityMacroSnapshot.PlotItem>());
        }

        if (!_offlineReportShown
            && _controller.LastOfflineReport is { HadProgression: true } report)
        {
            _offlineReport.ShowReport(report);
            _offlineReportShown = true;
        }
        else
        {
            _offlineReport.ShowLog(snapshot.Events);
        }
    }

    /// <summary>
    /// Reconciles the host's state with <see cref="_modalWantsOpen"/>.
    /// When the player dismisses the modal (X / ESC / scrim), the
    /// host's <see cref="ModalHost.Closed"/> signal flips the flag
    /// back to false via <see cref="OnModalHostClosed"/> so the modal
    /// does not auto-reopen on the next refresh.
    /// </summary>
    private void SyncModalHost()
    {
        if (_modalWantsOpen)
        {
            if (!_modalHost.IsOpen)
            {
                _modalHost.Open(_constructionPanel);
            }
        }
        else
        {
            if (_modalHost.IsOpen)
            {
                _modalHost.Close();
            }
            else
            {
                _constructionPanel.Visible = false;
            }
        }
    }

    private void OnConstructionMenuPressed()
    {
        if (_modalHost.IsOpen)
        {
            _modalWantsOpen = false;
            _modalHost.Close();
        }
        else
        {
            _modalWantsOpen = true;
            SyncModalHost();
        }
    }

    private void OnConstructionPanelCloseRequested()
    {
        _modalWantsOpen = false;
        _modalHost.Close();
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

    internal static bool ResolveModalIntent(
        MacroMode? previousMode,
        MacroMode currentMode,
        bool currentIntent)
    {
        if (previousMode == currentMode) return currentIntent;
        if (currentMode == MacroMode.Construction) return true;
        if (previousMode == MacroMode.Construction) return false;
        return currentIntent;
    }

    private void OnHeroCreated(int citizenId)
    {
        Show();
        Refresh();
    }

    private void OnHeroProfilePressed() => _controller.SelectHero();

    private void UpdateConstructionMenuButton(MacroMode mode)
    {
        if (_modalHost.IsOpen)
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

    private void OnPlotProjectClicked(int projectId)
    {
        _modalWantsOpen = true;
        _constructionPanel.Refresh();
        SyncModalHost();
        var snapshot = _controller.GetCityMacroSnapshot();
        UpdateConstructionMenuButton(DetermineMacroMode(
            snapshot.Buildings.Count,
            snapshot.Projects.Count));
    }

    private void EnsureDefaultFocus(MacroMode mode)
    {
        Control? focused = GetViewport().GuiGetFocusOwner();
        if (focused is not null && focused.IsVisibleInTree()) return;
        if (_constructionMenuButton.Visible && mode != MacroMode.Empty)
        {
            _constructionMenuButton.GrabFocus();
        }
        else
        {
            _heroProfileButton.GrabFocus();
        }
    }

    private void OnAnyBuildingStateChanged(int buildingId) => Refresh();

    private void OnAnyProjectStateChanged(int projectId) => Refresh();

    private void OnWorldTickAdvanced(int tick)
    {
        _statusPanel.Refresh(_controller);
        _offlineReport.ShowLog(_controller.GetCityMacroSnapshot().Events);
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
            // Any selection away from the macro view must release the
            // modal so the next non-macro selection isn't blocked by a
            // stale construction panel sitting on top.
            if (_modalHost is not null && _modalHost.IsOpen)
            {
                _modalWantsOpen = false;
                _modalHost.Close();
            }
            _constructionMenuButton.Visible = false;
            Hide();
        }
    }
}
