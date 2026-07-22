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
    [Export] public NodePath ControllerPath { get; set; } = "../../../CityWorldController";
    [Export] public NodePath StatusPanelPath { get; set; } = "../../CityStatusPanel";
    [Export] public NodePath OfflineReportPath { get; set; } = "OfflineReportPanel";
    [Export] public NodePath ModalHostPath { get; set; } = "ModalHost";
    [Export] public NodePath EmptyPanelPath { get; set; } = "Center/EmptyPanel";
    [Export] public NodePath EmptyGuidanceLabelPath { get; set; } =
        "Center/EmptyPanel/Margin/Content/GuidanceLabel";
    [Export] public NodePath GatherWoodButtonPath { get; set; } =
        "Center/EmptyPanel/Margin/Content/GatherWoodButton";
    [Export] public NodePath HeroAccessButtonPath { get; set; } = "../MacroActions/Actions/HeroAccessButton";
    [Export] public NodePath PlotStagePath { get; set; } = "BuildingPlotStage";
    [Export] public NodePath ConstructionMenuButtonPath { get; set; } = "../MacroActions/Actions/ConstructionMenuButton";
    [Export] public NodePath AttentionBannerPath { get; set; } = "../../../AttentionBanner";

    private CityWorldController _controller = null!;
    private MacroCitizenActivity _activity = null!;
    private CityStatusPanel _statusPanel = null!;
    private OfflineReportPanel _offlineReport = null!;
    private ModalHost _modalHost = null!;
    private ConstructionPanel _constructionPanel = null!;
    private PanelContainer _emptyPanel = null!;
    private Label _emptyGuidanceLabel = null!;
    private Button _gatherWoodButton = null!;
    private HeroAccessButton _heroAccessButton = null!;
    private BuildingPlotStage _plotStage = null!;
    private bool _offlineReportShown;
    private IconButton _constructionMenuButton = null!;
    private AttentionBanner _attentionBanner = null!;
    private bool _modalWantsOpen;
    private MacroMode? _lastMacroMode;

    public override void _Ready()
    {
        _controller = GetNode<CityWorldController>(ControllerPath);
        _activity = GetNode<MacroCitizenActivity>(ActivityPath);
        _statusPanel = GetNode<CityStatusPanel>(StatusPanelPath);
        _statusPanel.AttachController(_controller);
        _offlineReport = GetNode<OfflineReportPanel>(OfflineReportPath);
        _offlineReport.SetController(_controller);
        _modalHost = GetNode<ModalHost>(ModalHostPath);
        _constructionPanel = GetNode<ConstructionPanel>("Center/ConstructionPanel");
        _emptyPanel = GetNode<PanelContainer>(EmptyPanelPath);
        _emptyGuidanceLabel = GetNode<Label>(EmptyGuidanceLabelPath);
        _gatherWoodButton = GetNode<Button>(GatherWoodButtonPath);
        _heroAccessButton = GetNode<HeroAccessButton>(HeroAccessButtonPath);
        _plotStage = GetNode<BuildingPlotStage>(PlotStagePath);
        _constructionMenuButton = GetNode<IconButton>(ConstructionMenuButtonPath);
        _attentionBanner = GetNode<AttentionBanner>(AttentionBannerPath);

        _controller.BuildingStateChanged += OnAnyBuildingStateChanged;
        _controller.ProjectStateChanged += OnAnyProjectStateChanged;
        _controller.WorldTickAdvanced += OnWorldTickAdvanced;
        _controller.SelectionChanged += OnSelectionChanged;
        _controller.HeroCreated += OnHeroCreated;
        _plotStage.BuildingClicked += OnPlotBuildingClicked;
        _plotStage.ProjectClicked += OnPlotProjectClicked;
        _activity.HeroClicked += OnHeroClicked;
        _constructionMenuButton.Pressed += OnConstructionMenuPressed;
        _constructionPanel.CloseRequested += OnConstructionPanelCloseRequested;
        _gatherWoodButton.Pressed += OnGatherWoodPressed;
        _modalHost.Opened += OnModalHostOpened;
        _modalHost.Closed += OnModalHostClosed;

        _heroAccessButton.FocusNeighborRight = _constructionMenuButton.GetPath();
        _constructionMenuButton.FocusNeighborLeft = _heroAccessButton.GetPath();

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
        if (_activity is not null)
        {
            _activity.HeroClicked -= OnHeroClicked;
        }
        if (_constructionMenuButton is not null)
        {
            _constructionMenuButton.Pressed -= OnConstructionMenuPressed;
        }
        if (_gatherWoodButton is not null)
        {
            _gatherWoodButton.Pressed -= OnGatherWoodPressed;
        }
        if (_constructionPanel is not null)
        {
            _constructionPanel.CloseRequested -= OnConstructionPanelCloseRequested;
        }
        if (_modalHost is not null)
        {
            _modalHost.Opened -= OnModalHostOpened;
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
        // Restore the chronicle once the modal is dismissed so the
        // player can still read the offline report and live log.
        RestoreChronicleVisibility();
    }

    /// <summary>
    /// Hides the chronicle while any modal is open so it cannot
    /// compete for focus or visually break the modal's scrim.
    /// </summary>
    /// <summary>
    /// Surfaces the next concrete step inside the empty panel so the
    /// player has a clear hook instead of an empty stage. The status
    /// bar already shows the wood count; the guidance only calls out
    /// the action.
    /// </summary>
    private void UpdateEmptyPanelGuidance(CityMacroSnapshot snapshot)
    {
        var status = _controller.GetCityStatusSnapshot();
        bool hasWood = status.WoodStock >= 1;
        _emptyGuidanceLabel.Text = hasWood
            ? "You have at least 1 wood — open the Construction menu to authorise the Basic Shelter."
            : "You need 1 wood to authorise the Basic Shelter. Open a Forest plot and assign your hero to gather it.";
        _emptyGuidanceLabel.Visible = true;
    }

    /// <summary>
    /// Enables the quick gather button only when a Forest still has
    /// wood in its reserve. The button is the bridge between Forest
    /// art and the construction deposit while the player has not yet
    /// learned to assign workers to a Forest.
    /// </summary>
    private void UpdateGatherWoodButton(CityMacroSnapshot snapshot)
    {
        int available = 0;
        foreach (var building in _controller.World.Buildings.Values)
        {
            if (building.Kind == BuildingKind.Forest && building.WoodReserve > 0)
            {
                available += building.WoodReserve;
            }
        }
        _gatherWoodButton.Disabled = available <= 0;
        _gatherWoodButton.Text = available > 0
            ? $"Gather 2 wood ({available} left in forests)"
            : "Gather 2 wood";
    }

    /// <summary>
    /// Drains 2 wood from the first Forest with reserve. We use the
    /// dedicated <c>GatherWood</c> entry point instead of assigning
    /// workers so the action is immediate and visible in the status
    /// bar — the canonical Forest worker flow is taught once the
    /// player has at least one building.
    /// </summary>
    private void OnGatherWoodPressed()
    {
        foreach (var building in _controller.World.Buildings.Values)
        {
            if (building.Kind != BuildingKind.Forest || building.WoodReserve <= 0) continue;
            int gathered = _controller.GatherWood(building.Id, 2);
            if (gathered > 0)
            {
                Notifier.Show($"Gathered {gathered} wood from {building.DisplayName}.");
                return;
            }
        }
        Notifier.ShowError("No wood available in the forests.");
    }

    private void OnModalHostOpened() => _offlineReport.Visible = false;

    /// <summary>
    /// Reapplies the chronicle's desired visibility based on the
    /// current offline / live state. Called when the modal closes.
    /// </summary>
    private void RestoreChronicleVisibility()
    {
        var snapshot = _controller.GetCityMacroSnapshot();
        _offlineReport.ShowLog(snapshot.Events);
    }

    public void OnReturnedToCity()
    {
        if (_controller.NeedsOnboarding()) return;
        Show();
        Refresh();
    }

    private void Refresh()
    {
        CitizenSpriteBank.Instance.PruneExcept(_controller.World.Citizens.Keys);
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
        UpdateEmptyPanelGuidance(snapshot);
        UpdateGatherWoodButton(snapshot);
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

        UpdateAttentionBanner(snapshot);
    }

    private void UpdateAttentionBanner(CityMacroSnapshot snapshot)
    {
        int attentionCount = 0;
        var status = _controller.GetCityStatusSnapshot();
        foreach (var building in status.Buildings)
        {
            if (building.StopCause is ProductionStopCause.NoWorkers
                or ProductionStopCause.WorkersExhausted
                or ProductionStopCause.MissingInputs)
            {
                attentionCount++;
            }
        }
        _attentionBanner.Update(attentionCount);
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

    private void UpdateConstructionMenuButton(MacroMode mode)
    {
        if (_modalHost.IsOpen)
        {
            _constructionMenuButton.SetIconAndLabel(IconPaths.Close, "Close construction");
            _constructionMenuButton.TooltipText = "Close the construction menu (work continues).";
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

        // Surface the next-step hint in the tooltip so the player does
        // not have to read the empty panel before opening the modal.
        if (mode == MacroMode.Empty)
        {
            var status = _controller.GetCityStatusSnapshot();
            _constructionMenuButton.TooltipText = status.WoodStock >= 1
                ? "Open the construction menu to authorise the Basic Shelter."
                : "Open the construction menu to authorise the Basic Shelter. You will need 1 wood — gather it from a Forest first.";
        }
        else
        {
            _constructionMenuButton.TooltipText = "Open the construction menu.";
        }
    }

    private void OnPlotBuildingClicked(int buildingId) =>
        _controller.SelectBuilding(new BuildingId(buildingId));

    private void OnHeroClicked() => _controller.SelectHero();

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
            _heroAccessButton.GrabFocus();
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
