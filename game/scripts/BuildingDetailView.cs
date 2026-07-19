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
    [Export] public NodePath BackButtonPath { get; set; } = "BackButton";
    [Export] public NodePath TitlePath { get; set; } = "Title";
    [Export] public NodePath MacroViewPath { get; set; } = "../CityMacroView";

    private CityWorldController _controller = null!;
    private VisibleWorkerSlots _slots = null!;
    private AssignmentPanel _assignmentPanel = null!;
    private ProductionPanel _productionPanel = null!;
    private Button _backButton = null!;
    private Label _title = null!;
    private CityMacroView _macroView = null!;
    private BuildingId _currentBuilding;

    public override void _Ready()
    {
        _controller = GetParent().GetNode<CityWorldController>("CityWorldController");
        _slots = RequireNode<VisibleWorkerSlots>(SlotsPath);
        _assignmentPanel = RequireNode<AssignmentPanel>(AssignmentPanelPath);
        _productionPanel = RequireNode<ProductionPanel>(ProductionPanelPath);
        _backButton = RequireNode<Button>(BackButtonPath);
        _title = RequireNode<Label>(TitlePath);
        _macroView = GetNode<CityMacroView>(MacroViewPath);

        _slots.CitizenClicked += OnSlotCitizenClicked;
        _assignmentPanel.AssignRequested += OnAssignRequested;
        _assignmentPanel.UnassignRequested += OnUnassignRequested;
        _productionPanel.AdvanceRequested += OnAdvanceRequested;
        _productionPanel.PolicyChangeRequested += OnPolicyChangeRequested;
        _backButton.Pressed += OnBackPressed;

        _controller.BuildingStateChanged += OnBuildingStateChanged;
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
            _productionPanel.AdvanceRequested -= OnAdvanceRequested;
            _productionPanel.PolicyChangeRequested -= OnPolicyChangeRequested;
        }
        if (_controller is not null)
        {
            _controller.BuildingStateChanged -= OnBuildingStateChanged;
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
        var building = _controller.GetBuilding(_currentBuilding);
        if (building is null) return;

        // Title shows the full label: "Quarry (Stone)" rather than
        // just "Quarry" — gives each building a distinguishable name
        // in the detail view even when its visual asset is similar.
        _title.Text = building.FullDisplayLabel;

        var visibleIds = _controller.World.GetCurrentlyVisibleOccupants(building);
        _slots.Render(visibleIds, building, _controller.Citizens());

        // Home is a non-production building: no assignment, no
        // production panel. Hide them so the detail view shows
        // only the slots stage (the "resting" list).
        bool isHome = building.Kind == BuildingKind.Home;
        _assignmentPanel.Visible = !isHome;
        _productionPanel.Visible = !isHome;
        if (isHome)
        {
            return;
        }

        _assignmentPanel.Refresh(building, _controller);
        _productionPanel.Refresh(building, _controller);
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

    private void OnAdvanceRequested() =>
        _controller.AdvanceProduction(_currentBuilding);

    private void OnPolicyChangeRequested(bool enabled, int targetStock) =>
        _controller.ConfigureProductionPolicy(_currentBuilding, enabled, targetStock);

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
