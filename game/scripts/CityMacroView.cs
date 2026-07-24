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
    [Export] public NodePath TerrainPath { get; set; } = "OrthogonalParcelTerrain";
    [Export] public NodePath HeroAccessButtonPath { get; set; } = "../MacroActions/Actions/HeroAccessButton";
    [Export] public NodePath MacroActionsPath { get; set; } = "../MacroActions";
    [Export] public NodePath PlotStagePath { get; set; } = "BuildingPlotStage";
    [Export] public NodePath ConstructionMenuButtonPath { get; set; } = "../MacroActions/Actions/ConstructionMenuButton";
    [Export] public NodePath GameMenuButtonPath { get; set; } = "../MacroActions/Actions/GameMenuButton";
    [Export] public NodePath AttentionBannerPath { get; set; } = "../../../AttentionBanner";

    private CityWorldController _controller = null!;
    private MacroCitizenActivity _activity = null!;
    private CityStatusPanel _statusPanel = null!;
    private OfflineReportPanel _offlineReport = null!;
    private ModalHost _modalHost = null!;
    private ConstructionPanel _constructionPanel = null!;
    private PanelContainer _emptyPanel = null!;
    private Label _emptyGuidanceLabel = null!;
    private OrthogonalParcelTerrain _terrain = null!;
    private HeroAccessButton _heroAccessButton = null!;
    private Control _macroActions = null!;
    private BuildingPlotStage _plotStage = null!;
    private bool _offlineReportShown;
    private IconButton _constructionMenuButton = null!;
    private IconButton _gameMenuButton = null!;
    private AttentionBanner _attentionBanner = null!;
    private ConstructionPlacementOverlay _placementOverlay = null!;
    private bool _modalWantsOpen;
    private bool _heroLayoutRefreshQueued;
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
        _terrain = GetNode<OrthogonalParcelTerrain>(TerrainPath);
        _heroAccessButton = GetNode<HeroAccessButton>(HeroAccessButtonPath);
        _macroActions = GetNode<Control>(MacroActionsPath);
        _plotStage = GetNode<BuildingPlotStage>(PlotStagePath);
        _constructionMenuButton = GetNode<IconButton>(ConstructionMenuButtonPath);
        _gameMenuButton = GetNode<IconButton>(GameMenuButtonPath);
        _attentionBanner = GetNode<AttentionBanner>(AttentionBannerPath);
        _placementOverlay = new ConstructionPlacementOverlay
        {
            Name = nameof(ConstructionPlacementOverlay),
        };
        AddChild(_placementOverlay);

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
        _constructionPanel.PlacementRequested += OnPlacementRequested;
        _terrain.GatherRequested += OnResourceGatherRequested;
        _terrain.Resized += OnTerrainResized;
        _placementOverlay.PlacementConfirmed += OnPlacementConfirmed;
        _placementOverlay.PlacementCancelled += OnPlacementCancelled;
        _modalHost.Opened += OnModalHostOpened;
        _modalHost.Closed += OnModalHostClosed;

        _heroAccessButton.FocusNeighborRight = _constructionMenuButton.GetPath();
        _constructionMenuButton.FocusNeighborLeft = _heroAccessButton.GetPath();
        _constructionMenuButton.FocusNeighborRight = _gameMenuButton.GetPath();
        _gameMenuButton.FocusNeighborLeft = _constructionMenuButton.GetPath();

        Visible = !_controller.NeedsOnboarding();
        _macroActions.Visible = Visible;
        if (Visible)
        {
            Refresh();
            QueueHeroLayoutRefresh();
        }
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
        if (_terrain is not null)
        {
            _terrain.GatherRequested -= OnResourceGatherRequested;
            _terrain.Resized -= OnTerrainResized;
        }
        if (_constructionPanel is not null)
        {
            _constructionPanel.CloseRequested -= OnConstructionPanelCloseRequested;
            _constructionPanel.PlacementRequested -= OnPlacementRequested;
        }
        if (_placementOverlay is not null)
        {
            _placementOverlay.PlacementConfirmed -= OnPlacementConfirmed;
            _placementOverlay.PlacementCancelled -= OnPlacementCancelled;
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
            : "You need 1 wood. Select a tree, choose Gather, and your hero will walk there automatically.";
        _emptyGuidanceLabel.Visible = true;
    }

    private void OnResourceGatherRequested(
        int forestId,
        int unitId,
        Vector2 targetPosition)
    {
        Citizen? hero = _controller.World.Hero;
        if (hero is null || hero.CurrentAssignment.HasValue)
        {
            string name = hero?.Name ?? "The founder";
            Notifier.ShowError(
                $"{name} is already assigned. Unassign them before gathering wood.");
            return;
        }
        _activity.TravelHeroTo(
            targetPosition,
            _plotStage.GetOccupiedGlobalRects(),
            () =>
        {
            int gathered = _controller.GatherWood(
                new BuildingId(forestId),
                unitId,
                2);
            if (gathered > 0) Notifier.Show($"Gathered {gathered} wood.");
            else Notifier.ShowError("This tree no longer has wood available.");
        });
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
        var mode = DetermineMacroMode(
            snapshot.Buildings.Count,
            snapshot.Projects.Count);

        _emptyPanel.Visible = mode == MacroMode.Empty;
        UpdateEmptyPanelGuidance(snapshot);
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

        if (mode is not MacroMode.Empty)
        {
            _plotStage.Render(snapshot.Buildings, snapshot.Projects);
        }
        else
        {
            _plotStage.Render(
                Array.Empty<CityMacroSnapshot.PlotItem>(),
                Array.Empty<CityMacroSnapshot.PlotItem>());
        }
        Citizen? hero = _controller.World.Hero;
        bool canGather = hero is not null && !hero.CurrentAssignment.HasValue;
        string unavailableReason = hero is null
            ? "No founder is available to gather."
            : $"{hero.Name} is already assigned. Unassign them before gathering.";
        _terrain.RenderResources(
            snapshot.Buildings,
            _plotStage.GetOccupiedGlobalRects(),
            canGather,
            unavailableReason);
        Vector2? heroAnchor = ResolveHeroResourceAnchor(snapshot);
        _activity.Hero = snapshot.Hero;
        _activity.Populate(
            snapshot.Citizens,
            snapshot.Buildings.Count,
            snapshot.Projects.Count,
            heroAnchor);
        _constructionPanel.Refresh();

        if (!_modalHost.IsOpen
            && !_placementOverlay.Visible
            && !_offlineReportShown
            && _controller.LastOfflineReport is { HadProgression: true } report)
        {
            _offlineReport.ShowReport(report);
            _offlineReportShown = true;
        }
        else if (!_modalHost.IsOpen && !_placementOverlay.Visible)
        {
            _offlineReport.ShowLog(snapshot.Events);
        }

        UpdateAttentionBanner(snapshot);
        if (_placementOverlay.Visible) _attentionBanner.Hide();
    }

    private void OnTerrainResized() => QueueHeroLayoutRefresh();

    private void QueueHeroLayoutRefresh()
    {
        if (_heroLayoutRefreshQueued || !Visible) return;
        _heroLayoutRefreshQueued = true;
        Callable.From(RefreshHeroAnchorAfterLayout).CallDeferred();
    }

    private void RefreshHeroAnchorAfterLayout()
    {
        _heroLayoutRefreshQueued = false;
        if (!Visible || !IsInsideTree()) return;
        CityMacroSnapshot snapshot = _controller.GetCityMacroSnapshot();
        Vector2? anchor = ResolveHeroResourceAnchor(snapshot);
        if (anchor.HasValue) _activity.SetHeroAnchor(anchor.Value);
    }

    private Vector2? ResolveHeroResourceAnchor(CityMacroSnapshot snapshot)
    {
        if (snapshot.Citizens.Count == 0) return null;
        CityMacroSnapshot.CitizenItem hero = snapshot.Citizens[0];
        if (hero.CurrentAssignment is BuildingId assignment
            && _plotStage.TryGetEntityGlobalPosition(
                assignment,
                out Vector2 workPosition))
        {
            return workPosition;
        }
        if (hero.LastVisitedResourcePositionIndex is int positionIndex
            && _terrain.TryGetLogicalSlotGlobalPosition(
                positionIndex,
                out Vector2 persistedPosition))
        {
            return persistedPosition;
        }
        if (hero.LastVisitedResourceBuildingId is BuildingId forestId
            && hero.LastVisitedResourceUnitId is int unitId
            && _terrain.TryGetResourceGlobalPosition(
                forestId.Value,
                unitId,
                out Vector2 globalPosition))
        {
            return globalPosition;
        }
        IReadOnlyList<ConstructionLot> freeLots =
            _controller.AvailableConstructionLots();
        return freeLots.Count > 0
            ? _terrain.GetLotGlobalCenter(freeLots[0])
            : null;
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

    internal void ShowConstructionScrollForVisualRegression()
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        _modalWantsOpen = true;
        _constructionPanel.Refresh();
        SyncModalHost();
        _constructionPanel.ScrollBodyToEndForVisualRegression();
    }

    internal void ShowPlacementForVisualRegression()
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        OnPlacementRequested((int)ConstructionKind.Farm);
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
            var snapshot = _controller.GetCityMacroSnapshot();
            UpdateConstructionMenuButton(DetermineMacroMode(
                snapshot.Buildings.Count,
                snapshot.Projects.Count));
        }
    }

    private void OnConstructionPanelCloseRequested()
    {
        _modalWantsOpen = false;
        _modalHost.Close();
    }

    private void OnPlacementRequested(int constructionKind)
    {
        IReadOnlyList<ConstructionLot> lots = _controller.AvailableConstructionLots();
        if (lots.Count == 0)
        {
            Notifier.ShowError("No unlocked parcel has a free building lot.");
            return;
        }
        _modalWantsOpen = false;
        SyncModalHost();
        _offlineReport.Hide();
        _attentionBanner.Hide();
        _macroActions.Hide();
        _placementOverlay.Begin((ConstructionKind)constructionKind, lots);
    }

    private void OnPlacementConfirmed(
        int constructionKind,
        int parcelId,
        int parcelColumn,
        int parcelRow,
        int lotColumn,
        int lotRow)
    {
        var lot = new ConstructionLot(
            new ParcelId(parcelId),
            parcelColumn,
            parcelRow,
            lotColumn,
            lotRow);
        ConstructionAuthorizationResult result =
            _controller.TryAuthorizeConstruction((ConstructionKind)constructionKind, lot);
        if (!result.IsSuccess)
        {
            Notifier.ShowError(ConstructionPanel.FormatAuthorizationError(result.Outcome));
            return;
        }
        _placementOverlay.Hide();
        _macroActions.Show();
        Refresh();
    }

    private void OnPlacementCancelled()
    {
        _placementOverlay.Hide();
        _macroActions.Show();
        RestoreChronicleVisibility();
        _modalWantsOpen = true;
        SyncModalHost();
        UpdateAttentionBanner(_controller.GetCityMacroSnapshot());
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
        _macroActions.Show();
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
        if (!_modalHost.IsOpen && !_placementOverlay.Visible)
        {
            _offlineReport.ShowLog(_controller.GetCityMacroSnapshot().Events);
        }
    }

    private void OnSelectionChanged(int selectionState)
    {
        if ((CityWorldController.Selection)selectionState == CityWorldController.Selection.MacroView
            && !_controller.NeedsOnboarding())
        {
            _macroActions.Show();
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
            _macroActions.Hide();
            _constructionMenuButton.Visible = false;
            Hide();
        }
    }
}
