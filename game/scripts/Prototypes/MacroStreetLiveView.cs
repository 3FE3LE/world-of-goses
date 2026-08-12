#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;
// A4: the plot/tree records live on the renderer. Aliasing them here keeps
// the in-class references inside this view compilable without touching the
// 50+ bodies that already use the unqualified names.
using PlotBox = WorldofGoses.Prototypes.MacroStreetRenderer.PlotBox;
using TreeBox = WorldofGoses.Prototypes.MacroStreetRenderer.TreeBox;
using PlacementLotBox = WorldofGoses.Prototypes.PlacementPresenter.PlacementLotBox;
using PlacementCellBox = WorldofGoses.Prototypes.PlacementPresenter.PlacementCellBox;
using CitizenJourney = WorldofGoses.Prototypes.CitizenJourneyPresenter.JourneyState;

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
/// (<see cref="ToggleCameraMode"/>, F key or the primary navigation button),
/// independent from selection. Free mode decouples the vanishing point
/// (<see cref="CameraLateral"/>/
/// <see cref="CameraDepthAnchor"/>) from the founder's own true position
/// (<see cref="_journeys.HeroLateral"/>/<see cref="_journeys.HeroStreet"/>, which keeps
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
    // A4: every shared numeric and color lives in MacroViewConstants. The
    // view keeps the same `private const` / `private static readonly` names
    // as compile-time-or-static forwarders so the existing in-class
    // references (and the existing test surface) keep compiling unchanged.
    private const bool DefaultCameraFollowsHero = MacroViewConstants.DefaultCameraFollowsHero;
    private const float CenterX = MacroViewConstants.CenterX;
    private const float BaseY = MacroViewConstants.BaseY; // ScreenContent-local. HUD migrations do not
    // retune the world projection; doing so would shift every authored depth row.
    private const float CameraZoomPivotY = MacroViewConstants.CameraZoomPivotY;
    private const float LotUnitPx = MacroViewConstants.LotUnitPx;
    private const int DefaultWorldParcelColumns = MacroViewConstants.DefaultWorldParcelColumns;
    private const int DefaultWorldParcelRows = MacroViewConstants.DefaultWorldParcelRows;

    // Quantized zoom: discrete steps, never a continuous drag/slider.
    private const float ZoomStep = MacroViewConstants.ZoomStep;
    private const float MinZoom = MacroViewConstants.MinZoom;
    private const float DefaultZoom = MacroViewConstants.DefaultZoom;
    private const float MaxZoom = MacroViewConstants.MaxZoom;

    // Holding vertical pan repeats slowly at first, then gently accelerates.
    // The camera still advances only on the 12 Hz pixel-motion cadence and
    // still crosses integer streets through discrete transition steps.
    private const float VerticalPanInitialRepeatSeconds = MacroViewConstants.VerticalPanInitialRepeatSeconds;
    private const float VerticalPanMinimumRepeatSeconds = MacroViewConstants.VerticalPanMinimumRepeatSeconds;
    private const float VerticalPanAccelerationSeconds = MacroViewConstants.VerticalPanAccelerationSeconds;
    private const float VerticalPanMaximumTransitionMultiplier = MacroViewConstants.VerticalPanMaximumTransitionMultiplier;

    // Same cadence discipline as the earlier prototypes (design bible §08,
    // "Pixel-motion grammar"): no continuous tweening.
    private const int TransitionSteps = MacroViewConstants.TransitionSteps;
    private const float DepthStepSize = MacroViewConstants.DepthStepSize;

    // Building-entry camera push: a handful of DISCRETE zoom steps toward
    // the clicked building (same stepped cadence as citizen/camera motion —
    // never a continuous Tween), applied to THIS node's own Scale/Position
    // (the map), not to BuildingDetailView. See BeginBuildingEntry.
    private const int BuildingEntryZoomSteps = MacroViewConstants.BuildingEntryZoomSteps;
    private const float BuildingEntryZoomLevel = MacroViewConstants.BuildingEntryZoomLevel;

    // One resource unit owns one frontage cell. Its visual canvas therefore
    // stays within that cell instead of visually claiming a whole 3×3 lot.
    private const float ResourceUnitBaseSizePx = MacroViewConstants.ResourceUnitBaseSizePx;
    // Lateral span a living tree blocks when crossing its band (its lot).
    // Half hero width plus a small margin: how much free lateral space a
    // crossing between streets needs to count as viable.
    private const float RouteClearancePx = MacroViewConstants.RouteClearancePx;
    // Granularity when scanning a band for a viable crossing point. Must be
    // small enough to reliably land inside the narrowest realistic gap
    // between two adjacent same-row obstacles (with today's spacing, as
    // little as ~18 px) — a coarser step can jump clean over a legitimate
    // gap and force the search much farther out, reading as if the hero
    // detoured around a whole row instead of threading through it.
    private const float CrossingScanStepPx = MacroViewConstants.CrossingScanStepPx;
    // LPC frames center the body; feet sit ~28 frame px below center, which
    // is 7 px at the carrier's 0.25 macro scale (before depth scaling).
    private const float HeroFootOffsetMacroPx = MacroViewConstants.HeroFootOffsetMacroPx;

    private const string ResourceActionMenuScenePath = MacroViewConstants.ResourceActionMenuScenePath;
    private const string CultivationActionMenuScenePath = MacroViewConstants.CultivationActionMenuScenePath;

    // Floor tiles sample the Kenney atlas ResourceTree already uses for
    // trees (S-1.3 biome pass), keyed by street so the corridor reads as
    // distinct ground per calle.
    private const float TileUnitPx = MacroViewConstants.TileUnitPx; // ParcelGrid.TilesPerStandardLot
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
    private const float PixelStepPx = MacroViewConstants.PixelStepPx;
    // Ground biome atlas coordinates in the shared Kenney roguelike sheet
    // (see ResourceTree.TerrainAtlasPath/AtlasRegionRect for the 16px+1
    // stride convention). Rows 0/1 hold two near-identical variants of each
    // solid ground swatch — used the same way GroundBiome's own "alternate"
    // checkerboard used to alternate between two flat colors, now between
    // two real tiles of the same material. Column 4 (dirt) is deliberately
    // the same swatch reserved for the future worn-path pass (H-32 S-1.3
    // follow-up): a trampled tile will simply render as this same Dirt
    // biome rather than needing a fourth texture.
    private const int GrassAtlasColumn = MacroViewConstants.GrassAtlasColumn;
    private const int DirtAtlasColumn = MacroViewConstants.DirtAtlasColumn;
    private const int StoneAtlasColumn = MacroViewConstants.StoneAtlasColumn;
    private const int GroundAtlasRowA = MacroViewConstants.GroundAtlasRowA;
    private const int GroundAtlasRowB = MacroViewConstants.GroundAtlasRowB;
    private const float StatusBadgeSize = MacroViewConstants.StatusBadgeSize;
    private const float StatusBadgeBorder = MacroViewConstants.StatusBadgeBorder;
    private static readonly Color BuildingColor = MacroViewConstants.BuildingColor;
    private static readonly Color UnderConstructionModulate = MacroViewConstants.UnderConstructionModulate;
    private static readonly Color PlacementAvailableColor = MacroViewConstants.PlacementAvailableColor;
    private static readonly Color PlacementHoveredValidColor = MacroViewConstants.PlacementHoveredValidColor;
    private static readonly Color PlacementHoveredInvalidColor = MacroViewConstants.PlacementHoveredInvalidColor;
    private static readonly Color PlacementBlockedCellColor = MacroViewConstants.PlacementBlockedCellColor;
    private static readonly Color PlacementGridColor = MacroViewConstants.PlacementGridColor;
    // Territory tints: opaque for Locked so the player cannot mistake
    // it for buildable ground; progressively lighter for intermediate
    // states so the visual cost of an expedition reads at a glance.
    private static readonly Color LockedParcelColor = MacroViewConstants.LockedParcelColor;
    private static readonly Color ReconnoitredParcelColor = MacroViewConstants.ReconnoitredParcelColor;
    private static readonly Color RouteSecuredParcelColor = MacroViewConstants.RouteSecuredParcelColor;
    private static readonly Color PlacementSelectedColor = MacroViewConstants.PlacementSelectedColor;

    [Export] public NodePath ControllerPath { get; set; } = "../../../CityWorldController";
    [Export] public NodePath StatusPanelPath { get; set; } = "../../CityStatusPanel";
    [Export] public NodePath ConstructionPanelPath { get; set; } = "../Center/ConstructionPanel";
    [Export] public NodePath ExpeditionPanelPath { get; set; } = "../Center/ExpeditionPanel";
    [Export] public NodePath PoliciesPanelPath { get; set; } = "../PoliciesPanel";
    [Export] public NodePath CitizensPanelPath { get; set; } = "../MigrantPanel";
    [Export] public NodePath ModalHostPath { get; set; } = "../ModalHost";
    /// <summary>
    /// The primary navigation dock. One path replaces the literal button paths this
    /// view used to carry: the dock owns its own structure and hands back typed
    /// buttons, so moving a button inside it no longer breaks the world view.
    /// </summary>
    [Export] public NodePath PrimaryNavDockPath { get; set; } = "../PrimaryNavDock";
    [Export] public NodePath CitySummaryPanelPath { get; set; } = "../CitySummaryPanel";
    [Export] public NodePath ExpeditionRailPath { get; set; } = "../ExpeditionRail";
    /// <summary>
    /// The contextual selection surface. Authored in the scene rather than
    /// constructed here, so its placement is anchors instead of a per-frame
    /// reposition.
    /// </summary>
    [Export] public NodePath ContextInspectorPath { get; set; } = "../ContextInspector";
    /// <summary>The contextual action tray, shown only while a mode needs it.</summary>
    [Export] public NodePath ActionDockPath { get; set; } = "../ActionDock";
    [Export] public NodePath PauseMenuPath { get; set; } = "../../../PauseMenu";
    [Export] public NodePath BuildingDetailViewPath { get; set; } = "../BuildingDetailView";

    private CityWorldController _controller = null!;
    private LocaleManager _localeManager = null!;
    private CityStatusPanel _statusPanel = null!;
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
    private PrimaryNavDock _primaryNavDock = null!;
    private CitySummaryPanel _citySummaryPanel = null!;
    private ExpeditionRail _expeditionRail = null!;
    private BuildingDetailView _buildingDetailView = null!;
    private IconButton _cameraModeButton = null!;
    private CursorController? _cursorController;
    private Texture2D _terrainAtlas = null!;
    private Texture2D _storageFullIcon = null!;
    private WorldStatusBubble _worldStatusBubble = null!;
    // A4: building texture cache and terrain wear moved to MacroStreetRenderer.
    // (TerrainWearGrid kept here only because the doc-comments above still
    // reference it; the renderer is the canonical owner — see its
    // WearAt/TrampleHeroTile.)
    // A4: _hoveredCitizenId, _hoveredStorageBuildingId, _visualStatusCitizenId,
    // _selectionIsMacro moved to MacroInteractionController.
    // A4: _zoomLevel, _neutralPosition, _pendingBuildingEntry,
    // _buildingEntryPivotLocal, _buildingEntryStartZoom, _buildingEntryStep,
    // _buildingEntryAccumulator, _cameraFollowsHero, _freeCameraLateral,
    // _freeCameraStreet, _cameraDepthAnchor, _cameraDepthTarget,
    // _cameraTransitionAccumulator, _verticalPanDirection,
    // _verticalPanHoldSeconds, _verticalPanRepeatAccumulator moved to
    // MacroCameraController.

    // Placement mode: select-then-confirm frontage picking projected directly on
    // the same terrain geometry as the city.
    private readonly record struct ResourceFeedbackAnchor(
        Vector2 Position,
        Node2D? FollowTarget,
        Vector2 FollowOffset);
    // A4: hit-rect collections moved to MacroHitRects bag; the placement
    // state (active flag, kind, projected lots and cells, hover and
    // selection) moved to PlacementPresenter.
    private readonly PlacementPresenter _placement = new();
    private bool _selectHeroAfterModalClose;
    private ActionDock _actionDock = null!;
    private PauseMenu _pauseMenu = null!;

    // A4: _plots and _trees moved to MacroStreetRenderer.
    // S-1.3 phase 2: session-scoped foot-traffic wear, not persisted (see
    // TerrainWearGrid's own doc for why it deliberately stays out of WorldSave).
    // A4: terrain wear grid moved to MacroStreetRenderer.
    // A4: _hitRects.BuildingClickableRects, _hitRects.TreeClickableRects, _hitRects.CitizenClickableRects, _hitRects.StorageBadgeRects
    // moved to MacroHitRects bag.
    // A4: _bandOccupancy and EmptyBand moved to MacroStreetRenderer.

    // A4 (kept for legacy reads; the renderer is the canonical owner):
    private int _streetCount = 1;
    private float _lateralHalfWidthPx = LotUnitPx;
    private int _worldParcelColumns = DefaultWorldParcelColumns;
    private int _worldParcelRows = DefaultWorldParcelRows;
    // A4: _citizenStates and _parcelTerritory moved to MacroStreetRenderer.

    // A4: founder's physical position and journey state moved to
    // CitizenJourneyPresenter. The view's selection state also moved
    // to MacroInteractionController above.

    private ContextInspector _contextInspector = null!;

    // A4: _heroCarrier, _heroStreet, _heroLateral, _depthAnchor, _depthTarget,
    // _motionAccumulator, _transitionAccumulator, _heroWalking,
    // _heroPositionInitialized, _journeys.Journeys, _route, _routeIndex,
    // _pendingGather, _pendingAssignment, _pendingReturnHome,
    // _heroIsGatheringOutsideHome, _heroAmbientRoute,
    // _heroNextAmbientDecisionTick, _routePacingStartTick, _routeTotalSteps,
    // _routeStepsApplied, _lastKnownAssignment, _lastKnownHeroLocation,
    // _journeys.NavmeshPlanner moved to CitizenJourneyPresenter.

    internal readonly record struct ReconstructedRoutePosition(
        int Street,
        float Lateral,
        int RouteIndex,
        int StepsApplied);
    // A4: navmeshPlanner, _route, _routeIndex, _pendingGather, _pendingAssignment,
    // _pendingReturnHome, _heroIsGatheringOutsideHome, _heroAmbientRoute,
    // _heroNextAmbientDecisionTick, _routePacingStartTick, _routeTotalSteps,
    // _routeStepsApplied, _lastKnownAssignment, _lastKnownHeroLocation
    // moved to CitizenJourneyPresenter.

    internal float CameraLateralForVisualRegression => _camera.FreeCameraLateral;
    private bool _treeHovered;
    private ResourceType _hoveredResource = ResourceType.Wood;

    // A1 boundary closure: cached read-only projection of the aggregate
    // facts this view reads every frame. Rebuilt on WorldTickAdvanced and
    // on demand by RefreshMacroViewState(). Replaces 30+ direct
    // `_controller.World.X` reads without holding the live aggregate.
    private MacroStreetLiveViewState _macroState = default!;
    private bool _macroStateInitialized;
    private readonly MacroStreetRenderer _renderer = new();
    private readonly MacroInteractionController _interaction = new();
    private readonly CitizenJourneyPresenter _journeys = new();
    private readonly MacroCameraController _camera = new();
    private readonly MacroHitRects _hitRects = new();

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
        _localeManager = GetNode<LocaleManager>("/root/LocaleManager");
        _statusPanel = GetNode<CityStatusPanel>(StatusPanelPath);
        _statusPanel.AttachController(_controller);
        _statusPanel.Refresh(_controller);
        _constructionPanel = GetNode<ConstructionPanel>(ConstructionPanelPath);
        _expeditionPanel = GetNode<ExpeditionPanel>(ExpeditionPanelPath);
        _policiesPanel = GetNode<PoliciesPanel>(PoliciesPanelPath);
        _citizensPanel = GetNode<MigrantPanel>(CitizensPanelPath);
        _modalHost = GetNode<ModalHost>(ModalHostPath);
        _primaryNavDock = GetNode<PrimaryNavDock>(PrimaryNavDockPath);
        _citySummaryPanel = GetNode<CitySummaryPanel>(CitySummaryPanelPath);
        _expeditionRail = GetNode<ExpeditionRail>(ExpeditionRailPath);
        _contextInspector = GetNode<ContextInspector>(ContextInspectorPath);
        _actionDock = GetNode<ActionDock>(ActionDockPath);
        _pauseMenu = GetNode<PauseMenu>(PauseMenuPath);
        _constructionMenuButton = _primaryNavDock.ConstructionButton;
        _expeditionMenuButton = _primaryNavDock.ExpeditionButton;
        _policiesButton = _primaryNavDock.PoliciesButton;
        _citizensButton = _primaryNavDock.CitizensButton;
        _cameraModeButton = _statusPanel.CameraButton;
        _statusPanel.MenuButton.Pressed += OnUtilityClusterMenuPressed;
        _statusPanel.SpeedButton.AttachController(_controller);
        _buildingDetailView = GetNode<BuildingDetailView>(BuildingDetailViewPath);
        _cursorController = GetNodeOrNull<CursorController>("/root/CursorController");
        _terrainAtlas = GD.Load<Texture2D>(ResourceTree.TerrainAtlasPath);
        _renderer.LoadTerrainAtlas(ResourceTree.TerrainAtlasPath);
        _storageFullIcon = GD.Load<Texture2D>(IconPaths.Check);
        _worldStatusBubble = new WorldStatusBubble();
        GetParent().CallDeferred(Node.MethodName.AddChild, _worldStatusBubble);
        // Pixel-art atlas tiles scale up crisp instead of smearing.
        TextureFilter = TextureFilterEnum.Nearest;

        // A4: the renderer owns the per-street band layer stack. The painter
        // is the view's DrawStreetObstacles — the renderer installs it on
        // every new layer it creates.
        _renderer.Attach(this);
        _renderer.Painter = DrawStreetObstacles;
        _interaction.Attach(_worldStatusBubble, _cursorController);
        _camera.Attach(this);

        _streetCount = _worldParcelRows * ParcelGrid.LotsPerAxis;
        _lateralHalfWidthPx =
            _worldParcelColumns * ParcelGrid.LotsPerAxis * LotUnitPx * 0.5f;
        _renderer.StreetCount = _streetCount;
        _renderer.LateralHalfWidthPx = _lateralHalfWidthPx;
        _renderer.WorldParcelColumns = _worldParcelColumns;
        _renderer.WorldParcelRows = _worldParcelRows;

        _actionMenu = GD.Load<PackedScene>(ResourceActionMenuScenePath).Instantiate<ResourceActionMenu>();
        _actionMenu.GatherRequested += OnGatherRequested;
        // ScreenContent is still mid-_Ready() for its children, so transient
        // controls are attached after the scene-tree setup pass.
        GetParent().CallDeferred(Node.MethodName.AddChild, _actionMenu);
        _cultivationActionMenu = GD.Load<PackedScene>(CultivationActionMenuScenePath)
            .Instantiate<CultivationActionMenu>();
        _cultivationActionMenu.CultivationRequested += OnCultivationRequested;
        GetParent().CallDeferred(Node.MethodName.AddChild, _cultivationActionMenu);
        _journeys.NavmeshPlanner = new StreetNavigationServerPlanner();
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
        _constructionPanel.ViewHeroRequested += OnConstructionHeroRequested;
        _modalHost.Closed += OnModalHostClosedForNavigationState;
        _localeManager.LocaleChanged += OnLocaleChanged;
        _cameraModeButton.Pressed += ToggleCameraMode;
        UpdateCameraModeButtonLabel();
        UpdatePrimaryNavigationState();

        RefreshPlots();
        _camera.FreeCameraStreet = Mathf.Clamp(2, 0, _streetCount - 1);
        _camera.CameraDepthAnchor = _camera.FreeCameraStreet;
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
        _camera.NeutralPosition = Position;
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
        _constructionPanel.ViewHeroRequested -= OnConstructionHeroRequested;
        _modalHost.Closed -= OnModalHostClosedForNavigationState;
        _localeManager.LocaleChanged -= OnLocaleChanged;
        _cameraModeButton.Pressed -= ToggleCameraMode;
        _actionDock.ConfirmButton.Pressed -= OnPlacementConfirmPressed;
        _actionDock.CancelButton.Pressed -= OnPlacementCancelPressed;
        _journeys.NavmeshPlanner?.Dispose();
        _renderer.Dispose();
    }

    /// <summary>
    /// Labels the dock's actions for placement mode and subscribes to them. The
    /// lots themselves are drawn and hit-tested like every other element in this
    /// view (see <see cref="_Draw"/>/<see cref="TryClick"/>), not as a button grid,
    /// since their position depends on the depth projection.
    /// </summary>
    private void BuildPlacementChrome()
    {
        _actionDock.ConfirmButton.SetIconAndLabel(
            IconPaths.Check, UiText.Get("Confirm placement"));
        _actionDock.ConfirmButton.Disabled = true;
        _actionDock.CancelButton.SetIconAndLabel(IconPaths.Close, UiText.Get("Cancel"));
        _actionDock.ConfirmButton.Pressed += OnPlacementConfirmPressed;
        _actionDock.CancelButton.Pressed += OnPlacementCancelPressed;
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
    }

    private void OnWorldTickAdvanced(int _)
    {
        RefreshMacroViewState();
        _statusPanel.Refresh(_controller);
        RefreshPlots();
        RefreshConstructionPanelIfOpen();
    }

    /// <summary>
    /// Rebuilds the cached <see cref="MacroStreetLiveViewState"/> from the
    /// controller's projection. Called on every world tick and on demand
    /// before any read that needs fresh values.
    /// </summary>
    private void RefreshMacroViewState()
    {
        _macroState = _controller.GetMacroStreetViewState();
        _macroStateInitialized = true;
    }

    private MacroStreetLiveViewState MacroState
    {
        get
        {
            if (!_macroStateInitialized) RefreshMacroViewState();
            return _macroState;
        }
    }

    /// <summary>
    /// Keeps the city hidden while a detail/profile screen owns selection.
    /// </summary>
    private void OnSelectionChanged(int selectionState)
    {
        _interaction.SelectionIsMacro =
            (CityWorldController.Selection)selectionState == CityWorldController.Selection.MacroView;
        if (!_interaction.SelectionIsMacro)
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
        ShowMacroHudSurfaces();
        if (!_placement.PlacementActive) ShowPrimaryNavigation();
        _actionMenu.Hide();
        _cultivationActionMenu.Hide();
        _contextInspector.Hide();
        _worldStatusBubble.Hide();
        Show();
        RefreshPlots();
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
        foreach ((Rect2 rect, int id) in _hitRects.BuildingClickableRects)
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
        _primaryNavDock.Hide();
        _journeys.HeroCarrier?.SetState(CitizenSpriteCarrier.VisualState.Hidden);
    }

    public void CompleteFounderArrival()
    {
        ActivatePerspective();
        ShowPrimaryNavigation();
        EnsureHeroCarrier(_controller.GetCityMacroSnapshot());
    }

    /// <summary>Hides this view plus its own transient surfaces (menu, axe cursor, placement, selection, zoom).</summary>
    private void Deactivate()
    {
        ClearWorldStatusHover();
        _interaction.VisualStatusCitizenId = null;
        Hide();
        HideMacroHudSurfaces();
        _primaryNavDock.Hide();
        _actionMenu.Hide();
        _cultivationActionMenu.Hide();
        _contextInspector.Hide();
        _interaction.SelectedTree = null;
        _interaction.SelectedBuildingId = null;
        ClearTreeHover();
        if (_placement.PlacementActive) EndPlacement(restorePrimaryNavigation: false);
        ResetZoom();
    }

    private void ShowMacroHudSurfaces()
    {
        _citySummaryPanel.Show();
        _expeditionRail.Show();
    }

    private void HideMacroHudSurfaces()
    {
        _citySummaryPanel.Hide();
        _expeditionRail.Hide();
    }

    /// <summary>
    /// Always starts fresh next time this view reactivates — a building
    /// entry's zoom-in push (<see cref="BeginBuildingEntry"/>) must not
    /// leave the map zoomed in when the player later backs out to it.
    /// </summary>
    private void ResetZoom()
    {
        _camera.ZoomLevel = DefaultZoom;
        Scale = Vector2.One;
        Position = _camera.NeutralPosition;
        ZoomTowardPivot(DefaultZoom, new Vector2(CenterX, CameraZoomPivotY));
        _camera.PendingBuildingEntry = null;
    }

    /// <summary>Opens or closes construction from the city toolbar.</summary>
    private void OnConstructionMenuPressed()
    {
        if (!Visible) return;
        ClearWorldStatusHover();
        if (_placement.PlacementActive)
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
        if (_modalHost.IsOpen)
        {
            _modalHost.Close();
        }
        else
        {
            _expeditionPanel.Open();
        }
        UpdatePrimaryNavigationState();
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
        if (_modalHost.IsOpen)
        {
            _modalHost.Close();
        }
        else
        {
            _policiesPanel.Open();
        }
        UpdatePrimaryNavigationState();
    }

    private void OnCitizensPressed()
    {
        if (!Visible) return;
        ClearWorldStatusHover();
        if (_modalHost.IsOpen)
        {
            _modalHost.Close();
        }
        else
        {
            _citizensPanel.Open();
        }
        UpdatePrimaryNavigationState();
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
        UpdatePrimaryNavigationState();
        _constructionPanel.ScrollBodyToEndForVisualRegression();
    }

    internal void ShowConstructionPlacementHoverForVisualRegression(bool valid)
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        ActivatePerspective();
        OnPlacementRequested((int)ConstructionKind.Farm);
        foreach (PlacementLotBox candidate in _placement.PlacementLots)
        {
            if (candidate.Window.IsValid != valid) continue;
            _placement.SetHoveredLot(candidate);
            _actionDock.InstructionText = PlacementHoverText(candidate.Window.State);
            QueueRedraw();
            return;
        }
    }

    internal void PreparePlacementConfirmationForVisualRegression()
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        ActivatePerspective();
        OnPlacementRequested((int)ConstructionKind.Farm);
        foreach (PlacementLotBox candidate in _placement.PlacementLots)
        {
            if (!candidate.Window.IsValid) continue;
            SelectPlacementLot(candidate);
            return;
        }
        GD.PushError("Placement confirmation fixture found no valid lot.");
    }

    internal void ShowEarlyGameResourcesForVisualRegression()
    {
        ActivatePerspective();
    }

    internal void ShowCitizenStatusForVisualRegression(CitizenId citizenId)
    {
        RefreshPlots();
        _interaction.VisualStatusCitizenId = citizenId;
        CitizenSpriteCarrier? carrier = null;
        if (_journeys.HeroCarrier?.Id == citizenId)
        {
            carrier = _journeys.HeroCarrier;
        }
        else if (_journeys.Journeys.TryGetValue(citizenId.Value, out CitizenJourney? journey))
        {
            carrier = journey.Carrier;
        }

        if (carrier is null || !IsVisibleMacroCarrier(carrier))
        {
            GD.PushError($"World-status fixture could not expose citizen {citizenId.Value} on the macro map.");
            return;
        }
        if (!_renderer.CitizenStates.TryGetValue(citizenId.Value, out CityMacroSnapshot.CitizenItem? citizen)) return;
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
        if (_journeys.HeroCarrier?.Id == citizenId)
        {
            carrier = _journeys.HeroCarrier;
        }
        else if (_journeys.Journeys.TryGetValue(citizenId.Value, out CitizenJourney? journey))
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

    private void OnModalHostClosedForNavigationState()
    {
        if (_selectHeroAfterModalClose)
        {
            _selectHeroAfterModalClose = false;
            _controller.SelectHero();
            return;
        }
        if (!Visible) return;
        UpdatePrimaryNavigationState();
    }

    private void OnConstructionHeroRequested()
    {
        if (!Visible || !_modalHost.IsOpen || _modalHost.Content != _constructionPanel) return;
        _selectHeroAfterModalClose = true;
        _modalHost.Close();
    }

    private void OnLocaleChanged(string _)
    {
        UpdatePrimaryNavigationState();
        UpdateCameraModeButtonLabel();
    }

    private void UpdatePrimaryNavigationState()
    {
        UpdateConstructionButtonLabel();
        UpdateModalNavigationButton(
            _expeditionMenuButton,
            _expeditionPanel,
            IconPaths.Backpack,
            "ui.nav.expedition_short",
            "Send the founder on a reconnaissance");
        UpdateModalNavigationButton(
            _policiesButton,
            _policiesPanel,
            IconPaths.ClipboardNote,
            "ui.nav.policies_short",
            "Review city-wide policies");
        UpdateModalNavigationButton(
            _citizensButton,
            _citizensPanel,
            IconPaths.Users,
            "ui.nav.citizens_short",
            "Open the citizens roster");
    }

    private void UpdateModalNavigationButton(
        IconButton button,
        Control content,
        string normalIcon,
        string normalLabel,
        string normalTooltip)
    {
        bool selected = _modalHost.IsOpen && _modalHost.Content == content;
        button.SetIconAndLabel(
            selected ? IconPaths.Close : normalIcon,
            UiText.Get(selected ? "Close" : normalLabel));
        button.ThemeTypeVariation = selected ? "HudButtonSelected" : "HudButton";
        button.TooltipText = UiText.Get(selected ? "Close" : normalTooltip);
    }

    private void UpdateConstructionButtonLabel()
    {
        if (_modalHost.IsOpen && _modalHost.Content == _constructionPanel)
        {
            _constructionMenuButton.SetIconAndLabel(IconPaths.Close, UiText.Get("Close"));
            _constructionMenuButton.ThemeTypeVariation = "HudButtonSelected";
            _constructionMenuButton.TooltipText = UiText.Get("Close the construction menu (work continues).");
        }
        else if (_placement.PlacementActive)
        {
            _constructionMenuButton.SetIconAndLabel(IconPaths.Close, UiText.Get("Cancel"));
            _constructionMenuButton.ThemeTypeVariation = "HudButtonSelected";
        }
        else
        {
            _constructionMenuButton.SetIconAndLabel(
                IconPaths.Building, UiText.Get("ui.nav.build_short"));
            _constructionMenuButton.ThemeTypeVariation = "HudButton";
            _constructionMenuButton.TooltipText = UiText.Get("Open the construction menu.");
        }
    }

    private void ShowPrimaryNavigation()
    {
        _actionDock.Hide();
        _primaryNavDock.Show();
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
        _placement.Begin(
            kind,
            UiText.Format(
                "ui.construction.choose_lot",
                UiText.Get(ConstructionRules.DisplayNameFor(kind))));
        _actionDock.ConfirmButton.Disabled = true;
        _actionDock.InstructionText = _placement.PlacementBaseInstruction;
        _primaryNavDock.Hide();
        _actionDock.Show();
        _actionMenu.Hide();
        _cultivationActionMenu.Hide();
        _contextInspector.Hide();
        _interaction.SelectedTree = null;
        _interaction.SelectedBuildingId = null;
        ClearTreeHover();
        float totalFrontageColumns = _worldParcelColumns * ParcelGrid.FrontageColumnsPerParcel;
        foreach (ConstructionPlacementSnapshot.WindowItem window in placement.Windows)
        {
            ConstructionLot lot = window.Lot;
            int street = lot.RowId.Value;
            float frontageCenter = lot.StartColumn + lot.FrontageColumns * 0.5f;
            float lateralOffset = (frontageCenter - totalFrontageColumns * 0.5f) * TileUnitPx;
            _placement.AddLot(new PlacementLotBox(
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
            _placement.AddCell(new PlacementCellBox(
                cell,
                cell.RowId.Value,
                lateralOffset,
                TileUnitPx,
                BuildingReservation.RequiredDepthRows * TileUnitPx));
        }
        QueueRedraw();
    }

    private void EndPlacement(bool restorePrimaryNavigation = true)
    {
        _placement.End();
        _hitRects.PlacementRects.Clear();
        _actionDock.Hide();
        if (restorePrimaryNavigation) _primaryNavDock.Show();
        UpdateConstructionButtonLabel();
        QueueRedraw();
    }

    private void SelectPlacementLot(PlacementLotBox lot)
    {
        _placement.SelectLot(lot);
        bool selected = _placement.SelectedPlacementLot.HasValue;
        _actionDock.ConfirmButton.Disabled = !selected;
        _actionDock.InstructionText = selected
            ? UiText.Get("ui.construction.placement_selected")
            : PlacementHoverText(lot.Window.State);
        QueueRedraw();
    }

    private void UpdatePlacementHover(Vector2 mousePosition)
    {
        PlacementLotBox? nearest =
            PlacementPresenter.TryFindNearestLot(mousePosition, _hitRects.PlacementRects);
        if (!_placement.SetHoveredLot(nearest)) return;
        _actionDock.InstructionText = nearest is PlacementLotBox hovered
            ? PlacementHoverText(hovered.Window.State)
            : _placement.SelectedPlacementLot.HasValue
                ? UiText.Get("ui.construction.placement_selected")
                : _placement.PlacementBaseInstruction;
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
        if (_placement.SelectedPlacementLot is not ConstructionLot lot) return;
        ConstructionAuthorizationResult result =
            _controller.TryAuthorizeConstruction(_placement.PlacementKind, lot);
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
        _renderer.ClearPlotsAndTrees();
        _renderer.ClearBandOccupancy();
        CityMacroSnapshot snapshot = _controller.GetCityMacroSnapshot();
        RefreshParcelEnvelope(snapshot);
        _renderer.ClearCitizenStates();
        foreach (CityMacroSnapshot.CitizenItem citizen in snapshot.Citizens)
        {
            _renderer.SetCitizenState(citizen.Id.Value, citizen);
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
        _renderer.ClearParcelTerritory();
        int maximumColumn = -1;
        int maximumRow = -1;
        foreach (CityMacroSnapshot.ParcelItem parcel in snapshot.Parcels)
        {
            _renderer.SetParcelTerritory(parcel.LogicalRow, parcel.LogicalColumn, parcel.TerritoryState);
            maximumColumn = Math.Max(maximumColumn, parcel.LogicalColumn);
            maximumRow = Math.Max(maximumRow, parcel.LogicalRow);
        }
        _worldParcelColumns = Math.Max(1, maximumColumn + 1);
        _worldParcelRows = Math.Max(1, maximumRow + 1);
        _streetCount = _worldParcelRows * ParcelGrid.ConstructionRowsPerParcel;
        _lateralHalfWidthPx = _worldParcelColumns
            * ParcelGrid.ConstructionRowsPerParcel
            * LotUnitPx * 0.5f;
        _renderer.StreetCount = _streetCount;
        _renderer.LateralHalfWidthPx = _lateralHalfWidthPx;
        _renderer.WorldParcelColumns = _worldParcelColumns;
        _renderer.WorldParcelRows = _worldParcelRows;
    }

    /// <summary>
    /// Keeps the selection panel's remaining reserve live as the world ticks
    /// or a gather completes. Clears the
    /// selection if the selected tree is gone (fully depleted units are
    /// dropped from <see cref="_trees"/> — see <see cref="AddTrees"/>).
    /// </summary>
    private void RefreshSelectionInfoIfShown()
    {
        if (_interaction.SelectedTree is { } selectedTree)
        {
            foreach (TreeBox tree in _renderer.Trees)
            {
                if (tree.ForestId != selectedTree.ForestId || tree.UnitId != selectedTree.UnitId) continue;
                SelectTree(tree);
                return;
            }
            ClearSelection();
            return;
        }
        if (_interaction.SelectedCitizenId is { } selectedCitizenId)
        {
            if (_renderer.CitizenStates.ContainsKey(selectedCitizenId.Value))
            {
                SelectCitizen(selectedCitizenId);
                return;
            }
            ClearSelection();
            return;
        }
        if (_interaction.SelectedBuildingId is not { } buildingId) return;
        foreach (PlotBox plot in _renderer.Plots)
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
        if (!_renderer.CitizenStates.TryGetValue(citizenId.Value, out CityMacroSnapshot.CitizenItem? citizen))
        {
            ClearSelection();
            return;
        }
        _interaction.SelectedCitizenId = citizenId;
        _interaction.SelectedTree = null;
        _interaction.SelectedBuildingId = null;
        Texture2D? icon = ResourceLoader.Load<Texture2D>(IconPaths.User);
        _contextInspector.ShowSelection(icon, citizen.Name, FormatCitizenSelectionDetail(citizen));
    }

    internal static string FormatCitizenSelectionDetail(CityMacroSnapshot.CitizenItem citizen) =>
        MacroSelectionTextBuilder.FormatCitizenSelectionDetail(citizen);

    /// <summary>
    /// Translates raw domain values into the strings the view layer formats
    /// via <see cref="UiText.Format"/>. The only translation needed today is
    /// the wound recovery duration (ticks → human-readable string); the
    /// severity key is already a localization key and passes through unchanged.
    /// </summary>
    private static object[] TranslateSelectionArgs(IReadOnlyList<object> formatArgs) =>
        MacroSelectionTextBuilder.TranslateSelectionArgs(formatArgs);

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
    internal static IReadOnlyList<SelectionLine> BuildCitizenSelectionKeys(CityMacroSnapshot.CitizenItem citizen) =>
        MacroSelectionTextBuilder.BuildCitizenSelectionKeys(citizen);

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
            _renderer.AddTree(forest, unitId, (int)totalLotColumns);
        }
    }

    private void AddPlot(CityMacroSnapshot.PlotItem item, bool clickable) =>
        _renderer.AddPlot(item, clickable, _worldParcelColumns);

    internal static StreetRoutePlanner.Interval BuildingObstacleInterval(
        CityMacroSnapshot.PlotItem item,
        float totalFrontageColumns,
        float tileUnitPx) =>
        MacroObstacleGeometry.BuildingObstacleInterval(item, totalFrontageColumns, tileUnitPx);

    internal static StreetRoutePlanner.Interval ObstacleIntervalFromClearances(
        float reservedStart,
        float reservedWidth,
        float leftClearance,
        float rightClearance) =>
        MacroObstacleGeometry.ObstacleIntervalFromClearances(
            reservedStart, reservedWidth, leftClearance, rightClearance);

    private void AddBandInterval(int band, float start, float end) =>
        _renderer.AddBandInterval(band, start, end);

    private IReadOnlyList<StreetRoutePlanner.Interval> GetBandOccupancy(int band) =>
        _renderer.GetBandOccupancy(band);

    /// <summary>The vanishing point's lateral position — the founder's own
    /// while following, an independently-steered value while free.</summary>
    private float CameraLateral =>
        _camera.CameraFollowsHero && TryGetObservedCitizenAnchor(out _, out float lateral)
            ? lateral
            : _camera.FreeCameraLateral;

    /// <summary>The vanishing point's smoothed depth — see the class doc's
    /// "Camera mode" note and <see cref="AdvanceTransition"/>.</summary>
    internal float CameraDepthAnchor =>
        _camera.CameraFollowsHero && TryGetObservedCitizenAnchor(out float depth, out _)
            ? depth
            : _camera.CameraDepthAnchor;

    private bool IsProjectedDepthVisible(float relativeDepth) =>
        MacroProjectionHelpers.IsProjectedDepthVisible(relativeDepth);

    private static float ProjectedRowScreenY(float relativeDepth) =>
        MacroProjectionHelpers.ProjectedRowScreenY(relativeDepth, BaseY);

    private static float ProjectedHorizontalScale(float relativeDepth) =>
        MacroProjectionHelpers.HorizontalScale(relativeDepth);

    private (Vector2 Position, Vector2 Scale) ProjectDepth(
        float relativeDepth,
        float lateralOffset) => MacroProjectionHelpers.Project(
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
        && _journeys.Journeys.TryGetValue(citizenId.Value, out CitizenJourney? journey)
        && journey.Route is not null;

    public void ShowThirdStreetDepthForVisualRegression()
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        SetCameraFollowsHero(false);
        _camera.FreeCameraStreet = Mathf.Clamp(2, 0, _streetCount - 1);
        _camera.CameraDepthAnchor = _camera.FreeCameraStreet;
        _camera.CameraDepthTarget = null;
        _camera.CameraTransitionAccumulator = 0f;
        QueueRedraw();
    }

    public void ShowLongTerrariumForVisualRegression()
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        ActivatePerspective();
        SetCameraFollowsHero(false);
        _camera.FreeCameraStreet = Mathf.Clamp(2, 0, _streetCount - 1);
        _camera.CameraDepthAnchor = _camera.FreeCameraStreet;
        _camera.CameraDepthTarget = null;
        _camera.CameraTransitionAccumulator = 0f;
        ZoomTowardPivot(MinZoom, new Vector2(CenterX, CameraZoomPivotY));
        GD.Print(
            $"Long terrarium fixture: {_worldParcelRows} parcel rows, "
            + $"{_streetCount} streets, zoom {_camera.ZoomLevel:F2}.");
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
        bool hasCitizenTravel = _journeys.Route is not null
            || _journeys.Journeys.Values.Any(journey => journey.Route is not null);
        if (!Visible && !hasCitizenTravel) return;
        _journeys.MotionAccumulator += (float)delta;
        while (_journeys.MotionAccumulator >= PixelMotion.CadenceSeconds)
        {
            _journeys.MotionAccumulator -= PixelMotion.CadenceSeconds;
            MotionTick(allowCameraInput: CanUseWorldNavigationInput);
            AdvanceCitizenJourneysTick();
        }
        // The founder's own smoothed row (always active — it also paces
        // AdvanceRouteTick regardless of camera mode) and, independently,
        // the free camera's own smoothed row when not following.
        bool heroDepthAnimating = _journeys.DepthTarget.HasValue;
        bool cameraDepthAnimating = _camera.CameraDepthTarget.HasValue;
        float heroDepthAnchor = _journeys.DepthAnchor;
        float? heroDepthTarget = _journeys.DepthTarget;
        float heroTransitionAccumulator = _journeys.TransitionAccumulator;
        AdvanceTransition(ref heroDepthAnchor, ref heroDepthTarget, ref heroTransitionAccumulator, delta);
        _journeys.UpdateFounderTransition(heroDepthAnchor, heroDepthTarget, heroTransitionAccumulator);
        float cameraDepthAnchor = _camera.CameraDepthAnchor;
        float? cameraDepthTarget = _camera.CameraDepthTarget;
        float cameraTransitionAccumulator = _camera.CameraTransitionAccumulator;
        AdvanceTransition(
            ref cameraDepthAnchor,
            ref cameraDepthTarget,
            ref cameraTransitionAccumulator,
            delta,
            DepthStepSize * VerticalPanTransitionMultiplier(_camera.VerticalPanHoldSeconds));
        bool citizenDepthAnimating = false;
        foreach (CitizenJourney journey in _journeys.Journeys.Values)
        {
            citizenDepthAnimating |= journey.DepthTarget.HasValue;
            AdvanceJourneyTransition(journey, delta);
        }
        if (heroDepthAnimating || cameraDepthAnimating || citizenDepthAnimating) QueueRedraw();
        if (Visible)
        {
            AdvanceBuildingEntry(delta);
            if (!_interaction.VisualStatusCitizenId.HasValue)
            {
                // The world owns the pointer whenever the cursor sits over
                // a citizen or a full-storage badge. The macro view's own
                // hit-rects are the single source of truth for what the
                // world can claim — overlaying a PanelContainer with
                // MouseFilter = Stop (ExpeditionRail, MigrantPanel,
                // etc.) must not strip the bubble, because the macro view
                // is the only world surface and a Stop overlay sitting
                // beside a citizen does not mean the world yields input.
                // Without this, the bubble blinks open on the motion event
                // and ClearWorldStatusHover hides it one frame later —
                // the exact symptom the user reported (visible only when
                // an external window forced a redraw).
                Vector2 localMouse = ToLocal(GetViewport().GetMousePosition());
                if (_placement.PlacementActive)
                {
                    UpdatePlacementHover(localMouse);
                }
                bool worldOwnsPointer = TryFindHoveredCitizen(localMouse, out _, out _)
                    || IsCursorOverStorageBadge(localMouse);
                if (!worldOwnsPointer && (
                    _modalHost.IsOpen
                    || _actionMenu.Visible
                    || _placement.PlacementActive
                    || _camera.PendingBuildingEntry is not null
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

    public override void _Input(InputEvent @event)
    {
        if (!Visible
            || _pauseMenu.Visible
            || _modalHost?.IsOpen == true
            || _placement.PlacementActive
            || _actionMenu.Visible
            || _cultivationActionMenu.Visible
            || @event is not InputEventKey { Pressed: true } key
            || !IsWorldNavigationArrow(key))
        {
            return;
        }

        // Arrow keys are a world-camera binding in macro mode. Godot also
        // maps them to ui_left/right/up/down, which otherwise moves focus in
        // the HUD while Input.IsActionPressed moves the camera in the same
        // frame. Handling the physical key before GUI dispatch reserves it
        // for the world. Gamepad D-pad events remain available to the HUD's
        // explicit focus neighbours.
        GetViewport().SetInputAsHandled();
    }

    internal static bool IsWorldNavigationArrow(InputEventKey key) =>
        key.Keycode is Key.Left or Key.Right or Key.Up or Key.Down
        || key.PhysicalKeycode is Key.Left or Key.Right or Key.Up or Key.Down;

    private bool CanUseWorldNavigationInput =>
        Visible
        && !_pauseMenu.Visible
        && !_modalHost.IsOpen
        && !_placement.PlacementActive
        && !_actionMenu.Visible
        && !_cultivationActionMenu.Visible;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible) return;
        // A building-entry push is a brief, exclusive, non-interruptible
        // transition — same spirit as the fullscreen placement scrim.
        if (_camera.PendingBuildingEntry is not null) return;
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
        if (_placement.PlacementActive && @event.IsActionPressed("ui_cancel"))
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
        if (!CanUseWorldNavigationInput
            && (@event.IsActionPressed(CameraInputActions.PanLeft)
                || @event.IsActionPressed(CameraInputActions.PanRight)
                || @event.IsActionPressed(CameraInputActions.PanUp)
                || @event.IsActionPressed(CameraInputActions.PanDown)
                || @event.IsActionPressed(CameraInputActions.ToggleFollow)))
        {
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
                if (_placement.PlacementActive)
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
        float newZoom = Mathf.Clamp(_camera.ZoomLevel + delta, MinZoom, MaxZoom);
        if (Mathf.IsEqualApprox(newZoom, _camera.ZoomLevel)) return;
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
        _camera.ZoomLevel = newZoom;
    }

    /// <summary>
    /// Camera mode toggle (design bible §04 "Cámara-sigue"): follow the
    /// founder or pan freely (the default), independent of any selection.
    /// Placement does not alter or lock this choice; directional input keeps
    /// its camera-only meaning while the player inspects candidate lots.
    /// </summary>
    private void ToggleCameraMode()
    {
        SetCameraFollowsHero(!_camera.CameraFollowsHero);
    }

    /// <summary>
    /// Opens or closes the pause menu when the top-bar utility cluster's
    /// menu button is pressed. The cluster's button is wired in <c>_Ready</c>;
    /// the existing <see cref="_pauseMenu"/> owns the toggle behaviour so the
    /// cluster and the dock's old menu button converge on the same action.
    /// </summary>
    private void OnUtilityClusterMenuPressed() => _pauseMenu.Toggle();

    private void SetCameraFollowsHero(bool value)
    {
        if (_camera.CameraFollowsHero == value) return;
        if (!value)
        {
            // Entering free mode starts exactly where follow mode left
            // off — no visual jump at the moment of toggling, only
            // subsequent free-camera input diverges it from the founder.
            float currentLateral = CameraLateral;
            float currentDepth = CameraDepthAnchor;
            _camera.FreeCameraLateral = currentLateral;
            _camera.FreeCameraStreet = Mathf.RoundToInt(currentDepth);
            _camera.CameraDepthAnchor = currentDepth;
            _camera.CameraDepthTarget = null;
            _camera.CameraTransitionAccumulator = 0f;
        }
        _camera.CameraFollowsHero = value;
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
        CitizenId? founderId = _controller.GetHeroId();
        if (observedId is null || observedId == founderId)
        {
            depth = _journeys.DepthAnchor;
            lateral = _journeys.HeroLateral;
            return founderId is not null;
        }
        if (_journeys.Journeys.TryGetValue(observedId.Value.Value, out CitizenJourney? journey))
        {
            depth = journey.DepthAnchor;
            lateral = journey.Lateral;
            return true;
        }

        CitizenRoutineSnapshot? routine = _controller.GetCitizenRoutineSnapshot(observedId.Value);
        if (routine?.ContextBuildingId is BuildingId buildingId
            && FindPlot(buildingId) is PlotBox plot)
        {
            depth = WorkplaceEntranceStreet(plot.Street);
            lateral = plot.LateralOffset;
            return true;
        }
        depth = _journeys.DepthAnchor;
        lateral = _journeys.HeroLateral;
        return false;
    }

    private void UpdateCameraModeButtonLabel()
    {
        _cameraModeButton.SetIconAndLabel(
            IconPaths.Camera,
            _camera.CameraFollowsHero
                ? UiText.Get("ui.camera.follow_short")
                : UiText.Get("ui.camera.free_short"));
        _cameraModeButton.TooltipText = _camera.CameraFollowsHero
            ? UiText.Get("ui.camera.follow_tooltip")
            : UiText.Get("ui.camera.free_tooltip");
        _cameraModeButton.ThemeTypeVariation = _camera.CameraFollowsHero
            ? "HudButtonSelected"
            : "HudButton";
    }

    /// <summary>
    /// Left click: select. Both trees and buildings populate
    /// <see cref="_contextInspector"/> with their details instead of
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
        if (_placement.PlacementActive)
        {
            PlacementLotBox? nearest =
                PlacementPresenter.TryFindNearestLot(clickPosition, _hitRects.PlacementRects);
            if (nearest is PlacementLotBox selected) SelectPlacementLot(selected);
            return;
        }
        foreach ((Rect2 rect, TreeBox tree) in _hitRects.TreeClickableRects)
        {
            if (!rect.HasPoint(clickPosition)) continue;
            SelectTree(tree);
            return;
        }
        foreach ((Rect2 rect, CitizenId citizenId) in _hitRects.CitizenClickableRects)
        {
            if (!rect.HasPoint(clickPosition)) continue;
            SelectCitizen(citizenId);
            return;
        }
        foreach ((Rect2 rect, int buildingId) in _hitRects.BuildingClickableRects)
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
        if (_placement.PlacementActive) return;
        foreach ((Rect2 rect, TreeBox tree) in _hitRects.TreeClickableRects)
        {
            if (!rect.HasPoint(clickPosition)) continue;
            SelectTree(tree);
            OpenGatherMenu(tree, rect);
            return;
        }
        foreach ((Rect2 rect, CitizenId citizenId) in _hitRects.CitizenClickableRects)
        {
            if (!rect.HasPoint(clickPosition)) continue;
            SelectCitizen(citizenId);
            return;
        }
        foreach ((Rect2 rect, int buildingId) in _hitRects.BuildingClickableRects)
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
        _interaction.SelectedTree = tree;
        _interaction.SelectedBuildingId = null;
        // The selection icon is the same sprite the world draws, so the panel
        // and the plot agree. Non-wood resources used to fall back to a
        // generic leaf glyph regardless of what they actually were.
        Texture2D icon = ResourceTree.CreateRegion(
            _terrainAtlas,
            tree.ResourceType == ResourceType.Wood
                // The trunk tile of the very tree that was clicked, so the
                // panel shows a cactus when a cactus was selected.
                ? TerrainAtlas.RegionOfId(
                    TerrainAtlas.TreeFor(_renderer.GroundBiome, tree.ForestId, tree.UnitId).TrunkId)
                : TerrainAtlas.ResourceRegion(tree.ResourceType, tree.ForestId, tree.UnitId));
        string resourceName = UiText.Get(tree.ResourceType.ToString().ToLowerInvariant());
        string detail = UiText.Format("ui.resource.units_remain", tree.Reserve, resourceName);
        _contextInspector.ShowSelection(icon, resourceName, detail);
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
            _interaction.SelectedBuildingId = buildingId;
            _interaction.SelectedTree = null;
            string cultivationDetail = CultivationDetail(plot, state);
            _contextInspector.ShowSelection(
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
        _interaction.SelectedBuildingId = buildingId;
        _interaction.SelectedTree = null;
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
        _contextInspector.ShowSelection(icon, fullLabel, detail);
    }

    private string CultivationDetail(PlotBox plot, CultivationPlotState state)
    {
        if (state == CultivationPlotState.Growing
            && plot.ReadyAtTick is int readyAtTick)
        {
            int remaining = Mathf.Max(0, readyAtTick - _controller.CurrentTick);
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
        CitizenId? founderId = _controller.GetHeroId();
        bool founderAvailable = founderId.HasValue && MacroState.HeroIsAvailable;
        bool canAct = founderAvailable
            && state is (CultivationPlotState.Prepared or CultivationPlotState.Ready);
        string tooltip = !founderAvailable
            ? UiText.Get("ui.cultivation.founder_unavailable")
            : state switch
        {
            CultivationPlotState.Prepared => _controller.GetFoodStock()
                >= CultivationRules.SeedFoodCost
                    ? UiText.Get("ui.cultivation.sow_action")
                    : UiText.Get("ui.cultivation.missing_seed_food"),
            CultivationPlotState.Ready => UiText.Get("ui.cultivation.harvest_action"),
            CultivationPlotState.Sown or CultivationPlotState.Growing =>
                UiText.Get("ui.cultivation.not_ready"),
            _ => UiText.Get("ui.cultivation.spent"),
        };
        if (state == CultivationPlotState.Prepared
            && _controller.GetFoodStock() < CultivationRules.SeedFoodCost)
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
        CultivationSiteSnapshot? site = _controller.GetCultivationSiteSnapshot(new BuildingId(siteId));
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
        if (_interaction.SelectedTree is null && _interaction.SelectedBuildingId is null && _interaction.SelectedCitizenId is null) return;
        _interaction.SelectedTree = null;
        _interaction.SelectedBuildingId = null;
        _interaction.SelectedCitizenId = null;
        _contextInspector.Hide();
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
        if (_camera.PendingBuildingEntry is not null) return;
        ClearWorldStatusHover();
        _camera.PendingBuildingEntry = buildingId;
        _camera.BuildingEntryPivotLocal = pivotLocal;
        _camera.BuildingEntryStartZoom = _camera.ZoomLevel;
        _camera.BuildingEntryStep = 0;
        _camera.BuildingEntryAccumulator = 0f;
    }

    private void AdvanceBuildingEntry(double delta)
    {
        if (_camera.PendingBuildingEntry is not { } buildingId) return;
        _camera.BuildingEntryAccumulator += (float)delta;
        while (_camera.BuildingEntryAccumulator >= PixelMotion.CadenceSeconds && _camera.PendingBuildingEntry is not null)
        {
            _camera.BuildingEntryAccumulator -= PixelMotion.CadenceSeconds;
            _camera.BuildingEntryStep++;
            float t = (float)_camera.BuildingEntryStep / BuildingEntryZoomSteps;
            ZoomTowardPivot(Mathf.Lerp(_camera.BuildingEntryStartZoom, BuildingEntryZoomLevel, t), _camera.BuildingEntryPivotLocal);
            if (_camera.BuildingEntryStep >= BuildingEntryZoomSteps)
            {
                _camera.PendingBuildingEntry = null;
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
        if (_placement.PlacementActive)
        {
            ClearTreeHover();
            return;
        }
        bool hovering = _interaction.TryFindHoveredTree(
            mousePosition, _hitRects.TreeClickableRects, out ResourceType hoveredResource);
        if (hovering == _interaction.TreeHovered
            && hoveredResource == _interaction.HoveredResource) return;
        _interaction.TreeHovered = hovering;
        _interaction.HoveredResource = hoveredResource;
        if (hovering) _interaction.CursorController?.UseGatherCursor(hoveredResource);
        else _interaction.CursorController?.RestoreSurfaceCursor();
    }

    private void UpdateWorldHover(Vector2 mousePosition)
    {
        if (_placement.PlacementActive || UiInputBoundary.IsPointerOwnedByUi(GetViewport()))
        {
            ClearTreeHover();
            _interaction.ClearWorldStatusHover();
            return;
        }

        if (TryFindHoveredCitizen(mousePosition, out CityMacroSnapshot.CitizenItem? citizen, out Rect2 citizenRect)
            && citizen is not null)
        {
            ClearTreeHover();
            if (_interaction.HoveredCitizenId != citizen.Id.Value || _interaction.HoveredStorageBuildingId.HasValue)
            {
                _interaction.HoveredCitizenId = citizen.Id.Value;
                _interaction.HoveredStorageBuildingId = null;
            }
            ShowCitizenStatus(citizen, citizenRect);
            return;
        }

        foreach ((Rect2 rect, PlotBox plot) in _hitRects.StorageBadgeRects)
        {
            if (!rect.HasPoint(mousePosition)) continue;
            ClearTreeHover();
            if (_interaction.HoveredCitizenId.HasValue || _interaction.HoveredStorageBuildingId != plot.BuildingId)
            {
                _interaction.HoveredCitizenId = null;
                _interaction.HoveredStorageBuildingId = plot.BuildingId;
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

        _interaction.ClearWorldStatusHover();
        UpdateTreeHover(mousePosition);
    }

    private void ClearWorldStatusHover() => _interaction.ClearWorldStatusHover();

    private bool IsCursorOverStorageBadge(Vector2 localMouse) =>
        _interaction.IsCursorOverStorageBadge(localMouse, _hitRects.StorageBadgeRects);

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
        if (_journeys.HeroCarrier is not null
            && IsVisibleMacroCarrier(_journeys.HeroCarrier)
            && _renderer.CitizenStates.TryGetValue(_journeys.HeroCarrier.Id.Value, out CityMacroSnapshot.CitizenItem? heroState))
        {
            Rect2 heroRect = CitizenHoverRect(_journeys.HeroCarrier);
            if (heroRect.HasPoint(mousePosition))
            {
                citizen = heroState;
                citizenRect = heroRect;
                return true;
            }
        }

        foreach (CitizenJourney journey in _journeys.Journeys.Values)
        {
            if (!IsVisibleMacroCarrier(journey.Carrier)
                || !_renderer.CitizenStates.TryGetValue(journey.CitizenId.Value, out CityMacroSnapshot.CitizenItem? state))
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
        if (IsDuplicateGatherRequest(_journeys.PendingGather, forestId, unitId)) return;
        NaturalResourceGatherResult availability =
            _controller.GetNaturalResourceGatherAvailability(forestId, unitId);
        if (!availability.CanGather)
        {
            Notifier.ShowError(DescribeGatherBlocker(availability.Outcome));
            return;
        }
        TreeBox? target = null;
        foreach (TreeBox tree in _renderer.Trees)
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
        _journeys.PendingReturnHome = false;
        _journeys.PendingAssignment = null;
        _journeys.PendingGather = (forestId, unitId);
        _journeys.Route = PlanCitizenRoute(_journeys.HeroStreet, _journeys.HeroLateral, target.Value.Street, target.Value.LateralOffset);
        _journeys.RouteIndex = 0;
        // Gathering is not a work assignment, so the domain holds no journey to
        // pace this against. Keep the plain cadence gait.
        AnchorHeroRoutePacing(null);
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
        if (_journeys.Route is not null)
        {
            AdvanceRouteTick();
        }
        else if (_journeys.HeroWalking && !_journeys.DepthTarget.HasValue)
        {
            _journeys.HeroWalking = false;
            _journeys.HeroCarrier?.Idle(Vector2.Down);
        }
        if (!allowCameraInput) return;
        TryPanCameraLateral();
        ContinueVerticalCameraPan();
    }

    private void AdvanceRouteTick()
    {
        if (_journeys.Route is null)
        {
            CompleteRoute();
            return;
        }
        if (_journeys.RoutePacingStartTick is int startedAt)
        {
            // A domain journey: walk to wherever the world clock says we should
            // be. The route cannot finish early, cannot finish late, and cannot
            // decide anything — the domain already owns the arrival.
            AdvanceHeroRouteToStep(PacedRouteSteps(
                _journeys.RouteTotalSteps,
                _controller.CurrentTick - startedAt,
                _controller.CurrentTickPhase,
                CityEconomyRules.AbstractTravelTicks));
            return;
        }
        AdvanceHeroRouteOneStep();
    }

    /// <summary>
    /// Walks the founder forward until the paced step budget is spent. Steps are
    /// still discrete 4 px / one-street moves; only their timing now comes from
    /// the world clock rather than from the render cadence.
    /// </summary>
    private void AdvanceHeroRouteToStep(int targetSteps)
    {
        if (_journeys.Route is null) return;
        bool moved = false;
        while (_journeys.RouteStepsApplied < targetSteps && _journeys.RouteIndex < _journeys.Route.Count)
        {
            int previousStreet = _journeys.HeroStreet;
            float previousLateral = _journeys.HeroLateral;
            int routeIndex = _journeys.RouteIndex;
            int heroStreet = _journeys.HeroStreet;
            float heroLateral = _journeys.HeroLateral;
            AdvanceReconstructedRouteStep(_journeys.Route, ref heroStreet, ref heroLateral, ref routeIndex);
            _journeys.HeroStreet = heroStreet;
            _journeys.HeroLateral = heroLateral;
            _journeys.RouteIndex = routeIndex;
            _journeys.RouteStepsApplied++;
            if (_journeys.HeroStreet != previousStreet)
            {
                _journeys.HeroCarrier?.Walk(_journeys.HeroStreet > previousStreet ? Vector2.Up : Vector2.Down);
                _journeys.HeroWalking = true;
                _journeys.DepthTarget = _journeys.HeroStreet;
                moved = true;
            }
            else if (!Mathf.IsEqualApprox(_journeys.HeroLateral, previousLateral))
            {
                _journeys.HeroCarrier?.Walk(_journeys.HeroLateral > previousLateral ? Vector2.Right : Vector2.Left);
                _journeys.HeroWalking = true;
                moved = true;
            }
        }
        if (moved)
        {
            TrampleHeroTile();
            QueueRedraw();
        }
        if (_journeys.RouteIndex >= _journeys.Route.Count) CompleteRoute();
    }

    /// <summary>
    /// The original cadence gait, kept for routes that are not domain journeys:
    /// the gather walk and ambient wandering, neither of which has an arrival
    /// tick to be paced against.
    /// </summary>
    private void AdvanceHeroRouteOneStep()
    {
        if (_journeys.DepthTarget.HasValue) return; // mid street transition
        if (_journeys.Route is null || _journeys.RouteIndex >= _journeys.Route.Count)
        {
            CompleteRoute();
            return;
        }
        StreetRoutePlanner.Waypoint waypoint = _journeys.Route[_journeys.RouteIndex];
        if (waypoint.Street != _journeys.HeroStreet)
        {
            int direction = Mathf.Sign(waypoint.Street - _journeys.HeroStreet);
            _journeys.HeroCarrier?.Walk(direction > 0 ? Vector2.Up : Vector2.Down);
            _journeys.HeroWalking = true;
            _journeys.HeroStreet += direction;
            _journeys.DepthTarget = _journeys.HeroStreet;
            TrampleHeroTile();
            QueueRedraw();
            return;
        }
        if (Mathf.Abs(waypoint.Lateral - _journeys.HeroLateral) >= 1f)
        {
            float direction = Mathf.Sign(waypoint.Lateral - _journeys.HeroLateral);
            _journeys.HeroCarrier?.Walk(direction > 0f ? Vector2.Right : Vector2.Left);
            _journeys.HeroWalking = true;
            _journeys.HeroLateral = Mathf.MoveToward(_journeys.HeroLateral, waypoint.Lateral, PixelMotion.StepPixels);
            TrampleHeroTile();
            QueueRedraw();
            return;
        }
        _journeys.RouteIndex++;
        if (_journeys.RouteIndex >= _journeys.Route.Count) CompleteRoute();
    }

    private void CompleteRoute()
    {
        if (_journeys.HeroAmbientRoute)
        {
            _journeys.HeroAmbientRoute = false;
            _journeys.Route = null;
            _journeys.RouteIndex = 0;
            _journeys.HeroWalking = false;
            _journeys.HeroNextAmbientDecisionTick = _controller.CurrentTick + 30;
            _journeys.HeroCarrier?.Idle(Vector2.Down);
            return;
        }
        _journeys.Route = null;
        _journeys.RouteIndex = 0;
        _journeys.HeroWalking = false;
        _journeys.RoutePacingStartTick = null;
        if (_journeys.PendingReturnHome)
        {
            _journeys.PendingReturnHome = false;
            CitizenId founderId = _controller.GetHeroId()!.Value;
            BuildingId? homeId = _controller.GetPrimaryHomeId();
            LogCitizenTravel("arrived", founderId, homeId, returningHome: true);
            // Arrival means the citizen crossed the threshold. The macro
            // carrier disappears inside; a later order remounts the same
            // flyweight before planning its next route. The domain already
            // recorded the arrival on the tick this route was paced to end on.
            _journeys.HeroCarrier?.SetState(CitizenSpriteCarrier.VisualState.Hidden);
            return;
        }
        if (_journeys.PendingAssignment is BuildingId workplace)
        {
            LogCitizenTravel(
                "arrived",
                _controller.GetHeroId()!.Value,
                workplace,
                returningHome: false);
            _journeys.PendingAssignment = null;
            // Facing "into" the workplace (deeper on this row), matching
            // the gather pose's own orientation once arrived.
            _journeys.HeroCarrier?.Idle(Vector2.Up);
            return;
        }
        (int ForestId, int UnitId)? pending = _journeys.PendingGather;
        _journeys.PendingGather = null;
        if (pending is null)
        {
            _journeys.HeroCarrier?.Idle(Vector2.Down);
            return;
        }
        // The tree stands behind the hero's road (deeper), so swing away
        // from the viewer, then settle back to idle after the one-shot.
        _journeys.HeroCarrier?.Slash(Vector2.Up);
        TreeBox? target = _renderer.Trees.FirstOrDefault(tree =>
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
            if (IsInstanceValid(this) && _journeys.Route is null && IsInstanceValid(_journeys.HeroCarrier))
            {
                _journeys.HeroCarrier?.Idle(Vector2.Up);
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
        foreach (PlotBox plot in _renderer.Plots)
        {
            if (plot.Kind != BuildingKind.Home) continue;
            if (!plot.IsUnderConstruction)
            {
                storagePlot = plot;
                break;
            }
        }
        if (storagePlot is null
            && _controller.GetFoundingStorageBuildingId() is BuildingId foundingBuildingId)
        {
            foreach (PlotBox plot in _renderer.Plots)
            {
                if (plot.BuildingId != foundingBuildingId.Value) continue;
                storagePlot = plot;
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

        if (IsInstanceValid(_journeys.HeroCarrier))
        {
            return new ResourceFeedbackAnchor(
                Vector2.Zero,
                _journeys.HeroCarrier,
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
        if (_camera.VerticalPanDirection == direction) return;
        _camera.VerticalPanDirection = direction;
        _camera.VerticalPanHoldSeconds = 0f;
        _camera.VerticalPanRepeatAccumulator = 0f;
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
        if (direction != _camera.VerticalPanDirection)
        {
            BeginVerticalCameraPan(direction);
            return;
        }

        _camera.VerticalPanHoldSeconds += PixelMotion.CadenceSeconds;
        _camera.VerticalPanRepeatAccumulator += PixelMotion.CadenceSeconds;
        float repeatSeconds = VerticalPanRepeatSeconds(_camera.VerticalPanHoldSeconds);
        if (_camera.VerticalPanRepeatAccumulator < repeatSeconds) return;
        _camera.VerticalPanRepeatAccumulator -= repeatSeconds;
        PanCameraStreet(direction);
    }

    private void ResetVerticalCameraPanHold()
    {
        _camera.VerticalPanDirection = 0;
        _camera.VerticalPanHoldSeconds = 0f;
        _camera.VerticalPanRepeatAccumulator = 0f;
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
    /// <see cref="_journeys.Route"/> is cleared, undoing the arrival Slash animation
    /// before the player ever sees it.</description></item>
    /// </list>
    /// <see cref="_journeys.HeroIsGatheringOutsideHome"/> is cleared once a real
    /// domain-tracked journey takes over (<see cref="BeginWalkToAssignment"/>/
    /// <see cref="BeginWalkHome"/>/departing on an expedition).
    /// </summary>
    private void EnsureHeroCarrierReadyToMove()
    {
        _journeys.HeroIsGatheringOutsideHome = true;
        if (_journeys.HeroCarrier is null) return;
        if (_journeys.HeroCarrier.State != CitizenSpriteCarrier.VisualState.Macro)
        {
            _journeys.HeroCarrier.SetState(CitizenSpriteCarrier.VisualState.Macro);
        }
        _journeys.HeroCarrier.CancelMotion();
    }

    /// <summary>
    /// Free camera's own manual depth step — an observer, not a body, so
    /// it never checks citizen obstacle clearance (design bible §04: free
    /// pan is always available).
    /// </summary>
    private void StepFreeCameraStreet(int direction)
    {
        int nextStreet = Mathf.Clamp(_camera.FreeCameraStreet + direction, 0, _streetCount - 1);
        if (nextStreet == _camera.FreeCameraStreet) return;
        _camera.FreeCameraStreet = nextStreet;
        _camera.CameraDepthTarget = _camera.FreeCameraStreet;
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
            _camera.FreeCameraLateral + direction * PixelMotion.StepPixels,
            -_lateralHalfWidthPx,
            _lateralHalfWidthPx);
        if (next == _camera.FreeCameraLateral) return false;
        _camera.FreeCameraLateral = next;
        QueueRedraw();
        return true;
    }

    private void EnsureFreeCameraForManualPan()
    {
        if (_camera.CameraFollowsHero) SetCameraFollowsHero(false);
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
    /// building, this stops treating <see cref="_journeys.HeroStreet"/>/
    /// <see cref="_journeys.HeroLateral"/> as free-roam camera state and instead
    /// walks them to the workplace's own calle/lateral — matching the flat
    /// view's model, where an assigned worker's macro-view position is
    /// their workplace, not wherever they last wandered. A NEW assignment
    /// (tracked via <see cref="_journeys.LastKnownAssignment"/>) triggers exactly
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
            _journeys.LastKnownAssignment = null;
            _journeys.LastKnownHeroLocation = null;
            _journeys.HeroIsGatheringOutsideHome = false;
            return;
        }
        if (!Visible && heroState.Location != CitizenLocation.InTransit && _journeys.Route is null) return;
        _journeys.HeroCarrier = CitizenSpriteBank.Instance.GetOrCreate(
            hero.Id, hero.Lineage, hero.Gender, hero.Appearance);
        CitizenSpriteBank.Instance.Mount(_journeys.HeroCarrier, this);
        // The city sits on the site the founder's fall reached. Resolved here
        // because this is where the founder first becomes known to the view;
        // it changes nothing mechanical, only which ground tiles are drawn.
        _renderer.SetGroundBiomeForLineage(hero.Lineage);
        if (!_journeys.HeroPositionInitialized)
        {
            _journeys.HeroStreet = FoundingLayout.InitialParcelRow
                * ParcelGrid.ConstructionRowsPerParcel
                + FoundingLayout.FounderRowWithinParcel;
            float totalFrontageColumns =
                _worldParcelColumns * ParcelGrid.FrontageColumnsPerParcel;
            float frontageCenter = FoundingLayout.InitialParcelColumn
                * ParcelGrid.FrontageColumnsPerParcel
                + FoundingLayout.FounderFrontageColumnWithinParcel
                + 0.5f;
            _journeys.HeroLateral = (frontageCenter - totalFrontageColumns * 0.5f) * TileUnitPx;
            _journeys.DepthAnchor = _journeys.HeroStreet;
            _journeys.HeroPositionInitialized = true;
        }
        BuildingId? currentAssignment = heroState.CurrentAssignment;
        CitizenLocation heroLocation = heroState.Location;
        if (heroLocation == CitizenLocation.InTransit && _journeys.HeroAmbientRoute)
        {
            _journeys.HeroAmbientRoute = false;
            _journeys.Route = null;
            _journeys.RouteIndex = 0;
            _journeys.HeroWalking = false;
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
            hasRoute: _journeys.Route is not null,
            pendingReturnHome: _journeys.PendingReturnHome,
            isGatheringOutsideHome: _journeys.HeroIsGatheringOutsideHome)
            && !shouldRemainVisibleAtHome)
        {
            _journeys.HeroCarrier.CancelMotion();
            _journeys.HeroCarrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
            _journeys.LastKnownAssignment = null;
            _journeys.LastKnownHeroLocation = heroLocation;
            return;
        }
        if (currentAssignment.HasValue && heroLocation == CitizenLocation.AtWork)
        {
            _journeys.HeroCarrier.CancelMotion();
            _journeys.HeroCarrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
            _journeys.LastKnownAssignment = currentAssignment;
            _journeys.LastKnownHeroLocation = heroLocation;
            _journeys.PendingAssignment = null;
            _journeys.PendingReturnHome = false;
            _journeys.Route = null;
            _journeys.RouteIndex = 0;
            _journeys.HeroWalking = false;
            return;
        }
        if (_journeys.HeroCarrier.State != CitizenSpriteCarrier.VisualState.Macro)
        {
            // Same leftover-GoTo hazard as the ambient worker loop below:
            // a building-detail exit animation interrupted before its
            // completion callback fired would otherwise keep stepping the
            // carrier toward an interior-space target while this class's
            // own UpdateHeroVisual snaps it back to the macro position
            // every frame.
            _journeys.HeroCarrier.CancelMotion();
            _journeys.HeroCarrier.SetState(CitizenSpriteCarrier.VisualState.Macro);
            _journeys.HeroCarrier.Idle(Vector2.Down);
        }

        if (heroLocation == CitizenLocation.AtHome && _journeys.Route is null)
        {
            PlotBox? shelter = FindHomePlot();
            if (shelter is { } home
                && (_journeys.LastKnownHeroLocation != CitizenLocation.AtHome
                    || heroState.Activity == CitizenRoutineActivity.Recovering))
            {
                BuildingVisualAnchors anchors = VisualAnchorsFor(home);
                StreetVisualAnchor anchor = heroState.Activity == CitizenRoutineActivity.Recovering
                    ? anchors.Waiting
                    : anchors.Entrance;
                _journeys.HeroStreet = anchor.Street;
                _journeys.HeroLateral = anchor.Lateral;
                _journeys.DepthAnchor = _journeys.HeroStreet;
                _journeys.DepthTarget = null;
            }
            if (mayWander && shelter is { } wanderAnchor)
            {
                TryStartHeroAmbientRoute(wanderAnchor);
            }
        }

        if (currentAssignment != _journeys.LastKnownAssignment)
        {
            _journeys.LastKnownAssignment = currentAssignment;
        }
        // The domain can reverse a journey underneath the drawn route: a trip
        // that comes due after the workday turns back toward Home, keeping the
        // standing order. Before A2 the view learned this by having its arrival
        // refused; now that arrivals are not the view's to claim, it has to
        // notice that the route it is drawing points the wrong way and drop it,
        // so the branches below can plan the journey the domain actually has.
        if (heroLocation == CitizenLocation.InTransit
            && _journeys.Route is not null
            && !_journeys.HeroAmbientRoute
            && _journeys.PendingGather is null
            && RouteContradictsDomain(
                heroState.IsReturningHome,
                routeTargetsAssignment: _journeys.PendingAssignment is not null,
                routeTargetsHome: _journeys.PendingReturnHome))
        {
            _journeys.Route = null;
            _journeys.RouteIndex = 0;
            _journeys.RoutePacingStartTick = null;
            _journeys.RouteStepsApplied = 0;
            _journeys.PendingAssignment = null;
            _journeys.PendingReturnHome = false;
            _journeys.HeroWalking = false;
        }

        if (currentAssignment.HasValue
            && heroLocation == CitizenLocation.InTransit
            && heroState.IsReturningHome
            && _journeys.Route is null
            && !_journeys.PendingReturnHome)
        {
            BeginWalkHome(heroState.TransitStartedAtTick);
        }
        else if (ShouldBeginWorkRoute(
                currentAssignment,
                heroState.Location,
                heroState.IsReturningHome,
                hasRoute: _journeys.Route is not null)
            && currentAssignment is BuildingId unsettledWorkplace
            && _journeys.Route is null)
        {
            // A view transition can replace the flyweight carrier's previous
            // movement callback. If the domain still says InTransit after the
            // visual route disappeared, resume/reconcile instead of leaving
            // the citizen permanently assigned but non-productive.
            BeginWalkToAssignment(unsettledWorkplace, heroState.TransitStartedAtTick);
        }
        _journeys.LastKnownHeroLocation = heroLocation;
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

    /// <summary>
    /// True when the route currently being drawn heads the opposite way from
    /// the journey the domain says the citizen is on. The drawn route is a
    /// projection, so when the two disagree the drawing is what is wrong.
    /// </summary>
    internal static bool RouteContradictsDomain(
        bool isReturningHome,
        bool routeTargetsAssignment,
        bool routeTargetsHome) =>
        (isReturningHome && routeTargetsAssignment)
        || (!isReturningHome && routeTargetsHome);

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
                && (!_journeys.Journeys.TryGetValue(citizen.Id.Value, out CitizenJourney? existing)
                    || existing.Route is null))
            {
                continue;
            }
            ReconcileCitizenJourney(citizen, homePlot);
        }

        List<int>? staleCitizenIds = null;
        foreach (int citizenId in _journeys.Journeys.Keys)
        {
            if (activeCitizenIds.Contains(citizenId)) continue;
            (staleCitizenIds ??= new List<int>()).Add(citizenId);
        }
        if (staleCitizenIds is not null)
        {
            foreach (int citizenId in staleCitizenIds)
            {
                CitizenJourney journey = _journeys.Journeys[citizenId];
                journey.Carrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
                _journeys.Journeys.Remove(citizenId);
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
            if (_journeys.Journeys.TryGetValue(citizen.Id.Value, out CitizenJourney? away))
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
                    && _controller.CurrentTick >= journey.NextAmbientDecisionTick
                    && homePlot is { } ambientAnchor)
                {
                    StartAmbientJourney(journey, ambientAnchor, _controller.CurrentTick);
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
                $"returningHome={citizen.IsReturningHome}, tick={_controller.CurrentTick}.");
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
                _controller.CurrentTick);
        }
        ShowJourneyCarrier(journey, Vector2.Up);
    }

    private CitizenJourney GetOrCreateCitizenJourney(
        CityMacroSnapshot.CitizenItem citizen,
        PlotBox? homePlot,
        PlotBox? assignmentPlot)
    {
        if (_journeys.Journeys.TryGetValue(citizen.Id.Value, out CitizenJourney? existing))
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
        var created = new CitizenJourney(_journeys, citizen.Id, carrier, street, lateral);
        _journeys.Journeys.Add(citizen.Id.Value, created);
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
        journey.PacingStartTick = transitStartedAtTick;
        journey.TotalSteps = CountRouteSteps(journey.Route, journey.Street, journey.Lateral);
        journey.StepsApplied = 0;
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
            journey.StepsApplied = reconstructed.StepsApplied;
        }
        LogCitizenTravel("started", journey.CitizenId, destination, returningHome);
    }

    /// <summary>
    /// Rebuilds a presentation position from semantic transit timing. The
    /// result is ephemeral and never enters WorldSave; it only prevents a load
    /// or view re-entry from replaying the already elapsed part of a journey.
    ///
    /// <para>
    /// This is also the live pacing rule, not only the restore rule: a route is
    /// walked by asking where the world clock says the citizen should be, so
    /// resuming a saved journey and walking a fresh one are the same
    /// calculation. See <see cref="PacedRouteSteps"/>.
    /// </para>
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
            return new ReconstructedRoutePosition(startStreet, startLateral, 0, 0);
        }

        int totalSteps = CountRouteSteps(route, startStreet, startLateral);
        // Reconstruction stops one step short on purpose: re-entering the view
        // must never be what finishes a journey. Completion belongs to the
        // clock, and the clock is read every frame by the pacing path.
        int stepsToApply = Math.Min(
            Math.Max(0, totalSteps - 1),
            PacedRouteSteps(totalSteps, elapsedTicks, tickPhase: 0d, expectedDurationTicks));
        int street = startStreet;
        float lateral = startLateral;
        int routeIndex = 0;
        int applied = 0;
        for (int step = 0; step < stepsToApply && routeIndex < route.Count; step++)
        {
            AdvanceReconstructedRouteStep(route, ref street, ref lateral, ref routeIndex);
            applied++;
        }
        return new ReconstructedRoutePosition(street, lateral, routeIndex, applied);
    }

    /// <summary>
    /// How many route steps should have been walked by now, given the domain's
    /// own journey window. This is what makes the drawn walk a projection of
    /// world time rather than a second opinion about it: the route is spread
    /// across <paramref name="expectedDurationTicks"/> world ticks, so 2x and 4x
    /// speed it up for free and a dropped frame merely catches up next frame.
    /// </summary>
    /// <param name="tickPhase">
    /// Fraction of the current world tick already elapsed, so motion stays
    /// smooth between one-second ticks. Purely cosmetic; it can only move the
    /// citizen within a step of where whole ticks already put them.
    /// </param>
    internal static int PacedRouteSteps(
        int totalSteps,
        int elapsedTicks,
        double tickPhase,
        int expectedDurationTicks)
    {
        if (totalSteps <= 0) return 0;
        if (expectedDurationTicks <= 0) return totalSteps;
        double progress = Math.Clamp(
            (elapsedTicks + Math.Clamp(tickPhase, 0d, 1d)) / expectedDurationTicks, 0d, 1d);
        return Math.Min(totalSteps, (int)Math.Floor(totalSteps * progress));
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
        int currentTick = _controller.CurrentTick;
        double tickPhase = _controller.CurrentTickPhase;
        foreach ((int citizenId, CitizenJourney journey) in _journeys.Journeys.ToArray())
        {
            if (journey.Route is null) continue;
            if (journey.PacingStartTick is int startedAt)
            {
                advancedAnyJourney = true;
                AdvanceJourneyToStep(
                    new CitizenId(citizenId),
                    journey,
                    PacedRouteSteps(
                        journey.TotalSteps,
                        currentTick - startedAt,
                        tickPhase,
                        CityEconomyRules.AbstractTravelTicks));
                continue;
            }
            if (journey.DepthTarget.HasValue) continue;
            advancedAnyJourney = true;
            AdvanceAmbientJourneyOneStep(new CitizenId(citizenId), journey);
        }
        if (advancedAnyJourney) QueueRedraw();
    }

    /// <summary>
    /// Walks one citizen forward until the paced step budget is spent. The
    /// budget comes from the domain's journey window, so the drawn arrival
    /// lands on the tick the domain already chose.
    /// </summary>
    private void AdvanceJourneyToStep(CitizenId citizenId, CitizenJourney journey, int targetSteps)
    {
        if (journey.Route is null) return;
        while (journey.StepsApplied < targetSteps && journey.RouteIndex < journey.Route.Count)
        {
            int previousStreet = journey.Street;
            float previousLateral = journey.Lateral;
            int street = journey.Street;
            float lateral = journey.Lateral;
            int routeIndex = journey.RouteIndex;
            AdvanceReconstructedRouteStep(journey.Route, ref street, ref lateral, ref routeIndex);
            journey.Street = street;
            journey.Lateral = lateral;
            journey.RouteIndex = routeIndex;
            journey.StepsApplied++;
            if (journey.Street != previousStreet)
            {
                journey.Carrier.Walk(journey.Street > previousStreet ? Vector2.Up : Vector2.Down);
                journey.Walking = true;
                journey.DepthTarget = journey.Street;
            }
            else if (!Mathf.IsEqualApprox(journey.Lateral, previousLateral))
            {
                journey.Carrier.Walk(journey.Lateral > previousLateral ? Vector2.Right : Vector2.Left);
                journey.Walking = true;
            }
        }
        if (journey.RouteIndex >= journey.Route.Count)
        {
            CompleteCitizenJourney(citizenId, journey);
        }
    }

    /// <summary>
    /// The original cadence gait, kept for ambient wandering — it draws no
    /// domain journey and therefore has no arrival tick to be paced against.
    /// </summary>
    private void AdvanceAmbientJourneyOneStep(CitizenId citizenId, CitizenJourney journey)
    {
        if (journey.Route is null) return;
        if (journey.RouteIndex >= journey.Route.Count)
        {
            if (journey.IsAmbient) CompleteAmbientJourney(journey);
            else CompleteCitizenJourney(citizenId, journey);
            return;
        }
        StreetRoutePlanner.Waypoint waypoint = journey.Route[journey.RouteIndex];
        if (waypoint.Street != journey.Street)
        {
            int direction = Math.Sign(waypoint.Street - journey.Street);
            journey.Carrier.Walk(direction > 0 ? Vector2.Up : Vector2.Down);
            journey.Walking = true;
            journey.Street += direction;
            journey.DepthTarget = journey.Street;
            return;
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
            return;
        }
        journey.RouteIndex++;
        if (journey.RouteIndex >= journey.Route.Count)
        {
            if (journey.IsAmbient) CompleteAmbientJourney(journey);
            else CompleteCitizenJourney(citizenId, journey);
        }
    }

    private void CompleteCitizenJourney(CitizenId citizenId, CitizenJourney journey)
    {
        BuildingId? destination = journey.Destination;
        bool returningHome = journey.ReturningHome;
        StopJourney(journey);
        // The domain recorded this arrival on the tick the route was paced to
        // end on. Nothing here can confirm, delay or deny it — only draw it.
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
        // Wandering draws no domain journey, so it has no window to be paced to.
        journey.PacingStartTick = null;
        journey.TotalSteps = 0;
        journey.StepsApplied = 0;
        journey.NextAmbientDecisionTick = currentTick + 20 + phase % 31;
    }

    private void CompleteAmbientJourney(CitizenJourney journey)
    {
        journey.Route = null;
        journey.RouteIndex = 0;
        journey.IsAmbient = false;
        journey.Walking = false;
        journey.DepthTarget = null;
        journey.PacingStartTick = null;
        journey.NextAmbientDecisionTick = _controller.CurrentTick + 30;
        journey.Carrier.Idle(Vector2.Down);
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
        foreach (CitizenJourney journey in _journeys.Journeys.Values)
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
    /// get from <see cref="ContextInspector"/>.
    /// </summary>
    private void UpdateCitizenHitRects()
    {
        if (_journeys.HeroCarrier is not null && IsVisibleMacroCarrier(_journeys.HeroCarrier))
        {
            _hitRects.CitizenClickableRects.Add((CitizenHoverRect(_journeys.HeroCarrier), _journeys.HeroCarrier.Id));
        }
        foreach (CitizenJourney journey in _journeys.Journeys.Values)
        {
            if (!IsVisibleMacroCarrier(journey.Carrier)) continue;
            _hitRects.CitizenClickableRects.Add((CitizenHoverRect(journey.Carrier), journey.CitizenId));
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
        journey.PacingStartTick = null;
        journey.TotalSteps = 0;
        journey.StepsApplied = 0;
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
        foreach (PlotBox plot in _renderer.Plots)
        {
            if (plot.Kind == BuildingKind.Home && !plot.IsUnderConstruction) return plot;
        }
        return null;
    }

    private PlotBox? FindPlot(BuildingId buildingId)
    {
        foreach (PlotBox plot in _renderer.Plots)
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
            $"returningHome={returningHome}, tick={_controller.CurrentTick}, " +
            $"activity={debug?.Routine.Activity}, context={debug?.Routine.ContextLocation}, " +
            $"blocker={debug?.Routine.BlockReason}, started={debug?.Routine.ActivityStartedAtTick}, " +
            $"expected={debug?.Routine.ExpectedCompletionTick}, next={debug?.Routine.NextTransitionTick}.");
    }
    /// <summary>
    /// Routes the hero from wherever they currently are to their new
    /// workplace's calle/lateral, reusing the same quantized
    /// <see cref="StreetRoutePlanner"/>/<see cref="_journeys.Route"/> machinery as
    /// gather. Once the route completes, <see cref="CompleteRoute"/> just
    /// settles them into an idle "at work" pose instead of gathering wood.
    /// </summary>
    private void BeginWalkToAssignment(BuildingId workplace, int? transitStartedAtTick = null)
    {
        _journeys.HeroAmbientRoute = false;
        _journeys.HeroIsGatheringOutsideHome = false;
        // The canonical flyweight may still carry a GoTo started by the
        // building-detail slot where the assignment was requested. Once the
        // macro view takes route ownership, that interior movement must stop:
        // otherwise CitizenSpriteCarrier._Process and UpdateHeroVisual write
        // the same Position concurrently and the sprite oscillates just short
        // of the entrance without either completion callback winning.
        _journeys.HeroCarrier?.CancelMotion();
        PlotBox? target = null;
        foreach (PlotBox plot in _renderer.Plots)
        {
            if (plot.BuildingId != workplace.Value) continue;
            target = plot;
            break;
        }
        if (target is null)
        {
            GD.PushWarning(
                $"Citizen route target missing: citizen={_controller.GetHeroId()!.Value.Value}, " +
                $"assignment={workplace.Value}, tick={_controller.CurrentTick}.");
            return;
        }
        _journeys.PendingGather = null;
        _journeys.PendingReturnHome = false;
        _journeys.PendingAssignment = workplace;
        int entranceStreet = WorkplaceEntranceStreet(target.Value.Street);
        _journeys.Route = PlanCitizenRoute(_journeys.HeroStreet, _journeys.HeroLateral, entranceStreet, target.Value.LateralOffset);
        _journeys.RouteIndex = 0;
        AnchorHeroRoutePacing(transitStartedAtTick);
        LogCitizenTravel(
            "started",
            _controller.GetHeroId()!.Value,
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
        _journeys.HeroAmbientRoute = false;
        _journeys.HeroIsGatheringOutsideHome = false;
        // Route ownership can also transfer from an interior exit animation.
        // Keep exactly one position writer for the shared citizen carrier.
        _journeys.HeroCarrier?.CancelMotion();
        PlotBox? shelter = null;
        foreach (PlotBox plot in _renderer.Plots)
        {
            if (plot.Kind != BuildingKind.Home || plot.IsUnderConstruction) continue;
            shelter = plot;
            break;
        }

        _journeys.PendingGather = null;
        _journeys.PendingAssignment = null;
        _journeys.RouteIndex = 0;
        _journeys.HeroWalking = false;
        if (shelter is null)
        {
            _journeys.PendingReturnHome = false;
            _journeys.Route = null;
            _journeys.HeroCarrier?.Idle(Vector2.Down);
            GD.PushWarning(
                $"Citizen return route unresolved: citizen={_controller.GetHeroId()!.Value.Value}, " +
                $"tick={_controller.CurrentTick}, reason=no completed Shelter.");
            return;
        }

        _journeys.PendingReturnHome = true;
        int entranceStreet = WorkplaceEntranceStreet(shelter.Value.Street);
        _journeys.Route = PlanCitizenRoute(
            _journeys.HeroStreet,
            _journeys.HeroLateral,
            entranceStreet,
            shelter.Value.LateralOffset);
        AnchorHeroRoutePacing(transitStartedAtTick);
        LogCitizenTravel(
            "started",
            _controller.GetHeroId()!.Value,
            new BuildingId(shelter.Value.BuildingId),
            returningHome: true);
    }

    /// <summary>
    /// Binds a freshly planned founder route to the domain journey that caused
    /// it: records the pacing window, counts the route, and — when the journey
    /// is already part-elapsed (a load, or re-entering the view) — skips ahead
    /// to where the clock says the founder already is.
    /// </summary>
    private void AnchorHeroRoutePacing(int? transitStartedAtTick)
    {
        _journeys.RoutePacingStartTick = null;
        _journeys.RouteTotalSteps = 0;
        _journeys.RouteStepsApplied = 0;
        if (_journeys.Route is null || transitStartedAtTick is not int startedAt) return;

        _journeys.RoutePacingStartTick = startedAt;
        _journeys.RouteTotalSteps = CountRouteSteps(_journeys.Route, _journeys.HeroStreet, _journeys.HeroLateral);
        if (_controller.CurrentTick <= startedAt) return;

        ReconstructedRoutePosition reconstructed = ReconstructRouteProgress(
            _journeys.Route,
            _journeys.HeroStreet,
            _journeys.HeroLateral,
            _controller.CurrentTick - startedAt,
            CityEconomyRules.AbstractTravelTicks);
        _journeys.HeroStreet = reconstructed.Street;
        _journeys.HeroLateral = reconstructed.Lateral;
        _journeys.DepthAnchor = reconstructed.Street;
        _journeys.DepthTarget = null;
        _journeys.RouteIndex = reconstructed.RouteIndex;
        _journeys.RouteStepsApplied = reconstructed.StepsApplied;
    }

    private void TryStartHeroAmbientRoute(PlotBox anchor)
    {
        int currentTick = _controller.CurrentTick;
        if (_journeys.Route is not null || currentTick < _journeys.HeroNextAmbientDecisionTick) return;
        int founderId = _controller.GetHeroId()?.Value ?? 1;
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
        _journeys.Route = PlanCitizenRoute(
            _journeys.HeroStreet,
            _journeys.HeroLateral,
            targetStreet,
            targetLateral);
        _journeys.RouteIndex = 0;
        _journeys.HeroAmbientRoute = true;
        // Wandering answers to nothing in the domain; it keeps the cadence gait.
        AnchorHeroRoutePacing(null);
        _journeys.HeroNextAmbientDecisionTick = currentTick + 20 + phase % 31;
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
        List<StreetRoutePlanner.Waypoint>? navmeshRoute = _journeys.NavmeshPlanner?.Plan(
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
        if (_journeys.HeroCarrier is null
            || !IsInstanceValid(_journeys.HeroCarrier)
            || _journeys.HeroCarrier.GetParent() != this
            || _journeys.HeroCarrier.State != CitizenSpriteCarrier.VisualState.Macro)
        {
            return;
        }
        float depth = _journeys.DepthAnchor - CameraDepthAnchor;
        _journeys.HeroCarrier.Visible = IsProjectedDepthVisible(depth);
        if (!_journeys.HeroCarrier.Visible) return;
        float lateralOffset = _journeys.HeroLateral - CameraLateral;
        (Vector2 position, Vector2 scale) = ProjectDepth(depth, lateralOffset);
        _journeys.HeroCarrier.Scale =
            CitizenSpriteCarrier.ScaleForState(CitizenSpriteCarrier.VisualState.Macro) * scale;
        _journeys.HeroCarrier.ZIndex = CitizenZ(depth);
        _journeys.HeroCarrier.Position = PixelMotion.Snap(new Vector2(
            position.X,
            position.Y - HeroFootOffsetMacroPx * scale.Y));
    }

    public override void _Draw()
    {
        _renderer.Draw(this, _hitRects);
        SyncStreetBandLayers();
        UpdateHeroVisual();
        UpdateCitizenJourneyVisuals();
        UpdateCitizenHitRects();
    }

    /// <summary>
    /// Vertical spacing between consecutive street bands in z. Leaves room for
    /// a citizen to land between two bands rather than tying with one.
    /// </summary>
    private const int BandZStep = MacroViewConstants.BandZStep;

    /// <summary>
    /// Turns a projected depth into a draw order. Nearer to camera means a
    /// larger z, and it is the <em>same</em> function for street bands and for
    /// citizen carriers — which is the whole point: before this they were
    /// ordered on two incomparable axes, so a citizen always won.
    /// </summary>
    private int DepthToZ(float depth) =>
        MacroProjectionHelpers.DepthToZ(depth, _streetCount, CameraDepthAnchor);

    /// <summary>
    /// Draw order for a citizen. A citizen stands on the walkable front band
    /// of its lot, in front of whatever that lot holds, so it takes its own
    /// band's order plus one step. Anything on a nearer band still wins,
    /// which is the case that was broken.
    /// </summary>
    private int CitizenZ(float depth) =>
        MacroProjectionHelpers.CitizenZ(depth, _streetCount, CameraDepthAnchor);

    /// <summary>
    /// Creates one obstacle layer per street and keeps their z in step with
    /// the camera. Layers are reused across redraws; only their count follows
    /// <c>_streetCount</c>. The renderer owns the layer list now; this
    /// method is a thin forwarder so the view's _Draw body keeps its shape.
    /// </summary>
    private void SyncStreetBandLayers() =>
        _renderer.SyncBandLayers(_streetCount, CameraDepthAnchor);

    /// <summary>Brings the layer count in line with the street count, off-frame.
    /// The renderer owns the rebuild pass; this is a forwarder.</summary>
    private void RebuildStreetBandLayers()
    {
        _renderer.RequestBandLayerRebuild(_streetCount);
        QueueRedraw();
    }

    /// <summary>
    /// Every lateral offset is relative to the hero's own
    /// <see cref="_journeys.HeroLateral"/> — the vanishing point follows the viewer,
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
    /// <see cref="_journeys.HeroLateral"/>, which is why it only became obvious once
    /// walking sideways.
    /// </summary>
    /// <summary>
    /// The ground of one street: floor, territory tint and placement lots.
    /// Stays on the view's own canvas because terrain is always behind
    /// everything; the obstacles that need depth-ordering against citizens
    /// live in <see cref="DrawStreetObstacles"/> instead.
    /// </summary>
    internal void DrawStreetGround(int street)
    {
        float depth = street - CameraDepthAnchor;
        _renderer.DrawTiledFloor(this, street, depth, CameraLateral);
        // Territory tint per parcel column: visualises the locked /
        // reconnoitred / route-secured / available state so the player
        // can see what the world still hides and what an expedition
        // actually unlocked. Drawn before buildings/trees so the sprites
        // sit on top of the band; drawn after the floor so the tint
        // reads as overlay, not as the ground itself.
        DrawParcelTerritoryTints(street, depth);

        if (_placement.PlacementActive)
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
    private void DrawStreetObstacles(CanvasItem canvas, int street) =>
        _renderer.DrawStreetObstacles(this, canvas, street, CameraDepthAnchor, CameraLateral, _hitRects);

    internal void DrawCultivationSite(CanvasItem canvas, Rect2 rect, CultivationPlotState state) =>
        _renderer.DrawCultivationSite(canvas, rect, state);

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
    internal void DrawNaturalResourceUnit(CanvasItem canvas, TreeBox unit, Rect2 rect) =>
        _renderer.DrawNaturalResourceUnit(canvas, unit, rect, _terrainAtlas);

    internal void DrawStorageFullBadge(CanvasItem canvas, Rect2 buildingRect, PlotBox plot) =>
        _renderer.DrawStorageFullBadge(canvas, buildingRect, plot, _storageFullIcon, _hitRects);


    /// <summary>Depth at which sprites anchor within their calle's lot: half a tile
    /// behind the calle's own near edge, i.e. near the lot's front rather than its back.</summary>
    private static float AnchorDepth(float streetDepth) =>
        MacroProjectionHelpers.AnchorDepth(streetDepth);

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
        TerrainAtlas.GroundBiome biome = _renderer.GroundBiome;
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
                if (!_renderer.ParcelTerritory.ContainsKey((parcelRow, parcelColumn))) continue;
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

                float wear = tileRow == 0 ? _renderer.WearAt(street, tileIndex) : 0f;
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
    // A4: ground biome moved to MacroStreetRenderer.

    /// <summary>
    /// Which lateral tile index (the same granularity <see cref="DrawTiledFloor"/>
    /// tiles the floor at) a global lateral offset falls into — used to mark
    /// the hero's own footprint for <see cref="_terrainWear"/>, independent
    /// of camera position (same "global" lateral space <c>_journeys.HeroLateral</c>
    /// and <see cref="StreetRoutePlanner"/> already use).
    /// </summary>
    private int TileIndexAtLateral(float lateral)
    {
        int totalTiles = Mathf.RoundToInt(2f * _lateralHalfWidthPx / TileUnitPx);
        int index = Mathf.FloorToInt((lateral + _lateralHalfWidthPx) / TileUnitPx);
        return Mathf.Clamp(index, 0, totalTiles - 1);
    }

    /// <summary>Marks the tile under the hero's current feet as trampled (S-1.3 phase 2).</summary>
    private void TrampleHeroTile() =>
        _renderer.TrampleHeroTile(_journeys.HeroStreet, TileIndexAtLateral(_journeys.HeroLateral));

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

    private static float SnapPixel(float value) =>
        MacroProjectionHelpers.SnapPixel(value, PixelStepPx);

    private Texture2D? GetBuildingTexture(BuildingKind kind) =>
        _renderer.GetBuildingTexture(kind);

    /// <summary>
    /// Renders each available three-column frontage window as its real 3x3
    /// ground footprint. Overlapping windows stay lightly filled and a click
    /// resolves to the nearest projected center. Every window projects its
    /// near and far edges independently,
    /// so the blueprint shares the terrain's vanishing point instead of
    /// becoming an axis-aligned screen rectangle after projecting only its
    /// centre.
    /// </summary>
    private void DrawPlacementLots(int street, float streetDepth) =>
        _renderer.DrawPlacementLots(
            this,
            street,
            streetDepth,
            _camera.CameraLateral,
            _placement.PlacementCells,
            _placement.PlacementLots,
            _placement.HoveredPlacementLot,
            _placement.SelectedPlacementLot,
            _hitRects);

    private void ProjectPlacementFootprint(
        float lateralOffset,
        float width,
        float streetDepth,
        out Vector2 nearLeft,
        out Vector2 nearRight,
        out Vector2 farRight,
        out Vector2 farLeft) =>
        _renderer.ProjectPlacementFootprint(
            lateralOffset, width, streetDepth, _camera.CameraLateral,
            out nearLeft, out nearRight, out farRight, out farLeft);

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
    private void DrawParcelTerritoryTints(int street, float streetDepth) =>
        _renderer.DrawParcelTerritoryTints(
            this,
            street,
            streetDepth,
            _camera.CameraLateral,
            _worldParcelColumns);

    private void DrawSteppedTintTrapezoid(
        Vector2 nearLeft,
        Vector2 nearRight,
        Vector2 farRight,
        Vector2 farLeft,
        Color fill) =>
        _renderer.DrawSteppedTintTrapezoid(
            this, nearLeft, nearRight, farRight, farLeft, fill);

    private void DrawSteppedPlacementFootprint(
        Vector2 nearLeft,
        Vector2 nearRight,
        Vector2 farRight,
        Vector2 farLeft,
        Color fill,
        Color outline,
        int frontageDivisions,
        int depthDivisions,
        bool drawInvalidMarker) =>
        _renderer.DrawSteppedPlacementFootprint(
            this, nearLeft, nearRight, farRight, farLeft,
            fill, outline, frontageDivisions, depthDivisions, drawInvalidMarker);

    private void DrawSteppedPlacementEdge(
        Vector2 from,
        Vector2 to,
        int steps,
        Color color) =>
        _renderer.DrawSteppedPlacementEdge(this, from, to, steps, color);
}
