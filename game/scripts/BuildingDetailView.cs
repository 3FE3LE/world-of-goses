#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Presentation;
using WorldofGoses.Ui;

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
	[Export] public NodePath SlotsPath { get; set; } = "SafeArea/Layout/Content/Main/VisualStage/VisibleWorkerSlots";
	[Export] public NodePath AssignmentPanelPath { get; set; } = "SafeArea/Layout/Content/Main/AssignmentPanel";
	[Export] public NodePath ProductionPanelPath { get; set; } = "SafeArea/Layout/Content/Details/ProductionPanel";
	[Export] public NodePath BackButtonPath { get; set; } = "SafeArea/Layout/Header/BackButton";
	[Export] public NodePath TitlePath { get; set; } = "SafeArea/Layout/Header/Title";
	[Export] public NodePath ArtHeaderPath { get; set; } = "SafeArea/Layout/Content/Main/VisualStage/BuildingArtHeader";

	private const string ProvisionalArtPath = "SafeArea/Layout/Content/Main/VisualStage/ProvisionalArt";
	private const string TownHallProspectPath = "SafeArea/Layout/Content/Details/TownHallProspect";
	private const string HomeSummaryPath = "SafeArea/Layout/Content/Details/HomeSummary";

	private CityWorldController _controller = null!;
	private VisibleWorkerSlots _slots = null!;
	private AssignmentPanel _assignmentPanel = null!;
	private ProductionPanel _productionPanel = null!;
	private Button _backButton = null!;
	private Label _title = null!;
	private TextureRect _artHeader = null!;
	private ColorRect _provisionalArt = null!;
	private Label _provisionalArtLabel = null!;
	private PanelContainer? _homeSummary;
	private Label? _homeSummaryLabel;
	private Button? _craftAxeButton;
	private ResourceInventoryPanel? _shelterResourcesPanel;
	private PanelContainer? _townHallPanel;
	private Label? _prospectLabel;
	private Button? _acceptProspectButton;
	private BuildingId _currentBuilding;

	public override void _Ready()
	{
		// HUD chrome: this view replaces the map, so the map's ambient
		// day/night tint must not reach its panels or its back button.
		OverlayLayers.Apply(this, OverlayLayers.Hud);

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
		// A11: the placeholder that stands in for missing building art is
		// authored beside the art header, between it and the worker slots, so
		// its draw order is the scene's rather than two MoveToFront calls
		// racing to express the same thing.
		_provisionalArt = RequireNode<ColorRect>(ProvisionalArtPath);
		_provisionalArtLabel = RequireNode<Label>($"{ProvisionalArtPath}/Caption");

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
		_controller.CitizensChanged += OnCitizensChanged;
		_controller.ExpeditionStateChanged += OnExpeditionStateChanged;

		Hide();
	}

	public override void _ExitTree()
	{
		if (_slots is not null)
		{
			_slots.CitizenClicked -= OnSlotCitizenClicked;
		}
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
			_controller.CitizensChanged -= OnCitizensChanged;
			_controller.ExpeditionStateChanged -= OnExpeditionStateChanged;
		}
	}

	/// <summary>
	/// The camera push toward the clicked building now happens on the map
	/// itself (<c>MacroStreetLiveView.BeginBuildingEntry</c>) before this
	/// view ever opens, so by the time <see cref="ShowBuilding"/> runs the
	/// "entering" sensation is already delivered — this view just needs a
	/// quick fade, not its own zoom/pivot animation (which used to scale
	/// this Control instead of the world, per 2026-07-27 user feedback).
	/// </summary>
	public void ShowBuilding(BuildingId buildingId)
	{
		_currentBuilding = buildingId;
		Show();
		Refresh();
		_backButton.GrabFocus();
		UiMotion.FadeIn(this);
	}

	public void HideBuilding()
	{
		Hide();
		Modulate = Colors.White;
	}

	private void Refresh()
	{
		var snapshot = _controller.GetBuildingDetailSnapshot(_currentBuilding);
		if (snapshot is null) return;

		// Navigation belongs to the stable header, never to the dynamic
		// worker/assignment subtree rebuilt below.
		_backButton.Show();
		_backButton.MoveToFront();

		// Title shows the full label: "Quarry (Stone)" rather than
		// just "Quarry" — gives each building a distinguishable name
		// in the detail view even when its visual asset is similar.
		_title.Text = UiText.Format(
			"ui.building_detail.full_label", UiText.Get(snapshot.DisplayName), UiText.Get(snapshot.ResourceLabel));

		// Texture header shows the building's art above the worker
		// slots. Hidden when the kind has no art yet (Smithy, PotionLab)
		// so the detail view degrades gracefully instead of crashing.
		var texturePath = BuildingArt.GetTexturePath(snapshot.Kind);
		if (texturePath is not null)
		{
			_artHeader.Texture = ResourceLoader.Load<Texture2D>(texturePath);
			_artHeader.Visible = true;
			_provisionalArt.Visible = false;
			_artHeader.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		}
		else
		{
			_artHeader.Texture = null;
			_artHeader.Visible = false;
			_provisionalArtLabel.Text = UiText.Get(snapshot.DisplayName);
			_provisionalArt.Visible = true;
		}

		_slots.Render(_currentBuilding, snapshot.VisibleCitizens);
		_slots.Visible = snapshot.VisibleCitizens.Count > 0;

		// Home is non-productive (only the worker slots list). Forests
		// are productive like Farms and Quarries now — assign workers
		// and they produce wood from the reserve, so they reuse the
		// AssignmentPanel + ProductionPanel pair.
		bool isHome = snapshot.IsHome;
		bool isTownHall = snapshot.IsTownHall;
		_assignmentPanel.Visible = !isHome && !isTownHall;
		_productionPanel.Visible = !isHome && !isTownHall;
		if (_homeSummary is not null) _homeSummary.Visible = isHome;
		if (_townHallPanel is not null) _townHallPanel.Visible = isTownHall;
		if (isTownHall)
		{
			RefreshTownHall();
			return;
		}
		if (isHome)
		{
			RefreshHomeSummary(snapshot);
			return;
		}

		_assignmentPanel.Refresh(snapshot);
		_productionPanel.Refresh(snapshot);
	}

	private void RefreshTownHall()
	{
		EnsureTownHallPanel();
		BuildingDetailSnapshot? buildingSnapshot = _controller.GetBuildingDetailSnapshot(_currentBuilding);
		BuildingDetailSnapshot.PendingProspectItem? prospect =
			buildingSnapshot?.PendingProspect;
		if (prospect is null)
		{
			_slots.Render(_currentBuilding, System.Array.Empty<BuildingDetailSnapshot.CitizenItem>());
			_slots.Visible = false;
			_prospectLabel!.Text = UiText.Get("ui.town_hall.no_prospect");
			_acceptProspectButton!.Visible = false;
			_townHallPanel!.Visible = true;
			return;
		}

		_slots.RenderIdle(
			_currentBuilding,
			new[]
			{
				new BuildingDetailSnapshot.CitizenItem(
					prospect.Seed,
					prospect.Name,
					prospect.Lineage,
					prospect.Gender,
					prospect.Appearance),
			});
		_slots.Visible = true;
		_prospectLabel!.Text = UiText.Format(
			"ui.town_hall.prospect_detail",
			prospect.Name,
			UiText.Get(ProfileCatalog.Get(prospect.Lineage).DisplayName),
			CitizenNatureText.FormatLocalized(
				prospect.CubeProfile,
				prospect.Lineage,
				prospect.CombatNature));
		_acceptProspectButton!.Visible = true;
		_acceptProspectButton.Disabled = buildingSnapshot?.IsHousingFull ?? false;
		_acceptProspectButton.TooltipText = UiText.Get(
			_acceptProspectButton.Disabled
				? "ui.town_hall.no_housing"
				: "ui.town_hall.accept_hint");
		_townHallPanel!.Visible = true;
	}

	/// <summary>
	/// Binds the authored Town Hall surface. A11: it is a conditional
	/// surface, not a dynamic one — its shape is the same for every Town
	/// Hall, so it lives in the scene hidden and this only wires it. The
	/// lazy-build guard stays because <see cref="Refresh"/> can reach here
	/// on any building and only the Town Hall needs the wiring.
	/// </summary>
	private void EnsureTownHallPanel()
	{
		if (_townHallPanel is not null) return;
		_townHallPanel = RequireNode<PanelContainer>(TownHallProspectPath);
		_prospectLabel = RequireNode<Label>($"{TownHallProspectPath}/Rows/Prospect");
		_acceptProspectButton = RequireNode<Button>($"{TownHallProspectPath}/Rows/AcceptButton");
		_acceptProspectButton.Text = UiText.Get("ui.town_hall.accept");
		_acceptProspectButton.Pressed += OnAcceptProspect;
	}

	private void OnAcceptProspect()
	{
		CityWorld.MigrantResult result = _controller.TryAcceptPendingProspect();
		if (!result.IsSuccess)
		{
			Notifier.ShowError(UiText.Format("ui.citizens.recruit_failed", result.Outcome));
		}
		Refresh();
	}

	private void OnCitizensChanged() { if (Visible) Refresh(); }
	private void OnExpeditionStateChanged(int _) { if (Visible) Refresh(); }

	private void RefreshHomeSummary(BuildingDetailSnapshot snapshot)
	{
		// Surfaces the metrics the player cares about when looking at
		// the Home: capacity and who's currently inside. The label
		// reuses the icon-chip vocabulary so it reads as part of the
		// status bar. The "resting" count must come from the same
		// VisibleCitizens list the slots render — citizens physically
		// at home (CitizenLocation.AtHome) — not from the Home's own
		// _assigned roster, which is empty unless the player treats
		// the Home as a production building (no such recipe today).
		// Reading those two numbers from different sources used to
		// leave the slots full and the summary reading "empty" at the
		// same instant.
		int resting = snapshot.VisibleCitizens.Count;
		int capacity = snapshot.WorkerCapacity;
		EnsureHomeSummary();
		_homeSummaryLabel!.Text = resting switch
		{
			0 => UiText.Format("ui.building_detail.capacity_empty", capacity),
			1 => UiText.Format("ui.building_detail.capacity_resting_one", capacity, resting),
			_ => UiText.Format("ui.building_detail.capacity_resting_many", capacity, resting),
		};
		string shelterState = _homeSummaryLabel.Text;
		_homeSummaryLabel.Text = shelterState + "\n" + UiText.Get(
			snapshot.HasPrimitiveAxe
				? "ui.tools.primitive_axe_stored"
				: "ui.tools.primitive_axe_missing");
		_craftAxeButton!.Text = UiText.Get(snapshot.HasPrimitiveAxe
			? "ui.tools.primitive_axe_owned"
			: "ui.tools.craft_primitive_axe");
		_craftAxeButton.Disabled = snapshot.HasPrimitiveAxe || !snapshot.CanCraftPrimitiveAxe;
		_craftAxeButton.TooltipText = snapshot.HasPrimitiveAxe
			? UiText.Get("ui.tools.primitive_axe_stored")
			: snapshot.PrimitiveAxeMissingResource is ResourceType missing
				? UiText.Format(
					"ui.tools.primitive_axe_missing_resource",
					ResourceTypeLocalizer.Label(missing))
				: UiText.Get("ui.tools.primitive_axe_recipe");
		_shelterResourcesPanel!.Render(
			snapshot.Resources,
			snapshot.FoundingStorageCount,
			snapshot.FoundingStorageCapacity,
			ResourceInventoryOwner.Shelter);
		_homeSummary!.Visible = true;
	}

	private void EnsureHomeSummary()
	{
		if (_homeSummary is not null) return;

		_homeSummary = RequireNode<PanelContainer>(HomeSummaryPath);
		_homeSummaryLabel = RequireNode<Label>($"{HomeSummaryPath}/Rows/Capacity");
		_shelterResourcesPanel = RequireNode<ResourceInventoryPanel>(
			$"{HomeSummaryPath}/Rows/ShelterResources");
		_craftAxeButton = RequireNode<Button>($"{HomeSummaryPath}/Rows/CraftAxeButton");
		// Text and tooltip are rewritten every Refresh from the snapshot, so
		// the scene authors neither; these are only the starting values.
		_craftAxeButton.Text = UiText.Get("ui.tools.craft_primitive_axe");
		_craftAxeButton.TooltipText = UiText.Get("ui.tools.primitive_axe_recipe");
		_craftAxeButton.Pressed += OnCraftPrimitiveAxe;
	}

	private void OnCraftPrimitiveAxe()
	{
		ToolCraftResult result = _controller.TryCraftTool(ToolKind.PrimitiveAxe);
		if (!result.IsSuccess)
		{
			Notifier.ShowError(UiText.Get("ui.tools.craft_failed"));
		}
		else
		{
			Notifier.Show(UiText.Get("ui.tools.primitive_axe_crafted"));
		}
		Refresh();
	}

	internal void ExpandShelterResourcesForVisualRegression()
	{
		if (!WorldofGoses.Testing.VisualRegressionHarness.IsActive) return;
		Refresh();
		_shelterResourcesPanel?.SetExpandedForVisualRegression(expanded: true);
	}

	private void OnSlotCitizenClicked(int citizenIdValue)
	{
		var snapshot = _controller.GetBuildingDetailSnapshot(_currentBuilding);
		if (snapshot?.IsTownHall == true) return;
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
		if (!result.IsSuccess) Notifier.ShowError(AssignmentErrorText.Format(result.Outcome));
	}

	private void OnUnassignRequested(int citizenIdValue)
	{
		var result = _controller.TryUnassignCitizen(_currentBuilding, new CitizenId(citizenIdValue));
		if (!result.IsSuccess) Notifier.ShowError(AssignmentErrorText.Format(result.Outcome));
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
		// ReturnToCity's SelectionChanged signal already restores whichever
		// world view should be visible (MacroStreetLiveView today) — a
		// direct macro-view callback here used to
		// unconditionally re-show the flat view on top of it.
		_controller.ReturnToCity();
		HideBuilding();
	}

	private void OnBuildingStateChanged(int buildingId)
	{
		if (!Visible) return;
		BuildingDetailSnapshot? current = _controller.GetBuildingDetailSnapshot(_currentBuilding);
		if (buildingId != _currentBuilding.Value && current?.IsHome != true) return;
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
		Notifier.ShowError(UiText.Format("ui.building_detail.assignment_rejected", reason));

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
