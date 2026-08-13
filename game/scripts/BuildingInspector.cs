#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Presentation;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Contextual inspector for a single building, replacing the former
/// top-level <c>BuildingDetailView</c> shell. The macro stays visible:
/// the inspector sits over the right edge of it as a HUD-level surface
/// (OverlayLayers.Hud), composes the same panels its predecessor did
/// (worker slots, assignment, production, town hall prospect, home
/// summary, shelter resources and Primitive Axe crafting), and closes
/// with a single in-shell action instead of a navigation back to the
/// map.
///
/// <para>
/// The decision is the spatial-grammar one (#18): selecting a building
/// is now a contextual action. The inspector is owned by the same
/// surface layout as the macro, not a new top-level route; the
/// selection signal still drives the macro's building anchor, the
/// camera does not reset on open/close, and there is no
/// <c>BackToCityButton</c>.
/// </para>
/// </summary>
public partial class BuildingInspector : Control
{
	[Export] public NodePath ControllerPath { get; set; } = "../../../CityWorldController";
	[Export] public NodePath SlotsPath { get; set; } = "SafeArea/Layout/Content/Main/VisualStage/VisibleWorkerSlots";
	[Export] public NodePath AssignmentPanelPath { get; set; } = "SafeArea/Layout/Content/Main/AssignmentPanel";
	[Export] public NodePath ProductionPanelPath { get; set; } = "SafeArea/Layout/Content/Details/ProductionPanel";
	[Export] public NodePath CloseButtonPath { get; set; } = "SafeArea/Layout/Header/CloseButton";
	[Export] public NodePath TitlePath { get; set; } = "SafeArea/Layout/Header/Title";
	[Export] public NodePath ArtHeaderPath { get; set; } = "SafeArea/Layout/Content/Main/VisualStage/BuildingArtHeader";

	private const string ProvisionalArtPath = "SafeArea/Layout/Content/Main/VisualStage/ProvisionalArt";
	private const string TownHallProspectPath = "SafeArea/Layout/Content/Details/TownHallProspect";
	private const string HomeSummaryPath = "SafeArea/Layout/Content/Details/HomeSummary";

	private CityWorldController _controller = null!;
	private VisibleWorkerSlots _slots = null!;
	private AssignmentPanel _assignmentPanel = null!;
	private ProductionPanel _productionPanel = null!;
	private Button _closeButton = null!;
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
		// HUD chrome: this panel sits over the macro, so it claims the HUD
		// overlay layer; the macro's ambient day/night tint must not reach
		// it.
		OverlayLayers.Apply(this, OverlayLayers.Hud);

		_controller = GetNode<CityWorldController>(ControllerPath);
		_slots = RequireNode<VisibleWorkerSlots>(SlotsPath);
		_assignmentPanel = RequireNode<AssignmentPanel>(AssignmentPanelPath);
		_productionPanel = RequireNode<ProductionPanel>(ProductionPanelPath);
		_closeButton = RequireNode<Button>(CloseButtonPath);
		_title = RequireNode<Label>(TitlePath);
		_artHeader = RequireNode<TextureRect>(ArtHeaderPath);
		_provisionalArt = RequireNode<ColorRect>(ProvisionalArtPath);
		_provisionalArtLabel = RequireNode<Label>($"{ProvisionalArtPath}/Caption");

		_slots.CitizenClicked += OnSlotCitizenClicked;
		_assignmentPanel.AssignRequested += OnAssignRequested;
		_assignmentPanel.UnassignRequested += OnUnassignRequested;
		_productionPanel.PolicyChangeRequested += OnPolicyChangeRequested;
		_productionPanel.PolicyConfigureRequested += OnPolicyConfigureRequested;
		_closeButton.Pressed += OnClosePressed;

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
	/// Opens (or refreshes) the inspector for the given building. The
	/// macro view is **not** hidden; the inspector sits over the
	/// existing canvas as the contextual action for the selected
	/// building. The world camera and the building's selection
	/// highlight stay on top of the inspector (or follow the building
	/// if a follow-mode camera was active).
	/// </summary>
	public void ShowBuilding(BuildingId buildingId)
	{
		_currentBuilding = buildingId;
		Show();
		Refresh();
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

		_title.Text = UiText.Format(
			"ui.building_detail.full_label", UiText.Get(snapshot.DisplayName), UiText.Get(snapshot.ResourceLabel));

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

		// Home is non-productive (only the worker slots list).
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

	/// <summary>
	/// The contextual close action. Hides the inspector and returns the
	/// selection to the macro view without resetting the camera — the
	/// building remains selected at the same highlight until the player
	/// picks another building or explicit clears the selection. There is
	/// no navigation back to the city; the macro was never left.
	/// </summary>
	private void OnClosePressed()
	{
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
	/// that wants to open the building detail). Opens the inspector for
	/// the building id carried by the signal.
	/// </summary>
	private void OnBuildingSelected(int buildingId) =>
		ShowBuilding(new BuildingId(buildingId));

	/// <summary>
	/// Keeps the inspector in sync with the controller's selection state.
	/// Hides whenever the active selection is not BuildingDetail — the
	/// player's <c>View hero</c>, a return path from another surface or a
	/// fresh selection all implicitly close without leaving a stranded
	/// inspector on top.
	/// </summary>
	private void OnSelectionChanged(int selectionState)
	{
		var selection = (CityWorldController.Selection)selectionState;
		if (selection == CityWorldController.Selection.BuildingDetail)
		{
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
				$"BuildingInspector: path '{path}' did not resolve to a {typeof(T).Name}. " +
				"Check CityPrototype.tscn parent declarations.");
			throw new System.InvalidOperationException(
				$"BuildingInspector: missing wired node at path '{path}'.");
		}
		return node;
	}
}
