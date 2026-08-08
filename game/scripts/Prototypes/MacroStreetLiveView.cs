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
/// owns that rule for every citizen journey and the founder's gather
/// routes, threading BETWEEN obstacles rather than around them
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
/// Camera mode (design bible §04 "Cámara-sigue"): free pan by default,
/// with follow-the-founder available only through an explicit toggle
/// (<see cref="ToggleCameraMode"/>, F key or the navigation rail button),
/// independent from selection. Free mode decouples the vanishing point
/// (<see cref="CameraLateral"/>/
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
/// See docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE.md,
/// "Cámara y mundo caminable".
/// </summary>
public partial class MacroStreetLiveView : Node2D
{
    private const bool DefaultCameraFollowsHero = false;
    private const float CenterX = 640f;
    private const float BaseY = 580f; // ScreenContent-local. Unchanged by the move
    // from a full-width top band to a left rail: freeing the band gives the world
    // headroom it simply does not use, and retuning it would shift every depth row.
    private const float CameraZoomPivotY = 680f;
    private const float LotUnitPx = 90f;
    private const int DefaultWorldParcelColumns = 5;
    private const int DefaultWorldParcelRows = 2;

    // Quantized zoom: discrete steps, never a continuous drag/slider.
    private const float ZoomStep = 0.15f;
    private const float MinZoom = 1.3f;
    private const float DefaultZoom = 1.45f;
    private const float MaxZoom = 3.0f;

    // Holding vertical pan repeats slowly at first, then gently accelerates.
    // The camera still advances only on the 12 Hz pixel-motion cadence and
    // still crosses integer streets through discrete transition steps.
    private const float VerticalPanInitialRepeatSeconds = 0.48f;
    private const float VerticalPanMinimumRepeatSeconds = 0.26f;
    private const float VerticalPanAccelerationSeconds = 3f;
    private const float VerticalPanMaximumTransitionMultiplier = 1.55f;

    // Same cadence discipline as the earlier prototypes (design bible §08,
    // "Pixel-motion grammar"): no continuous tweening.
    private const int TransitionSteps = 10;
    private const float DepthStepSize = 1f / TransitionSteps;

    // Building-entry camera push: a handful of DISCRETE zoom steps toward
    // the clicked building (same stepped cadence as citizen/camera motion —
    // never a continuous Tween), applied to THIS node's own Scale/Position
    // (the map), not to BuildingDetailView. See BeginBuildingEntry.
    private const int BuildingEntryZoomSteps = 10;
    private const float BuildingEntryZoomLevel = 1.75f;

    // One resource unit owns one frontage cell. Its visual canvas therefore
    // stays within that cell instead of visually claiming a whole 3×3 lot.
    private const float ResourceUnitBaseSizePx = TileUnitPx;
    // Lateral span a living tree blocks when crossing its band (its lot).
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
    private const string CultivationActionMenuScenePath =
        "res://scenes/Components/CultivationActionMenu.tscn";

    // Floor tiles sample the Kenney atlas ResourceTree already uses for
    // trees (S-1.3 biome pass), keyed by street so the corridor reads as
    // distinct ground per calle.
    private const float TileUnitPx = LotUnitPx / 3f; // ParcelGrid.TilesPerStandardLot
    // Chunky pixel-grid step for the floor's staircase edges — see
    // DrawPixelStaircaseTrapezoid. Independent of PixelMotion.StepPixels:
    // coarse enough to read as deliberate pixel art, fine enough that the
    // trapezoid shape stays legible instead of looking blocky/broken.
    // 2, down from 4: the trapezoid edges still climb in whole-pixel treads —
    // a true diagonal would betray the pixel art — but a 4 px tread read as a
    // sawtooth on the long shallow edges of the near streets. This is a grain
    // adjustment, not a move toward antialiasing: edges stay snapped to a
    // whole-pixel grid and nothing is interpolated. Two is the floor worth
    // taking; at 1 px the treads stop reading as deliberate and the edge
    // becomes the diagonal this quantisation exists to avoid.
    private const float PixelStepPx = 2f;
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
    private const float StatusBadgeSize = 24f;
    private const float StatusBadgeBorder = 2f;
    private static readonly Color BuildingColor = new("#8a7a54");
    private static readonly Color UnderConstructionModulate = new(0.55f, 0.55f, 0.55f);
    private static readonly Color PlacementAvailableColor = new("#2f8f5b22");
    private static readonly Color PlacementHoveredValidColor = new("#45c87866");
    private static readonly Color PlacementHoveredInvalidColor = new("#c94f4f70");
    private static readonly Color PlacementBlockedCellColor = new("#8f3f3f2e");
    private static readonly Color PlacementGridColor = new("#d8cda566");
    // Territory tints: opaque for Locked so the player cannot mistake
    // it for buildable ground; progressively lighter for intermediate
    // states so the visual cost of an expedition reads at a glance.
    private static readonly Color LockedParcelColor = new(0.08f, 0.07f, 0.05f, 0.78f);
    private static readonly Color ReconnoitredParcelColor = new(0.86f, 0.72f, 0.28f, 0.32f);
    private static readonly Color RouteSecuredParcelColor = new(0.47f, 0.62f, 0.34f, 0.22f);
    private static readonly Color PlacementSelectedColor = new("#f2c94ccc");

    [Export] public NodePath ControllerPath { get; set; } = "../../../CityWorldController";
    [Export] public NodePath StatusPanelPath { get; set; } = "../../CityStatusPanel";
    [Export] public NodePath ChroniclePath { get; set; } = "../OfflineReportPanel";
    [Export] public NodePath ConstructionPanelPath { get; set; } = "../Center/ConstructionPanel";
    [Export] public NodePath ExpeditionPanelPath { get; set; } = "../ExpeditionPanel";
    [Export] public NodePath PoliciesPanelPath { get; set; } = "../PoliciesPanel";
    [Export] public NodePath CitizensPanelPath { get; set; } = "../MigrantPanel";
    [Export] public NodePath ModalHostPath { get; set; } = "../ModalHost";
    /// <summary>
    /// The navigation rail. One path replaces the five literal button paths this
    /// view used to carry: the rail owns its own structure and hands back typed
    /// buttons, so moving a button inside it no longer breaks the world view.
    /// </summary>
    [Export] public NodePath NavigationRailPath { get; set; } = "../NavigationRail";
    [Export] public NodePath BuildingDetailViewPath { get; set; } = "../BuildingDetailView";

    private CityWorldController _controller = null!;
    private CityStatusPanel _statusPanel = null!;
    private OfflineReportPanel _chronicle = null!;
    private ResourceActionMenu _actionMenu = null!;
    private CultivationActionMenu _cultivationActionMenu = null!;
    private IconButton _constructionMenuButton = null!;
    private ConstructionPanel _constructionPanel = null!;
    private IconButton _expeditionMenuButton = null!;
    private ExpeditionPanel _expeditionPanel = null!;
    private IconButton _policiesButton = null!;
    private PoliciesPanel _policiesPanel = null!;
    private IconButton _citizensButton = null!;
    private MigrantPanel _citizensPanel = null!;
    private ModalHost _modalHost = null!;
    private NavigationRail _navigationRail = null!;
    private BuildingDetailView _buildingDetailView = null!;
    private IconButton _cameraModeButton = null!;
    private CursorController? _cursorController;
    private Texture2D _terrainAtlas = null!;
    private Texture2D _storageFullIcon = null!;
    private WorldStatusBubble _worldStatusBubble = null!;
    private readonly Dictionary<BuildingKind, Texture2D?> _buildingTextureCache = new();
    private readonly Dictionary<int, CityMacroSnapshot.CitizenItem> _citizenStates = new();
    private int? _hoveredCitizenId;
    private int? _hoveredStorageBuildingId;
    private CitizenId? _visualStatusCitizenId;
    private bool _selectionIsMacro = true;
    private float _zoomLevel = DefaultZoom;
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
    private bool _cameraFollowsHero = DefaultCameraFollowsHero;
    private float _freeCameraLateral;
    private int _freeCameraStreet;
    private float _cameraDepthAnchor;
    private float? _cameraDepthTarget;
    private float _cameraTransitionAccumulator;
    private int _verticalPanDirection;
    private float _verticalPanHoldSeconds;
    private float _verticalPanRepeatAccumulator;

    // Placement mode: select-then-confirm frontage picking projected directly on
    // the same terrain geometry as the city.
    private readonly record struct PlacementLotBox(
        ConstructionPlacementSnapshot.WindowItem Window,
        int Street,
        float LateralOffset,
        float Width,
        float Height);
    private readonly record struct PlacementCellBox(
        ConstructionPlacementSnapshot.CellItem Cell,
        int Street,
        float LateralOffset,
        float Width,
        float Height);
    private readonly record struct ResourceFeedbackAnchor(
        Vector2 Position,
        Node2D? FollowTarget,
        Vector2 FollowOffset);
    private readonly List<PlacementLotBox> _placementLots = new();
    private readonly List<PlacementCellBox> _placementCells = new();
    private readonly List<(Rect2 Rect, PlacementLotBox Lot)> _clickablePlacementRects = new();
    private bool _placementActive;
    private ConstructionKind _placementKind;
    private ConstructionLot? _selectedPlacementLot;
    private PlacementLotBox? _hoveredPlacementLot;
    private string _placementBaseInstruction = string.Empty;
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
    private readonly List<(Rect2 Rect, CitizenId CitizenId)> _clickableCitizenRects = new();
    private readonly List<(Rect2 Rect, PlotBox Plot)> _storageFullBadgeRects = new();
    private readonly Dictionary<int, List<StreetRoutePlanner.Interval>> _bandOccupancy = new();
    private static readonly List<StreetRoutePlanner.Interval> EmptyBand = new();

    private int _streetCount = 1;
    private float _lateralHalfWidthPx = LotUnitPx;
    private int _worldParcelColumns = DefaultWorldParcelColumns;
    private int _worldParcelRows = DefaultWorldParcelRows;
    private readonly Dictionary<(int Row, int Column), ParcelTerritoryState> _parcelTerritory = new();

    // The founder has an independent physical position. Follow mode may use
    // it as the camera anchor, but selection and keyboard input never move it.
    private int _heroStreet;
    private float _heroLateral;
    private float _depthAnchor;
    private float? _depthTarget;
    private float _motionAccumulator;
    private float _transitionAccumulator;
    private bool _heroWalking;
    private bool _heroPositionInitialized;

    private SelectionInfoPanel _selectionInfoPanel = null!;
    private TreeBox? _selectedTree;
    private int? _selectedBuildingId;
    private CitizenId? _selectedCitizenId;

    private CitizenSpriteCarrier? _heroCarrier;
    private sealed class CitizenJourney
    {
        public CitizenJourney(
            CitizenId citizenId,
            CitizenSpriteCarrier carrier,
            int street,
            float lateral)
        {
            CitizenId = citizenId;
            Carrier = carrier;
            Street = street;
            Lateral = lateral;
            DepthAnchor = street;
        }

        public CitizenId CitizenId { get; }
        public CitizenSpriteCarrier Carrier { get; }
        public int Street { get; set; }
        public float Lateral { get; set; }
        public float DepthAnchor { get; set; }
        public float? DepthTarget { get; set; }
        public float TransitionAccumulator { get; set; }
        public List<StreetRoutePlanner.Waypoint>? Route { get; set; }
        public int RouteIndex { get; set; }
        public BuildingId? Destination { get; set; }
        public bool ReturningHome { get; set; }
        public bool Walking { get; set; }
        public bool IsAmbient { get; set; }
        public int NextAmbientDecisionTick { get; set; }
    }

    // Every non-founder citizen uses the same street route planner and cadence
    // as the founder. This is presentation-only and never persists pixels.
    private readonly Dictionary<int, CitizenJourney> _citizenJourneys = new();

    internal readonly record struct ReconstructedRoutePosition(
        int Street,
        float Lateral,
        int RouteIndex);
    private StreetNavigationServerPlanner? _navmeshPlanner;
    private List<StreetRoutePlanner.Waypoint>? _route;
    private int _routeIndex;
    private (int ForestId, int UnitId)? _pendingGather;
    private BuildingId? _pendingAssignment;
    private bool _pendingReturnHome;
    // Domain CurrentLocation stays AtHome during a Gather route because
    // gathering is not a work assignment. Without this latch a world refresh
    // could re-hide the founder just as the gather animation resolves.
    private bool _heroIsGatheringOutsideHome;
    private bool _heroAmbientRoute;
    private int _heroNextAmbientDecisionTick;
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
    private ResourceType _hoveredResource = ResourceType.Wood;

    private readonly record struct PlotBox(
        int Street,
        float LateralOffset,
        float Width,
        float Height,
        int BuildingId,
        string DisplayName,
        BuildingKind Kind,
        bool IsUnderConstruction,
        bool IsClickable,
        int Stock,
        int StorageCapacity,
        CultivationPlotState? CultivationState,
        int? ReadyAtTick)
    {
        public bool IsStorageFull => StorageCapacity > 0 && Stock >= StorageCapacity;
    }

    private readonly record struct TreeBox(
        int Street,
        float LateralOffset,
        int ForestId,
        int UnitId,
        ResourceType ResourceType,
        int Reserve);

    public override void _Ready()
    {
        // The view sorts its own contents by depth using child z-indices, and
        // children are relative to their parent. Parking the view low keeps
        // that whole range under the ambient tint, the HUD and the Chronicle
        // — without it, a tree on the nearest street outranked them and drew
        // over the panels.
        ZIndex = OverlayLayers.WorldDepthBase;
        CameraInputActions.EnsureRegistered();
        _controller = GetNode<CityWorldController>(ControllerPath);
        _statusPanel = GetNode<CityStatusPanel>(StatusPanelPath);
        _statusPanel.AttachController(_controller);
        _statusPanel.Refresh(_controller);
        _chronicle = GetNode<OfflineReportPanel>(ChroniclePath);
        _chronicle.SetController(_controller);
        _chronicle.Hide();
        _constructionPanel = GetNode<ConstructionPanel>(ConstructionPanelPath);
        _expeditionPanel = GetNode<ExpeditionPanel>(ExpeditionPanelPath);
        _policiesPanel = GetNode<PoliciesPanel>(PoliciesPanelPath);
        _citizensPanel = GetNode<MigrantPanel>(CitizensPanelPath);
        _modalHost = GetNode<ModalHost>(ModalHostPath);
        _navigationRail = GetNode<NavigationRail>(NavigationRailPath);
        _constructionMenuButton = _navigationRail.ConstructionButton;
        _expeditionMenuButton = _navigationRail.ExpeditionButton;
        _policiesButton = _navigationRail.PoliciesButton;
        _citizensButton = _navigationRail.CitizensButton;
        _cameraModeButton = _navigationRail.CameraButton;
        _buildingDetailView = GetNode<BuildingDetailView>(BuildingDetailViewPath);
        _cursorController = GetNodeOrNull<CursorController>("/root/CursorController");
        _terrainAtlas = GD.Load<Texture2D>(ResourceTree.TerrainAtlasPath);
        _storageFullIcon = GD.Load<Texture2D>(IconPaths.Check);
        _worldStatusBubble = new WorldStatusBubble();
        GetParent().CallDeferred(Node.MethodName.AddChild, _worldStatusBubble);
        // Pixel-art atlas tiles scale up crisp instead of smearing.
        TextureFilter = TextureFilterEnum.Nearest;

        _streetCount = _worldParcelRows * ParcelGrid.LotsPerAxis;
        _lateralHalfWidthPx =
            _worldParcelColumns * ParcelGrid.LotsPerAxis * LotUnitPx * 0.5f;

        _actionMenu = GD.Load<PackedScene>(ResourceActionMenuScenePath).Instantiate<ResourceActionMenu>();
        _actionMenu.GatherRequested += OnGatherRequested;
        // ScreenContent is still mid-_Ready() for its children, so transient
        // controls are attached after the scene-tree setup pass.
        GetParent().CallDeferred(Node.MethodName.AddChild, _actionMenu);
        _cultivationActionMenu = GD.Load<PackedScene>(CultivationActionMenuScenePath)
            .Instantiate<CultivationActionMenu>();
        _cultivationActionMenu.CultivationRequested += OnCultivationRequested;
        GetParent().CallDeferred(Node.MethodName.AddChild, _cultivationActionMenu);
        _selectionInfoPanel = new SelectionInfoPanel();
        GetParent().CallDeferred(Node.MethodName.AddChild, _selectionInfoPanel);
        _navmeshPlanner = new StreetNavigationServerPlanner();
        BuildPlacementChrome();

        _controller.BuildingStateChanged += OnWorldChanged;
        _controller.ProjectStateChanged += OnWorldChanged;
        _controller.NaturalResourceStateChanged += OnWorldChanged;
        _controller.CultivationSiteStateChanged += OnWorldChanged;
        _controller.WorldTickAdvanced += OnWorldTickAdvanced;
        _controller.SelectionChanged += OnSelectionChanged;
        _controller.HeroCreated += OnHeroCreated;
        _controller.ObservedCitizenChanged += OnObservedCitizenChanged;
        _constructionMenuButton.Pressed += OnConstructionMenuPressed;
        _expeditionMenuButton.Pressed += OnExpeditionMenuPressed;
        _policiesButton.Pressed += OnPoliciesPressed;
        _citizensButton.Pressed += OnCitizensPressed;
        _constructionPanel.PlacementRequested += OnPlacementRequested;
        _constructionPanel.CloseRequested += OnConstructionPanelCloseRequested;
        _modalHost.Closed += OnModalHostClosedForButtonLabel;
        _cameraModeButton.Pressed += ToggleCameraMode;
        UpdateCameraModeButtonLabel();
        UpdateConstructionButtonLabel();

        RefreshPlots();
        _freeCameraStreet = Mathf.Clamp(2, 0, _streetCount - 1);
        _cameraDepthAnchor = _freeCameraStreet;
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
        Scale = Vector2.One;
        ZoomTowardPivot(DefaultZoom, new Vector2(CenterX, CameraZoomPivotY));
    }

    public override void _ExitTree()
    {
        _controller.BuildingStateChanged -= OnWorldChanged;
        _controller.ProjectStateChanged -= OnWorldChanged;
        _controller.NaturalResourceStateChanged -= OnWorldChanged;
        _controller.CultivationSiteStateChanged -= OnWorldChanged;
        _controller.WorldTickAdvanced -= OnWorldTickAdvanced;
        _controller.SelectionChanged -= OnSelectionChanged;
        _controller.HeroCreated -= OnHeroCreated;
        _controller.ObservedCitizenChanged -= OnObservedCitizenChanged;
        _actionMenu.GatherRequested -= OnGatherRequested;
        _cultivationActionMenu.CultivationRequested -= OnCultivationRequested;
        _constructionMenuButton.Pressed -= OnConstructionMenuPressed;
        _expeditionMenuButton.Pressed -= OnExpeditionMenuPressed;
        _policiesButton.Pressed -= OnPoliciesPressed;
        _citizensButton.Pressed -= OnCitizensPressed;
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
        _cultivationActionMenu.Hide();
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
        _navigationRail.Show();
        _actionMenu.Hide();
        _cultivationActionMenu.Hide();
        _selectionInfoPanel.Hide();
        _worldStatusBubble.Hide();
        Show();
        RefreshPlots();
        _chronicle.ShowLog(_controller.GetCityMacroSnapshot().Events);
    }

    /// <summary>
    /// Projected position of a building's base, or <c>Vector2.Zero</c> when it
    /// is not currently drawn (off-window, or the city has no such building).
    ///
    /// <para>
    /// The first night needs this for the campfire. It used to assume the
    /// campfire sat at the founder's own projected spot minus 32 px, which put
    /// it literally on top of the citizen — invisible while the embers were a
    /// faint wireframe, obvious the moment they became a sprite.
    /// </para>
    /// </summary>
    public Vector2 GetBuildingGlobalPosition(int buildingId)
    {
        foreach ((Rect2 rect, int id) in _clickableRects)
        {
            if (id != buildingId) continue;
            return ToGlobal(new Vector2(rect.Position.X + rect.Size.X * 0.5f, rect.End.Y));
        }
        return Vector2.Zero;
    }

    public Vector2 GetFoundingArrivalGlobalPosition()
    {
        int street = FoundingLayout.InitialParcelRow * ParcelGrid.ConstructionRowsPerParcel
            + FoundingLayout.FounderRowWithinParcel;
        float totalFrontageColumns = _worldParcelColumns * ParcelGrid.FrontageColumnsPerParcel;
        float frontageCenter = FoundingLayout.InitialParcelColumn
            * ParcelGrid.FrontageColumnsPerParcel
            + FoundingLayout.FounderFrontageColumnWithinParcel
            + 0.5f;
        float lateral = (frontageCenter - totalFrontageColumns * 0.5f) * TileUnitPx;
        (Vector2 position, _) = ProjectDepth(
            AnchorDepth(street - CameraDepthAnchor),
            lateral - CameraLateral);
        return ToGlobal(position);
    }

    public void PrepareFounderArrival()
    {
        ActivatePerspective();
        _navigationRail.Hide();
        _heroCarrier?.SetState(CitizenSpriteCarrier.VisualState.Hidden);
    }

    public void CompleteFounderArrival()
    {
        ActivatePerspective();
        _navigationRail.Show();
        EnsureHeroCarrier(_controller.GetCityMacroSnapshot());
    }

    /// <summary>Hides this view plus its own transient surfaces (menu, axe cursor, placement, selection, zoom).</summary>
    private void Deactivate()
    {
        ClearWorldStatusHover();
        _visualStatusCitizenId = null;
        Hide();
        _navigationRail.Hide();
        _chronicle.Hide();
        _actionMenu.Hide();
        _cultivationActionMenu.Hide();
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
        _zoomLevel = DefaultZoom;
        Scale = Vector2.One;
        Position = _neutralPosition;
        ZoomTowardPivot(DefaultZoom, new Vector2(CenterX, CameraZoomPivotY));
        _pendingBuildingEntry = null;
    }

    /// <summary>Opens or closes construction from the city toolbar.</summary>
    private void OnConstructionMenuPressed()
    {
        if (!Visible) return;
        ClearWorldStatusHover();
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
        ClearWorldStatusHover();
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
        ConstructionPlacementSnapshot placement =
            _controller.GetConstructionPlacementSnapshot();
        if (!placement.Windows.Any(window => window.IsValid))
        {
            Notifier.ShowError(UiText.Get("No unlocked parcel has a free building lot."));
            return;
        }
        _modalHost.Close();
        BeginPlacement((ConstructionKind)constructionKind, placement);
    }

    private void OnConstructionPanelCloseRequested()
    {
        if (!Visible) return;
        _modalHost.Close();
    }

    private void OnPoliciesPressed()
    {
        if (!Visible) return;
        ClearWorldStatusHover();
        if (_modalHost.IsOpen && _modalHost.Content == _policiesPanel)
        {
            _modalHost.Close();
            return;
        }
        _policiesPanel.Open();
    }

    private void OnCitizensPressed()
    {
        if (!Visible) return;
        ClearWorldStatusHover();
        if (_modalHost.IsOpen && _modalHost.Content == _citizensPanel)
        {
            _modalHost.Close();
            return;
        }
        _citizensPanel.Open();
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

    internal void ShowConstructionPlacementHoverForVisualRegression(bool valid)
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        ActivatePerspective();
        OnPlacementRequested((int)ConstructionKind.Farm);
        foreach (PlacementLotBox candidate in _placementLots)
        {
            if (candidate.Window.IsValid != valid) continue;
            _hoveredPlacementLot = candidate;
            _placementInstruction.Text = PlacementHoverText(candidate.Window.State);
            QueueRedraw();
            return;
        }
    }

    internal void ShowEarlyGameResourcesForVisualRegression()
    {
        ActivatePerspective();
    }

    internal void ShowCitizenStatusForVisualRegression(CitizenId citizenId)
    {
        RefreshPlots();
        _visualStatusCitizenId = citizenId;
        CitizenSpriteCarrier? carrier = null;
        if (_heroCarrier?.Id == citizenId)
        {
            carrier = _heroCarrier;
        }
        else if (_citizenJourneys.TryGetValue(citizenId.Value, out CitizenJourney? journey))
        {
            carrier = journey.Carrier;
        }

        if (carrier is null || !IsVisibleMacroCarrier(carrier))
        {
            GD.PushError($"World-status fixture could not expose citizen {citizenId.Value} on the macro map.");
            return;
        }
        if (!_citizenStates.TryGetValue(citizenId.Value, out CityMacroSnapshot.CitizenItem? citizen)) return;
        ShowCitizenStatus(citizen, CitizenHoverRect(carrier));
    }

    /// <summary>
    /// Exercises the production click path (no direct call to the bubble or
    /// selection panel) by finding the citizen's hit rect and routing it
    /// through <see cref="TryClick"/>. The visual regression fixture uses
    /// this so the matrix proves the citizen summary actually surfaces via
    /// the same code path the player hits with a real left-click — not just
    /// via the bubble's manual call.
    /// </summary>
    internal void TriggerCitizenClickForVisualRegression(CitizenId citizenId)
    {
        RefreshPlots();
        CitizenSpriteCarrier? carrier = null;
        if (_heroCarrier?.Id == citizenId)
        {
            carrier = _heroCarrier;
        }
        else if (_citizenJourneys.TryGetValue(citizenId.Value, out CitizenJourney? journey))
        {
            carrier = journey.Carrier;
        }

        if (carrier is null || !IsVisibleMacroCarrier(carrier))
        {
            GD.PushError($"Citizen-click fixture could not expose citizen {citizenId.Value} on the macro map.");
            return;
        }
        Rect2 hit = CitizenHoverRect(carrier);
        try
        {
            TryClick(hit.GetCenter());
        }
        catch (System.Exception ex)
        {
            GD.PushError($"Citizen-click fixture failed: {ex.Message}");
        }
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
    /// frontage window at its calle/lateral position (same mapping
    /// <see cref="AddPlot"/> uses), matching real domain
    /// <see cref="ConstructionLot"/> data. Base blueprints reserve three
    /// whole frontage columns and three depth rows.
    /// </summary>
    private void BeginPlacement(
        ConstructionKind kind,
        ConstructionPlacementSnapshot placement)
    {
        _placementActive = true;
        _placementKind = kind;
        _selectedPlacementLot = null;
        _hoveredPlacementLot = null;
        _placementConfirmButton.Disabled = true;
        _placementBaseInstruction = UiText.Format(
            "ui.construction.choose_lot",
            UiText.Get(ConstructionRules.DisplayNameFor(kind)));
        _placementInstruction.Text = _placementBaseInstruction;
        _placementInstruction.Visible = true;
        _placementFooter.Visible = true;
        _actionMenu.Hide();
        _cultivationActionMenu.Hide();
        _selectionInfoPanel.Hide();
        _selectedTree = null;
        _selectedBuildingId = null;
        ClearTreeHover();
        _navigationRail.Hide();
        float totalFrontageColumns = _worldParcelColumns * ParcelGrid.FrontageColumnsPerParcel;
        _placementLots.Clear();
        _placementCells.Clear();
        foreach (ConstructionPlacementSnapshot.WindowItem window in placement.Windows)
        {
            ConstructionLot lot = window.Lot;
            int street = lot.RowId.Value;
            float frontageCenter = lot.StartColumn + lot.FrontageColumns * 0.5f;
            float lateralOffset = (frontageCenter - totalFrontageColumns * 0.5f) * TileUnitPx;
            _placementLots.Add(new PlacementLotBox(
                window,
                street,
                lateralOffset,
                lot.FrontageColumns * TileUnitPx,
                BuildingReservation.RequiredDepthRows * TileUnitPx));
        }
        foreach (ConstructionPlacementSnapshot.CellItem cell in placement.Cells)
        {
            float frontageCenter = cell.FrontageColumn + 0.5f;
            float lateralOffset = (frontageCenter - totalFrontageColumns * 0.5f) * TileUnitPx;
            _placementCells.Add(new PlacementCellBox(
                cell,
                cell.RowId.Value,
                lateralOffset,
                TileUnitPx,
                BuildingReservation.RequiredDepthRows * TileUnitPx));
        }
        QueueRedraw();
    }

    private void EndPlacement()
    {
        _placementActive = false;
        _selectedPlacementLot = null;
        _hoveredPlacementLot = null;
        _placementLots.Clear();
        _placementCells.Clear();
        _clickablePlacementRects.Clear();
        _placementInstruction.Visible = false;
        _placementFooter.Visible = false;
        _navigationRail.Show();
        UpdateConstructionButtonLabel();
        QueueRedraw();
    }

    private void SelectPlacementLot(PlacementLotBox lot)
    {
        if (!lot.Window.IsValid)
        {
            _selectedPlacementLot = null;
            _placementConfirmButton.Disabled = true;
            _placementInstruction.Text = PlacementHoverText(lot.Window.State);
            QueueRedraw();
            return;
        }
        _selectedPlacementLot = lot.Window.Lot;
        _placementConfirmButton.Disabled = false;
        _placementInstruction.Text = UiText.Get("ui.construction.placement_selected");
        QueueRedraw();
    }

    private void UpdatePlacementHover(Vector2 mousePosition)
    {
        PlacementLotBox? nearest = null;
        float nearestDistanceSquared = float.MaxValue;
        foreach ((Rect2 rect, PlacementLotBox lot) in _clickablePlacementRects)
        {
            if (!rect.HasPoint(mousePosition)) continue;
            float distanceSquared = mousePosition.DistanceSquaredTo(rect.GetCenter());
            if (distanceSquared >= nearestDistanceSquared) continue;
            nearest = lot;
            nearestDistanceSquared = distanceSquared;
        }

        if (_hoveredPlacementLot == nearest) return;
        _hoveredPlacementLot = nearest;
        _placementInstruction.Text = nearest is PlacementLotBox hovered
            ? PlacementHoverText(hovered.Window.State)
            : _selectedPlacementLot.HasValue
                ? UiText.Get("ui.construction.placement_selected")
                : _placementBaseInstruction;
        QueueRedraw();
    }

    private static string PlacementHoverText(FrontageCellState state) =>
        UiText.Get(state switch
        {
            FrontageCellState.Available => "ui.construction.placement_valid",
            FrontageCellState.NaturalResource => "ui.construction.placement_blocked_resource",
            FrontageCellState.ReservedByBuilding => "ui.construction.placement_blocked_building",
            FrontageCellState.ReservedAsCorridor => "ui.construction.placement_blocked_corridor",
            _ => "ui.construction.placement_blocked_territory",
        });

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
        RefreshParcelEnvelope(snapshot);
        _citizenStates.Clear();
        foreach (CityMacroSnapshot.CitizenItem citizen in snapshot.Citizens)
        {
            _citizenStates[citizen.Id.Value] = citizen;
        }
        float totalLotColumns = _worldParcelColumns * ParcelGrid.LotsPerAxis;

        foreach (CityMacroSnapshot.PlotItem item in snapshot.Buildings)
        {
            if (item.Kind == BuildingKind.Forest)
            {
                AddTrees(item, totalLotColumns);
                continue;
            }
            AddPlot(item, clickable: true);
        }
        foreach (CityMacroSnapshot.PlotItem item in snapshot.Projects)
        {
            AddPlot(item, clickable: false);
        }
        EnsureHeroCarrier(snapshot);
        RefreshCitizenVisuals(snapshot);
        RefreshSelectionInfoIfShown();
        QueueRedraw();
    }

    private void RefreshParcelEnvelope(CityMacroSnapshot snapshot)
    {
        _parcelTerritory.Clear();
        int maximumColumn = -1;
        int maximumRow = -1;
        foreach (CityMacroSnapshot.ParcelItem parcel in snapshot.Parcels)
        {
            _parcelTerritory[(parcel.LogicalRow, parcel.LogicalColumn)] = parcel.TerritoryState;
            maximumColumn = Math.Max(maximumColumn, parcel.LogicalColumn);
            maximumRow = Math.Max(maximumRow, parcel.LogicalRow);
        }
        _worldParcelColumns = Math.Max(1, maximumColumn + 1);
        _worldParcelRows = Math.Max(1, maximumRow + 1);
        _streetCount = _worldParcelRows * ParcelGrid.ConstructionRowsPerParcel;
        _lateralHalfWidthPx = _worldParcelColumns
            * ParcelGrid.LotsPerAxis
            * LotUnitPx
            * 0.5f;
    }

    /// <summary>
    /// Keeps the selection panel's remaining reserve live as the world ticks
    /// or a gather completes. Clears the
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
        if (_selectedCitizenId is { } selectedCitizenId)
        {
            if (_citizenStates.ContainsKey(selectedCitizenId.Value))
            {
                SelectCitizen(selectedCitizenId);
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
    /// Populates the selection panel with the same at-a-glance summary the
    /// building branch writes (icon + title + detail). The detail is the
    /// first non-empty line of the citizen's current world status (activity,
    /// location, wound, treatment) so a single click is enough to read what
    /// the citizen is doing and why they're not at work — the same affordance
    /// the player already gets for trees and buildings.
    /// </summary>
    private void SelectCitizen(CitizenId citizenId)
    {
        if (!_citizenStates.TryGetValue(citizenId.Value, out CityMacroSnapshot.CitizenItem? citizen))
        {
            ClearSelection();
            return;
        }
        _selectedCitizenId = citizenId;
        _selectedTree = null;
        _selectedBuildingId = null;
        Texture2D? icon = ResourceLoader.Load<Texture2D>(IconPaths.User);
        _selectionInfoPanel.ShowSelection(icon, citizen.Name, FormatCitizenSelectionDetail(citizen));
    }

    internal static string FormatCitizenSelectionDetail(CityMacroSnapshot.CitizenItem citizen)
    {
        var lines = new List<string>();
        foreach (SelectionLine line in BuildCitizenSelectionKeys(citizen))
        {
            _ = line.IconPath;
            if (line.FormatArgs is null)
            {
                lines.Add(UiText.Get(line.TextKey));
                continue;
            }
            object[] translated = TranslateSelectionArgs(line.FormatArgs);
            lines.Add(UiText.Format(line.TextKey, translated));
        }
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Translates raw domain values into the strings the view layer formats
    /// via <see cref="UiText.Format"/>. The only translation needed today is
    /// the wound recovery duration (ticks → human-readable string); the
    /// severity key is already a localization key and passes through unchanged.
    /// </summary>
    private static object[] TranslateSelectionArgs(IReadOnlyList<object> formatArgs)
    {
        var translated = new object[formatArgs.Count];
        for (int index = 0; index < formatArgs.Count; index++)
        {
            translated[index] = formatArgs[index] is int ticks
                ? SimulationTimeText.FormatDurationLocalized(ticks)
                : formatArgs[index];
        }
        return translated;
    }

    internal readonly record struct SelectionLine(
        string IconPath,
        string TextKey,
        IReadOnlyList<object>? FormatArgs);

    /// <summary>
    /// Returns the same lines the bubble/body would render, but as raw
    /// (icon, key, formatArgs) so the structure can be unit-tested without
    /// pulling Godot's translation runtime into a Godot-free xUnit process.
    /// Translation happens at the view layer (<see cref="FormatCitizenSelectionDetail"/>).
    /// The remaining <see cref="citizen.WoundRecoveryTicksRemaining"/> slot
    /// is the raw tick count; the view layer resolves it to a localized
    /// duration string before passing it to <see cref="UiText.Format"/>.
    /// </summary>
    internal static IReadOnlyList<SelectionLine> BuildCitizenSelectionKeys(CityMacroSnapshot.CitizenItem citizen)
    {
        var lines = new List<SelectionLine>();
        if (citizen.IsOnExpedition)
        {
            lines.Add(new SelectionLine(
                IconPaths.Shield,
                "ui.world_status.expedition",
                null));
        }
        else
        {
            if (citizen.BlockReason == CitizenRoutineBlockReason.NoFood)
            {
                lines.Add(new SelectionLine(
                    IconPaths.Warning,
                    "ui.world_status.no_food",
                    null));
            }
            else
            {
                string key = citizen.Activity switch
                {
                    CitizenRoutineActivity.Working => "ui.world_status.working",
                    CitizenRoutineActivity.TravellingToWork => "ui.world_status.travelling",
                    CitizenRoutineActivity.TravellingHome => "ui.world_status.travelling",
                    CitizenRoutineActivity.WaitingForStorage => "ui.world_status.waiting_storage",
                    CitizenRoutineActivity.WaitingForResources => "ui.world_status.waiting_resources",
                    CitizenRoutineActivity.WorkplaceIdle => "ui.world_status.work_paused",
                    CitizenRoutineActivity.OffDuty => "ui.world_status.off_duty",
                    CitizenRoutineActivity.Resting => "ui.world_status.resting",
                    CitizenRoutineActivity.Recovering => "ui.world_status.recovering",
                    CitizenRoutineActivity.Leisure => "ui.world_status.idle",
                    _ => "ui.world_status.unavailable",
                };
                lines.Add(new SelectionLine(IconPaths.Cog, key, null));
            }
        }
        if (citizen.WoundSeverity is WoundSeverity severity)
        {
            string severityKey = severity == WoundSeverity.Severe
                ? "ui.wound.severe"
                : "ui.wound.moderate";
            lines.Add(new SelectionLine(
                IconPaths.Heart,
                "ui.world_status.wound",
                new object[] { severityKey }));
            if (citizen.IsReceivingWoundTreatment)
            {
                lines.Add(new SelectionLine(
                    IconPaths.Clock,
                    "ui.world_status.treatment",
                    new object[] { citizen.WoundRecoveryTicksRemaining }));
            }
        }
        return lines;
    }

    /// <summary>
    /// Each natural-resource patch has one reserve per visible unit;
    /// <c>ParcelGrid.NaturalResourceLot</c> gives every unit a stable minimum
    /// reservation. Navigation uses the unit's authored obstacle clearances,
    /// never a special rule for trees or the full reserved lot.
    /// </summary>
    private void AddTrees(CityMacroSnapshot.PlotItem forest, float totalLotColumns)
    {
        for (int unitId = 0; unitId < forest.WoodUnitReserves.Count; unitId++)
        {
            if (forest.WoodUnitReserves[unitId] <= 0) continue;
            if (unitId >= forest.ResourceUnitPositions.Count) continue;
            NaturalResourceUnitPosition position = forest.ResourceUnitPositions[unitId];
            int street = forest.ParcelRow * ParcelGrid.ConstructionRowsPerParcel
                + position.RowWithinParcel;
            float totalFrontageColumns = totalLotColumns * ParcelGrid.TilesPerStandardLot;
            float frontageCenter = forest.ParcelColumn * ParcelGrid.FrontageColumnsPerParcel
                + position.FrontageColumnWithinParcel
                + 0.5f;
            float lateralOffset = (frontageCenter - totalFrontageColumns * 0.5f) * TileUnitPx;
            _trees.Add(new TreeBox(
                street,
                lateralOffset,
                forest.Id.Value,
                unitId,
                forest.GroundResourceType ?? ResourceType.Wood,
                forest.WoodUnitReserves[unitId]));
            ObstacleFootprintTemplate footprint = NaturalResourceFootprintCatalog.Get(
                forest.GroundResourceType ?? ResourceType.Wood);
            float halfTileUnitPx = TileUnitPx * 0.5f;
            float reservedWidth = footprint.ReservedArea.Width * halfTileUnitPx;
            StreetRoutePlanner.Interval obstacle = ObstacleIntervalFromClearances(
                lateralOffset - reservedWidth * 0.5f,
                reservedWidth,
                footprint.LeftClearance * halfTileUnitPx,
                footprint.RightClearance * halfTileUnitPx);
            AddBandInterval(street, obstacle.Start, obstacle.End);
        }
    }

    private void AddPlot(CityMacroSnapshot.PlotItem item, bool clickable)
    {
        int street = item.RowId;
        float totalFrontageColumns = _worldParcelColumns * ParcelGrid.FrontageColumnsPerParcel;
        float frontageCenter = item.StartColumn + item.FrontageColumns * 0.5f;
        float lateralOffset = (frontageCenter - totalFrontageColumns * 0.5f) * TileUnitPx;
        float width = item.FrontageColumns * TileUnitPx;
        _plots.Add(new PlotBox(
            street,
            lateralOffset,
            width,
            item.DepthRows * TileUnitPx,
            item.Id.Value,
            item.DisplayName,
            item.Kind,
            item.IsUnderConstruction,
            clickable,
            item.Stock,
            item.StorageCapacity,
            item.CultivationState,
            item.ReadyAtTick));
        StreetRoutePlanner.Interval obstacle = BuildingObstacleInterval(
            item,
            totalFrontageColumns,
            TileUnitPx);
        AddBandInterval(street, obstacle.Start, obstacle.End);
    }

    internal static StreetRoutePlanner.Interval BuildingObstacleInterval(
        CityMacroSnapshot.PlotItem item,
        float totalFrontageColumns,
        float tileUnitPx)
    {
        float reservedLeft = (item.StartColumn
            - totalFrontageColumns * 0.5f) * tileUnitPx;
        float reservedWidth = item.FrontageColumns * tileUnitPx;
        float solidLeft = (item.StructuralStartHalfColumn * 0.5f
            - totalFrontageColumns * 0.5f) * tileUnitPx;
        float solidWidth = item.StructuralFrontageHalfColumns * 0.5f * tileUnitPx;
        return ObstacleIntervalFromClearances(
            reservedLeft,
            reservedWidth,
            solidLeft - reservedLeft,
            reservedLeft + reservedWidth - solidLeft - solidWidth);
    }

    internal static StreetRoutePlanner.Interval ObstacleIntervalFromClearances(
        float reservedStart,
        float reservedWidth,
        float leftClearance,
        float rightClearance)
    {
        if (reservedWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(reservedWidth));
        if (leftClearance < 0f) throw new ArgumentOutOfRangeException(nameof(leftClearance));
        if (rightClearance < 0f) throw new ArgumentOutOfRangeException(nameof(rightClearance));
        if (leftClearance + rightClearance >= reservedWidth)
        {
            throw new ArgumentException(
                "Obstacle clearances must leave a positive solid interval.");
        }
        return new StreetRoutePlanner.Interval(
            reservedStart + leftClearance,
            reservedStart + reservedWidth - rightClearance);
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
    private float CameraLateral =>
        _cameraFollowsHero && TryGetObservedCitizenAnchor(out _, out float lateral)
            ? lateral
            : _freeCameraLateral;

    /// <summary>The vanishing point's smoothed depth — see the class doc's
    /// "Camera mode" note and <see cref="AdvanceTransition"/>.</summary>
    private float CameraDepthAnchor =>
        _cameraFollowsHero && TryGetObservedCitizenAnchor(out float depth, out _)
            ? depth
            : _cameraDepthAnchor;

    private bool IsProjectedDepthVisible(float relativeDepth) =>
        StreetDepthProjection.IsVisibleDepth(relativeDepth);

    private static float ProjectedRowScreenY(float relativeDepth) =>
        StreetDepthProjection.RowScreenY(relativeDepth, BaseY);

    private static float ProjectedHorizontalScale(float relativeDepth) =>
        StreetDepthProjection.HorizontalScale(relativeDepth);

    private (Vector2 Position, Vector2 Scale) ProjectDepth(
        float relativeDepth,
        float lateralOffset) => StreetDepthProjection.Project(
            relativeDepth,
            lateralOffset,
            CenterX,
            BaseY);

    internal static bool FollowsFounderByDefault => DefaultCameraFollowsHero;
    internal static float MinimumZoomForTests => MinZoom;
    internal static float MaximumZoomForTests => MaxZoom;
    internal static float CameraZoomPivotYForTests => CameraZoomPivotY;

    internal bool HasActiveCitizenJourneyForVisualRegression(CitizenId citizenId) =>
        System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") == "1"
        && _citizenJourneys.TryGetValue(citizenId.Value, out CitizenJourney? journey)
        && journey.Route is not null;

    public void ShowThirdStreetDepthForVisualRegression()
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        SetCameraFollowsHero(false);
        _freeCameraStreet = Mathf.Clamp(2, 0, _streetCount - 1);
        _cameraDepthAnchor = _freeCameraStreet;
        _cameraDepthTarget = null;
        _cameraTransitionAccumulator = 0f;
        QueueRedraw();
    }

    public void ShowLongTerrariumForVisualRegression()
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        ActivatePerspective();
        SetCameraFollowsHero(false);
        _freeCameraStreet = Mathf.Clamp(2, 0, _streetCount - 1);
        _cameraDepthAnchor = _freeCameraStreet;
        _cameraDepthTarget = null;
        _cameraTransitionAccumulator = 0f;
        ZoomTowardPivot(MinZoom, new Vector2(CenterX, CameraZoomPivotY));
        GD.Print(
            $"Long terrarium fixture: {_worldParcelRows} parcel rows, "
            + $"{_streetCount} streets, zoom {_zoomLevel:F2}.");
        QueueRedraw();
        if (!string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable(
                "WOG_LONG_TERRARIUM_CAPTURE")))
        {
            GetTree().CreateTimer(0.75).Timeout += CaptureLongTerrariumViewport;
        }
    }

    private void CaptureLongTerrariumViewport()
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        string? configuredPath = System.Environment.GetEnvironmentVariable(
            "WOG_LONG_TERRARIUM_CAPTURE");
        if (string.IsNullOrWhiteSpace(configuredPath)) return;
        string outputPath = configuredPath;
        Error result = GetViewport().GetTexture().GetImage().SavePng(outputPath);
        if (result == Error.Ok)
        {
            GD.Print($"Long terrarium viewport captured: {outputPath}");
        }
        else
        {
            GD.PushError($"Long terrarium viewport capture failed: {result}.");
        }
    }

    public override void _Process(double delta)
    {
        bool hasCitizenTravel = _route is not null
            || _citizenJourneys.Values.Any(journey => journey.Route is not null);
        if (!Visible && !hasCitizenTravel) return;
        _motionAccumulator += (float)delta;
        while (_motionAccumulator >= PixelMotion.CadenceSeconds)
        {
            _motionAccumulator -= PixelMotion.CadenceSeconds;
            MotionTick(allowCameraInput: Visible);
            AdvanceCitizenJourneysTick();
        }
        // The founder's own smoothed row (always active — it also paces
        // AdvanceRouteTick regardless of camera mode) and, independently,
        // the free camera's own smoothed row when not following.
        bool heroDepthAnimating = _depthTarget.HasValue;
        bool cameraDepthAnimating = _cameraDepthTarget.HasValue;
        AdvanceTransition(ref _depthAnchor, ref _depthTarget, ref _transitionAccumulator, delta);
        AdvanceTransition(
            ref _cameraDepthAnchor,
            ref _cameraDepthTarget,
            ref _cameraTransitionAccumulator,
            delta,
            DepthStepSize * VerticalPanTransitionMultiplier(_verticalPanHoldSeconds));
        bool citizenDepthAnimating = false;
        foreach (CitizenJourney journey in _citizenJourneys.Values)
        {
            citizenDepthAnimating |= journey.DepthTarget.HasValue;
            AdvanceJourneyTransition(journey, delta);
        }
        if (heroDepthAnimating || cameraDepthAnimating || citizenDepthAnimating) QueueRedraw();
        if (Visible)
        {
            AdvanceBuildingEntry(delta);
            if (!_visualStatusCitizenId.HasValue)
            {
                // The world owns the pointer whenever the cursor sits over
                // a citizen or a full-storage badge. The macro view's own
                // hit-rects are the single source of truth for what the
                // world can claim — overlaying a PanelContainer with
                // MouseFilter = Stop (OfflineReportPanel, MigrantPanel,
                // etc.) must not strip the bubble, because the macro view
                // is the only world surface and a Stop overlay sitting
                // beside a citizen does not mean the world yields input.
                // Without this, the bubble blinks open on the motion event
                // and ClearWorldStatusHover hides it one frame later —
                // the exact symptom the user reported (visible only when
                // an external window forced a redraw).
                Vector2 localMouse = ToLocal(GetViewport().GetMousePosition());
                if (_placementActive)
                {
                    UpdatePlacementHover(localMouse);
                }
                bool worldOwnsPointer = TryFindHoveredCitizen(localMouse, out _, out _)
                    || IsCursorOverStorageBadge(localMouse);
                if (!worldOwnsPointer && (
                    _modalHost.IsOpen
                    || _actionMenu.Visible
                    || _placementActive
                    || _pendingBuildingEntry is not null
                    || UiInputBoundary.IsPointerOwnedByUi(GetViewport())))
                {
                    ClearWorldStatusHover();
                }
                else
                {
                    UpdateWorldHover(localMouse);
                }
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible) return;
        // A building-entry push is a brief, exclusive, non-interruptible
        // transition — same spirit as the fullscreen placement scrim.
        if (_pendingBuildingEntry is not null) return;
        if (UiInputBoundary.IsWheelEvent(@event))
        {
            bool pointerIsOverScrollableUi = UiInputBoundary.IsPointerOverScrollableUi(GetViewport());
            if (!UiInputBoundary.ShouldWorldCameraHandleWheel(
                    isWheelEvent: true,
                    pointerIsOverScrollableUi))
            {
                GetViewport().SetInputAsHandled();
                return;
            }
        }
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
        if (_cultivationActionMenu.Visible && @event.IsActionPressed("ui_cancel"))
        {
            _cultivationActionMenu.Hide();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed(CameraInputActions.PanUp))
        {
            BeginVerticalCameraPan(1);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed(CameraInputActions.PanDown))
        {
            BeginVerticalCameraPan(-1);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed(CameraInputActions.ToggleFollow))
        {
            ToggleCameraMode();
            GetViewport().SetInputAsHandled();
            return;
        }
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true }:
                AdjustZoom(ZoomStep);
                GetViewport().SetInputAsHandled();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true }:
                AdjustZoom(-ZoomStep);
                GetViewport().SetInputAsHandled();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click:
                TryClick(ToLocal(click.Position));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } rightClick:
                TryRightClick(ToLocal(rightClick.Position));
                break;
            case InputEventMouseMotion motion:
                if (_placementActive)
                {
                    UpdatePlacementHover(ToLocal(motion.Position));
                }
                else
                {
                    UpdateWorldHover(ToLocal(motion.Position));
                }
                break;
        }
    }

    /// <summary>
    /// Quantized camera zoom (discrete steps, never a continuous drag) via
    /// this node's own <see cref="Node2D.Scale"/>, keeping the vanishing
    /// point (<see cref="CenterX"/>,<see cref="CameraZoomPivotY"/> in local
    /// space) fixed on screen. The lower-than-terrain pivot lets maximum
    /// zoom-out frame the first and last bands of a four-parcel-row window
    /// near the viewport edges without changing the projection angle.
    /// </summary>
    private void AdjustZoom(float delta)
    {
        float newZoom = Mathf.Clamp(_zoomLevel + delta, MinZoom, MaxZoom);
        if (Mathf.IsEqualApprox(newZoom, _zoomLevel)) return;
        ZoomTowardPivot(newZoom, new Vector2(CenterX, CameraZoomPivotY));
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
    /// founder or pan freely (the default), independent of any selection.
    /// Placement does not alter or lock this choice; directional input keeps
    /// its camera-only meaning while the player inspects candidate lots.
    /// </summary>
    private void ToggleCameraMode()
    {
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
            float currentLateral = CameraLateral;
            float currentDepth = CameraDepthAnchor;
            _freeCameraLateral = currentLateral;
            _freeCameraStreet = Mathf.RoundToInt(currentDepth);
            _cameraDepthAnchor = currentDepth;
            _cameraDepthTarget = null;
            _cameraTransitionAccumulator = 0f;
        }
        _cameraFollowsHero = value;
        UpdateCameraModeButtonLabel();
        QueueRedraw();
    }

    private void OnObservedCitizenChanged(int _)
    {
        // Selection only changes the potential target. It deliberately does
        // not activate follow; if follow was already explicit, it tracks the
        // newly selected citizen on the next projection.
        UpdateCameraModeButtonLabel();
        QueueRedraw();
    }

    private bool TryGetObservedCitizenAnchor(out float depth, out float lateral)
    {
        CitizenId? observedId = _controller.ObservedCitizenId;
        CitizenId? founderId = _controller.World.Hero?.Id;
        if (observedId is null || observedId == founderId)
        {
            depth = _depthAnchor;
            lateral = _heroLateral;
            return founderId is not null;
        }
        if (_citizenJourneys.TryGetValue(observedId.Value.Value, out CitizenJourney? journey))
        {
            depth = journey.DepthAnchor;
            lateral = journey.Lateral;
            return true;
        }

        CitizenRoutineSnapshot? routine = _controller.World.GetCitizenRoutine(observedId.Value);
        if (routine?.ContextBuildingId is BuildingId buildingId
            && FindPlot(buildingId) is PlotBox plot)
        {
            depth = WorkplaceEntranceStreet(plot.Street);
            lateral = plot.LateralOffset;
            return true;
        }
        depth = _depthAnchor;
        lateral = _heroLateral;
        return false;
    }

    private void UpdateCameraModeButtonLabel()
    {
        _cameraModeButton.SetIconAndLabel(
            IconPaths.Camera,
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
        if (_cultivationActionMenu.Visible) _cultivationActionMenu.Hide();

        // Placement mode is exclusive and blocks every other world click
        // while a lot is being chosen.
        if (_placementActive)
        {
            PlacementLotBox? nearest = null;
            float nearestDistanceSquared = float.MaxValue;
            foreach ((Rect2 rect, PlacementLotBox lot) in _clickablePlacementRects)
            {
                if (!rect.HasPoint(clickPosition)) continue;
                float distanceSquared = clickPosition.DistanceSquaredTo(rect.GetCenter());
                if (distanceSquared >= nearestDistanceSquared) continue;
                nearest = lot;
                nearestDistanceSquared = distanceSquared;
            }
            if (nearest is PlacementLotBox selected) SelectPlacementLot(selected);
            return;
        }
        foreach ((Rect2 rect, TreeBox tree) in _clickableTreeRects)
        {
            if (!rect.HasPoint(clickPosition)) continue;
            SelectTree(tree);
            return;
        }
        foreach ((Rect2 rect, CitizenId citizenId) in _clickableCitizenRects)
        {
            if (!rect.HasPoint(clickPosition)) continue;
            SelectCitizen(citizenId);
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
        foreach ((Rect2 rect, CitizenId citizenId) in _clickableCitizenRects)
        {
            if (!rect.HasPoint(clickPosition)) continue;
            SelectCitizen(citizenId);
            return;
        }
        foreach ((Rect2 rect, int buildingId) in _clickableRects)
        {
            if (!rect.HasPoint(clickPosition)) continue;
            SelectBuildingPlot(buildingId);
            PlotBox? plot = FindPlot(new BuildingId(buildingId));
            if (plot is { CultivationState: CultivationPlotState state })
            {
                OpenCultivationMenu(plot.Value, state, rect);
                return;
            }
            BeginBuildingEntry(new BuildingId(buildingId), clickPosition);
            return;
        }
        if (_actionMenu.Visible) _actionMenu.Hide();
        if (_cultivationActionMenu.Visible) _cultivationActionMenu.Hide();
    }

    private void SelectTree(TreeBox tree)
    {
        _selectedTree = tree;
        _selectedBuildingId = null;
        // The selection icon is the same sprite the world draws, so the panel
        // and the plot agree. Non-wood resources used to fall back to a
        // generic leaf glyph regardless of what they actually were.
        Texture2D icon = ResourceTree.CreateRegion(
            _terrainAtlas,
            tree.ResourceType == ResourceType.Wood
                // The trunk tile of the very tree that was clicked, so the
                // panel shows a cactus when a cactus was selected.
                ? TerrainAtlas.RegionOfId(
                    TerrainAtlas.TreeFor(_groundBiome, tree.ForestId, tree.UnitId).TrunkId)
                : TerrainAtlas.ResourceRegion(tree.ResourceType, tree.ForestId, tree.UnitId));
        string resourceName = UiText.Get(tree.ResourceType.ToString().ToLowerInvariant());
        string detail = UiText.Format("ui.resource.units_remain", tree.Reserve, resourceName);
        _selectionInfoPanel.ShowSelection(icon, resourceName, detail);
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
        PlotBox? selectedPlot = FindPlot(new BuildingId(buildingId));
        if (selectedPlot is { CultivationState: CultivationPlotState state })
        {
            PlotBox plot = selectedPlot.Value;
            _selectedBuildingId = buildingId;
            _selectedTree = null;
            string cultivationDetail = CultivationDetail(plot, state);
            _selectionInfoPanel.ShowSelection(
                GD.Load<Texture2D>(IconPaths.Leaf),
                UiText.Get("Cultivation Site"),
                cultivationDetail);
            return;
        }
        BuildingDetailSnapshot? snapshot = _controller.GetBuildingDetailSnapshot(new BuildingId(buildingId));
        if (snapshot is null)
        {
            ClearSelection();
            return;
        }
        _selectedBuildingId = buildingId;
        _selectedTree = null;
        Texture2D? icon = GetBuildingTexture(snapshot.Kind);
        // For Home: count citizens physically at home (VisibleCitizens).
        // For production buildings: VisibleWorkerCount/HiddenWorkerCount
        // were derived from the building's own _assigned roster, which
        // can exceed VisibleCitizens during in-transit ticks (assigned
        // but not yet AtWork). Reading VisibleCitizens keeps the
        // selection panel consistent with the detail panel.
        int occupants = snapshot.VisibleCitizens.Count;
        string detail = snapshot.IsHome
            ? UiText.Format("ui.selection.building_home", occupants, snapshot.WorkerCapacity)
            : UiText.Format("ui.selection.building_workers", occupants, snapshot.WorkerCapacity);
        string fullLabel = UiText.Format(
            "ui.building_detail.full_label", UiText.Get(snapshot.DisplayName), UiText.Get(snapshot.ResourceLabel));
        _selectionInfoPanel.ShowSelection(icon, fullLabel, detail);
    }

    private string CultivationDetail(PlotBox plot, CultivationPlotState state)
    {
        if (state == CultivationPlotState.Growing
            && plot.ReadyAtTick is int readyAtTick)
        {
            int remaining = Mathf.Max(0, readyAtTick - _controller.World.CurrentTick);
            int days = (int)System.Math.Ceiling(
                remaining / (double)GameClock.TicksPerInGameDay);
            return UiText.Format("ui.cultivation.growing", days);
        }
        return state switch
        {
            CultivationPlotState.Prepared => UiText.Get("ui.cultivation.prepared"),
            CultivationPlotState.Sown => UiText.Get("ui.cultivation.sown"),
            CultivationPlotState.Ready => UiText.Format(
                "ui.cultivation.ready", CultivationRules.HarvestFoodYield),
            CultivationPlotState.Spent => UiText.Get("ui.cultivation.spent"),
            _ => UiText.Get("Cultivation Site"),
        };
    }

    private void OpenCultivationMenu(
        PlotBox plot,
        CultivationPlotState state,
        Rect2 rect)
    {
        Citizen? founder = _controller.World.Hero;
        bool founderAvailable = founder?.IsAvailable == true;
        bool canAct = founderAvailable
            && state is (CultivationPlotState.Prepared or CultivationPlotState.Ready);
        string tooltip = !founderAvailable
            ? UiText.Get("ui.cultivation.founder_unavailable")
            : state switch
        {
            CultivationPlotState.Prepared => _controller.World.FoodStock
                >= CultivationRules.SeedFoodCost
                    ? UiText.Get("ui.cultivation.sow_action")
                    : UiText.Get("ui.cultivation.missing_seed_food"),
            CultivationPlotState.Ready => UiText.Get("ui.cultivation.harvest_action"),
            CultivationPlotState.Sown or CultivationPlotState.Growing =>
                UiText.Get("ui.cultivation.not_ready"),
            _ => UiText.Get("ui.cultivation.spent"),
        };
        if (state == CultivationPlotState.Prepared
            && _controller.World.FoodStock < CultivationRules.SeedFoodCost)
        {
            canAct = false;
        }
        _actionMenu.Hide();
        Vector2 menuAnchor = ToGlobal(rect.GetCenter())
            - ((Control)GetParent()).GlobalPosition;
        _cultivationActionMenu.Open(
            plot.BuildingId,
            state,
            menuAnchor,
            canAct,
            tooltip);
    }

    private void OnCultivationRequested(int siteId)
    {
        CultivationSite? site = _controller.World.GetCultivationSite(new BuildingId(siteId));
        if (site is null) return;
        CultivationActionResult result = site.State == CultivationPlotState.Prepared
            ? _controller.TrySowCultivationSite(site.Id)
            : _controller.TryHarvestCultivationSite(site.Id);
        if (result.IsSuccess)
        {
            Notifier.Show(result.FoodDelta < 0
                ? UiText.Get("ui.cultivation.sown_notice")
                : UiText.Format("ui.cultivation.harvest_notice", result.FoodDelta));
            _statusPanel.Refresh(_controller);
        }
        else
        {
            string key = result.Outcome switch
            {
                CultivationActionOutcome.FounderUnavailable =>
                    "ui.cultivation.founder_unavailable",
                CultivationActionOutcome.MissingFood =>
                    "ui.cultivation.missing_seed_food",
                _ => "ui.cultivation.action_unavailable",
            };
            Notifier.ShowError(UiText.Get(key));
        }
    }

    /// <summary>Extension point: citizens will route here too once they get selection info.</summary>
    private void ClearSelection()
    {
        if (_selectedTree is null && _selectedBuildingId is null && _selectedCitizenId is null) return;
        _selectedTree = null;
        _selectedBuildingId = null;
        _selectedCitizenId = null;
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
        ClearWorldStatusHover();
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
        ResourceType hoveredResource = ResourceType.Wood;
        foreach ((Rect2 rect, TreeBox unit) in _clickableTreeRects)
        {
            if (!rect.HasPoint(mousePosition)) continue;
            hovering = true;
            hoveredResource = unit.ResourceType;
            break;
        }
        // Track the resource too, not just "something is hovered": moving
        // between a tree and a stone without leaving the resource band has to
        // swap the tool, and it used to raise the axe for every one of them.
        if (hovering == _treeHovered && hoveredResource == _hoveredResource) return;
        _treeHovered = hovering;
        _hoveredResource = hoveredResource;
        if (hovering) _cursorController?.UseGatherCursor(hoveredResource);
        else _cursorController?.RestoreSurfaceCursor();
    }

    private void UpdateWorldHover(Vector2 mousePosition)
    {
        if (_placementActive || UiInputBoundary.IsPointerOwnedByUi(GetViewport()))
        {
            ClearTreeHover();
            ClearWorldStatusHover();
            return;
        }

        if (TryFindHoveredCitizen(mousePosition, out CityMacroSnapshot.CitizenItem? citizen, out Rect2 citizenRect)
            && citizen is not null)
        {
            ClearTreeHover();
            if (_hoveredCitizenId != citizen.Id.Value || _hoveredStorageBuildingId.HasValue)
            {
                _hoveredCitizenId = citizen.Id.Value;
                _hoveredStorageBuildingId = null;
            }
            ShowCitizenStatus(citizen, citizenRect);
            return;
        }

        foreach ((Rect2 rect, PlotBox plot) in _storageFullBadgeRects)
        {
            if (!rect.HasPoint(mousePosition)) continue;
            ClearTreeHover();
            if (_hoveredCitizenId.HasValue || _hoveredStorageBuildingId != plot.BuildingId)
            {
                _hoveredCitizenId = null;
                _hoveredStorageBuildingId = plot.BuildingId;
            }
            _worldStatusBubble.ShowAt(
                ToGlobal(new Vector2(rect.GetCenter().X, rect.Position.Y)),
                UiText.Get(plot.DisplayName),
                new[]
                {
                    new WorldStatusBubble.Item(
                        IconPaths.Check,
                        UiText.Format("ui.world_status.storage_full", plot.Stock, plot.StorageCapacity)),
                });
            return;
        }

        ClearWorldStatusHover();
        UpdateTreeHover(mousePosition);
    }

    private void ClearWorldStatusHover()
    {
        _hoveredCitizenId = null;
        _hoveredStorageBuildingId = null;
        _worldStatusBubble.Hide();
    }

    private bool IsCursorOverStorageBadge(Vector2 localMouse)
    {
        foreach ((Rect2 rect, PlotBox _) in _storageFullBadgeRects)
        {
            if (rect.HasPoint(localMouse)) return true;
        }
        return false;
    }

    private void ShowCitizenStatus(CityMacroSnapshot.CitizenItem citizen, Rect2 citizenRect)
    {
        _worldStatusBubble.ShowAt(
            ToGlobal(new Vector2(citizenRect.GetCenter().X, citizenRect.Position.Y)),
            citizen.Name,
            BuildCitizenStatusItems(citizen));
    }

    private bool TryFindHoveredCitizen(
        Vector2 mousePosition,
        out CityMacroSnapshot.CitizenItem? citizen,
        out Rect2 citizenRect)
    {
        if (_heroCarrier is not null
            && IsVisibleMacroCarrier(_heroCarrier)
            && _citizenStates.TryGetValue(_heroCarrier.Id.Value, out CityMacroSnapshot.CitizenItem? heroState))
        {
            Rect2 heroRect = CitizenHoverRect(_heroCarrier);
            if (heroRect.HasPoint(mousePosition))
            {
                citizen = heroState;
                citizenRect = heroRect;
                return true;
            }
        }

        foreach (CitizenJourney journey in _citizenJourneys.Values)
        {
            if (!IsVisibleMacroCarrier(journey.Carrier)
                || !_citizenStates.TryGetValue(journey.CitizenId.Value, out CityMacroSnapshot.CitizenItem? state))
            {
                continue;
            }
            Rect2 rect = CitizenHoverRect(journey.Carrier);
            if (!rect.HasPoint(mousePosition)) continue;
            citizen = state;
            citizenRect = rect;
            return true;
        }

        citizen = null;
        citizenRect = default;
        return false;
    }

    private static bool IsVisibleMacroCarrier(CitizenSpriteCarrier carrier) =>
        IsInstanceValid(carrier)
        && carrier.Visible
        && carrier.State == CitizenSpriteCarrier.VisualState.Macro;

    private static Rect2 CitizenHoverRect(CitizenSpriteCarrier carrier)
    {
        Vector2 scaledSize = new(
            Mathf.Max(StatusBadgeSize, PresentationConstants.DetailedCitizenWidth * Mathf.Abs(carrier.Scale.X)),
            Mathf.Max(StatusBadgeSize, PresentationConstants.DetailedCitizenHeight * Mathf.Abs(carrier.Scale.Y)));
        return new Rect2(carrier.Position - scaledSize * 0.5f, scaledSize);
    }

    private static IReadOnlyList<WorldStatusBubble.Item> BuildCitizenStatusItems(
        CityMacroSnapshot.CitizenItem citizen)
    {
        var items = new List<WorldStatusBubble.Item>();
        if (citizen.WoundSeverity is WoundSeverity severity)
        {
            string severityLabel = UiText.Get(severity == WoundSeverity.Severe
                ? "ui.wound.severe"
                : "ui.wound.moderate");
            items.Add(new WorldStatusBubble.Item(
                IconPaths.Heart,
                UiText.Format("ui.world_status.wound", severityLabel)));
            if (citizen.IsReceivingWoundTreatment)
            {
                items.Add(new WorldStatusBubble.Item(
                    IconPaths.Clock,
                    UiText.Format(
                        "ui.world_status.treatment",
                        SimulationTimeText.FormatDurationLocalized(citizen.WoundRecoveryTicksRemaining))));
            }
        }

        if (citizen.BlockReason == CitizenRoutineBlockReason.NoFood)
        {
            items.Add(new WorldStatusBubble.Item(
                IconPaths.Warning,
                UiText.Get("ui.world_status.no_food")));
            return items;
        }
        if (items.Count > 0) return items;

        (string icon, string textKey) = citizen.Activity switch
        {
            CitizenRoutineActivity.Working => (IconPaths.Cog, "ui.world_status.working"),
            CitizenRoutineActivity.TravellingToWork or CitizenRoutineActivity.TravellingHome =>
                (IconPaths.User, "ui.world_status.travelling"),
            CitizenRoutineActivity.OnExpedition => (IconPaths.Shield, "ui.world_status.expedition"),
            CitizenRoutineActivity.WaitingForStorage => (IconPaths.Check, "ui.world_status.waiting_storage"),
            CitizenRoutineActivity.WaitingForResources => (IconPaths.Warning, "ui.world_status.waiting_resources"),
            CitizenRoutineActivity.WorkplaceIdle => (IconPaths.Pause, "ui.world_status.work_paused"),
            CitizenRoutineActivity.OffDuty => (IconPaths.Moon, "ui.world_status.off_duty"),
            CitizenRoutineActivity.Resting => (IconPaths.Moon, "ui.world_status.resting"),
            CitizenRoutineActivity.Recovering => (IconPaths.Clock, "ui.world_status.recovering"),
            CitizenRoutineActivity.Leisure => (IconPaths.Moon, "ui.world_status.idle"),
            _ => (IconPaths.Info, "ui.world_status.unavailable"),
        };
        items.Add(new WorldStatusBubble.Item(icon, UiText.Get(textKey)));
        return items;
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
        ClearWorldStatusHover();
        _cultivationActionMenu.Hide();
        NaturalResourceGatherResult availability =
            _controller.GetNaturalResourceGatherAvailability(tree.ForestId, tree.UnitId);
        bool canGather = availability.CanGather;
        string unavailableReason = DescribeGatherBlocker(availability.Outcome);
        // The menu is a sibling child of ScreenContent, not a child of this
        // (possibly zoomed/offset) Node2D — convert the local rect center to
        // global space first, then into ScreenContent's own local space.
        Vector2 menuAnchor = ToGlobal(rect.GetCenter()) - ((Control)GetParent()).GlobalPosition;
        _actionMenu.Open(
            tree.ForestId,
            tree.UnitId,
            tree.ResourceType,
            menuAnchor,
            menuAnchor,
            canGather,
            unavailableReason);
    }

    private void OnGatherRequested(int forestId, int unitId, Vector2 _)
    {
        // A faulty mouse can emit two presses within the same frame. The first
        // request owns the route; accepting the duplicate would restart that
        // route and can make the founder appear never to arrive.
        if (IsDuplicateGatherRequest(_pendingGather, forestId, unitId)) return;
        NaturalResourceGatherResult availability =
            _controller.GetNaturalResourceGatherAvailability(forestId, unitId);
        if (!availability.CanGather)
        {
            Notifier.ShowError(DescribeGatherBlocker(availability.Outcome));
            return;
        }
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
        _route = PlanCitizenRoute(_heroStreet, _heroLateral, target.Value.Street, target.Value.LateralOffset);
        _routeIndex = 0;
    }

    internal static bool IsDuplicateGatherRequest(
        (int ForestId, int UnitId)? pendingGather,
        int forestId,
        int unitId) => pendingGather == (forestId, unitId);

    /// <summary>
    /// One 12 Hz quantized motion step. The founder advances only along an
    /// autonomous route. Manual lateral input always belongs to the camera,
    /// independent of whether follow mode was active before that input.
    /// </summary>
    private void MotionTick(bool allowCameraInput)
    {
        if (_route is not null)
        {
            AdvanceRouteTick();
        }
        else if (_heroWalking && !_depthTarget.HasValue)
        {
            _heroWalking = false;
            _heroCarrier?.Idle(Vector2.Down);
        }
        if (!allowCameraInput) return;
        TryPanCameraLateral();
        ContinueVerticalCameraPan();
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
        if (_heroAmbientRoute)
        {
            _heroAmbientRoute = false;
            _route = null;
            _routeIndex = 0;
            _heroWalking = false;
            _heroNextAmbientDecisionTick = _controller.World.CurrentTick + 30;
            _heroCarrier?.Idle(Vector2.Down);
            return;
        }
        _route = null;
        _routeIndex = 0;
        _heroWalking = false;
        if (_pendingReturnHome)
        {
            _pendingReturnHome = false;
            CitizenId founderId = _controller.World.Hero!.Id;
            BuildingId? homeId = _controller.World.PrimaryHome?.Id;
            bool arrivedHome = _controller.ConfirmCitizenArrivedHome(founderId);
            if (!arrivedHome)
            {
                LogRejectedArrival(founderId, homeId, returningHome: true);
                _heroCarrier?.Idle(Vector2.Down);
                Callable.From(RefreshPlots).CallDeferred();
                return;
            }
            LogCitizenTravel("arrived", founderId, homeId, returningHome: true);
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
            if (!arrived)
            {
                LogRejectedArrival(_controller.World.Hero.Id, workplace, returningHome: false);
                _pendingAssignment = null;
                Callable.From(RefreshPlots).CallDeferred();
            }
            else
            {
                LogCitizenTravel(
                    "arrived",
                    _controller.World.Hero.Id,
                    workplace,
                    returningHome: false);
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
        TreeBox? target = _trees.FirstOrDefault(tree =>
            tree.ForestId == pending.Value.ForestId && tree.UnitId == pending.Value.UnitId);
        NaturalResourceGatherResult gatherResult = _controller.TryGatherFromPatch(
            pending.Value.ForestId,
            pending.Value.UnitId,
            amount: 2);
        if (gatherResult.IsSuccess && target is TreeBox gatheredTarget)
        {
            ResourceFeedbackAnchor anchor = ResolveFoundingStoragePopupAnchor();
            if (anchor.FollowTarget is Node2D followTarget)
            {
                ResourceGainPopup.ShowGainFollowing(
                    this,
                    followTarget,
                    gatheredTarget.ResourceType,
                    gatherResult.GatheredAmount,
                    anchor.FollowOffset);
            }
            else
            {
                ResourceGainPopup.ShowGain(
                    this,
                    gatheredTarget.ResourceType,
                    gatherResult.GatheredAmount,
                    anchor.Position);
            }
        }
        else
        {
            Notifier.ShowError(DescribeGatherBlocker(gatherResult.Outcome));
        }
        GetTree().CreateTimer(0.6).Timeout += () =>
        {
            if (IsInstanceValid(this) && _route is null && IsInstanceValid(_heroCarrier))
            {
                _heroCarrier?.Idle(Vector2.Up);
            }
        };
    }

    /// <summary>
    /// Resource feedback belongs to the storage destination, not the citizen.
    /// A completed Shelter wins; an incomplete Founding Site becomes storage
    /// only after its Cache module exists. Before that the founder physically
    /// carries the six-unit load, so feedback truthfully follows the citizen.
    /// </summary>
    private ResourceFeedbackAnchor ResolveFoundingStoragePopupAnchor()
    {
        PlotBox? storagePlot = null;
        foreach (PlotBox plot in _plots)
        {
            if (plot.Kind != BuildingKind.Home) continue;
            if (!plot.IsUnderConstruction)
            {
                storagePlot = plot;
                break;
            }
        }
        if (storagePlot is null)
        {
            foreach (ConstructionProject project in _controller.World.Projects.Values)
            {
                if (project.Kind != ConstructionKind.FoundingSite
                    || !project.HasCompletedFoundingModule(FoundingSiteModule.Cache))
                {
                    continue;
                }
                foreach (PlotBox plot in _plots)
                {
                    if (plot.BuildingId != project.Id.Value) continue;
                    storagePlot = plot;
                    break;
                }
                break;
            }
        }
        if (storagePlot is PlotBox plotBox)
        {
            float depth = AnchorDepth(plotBox.Street - CameraDepthAnchor);
            float lateral = plotBox.LateralOffset - CameraLateral;
            (Vector2 position, Vector2 scale) = ProjectDepth(depth, lateral);
            return new ResourceFeedbackAnchor(
                PixelMotion.Snap(new Vector2(
                    position.X,
                    position.Y - plotBox.Height * scale.Y - 12f)),
                null,
                Vector2.Zero);
        }

        if (IsInstanceValid(_heroCarrier))
        {
            return new ResourceFeedbackAnchor(
                Vector2.Zero,
                _heroCarrier,
                Vector2.Up * 72f);
        }

        int street = FoundingLayout.InitialParcelRow * ParcelGrid.ConstructionRowsPerParcel
            + FoundingLayout.FounderRowWithinParcel;
        float totalFrontageColumns = _worldParcelColumns * ParcelGrid.FrontageColumnsPerParcel;
        float frontageCenter = FoundingLayout.InitialParcelColumn
            * ParcelGrid.FrontageColumnsPerParcel
            + FoundingLayout.FounderFrontageColumnWithinParcel
            + 0.5f;
        float lateralOffset = (frontageCenter - totalFrontageColumns * 0.5f) * TileUnitPx;
        (Vector2 fallback, _) = ProjectDepth(
            AnchorDepth(street - CameraDepthAnchor),
            lateralOffset - CameraLateral);
        return new ResourceFeedbackAnchor(
            PixelMotion.Snap(fallback + Vector2.Up * 48f),
            null,
            Vector2.Zero);
    }

    private static string DescribeGatherBlocker(NaturalResourceGatherOutcome outcome) =>
        UiText.Get(outcome switch
        {
            NaturalResourceGatherOutcome.HeroUnavailable =>
                "ui.gather.founder_unavailable",
            NaturalResourceGatherOutcome.StorageFull =>
                "ui.gather.storage_full",
            NaturalResourceGatherOutcome.MissingRequiredTool =>
                "ui.gather.axe_required",
            NaturalResourceGatherOutcome.NodeUnavailable =>
                "This resource node is no longer available.",
            _ => "ui.gather.action_unavailable",
        });

    /// <summary>
    /// Manual depth input always pans the camera. If follow mode is active,
    /// the first manual step releases it before moving the observer.
    /// </summary>
    private void PanCameraStreet(int direction)
    {
        EnsureFreeCameraForManualPan();
        StepFreeCameraStreet(direction);
    }

    private void BeginVerticalCameraPan(int direction)
    {
        if (_verticalPanDirection == direction) return;
        _verticalPanDirection = direction;
        _verticalPanHoldSeconds = 0f;
        _verticalPanRepeatAccumulator = 0f;
        PanCameraStreet(direction);
    }

    private void ContinueVerticalCameraPan()
    {
        int direction = ReadVerticalDirection();
        if (direction == 0)
        {
            ResetVerticalCameraPanHold();
            return;
        }
        if (direction != _verticalPanDirection)
        {
            BeginVerticalCameraPan(direction);
            return;
        }

        _verticalPanHoldSeconds += PixelMotion.CadenceSeconds;
        _verticalPanRepeatAccumulator += PixelMotion.CadenceSeconds;
        float repeatSeconds = VerticalPanRepeatSeconds(_verticalPanHoldSeconds);
        if (_verticalPanRepeatAccumulator < repeatSeconds) return;
        _verticalPanRepeatAccumulator -= repeatSeconds;
        PanCameraStreet(direction);
    }

    private void ResetVerticalCameraPanHold()
    {
        _verticalPanDirection = 0;
        _verticalPanHoldSeconds = 0f;
        _verticalPanRepeatAccumulator = 0f;
    }

    internal static float VerticalPanRepeatSeconds(float holdSeconds)
    {
        float progress = VerticalPanAccelerationProgress(holdSeconds);
        return Mathf.Lerp(
            VerticalPanInitialRepeatSeconds,
            VerticalPanMinimumRepeatSeconds,
            progress);
    }

    internal static float VerticalPanTransitionMultiplier(float holdSeconds)
    {
        float progress = VerticalPanAccelerationProgress(holdSeconds);
        return Mathf.Lerp(1f, VerticalPanMaximumTransitionMultiplier, progress);
    }

    private static float VerticalPanAccelerationProgress(float holdSeconds)
    {
        float linear = Mathf.Clamp(holdSeconds / VerticalPanAccelerationSeconds, 0f, 1f);
        return linear * linear * (3f - 2f * linear);
    }

    /// <summary>
    /// Called before a founder gather route, which this class does not learn
    /// about through a synchronous domain travel event. An assignment is
    /// different — <c>TryAssignCitizen</c> fires
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
    /// <see cref="CompleteRoute"/>'s gather branch calls
    /// <c>TryGatherFromPatch</c>
    /// (itself a synchronous <c>BuildingStateChanged</c>) the instant after
    /// <see cref="_route"/> is cleared, undoing the arrival Slash animation
    /// before the player ever sees it.</description></item>
    /// </list>
    /// <see cref="_heroIsGatheringOutsideHome"/> is cleared once a real
    /// domain-tracked journey takes over (<see cref="BeginWalkToAssignment"/>/
    /// <see cref="BeginWalkHome"/>/departing on an expedition).
    /// </summary>
    private void EnsureHeroCarrierReadyToMove()
    {
        _heroIsGatheringOutsideHome = true;
        if (_heroCarrier is null) return;
        if (_heroCarrier.State != CitizenSpriteCarrier.VisualState.Macro)
        {
            _heroCarrier.SetState(CitizenSpriteCarrier.VisualState.Macro);
        }
        _heroCarrier.CancelMotion();
    }

    /// <summary>
    /// Free camera's own manual depth step — an observer, not a body, so
    /// it never checks citizen obstacle clearance (design bible §04: free
    /// pan is always available).
    /// </summary>
    private void StepFreeCameraStreet(int direction)
    {
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
    private static void AdvanceTransition(
        ref float anchor,
        ref float? target,
        ref float accumulator,
        double delta,
        float stepSize = DepthStepSize)
    {
        if (!target.HasValue) return;
        accumulator += (float)delta;
        while (accumulator >= PixelMotion.CadenceSeconds && target.HasValue)
        {
            accumulator -= PixelMotion.CadenceSeconds;
            float value = target.Value;
            if (Mathf.Abs(value - anchor) <= stepSize)
            {
                anchor = value;
                target = null;
            }
            else
            {
                anchor += Mathf.Sign(value - anchor) * stepSize;
            }
        }
    }

    /// <summary>Manual lateral input pans only the camera.</summary>
    private bool TryPanCameraLateral()
    {
        float direction = ReadLateralDirection();
        if (direction == 0f) return false;
        EnsureFreeCameraForManualPan();
        float next = Mathf.Clamp(
            _freeCameraLateral + direction * PixelMotion.StepPixels,
            -_lateralHalfWidthPx,
            _lateralHalfWidthPx);
        if (next == _freeCameraLateral) return false;
        _freeCameraLateral = next;
        QueueRedraw();
        return true;
    }

    private void EnsureFreeCameraForManualPan()
    {
        if (_cameraFollowsHero) SetCameraFollowsHero(false);
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
        if (Input.IsActionPressed(CameraInputActions.PanLeft)) return -1f;
        if (Input.IsActionPressed(CameraInputActions.PanRight)) return 1f;
        return 0f;
    }

    private static int ReadVerticalDirection()
    {
        if (Input.IsActionPressed(CameraInputActions.PanUp)) return 1;
        if (Input.IsActionPressed(CameraInputActions.PanDown)) return -1;
        return 0;
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
        if (snapshot.Hero is not { } hero) return;
        CityMacroSnapshot.CitizenItem? heroState = snapshot.Citizens
            .FirstOrDefault(citizen => citizen.Id == hero.Id);
        if (heroState is null) return;
        if (heroState.IsOnExpedition)
        {
            if (CitizenSpriteBank.Instance.TryGet(hero.Id, out CitizenSpriteCarrier? awayCarrier)
                && awayCarrier is not null
                && awayCarrier.GetParent() == this)
            {
                awayCarrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
            }
            _lastKnownAssignment = null;
            _lastKnownHeroLocation = null;
            _heroIsGatheringOutsideHome = false;
            return;
        }
        if (!Visible && heroState.Location != CitizenLocation.InTransit && _route is null) return;
        _heroCarrier = CitizenSpriteBank.Instance.GetOrCreate(
            hero.Id, hero.Lineage, hero.Gender, hero.Appearance);
        CitizenSpriteBank.Instance.Mount(_heroCarrier, this);
        // The city sits on the site the founder's fall reached. Resolved here
        // because this is where the founder first becomes known to the view;
        // it changes nothing mechanical, only which ground tiles are drawn.
        _groundBiome = TerrainAtlas.BiomeFor(hero.Lineage);
        if (!_heroPositionInitialized)
        {
            _heroStreet = FoundingLayout.InitialParcelRow
                * ParcelGrid.ConstructionRowsPerParcel
                + FoundingLayout.FounderRowWithinParcel;
            float totalFrontageColumns =
                _worldParcelColumns * ParcelGrid.FrontageColumnsPerParcel;
            float frontageCenter = FoundingLayout.InitialParcelColumn
                * ParcelGrid.FrontageColumnsPerParcel
                + FoundingLayout.FounderFrontageColumnWithinParcel
                + 0.5f;
            _heroLateral = (frontageCenter - totalFrontageColumns * 0.5f) * TileUnitPx;
            _depthAnchor = _heroStreet;
            _heroPositionInitialized = true;
        }
        BuildingId? currentAssignment = heroState.CurrentAssignment;
        CitizenLocation heroLocation = heroState.Location;
        if (heroLocation == CitizenLocation.InTransit && _heroAmbientRoute)
        {
            _heroAmbientRoute = false;
            _route = null;
            _routeIndex = 0;
            _heroWalking = false;
        }
        bool hasShelter = snapshot.Buildings.Any(building =>
            building.Kind == BuildingKind.Home && !building.IsUnderConstruction);
        bool mayWander = CanWander(heroState.Activity);
        bool shouldRemainVisibleAtHome = mayWander
            || heroState.Activity == CitizenRoutineActivity.Recovering;
        if (ShouldHideHeroInsideShelter(
            currentAssignment,
            heroLocation,
            hasShelter,
            hasRoute: _route is not null,
            pendingReturnHome: _pendingReturnHome,
            isGatheringOutsideHome: _heroIsGatheringOutsideHome)
            && !shouldRemainVisibleAtHome)
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

        if (heroLocation == CitizenLocation.AtHome && _route is null)
        {
            PlotBox? shelter = FindHomePlot();
            if (shelter is { } home
                && (_lastKnownHeroLocation != CitizenLocation.AtHome
                    || heroState.Activity == CitizenRoutineActivity.Recovering))
            {
                BuildingVisualAnchors anchors = VisualAnchorsFor(home);
                StreetVisualAnchor anchor = heroState.Activity == CitizenRoutineActivity.Recovering
                    ? anchors.Waiting
                    : anchors.Entrance;
                _heroStreet = anchor.Street;
                _heroLateral = anchor.Lateral;
                _depthAnchor = _heroStreet;
                _depthTarget = null;
            }
            if (mayWander && shelter is { } wanderAnchor)
            {
                TryStartHeroAmbientRoute(wanderAnchor);
            }
        }

        if (currentAssignment != _lastKnownAssignment)
        {
            _lastKnownAssignment = currentAssignment;
        }
        if (currentAssignment.HasValue
            && heroLocation == CitizenLocation.InTransit
            && heroState.IsReturningHome
            && _route is null
            && !_pendingReturnHome)
        {
            BeginWalkHome(heroState.TransitStartedAtTick);
        }
        else if (ShouldBeginWorkRoute(
                currentAssignment,
                heroState.Location,
                heroState.IsReturningHome,
                hasRoute: _route is not null)
            && currentAssignment is BuildingId unsettledWorkplace
            && _route is null)
        {
            // A view transition can replace the flyweight carrier's previous
            // movement callback. If the domain still says InTransit after the
            // visual route disappeared, resume/reconcile instead of leaving
            // the citizen permanently assigned but non-productive.
            BeginWalkToAssignment(unsettledWorkplace, heroState.TransitStartedAtTick);
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

    internal static bool ShouldBeginWorkRoute(
        BuildingId? currentAssignment,
        CitizenLocation location,
        bool isReturningHome,
        bool hasRoute) =>
        currentAssignment.HasValue
        && location == CitizenLocation.InTransit
        && !isReturningHome
        && !hasRoute;

    internal static bool ShouldHideHeroInsideShelter(
        BuildingId? currentAssignment,
        CitizenLocation location,
        bool hasShelter,
        bool hasRoute,
        bool pendingReturnHome,
        bool isGatheringOutsideHome = false) =>
        currentAssignment is null
        && location == CitizenLocation.AtHome
        && hasShelter
        && !hasRoute
        && !pendingReturnHome
        && !isGatheringOutsideHome;

    /// <summary>
    /// Mirror of <see cref="ShouldHideHeroInsideShelter"/> for non-founder
    /// citizens at home. The founder gets the same hiding rule via the
    /// dedicated founder-carrier path; this helper exists so every
    /// regular citizen (Resting / OffDuty / etc.) is hidden from the
    /// macro map when they are physically inside the Home and not
    /// actively wandering or recovering — leaving them on the entrance
    /// anchor used to make them reappear at the building's front as
    /// soon as the player closed the detail view, while the detail
    /// view itself kept showing the same citizens inside.
    /// </summary>
    internal static bool ShouldHideCitizenAtHome(CitizenLocation location, CitizenRoutineActivity activity) =>
        location == CitizenLocation.AtHome
        && !CanWander(activity)
        && activity != CitizenRoutineActivity.Recovering;

    /// <summary>
    /// Resolves the physical destination of a domain-tracked journey. This
    /// rule deliberately has no founder/non-founder branch: every citizen
    /// in transit travels to the assignment, or back to the shared home.
    /// </summary>
    internal static BuildingId? ResolveTravelDestination(
        CitizenLocation location,
        bool isReturningHome,
        BuildingId? currentAssignment,
        BuildingId? homeBuildingId)
    {
        if (location != CitizenLocation.InTransit) return null;
        return isReturningHome ? homeBuildingId : currentAssignment;
    }

    internal static bool CanWander(CitizenRoutineActivity activity) => activity is
        CitizenRoutineActivity.Leisure
        or CitizenRoutineActivity.WaitingForStorage
        or CitizenRoutineActivity.WaitingForResources
        or CitizenRoutineActivity.WorkplaceIdle;

    /// <summary>
    /// Reconciles every non-founder citizen with one real street journey.
    /// InTransit never means "place at destination": it means plan/continue
    /// the same obstacle-aware route used by the founder and confirm domain
    /// arrival only after the final waypoint is physically reached.
    /// </summary>
    private void RefreshCitizenVisuals(CityMacroSnapshot snapshot)
    {
        PlotBox? homePlot = FindHomePlot();
        CitizenId? founderId = snapshot.Hero?.Id;
        var activeCitizenIds = new HashSet<int>();
        foreach (CityMacroSnapshot.CitizenItem citizen in snapshot.Citizens)
        {
            // Only the founding hero owns the dedicated founder carrier path.
            // A later citizen who earns RoleId.Hero remains an ordinary
            // citizen for work travel and must not disappear from this loop.
            if (citizen.Id == founderId) continue;
            activeCitizenIds.Add(citizen.Id.Value);
            if (!Visible
                && citizen.Location != CitizenLocation.InTransit
                && (!_citizenJourneys.TryGetValue(citizen.Id.Value, out CitizenJourney? existing)
                    || existing.Route is null))
            {
                continue;
            }
            ReconcileCitizenJourney(citizen, homePlot);
        }

        List<int>? staleCitizenIds = null;
        foreach (int citizenId in _citizenJourneys.Keys)
        {
            if (activeCitizenIds.Contains(citizenId)) continue;
            (staleCitizenIds ??= new List<int>()).Add(citizenId);
        }
        if (staleCitizenIds is not null)
        {
            foreach (int citizenId in staleCitizenIds)
            {
                CitizenJourney journey = _citizenJourneys[citizenId];
                journey.Carrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
                _citizenJourneys.Remove(citizenId);
            }
        }
        UpdateCitizenJourneyVisuals();
    }

    private void ReconcileCitizenJourney(
        CityMacroSnapshot.CitizenItem citizen,
        PlotBox? homePlot)
    {
        if (citizen.IsOnExpedition)
        {
            if (_citizenJourneys.TryGetValue(citizen.Id.Value, out CitizenJourney? away))
            {
                StopJourney(away);
                away.Carrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
            }
            return;
        }

        PlotBox? assignmentPlot = citizen.CurrentAssignment is BuildingId assignment
            ? FindPlot(assignment)
            : null;
        CitizenJourney journey = GetOrCreateCitizenJourney(citizen, homePlot, assignmentPlot);

        if (citizen.Location == CitizenLocation.AtWork)
        {
            StopJourney(journey);
            if (assignmentPlot is { } workplace)
            {
                SetJourneyPosition(journey, workplace);
            }
            journey.Carrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
            return;
        }

        if (citizen.Location == CitizenLocation.AtHome)
        {
            if (CanWander(citizen.Activity))
            {
                if (journey.Route is null
                    && _controller.World.CurrentTick >= journey.NextAmbientDecisionTick
                    && homePlot is { } ambientAnchor)
                {
                    StartAmbientJourney(journey, ambientAnchor, _controller.World.CurrentTick);
                }
                ShowJourneyCarrier(journey, Vector2.Up);
                return;
            }
            StopJourney(journey);
            // Recovering citizens stay visible at the Waiting anchor so
            // the player can see who is being treated; every other
            // at-home activity (Resting, OffDuty, etc.) follows the
            // same hide-inside-the-home rule the founder uses via
            // ShouldHideHeroInsideShelter. Without this, leaving the
            // building detail would snap every resting citizen back to
            // the home's entrance anchor — visibly outside while the
            // detail panel had just shown them inside.
            if (citizen.Activity == CitizenRoutineActivity.Recovering && homePlot is { } home)
            {
                SetJourneyPosition(journey, home, citizen.Activity);
                ShowJourneyCarrier(journey, Vector2.Up);
            }
            else
            {
                journey.Carrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
            }
            return;
        }

        BuildingId? destinationId = ResolveTravelDestination(
            citizen.Location,
            citizen.IsReturningHome,
            citizen.CurrentAssignment,
            homePlot is { } homeBuilding ? new BuildingId(homeBuilding.BuildingId) : null);
        PlotBox? destination = destinationId is BuildingId id ? FindPlot(id) : null;
        if (destination is not { } target)
        {
            GD.PushWarning(
                $"Citizen travel unresolved: citizen={citizen.Id.Value}, assignment={citizen.CurrentAssignment?.Value}, " +
                $"returningHome={citizen.IsReturningHome}, tick={_controller.World.CurrentTick}.");
            journey.Carrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
            return;
        }

        bool sameJourney = journey.Route is not null
            && journey.Destination == new BuildingId(target.BuildingId)
            && journey.ReturningHome == citizen.IsReturningHome;
        if (!sameJourney)
        {
            StartCitizenJourney(
                journey,
                new BuildingId(target.BuildingId),
                target,
                citizen.IsReturningHome,
                citizen.TransitStartedAtTick,
                _controller.World.CurrentTick);
        }
        ShowJourneyCarrier(journey, Vector2.Up);
    }

    private CitizenJourney GetOrCreateCitizenJourney(
        CityMacroSnapshot.CitizenItem citizen,
        PlotBox? homePlot,
        PlotBox? assignmentPlot)
    {
        if (_citizenJourneys.TryGetValue(citizen.Id.Value, out CitizenJourney? existing))
        {
            return existing;
        }
        PlotBox? origin = citizen.Location == CitizenLocation.AtWork
            || (citizen.Location == CitizenLocation.InTransit && citizen.IsReturningHome)
                ? assignmentPlot
                : homePlot;
        int street = origin is { } plot ? WorkplaceEntranceStreet(plot.Street) : 0;
        float lateral = origin?.LateralOffset ?? 0f;
        CitizenSpriteCarrier carrier = CitizenSpriteBank.Instance.GetOrCreate(
            citizen.Id, citizen.Lineage, citizen.Gender, citizen.Appearance);
        var created = new CitizenJourney(citizen.Id, carrier, street, lateral);
        _citizenJourneys.Add(citizen.Id.Value, created);
        return created;
    }

    private void StartCitizenJourney(
        CitizenJourney journey,
        BuildingId destination,
        PlotBox target,
        bool returningHome,
        int? transitStartedAtTick,
        int currentTick)
    {
        journey.Carrier.CancelMotion();
        journey.Destination = destination;
        journey.ReturningHome = returningHome;
        journey.IsAmbient = false;
        BuildingVisualAnchors anchors = VisualAnchorsFor(target);
        journey.Route = PlanCitizenRoute(
            journey.Street,
            journey.Lateral,
            anchors.Entrance.Street,
            anchors.Entrance.Lateral);
        journey.RouteIndex = 0;
        journey.Walking = false;
        if (transitStartedAtTick is int startedAt && currentTick > startedAt)
        {
            ReconstructedRoutePosition reconstructed = ReconstructRouteProgress(
                journey.Route,
                journey.Street,
                journey.Lateral,
                currentTick - startedAt,
                CityEconomyRules.AbstractTravelTicks);
            journey.Street = reconstructed.Street;
            journey.Lateral = reconstructed.Lateral;
            journey.DepthAnchor = reconstructed.Street;
            journey.RouteIndex = reconstructed.RouteIndex;
        }
        LogCitizenTravel("started", journey.CitizenId, destination, returningHome);
    }

    /// <summary>
    /// Rebuilds a presentation position from semantic transit timing. The
    /// result is ephemeral and never enters WorldSave; it only prevents a load
    /// or view re-entry from replaying the already elapsed part of a journey.
    /// </summary>
    internal static ReconstructedRoutePosition ReconstructRouteProgress(
        IReadOnlyList<StreetRoutePlanner.Waypoint> route,
        int startStreet,
        float startLateral,
        int elapsedTicks,
        int expectedDurationTicks)
    {
        if (route.Count == 0 || elapsedTicks <= 0 || expectedDurationTicks <= 0)
        {
            return new ReconstructedRoutePosition(startStreet, startLateral, 0);
        }

        int totalSteps = CountRouteSteps(route, startStreet, startLateral);
        int stepsToApply = Math.Min(
            Math.Max(0, totalSteps - 1),
            (int)Math.Floor(totalSteps * Math.Min(1d, (double)elapsedTicks / expectedDurationTicks)));
        int street = startStreet;
        float lateral = startLateral;
        int routeIndex = 0;
        for (int step = 0; step < stepsToApply && routeIndex < route.Count; step++)
        {
            AdvanceReconstructedRouteStep(route, ref street, ref lateral, ref routeIndex);
        }
        return new ReconstructedRoutePosition(street, lateral, routeIndex);
    }

    private static int CountRouteSteps(
        IReadOnlyList<StreetRoutePlanner.Waypoint> route,
        int startStreet,
        float startLateral)
    {
        int street = startStreet;
        float lateral = startLateral;
        int routeIndex = 0;
        int steps = 0;
        while (routeIndex < route.Count)
        {
            AdvanceReconstructedRouteStep(route, ref street, ref lateral, ref routeIndex);
            steps++;
        }
        return steps;
    }

    private static void AdvanceReconstructedRouteStep(
        IReadOnlyList<StreetRoutePlanner.Waypoint> route,
        ref int street,
        ref float lateral,
        ref int routeIndex)
    {
        StreetRoutePlanner.Waypoint waypoint = route[routeIndex];
        if (waypoint.Street != street)
        {
            street += Math.Sign(waypoint.Street - street);
            return;
        }
        if (Mathf.Abs(waypoint.Lateral - lateral) >= 1f)
        {
            lateral = Mathf.MoveToward(lateral, waypoint.Lateral, PixelMotion.StepPixels);
            return;
        }
        routeIndex++;
    }

    private void AdvanceCitizenJourneysTick()
    {
        bool advancedAnyJourney = false;
        foreach ((int citizenId, CitizenJourney journey) in _citizenJourneys.ToArray())
        {
            if (journey.Route is null || journey.DepthTarget.HasValue) continue;
            advancedAnyJourney = true;
            if (journey.RouteIndex >= journey.Route.Count)
            {
                if (journey.IsAmbient) CompleteAmbientJourney(journey);
                else CompleteCitizenJourney(new CitizenId(citizenId), journey);
                continue;
            }
            StreetRoutePlanner.Waypoint waypoint = journey.Route[journey.RouteIndex];
            if (waypoint.Street != journey.Street)
            {
                int direction = Math.Sign(waypoint.Street - journey.Street);
                journey.Carrier.Walk(direction > 0 ? Vector2.Up : Vector2.Down);
                journey.Walking = true;
                journey.Street += direction;
                journey.DepthTarget = journey.Street;
                continue;
            }
            if (Mathf.Abs(waypoint.Lateral - journey.Lateral) >= 1f)
            {
                float direction = Mathf.Sign(waypoint.Lateral - journey.Lateral);
                journey.Carrier.Walk(direction > 0f ? Vector2.Right : Vector2.Left);
                journey.Walking = true;
                journey.Lateral = Mathf.MoveToward(
                    journey.Lateral,
                    waypoint.Lateral,
                    PixelMotion.StepPixels);
                continue;
            }
            journey.RouteIndex++;
            if (journey.RouteIndex >= journey.Route.Count)
            {
                if (journey.IsAmbient) CompleteAmbientJourney(journey);
                else CompleteCitizenJourney(new CitizenId(citizenId), journey);
            }
        }
        if (advancedAnyJourney) QueueRedraw();
    }

    private void CompleteCitizenJourney(CitizenId citizenId, CitizenJourney journey)
    {
        BuildingId? destination = journey.Destination;
        bool returningHome = journey.ReturningHome;
        StopJourney(journey);
        bool confirmed = returningHome
            ? _controller.ConfirmCitizenArrivedHome(citizenId)
            : destination is BuildingId assignment
                && _controller.ConfirmCitizenArrivedAtAssignment(assignment, citizenId);
        if (!confirmed)
        {
            LogRejectedArrival(citizenId, destination, returningHome);
            ReconcileRejectedArrival(citizenId, journey);
            return;
        }
        LogCitizenTravel("arrived", citizenId, destination, returningHome);
        if (returningHome)
        {
            ShowJourneyCarrier(journey, Vector2.Up);
        }
        else
        {
            journey.Carrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
        }
    }

    private void StartAmbientJourney(CitizenJourney journey, PlotBox anchor, int currentTick)
    {
        int phase = Math.Abs(journey.CitizenId.Value * 31 + currentTick / 30);
        int streetDelta = phase % 3 - 1;
        BuildingVisualAnchors anchors = VisualAnchorsFor(anchor);
        StreetVisualAnchor leisure = (phase / 3) % 2 == 0
            ? anchors.LeisureLeft
            : anchors.LeisureRight;
        int targetStreet = Mathf.Clamp(
            leisure.Street + streetDelta,
            0,
            _streetCount - 1);
        float targetLateral = leisure.Lateral;
        journey.Route = PlanCitizenRoute(
            journey.Street,
            journey.Lateral,
            targetStreet,
            targetLateral);
        journey.RouteIndex = 0;
        journey.Destination = null;
        journey.ReturningHome = false;
        journey.IsAmbient = true;
        journey.Walking = false;
        journey.NextAmbientDecisionTick = currentTick + 20 + phase % 31;
    }

    private void CompleteAmbientJourney(CitizenJourney journey)
    {
        journey.Route = null;
        journey.RouteIndex = 0;
        journey.IsAmbient = false;
        journey.Walking = false;
        journey.DepthTarget = null;
        journey.NextAmbientDecisionTick = _controller.World.CurrentTick + 30;
        journey.Carrier.Idle(Vector2.Down);
    }

    private void ReconcileRejectedArrival(CitizenId citizenId, CitizenJourney journey)
    {
        Citizen? citizen = _controller.World.GetCitizen(citizenId);
        if (citizen is null)
        {
            journey.Carrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
            return;
        }
        Callable.From(RefreshPlots).CallDeferred();
    }

    private void AdvanceJourneyTransition(CitizenJourney journey, double delta)
    {
        float anchor = journey.DepthAnchor;
        float? target = journey.DepthTarget;
        float accumulator = journey.TransitionAccumulator;
        AdvanceTransition(ref anchor, ref target, ref accumulator, delta);
        journey.DepthAnchor = anchor;
        journey.DepthTarget = target;
        journey.TransitionAccumulator = accumulator;
    }

    private void UpdateCitizenJourneyVisuals()
    {
        foreach (CitizenJourney journey in _citizenJourneys.Values)
        {
            if (!IsInstanceValid(journey.Carrier)
                || journey.Carrier.State != CitizenSpriteCarrier.VisualState.Macro)
            {
                continue;
            }
            float depth = journey.DepthAnchor - CameraDepthAnchor;
            journey.Carrier.Visible = IsProjectedDepthVisible(depth);
            if (!journey.Carrier.Visible) continue;
            float relativeOffset = journey.Lateral - CameraLateral;
            (Vector2 position, Vector2 scale) = ProjectDepth(depth, relativeOffset);
            journey.Carrier.Scale =
                CitizenSpriteCarrier.ScaleForState(CitizenSpriteCarrier.VisualState.Macro) * scale;
            journey.Carrier.ZIndex = CitizenZ(depth);
            journey.Carrier.Position = PixelMotion.Snap(new Vector2(
                position.X,
                position.Y - HeroFootOffsetMacroPx * scale.Y));
        }
    }

    /// <summary>
    /// Same hit-rect contract used for trees and buildings: every visible
    /// macro citizen becomes a clickable rect on the very same frame the
    /// carrier's Perspective-Projected position is written. The rect is the
    /// shallow hover box (max(scaledSize, StatusBadgeSize)) so it never
    /// out-grows the rendered sprite — citizens need a real left-click path
    /// to surface the same at-a-glance summary trees and buildings already
    /// get from <see cref="SelectionInfoPanel"/>.
    /// </summary>
    private void UpdateCitizenHitRects()
    {
        if (_heroCarrier is not null && IsVisibleMacroCarrier(_heroCarrier))
        {
            _clickableCitizenRects.Add((CitizenHoverRect(_heroCarrier), _heroCarrier.Id));
        }
        foreach (CitizenJourney journey in _citizenJourneys.Values)
        {
            if (!IsVisibleMacroCarrier(journey.Carrier)) continue;
            _clickableCitizenRects.Add((CitizenHoverRect(journey.Carrier), journey.CitizenId));
        }
    }

    private void ShowJourneyCarrier(CitizenJourney journey, Vector2 facing)
    {
        CitizenSpriteBank.Instance.Mount(journey.Carrier, this);
        journey.Carrier.CancelMotion();
        journey.Carrier.SetState(CitizenSpriteCarrier.VisualState.Macro);
        if (!journey.Walking) journey.Carrier.Idle(facing);
    }

    private static void StopJourney(CitizenJourney journey)
    {
        journey.Route = null;
        journey.RouteIndex = 0;
        journey.Destination = null;
        journey.ReturningHome = false;
        journey.IsAmbient = false;
        journey.DepthTarget = null;
        journey.Walking = false;
        journey.Carrier.CancelMotion();
    }

    private void SetJourneyPosition(
        CitizenJourney journey,
        PlotBox plot,
        CitizenRoutineActivity activity = CitizenRoutineActivity.Unavailable)
    {
        BuildingVisualAnchors anchors = VisualAnchorsFor(plot);
        StreetVisualAnchor anchor = activity == CitizenRoutineActivity.Recovering
            ? anchors.Waiting
            : anchors.Entrance;
        journey.Street = anchor.Street;
        journey.Lateral = anchor.Lateral;
        journey.DepthAnchor = journey.Street;
        journey.DepthTarget = null;
    }

    private PlotBox? FindHomePlot()
    {
        foreach (PlotBox plot in _plots)
        {
            if (plot.Kind == BuildingKind.Home && !plot.IsUnderConstruction) return plot;
        }
        return null;
    }

    private PlotBox? FindPlot(BuildingId buildingId)
    {
        foreach (PlotBox plot in _plots)
        {
            if (plot.BuildingId == buildingId.Value) return plot;
        }
        return null;
    }

    private BuildingVisualAnchors VisualAnchorsFor(PlotBox plot) =>
        BuildingVisualAnchors.FromPlacement(
            WorkplaceEntranceStreet(plot.Street),
            plot.LateralOffset,
            _streetCount,
            _lateralHalfWidthPx,
            PixelMotion.StepPixels);

    private void LogRejectedArrival(
        CitizenId citizenId,
        BuildingId? destination,
        bool returningHome)
    {
        Citizen? citizen = _controller.World.GetCitizen(citizenId);
        CitizenDebugSnapshot? debug = _controller.GetCitizenDebugSnapshot(citizenId);
        GD.PushWarning(
            $"Citizen arrival rejected: citizen={citizenId.Value}, destination={destination?.Value}, " +
            $"returningHome={returningHome}, tick={_controller.World.CurrentTick}, " +
            $"daytime={GameClock.IsDaytime(_controller.World.CurrentTick)}, " +
            $"location={citizen?.CurrentLocation}, assignment={citizen?.CurrentAssignment?.Value}, " +
            $"activity={debug?.Routine.Activity}, blocker={debug?.Routine.BlockReason}, " +
            $"started={debug?.Routine.ActivityStartedAtTick}, expected={debug?.Routine.ExpectedCompletionTick}, " +
            $"next={debug?.Routine.NextTransitionTick}.");
    }

    private void LogCitizenTravel(
        string phase,
        CitizenId citizenId,
        BuildingId? destination,
        bool returningHome)
    {
        if (!OS.IsDebugBuild()) return;
        CitizenDebugSnapshot? debug = _controller.GetCitizenDebugSnapshot(citizenId);
        GD.Print(
            $"[CitizenTravel] {phase}: citizen={citizenId.Value}, destination={destination?.Value}, " +
            $"returningHome={returningHome}, tick={_controller.World.CurrentTick}, " +
            $"activity={debug?.Routine.Activity}, context={debug?.Routine.ContextLocation}, " +
            $"blocker={debug?.Routine.BlockReason}, started={debug?.Routine.ActivityStartedAtTick}, " +
            $"expected={debug?.Routine.ExpectedCompletionTick}, next={debug?.Routine.NextTransitionTick}.");
    }
    /// <summary>
    /// Routes the hero from wherever they currently are to their new
    /// workplace's calle/lateral, reusing the same quantized
    /// <see cref="StreetRoutePlanner"/>/<see cref="_route"/> machinery as
    /// gather. Once the route completes, <see cref="CompleteRoute"/> just
    /// settles them into an idle "at work" pose instead of gathering wood.
    /// </summary>
    private void BeginWalkToAssignment(BuildingId workplace, int? transitStartedAtTick = null)
    {
        _heroAmbientRoute = false;
        _heroIsGatheringOutsideHome = false;
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
            GD.PushWarning(
                $"Citizen route target missing: citizen={_controller.World.Hero!.Id.Value}, " +
                $"assignment={workplace.Value}, tick={_controller.World.CurrentTick}.");
            return;
        }
        _pendingGather = null;
        _pendingReturnHome = false;
        _pendingAssignment = workplace;
        int entranceStreet = WorkplaceEntranceStreet(target.Value.Street);
        _route = PlanCitizenRoute(_heroStreet, _heroLateral, entranceStreet, target.Value.LateralOffset);
        _routeIndex = 0;
        ReconstructHeroRouteIfElapsed(transitStartedAtTick);
        LogCitizenTravel(
            "started",
            _controller.World.Hero!.Id,
            workplace,
            returningHome: false);
    }

    /// <summary>
    /// Gives a released founder a concrete next intention. Assignment is a
    /// domain concern; this route is its visual consequence, so the citizen
    /// walks back to the Shelter instead of freezing wherever the previous
    /// workplace route was cancelled.
    /// </summary>
    private void BeginWalkHome(int? transitStartedAtTick = null)
    {
        _heroAmbientRoute = false;
        _heroIsGatheringOutsideHome = false;
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
            GD.PushWarning(
                $"Citizen return route unresolved: citizen={_controller.World.Hero!.Id.Value}, " +
                $"tick={_controller.World.CurrentTick}, reason=no completed Shelter.");
            return;
        }

        _pendingReturnHome = true;
        int entranceStreet = WorkplaceEntranceStreet(shelter.Value.Street);
        _route = PlanCitizenRoute(
            _heroStreet,
            _heroLateral,
            entranceStreet,
            shelter.Value.LateralOffset);
        ReconstructHeroRouteIfElapsed(transitStartedAtTick);
        LogCitizenTravel(
            "started",
            _controller.World.Hero!.Id,
            new BuildingId(shelter.Value.BuildingId),
            returningHome: true);
    }

    private void ReconstructHeroRouteIfElapsed(int? transitStartedAtTick)
    {
        if (_route is null
            || transitStartedAtTick is not int startedAt
            || _controller.World.CurrentTick <= startedAt)
        {
            return;
        }
        ReconstructedRoutePosition reconstructed = ReconstructRouteProgress(
            _route,
            _heroStreet,
            _heroLateral,
            _controller.World.CurrentTick - startedAt,
            CityEconomyRules.AbstractTravelTicks);
        _heroStreet = reconstructed.Street;
        _heroLateral = reconstructed.Lateral;
        _depthAnchor = reconstructed.Street;
        _depthTarget = null;
        _routeIndex = reconstructed.RouteIndex;
    }

    private void TryStartHeroAmbientRoute(PlotBox anchor)
    {
        int currentTick = _controller.World.CurrentTick;
        if (_route is not null || currentTick < _heroNextAmbientDecisionTick) return;
        int founderId = _controller.World.Hero?.Id.Value ?? 1;
        int phase = Math.Abs(founderId * 37 + currentTick / 30);
        int targetStreet = Mathf.Clamp(
            WorkplaceEntranceStreet(anchor.Street) + phase % 3 - 1,
            0,
            _streetCount - 1);
        float direction = (phase / 3) % 2 == 0 ? -1f : 1f;
        float targetLateral = Mathf.Clamp(
            anchor.LateralOffset + direction * PixelMotion.StepPixels * (2 + phase % 3),
            -_lateralHalfWidthPx,
            _lateralHalfWidthPx);
        _route = PlanCitizenRoute(
            _heroStreet,
            _heroLateral,
            targetStreet,
            targetLateral);
        _routeIndex = 0;
        _heroAmbientRoute = true;
        _heroNextAmbientDecisionTick = currentTick + 20 + phase % 31;
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
    private List<StreetRoutePlanner.Waypoint> PlanCitizenRoute(
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
        _heroCarrier.Visible = IsProjectedDepthVisible(depth);
        if (!_heroCarrier.Visible) return;
        float lateralOffset = _heroLateral - CameraLateral;
        (Vector2 position, Vector2 scale) = ProjectDepth(depth, lateralOffset);
        _heroCarrier.Scale =
            CitizenSpriteCarrier.ScaleForState(CitizenSpriteCarrier.VisualState.Macro) * scale;
        _heroCarrier.ZIndex = CitizenZ(depth);
        _heroCarrier.Position = PixelMotion.Snap(new Vector2(
            position.X,
            position.Y - HeroFootOffsetMacroPx * scale.Y));
    }

    public override void _Draw()
    {
        _clickableRects.Clear();
        _clickableTreeRects.Clear();
        _clickableCitizenRects.Clear();
        _clickablePlacementRects.Clear();
        _storageFullBadgeRects.Clear();
        for (int street = _streetCount - 1; street >= 0; street--)
        {
            if (!IsProjectedDepthVisible(street - CameraDepthAnchor)) continue;
            DrawStreetGround(street);
        }
        SyncStreetBandLayers();
        UpdateHeroVisual();
        UpdateCitizenJourneyVisuals();
        UpdateCitizenHitRects();
    }

    /// <summary>
    /// Vertical spacing between consecutive street bands in z. Leaves room for
    /// a citizen to land between two bands rather than tying with one.
    /// </summary>
    private const int BandZStep = 4;

    private readonly List<StreetBandLayer> _bandLayers = new();

    /// <summary>
    /// Turns a projected depth into a draw order. Nearer to camera means a
    /// larger z, and it is the <em>same</em> function for street bands and for
    /// citizen carriers — which is the whole point: before this they were
    /// ordered on two incomparable axes, so a citizen always won.
    /// </summary>
    private int DepthToZ(float depth) => Mathf.Clamp(
        Mathf.RoundToInt((_streetCount - (depth + CameraDepthAnchor)) * BandZStep),
        -4000,
        4000);

    /// <summary>
    /// Draw order for a citizen. A citizen stands on the walkable front band
    /// of its lot, in front of whatever that lot holds, so it takes its own
    /// band's order plus one step. Anything on a nearer band still wins,
    /// which is the case that was broken.
    /// </summary>
    private int CitizenZ(float depth) => DepthToZ(depth) + 1;

    /// <summary>
    /// Creates one obstacle layer per street and keeps their z in step with
    /// the camera. Layers are reused across redraws; only their count follows
    /// <c>_streetCount</c>.
    /// </summary>
    private void SyncStreetBandLayers()
    {
        // The scene tree cannot be edited while drawing, so a count change
        // schedules the rebuild for after the frame and this pass only
        // refreshes the layers that already exist.
        if (_bandLayers.Count != _streetCount)
        {
            Callable.From(RebuildStreetBandLayers).CallDeferred();
        }

        for (int street = 0; street < _bandLayers.Count; street++)
        {
            StreetBandLayer layer = _bandLayers[street];
            layer.Street = street;
            layer.Visible = IsProjectedDepthVisible(street - CameraDepthAnchor);
            layer.ZIndex = DepthToZ(street - CameraDepthAnchor);
            if (layer.Visible) layer.QueueRedraw();
        }
    }

    /// <summary>Brings the layer count in line with the street count, off-frame.</summary>
    private void RebuildStreetBandLayers()
    {
        while (_bandLayers.Count < _streetCount)
        {
            var layer = new StreetBandLayer
            {
                Name = $"StreetBand{_bandLayers.Count}",
                Painter = DrawStreetObstacles,
            };
            AddChild(layer);
            _bandLayers.Add(layer);
        }
        while (_bandLayers.Count > _streetCount)
        {
            StreetBandLayer extra = _bandLayers[^1];
            _bandLayers.RemoveAt(_bandLayers.Count - 1);
            extra.QueueFree();
        }
        QueueRedraw();
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
    /// <summary>
    /// The ground of one street: floor, territory tint and placement lots.
    /// Stays on the view's own canvas because terrain is always behind
    /// everything; the obstacles that need depth-ordering against citizens
    /// live in <see cref="DrawStreetObstacles"/> instead.
    /// </summary>
    private void DrawStreetGround(int street)
    {
        float depth = street - CameraDepthAnchor;
        DrawTiledFloor(street, depth);
        // Territory tint per parcel column: visualises the locked /
        // reconnoitred / route-secured / available state so the player
        // can see what the world still hides and what an expedition
        // actually unlocked. Drawn before buildings/trees so the sprites
        // sit on top of the band; drawn after the floor so the tint
        // reads as overlay, not as the ground itself.
        DrawParcelTerritoryTints(street, depth);

        if (_placementActive)
        {
            DrawPlacementLots(street, depth);
        }
    }

    /// <summary>
    /// Buildings and natural resources of one street, painted onto
    /// <paramref name="canvas"/> — a <see cref="StreetBandLayer"/> whose
    /// <c>ZIndex</c> encodes the band's depth, so citizens can be ordered
    /// against them.
    /// </summary>
    private void DrawStreetObstacles(CanvasItem canvas, int street)
    {
        float depth = street - CameraDepthAnchor;
        float anchorDepth = AnchorDepth(depth);

        foreach (PlotBox plot in _plots)
        {
            if (plot.Street != street) continue;
            float relativeOffset = plot.LateralOffset - CameraLateral;
            (Vector2 position, Vector2 scale) = ProjectDepth(anchorDepth, relativeOffset);
            var size = new Vector2(plot.Width * scale.X, plot.Height * scale.Y);
            var rect = new Rect2(
                new Vector2(position.X - size.X * 0.5f, position.Y - size.Y),
                size);
            if (plot.CultivationState is CultivationPlotState cultivationState)
            {
                DrawCultivationSite(canvas, rect, cultivationState);
                if (plot.IsClickable) _clickableRects.Add((rect, plot.BuildingId));
                continue;
            }
            Texture2D? texture = GetBuildingTexture(plot.Kind);
            if (texture is not null)
            {
                canvas.DrawTextureRect(
                    texture,
                    rect,
                    tile: false,
                    modulate: plot.IsUnderConstruction ? UnderConstructionModulate : Colors.White);
            }
            else
            {
                canvas.DrawRect(rect, BuildingColor);
            }
            if (plot.IsClickable) _clickableRects.Add((rect, plot.BuildingId));
            if (plot.IsStorageFull)
            {
                DrawStorageFullBadge(canvas, rect, plot);
            }
        }

        foreach (TreeBox tree in _trees)
        {
            if (tree.Street != street) continue;
            float treeRelativeOffset = tree.LateralOffset - CameraLateral;
            (Vector2 treePosition, Vector2 treeScale) =
                ProjectDepth(anchorDepth, treeRelativeOffset);
            var treeSize = new Vector2(
                ResourceUnitBaseSizePx * treeScale.X,
                ResourceUnitBaseSizePx * treeScale.Y);
            var treeRect = new Rect2(
                new Vector2(treePosition.X - treeSize.X * 0.5f, treePosition.Y - treeSize.Y),
                treeSize);
            DrawNaturalResourceUnit(canvas, tree, treeRect);
            _clickableTreeRects.Add((treeRect, tree));
        }
    }

    private void DrawCultivationSite(CanvasItem canvas, Rect2 rect, CultivationPlotState state)
    {
        Color soil = state == CultivationPlotState.Prepared
            ? new Color("#71513a")
            : new Color("#59412f");
        canvas.DrawRect(rect, soil);
        float lineWidth = Mathf.Max(2f, rect.Size.X * 0.035f);
        for (int row = 1; row <= 3; row++)
        {
            float y = Mathf.Round(rect.Position.Y + rect.Size.Y * row / 4f);
            canvas.DrawLine(
                new Vector2(rect.Position.X + rect.Size.X * 0.12f, y),
                new Vector2(rect.End.X - rect.Size.X * 0.12f, y),
                new Color("#3d2b22"),
                lineWidth,
                antialiased: false);
        }
        if (state == CultivationPlotState.Prepared) return;

        Color plant = state switch
        {
            CultivationPlotState.Sown => new Color("#b89a52"),
            CultivationPlotState.Growing => new Color("#6f9f48"),
            CultivationPlotState.Ready => new Color("#d2b24c"),
            CultivationPlotState.Spent => new Color("#8a7457"),
            _ => Colors.White,
        };
        int markerCount = state == CultivationPlotState.Sown ? 3 : 5;
        float markerSize = Mathf.Max(3f, rect.Size.X * 0.075f);
        for (int marker = 0; marker < markerCount; marker++)
        {
            float x = rect.Position.X
                + rect.Size.X * (marker + 1f) / (markerCount + 1f);
            float height = state switch
            {
                CultivationPlotState.Sown => markerSize,
                CultivationPlotState.Growing => markerSize * 2f,
                CultivationPlotState.Ready => markerSize * 3f,
                _ => markerSize * 1.4f,
            };
            canvas.DrawRect(
                new Rect2(
                    new Vector2(
                        Mathf.Round(x - markerSize * 0.5f),
                        Mathf.Round(rect.End.Y - rect.Size.Y * 0.22f - height)),
                    new Vector2(markerSize, height)),
                plant);
        }
        if (state == CultivationPlotState.Ready)
        {
            float badge = Mathf.Max(5f, rect.Size.X * 0.12f);
            canvas.DrawCircle(
                new Vector2(rect.End.X - badge, rect.Position.Y + badge),
                badge * 0.55f,
                new Color("#f2d35f"),
                filled: true,
                width: -1f,
                antialiased: false);
        }
    }

    /// <summary>
    /// Draws one gatherable ground unit from the shared terrain atlas.
    ///
    /// <para>
    /// Every resource is a real sprite now. Branches, fibre, stone and wild
    /// food used to be flat <c>DrawRect</c> markers in four hard-coded
    /// colours, which at macro distance all read as the same coloured square
    /// and told the player nothing about what they were about to gather.
    /// </para>
    /// </summary>
    private void DrawNaturalResourceUnit(CanvasItem canvas, TreeBox unit, Rect2 rect)
    {
        if (unit.ResourceType != ResourceType.Wood)
        {
            canvas.DrawTextureRectRegion(
                _terrainAtlas,
                rect,
                TerrainAtlas.ResourceRegion(unit.ResourceType, unit.ForestId, unit.UnitId));
            return;
        }

        // A tree is two tiles tall. Drawn at the shared one-tile footprint it
        // came out the same size as a berry bush, so the canopy grows upward
        // out of the rect while the trunk keeps the rect's own ground line —
        // the plot footprint, and therefore the click target, is unchanged.
        // Which tree grows here comes from the biome, so a cactus stands in the
        // sand and a fruiting broadleaf does not. A cactus is a single tile and
        // simply has no canopy above the rect.
        TerrainAtlas.TreeVariant variant =
            TerrainAtlas.TreeFor(_groundBiome, unit.ForestId, unit.UnitId);
        if (variant.IsTall)
        {
            var canopy = new Rect2(
                new Vector2(rect.Position.X, rect.Position.Y - rect.Size.Y),
                rect.Size);
            canvas.DrawTextureRectRegion(
                _terrainAtlas, canopy, TerrainAtlas.RegionOfId(variant.CanopyId));
        }
        canvas.DrawTextureRectRegion(
            _terrainAtlas,
            new Rect2(rect.Position, rect.Size),
            TerrainAtlas.RegionOfId(variant.TrunkId));
    }

    private void DrawStorageFullBadge(CanvasItem canvas, Rect2 buildingRect, PlotBox plot)
    {
        var badgeRect = new Rect2(
            new Vector2(
                buildingRect.End.X - StatusBadgeSize,
                buildingRect.Position.Y - StatusBadgeSize * 0.5f),
            new Vector2(StatusBadgeSize, StatusBadgeSize));
        var borderRect = badgeRect.Grow(StatusBadgeBorder);
        canvas.DrawRect(borderRect, LineageThemeRegistry.IconAccent);
        canvas.DrawRect(badgeRect, new Color(0.06f, 0.05f, 0.04f, 0.94f));
        canvas.DrawTextureRect(_storageFullIcon, badgeRect, tile: false);
        _storageFullBadgeRects.Add((borderRect, plot));
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
        TerrainAtlas.GroundBiome biome = _groundBiome;
        int totalTiles = Mathf.RoundToInt(2f * _lateralHalfWidthPx / TileUnitPx);
        int parcelRow = street / ParcelGrid.ConstructionRowsPerParcel;
        for (int tileRow = 0; tileRow < ParcelGrid.TilesPerStandardLot; tileRow++)
        {
            float depthNear = depth + tileRow / (float)ParcelGrid.TilesPerStandardLot;
            float depthFar = depth + (tileRow + 1) / (float)ParcelGrid.TilesPerStandardLot;
            float yNear = ProjectedRowScreenY(depthNear);
            float yFar = ProjectedRowScreenY(depthFar);
            float scaleNear = ProjectedHorizontalScale(depthNear);
            float scaleFar = ProjectedHorizontalScale(depthFar);
            int globalTileRow = street * ParcelGrid.TilesPerStandardLot + tileRow;

            for (int tileIndex = 0; tileIndex < totalTiles; tileIndex++)
            {
                int parcelColumn = tileIndex / ParcelGrid.FrontageColumnsPerParcel;
                if (!_parcelTerritory.ContainsKey((parcelRow, parcelColumn))) continue;
                float tileCenterGlobal = (tileIndex + 0.5f) * TileUnitPx - _lateralHalfWidthPx;
                float leftGlobal = tileCenterGlobal - TileUnitPx * 0.5f - CameraLateral;
                float rightGlobal = tileCenterGlobal + TileUnitPx * 0.5f - CameraLateral;
                // Deterministic terrain hash over tile index and global tile
                // row. It used to be thrown away by `% 11 == 0`, a boolean
                // that flipped ~1 tile in 11 between two near-identical
                // variants of the same swatch — which is why the ground read
                // as three flat bands. The whole hash now indexes the biome's
                // fill list, and the material comes from the city's site
                // rather than from `street % 3`.
                // A spatial hash, not the old `tileIndex * 3 + row * 5`: with
                // three fill variants that expression degenerates, because
                // `tileIndex * 3 % 3` is always zero and the choice collapses
                // to the row — the ground came out in flat horizontal stripes.
                int variant = TerrainAtlas.GroundVariantIndex(
                    tileIndex, globalTileRow, biome.Fill.Length);
                // Only tileRow 0 (the calle's own walkable front band) can
                // wear into a path — the lot depth behind it (tileRow 1/2)
                // is never trodden, it's where buildings/trees sit.
                DrawPixelStaircaseTrapezoid(
                    yNear, yFar,
                    CenterX + leftGlobal * scaleNear, CenterX + rightGlobal * scaleNear,
                    CenterX + leftGlobal * scaleFar, CenterX + rightGlobal * scaleFar,
                    _terrainAtlas, TerrainAtlas.RegionOfId(biome.Fill[variant]));

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
                    _terrainAtlas, TerrainAtlas.RegionOfId(biome.Path));
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
    private TerrainAtlas.GroundBiome _groundBiome = TerrainAtlas.BiomeFor(LineageId.Ardhen);

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
    /// Renders each available three-column frontage window as its real 3x3
    /// ground footprint. Overlapping windows stay lightly filled and a click
    /// resolves to the nearest projected center. Every window projects its
    /// near and far edges independently,
    /// so the blueprint shares the terrain's vanishing point instead of
    /// becoming an axis-aligned screen rectangle after projecting only its
    /// centre.
    /// </summary>
    private void DrawPlacementLots(int street, float streetDepth)
    {
        foreach (PlacementCellBox cell in _placementCells)
        {
            if (cell.Street != street) continue;
            ProjectPlacementFootprint(
                cell.LateralOffset,
                cell.Width,
                streetDepth,
                out Vector2 nearLeft,
                out Vector2 nearRight,
                out Vector2 farRight,
                out Vector2 farLeft);
            bool blocked = cell.Cell.State != FrontageCellState.Available;
            DrawSteppedPlacementFootprint(
                nearLeft,
                nearRight,
                farRight,
                farLeft,
                blocked ? PlacementBlockedCellColor : PlacementAvailableColor,
                PlacementGridColor,
                frontageDivisions: 1,
                depthDivisions: BuildingReservation.RequiredDepthRows,
                drawInvalidMarker: blocked);
        }

        foreach (PlacementLotBox lot in _placementLots)
        {
            if (lot.Street != street) continue;
            ProjectPlacementFootprint(
                lot.LateralOffset,
                lot.Width,
                streetDepth,
                out Vector2 nearLeft,
                out Vector2 nearRight,
                out Vector2 farRight,
                out Vector2 farLeft);
            Vector2 boundsMin = new(
                Mathf.Min(Mathf.Min(nearLeft.X, nearRight.X), Mathf.Min(farLeft.X, farRight.X)),
                Mathf.Min(nearLeft.Y, farLeft.Y));
            Vector2 boundsMax = new(
                Mathf.Max(Mathf.Max(nearLeft.X, nearRight.X), Mathf.Max(farLeft.X, farRight.X)),
                Mathf.Max(nearLeft.Y, farLeft.Y));
            _clickablePlacementRects.Add((new Rect2(boundsMin, boundsMax - boundsMin), lot));
        }

        PlacementLotBox? preview = _hoveredPlacementLot is PlacementLotBox hovered
            && hovered.Street == street
                ? hovered
                : null;
        if (preview is null && _selectedPlacementLot is ConstructionLot selected)
        {
            foreach (PlacementLotBox candidate in _placementLots)
            {
                if (candidate.Street != street || candidate.Window.Lot != selected) continue;
                preview = candidate;
                break;
            }
        }
        if (preview is not PlacementLotBox highlighted) return;
        ProjectPlacementFootprint(
            highlighted.LateralOffset,
            highlighted.Width,
            streetDepth,
            out Vector2 previewNearLeft,
            out Vector2 previewNearRight,
            out Vector2 previewFarRight,
            out Vector2 previewFarLeft);
        bool isSelected = _selectedPlacementLot is ConstructionLot selectedLot
            && selectedLot == highlighted.Window.Lot
            && _hoveredPlacementLot is null;
        Color previewFill = !highlighted.Window.IsValid
            ? PlacementHoveredInvalidColor
            : isSelected
                ? PlacementSelectedColor
                : PlacementHoveredValidColor;
        Color previewOutline = !highlighted.Window.IsValid
            ? new Color("#ff7777")
            : isSelected
                ? new Color("#ffe08a")
                : new Color("#8dffad");
        DrawSteppedPlacementFootprint(
            previewNearLeft,
            previewNearRight,
            previewFarRight,
            previewFarLeft,
            previewFill,
            previewOutline,
            frontageDivisions: highlighted.Window.Lot.FrontageColumns,
            depthDivisions: BuildingReservation.RequiredDepthRows,
            drawInvalidMarker: !highlighted.Window.IsValid);
    }

    private void ProjectPlacementFootprint(
        float lateralOffset,
        float width,
        float streetDepth,
        out Vector2 nearLeft,
        out Vector2 nearRight,
        out Vector2 farRight,
        out Vector2 farLeft)
    {
        float lotLeft = lateralOffset - width * 0.5f - CameraLateral;
        float lotRight = lotLeft + width;
        float depthNear = streetDepth;
        float depthFar = streetDepth + 1f;
        float yNear = ProjectedRowScreenY(depthNear);
        float yFar = ProjectedRowScreenY(depthFar);
        float scaleNear = ProjectedHorizontalScale(depthNear);
        float scaleFar = ProjectedHorizontalScale(depthFar);
        nearLeft = new Vector2(CenterX + lotLeft * scaleNear, yNear);
        nearRight = new Vector2(CenterX + lotRight * scaleNear, yNear);
        farRight = new Vector2(CenterX + lotRight * scaleFar, yFar);
        farLeft = new Vector2(CenterX + lotLeft * scaleFar, yFar);
    }

    /// <summary>
    /// Per-parcel-column tint driven by <see cref="CityParcel.TerritoryState"/>.
    /// Available parcels get no overlay so the terrain reads normally;
    /// Locked gets an opaque dark band so the player cannot mistake it
    /// for buildable ground; intermediate states get a translucent hue
    /// so the player can see an expedition has done partial work without
    /// the tint looking like a hard error. Drawn band-by-band on the
    /// stepped trapezoid that already projects the floor, so the tint
    /// shares the same vanishing point and lateral camera offset.
    /// </summary>
    private void DrawParcelTerritoryTints(int street, float streetDepth)
    {
        float totalLotColumns = _worldParcelColumns * ParcelGrid.LotsPerAxis;
        int parcelRow = street / ParcelGrid.ConstructionRowsPerParcel;
        float depthNear = streetDepth;
        float depthFar = streetDepth + 1f;
        float yNear = ProjectedRowScreenY(depthNear);
        float yFar = ProjectedRowScreenY(depthFar);
        float scaleNear = ProjectedHorizontalScale(depthNear);
        float scaleFar = ProjectedHorizontalScale(depthFar);
        foreach (((int Row, int Column) coordinate, ParcelTerritoryState territoryState) in _parcelTerritory)
        {
            if (coordinate.Row != parcelRow) continue;
            Color fill = territoryState switch
            {
                ParcelTerritoryState.Locked => LockedParcelColor,
                ParcelTerritoryState.Reconnoitred => ReconnoitredParcelColor,
                ParcelTerritoryState.RouteSecured => RouteSecuredParcelColor,
                ParcelTerritoryState.Available => Colors.Transparent,
                _ => Colors.Transparent,
            };
            if (fill.A == 0) continue;
            float parcelLeftColumn = coordinate.Column * ParcelGrid.LotsPerAxis;
            float parcelLeft = (parcelLeftColumn - totalLotColumns * 0.5f) * LotUnitPx
                - CameraLateral;
            float parcelRight = parcelLeft
                + ParcelGrid.LotsPerAxis * LotUnitPx;
            var nearLeft = new Vector2(CenterX + parcelLeft * scaleNear, yNear);
            var nearRight = new Vector2(CenterX + parcelRight * scaleNear, yNear);
            var farRight = new Vector2(CenterX + parcelRight * scaleFar, yFar);
            var farLeft = new Vector2(CenterX + parcelLeft * scaleFar, yFar);
            DrawSteppedTintTrapezoid(nearLeft, nearRight, farRight, farLeft, fill);
        }
    }

    private void DrawSteppedTintTrapezoid(
        Vector2 nearLeft,
        Vector2 nearRight,
        Vector2 farRight,
        Vector2 farLeft,
        Color fill)
    {
        float height = Mathf.Abs(farLeft.Y - nearLeft.Y);
        int stripes = Mathf.Max(1, Mathf.CeilToInt(height / PixelStepPx));
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
            float left = Mathf.Min(left0.X, left1.X);
            float right = Mathf.Max(right0.X, right1.X);
            DrawRect(new Rect2(
                new Vector2(left, top),
                new Vector2(Mathf.Max(1f, right - left), Mathf.Max(1f, bottom - top))),
                fill);
        }
    }

    private void DrawSteppedPlacementFootprint(
        Vector2 nearLeft,
        Vector2 nearRight,
        Vector2 farRight,
        Vector2 farLeft,
        Color fill,
        Color outline,
        int frontageDivisions,
        int depthDivisions,
        bool drawInvalidMarker)
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

        for (int column = 1; column < frontageDivisions; column++)
        {
            float t = column / (float)frontageDivisions;
            Vector2 near = PixelMotion.Snap(nearLeft.Lerp(nearRight, t));
            Vector2 far = PixelMotion.Snap(farLeft.Lerp(farRight, t));
            DrawSteppedPlacementEdge(near, far, stripes, outline);
        }
        for (int row = 1; row < depthDivisions; row++)
        {
            float t = row / (float)depthDivisions;
            Vector2 left = PixelMotion.Snap(nearLeft.Lerp(farLeft, t));
            Vector2 right = PixelMotion.Snap(nearRight.Lerp(farRight, t));
            DrawLine(left, right, outline, 2f, antialiased: false);
        }
        if (drawInvalidMarker)
        {
            Vector2 topLeft = PixelMotion.Snap(nearLeft.Lerp(farLeft, 0.75f));
            Vector2 topRight = PixelMotion.Snap(nearRight.Lerp(farRight, 0.75f));
            Vector2 bottomLeft = PixelMotion.Snap(nearLeft.Lerp(farLeft, 0.25f));
            Vector2 bottomRight = PixelMotion.Snap(nearRight.Lerp(farRight, 0.25f));
            DrawLine(topLeft.Lerp(topRight, 0.2f), bottomLeft.Lerp(bottomRight, 0.8f), outline, 3f, false);
            DrawLine(topRight.Lerp(topLeft, 0.2f), bottomRight.Lerp(bottomLeft, 0.8f), outline, 3f, false);
        }
    }

    private void DrawSteppedPlacementEdge(
        Vector2 from,
        Vector2 to,
        int steps,
        Color color)
    {
        Vector2 previous = from;
        for (int index = 1; index <= steps; index++)
        {
            Vector2 next = PixelMotion.Snap(from.Lerp(to, index / (float)steps));
            Vector2 corner = new(next.X, previous.Y);
            DrawLine(previous, corner, color, 2f, false);
            DrawLine(corner, next, color, 2f, false);
            previous = next;
        }
    }
}
