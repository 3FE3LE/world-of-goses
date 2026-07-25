#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Detailed view for a single building. Composes the worker-slot
/// stage, the assignment panel and the production panel, and
/// animates workers in and out as their assignments change.
/// </summary>
public partial class BuildingDetailView : Control
{
    private const string BackgroundNodePath = "DetailBackground";
    private const string SafeAreaNodePath = "SafeArea";

    [Export] public NodePath ControllerPath { get; set; } = "../../../CityWorldController";
    [Export] public NodePath SlotsPath { get; set; } = "SafeArea/Layout/Content/Main/VisibleWorkerSlots";
    [Export] public NodePath AssignmentPanelPath { get; set; } = "SafeArea/Layout/Content/AssignmentPanel";
    [Export] public NodePath ProductionPanelPath { get; set; } = "SafeArea/Layout/Content/Main/ProductionPanel";
    [Export] public NodePath BackButtonPath { get; set; } = "SafeArea/Layout/Header/BackButton";
    [Export] public NodePath TitlePath { get; set; } = "SafeArea/Layout/Header/Title";
    [Export] public NodePath MacroViewPath { get; set; } = "../CityMacroView";
    [Export] public NodePath ArtHeaderPath { get; set; } = "SafeArea/Layout/Content/Main/BuildingArtHeader";

    private CityWorldController _controller = null!;
    private VisibleWorkerSlots _slots = null!;
    private AssignmentPanel _assignmentPanel = null!;
    private ProductionPanel _productionPanel = null!;
    private Button _backButton = null!;
    private Label _title = null!;
    private TextureRect _artHeader = null!;
    private CityMacroView _macroView = null!;
    private PanelContainer? _homeSummary;
    private Label? _homeSummaryLabel;
    private BuildingId _currentBuilding;

    public override void _Ready()
    {
        // This view starts hidden. Re-apply the full parent rect at runtime so
        // its origin follows ScreenContent after GameUiShell reserves the HUD.
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        GetNode<Control>(BackgroundNodePath).SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        GetNode<Control>(SafeAreaNodePath).SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        _controller = GetNode<CityWorldController>(ControllerPath);
        _slots = RequireNode<VisibleWorkerSlots>(SlotsPath);
        _assignmentPanel = RequireNode<AssignmentPanel>(AssignmentPanelPath);
        _productionPanel = RequireNode<ProductionPanel>(ProductionPanelPath);
        _backButton = RequireNode<Button>(BackButtonPath);
        _title = RequireNode<Label>(TitlePath);
        _artHeader = RequireNode<TextureRect>(ArtHeaderPath);
        _macroView = GetNode<CityMacroView>(MacroViewPath);

        _slots.CitizenClicked += OnSlotCitizenClicked;
        _assignmentPanel.AssignRequested += OnAssignRequested;
        _assignmentPanel.UnassignRequested += OnUnassignRequested;
        _productionPanel.PolicyChangeRequested += OnPolicyChangeRequested;
        _productionPanel.PolicyConfigureRequested += OnPolicyConfigureRequested;
        _backButton.Pressed += OnBackPressed;

        _controller.BuildingStateChanged += OnBuildingStateChanged;
        _controller.BuildingSelected += OnBuildingSelected;
        _controller.SelectionChanged += OnSelectionChanged;
        _controller.CitizenAssignmentRejected += OnAssignmentRejected;

        Hide();
    }

    public override void _ExitTree()
    {
        if (_slots is not null) _slots.CitizenClicked -= OnSlotCitizenClicked;
        if (_assignmentPanel is not null)
        {
            _assignmentPanel.AssignRequested -= OnAssignRequested;
            _assignmentPanel.UnassignRequested -= OnUnassignRequested;
        }
        if (_productionPanel is not null)
        {
            _productionPanel.PolicyChangeRequested -= OnPolicyChangeRequested;
            _productionPanel.PolicyConfigureRequested -= OnPolicyConfigureRequested;
        }
        if (_controller is not null)
        {
            _controller.BuildingStateChanged -= OnBuildingStateChanged;
            _controller.BuildingSelected -= OnBuildingSelected;
            _controller.SelectionChanged -= OnSelectionChanged;
            _controller.CitizenAssignmentRejected -= OnAssignmentRejected;
        }
    }

    public void ShowBuilding(BuildingId buildingId)
    {
        _currentBuilding = buildingId;
        Show();
        Modulate = Colors.White;
        Refresh();
        _backButton.GrabFocus();
    }

    public void HideBuilding() => Hide();

    private void Refresh()
    {
        var snapshot = _controller.GetBuildingDetailSnapshot(_currentBuilding);
        if (snapshot is null) return;

        // Title shows the full label: "Quarry (Stone)" rather than
        // just "Quarry" — gives each building a distinguishable name
        // in the detail view even when its visual asset is similar.
        _title.Text = snapshot.FullDisplayLabel;

        // Texture header shows the building's art above the worker
        // slots. Hidden when the kind has no art yet (Smithy, PotionLab)
        // so the detail view degrades gracefully instead of crashing.
        var texturePath = BuildingArt.GetTexturePath(snapshot.Kind);
        if (texturePath is not null)
        {
            _artHeader.Texture = ResourceLoader.Load<Texture2D>(texturePath);
            _artHeader.Visible = true;
        }
        else
        {
            _artHeader.Texture = null;
            _artHeader.Visible = false;
        }

        _slots.Render(_currentBuilding, snapshot.VisibleCitizens);
        _slots.Visible = snapshot.VisibleCitizens.Count > 0;

        // Home is non-productive (only the worker slots list). Forests
        // are productive like Farms and Quarries now — assign workers
        // and they produce wood from the reserve, so they reuse the
        // AssignmentPanel + ProductionPanel pair.
        bool isHome = snapshot.IsHome;
        _assignmentPanel.Visible = !isHome;
        _productionPanel.Visible = !isHome;
        if (_homeSummary is not null) _homeSummary.Visible = isHome;
        if (isHome)
        {
            RefreshHomeSummary(snapshot);
            return;
        }

        _assignmentPanel.Refresh(snapshot);
        _productionPanel.Refresh(snapshot);
    }

    private void RefreshHomeSummary(BuildingDetailSnapshot snapshot)
    {
        // Surfaces the metrics the player cares about when looking at
        // the Home: capacity and who's currently inside. The label
        // reuses the icon-chip vocabulary so it reads as part of the
        // status bar.
        int resting = snapshot.HiddenWorkerCount + snapshot.VisibleWorkerCount;
        int capacity = snapshot.WorkerCapacity;
        EnsureHomeSummary();
        _homeSummaryLabel!.Text = resting == 0
            ? $"Capacity: {capacity} · No one is resting here."
            : $"Capacity: {capacity} · {resting} citizen{(resting == 1 ? string.Empty : "s")} resting here.";
        _homeSummary!.Visible = true;
    }

    private void EnsureHomeSummary()
    {
        if (_homeSummary is not null) return;

        _homeSummaryLabel = new Label
        {
            ThemeTypeVariation = "BodyText",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };

        _homeSummary = new PanelContainer { Name = "HomeSummary" };
        _homeSummary.AddThemeStyleboxOverride(
            "panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
        _homeSummary.AddChild(_homeSummaryLabel);
        _productionPanel.GetParent().AddChild(_homeSummary);
    }

    private void OnSlotCitizenClicked(int citizenIdValue)
    {
        var snapshot = _controller.GetBuildingDetailSnapshot(_currentBuilding);
        if (snapshot?.IsHome == true)
        {
            _controller.SelectHero();
            return;
        }
        _controller.TryUnassignCitizen(_currentBuilding, new CitizenId(citizenIdValue));
    }

    private void OnAssignRequested(int citizenIdValue)
    {
        var result = _controller.TryAssignCitizen(_currentBuilding, new CitizenId(citizenIdValue));
        if (!result.IsSuccess) Notifier.ShowError(FormatAssignmentError(result.Outcome));
    }

    private void OnUnassignRequested(int citizenIdValue)
    {
        var result = _controller.TryUnassignCitizen(_currentBuilding, new CitizenId(citizenIdValue));
        if (!result.IsSuccess) Notifier.ShowError(FormatAssignmentError(result.Outcome));
    }

    private void OnPolicyChangeRequested(bool enabled) =>
        _controller.SetProductionEnabled(_currentBuilding, enabled);

    private void OnPolicyConfigureRequested(int minStock, int maxStock)
    {
        var snapshot = _controller.GetBuildingDetailSnapshot(_currentBuilding);
        if (snapshot is null) return;
        bool enabled = snapshot.ProductionEnabled;
        int priority = snapshot.Priority;
        _controller.ConfigureProductionPolicy(_currentBuilding, enabled, minStock, maxStock, priority);
    }

    private void OnBackPressed()
    {
        _controller.ReturnToCity();
        _macroView.OnReturnedToCity();
        HideBuilding();
    }

    private void OnBuildingStateChanged(int buildingId)
    {
        if (buildingId != _currentBuilding.Value) return;
        Refresh();
    }

    /// <summary>
    /// Fired by <see cref="CityWorldController.SelectBuilding"/> when the
    /// player activates a plot in the macro view (or any other code path
    /// that wants to open the building detail). Opens the detail view
    /// for the building id carried by the signal.
    /// </summary>
    private void OnBuildingSelected(int buildingId) =>
        ShowBuilding(new BuildingId(buildingId));

    /// <summary>
    /// Keeps the detail view in sync with the controller's selection
    /// state. Only stays visible while <see cref="CityWorldController.Selection.BuildingDetail"/>
    /// is the active selection; hides on every other transition so
    /// navigation via <c>View hero</c>, the macro view's back path,
    /// or any future selection target never leaves the detail view
    /// stranded on top.
    /// </summary>
    private void OnSelectionChanged(int selectionState)
    {
        var selection = (CityWorldController.Selection)selectionState;
        if (selection == CityWorldController.Selection.BuildingDetail)
        {
            // BuildingSelected is what actually opens the view; this
            // handler just makes sure we stay visible if the selection
            // reasserts itself for the same building.
            return;
        }
        HideBuilding();
    }

    private void OnAssignmentRejected(int reason) =>
        Notifier.ShowError($"Assignment rejected (code {reason}).");

    private static string FormatAssignmentError(AssignmentOutcome outcome) => outcome switch
    {
        AssignmentOutcome.AtCapacity => "Project is at worker capacity.",
        AssignmentOutcome.AlreadyAssigned => "Citizen is already a contributor.",
        AssignmentOutcome.CitizenUnavailable => "Citizen is assigned elsewhere.",
        AssignmentOutcome.NotAssigned => "Citizen is not assigned here.",
        AssignmentOutcome.CitizenNotFound => "Citizen no longer exists.",
        AssignmentOutcome.BuildingNotFound => "Worksite no longer exists.",
        _ => "Assignment rejected.",
    };

    private T RequireNode<T>(NodePath path) where T : class
    {
        var node = GetNodeOrNull<T>(path);
        if (node is null)
        {
            GD.PushError(
                $"BuildingDetailView: path '{path}' did not resolve to a {typeof(T).Name}. " +
                "Check CityPrototype.tscn parent declarations.");
            throw new System.InvalidOperationException(
                $"BuildingDetailView: missing wired node at path '{path}'.");
        }
        return node;
    }
}
