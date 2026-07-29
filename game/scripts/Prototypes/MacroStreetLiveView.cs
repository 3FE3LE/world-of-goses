#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses.Prototypes;

/// <summary>
/// The pseudo-3D "perspectiva por calles" macro view — the ONLY macro world
/// view. This is the sole runtime representation of the city.
///
/// Street plan (design bible §08 "Ciudad macro (perspectiva por calles)" +
/// H-26's corridor reading): each calle is the free front band of a
/// lot-row; the lot behind it spans the full
/// <see cref="ParcelGrid.TilesPerStandardLot"/> tile depth, rendered as a
/// continuous tiled floor (<see cref="DrawTiledFloor"/>) so buildings and
/// trees sit on real depth-graduated ground instead of floating over a
/// flat painted strip. Crossing between adjacent streets is only viable
/// through the gaps trees/buildings leave — <see cref="StreetRoutePlanner"/>
/// owns that rule for both the hero's gather routes and the player's
/// manual W/S steps, threading BETWEEN obstacles rather than around them
/// (its gap-scan step is fine-grained specifically so narrow gaps between
/// adjacent same-row obstacles are never skipped over — see that class's
/// own docs for the exact bug this fixed).
///
/// The avatar is the real founder: the canonical
/// <see cref="CitizenSpriteCarrier"/> from <see cref="CitizenSpriteBank"/>
/// (one citizen, one sprite), walked with the shared 8 px / 12 Hz
/// quantized cadence via <see cref="_Process"/> (not <c>_PhysicsProcess</c>:
/// a variable/low render framerate can make Godot run several fixed physics
/// steps between two actually-rendered frames, which would make this
/// Draw-based view's motion look like it pops between two points instead
/// of stepping through them — <c>_Process</c>'s delta always matches what
/// gets rendered). Gather travels the street network before resolving,
/// matching the flat view's original "your hero will walk there" UX.
///
/// Camera zoom: quantized scroll-wheel steps (<see cref="AdjustZoom"/>),
/// applied as this node's own <see cref="Node2D.Scale"/> around the
/// vanishing-point pivot so it stays screen-centered at any zoom level —
/// matches the "no continuous free scaling" pixel-motion grammar (discrete
/// steps, not a smooth slider). Entering a building pushes the same Scale
/// toward the clicked building first, in discrete steps too (see
/// <see cref="BeginBuildingEntry"/>) — the "camera" push happens on this
/// map, not on <c>BuildingDetailView</c>.
///
/// Camera mode (design bible §04 "Cámara-sigue"): follow-the-founder
/// (default) or free pan, an explicit toggle (<see cref="ToggleCameraMode"/>,
/// F key or the MacroActions button) independent from selection. Follow
/// mode is exactly the historical behavior (the founder IS the viewer);
/// free mode decouples the vanishing point (<see cref="CameraLateral"/>/
/// <see cref="CameraDepthAnchor"/>) from the founder's own true position
/// (<see cref="_heroLateral"/>/<see cref="_heroStreet"/>, which keeps
/// moving/routing on its own regardless of camera mode) and projects the
/// founder as an ordinary sprite instead of always at depth 0.
///
/// Remaining known gaps (documented, not blocking): clicking an
/// in-progress construction project (the real game opens the construction
/// panel for that); the empty-state guidance text for a brand-new city;
/// buildings spanning more than one lot-row anchor to their
/// nearest-to-viewer row; no <c>BuildingDetailView</c> assignment surface
/// exists purpose-built for this view yet (it reuses the shared,
/// view-agnostic one via <see cref="CityWorldController.SelectBuilding"/>).
/// See docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md,
/// "Cámara y mundo caminable".
/// </summary>
public partial class MacroStreetLiveView : Node2D
{
    private const float CenterX = 640f;
    private const float BaseY = 580f; // ScreenContent-local: clear of the ~68px MacroActions band
    private const float LotUnitPx = 90f;
    private const int WorldParcelColumns = 4;
    private const int WorldParcelRows = 2;

    // Quantized zoom: discrete steps, never a continuous drag/slider.
    private const float ZoomStep = 0.15f;
    private const float MinZoom = 0.7f;
    private const float MaxZoom = 1.6f;

    // Same cadence discipline as the earlier prototypes (design bible §08,
    // "Pixel-motion grammar"): no continuous tweening.
    private const int TransitionSteps = 5;
    private const float DepthStepSize = 1f / TransitionSteps;

    // Building-entry camera push: a handful of DISCRETE zoom steps toward
    // the clicked building (same stepped cadence as citizen/camera motion —
    // never a continuous Tween), applied to THIS node's own Scale/Position
    // (the map), not to BuildingDetailView. See BeginBuildingEntry.
    private const int BuildingEntryZoomSteps = 5;
    private const float BuildingEntryZoomLevel = MaxZoom;

    // Trees reuse the flat view's Kenney atlas tiles (ResourceTree) so both
    // world views share one visual identity; 44 px base ≈ the flat view's
    // 48 px tree on this view's 30 px/tile lots.
    private const float TreeBaseSizePx = 44f;
    // Lateral span a living tree blocks when crossing its band (its lot).
    private const float TreeBlockHalfWidthPx = 22f;
    // Half hero width plus a small margin: how much free lateral space a
    // crossing between streets needs to count as viable.
    private const float RouteClearancePx = 14f;
    // Granularity when scanning a band for a viable crossing point. Must be
    // small enough to reliably land inside the narrowest realistic gap
    // between two adjacent same-row obstacles (with today's spacing, as
    // little as ~18 px) — a coarser step can jump clean over a legitimate
    // gap and force the search much farther out, reading as if the hero
    // detoured around a whole row instead of threading through it.
    private const float CrossingScanStepPx = 6f;
    // LPC frames center the body; feet sit ~28 frame px below center, which
    // is 7 px at the carrier's 0.25 macro scale (before depth scaling).
    private const float HeroFootOffsetMacroPx = 7f;

    private const string ResourceActionMenuScenePath = "res://scenes/Components/ResourceActionMenu.tscn";

    // Floor tiles sample the Kenney atlas ResourceTree already uses for
    // trees (S-1.3 biome pass), keyed by street so the corridor reads as
    // distinct ground per calle.
    private const float TileUnitPx = LotUnitPx / 3f; // ParcelGrid.TilesPerStandardLot
    // Chunky pixel-grid step for the floor's staircase edges — see
    // DrawPixelStaircaseTrapezoid. Half of PixelMotion.StepPixels (8px):
    // coarse enough to read as deliberate pixel art, fine enough that the
    // trapezoid shape stays legible instead of looking blocky/broken.
    private const float PixelStepPx = 4f;
    // Ground biome atlas coordinates in the shared Kenney roguelike sheet
    // (see ResourceTree.TerrainAtlasPath/AtlasRegionRect for the 16px+1
    // stride convention). Rows 0/1 hold two near-identical variants of each
    // solid ground swatch — used the same way GroundBiome's own "alternate"
    // checkerboard used to alternate between two flat colors, now between
    // two real tiles of the same material. Column 4 (dirt) is deliberately
    // the same swatch reserved for the future worn-path pass (H-32 S-1.3
    // follow-up): a trampled tile will simply render as this same Dirt
    // biome rather than needing a fourth texture.
    private const int GrassAtlasColumn = 5;
    private const int DirtAtlasColumn = 6;
    private const int StoneAtlasColumn = 7;
    private const int GroundAtlasRowA = 0;
    private const int GroundAtlasRowB = 1;
    private static readonly Color BuildingColor = new("#8a7a54");
    private static readonly Color UnderConstructionModulate = new(0.55f, 0.55f, 0.55f);
    private static readonly Color PlacementAvailableColor = new("#2f8f5b99");
    private static readonly Color PlacementSelectedColor = new("#f2c94ccc");

    [Export] public NodePath ControllerPath { get; set; } = "../../../CityWorldController";
    [Export] public NodePath StatusPanelPath { get; set; } = "../../CityStatusPanel";
    [Export] public NodePath ChroniclePath { get; set; } = "../OfflineReportPanel";
    [Export] public NodePath ConstructionMenuButtonPath { get; set; } =
        "../MacroActions/Actions/ConstructionMenuButton";
    [Export] public NodePath ConstructionPanelPath { get; set; } = "../Center/ConstructionPanel";
    [Export] public NodePath ExpeditionMenuButtonPath { get; set; } =
        "../MacroActions/Actions/ExpeditionMenuButton";
    [Export] public NodePath ExpeditionPanelPath { get; set; } = "../ExpeditionPanel";
    [Export] public NodePath ModalHostPath { get; set; } = "../ModalHost";
    [Export] public NodePath MacroActionsPath { get; set; } = "../MacroActions";
    [Export] public NodePath BuildingDetailViewPath { get; set; } = "../BuildingDetailView";
    [Export] public NodePath CameraModeButtonPath { get; set; } =
        "../MacroActions/Actions/CameraModeButton";

    private CityWorldController _controller = null!;
    private CityStatusPanel _statusPanel = null!;
    private OfflineReportPanel _chronicle = null!;
    private ResourceActionMenu _actionMenu = null!;
    private IconButton _constructionMenuButton = null!;
    private ConstructionPanel _constructionPanel = null!;
    private IconButton _expeditionMenuButton = null!;
    private ExpeditionPanel _expeditionPanel = null!;
    private ModalHost _modalHost = null!;
    private Control _macroActions = null!;
    private BuildingDetailView _buildingDetailView = null!;
    private IconButton _cameraModeButton = null!;
    private CursorController? _cursorController;
    private Texture2D _terrainAtlas = null!;
    private readonly Dictionary<BuildingKind, Texture2D?> _buildingTextureCache = new();
    private bool _selectionIsMacro = true;
    private float _zoomLevel = 1f;
    private Vector2 _neutralPosition;

    // Building-entry push state (see BeginBuildingEntry/AdvanceBuildingEntry).
    private BuildingId? _pendingBuildingEntry;
    private Vector2 _buildingEntryPivotLocal;
    private float _buildingEntryStartZoom;
    private int _buildingEntryStep;
    private float _buildingEntryAccumulator;

    // Camera mode (design bible §04 "Cámara-sigue"): follow-selected-target
    // vs. always-available free pan, an explicit toggle independent from
    // selection itself — see also the validated WalkableWorldCamera
    // prototype. Free camera state is intentionally separate from the
    // hero's own _heroStreet/_heroLateral (the hero's true position, which
    // keeps moving/routing on its own regardless of camera mode).
    private bool _cameraFollowsHero = true;
    private bool _wasFreeCameraBeforePlacement;
    private float _freeCameraLateral;
    private int _freeCameraStreet;
    private float _cameraDepthAnchor;
    private float? _cameraDepthTarget;
    private float _cameraTransitionAccumulator;

    // Placement mode: select-then-confirm lot picking projected directly on
    // the same terrain geometry as the city.
    private readonly record struct PlacementLotBox(
        ConstructionLot Lot, int Street, float LateralOffset, float Width, float Height);
    private readonly List<PlacementLotBox> _placementLots = new();
    private readonly List<(Rect2 Rect, PlacementLotBox Lot)> _clickablePlacementRects = new();
    private bool _placementActive;
    private ConstructionKind _placementKind;
    private ConstructionLot? _selectedPlacementLot;
    private Label _placementInstruction = null!;
    private Button _placementConfirmButton = null!;
    private Button _placementCancelButton = null!;
    private Control _placementFooter = null!;

    private readonly List<PlotBox> _plots = new();
    private readonly List<TreeBox> _trees = new();
    // S-1.3 phase 2: session-scoped foot-traffic wear, not persisted (see
    // TerrainWearGrid's own doc for why it deliberately stays out of WorldSave).
    private readonly TerrainWearGrid _terrainWear = new();
    private readonly List<(Rect2 Rect, int BuildingId)> _clickableRects = new();
    private readonly List<(Rect2 Rect, TreeBox Tree)> _clickableTreeRects = new();
    private readonly Dictionary<int, List<StreetRoutePlanner.Interval>> _bandOccupancy = new();
    private static readonly List<StreetRoutePlanner.Interval> EmptyBand = new();

    private int _streetCount = 1;
    private float _lateralHalfWidthPx = LotUnitPx;

    // The hero IS the viewer: the vanishing point follows them (validated
    // in the isolated prototype), so camera lateral == hero lateral and the
    // depth anchor chases the hero's street.
    private int _heroStreet;
    private float _heroLateral;
    private float _depthAnchor;
    private float? _depthTarget;
    private float _motionAccumulator;
    private float _transitionAccumulator;
    private bool _heroWalking;

    private SelectionInfoPanel _selectionInfoPanel = null!;
    private TreeBox? _selectedTree;
    private int? _selectedBuildingId;

    private CitizenSpriteCarrier? _heroCarrier;
    // S-1.4 follow-up: ambient, non-hero assigned citizens standing at
    // their workplace (see RefreshCitizenVisuals), keyed by citizen id.
    private readonly record struct WorkerSlot(CitizenSpriteCarrier Carrier, int BuildingId, int Index, int GroupSize);
    private readonly Dictionary<int, WorkerSlot> _workerCarriers = new();
    private const float WorkerLateralSpacingPx = 10f;
    private StreetNavigationServerPlanner? _navmeshPlanner;
    private List<StreetRoutePlanner.Waypoint>? _route;
    private int _routeIndex;
    private (int ForestId, int UnitId)? _pendingGather;
    private BuildingId? _pendingAssignment;
    private bool _pendingReturnHome;
    // Domain CurrentLocation never leaves AtHome for manual W/S/arrow
    // movement or a Gather route (neither is a real domain travel action),
    // so without this latch every subsequent Refresh() (fired on any world
    // tick) sees "AtHome, no route, no pending return" and re-hides the
    // founder — whether that is the instant the player stops pressing a
    // key, or right as CompleteRoute's gather branch clears _route and
    // calls GatherWood (itself a synchronous refresh trigger). Set by
    // EnsureHeroCarrierReadyToMove; cleared once a real domain-tracked
    // journey starts (BeginWalkHome/BeginWalkToAssignment), since a route
    // already suppresses the hide on its own from then on.
    private bool _heroWanderedAwayManually;
    // Tracks the domain's own hero.CurrentAssignment so a route to the
    // workplace fires exactly once per NEW assignment (see
    // EnsureHeroCarrier) — without this, every world tick re-triggered the
    // walk (or, worse, forced the carrier back to free-roam Macro state
    // while something else — the assignment/worker-slot flow — still
    // expected to own it), producing an endless fight that looked like the
    // citizen looping in place for no reason.
    private BuildingId? _lastKnownAssignment;
    private CitizenLocation? _lastKnownHeroLocation;
    private bool _treeHovered;

    private readonly record struct PlotBox(
        int Street,
        float LateralOffset,
        float Width,
        float Height,
        int BuildingId,
        BuildingKind Kind,
        bool IsUnderConstruction,
        bool IsClickable);

    private readonly record struct TreeBox(
        int Street,
        float LateralOffset,
        int ForestId,
        int UnitId,
        int Reserve,
        int TicksUntilRegeneration);

    public override void _Ready()
    {
        _controller = GetNode<CityWorldController>(ControllerPath);
        _statusPanel = GetNode<CityStatusPanel>(StatusPanelPath);
        _statusPanel.AttachController(_controller);
        _statusPanel.Refresh(_controller);
        _chronicle = GetNode<OfflineReportPanel>(ChroniclePath);
        _chronicle.SetController(_controller);
        _chronicle.Hide();
        _constructionMenuButton = GetNode<IconButton>(ConstructionMenuButtonPath);
        _constructionPanel = GetNode<ConstructionPanel>(ConstructionPanelPath);
        _expeditionMenuButton = GetNode<IconButton>(ExpeditionMenuButtonPath);
        _expeditionPanel = GetNode<ExpeditionPanel>(ExpeditionPanelPath);
        _modalHost = GetNode<ModalHost>(ModalHostPath);
        _macroActions = GetNode<Control>(MacroActionsPath);
        _buildingDetailView = GetNode<BuildingDetailView>(BuildingDetailViewPath);
        _cameraModeButton = GetNode<IconButton>(CameraModeButtonPath);
        _cursorController = GetNodeOrNull<CursorController>("/root/CursorController");
        _terrainAtlas = GD.Load<Texture2D>(ResourceTree.TerrainAtlasPath);
        // Pixel-art atlas tiles scale up crisp instead of smearing.
        TextureFilter = TextureFilterEnum.Nearest;

        _streetCount = WorldParcelRows * ParcelGrid.LotsPerAxis;
        _lateralHalfWidthPx =
            WorldParcelColumns * ParcelGrid.LotsPerAxis * LotUnitPx * 0.5f;

        _actionMenu = GD.Load<PackedScene>(ResourceActionMenuScenePath).Instantiate<ResourceActionMenu>();
        _actionMenu.GatherRequested += OnGatherRequested;
        // ScreenContent is still mid-_Ready() for its children, so transient
        // controls are attached after the scene-tree setup pass.
        GetParent().CallDeferred(Node.MethodName.AddChild, _actionMenu);
        _selectionInfoPanel = new SelectionInfoPanel();
        GetParent().CallDeferred(Node.MethodName.AddChild, _selectionInfoPanel);
        _navmeshPlanner = new StreetNavigationServerPlanner();
        BuildPlacementChrome();

        _controller.BuildingStateChanged += OnWorldChanged;
        _controller.ProjectStateChanged += OnWorldChanged;
        _controller.WorldTickAdvanced += OnWorldTickAdvanced;
        _controller.SelectionChanged += OnSelectionChanged;
        _controller.HeroCreated += OnHeroCreated;
        _constructionMenuButton.Pressed += OnConstructionMenuPressed;
        _expeditionMenuButton.Pressed += OnExpeditionMenuPressed;
        _constructionPanel.PlacementRequested += OnPlacementRequested;
        _constructionPanel.CloseRequested += OnConstructionPanelCloseRequested;
        _modalHost.Closed += OnModalHostClosedForButtonLabel;
        _cameraModeButton.Pressed += ToggleCameraMode;
        UpdateCameraModeButtonLabel();
        UpdateConstructionButtonLabel();

        RefreshPlots();
        Visible = false;
        // ScreenContent (this node's parent) sits below CityStatusPanel
        // inside a VBoxContainer, so its own local (0,0) is NOT the
        // viewport's top-left — CenterX/BaseY assume it is. Cancel that
        // offset once layout has settled (call_deferred runs after this
        // frame's container pass) instead of hardcoding the status bar's
        // height.
        CallDeferred(MethodName.NormalizePosition);

        // Activate up front unless onboarding still needs to run.
        if (!_controller.NeedsOnboarding())
        {
            ActivatePerspective();
        }
    }

    private void NormalizePosition()
    {
        Position -= GlobalPosition;
        _neutralPosition = Position;
    }

    public override void _ExitTree()
    {
        _controller.BuildingStateChanged -= OnWorldChanged;
        _controller.ProjectStateChanged -= OnWorldChanged;
        _controller.WorldTickAdvanced -= OnWorldTickAdvanced;
        _controller.SelectionChanged -= OnSelectionChanged;
        _controller.HeroCreated -= OnHeroCreated;
        _actionMenu.GatherRequested -= OnGatherRequested;
        _constructionMenuButton.Pressed -= OnConstructionMenuPressed;
        _constructionPanel.PlacementRequested -= OnPlacementRequested;
        _constructionPanel.CloseRequested -= OnConstructionPanelCloseRequested;
        _modalHost.Closed -= OnModalHostClosedForButtonLabel;
        _cameraModeButton.Pressed -= ToggleCameraMode;
        _placementConfirmButton.Pressed -= OnPlacementConfirmPressed;
        _placementCancelButton.Pressed -= OnPlacementCancelPressed;
        _navmeshPlanner?.Dispose();
    }

    /// <summary>
    /// Confirm/Cancel footer + instruction label for placement mode. The
    /// lots themselves are drawn and
    /// hit-tested like every other element in this view (see
    /// <see cref="_Draw"/>/<see cref="TryClick"/>), not as a button grid,
    /// since their position depends on the depth projection.
    /// </summary>
    private void BuildPlacementChrome()
    {
        _placementInstruction = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = "SectionTitle",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            OffsetTop = 12,
            OffsetBottom = 48,
            Visible = false,
        };
        _placementInstruction.SetAnchorsPreset(Control.LayoutPreset.TopWide);

        var footer = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            OffsetTop = -64,
            OffsetBottom = -16,
            Visible = false,
        };
        footer.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        footer.AddThemeConstantOverride("separation", 12);
        _placementConfirmButton = new Button
        {
            Text = UiText.Get("Confirm placement"),
            ThemeTypeVariation = "ButtonPrimary",
            Disabled = true,
        };
        _placementCancelButton = new IconButton { ThemeTypeVariation = "ButtonText" };
        ((IconButton)_placementCancelButton).SetIconAndLabel(IconPaths.Close, UiText.Get("Cancel"));
        footer.AddChild(_placementConfirmButton);
        footer.AddChild(_placementCancelButton);
        _placementConfirmButton.Pressed += OnPlacementConfirmPressed;
        _placementCancelButton.Pressed += OnPlacementCancelPressed;

        _placementFooter = footer;
        GetParent().CallDeferred(Node.MethodName.AddChild, _placementInstruction);
        GetParent().CallDeferred(Node.MethodName.AddChild, footer);
    }

    /// <summary>
    /// Makes the city visible once founder onboarding completes.
    /// </summary>
    private void OnHeroCreated(int citizenId) => ActivatePerspective();

    private void OnWorldChanged(int _)
    {
        _statusPanel.Refresh(_controller);
        RefreshPlots();
        RefreshConstructionPanelIfOpen();
        RefreshChronicleIfActive();
    }

    private void OnWorldTickAdvanced(int _)
    {
        _statusPanel.Refresh(_controller);
        RefreshPlots();
        RefreshConstructionPanelIfOpen();
        RefreshChronicleIfActive();
    }

    /// <summary>
    /// Keeps the city hidden while a detail/profile screen owns selection.
    /// </summary>
    private void OnSelectionChanged(int selectionState)
    {
        _selectionIsMacro =
            (CityWorldController.Selection)selectionState == CityWorldController.Selection.MacroView;
        if (!_selectionIsMacro)
        {
            Deactivate();
            return;
        }
        ActivatePerspective();
    }

    /// <summary>
    /// Shows the canonical city view.
    /// </summary>
    private void ActivatePerspective()
    {
        _macroActions.Show();
        _actionMenu.Hide();
        _selectionInfoPanel.Hide();
        Show();
        RefreshPlots();
        _chronicle.ShowLog(_controller.GetCityMacroSnapshot().Events);
    }

    public Vector2 GetFoundingArrivalGlobalPosition()
    {
        IReadOnlyList<ConstructionLot> lots = _controller.AvailableConstructionLots();
        if (lots.Count == 0) return ToGlobal(new Vector2(CenterX, BaseY));

        ConstructionLot lot = lots[0];
        int street = lot.ParcelRow * ParcelGrid.LotsPerAxis + lot.LotRow;
        float totalLotColumns = WorldParcelColumns * ParcelGrid.LotsPerAxis;
        float lotCenterColumn = lot.ParcelColumn * ParcelGrid.LotsPerAxis + lot.LotColumn + 0.5f;
        float lateral = (lotCenterColumn - totalLotColumns * 0.5f) * LotUnitPx;
        (Vector2 position, _) = StreetDepthProjection.Project(
            AnchorDepth(street - CameraDepthAnchor),
            lateral - CameraLateral,
            CenterX,
            BaseY);
        return ToGlobal(position);
    }

    public void PrepareFounderArrival()
    {
        ActivatePerspective();
        _macroActions.Hide();
        _heroCarrier?.SetState(CitizenSpriteCarrier.VisualState.Hidden);
    }

    public void CompleteFounderArrival()
    {
        ActivatePerspective();
        _macroActions.Show();
        EnsureHeroCarrier(_controller.GetCityMacroSnapshot());
    }

    /// <summary>Hides this view plus its own transient surfaces (menu, axe cursor, placement, selection, zoom).</summary>
    private void Deactivate()
    {
        Hide();
        _macroActions.Hide();
        _chronicle.Hide();
        _actionMenu.Hide();
        _selectionInfoPanel.Hide();
        _selectedTree = null;
        _selectedBuildingId = null;
        ClearTreeHover();
        if (_placementActive) EndPlacement();
        ResetZoom();
    }

    private void RefreshChronicleIfActive()
    {
        if (Visible && _selectionIsMacro)
        {
            _chronicle.ShowLog(_controller.GetCityMacroSnapshot().Events);
        }
    }

    /// <summary>
    /// Always starts fresh next time this view reactivates — a building
    /// entry's zoom-in push (<see cref="BeginBuildingEntry"/>) must not
    /// leave the map zoomed in when the player later backs out to it.
    /// </summary>
    private void ResetZoom()
    {
        _zoomLevel = 1f;
        Scale = Vector2.One;
        Position = _neutralPosition;
        _pendingBuildingEntry = null;
    }

    /// <summary>Opens or closes construction from the city toolbar.</summary>
    private void OnConstructionMenuPressed()
    {
        if (!Visible) return;
        if (_placementActive)
        {
            CancelPlacement();
            return;
        }
        if (_modalHost.IsOpen)
        {
            _modalHost.Close();
        }
        else
        {
            _modalHost.Open(_constructionPanel);
            _constructionPanel.Refresh();
        }
        UpdateConstructionButtonLabel();
    }

    private void OnExpeditionMenuPressed()
    {
        if (_modalHost.IsOpen && _modalHost.Content == _expeditionPanel)
        {
            _expeditionPanel.Close();
            return;
        }
        _expeditionPanel.Open();
    }

    /// <summary>Starts perspective-native lot placement.</summary>
    private void OnPlacementRequested(int constructionKind)
    {
        if (!Visible) return;
        IReadOnlyList<ConstructionLot> lots = _controller.AvailableConstructionLots();
        if (lots.Count == 0)
        {
            Notifier.ShowError(UiText.Get("No unlocked parcel has a free building lot."));
            return;
        }
        _modalHost.Close();
        BeginPlacement((ConstructionKind)constructionKind, lots);
    }

    private void OnConstructionPanelCloseRequested()
    {
        if (!Visible) return;
        _modalHost.Close();
    }

    internal void ShowConstructionForVisualRegression(bool placement)
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        ActivatePerspective();
        if (placement)
        {
            OnPlacementRequested((int)ConstructionKind.Farm);
            return;
        }
        _modalHost.Open(_constructionPanel);
        _constructionPanel.Refresh();
        _constructionPanel.ScrollBodyToEndForVisualRegression();
    }

    private void OnModalHostClosedForButtonLabel()
    {
        if (!Visible) return;
        UpdateConstructionButtonLabel();
    }

    private void UpdateConstructionButtonLabel()
    {
        if (_modalHost.IsOpen && _modalHost.Content == _constructionPanel)
        {
            _constructionMenuButton.SetIconAndLabel(IconPaths.Close, UiText.Get("Close construction"));
            _constructionMenuButton.TooltipText = UiText.Get("Close the construction menu (work continues).");
        }
        else if (_placementActive)
        {
            _constructionMenuButton.SetIconAndLabel(IconPaths.Close, UiText.Get("Cancel"));
        }
        else
        {
            _constructionMenuButton.SetIconAndLabel(IconPaths.Plus, "Construction");
            _constructionMenuButton.TooltipText = UiText.Get("Open the construction menu.");
        }
    }

    private void RefreshConstructionPanelIfOpen()
    {
        if (Visible && _modalHost.IsOpen && _modalHost.Content == _constructionPanel)
        {
            _constructionPanel.Refresh();
        }
    }

    /// <summary>
    /// Enters select-then-confirm placement mode: projects each available
    /// lot at its calle/lateral position (same mapping <see cref="AddPlot"/>
    /// uses), matching real domain <see cref="ConstructionLot"/> data —
    /// standard blueprints are always exactly one lot wide/tall in
    /// production today (see H-26), so <see cref="LotUnitPx"/> is used
    /// directly for both dimensions.
    /// </summary>
    private void BeginPlacement(ConstructionKind kind, IReadOnlyList<ConstructionLot> lots)
    {
        _placementActive = true;
        _placementKind = kind;
        _selectedPlacementLot = null;
        _placementConfirmButton.Disabled = true;
        _placementInstruction.Text = UiText.Format(
            "ui.construction.choose_lot",
            UiText.Get(ConstructionRules.DisplayNameFor(kind)));
        _placementInstruction.Visible = true;
        _placementFooter.Visible = true;
        _actionMenu.Hide();
        _selectionInfoPanel.Hide();
        _selectedTree = null;
        _selectedBuildingId = null;
        ClearTreeHover();
        _macroActions.Hide();
        // Placement always renders lots around the founder's current
        // position — force follow mode for the duration so lots never
        // render relative to wherever a free camera happens to be looking.
        _wasFreeCameraBeforePlacement = !_cameraFollowsHero;
        if (!_cameraFollowsHero) SetCameraFollowsHero(true);

        float totalLotColumns = WorldParcelColumns * ParcelGrid.LotsPerAxis;
        _placementLots.Clear();
        foreach (ConstructionLot lot in lots)
        {
            int street = lot.ParcelRow * ParcelGrid.LotsPerAxis + lot.LotRow;
            float lotCenterColumn = lot.ParcelColumn * ParcelGrid.LotsPerAxis + lot.LotColumn + 0.5f;
            float lateralOffset = (lotCenterColumn - totalLotColumns * 0.5f) * LotUnitPx;
            _placementLots.Add(new PlacementLotBox(lot, street, lateralOffset, LotUnitPx, LotUnitPx));
        }
        QueueRedraw();
    }

    private void EndPlacement()
    {
        _placementActive = false;
        _selectedPlacementLot = null;
        _placementLots.Clear();
        _clickablePlacementRects.Clear();
        _placementInstruction.Visible = false;
        _placementFooter.Visible = false;
        _macroActions.Show();
        UpdateConstructionButtonLabel();
        if (_wasFreeCameraBeforePlacement) SetCameraFollowsHero(false);
        QueueRedraw();
    }

    private void SelectPlacementLot(PlacementLotBox lot)
    {
        _selectedPlacementLot = lot.Lot;
        _placementConfirmButton.Disabled = false;
        QueueRedraw();
    }

    private void OnPlacementConfirmPressed()
    {
        if (_selectedPlacementLot is not ConstructionLot lot) return;
        ConstructionAuthorizationResult result = _controller.TryAuthorizeConstruction(_placementKind, lot);
        if (!result.IsSuccess)
        {
            Notifier.ShowError(ConstructionPanel.FormatAuthorizationError(result.Outcome));
            return;
        }
        EndPlacement();
        RefreshPlots();
    }

    private void OnPlacementCancelPressed() => CancelPlacement();

    /// <summary>
    /// Cancelling returns to the blueprint-choice panel: the player backed
    /// out of a specific lot, not out of building altogether.
    /// </summary>
    private void CancelPlacement()
    {
        EndPlacement();
        _modalHost.Open(_constructionPanel);
        _constructionPanel.Refresh();
        UpdateConstructionButtonLabel();
    }

    /// <summary>
    /// Calle = lot-row, lateral = lot-column (design bible §08, "Ciudad
    /// macro (perspectiva por calles)"), same mapping validated in
    /// <c>RealCityStreetPreview.cs</c>. Only completed buildings are
    /// clickable in this slice — in-progress projects render but do not
    /// open anything yet (they map to the construction panel in the real
    /// game, not <c>BuildingDetailView</c>; out of scope here).
    /// </summary>
    private void RefreshPlots()
    {
        _plots.Clear();
        _trees.Clear();
        _bandOccupancy.Clear();
        CityMacroSnapshot snapshot = _controller.GetCityMacroSnapshot();
        float totalLotColumns = WorldParcelColumns * ParcelGrid.LotsPerAxis;

        foreach (CityMacroSnapshot.PlotItem item in snapshot.Buildings)
        {
            if (item.Kind == BuildingKind.Forest)
            {
                AddTrees(item, totalLotColumns);
                continue;
            }
            AddPlot(item, totalLotColumns, clickable: true);
        }
        foreach (CityMacroSnapshot.PlotItem item in snapshot.Projects)
        {
            AddPlot(item, totalLotColumns, clickable: false);
        }
        if (Visible)
        {
            EnsureHeroCarrier(snapshot);
            RefreshCitizenVisuals(snapshot);
        }
        RefreshSelectionInfoIfShown();
        QueueRedraw();
    }

    /// <summary>
    /// Keeps the selection panel's numbers (wood remaining, regrowth
    /// time) live as the world ticks — a gather or regeneration tick
    /// would otherwise leave stale figures on screen. Clears the
    /// selection if the selected tree is gone (fully depleted units are
    /// dropped from <see cref="_trees"/> — see <see cref="AddTrees"/>).
    /// </summary>
    private void RefreshSelectionInfoIfShown()
    {
        if (_selectedTree is { } selectedTree)
        {
            foreach (TreeBox tree in _trees)
            {
                if (tree.ForestId != selectedTree.ForestId || tree.UnitId != selectedTree.UnitId) continue;
                SelectTree(tree);
                return;
            }
            ClearSelection();
            return;
        }
        if (_selectedBuildingId is not { } buildingId) return;
        foreach (PlotBox plot in _plots)
        {
            if (plot.BuildingId != buildingId) continue;
            SelectBuildingPlot(buildingId);
            return;
        }
        ClearSelection();
    }

    /// <summary>
    /// Each forest patch has one reserve per individual tree
    /// (<c>WoodUnitReserves</c>); <c>ParcelGrid.NaturalResourceLot</c>
    /// gives each unit its own lot within the patch's parcel, so trees get
    /// the same calle/lateral projection as buildings instead of all
    /// bunching up at the patch's own (fixed 0,0) lot. Depleted units
    /// (reserve 0) are skipped.
    /// </summary>
    private void AddTrees(CityMacroSnapshot.PlotItem forest, float totalLotColumns)
    {
        for (int unitId = 0; unitId < forest.WoodUnitReserves.Count; unitId++)
        {
            if (forest.WoodUnitReserves[unitId] <= 0) continue;
            (int lotColumn, int lotRow) = ParcelGrid.NaturalResourceLot(unitId);
            int street = forest.ParcelRow * ParcelGrid.LotsPerAxis + lotRow;
            float lotCenterColumn = forest.ParcelColumn * ParcelGrid.LotsPerAxis + lotColumn + 0.5f;
            float lateralOffset = (lotCenterColumn - totalLotColumns * 0.5f) * LotUnitPx;
            _trees.Add(new TreeBox(
                street,
                lateralOffset,
                forest.Id.Value,
                unitId,
                forest.WoodUnitReserves[unitId],
                forest.TicksUntilRegeneration));
            AddBandInterval(street, lateralOffset - TreeBlockHalfWidthPx, lateralOffset + TreeBlockHalfWidthPx);
        }
    }

    private void AddPlot(CityMacroSnapshot.PlotItem item, float totalLotColumns, bool clickable)
    {
        int street = item.ParcelRow * ParcelGrid.LotsPerAxis + item.LotRow;
        float lotCenterColumn = item.ParcelColumn * ParcelGrid.LotsPerAxis
            + item.LotColumn
            + item.LotWidth * 0.5f;
        float lateralOffset = (lotCenterColumn - totalLotColumns * 0.5f) * LotUnitPx;
        float width = item.LotWidth * LotUnitPx;
        _plots.Add(new PlotBox(
            street,
            lateralOffset,
            width,
            item.LotHeight * LotUnitPx,
            item.Id.Value,
            item.Kind,
            item.IsUnderConstruction,
            clickable));
        AddBandInterval(street, lateralOffset - width * 0.5f, lateralOffset + width * 0.5f);
    }

    private void AddBandInterval(int band, float start, float end)
    {
        if (!_bandOccupancy.TryGetValue(band, out List<StreetRoutePlanner.Interval>? intervals))
        {
            intervals = new List<StreetRoutePlanner.Interval>();
            _bandOccupancy[band] = intervals;
        }
        intervals.Add(new StreetRoutePlanner.Interval(start, end));
    }

    private IReadOnlyList<StreetRoutePlanner.Interval> GetBandOccupancy(int band) =>
        _bandOccupancy.TryGetValue(band, out List<StreetRoutePlanner.Interval>? intervals)
            ? intervals
            : EmptyBand;

    /// <summary>The vanishing point's lateral position — the founder's own
    /// while following, an independently-steered value while free.</summary>
    private float CameraLateral => _cameraFollowsHero ? _heroLateral : _freeCameraLateral;

    /// <summary>The vanishing point's smoothed depth — see the class doc's
    /// "Camera mode" note and <see cref="AdvanceTransition"/>.</summary>
    private float CameraDepthAnchor => _cameraFollowsHero ? _depthAnchor : _cameraDepthAnchor;

    public override void _Process(double delta)
    {
        if (!Visible) return;
        _motionAccumulator += (float)delta;
        while (_motionAccumulator >= PixelMotion.CadenceSeconds)
        {
            _motionAccumulator -= PixelMotion.CadenceSeconds;
            MotionTick();
        }
        // The founder's own smoothed row (always active — it also paces
        // AdvanceRouteTick regardless of camera mode) and, independently,
        // the free camera's own smoothed row when not following.
        bool heroDepthAnimating = _depthTarget.HasValue;
        bool cameraDepthAnimating = _cameraDepthTarget.HasValue;
        AdvanceTransition(ref _depthAnchor, ref _depthTarget, ref _transitionAccumulator, delta);
        AdvanceTransition(ref _cameraDepthAnchor, ref _cameraDepthTarget, ref _cameraTransitionAccumulator, delta);
        if (heroDepthAnimating || cameraDepthAnimating) QueueRedraw();
        AdvanceBuildingEntry(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible) return;
        // A building-entry push is a brief, exclusive, non-interruptible
        // transition — same spirit as the fullscreen placement scrim.
        if (_pendingBuildingEntry is not null) return;
        if (_placementActive && @event.IsActionPressed("ui_cancel"))
        {
            CancelPlacement();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (_actionMenu.Visible && @event.IsActionPressed("ui_cancel"))
        {
            _actionMenu.Hide();
            GetViewport().SetInputAsHandled();
            return;
        }
        switch (@event)
        {
            case InputEventKey { Pressed: true, Echo: false } keyEvent
                when keyEvent.Keycode is Key.Up or Key.W:
                StepStreet(1);
                break;
            case InputEventKey { Pressed: true, Echo: false } keyEvent
                when keyEvent.Keycode is Key.Down or Key.S:
                StepStreet(-1);
                break;
            case InputEventKey { Pressed: true, Echo: false } keyEvent
                when keyEvent.Keycode == Key.F:
                ToggleCameraMode();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true }:
                AdjustZoom(ZoomStep);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true }:
                AdjustZoom(-ZoomStep);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click:
                TryClick(ToLocal(click.Position));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } rightClick:
                TryRightClick(ToLocal(rightClick.Position));
                break;
            case InputEventMouseMotion motion:
                UpdateTreeHover(ToLocal(motion.Position));
                break;
        }
    }

    /// <summary>
    /// Quantized camera zoom (discrete steps, never a continuous drag) via
    /// this node's own <see cref="Node2D.Scale"/>, keeping the vanishing
    /// point (<see cref="CenterX"/>,<see cref="BaseY"/> in local space)
    /// fixed on screen so zooming feels centered on the avatar rather than
    /// dragging the whole view toward a corner.
    /// </summary>
    private void AdjustZoom(float delta)
    {
        float newZoom = Mathf.Clamp(_zoomLevel + delta, MinZoom, MaxZoom);
        if (Mathf.IsEqualApprox(newZoom, _zoomLevel)) return;
        ZoomTowardPivot(newZoom, new Vector2(CenterX, BaseY));
    }

    /// <summary>
    /// Applies <paramref name="newZoom"/> as this node's own Scale, keeping
    /// <paramref name="pivotLocal"/> fixed on screen — the general form of
    /// what <see cref="AdjustZoom"/> already did around the fixed vanishing
    /// point; <see cref="BeginBuildingEntry"/> reuses it around an
    /// arbitrary clicked building's position instead.
    /// </summary>
    private void ZoomTowardPivot(float newZoom, Vector2 pivotLocal)
    {
        Vector2 oldScale = Scale;
        var newScale = Vector2.One * newZoom;
        Position += pivotLocal * (oldScale - newScale);
        Scale = newScale;
        _zoomLevel = newZoom;
    }

    /// <summary>
    /// Camera mode toggle (design bible §04 "Cámara-sigue"): follow the
    /// founder (default) or pan freely, independent of any selection.
    /// Ignored during placement, which always forces follow (see
    /// BeginPlacement) so lots keep rendering around the founder.
    /// </summary>
    private void ToggleCameraMode()
    {
        if (_placementActive) return;
        SetCameraFollowsHero(!_cameraFollowsHero);
    }

    private void SetCameraFollowsHero(bool value)
    {
        if (_cameraFollowsHero == value) return;
        if (!value)
        {
            // Entering free mode starts exactly where follow mode left
            // off — no visual jump at the moment of toggling, only
            // subsequent free-camera input diverges it from the founder.
            _freeCameraLateral = _heroLateral;
            _freeCameraStreet = _heroStreet;
            _cameraDepthAnchor = _depthAnchor;
            _cameraDepthTarget = null;
            _cameraTransitionAccumulator = 0f;
        }
        _cameraFollowsHero = value;
        UpdateCameraModeButtonLabel();
        QueueRedraw();
    }

    private void UpdateCameraModeButtonLabel()
    {
        _cameraModeButton.SetIconAndLabel(
            IconPaths.User,
            _cameraFollowsHero ? UiText.Get("ui.camera.follow_label") : UiText.Get("ui.camera.free_label"));
        _cameraModeButton.TooltipText = _cameraFollowsHero
            ? UiText.Get("ui.camera.follow_tooltip")
            : UiText.Get("ui.camera.free_tooltip");
    }

    /// <summary>
    /// Left click: select. Both trees and buildings populate
    /// <see cref="_selectionInfoPanel"/> with their details instead of
    /// immediately acting — right click is reserved for actions (gather,
    /// entering a building) — see <see cref="TryRightClick"/>.
    /// </summary>
    private void TryClick(Vector2 clickPosition)
    {
        // A click that reaches this method never landed ON the gather
        // menu itself (its own Stop mouse filter would have consumed it
        // first via GUI input, before _UnhandledInput ever fires) — so
        // getting here at all means "outside" it. Dismiss without
        // gathering.
        if (_actionMenu.Visible) _actionMenu.Hide();

        // Placement mode is exclusive and blocks every other world click
        // while a lot is being chosen.
        if (_placementActive)
        {
            foreach ((Rect2 rect, PlacementLotBox lot) in _clickablePlacementRects)
            {
                if (!rect.HasPoint(clickPosition)) continue;
                SelectPlacementLot(lot);
                return;
            }
            return;
        }
        foreach ((Rect2 rect, TreeBox tree) in _clickableTreeRects)
        {
            if (!rect.HasPoint(clickPosition)) continue;
            SelectTree(tree);
            return;
        }
        foreach ((Rect2 rect, int buildingId) in _clickableRects)
        {
            if (!rect.HasPoint(clickPosition)) continue;
            SelectBuildingPlot(buildingId);
            return;
        }
        // Clicked empty ground: nothing selected any more.
        ClearSelection();
    }

    /// <summary>
    /// Right click: act. Trees gather (via the bare icon-only action
    /// button, matching the reference minimalist interaction style);
    /// buildings enter directly (see <see cref="BeginBuildingEntry"/> for
    /// the map's own camera push toward the clicked building). Also
    /// refreshes the selection info so the corner panel always reflects
    /// whatever the player is currently acting on.
    /// </summary>
    private void TryRightClick(Vector2 clickPosition)
    {
        if (_placementActive) return;
        foreach ((Rect2 rect, TreeBox tree) in _clickableTreeRects)
        {
            if (!rect.HasPoint(clickPosition)) continue;
            SelectTree(tree);
            OpenGatherMenu(tree, rect);
            return;
        }
        foreach ((Rect2 rect, int buildingId) in _clickableRects)
        {
            if (!rect.HasPoint(clickPosition)) continue;
            SelectBuildingPlot(buildingId);
            BeginBuildingEntry(new BuildingId(buildingId), clickPosition);
            return;
        }
        if (_actionMenu.Visible) _actionMenu.Hide();
    }

    private void SelectTree(TreeBox tree)
    {
        _selectedTree = tree;
        _selectedBuildingId = null;
        Texture2D icon = ResourceTree.CreateRegion(
            _terrainAtlas,
            (tree.ForestId + tree.UnitId) % 2 == 0 ? ResourceTree.TreeAtlasColumnA : ResourceTree.TreeAtlasColumnB,
            ResourceTree.TreeAtlasRow);
        int futureTick = _controller.World.CurrentTick + tree.TicksUntilRegeneration;
        string detail = UiText.Format("ui.resource.wood_remains", tree.Reserve)
            + "\n"
            + UiText.Format("ui.tree.regrows_at", SimulationTimeText.FormatLocalized(futureTick));
        _selectionInfoPanel.ShowSelection(icon, UiText.Get("ui.selection.tree_title"), detail);
    }

    /// <summary>
    /// Populates the selection panel with a quick building summary — worker
    /// occupancy for productive buildings, resting count for Home. The full
    /// assignment/production surface remains <see cref="BuildingDetailView"/>
    /// (opened by right-click); this is only the at-a-glance info left
    /// click now shows, same role it plays for trees.
    /// </summary>
    private void SelectBuildingPlot(int buildingId)
    {
        BuildingDetailSnapshot? snapshot = _controller.GetBuildingDetailSnapshot(new BuildingId(buildingId));
        if (snapshot is null)
        {
            ClearSelection();
            return;
        }
        _selectedBuildingId = buildingId;
        _selectedTree = null;
        Texture2D? icon = GetBuildingTexture(snapshot.Kind);
        int occupants = snapshot.VisibleWorkerCount + snapshot.HiddenWorkerCount;
        string detail = snapshot.IsHome
            ? UiText.Format("ui.selection.building_home", occupants, snapshot.WorkerCapacity)
            : UiText.Format("ui.selection.building_workers", occupants, snapshot.WorkerCapacity);
        string fullLabel = UiText.Format(
            "ui.building_detail.full_label", UiText.Get(snapshot.DisplayName), UiText.Get(snapshot.ResourceLabel));
        _selectionInfoPanel.ShowSelection(icon, fullLabel, detail);
    }

    /// <summary>Extension point: citizens will route here too once they get selection info.</summary>
    private void ClearSelection()
    {
        if (_selectedTree is null && _selectedBuildingId is null) return;
        _selectedTree = null;
        _selectedBuildingId = null;
        _selectionInfoPanel.Hide();
    }

    /// <summary>
    /// Camera push toward the clicked building, in a handful of DISCRETE
    /// zoom steps (see <see cref="AdvanceBuildingEntry"/>) applied to this
    /// node's own Scale/Position — i.e. on the MAP, not on
    /// <see cref="BuildingDetailView"/> (which no longer animates itself;
    /// the camera push already reads as "entering" before it takes over).
    /// Blocks other input for its short duration (see <see cref="_UnhandledInput"/>).
    /// </summary>
    private void BeginBuildingEntry(BuildingId buildingId, Vector2 pivotLocal)
    {
        if (_pendingBuildingEntry is not null) return;
        _pendingBuildingEntry = buildingId;
        _buildingEntryPivotLocal = pivotLocal;
        _buildingEntryStartZoom = _zoomLevel;
        _buildingEntryStep = 0;
        _buildingEntryAccumulator = 0f;
    }

    private void AdvanceBuildingEntry(double delta)
    {
        if (_pendingBuildingEntry is not { } buildingId) return;
        _buildingEntryAccumulator += (float)delta;
        while (_buildingEntryAccumulator >= PixelMotion.CadenceSeconds && _pendingBuildingEntry is not null)
        {
            _buildingEntryAccumulator -= PixelMotion.CadenceSeconds;
            _buildingEntryStep++;
            float t = (float)_buildingEntryStep / BuildingEntryZoomSteps;
            ZoomTowardPivot(Mathf.Lerp(_buildingEntryStartZoom, BuildingEntryZoomLevel, t), _buildingEntryPivotLocal);
            if (_buildingEntryStep >= BuildingEntryZoomSteps)
            {
                _pendingBuildingEntry = null;
                _controller.SelectBuilding(buildingId);
            }
        }
    }

    /// <summary>
    /// Same affordance the flat view's <c>ResourceTree</c> gives: the axe
    /// cursor over a living tree. Draw-based rects have no Control hover
    /// signals, so the transition is tracked from mouse motion here.
    /// </summary>
    private void UpdateTreeHover(Vector2 mousePosition)
    {
        if (_placementActive)
        {
            ClearTreeHover();
            return;
        }
        bool hovering = false;
        foreach ((Rect2 rect, TreeBox _) in _clickableTreeRects)
        {
            if (!rect.HasPoint(mousePosition)) continue;
            hovering = true;
            break;
        }
        if (hovering == _treeHovered) return;
        _treeHovered = hovering;
        if (hovering) _cursorController?.UseGatherCursor();
        else _cursorController?.RestoreSurfaceCursor();
    }

    private void ClearTreeHover()
    {
        if (!_treeHovered) return;
        _treeHovered = false;
        _cursorController?.RestoreSurfaceCursor();
    }

    /// <summary>
    /// Gathering requires an unassigned hero who is not on expedition.
    /// Unlike the first slice,
    /// confirming now routes the hero along the street network to the tree
    /// before gathering — see <see cref="OnGatherRequested"/>.
    /// </summary>
    private void OpenGatherMenu(TreeBox tree, Rect2 rect)
    {
        // The menu is about to take over the mouse: reset the world's
        // gather-cursor override now, since the button underneath the
        // cursor already carries its own axe icon, and CursorController
        // gives every Button a pointing-hand by default — without this
        // reset, moving from the tree straight onto the menu (a Control
        // with its own input handling that this Node2D never sees again)
        // would leave the axe cursor stuck, showing the same icon twice.
        ClearTreeHover();
        Citizen? hero = _controller.World.Hero;
        bool onExpedition = hero is not null && _controller.World.IsCitizenOnActiveExpedition(hero.Id);
        bool canGather = hero is not null && !hero.CurrentAssignment.HasValue && !onExpedition;
        string unavailableReason = hero is null
            ? UiText.Get("No founder is available to gather.")
            : onExpedition
                ? UiText.Format("ui.gather.away_on_expedition", hero.Name)
                : UiText.Format("ui.gather.already_assigned", hero.Name);
        // The menu is a sibling child of ScreenContent, not a child of this
        // (possibly zoomed/offset) Node2D — convert the local rect center to
        // global space first, then into ScreenContent's own local space.
        Vector2 menuAnchor = ToGlobal(rect.GetCenter()) - ((Control)GetParent()).GlobalPosition;
        _actionMenu.Open(
            tree.ForestId,
            tree.UnitId,
            menuAnchor,
            menuAnchor,
            canGather,
            unavailableReason);
    }

    private void OnGatherRequested(int forestId, int unitId, Vector2 _)
    {
        TreeBox? target = null;
        foreach (TreeBox tree in _trees)
        {
            if (tree.ForestId != forestId || tree.UnitId != unitId) continue;
            target = tree;
            break;
        }
        if (target is null)
        {
            Notifier.ShowError(UiText.Get("This tree no longer has wood available."));
            return;
        }
        EnsureHeroCarrierReadyToMove();
        _pendingReturnHome = false;
        _pendingAssignment = null;
        _pendingGather = (forestId, unitId);
        _route = PlanHeroRoute(_heroStreet, _heroLateral, target.Value.Street, target.Value.LateralOffset);
        _routeIndex = 0;
    }

    /// <summary>
    /// One 12 Hz quantized motion step. In follow mode: the founder's
    /// route first, then manual input (exactly as before camera modes
    /// existed). In free mode, the founder keeps acting on its own — a
    /// route or idle settle — independent of the free camera's own manual
    /// input, since the two are no longer the same thing.
    /// </summary>
    private void MotionTick()
    {
        if (_placementActive) return;
        if (_cameraFollowsHero)
        {
            if (_route is not null)
            {
                AdvanceRouteTick();
                return;
            }
            bool stepped = TryStepHeroLateral();
            if (!stepped && _heroWalking && !_depthTarget.HasValue)
            {
                _heroWalking = false;
                _heroCarrier?.Idle(Vector2.Down);
            }
            return;
        }
        if (_route is not null) AdvanceRouteTick();
        else if (_heroWalking && !_depthTarget.HasValue)
        {
            _heroWalking = false;
            _heroCarrier?.Idle(Vector2.Down);
        }
        TryStepFreeCameraLateral();
    }

    private void AdvanceRouteTick()
    {
        if (_depthTarget.HasValue) return; // mid street transition
        if (_route is null || _routeIndex >= _route.Count)
        {
            CompleteRoute();
            return;
        }
        StreetRoutePlanner.Waypoint waypoint = _route[_routeIndex];
        if (waypoint.Street != _heroStreet)
        {
            int direction = Mathf.Sign(waypoint.Street - _heroStreet);
            _heroCarrier?.Walk(direction > 0 ? Vector2.Up : Vector2.Down);
            _heroWalking = true;
            _heroStreet += direction;
            _depthTarget = _heroStreet;
            TrampleHeroTile();
            QueueRedraw();
            return;
        }
        if (Mathf.Abs(waypoint.Lateral - _heroLateral) >= 1f)
        {
            float direction = Mathf.Sign(waypoint.Lateral - _heroLateral);
            _heroCarrier?.Walk(direction > 0f ? Vector2.Right : Vector2.Left);
            _heroWalking = true;
            _heroLateral = Mathf.MoveToward(_heroLateral, waypoint.Lateral, PixelMotion.StepPixels);
            TrampleHeroTile();
            QueueRedraw();
            return;
        }
        _routeIndex++;
        if (_routeIndex >= _route.Count) CompleteRoute();
    }

    private void CompleteRoute()
    {
        _route = null;
        _routeIndex = 0;
        _heroWalking = false;
        if (_pendingReturnHome)
        {
            _pendingReturnHome = false;
            _controller.ConfirmCitizenArrivedHome(_controller.World.Hero!.Id);
            // Arrival means the citizen crossed the threshold. The macro
            // carrier disappears inside; a later order remounts the same
            // flyweight before planning its next route.
            _heroCarrier?.SetState(CitizenSpriteCarrier.VisualState.Hidden);
            return;
        }
        if (_pendingAssignment is BuildingId workplace)
        {
            bool arrived = _controller.ConfirmCitizenArrivedAtAssignment(
                workplace,
                _controller.World.Hero!.Id);
            if (arrived
                || _controller.World.Hero.CurrentLocation != CitizenLocation.InTransit)
            {
                _pendingAssignment = null;
            }
            // Facing "into" the workplace (deeper on this row), matching
            // the gather pose's own orientation once arrived.
            _heroCarrier?.Idle(Vector2.Up);
            return;
        }
        (int ForestId, int UnitId)? pending = _pendingGather;
        _pendingGather = null;
        if (pending is null)
        {
            _heroCarrier?.Idle(Vector2.Down);
            return;
        }
        // The tree stands behind the hero's road (deeper), so swing away
        // from the viewer, then settle back to idle after the one-shot.
        _heroCarrier?.Slash(Vector2.Up);
        int gathered = _controller.GatherWood(new BuildingId(pending.Value.ForestId), pending.Value.UnitId, 2);
        if (gathered > 0) Notifier.Show(UiText.Format("ui.gather.gathered_wood", gathered));
        else Notifier.ShowError("This tree no longer has wood available.");
        GetTree().CreateTimer(0.6).Timeout += () =>
        {
            if (IsInstanceValid(this) && _route is null && IsInstanceValid(_heroCarrier))
            {
                _heroCarrier?.Idle(Vector2.Up);
            }
        };
    }

    /// <summary>
    /// Manual depth step: moves the founder in follow mode (as before), or
    /// the free camera alone in free mode — see <see cref="_cameraFollowsHero"/>.
    /// </summary>
    private void StepStreet(int direction)
    {
        if (_cameraFollowsHero) StepHeroStreet(direction);
        else StepFreeCameraStreet(direction);
    }

    /// <summary>
    /// Founder's own manual depth step. Crossing is only viable through the
    /// gaps the constructions leave in the band between the two roads —
    /// the street-plan rule ("las calles viven entre las construcciones").
    /// </summary>
    private void StepHeroStreet(int direction)
    {
        // A citizen currently assigned to a workplace is busy there, not
        // free to wander — matches the flat view's own model (an assigned
        // worker renders at their workplace, not roaming the macro city).
        if (_placementActive || _lastKnownAssignment is not null || _depthTarget.HasValue || _route is not null) return;
        int nextStreet = Mathf.Clamp(_heroStreet + direction, 0, _streetCount - 1);
        if (nextStreet == _heroStreet) return;
        int band = direction > 0 ? _heroStreet : _heroStreet - 1;
        if (StreetRoutePlanner.IsCrossingBlocked(GetBandOccupancy(band), _heroLateral, RouteClearancePx))
        {
            Notifier.Show(UiText.Get("Something blocks the way — walk along the street to a gap first."));
            return;
        }
        EnsureHeroCarrierReadyToMove();
        _heroCarrier?.Walk(direction > 0 ? Vector2.Up : Vector2.Down);
        _heroWalking = true;
        _heroStreet = nextStreet;
        _depthTarget = _heroStreet;
        TrampleHeroTile();
    }

    /// <summary>
    /// Called before every hero-initiated macro action that this class does
    /// not learn about through a synchronous domain event: manual W/S/
    /// arrow-key movement, and <see cref="OnGatherRequested"/>'s route to a
    /// tree. An assignment is different — <c>TryAssignCitizen</c> fires
    /// <c>BuildingStateChanged</c>/<c>ProjectStateChanged</c> synchronously,
    /// forcing an <see cref="EnsureHeroCarrier"/> refresh (which un-hides
    /// the carrier) before <see cref="BeginWalkToAssignment"/> ever moves
    /// it. Neither manual movement nor a gather click touches the domain
    /// until the citizen already arrives, so nothing else would ever:
    /// <list type="bullet">
    /// <item><description>show the carrier if it was <see cref="CitizenSpriteCarrier.VisualState.Hidden"/>
    /// (settled inside the Shelter) — <see cref="CitizenSpriteCarrier.Walk"/>
    /// only plays the walk animation, and <see cref="UpdateHeroVisual"/>
    /// refuses to touch position/scale unless the state is already
    /// <see cref="CitizenSpriteCarrier.VisualState.Macro"/>, so the citizen
    /// would stay invisible while the dirt trail (which does not check
    /// carrier state) kept advancing under them;</description></item>
    /// <item><description>cancel a leftover <c>GoTo</c> motion from a
    /// different context, the same hazard <see cref="RefreshCitizenVisuals"/>'s
    /// ambient loop and <see cref="EnsureHeroCarrier"/>'s own Macro branch
    /// already guard against;</description></item>
    /// <item><description>keep the carrier from being re-hidden on the
    /// very next world tick: domain <c>CurrentLocation</c> never actually
    /// leaves <c>AtHome</c> for either of these actions (no domain travel
    /// is involved), so every subsequent <see cref="EnsureHeroCarrier"/>
    /// refresh would otherwise see "AtHome, no route, no pending return"
    /// and hide the citizen again — mid-walk, or worse, right as
    /// <see cref="CompleteRoute"/>'s gather branch calls <c>GatherWood</c>
    /// (itself a synchronous <c>BuildingStateChanged</c>) the instant after
    /// <see cref="_route"/> is cleared, undoing the arrival Slash animation
    /// before the player ever sees it.</description></item>
    /// </list>
    /// <see cref="_heroWanderedAwayManually"/> is cleared once a real
    /// domain-tracked journey takes over (<see cref="BeginWalkToAssignment"/>/
    /// <see cref="BeginWalkHome"/>/departing on an expedition).
    /// </summary>
    private void EnsureHeroCarrierReadyToMove()
    {
        _heroWanderedAwayManually = true;
        if (_heroCarrier is null) return;
        if (_heroCarrier.State != CitizenSpriteCarrier.VisualState.Macro)
        {
            _heroCarrier.SetState(CitizenSpriteCarrier.VisualState.Macro);
        }
        _heroCarrier.CancelMotion();
    }

    /// <summary>
    /// Free camera's own manual depth step — an observer, not a body, so
    /// unlike <see cref="StepHeroStreet"/> it never checks obstacle
    /// clearance (design bible §04: free pan is always available).
    /// </summary>
    private void StepFreeCameraStreet(int direction)
    {
        if (_placementActive || _cameraDepthTarget.HasValue) return;
        int nextStreet = Mathf.Clamp(_freeCameraStreet + direction, 0, _streetCount - 1);
        if (nextStreet == _freeCameraStreet) return;
        _freeCameraStreet = nextStreet;
        _cameraDepthTarget = _freeCameraStreet;
    }

    /// <summary>
    /// Shared stepped-transition advancer (design bible §08, pixel-motion
    /// grammar: discrete steps, never a continuous tween) — one instance
    /// paces the founder's own row-crossing pose, a second, independent
    /// instance paces the free camera's row-crossing when not following.
    /// </summary>
    private static void AdvanceTransition(ref float anchor, ref float? target, ref float accumulator, double delta)
    {
        if (!target.HasValue) return;
        accumulator += (float)delta;
        while (accumulator >= PixelMotion.CadenceSeconds && target.HasValue)
        {
            accumulator -= PixelMotion.CadenceSeconds;
            float value = target.Value;
            if (Mathf.Abs(value - anchor) <= DepthStepSize)
            {
                anchor = value;
                target = null;
            }
            else
            {
                anchor += Mathf.Sign(value - anchor) * DepthStepSize;
            }
        }
    }

    /// <summary>Returns true when the founder's own manual lateral step happened this tick.</summary>
    private bool TryStepHeroLateral()
    {
        if (_lastKnownAssignment is not null) return false;
        float direction = ReadLateralDirection();
        if (direction == 0f) return false;
        float next = Mathf.Clamp(
            _heroLateral + direction * PixelMotion.StepPixels,
            -_lateralHalfWidthPx,
            _lateralHalfWidthPx);
        if (next == _heroLateral) return false;
        EnsureHeroCarrierReadyToMove();
        _heroCarrier?.Walk(direction > 0f ? Vector2.Right : Vector2.Left);
        _heroWalking = true;
        _heroLateral = next;
        TrampleHeroTile();
        QueueRedraw();
        return true;
    }

    /// <summary>Free camera's own manual lateral step — no carrier/walk-pose side effects.</summary>
    private bool TryStepFreeCameraLateral()
    {
        float direction = ReadLateralDirection();
        if (direction == 0f) return false;
        float next = Mathf.Clamp(
            _freeCameraLateral + direction * PixelMotion.StepPixels,
            -_lateralHalfWidthPx,
            _lateralHalfWidthPx);
        if (next == _freeCameraLateral) return false;
        _freeCameraLateral = next;
        QueueRedraw();
        return true;
    }

    /// <summary>
    /// Godot's default "ui_left"/"ui_right" actions bind only the arrow
    /// keys, not A/D — but <see cref="_UnhandledInput"/> already accepts
    /// W/S as well as Up/Down for the depth axis, so a player naturally
    /// reaching for WASD would find the lateral axis unresponsive unless
    /// A/D are checked here too.
    /// </summary>
    private static float ReadLateralDirection()
    {
        if (Input.IsActionPressed("ui_left") || Input.IsKeyPressed(Key.A)) return -1f;
        if (Input.IsActionPressed("ui_right") || Input.IsKeyPressed(Key.D)) return 1f;
        return 0f;
    }

    /// <summary>
    /// Mounts the founder's canonical sprite carrier into this view. Only
    /// while visible — the flat view and the detail slots mount the same
    /// carrier, and whichever view is active owns it (one citizen, one
    /// sprite; docs/ARCHITECTURE.md §7b).
    ///
    /// Assignment-aware: when the hero is (or becomes) assigned to a
    /// building, this stops treating <see cref="_heroStreet"/>/
    /// <see cref="_heroLateral"/> as free-roam camera state and instead
    /// walks them to the workplace's own calle/lateral — matching the flat
    /// view's model, where an assigned worker's macro-view position is
    /// their workplace, not wherever they last wandered. A NEW assignment
    /// (tracked via <see cref="_lastKnownAssignment"/>) triggers exactly
    /// one route; without that guard, every world tick re-resolved this
    /// method and repeatedly yanked the carrier back into free-roam Macro
    /// state / re-triggered a walk, fighting whatever else (the
    /// worker-slot entrance animation in an open <c>BuildingDetailView</c>)
    /// also expected to own its position — visible as the citizen looping
    /// in place for no apparent reason.
    /// </summary>
    private void EnsureHeroCarrier(CityMacroSnapshot snapshot)
    {
        if (!Visible || snapshot.Hero is not { } hero) return;
        bool onExpedition = snapshot.Citizens.Count > 0 && snapshot.Citizens[0].IsOnExpedition;
        _heroCarrier = CitizenSpriteBank.Instance.GetOrCreate(
            hero.Id, hero.Lineage, hero.Gender, hero.Appearance);
        if (onExpedition)
        {
            if (_heroCarrier.GetParent() == this)
            {
                _heroCarrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
            }
            _lastKnownAssignment = null;
            _lastKnownHeroLocation = null;
            _heroWanderedAwayManually = false;
            return;
        }
        CitizenSpriteBank.Instance.Mount(_heroCarrier, this);
        BuildingId? currentAssignment = snapshot.Citizens.Count > 0
            ? snapshot.Citizens[0].CurrentAssignment
            : null;
        CitizenLocation heroLocation = snapshot.Citizens.Count > 0
            ? snapshot.Citizens[0].Location
            : CitizenLocation.AtHome;
        bool hasShelter = snapshot.Buildings.Any(building =>
            building.Kind == BuildingKind.Home && !building.IsUnderConstruction);
        if (ShouldHideHeroInsideShelter(
            currentAssignment,
            heroLocation,
            hasShelter,
            hasRoute: _route is not null,
            pendingReturnHome: _pendingReturnHome,
            hasWanderedManually: _heroWanderedAwayManually))
        {
            _heroCarrier.CancelMotion();
            _heroCarrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
            _lastKnownAssignment = null;
            _lastKnownHeroLocation = heroLocation;
            return;
        }
        if (currentAssignment.HasValue && heroLocation == CitizenLocation.AtWork)
        {
            _heroCarrier.CancelMotion();
            _heroCarrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
            _lastKnownAssignment = currentAssignment;
            _lastKnownHeroLocation = heroLocation;
            _pendingAssignment = null;
            _pendingReturnHome = false;
            _route = null;
            _routeIndex = 0;
            _heroWalking = false;
            return;
        }
        if (_heroCarrier.State != CitizenSpriteCarrier.VisualState.Macro)
        {
            // Same leftover-GoTo hazard as the ambient worker loop below:
            // a building-detail exit animation interrupted before its
            // completion callback fired would otherwise keep stepping the
            // carrier toward an interior-space target while this class's
            // own UpdateHeroVisual snaps it back to the macro position
            // every frame.
            _heroCarrier.CancelMotion();
            _heroCarrier.SetState(CitizenSpriteCarrier.VisualState.Macro);
            _heroCarrier.Idle(Vector2.Down);
        }

        if (currentAssignment != _lastKnownAssignment)
        {
            if (_lastKnownAssignment.HasValue && currentAssignment is null)
            {
                BeginWalkHome();
            }
            _lastKnownAssignment = currentAssignment;
            if (currentAssignment is BuildingId workplace) BeginWalkToAssignment(workplace);
        }
        else if (currentAssignment.HasValue
            && heroLocation == CitizenLocation.InTransit
            && snapshot.Citizens[0].IsReturningHome
            && ShouldBeginReturnHomeRoute(
                _lastKnownHeroLocation,
                hasRoute: _route is not null,
                pendingReturnHome: _pendingReturnHome))
        {
            BeginWalkHome();
        }
        else if (currentAssignment is BuildingId unsettledWorkplace
            && snapshot.Citizens.Count > 0
            && snapshot.Citizens[0].Location == CitizenLocation.InTransit
            && _route is null)
        {
            // A view transition can replace the flyweight carrier's previous
            // movement callback. If the domain still says InTransit after the
            // visual route disappeared, resume/reconcile instead of leaving
            // the citizen permanently assigned but non-productive.
            BeginWalkToAssignment(unsettledWorkplace);
        }
        _lastKnownHeroLocation = heroLocation;
        UpdateHeroVisual();
    }

    internal static bool ShouldBeginReturnHomeRoute(
        CitizenLocation? previousLocation,
        bool hasRoute,
        bool pendingReturnHome)
    {
        return previousLocation == CitizenLocation.AtWork
            || (previousLocation == CitizenLocation.InTransit
                && !hasRoute
                && !pendingReturnHome);
    }

    internal static bool ShouldHideHeroInsideShelter(
        BuildingId? currentAssignment,
        CitizenLocation location,
        bool hasShelter,
        bool hasRoute,
        bool pendingReturnHome,
        bool hasWanderedManually = false) =>
        currentAssignment is null
        && location == CitizenLocation.AtHome
        && hasShelter
        && !hasRoute
        && !pendingReturnHome
        && !hasWanderedManually;

    /// <summary>
    /// Which building plot, if any, a non-hero citizen should stand ambient
    /// at right now: their workplace when assigned, or the Shelter when idle
    /// at home. Returns <c>null</c> for the hero (own dedicated carrier), an
    /// expedition member, a citizen physically at work (rendered by the
    /// building's own worker slots instead), or an idle citizen with no
    /// Shelter yet to stand at. A recruited citizen with no job must remain
    /// a visible, concrete presence — not only a roster row — until they are
    /// given one (docs/FIRST_PLAYABLE_LOOP_AUDIT.md §11.1).
    /// </summary>
    internal static BuildingId? ResolveAmbientPlotKey(
        bool isHero,
        bool isOnExpedition,
        CitizenLocation location,
        BuildingId? currentAssignment,
        BuildingId? homeBuildingId)
    {
        if (isHero || isOnExpedition || location == CitizenLocation.AtWork) return null;
        if (currentAssignment is { } workplace) return workplace;
        return location == CitizenLocation.AtHome ? homeBuildingId : null;
    }

    /// <summary>
    /// Ambient presence for assigned, non-hero citizens (S-1.4 follow-up:
    /// the prerequisite the TO_DO's MultiMesh sub-item was missing —
    /// citizens weren't visible in this view AT ALL before this). Each
    /// stands at their workplace's or Shelter's plot in
    /// <see cref="CitizenSpriteCarrier.VisualState.Macro"/>, the same
    /// "arrived and settled" pose <see cref="BeginWalkToAssignment"/>/
    /// <see cref="CompleteRoute"/> gives the hero — no route-walking for
    /// these, they simply appear once assigned/idle-at-home and vanish once
    /// on expedition (see <see cref="ResolveAmbientPlotKey"/> for the
    /// eligibility rule). Reuses the same per-citizen
    /// <see cref="CitizenSpriteCarrier"/>/<see cref="CitizenSpriteBank"/>
    /// instancing the hero already uses; today's citizen counts are far
    /// below the documented 20-25 trigger, so per-node instancing (not
    /// MultiMesh) is still the right call — see that sub-item's own note.
    /// </summary>
    private void RefreshCitizenVisuals(CityMacroSnapshot snapshot)
    {
        BuildingId? homeBuildingId = null;
        foreach (CityMacroSnapshot.PlotItem building in snapshot.Buildings)
        {
            if (building.Kind != BuildingKind.Home) continue;
            homeBuildingId = building.Id;
            break;
        }

        var workersByBuilding = new Dictionary<int, List<CityMacroSnapshot.CitizenItem>>();
        var assignedCitizenIds = new HashSet<int>();
        var arrivedWorkers = new List<(BuildingId BuildingId, CitizenId CitizenId)>();
        var arrivedHome = new List<CitizenId>();
        foreach (CityMacroSnapshot.CitizenItem citizen in snapshot.Citizens)
        {
            if (!citizen.IsHero
                && citizen.Location == CitizenLocation.InTransit
                && citizen.IsReturningHome)
            {
                arrivedHome.Add(citizen.Id);
                continue;
            }
            if (ResolveAmbientPlotKey(
                    citizen.IsHero, citizen.IsOnExpedition, citizen.Location,
                    citizen.CurrentAssignment, homeBuildingId) is not { } plotKey) continue;
            assignedCitizenIds.Add(citizen.Id.Value);
            if (!workersByBuilding.TryGetValue(plotKey.Value, out List<CityMacroSnapshot.CitizenItem>? workers))
            {
                workers = new List<CityMacroSnapshot.CitizenItem>();
                workersByBuilding[plotKey.Value] = workers;
            }
            workers.Add(citizen);
        }

        foreach ((int buildingId, List<CityMacroSnapshot.CitizenItem> workers) in workersByBuilding)
        {
            PlotBox? workplacePlot = null;
            foreach (PlotBox plot in _plots)
            {
                if (plot.BuildingId != buildingId) continue;
                workplacePlot = plot;
                break;
            }
            if (workplacePlot is null) continue;

            for (int index = 0; index < workers.Count; index++)
            {
                CityMacroSnapshot.CitizenItem worker = workers[index];
                CitizenSpriteCarrier carrier = CitizenSpriteBank.Instance.GetOrCreate(
                    worker.Id, worker.Lineage, worker.Gender, worker.Appearance);
                CitizenSpriteBank.Instance.Mount(carrier, this);
                if (carrier.State != CitizenSpriteCarrier.VisualState.Macro)
                {
                    // The carrier may still carry a GoTo started by
                    // VisibleWorkerSlot's entrance/exit animation (e.g. the
                    // player left the building's detail view before that
                    // animation's completion callback fired, so it never
                    // reached Hidden). Without cancelling it here, that
                    // leftover interior-space target and this method's own
                    // UpdateWorkerVisuals both write Position every frame —
                    // the citizen visibly fights itself sideways in a loop
                    // instead of settling at its macro plot.
                    carrier.CancelMotion();
                    carrier.SetState(CitizenSpriteCarrier.VisualState.Macro);
                    carrier.Idle(Vector2.Up);
                }
                _workerCarriers[worker.Id.Value] = new WorkerSlot(carrier, buildingId, index, workers.Count);
                if (worker.Location == CitizenLocation.InTransit)
                {
                    arrivedWorkers.Add((new BuildingId(buildingId), worker.Id));
                }
            }
        }

        List<int>? staleCitizenIds = null;
        foreach (int citizenId in _workerCarriers.Keys)
        {
            if (assignedCitizenIds.Contains(citizenId)) continue;
            (staleCitizenIds ??= new List<int>()).Add(citizenId);
        }
        if (staleCitizenIds is not null)
        {
            foreach (int citizenId in staleCitizenIds)
            {
                _workerCarriers.Remove(citizenId, out WorkerSlot slot);
                if (IsInstanceValid(slot.Carrier))
                {
                    slot.Carrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
                }
            }
        }
        UpdateWorkerVisuals();
        foreach ((BuildingId buildingId, CitizenId citizenId) in arrivedWorkers)
        {
            _controller.ConfirmCitizenArrivedAtAssignment(buildingId, citizenId);
        }
        foreach (CitizenId citizenId in arrivedHome)
        {
            _controller.ConfirmCitizenArrivedHome(citizenId);
        }
    }

    /// <summary>
    /// Positions/scales each ambient worker exactly like <see cref="UpdateHeroVisual"/>
    /// positions the hero when settled at a workplace — same anchor depth
    /// as the building itself (<see cref="AnchorDepth"/>), so they read as
    /// standing at its front edge. Workers sharing one building fan out
    /// laterally by a small pixel-snapped step so they don't fully overlap.
    /// </summary>
    private void UpdateWorkerVisuals()
    {
        foreach (WorkerSlot slot in _workerCarriers.Values)
        {
            if (!IsInstanceValid(slot.Carrier) || slot.Carrier.State != CitizenSpriteCarrier.VisualState.Macro)
            {
                continue;
            }
            PlotBox? workplacePlot = null;
            foreach (PlotBox plot in _plots)
            {
                if (plot.BuildingId != slot.BuildingId) continue;
                workplacePlot = plot;
                break;
            }
            if (workplacePlot is not { } plotValue) continue;

            // Anchored at the calle's own front edge (depth, not
            // AnchorDepth(depth)) — half a tile CLOSER to the viewer than
            // the building's own anchor, so the worker stands visibly in
            // front of the building instead of overlapping its sprite.
            float depth = WorkplaceEntranceStreet(plotValue.Street) - CameraDepthAnchor;
            float fanOffset = (slot.Index - (slot.GroupSize - 1) * 0.5f) * WorkerLateralSpacingPx;
            float relativeOffset = plotValue.LateralOffset + fanOffset - CameraLateral;
            (Vector2 position, Vector2 scale) =
                StreetDepthProjection.Project(depth, relativeOffset, CenterX, BaseY);
            slot.Carrier.Scale = CitizenSpriteCarrier.ScaleForState(CitizenSpriteCarrier.VisualState.Macro) * scale;
            slot.Carrier.Position = PixelMotion.Snap(new Vector2(
                position.X,
                position.Y - HeroFootOffsetMacroPx * scale.Y));
        }
    }
    /// <summary>
    /// Routes the hero from wherever they currently are to their new
    /// workplace's calle/lateral, reusing the same quantized
    /// <see cref="StreetRoutePlanner"/>/<see cref="_route"/> machinery as
    /// gather. Once the route completes, <see cref="CompleteRoute"/> just
    /// settles them into an idle "at work" pose instead of gathering wood.
    /// </summary>
    private void BeginWalkToAssignment(BuildingId workplace)
    {
        _heroWanderedAwayManually = false;
        // The canonical flyweight may still carry a GoTo started by the
        // building-detail slot where the assignment was requested. Once the
        // macro view takes route ownership, that interior movement must stop:
        // otherwise CitizenSpriteCarrier._Process and UpdateHeroVisual write
        // the same Position concurrently and the sprite oscillates just short
        // of the entrance without either completion callback winning.
        _heroCarrier?.CancelMotion();
        PlotBox? target = null;
        foreach (PlotBox plot in _plots)
        {
            if (plot.BuildingId != workplace.Value) continue;
            target = plot;
            break;
        }
        if (target is null)
        {
            _controller.ConfirmCitizenArrivedAtAssignment(
                workplace,
                _controller.World.Hero!.Id);
            return;
        }
        _pendingGather = null;
        _pendingReturnHome = false;
        _pendingAssignment = workplace;
        int entranceStreet = WorkplaceEntranceStreet(target.Value.Street);
        _route = PlanHeroRoute(_heroStreet, _heroLateral, entranceStreet, target.Value.LateralOffset);
        _routeIndex = 0;
    }

    /// <summary>
    /// Gives a released founder a concrete next intention. Assignment is a
    /// domain concern; this route is its visual consequence, so the citizen
    /// walks back to the Shelter instead of freezing wherever the previous
    /// workplace route was cancelled.
    /// </summary>
    private void BeginWalkHome()
    {
        _heroWanderedAwayManually = false;
        // Route ownership can also transfer from an interior exit animation.
        // Keep exactly one position writer for the shared citizen carrier.
        _heroCarrier?.CancelMotion();
        PlotBox? shelter = null;
        foreach (PlotBox plot in _plots)
        {
            if (plot.Kind != BuildingKind.Home || plot.IsUnderConstruction) continue;
            shelter = plot;
            break;
        }

        _pendingGather = null;
        _pendingAssignment = null;
        _routeIndex = 0;
        _heroWalking = false;
        if (shelter is null)
        {
            _pendingReturnHome = false;
            _route = null;
            _heroCarrier?.Idle(Vector2.Down);
            return;
        }

        _pendingReturnHome = true;
        int entranceStreet = WorkplaceEntranceStreet(shelter.Value.Street);
        _route = PlanHeroRoute(
            _heroStreet,
            _heroLateral,
            entranceStreet,
            shelter.Value.LateralOffset);
    }

    /// <summary>
    /// A plot's street value already denotes the free front band of that lot
    /// row. Subtracting one stopped the citizen on the preceding road and then
    /// treated that visibly premature position as an arrival.
    /// </summary>
    internal static int WorkplaceEntranceStreet(int buildingStreet) =>
        Math.Max(0, buildingStreet);

    /// <summary>
    /// Prefers the real <see cref="StreetNavigationServerPlanner"/> navmesh
    /// query — a genuine A* over the gaps between obstacles across every
    /// intervening band, solving the multi-band zigzag the greedy
    /// <see cref="StreetRoutePlanner.Plan"/> heuristic below only
    /// approximates — and falls back to it only when the navmesh
    /// genuinely finds no path at all (fully sealed geometry), matching
    /// this view's existing "a best-effort route beats a stranded hero"
    /// philosophy.
    /// </summary>
    private List<StreetRoutePlanner.Waypoint> PlanHeroRoute(
        int fromStreet, float fromLateral, int toStreet, float toLateral)
    {
        List<StreetRoutePlanner.Waypoint>? navmeshRoute = _navmeshPlanner?.Plan(
            fromStreet,
            fromLateral,
            toStreet,
            toLateral,
            GetBandOccupancy,
            _streetCount,
            -_lateralHalfWidthPx,
            _lateralHalfWidthPx,
            RouteClearancePx);
        if (navmeshRoute is not null) return navmeshRoute;

        return StreetRoutePlanner.Plan(
            fromStreet,
            fromLateral,
            toStreet,
            toLateral,
            GetBandOccupancy,
            -_lateralHalfWidthPx,
            _lateralHalfWidthPx,
            RouteClearancePx,
            CrossingScanStepPx);
    }

    /// <summary>
    /// Positions/scales the carrier for the current camera state: feet on
    /// the hero's road, bottom-center anchored (design bible §08
    /// "Anclaje"), non-uniform depth scale on top of the 0.25 macro scale.
    /// Projected relative to the CAMERA (<see cref="CameraDepthAnchor"/>/
    /// <see cref="CameraLateral"/>), not always at depth 0 — while
    /// following, the camera IS the founder's own smoothed position, so
    /// this still nets out to dead center exactly as before; in free
    /// camera mode the founder instead renders like any other sprite,
    /// receding/approaching and drifting sideways as the free camera pans.
    /// </summary>
    private void UpdateHeroVisual()
    {
        if (_heroCarrier is null
            || !IsInstanceValid(_heroCarrier)
            || _heroCarrier.GetParent() != this
            || _heroCarrier.State != CitizenSpriteCarrier.VisualState.Macro)
        {
            return;
        }
        float depth = _depthAnchor - CameraDepthAnchor;
        float lateralOffset = _heroLateral - CameraLateral;
        (Vector2 position, Vector2 scale) =
            StreetDepthProjection.Project(depth, lateralOffset, CenterX, BaseY);
        _heroCarrier.Scale =
            CitizenSpriteCarrier.ScaleForState(CitizenSpriteCarrier.VisualState.Macro) * scale;
        _heroCarrier.Position = PixelMotion.Snap(new Vector2(
            position.X,
            position.Y - HeroFootOffsetMacroPx * scale.Y));
    }

    public override void _Draw()
    {
        _clickableRects.Clear();
        _clickableTreeRects.Clear();
        _clickablePlacementRects.Clear();
        for (int street = _streetCount - 1; street >= 0; street--)
        {
            DrawStreetRow(street);
        }
        UpdateHeroVisual();
        UpdateWorkerVisuals();
    }

    /// <summary>
    /// Every lateral offset is relative to the hero's own
    /// <see cref="_heroLateral"/> — the vanishing point follows the viewer,
    /// the fix validated in the earlier isolated prototype. The floor
    /// spans the lot's full tile depth as a tiled ground
    /// (<see cref="DrawTiledFloor"/>). Buildings/trees/lots anchor at
    /// <see cref="AnchorDepth"/> — half a tile behind the calle's own near
    /// edge, i.e. near the FRONT of their lot (a real building's baseline
    /// sits close to the street it fronts, not buried at the back of a
    /// 3-tile-deep footprint) — and, critically, use that SAME depth for
    /// BOTH their X and Y projection. Using the calle's shallow depth for X
    /// while anchoring Y to a different depth (an earlier version of this
    /// method did exactly that, via a separately-returned "roadTop") makes
    /// the horizontal scale wrong relative to the tiles actually underneath
    /// the sprite, so buildings visibly drift off the tile grid as the
    /// camera's lateral offset changes — the drift scales with
    /// <see cref="_heroLateral"/>, which is why it only became obvious once
    /// walking sideways.
    /// </summary>
    private void DrawStreetRow(int street)
    {
        float depth = street - CameraDepthAnchor;
        DrawTiledFloor(street, depth);
        float anchorDepth = AnchorDepth(depth);

        if (_placementActive)
        {
            DrawPlacementLots(street, depth);
        }

        foreach (PlotBox plot in _plots)
        {
            if (plot.Street != street) continue;
            float relativeOffset = plot.LateralOffset - CameraLateral;
            (Vector2 position, Vector2 scale) =
                StreetDepthProjection.Project(anchorDepth, relativeOffset, CenterX, BaseY);
            var size = new Vector2(plot.Width * scale.X, plot.Height * scale.Y);
            var rect = new Rect2(
                new Vector2(position.X - size.X * 0.5f, position.Y - size.Y),
                size);
            Texture2D? texture = GetBuildingTexture(plot.Kind);
            if (texture is not null)
            {
                DrawTextureRect(
                    texture,
                    rect,
                    tile: false,
                    modulate: plot.IsUnderConstruction ? UnderConstructionModulate : Colors.White);
            }
            else
            {
                DrawRect(rect, BuildingColor);
            }
            if (plot.IsClickable) _clickableRects.Add((rect, plot.BuildingId));
        }

        foreach (TreeBox tree in _trees)
        {
            if (tree.Street != street) continue;
            float treeRelativeOffset = tree.LateralOffset - CameraLateral;
            (Vector2 treePosition, Vector2 treeScale) =
                StreetDepthProjection.Project(anchorDepth, treeRelativeOffset, CenterX, BaseY);
            var treeSize = new Vector2(
                TreeBaseSizePx * treeScale.X,
                TreeBaseSizePx * treeScale.Y);
            var treeRect = new Rect2(
                new Vector2(treePosition.X - treeSize.X * 0.5f, treePosition.Y - treeSize.Y),
                treeSize);
            // Same two Kenney atlas tiles the flat view's ResourceTree uses,
            // variant kept stable per individual tree.
            int column = (tree.ForestId + tree.UnitId) % 2 == 0
                ? ResourceTree.TreeAtlasColumnA
                : ResourceTree.TreeAtlasColumnB;
            DrawTextureRectRegion(
                _terrainAtlas,
                treeRect,
                ResourceTree.AtlasRegionRect(column, ResourceTree.TreeAtlasRow));
            _clickableTreeRects.Add((treeRect, tree));
        }
    }

    /// <summary>Depth at which sprites anchor within their calle's lot: half a tile
    /// behind the calle's own near edge, i.e. near the lot's front rather than its back.</summary>
    private static float AnchorDepth(float streetDepth) =>
        streetDepth + 0.5f / ParcelGrid.TilesPerStandardLot;

    /// <summary>
    /// Draws one calle's floor as a full <see cref="ParcelGrid.TilesPerStandardLot"/>-deep
    /// tiled ground, each tile a true perspective trapezoid (narrower far
    /// edge than near edge, non-vertical side edges converging toward the
    /// vanishing point) instead of a uniformly-scaled rectangle — matching
    /// the Pole Position/Out Run reference the design bible calls for. Each
    /// of the <see cref="ParcelGrid.TilesPerStandardLot"/> sub-rows spans
    /// the depth interval [street + k/3, street + (k+1)/3); a sub-row's far
    /// edge depth always equals the next sub-row's near edge depth (and the
    /// next calle's own near edge), so adjacent tiles' edges align exactly
    /// with no seams or gaps — <see cref="StreetDepthProjection.HorizontalScale"/>
    /// is a pure function of depth, so two edges sharing a depth always get
    /// the same width.
    /// </summary>
    private void DrawTiledFloor(int street, float depth)
    {
        int baseAtlasColumn = StreetGroundAtlasColumn(street);
        int totalTiles = Mathf.RoundToInt(2f * _lateralHalfWidthPx / TileUnitPx);
        for (int tileRow = 0; tileRow < ParcelGrid.TilesPerStandardLot; tileRow++)
        {
            float depthNear = depth + tileRow / (float)ParcelGrid.TilesPerStandardLot;
            float depthFar = depth + (tileRow + 1) / (float)ParcelGrid.TilesPerStandardLot;
            float yNear = StreetDepthProjection.RowScreenY(depthNear, BaseY);
            float yFar = StreetDepthProjection.RowScreenY(depthFar, BaseY);
            float scaleNear = StreetDepthProjection.HorizontalScale(depthNear);
            float scaleFar = StreetDepthProjection.HorizontalScale(depthFar);
            int globalTileRow = street * ParcelGrid.TilesPerStandardLot + tileRow;

            for (int tileIndex = 0; tileIndex < totalTiles; tileIndex++)
            {
                float tileCenterGlobal = (tileIndex + 0.5f) * TileUnitPx - _lateralHalfWidthPx;
                float leftGlobal = tileCenterGlobal - TileUnitPx * 0.5f - CameraLateral;
                float rightGlobal = tileCenterGlobal + TileUnitPx * 0.5f - CameraLateral;
                // Deterministic terrain hash parameterized by tile index and
                // global tile row, picking between the
                // biome's two atlas variants instead of two flat colors.
                bool alternate = (tileIndex * 3 + globalTileRow * 5) % 11 == 0;
                int atlasRow = alternate ? GroundAtlasRowB : GroundAtlasRowA;
                // Only tileRow 0 (the calle's own walkable front band) can
                // wear into a path — the lot depth behind it (tileRow 1/2)
                // is never trodden, it's where buildings/trees sit.
                DrawPixelStaircaseTrapezoid(
                    yNear, yFar,
                    CenterX + leftGlobal * scaleNear, CenterX + rightGlobal * scaleNear,
                    CenterX + leftGlobal * scaleFar, CenterX + rightGlobal * scaleFar,
                    _terrainAtlas, ResourceTree.AtlasRegionRect(baseAtlasColumn, atlasRow));

                float wear = tileRow == 0 ? _terrainWear.WearAt(street, tileIndex) : 0f;
                if (wear <= 0f) continue;
                // Reveal a narrow dirt trace first, then widen it with traffic.
                // One passage affects only a couple of snapped pixels instead
                // of replacing the whole tile at an arbitrary threshold.
                float dirtWidthFactor = Mathf.Clamp(0.04f + wear * 0.96f, 0f, 1f);
                float halfDirtWidth = TileUnitPx * dirtWidthFactor * 0.5f;
                float dirtLeft = tileCenterGlobal - halfDirtWidth - CameraLateral;
                float dirtRight = tileCenterGlobal + halfDirtWidth - CameraLateral;
                DrawPixelStaircaseTrapezoid(
                    yNear, yFar,
                    CenterX + dirtLeft * scaleNear, CenterX + dirtRight * scaleNear,
                    CenterX + dirtLeft * scaleFar, CenterX + dirtRight * scaleFar,
                    _terrainAtlas, ResourceTree.AtlasRegionRect(DirtAtlasColumn, atlasRow));
            }
        }
    }

    /// <summary>
    /// Deterministic biome-per-calle assignment (S-1.3 phase 1): cycles
    /// Grass/Dirt/Stone across streets so the corridor shows visible ground
    /// variety without any new domain/save state — purely presentational.
    /// Terrain art must never become simulation state. <see cref="_terrainWear"/>
    /// (phase 2) overrides this per-tile in <see cref="DrawTiledFloor"/> for
    /// trampled tiles, without touching this method.
    /// </summary>
    private static int StreetGroundAtlasColumn(int street) => (((street % 3) + 3) % 3) switch
    {
        0 => GrassAtlasColumn,
        1 => DirtAtlasColumn,
        _ => StoneAtlasColumn,
    };

    /// <summary>
    /// Which lateral tile index (the same granularity <see cref="DrawTiledFloor"/>
    /// tiles the floor at) a global lateral offset falls into — used to mark
    /// the hero's own footprint for <see cref="_terrainWear"/>, independent
    /// of camera position (same "global" lateral space <c>_heroLateral</c>
    /// and <see cref="StreetRoutePlanner"/> already use).
    /// </summary>
    private int TileIndexAtLateral(float lateral)
    {
        int totalTiles = Mathf.RoundToInt(2f * _lateralHalfWidthPx / TileUnitPx);
        int index = Mathf.FloorToInt((lateral + _lateralHalfWidthPx) / TileUnitPx);
        return Mathf.Clamp(index, 0, totalTiles - 1);
    }

    /// <summary>Marks the tile under the hero's current feet as trampled (S-1.3 phase 2).</summary>
    private void TrampleHeroTile() => _terrainWear.Trample(_heroStreet, TileIndexAtLateral(_heroLateral));

    /// <summary>
    /// Approximates a perspective trapezoid as a "staircase" of small,
    /// axis-aligned, pixel-snapped rectangles instead of one smooth
    /// mathematically-perfect polygon. A true trapezoid's slanted sides
    /// render as a perfectly smooth anti-aliased diagonal, which reads as
    /// glossy/vector art and clashes with this game's pixel-art direction
    /// (discrete 8 px character steps, snapped positions everywhere else —
    /// see <c>PixelMotion</c>). Chunky, blocky diagonal edges are how
    /// genuine pixel-art perspective floors (Pole Position/Out Run-era)
    /// render a slant: each horizontal strip is flat, and the slant comes
    /// from strips shifting by whole pixel-grid steps, not from smooth
    /// interpolation. Each stripe samples its own cropped vertical slice of
    /// <paramref name="sourceRegion"/> (S-1.3 texture pass) rather than the
    /// whole tile stretched into every stripe, so the ground texture reads
    /// as one coherent tile top-to-bottom instead of repeating per stripe.
    /// </summary>
    private void DrawPixelStaircaseTrapezoid(
        float yNear, float yFar,
        float xLeftNear, float xRightNear,
        float xLeftFar, float xRightFar,
        Texture2D atlas, Rect2 sourceRegion)
    {
        float height = yNear - yFar;
        int stripes = Mathf.Clamp(Mathf.RoundToInt(height / PixelStepPx), 1, 32);
        for (int i = 0; i < stripes; i++)
        {
            float tNear = i / (float)stripes;
            float tFar = (i + 1) / (float)stripes;
            float stripeBottom = SnapPixel(Mathf.Lerp(yNear, yFar, tNear));
            float stripeTop = SnapPixel(Mathf.Lerp(yNear, yFar, tFar));
            // One constant width per stripe (sampled at its near edge) —
            // not interpolated across the stripe's own height — is what
            // produces the stepped look; the slant emerges from stripe to
            // stripe, never within one.
            float left = SnapPixel(Mathf.Lerp(xLeftNear, xLeftFar, tNear));
            float right = SnapPixel(Mathf.Lerp(xRightNear, xRightFar, tNear));
            if (right <= left || stripeBottom <= stripeTop) continue;
            var stripeSource = new Rect2(
                sourceRegion.Position.X,
                sourceRegion.Position.Y + sourceRegion.Size.Y * tNear,
                sourceRegion.Size.X,
                sourceRegion.Size.Y * (tFar - tNear));
            DrawTextureRectRegion(
                atlas,
                new Rect2(new Vector2(left, stripeTop), new Vector2(right - left, stripeBottom - stripeTop)),
                stripeSource);
        }
    }

    private static float SnapPixel(float value) => Mathf.Round(value / PixelStepPx) * PixelStepPx;

    private Texture2D? GetBuildingTexture(BuildingKind kind)
    {
        if (_buildingTextureCache.TryGetValue(kind, out Texture2D? cached)) return cached;
        string? path = BuildingArt.GetTexturePath(kind);
        Texture2D? texture = path is null ? null : GD.Load<Texture2D>(path);
        _buildingTextureCache[kind] = texture;
        return texture;
    }

    /// <summary>
    /// Renders each available construction lot as its real 3x3 ground
    /// footprint. Every cell projects its near and far edges independently,
    /// so the blueprint shares the terrain's vanishing point instead of
    /// becoming an axis-aligned screen rectangle after projecting only its
    /// centre.
    /// </summary>
    private void DrawPlacementLots(int street, float streetDepth)
    {
        foreach (PlacementLotBox lot in _placementLots)
        {
            if (lot.Street != street) continue;
            bool isSelected = _selectedPlacementLot is ConstructionLot selectedLot
                && selectedLot == lot.Lot;
            Color fill = isSelected ? PlacementSelectedColor : PlacementAvailableColor;
            Color outline = new("#f4e7b2");
            float lotLeft = lot.LateralOffset - lot.Width * 0.5f - CameraLateral;
            float lotRight = lotLeft + lot.Width;
            float depthNear = streetDepth;
            float depthFar = streetDepth + 1f;
            float yNear = StreetDepthProjection.RowScreenY(depthNear, BaseY);
            float yFar = StreetDepthProjection.RowScreenY(depthFar, BaseY);
            float scaleNear = StreetDepthProjection.HorizontalScale(depthNear);
            float scaleFar = StreetDepthProjection.HorizontalScale(depthFar);
            Vector2 nearLeft = new(CenterX + lotLeft * scaleNear, yNear);
            Vector2 nearRight = new(CenterX + lotRight * scaleNear, yNear);
            Vector2 farRight = new(CenterX + lotRight * scaleFar, yFar);
            Vector2 farLeft = new(CenterX + lotLeft * scaleFar, yFar);

            DrawSteppedPlacementFootprint(nearLeft, nearRight, farRight, farLeft, fill, outline);
            Vector2 boundsMin = new(
                Mathf.Min(Mathf.Min(nearLeft.X, nearRight.X), Mathf.Min(farLeft.X, farRight.X)),
                Mathf.Min(yNear, yFar));
            Vector2 boundsMax = new(
                Mathf.Max(Mathf.Max(nearLeft.X, nearRight.X), Mathf.Max(farLeft.X, farRight.X)),
                Mathf.Max(yNear, yFar));
            _clickablePlacementRects.Add((new Rect2(boundsMin, boundsMax - boundsMin), lot));
        }
    }

    private void DrawSteppedPlacementFootprint(
        Vector2 nearLeft,
        Vector2 nearRight,
        Vector2 farRight,
        Vector2 farLeft,
        Color fill,
        Color outline)
    {
        const float stripeHeight = 2f;
        float height = Mathf.Abs(farLeft.Y - nearLeft.Y);
        int stripes = Mathf.Max(1, Mathf.CeilToInt(height / stripeHeight));
        Vector2 previousLeft = PixelMotion.Snap(nearLeft);
        Vector2 previousRight = PixelMotion.Snap(nearRight);
        DrawLine(previousLeft, previousRight, outline, 2f, antialiased: false);

        for (int index = 0; index < stripes; index++)
        {
            float t0 = index / (float)stripes;
            float t1 = (index + 1) / (float)stripes;
            Vector2 left0 = PixelMotion.Snap(nearLeft.Lerp(farLeft, t0));
            Vector2 right0 = PixelMotion.Snap(nearRight.Lerp(farRight, t0));
            Vector2 left1 = PixelMotion.Snap(nearLeft.Lerp(farLeft, t1));
            Vector2 right1 = PixelMotion.Snap(nearRight.Lerp(farRight, t1));
            float top = Mathf.Min(left0.Y, left1.Y);
            float bottom = Mathf.Max(left0.Y, left1.Y);
            DrawRect(new Rect2(
                new Vector2(Mathf.Min(left0.X, left1.X), top),
                new Vector2(
                    Mathf.Max(right0.X, right1.X) - Mathf.Min(left0.X, left1.X),
                    Mathf.Max(1f, bottom - top))), fill);

            DrawLine(previousLeft, new Vector2(left1.X, previousLeft.Y), outline, 2f, false);
            DrawLine(new Vector2(left1.X, previousLeft.Y), left1, outline, 2f, false);
            DrawLine(previousRight, new Vector2(right1.X, previousRight.Y), outline, 2f, false);
            DrawLine(new Vector2(right1.X, previousRight.Y), right1, outline, 2f, false);
            previousLeft = left1;
            previousRight = right1;
        }
        DrawLine(previousLeft, previousRight, outline, 2f, antialiased: false);
    }
}
