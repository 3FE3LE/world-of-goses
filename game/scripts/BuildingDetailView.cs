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
    [Export] public NodePath SlotsPath { get; set; } = "VisibleWorkerSlots";
    [Export] public NodePath AssignmentPanelPath { get; set; } = "AssignmentPanel";
    [Export] public NodePath ProductionPanelPath { get; set; } = "ProductionPanel";
    [Export] public NodePath ForestGatherPanelPath { get; set; } = "ForestGatherPanel";
    [Export] public NodePath BackButtonPath { get; set; } = "BackButton";
    [Export] public NodePath TitlePath { get; set; } = "Title";
    [Export] public NodePath MacroViewPath { get; set; } = "../CityMacroView";
    [Export] public NodePath ArtHeaderPath { get; set; } = "BuildingArtHeader";

    private CityWorldController _controller = null!;
    private VisibleWorkerSlots _slots = null!;
    private AssignmentPanel _assignmentPanel = null!;
    private ProductionPanel _productionPanel = null!;
    private ForestGatherPanel _forestGatherPanel = null!;
    private Button _backButton = null!;
    private Label _title = null!;
    private TextureRect _artHeader = null!;
    private CityMacroView _macroView = null!;
    private BuildingId _currentBuilding;

    public override void _Ready()
    {
        _controller = GetParent().GetNode<CityWorldController>("CityWorldController");
        _slots = RequireNode<VisibleWorkerSlots>(SlotsPath);
        _assignmentPanel = RequireNode<AssignmentPanel>(AssignmentPanelPath);
        _productionPanel = RequireNode<ProductionPanel>(ProductionPanelPath);
        _forestGatherPanel = RequireNode<ForestGatherPanel>(ForestGatherPanelPath);
        _backButton = RequireNode<Button>(BackButtonPath);
        _title = RequireNode<Label>(TitlePath);
        _artHeader = RequireNode<TextureRect>(ArtHeaderPath);
        _macroView = GetNode<CityMacroView>(MacroViewPath);

        _slots.CitizenClicked += OnSlotCitizenClicked;
        _assignmentPanel.AssignRequested += OnAssignRequested;
        _assignmentPanel.UnassignRequested += OnUnassignRequested;
        _productionPanel.PolicyChangeRequested += OnPolicyChangeRequested;
        _forestGatherPanel.GatherRequested += OnGatherRequested;
        _backButton.Pressed += OnBackPressed;

        _controller.BuildingStateChanged += OnBuildingStateChanged;
        _controller.BuildingSelected += OnBuildingSelected;
        _controller.SelectionChanged += OnSelectionChanged;
        _controller.CitizenAssignmentRejected += OnAssignmentRejected;

        _forestGatherPanel.Visible = false;
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
        Refresh();
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

        _slots.Render(snapshot.VisibleCitizens);

        // Home and Forest are non-production buildings: no
        // assignment, no production panel. Hide them so the detail
        // view shows only the slots stage (the "resting" list) or,
        // for Forests, the gather affordance.
        bool isHome = snapshot.IsHome;
        bool isForest = snapshot.IsForest;
        _assignmentPanel.Visible = !isHome && !isForest;
        _productionPanel.Visible = !isHome && !isForest;
        if (isForest)
        {
            _forestGatherPanel.Visible = true;
            _forestGatherPanel.Refresh(snapshot);
        }
        else
        {
            _forestGatherPanel.Visible = false;
        }
        if (isHome)
        {
            return;
        }

        if (!isForest)
        {
            _assignmentPanel.Refresh(snapshot);
            _productionPanel.Refresh(snapshot);
        }
    }

    private void OnSlotCitizenClicked(int citizenIdValue) =>
        _controller.TryUnassignCitizen(_currentBuilding, new CitizenId(citizenIdValue));

    private void OnAssignRequested(int citizenIdValue)
    {
        var result = _controller.TryAssignCitizen(_currentBuilding, new CitizenId(citizenIdValue));
        if (!result.IsSuccess) GD.Print($"Assignment rejected: {result.Outcome}");
    }

    private void OnUnassignRequested(int citizenIdValue) =>
        _controller.TryUnassignCitizen(_currentBuilding, new CitizenId(citizenIdValue));

    private void OnPolicyChangeRequested(bool enabled, int minStock, int maxStock, int priority) =>
        _controller.ConfigureProductionPolicy(_currentBuilding, enabled, minStock, maxStock, priority);

    private void OnGatherRequested(int forestIdValue)
    {
        _controller.GatherWood(new BuildingId(forestIdValue), ForestGatherPanel.GatherAmount);
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
        GD.Print($"Assignment rejected by domain (code {reason}).");

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
