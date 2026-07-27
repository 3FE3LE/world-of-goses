#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Prototypes;

/// <summary>
/// The pseudo-3D "perspectiva por calles" macro view — now the default
/// world view (see <see cref="_perspectiveActive"/>'s initial value).
/// Renders the live city (via the same <see cref="CityWorldController"/>
/// instance <see cref="CityMacroView"/> uses): completed buildings route a
/// click to the real <c>BuildingDetailView</c>
/// (<c>_controller.SelectBuilding(...)</c>, same as
/// <c>BuildingPlotStage.OnPlotBuildingClicked</c>), and forest trees route a
/// click to a gather flow equivalent to
/// <c>CityMacroView.OnResourceGatherRequested</c> (minus the hero-walk
/// travel animation — see <see cref="OpenGatherMenu"/>).
///
/// <c>CityMacroView</c> (the flat view) is still reachable as a fallback via
/// F9/the HUD toggle button, and remains fully functional — nothing in this
/// class changes a line of it (or <c>OrthogonalParcelTerrain</c>/
/// <c>BuildingPlotStage</c>/<c>ConstructionPlacementOverlay</c>). It must be
/// a LATER sibling of <c>CityMacroView</c> under
/// <c>GameUiShell/ScreenContent</c> in the scene tree — Godot dispatches a
/// signal to connected callables in connection order, and connections
/// happen during each node's <c>_Ready()</c> in tree order, so this node's
/// <see cref="OnSelectionChanged"/>/<see cref="OnHeroCreated"/> always run
/// after <c>CityMacroView</c>'s own handlers and can override which of the
/// two world views ends up visible. This ordering dependency is intentional
/// and temporary — clean it up once this view replaces the flat one for
/// real.
///
/// Remaining known gaps (documented, not blocking): clicking an in-progress
/// construction project (the real game opens the construction panel for
/// that, not <c>BuildingDetailView</c>); the empty-state guidance text for a
/// brand-new city; buildings spanning more than one lot-row anchor to their
/// nearest-to-viewer row. See
/// docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md,
/// "Cámara y mundo caminable".
/// </summary>
public partial class MacroStreetLiveView : Node2D
{
    private const float CenterX = 640f;
    private const float BaseY = 580f; // ScreenContent-local: clear of the ~68px MacroActions band
    private const float LotUnitPx = 90f;
    private const float RoadHeightPx = 20f;
    private const float AvatarSize = 24f;

    // Same cadence discipline as the earlier prototypes (design bible §08,
    // "Pixel-motion grammar"): no continuous tweening.
    private const int TransitionSteps = 5;
    private const float DepthStepSize = 1f / TransitionSteps;

    private const float TreeSize = 28f;
    private const string ResourceActionMenuScenePath = "res://scenes/Components/ResourceActionMenu.tscn";

    private static readonly Color RoadColor = new("#5c5442");
    private static readonly Color BuildingColor = new("#8a7a54");
    private static readonly Color TreeColor = new("#4c7a3f");
    private static readonly Color AvatarColor = new("#d9a24e");

    [Export] public NodePath ControllerPath { get; set; } = "../../../CityWorldController";
    [Export] public NodePath CityMacroViewPath { get; set; } = "../CityMacroView";
    [Export] public NodePath ToggleButtonPath { get; set; } =
        "../MacroActions/Actions/PerspectiveToggleButton";
    // These three are only meaningful for the flat view, but CityMacroView's
    // own Refresh() drives their Visible independently of which world view
    // is on screen (it runs on every world-changed event regardless of its
    // own visibility) — since the structural-reparent slice made them
    // siblings instead of CityMacroView's children, they no longer inherit
    // its hidden state and must be hidden explicitly while perspective is
    // active, or their still-interactive Controls silently eat clicks meant
    // for this view underneath them.
    [Export] public NodePath PlotStagePath { get; set; } = "../BuildingPlotStage";
    [Export] public NodePath EmptyPanelPath { get; set; } = "../Center/EmptyPanel";
    [Export] public NodePath OfflineReportPath { get; set; } = "../OfflineReportPanel";

    private CityWorldController _controller = null!;
    private CityMacroView _cityMacroView = null!;
    private BaseButton _toggleButton = null!;
    private ResourceActionMenu _actionMenu = null!;
    private Control _plotStage = null!;
    private Control _emptyPanel = null!;
    private Control _offlineReport = null!;
    private bool _perspectiveActive = true;

    private readonly List<PlotBox> _plots = new();
    private readonly List<TreeBox> _trees = new();
    private readonly List<(Rect2 Rect, int BuildingId)> _clickableRects = new();
    private readonly List<(Rect2 Rect, TreeBox Tree)> _clickableTreeRects = new();

    private int _streetCount = 1;
    private float _lateralHalfWidthPx = LotUnitPx;

    private int _currentStreet;
    private float _depthAnchor;
    private float? _depthTarget;
    private float _lateralPosition;
    private float _lateralAccumulator;
    private float _transitionAccumulator;

    private readonly record struct PlotBox(
        int Street,
        float LateralOffset,
        float Width,
        float Height,
        int BuildingId);

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
        _cityMacroView = GetNode<CityMacroView>(CityMacroViewPath);
        _toggleButton = GetNode<BaseButton>(ToggleButtonPath);
        _plotStage = GetNode<Control>(PlotStagePath);
        _emptyPanel = GetNode<Control>(EmptyPanelPath);
        _offlineReport = GetNode<Control>(OfflineReportPath);

        _streetCount = OrthogonalParcelTerrain.ParcelRows * ParcelGrid.LotsPerAxis;
        _lateralHalfWidthPx =
            OrthogonalParcelTerrain.ParcelColumns * ParcelGrid.LotsPerAxis * LotUnitPx * 0.5f;

        _actionMenu = GD.Load<PackedScene>(ResourceActionMenuScenePath).Instantiate<ResourceActionMenu>();
        _actionMenu.GatherRequested += OnGatherRequested;
        // Deferred for the same reason as CityMacroView's placement overlay:
        // ScreenContent is still mid-_Ready() for its own children (this
        // node included), and Godot rejects add_child on a parent that is
        // "busy setting up children".
        GetParent().CallDeferred(Node.MethodName.AddChild, _actionMenu);

        _controller.BuildingStateChanged += OnWorldChanged;
        _controller.ProjectStateChanged += OnWorldChanged;
        _controller.WorldTickAdvanced += OnWorldTickAdvanced;
        _controller.SelectionChanged += OnSelectionChanged;
        _controller.HeroCreated += OnHeroCreated;
        _toggleButton.Pressed += TogglePerspective;

        RefreshPlots();
        Visible = false;
        // ScreenContent (this node's parent) sits below CityStatusPanel
        // inside a VBoxContainer, so its own local (0,0) is NOT the
        // viewport's top-left — CenterX/BaseY assume it is. Cancel that
        // offset once layout has settled (call_deferred runs after this
        // frame's container pass) instead of hardcoding the status bar's
        // height.
        CallDeferred(MethodName.NormalizePosition);

        // Perspective is the default view now: activate it up front unless
        // onboarding still needs to run (mirrors CityMacroView's own initial
        // visibility check — that view shows itself once onboarding
        // finishes, via OnHeroCreated below).
        if (_perspectiveActive && !_controller.NeedsOnboarding())
        {
            ActivatePerspective();
        }
    }

    private void NormalizePosition() => Position -= GlobalPosition;

    public override void _ExitTree()
    {
        _controller.BuildingStateChanged -= OnWorldChanged;
        _controller.ProjectStateChanged -= OnWorldChanged;
        _controller.WorldTickAdvanced -= OnWorldTickAdvanced;
        _controller.SelectionChanged -= OnSelectionChanged;
        _controller.HeroCreated -= OnHeroCreated;
        _toggleButton.Pressed -= TogglePerspective;
        _actionMenu.GatherRequested -= OnGatherRequested;
    }

    /// <summary>
    /// Mirrors <c>CityMacroView.OnHeroCreated</c>: without this, completing
    /// onboarding would only show the flat view (the one that actually
    /// listens for this signal today), leaving the perspective view hidden
    /// even though its toggle is still "on".
    /// </summary>
    private void OnHeroCreated(int citizenId)
    {
        if (_perspectiveActive) ActivatePerspective();
    }

    private void OnWorldChanged(int _) => RefreshPlots();

    private void OnWorldTickAdvanced(int _) => RefreshPlots();

    /// <summary>
    /// See the class doc: this always runs after
    /// <c>CityMacroView.OnSelectionChanged</c> and has the last word on
    /// which world view is actually visible.
    /// </summary>
    private void OnSelectionChanged(int selectionState)
    {
        bool isMacroView =
            (CityWorldController.Selection)selectionState == CityWorldController.Selection.MacroView;
        if (!isMacroView)
        {
            // Neither world view's own content belongs on screen while a
            // detail/profile screen is open — and CityMacroView.Refresh()
            // drives _plotStage/_emptyPanel/_offlineReport independently of
            // its own visibility, so they need the same explicit hide here
            // regardless of which world view had been active.
            Hide();
            HideFlatOnlyOverlays();
            return;
        }
        if (_perspectiveActive) ActivatePerspective();
        // else: the flat view already showed itself via its own
        // OnSelectionChanged, and its own Refresh() cycle already restores
        // _plotStage/_emptyPanel/_offlineReport correctly — nothing to do.
    }

    private void TogglePerspective()
    {
        _perspectiveActive = !_perspectiveActive;
        if (_perspectiveActive)
        {
            ActivatePerspective();
        }
        else
        {
            Hide();
            // Not just Show(): CityMacroView.Refresh() is private, so this
            // public method (also used elsewhere for "return to the city")
            // is the seam that recomputes _plotStage/_emptyPanel/
            // _offlineReport for the flat view without touching CityMacroView.cs.
            _cityMacroView.OnReturnedToCity();
        }
    }

    /// <summary>
    /// Shows this view and hides both the flat view and the three
    /// flat-view-only overlays it drives independently of its own
    /// visibility (see <see cref="PlotStagePath"/> etc.).
    /// </summary>
    private void ActivatePerspective()
    {
        _cityMacroView.Hide();
        HideFlatOnlyOverlays();
        Show();
        RefreshPlots();
    }

    private void HideFlatOnlyOverlays()
    {
        _plotStage.Hide();
        _emptyPanel.Hide();
        _offlineReport.Hide();
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
        CityMacroSnapshot snapshot = _controller.GetCityMacroSnapshot();
        float totalLotColumns = OrthogonalParcelTerrain.ParcelColumns * ParcelGrid.LotsPerAxis;

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
        QueueRedraw();
    }

    /// <summary>
    /// Each forest patch has one reserve per individual tree
    /// (<c>WoodUnitReserves</c>); <c>ParcelGrid.NaturalResourceLot</c> (the
    /// same helper <c>OrthogonalParcelTerrain.ResourceUnitCenter</c> uses)
    /// gives each unit's own lot within the patch's parcel, so trees get
    /// the same calle/lateral projection as buildings instead of all
    /// bunching up at the patch's own (fixed 0,0) lot. Depleted units
    /// (reserve 0) are skipped, matching <c>OrthogonalParcelTerrain.RebuildTrees</c>.
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
        }
    }

    private void AddPlot(CityMacroSnapshot.PlotItem item, float totalLotColumns, bool clickable)
    {
        int street = item.ParcelRow * ParcelGrid.LotsPerAxis + item.LotRow;
        float lotCenterColumn = item.ParcelColumn * ParcelGrid.LotsPerAxis
            + item.LotColumn
            + item.LotWidth * 0.5f;
        float lateralOffset = (lotCenterColumn - totalLotColumns * 0.5f) * LotUnitPx;
        _plots.Add(new PlotBox(
            street,
            lateralOffset,
            item.LotWidth * LotUnitPx,
            item.LotHeight * LotUnitPx,
            clickable ? item.Id.Value : -1));
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Visible) return;
        AdvanceLateralMovement(delta);
        AdvanceStreetTransition(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible) return;
        switch (@event)
        {
            case InputEventKey { Keycode: Key.F9, Pressed: true, Echo: false }:
                TogglePerspective();
                break;
            case InputEventKey { Pressed: true, Echo: false } keyEvent
                when keyEvent.Keycode is Key.Up or Key.W:
                StepStreet(1);
                break;
            case InputEventKey { Pressed: true, Echo: false } keyEvent
                when keyEvent.Keycode is Key.Down or Key.S:
                StepStreet(-1);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click:
                TryClick(click.Position);
                break;
        }
    }

    private void TryClick(Vector2 clickPosition)
    {
        foreach ((Rect2 rect, TreeBox tree) in _clickableTreeRects)
        {
            if (!rect.HasPoint(clickPosition)) continue;
            OpenGatherMenu(tree, rect);
            return;
        }
        foreach ((Rect2 rect, int buildingId) in _clickableRects)
        {
            if (!rect.HasPoint(clickPosition)) continue;
            _controller.SelectBuilding(new BuildingId(buildingId));
            return;
        }
    }

    /// <summary>
    /// Same validation <c>CityMacroView.OnResourceGatherRequested</c> uses
    /// (hero unassigned and not on expedition). Deliberately simplified vs.
    /// the flat view: no hero-walk travel animation — this view's avatar is
    /// a player navigation cursor, not a rendering of the hero citizen, so
    /// gathering resolves immediately on confirm instead of after a walk.
    /// </summary>
    private void OpenGatherMenu(TreeBox tree, Rect2 rect)
    {
        Citizen? hero = _controller.World.Hero;
        bool onExpedition = hero is not null && _controller.World.IsCitizenOnActiveExpedition(hero.Id);
        bool canGather = hero is not null && !hero.CurrentAssignment.HasValue && !onExpedition;
        string unavailableReason = hero is null
            ? "No founder is available to gather."
            : onExpedition
                ? $"{hero.Name} is away on an expedition."
                : $"{hero.Name} is already assigned. Unassign them before gathering wood.";
        _actionMenu.Open(
            tree.ForestId,
            tree.UnitId,
            tree.Reserve,
            tree.TicksUntilRegeneration,
            rect.GetCenter(),
            rect.GetCenter(),
            canGather,
            unavailableReason);
    }

    private void OnGatherRequested(int forestId, int unitId, Vector2 targetPosition)
    {
        int gathered = _controller.GatherWood(new BuildingId(forestId), unitId, 2);
        if (gathered > 0) Notifier.Show($"Gathered {gathered} wood.");
        else Notifier.ShowError("This tree no longer has wood available.");
    }

    private void StepStreet(int direction)
    {
        if (_depthTarget.HasValue) return;
        int nextStreet = Mathf.Clamp(_currentStreet + direction, 0, _streetCount - 1);
        if (nextStreet == _currentStreet) return;
        _currentStreet = nextStreet;
        _depthTarget = _currentStreet;
    }

    private void AdvanceStreetTransition(double delta)
    {
        if (!_depthTarget.HasValue) return;
        _transitionAccumulator += (float)delta;
        while (_transitionAccumulator >= PixelMotion.CadenceSeconds && _depthTarget.HasValue)
        {
            _transitionAccumulator -= PixelMotion.CadenceSeconds;
            float target = _depthTarget.Value;
            if (Mathf.Abs(target - _depthAnchor) <= DepthStepSize)
            {
                _depthAnchor = target;
                _depthTarget = null;
            }
            else
            {
                _depthAnchor += Mathf.Sign(target - _depthAnchor) * DepthStepSize;
            }
            QueueRedraw();
        }
    }

    private void AdvanceLateralMovement(double delta)
    {
        _lateralAccumulator += (float)delta;
        while (_lateralAccumulator >= PixelMotion.CadenceSeconds)
        {
            _lateralAccumulator -= PixelMotion.CadenceSeconds;
            TryStepLateral();
        }
    }

    private void TryStepLateral()
    {
        float direction = ReadLateralDirection();
        if (direction == 0f) return;
        float next = Mathf.Clamp(
            _lateralPosition + direction * PixelMotion.StepPixels,
            -_lateralHalfWidthPx,
            _lateralHalfWidthPx);
        if (next == _lateralPosition) return;
        _lateralPosition = next;
        QueueRedraw();
    }

    private static float ReadLateralDirection()
    {
        if (Input.IsActionPressed("ui_left")) return -1f;
        if (Input.IsActionPressed("ui_right")) return 1f;
        return 0f;
    }

    public override void _Draw()
    {
        _clickableRects.Clear();
        _clickableTreeRects.Clear();
        for (int street = _streetCount - 1; street >= 0; street--)
        {
            DrawStreetRow(street);
        }
        DrawAvatar();
    }

    /// <summary>
    /// Every lateral offset is relative to the avatar's own
    /// <see cref="_lateralPosition"/> — the vanishing point follows the
    /// viewer, the fix validated in the earlier isolated prototype.
    /// </summary>
    private void DrawStreetRow(int street)
    {
        float depth = street - _depthAnchor;
        (Vector2 roadPosition, Vector2 roadScale) =
            StreetDepthProjection.Project(depth, -_lateralPosition, CenterX, BaseY);
        var roadSize = new Vector2(
            2f * _lateralHalfWidthPx * roadScale.X,
            RoadHeightPx * roadScale.Y);
        DrawRect(new Rect2(roadPosition - roadSize * 0.5f, roadSize), RoadColor);

        foreach (PlotBox plot in _plots)
        {
            if (plot.Street != street) continue;
            float relativeOffset = plot.LateralOffset - _lateralPosition;
            (Vector2 position, Vector2 scale) =
                StreetDepthProjection.Project(depth, relativeOffset, CenterX, BaseY);
            var size = new Vector2(plot.Width * scale.X, plot.Height * scale.Y);
            var rect = new Rect2(position - size * 0.5f, size);
            DrawRect(rect, BuildingColor);
            if (plot.BuildingId >= 0) _clickableRects.Add((rect, plot.BuildingId));
        }

        foreach (TreeBox tree in _trees)
        {
            if (tree.Street != street) continue;
            float treeRelativeOffset = tree.LateralOffset - _lateralPosition;
            (Vector2 treePosition, Vector2 treeScale) =
                StreetDepthProjection.Project(depth, treeRelativeOffset, CenterX, BaseY);
            var treeSize = new Vector2(TreeSize * treeScale.X, TreeSize * treeScale.Y);
            var treeRect = new Rect2(treePosition - treeSize * 0.5f, treeSize);
            DrawRect(treeRect, TreeColor);
            _clickableTreeRects.Add((treeRect, tree));
        }
    }

    private void DrawAvatar()
    {
        float depth = _currentStreet - _depthAnchor;
        (Vector2 position, Vector2 scale) = StreetDepthProjection.Project(depth, 0f, CenterX, BaseY);
        var size = new Vector2(AvatarSize * scale.X, AvatarSize * scale.Y);
        DrawRect(new Rect2(position - size * 0.5f, size), AvatarColor);
    }
}
