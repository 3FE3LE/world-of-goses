#nullable enable
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Owns the camera state of the macro street view (A4). The controller
/// encapsulates zoom, free/follow mode, lateral/depth anchor, pan,
/// transition timing, and the building-entry push. The view applies
/// the resulting <c>Scale</c> and <c>Position</c> to its own
/// <see cref="Node2D"/> each frame; the camera never touches the
/// host directly except through the cached reference.
///
/// The camera is free by default (design bible §04 "Cámara-sigue"):
/// free pan decouples the vanishing point from the founder's own
/// true position, and follow mode requires the explicit toggle. WASD
/// and arrows always pan; selection never moves the camera.
/// </summary>
internal sealed class MacroCameraController
{
    private Node2D? _host;
    private float _zoomLevel = MacroViewConstants.DefaultZoom;
    private Vector2 _neutralPosition;
    private bool _cameraFollowsHero = MacroViewConstants.DefaultCameraFollowsHero;
    private float _freeCameraLateral;
    private int _freeCameraStreet;
    private float _cameraDepthAnchor;
    private float? _cameraDepthTarget;
    private float _cameraTransitionAccumulator;
    private int _verticalPanDirection;
    private float _verticalPanHoldSeconds;
    private float _verticalPanRepeatAccumulator;

    // Building-entry push state (see BeginBuildingEntry/AdvanceBuildingEntry).
    private BuildingId? _pendingBuildingEntry;
    private Vector2 _buildingEntryPivotLocal;
    private float _buildingEntryStartZoom;
    private int _buildingEntryStep;
    private float _buildingEntryAccumulator;

    /// <summary>Bound to the view's own host so the camera can read/write
    /// the host's <c>Scale</c> and <c>Position</c> through the
    /// <c>Node2D</c> surface.</summary>
    public void Attach(Node2D host) => _host = host;

    /// <summary>Reads the cached host. The view's <c>NormalizePosition</c>
    /// helper writes the host's neutral position through this.</summary>
    public Node2D? Host => _host;

    /// <summary>Test surface used by <c>MacroStreetLiveViewTests</c>.
    /// The view keeps an <c>internal static</c> forwarder that
    /// delegates here.</summary>
    public bool FollowsFounderByDefault => MacroViewConstants.DefaultCameraFollowsHero;

    /// <summary>Test surface for the view's <c>MinimumZoomForTests</c>.</summary>
    public float MinimumZoomForTests => MacroViewConstants.MinZoom;

    /// <summary>Test surface for the view's <c>MaximumZoomForTests</c>.</summary>
    public float MaximumZoomForTests => MacroViewConstants.MaxZoom;

    /// <summary>Test surface for the view's <c>CameraZoomPivotYForTests</c>.</summary>
    public float CameraZoomPivotYForTests => MacroViewConstants.CameraZoomPivotY;

    /// <summary>Free-camera lateral position. The view's
    /// <c>CameraLateral</c> property reads this; the camera pan
    /// helpers write to it.</summary>
    public float FreeCameraLateral
    {
        get => _freeCameraLateral;
        set => _freeCameraLateral = value;
    }

    /// <summary>Free-camera street. The view's <c>CameraLateral</c>
    /// property reads this in the free path; the camera pan
    /// helpers write to it.</summary>
    public int FreeCameraStreet
    {
        get => _freeCameraStreet;
        set => _freeCameraStreet = value;
    }

    /// <summary>Whether the camera tracks the founder's anchor.
    /// The view's <c>CameraLateral</c> / <c>CameraDepthAnchor</c>
    /// properties read this to decide between the founder anchor
    /// and the free anchor.</summary>
    public bool CameraFollowsHero
    {
        get => _cameraFollowsHero;
        set => _cameraFollowsHero = value;
    }

    /// <summary>Vanishing point's smoothed depth. The view's
    /// <c>CameraDepthAnchor</c> property reads this; the camera
    /// transition helpers write to it.</summary>
    public float CameraDepthAnchor
    {
        get => _cameraDepthAnchor;
        set => _cameraDepthAnchor = value;
    }

    /// <summary>Vanishing point's target depth. The camera
    /// transition helpers write to this.</summary>
    public float? CameraDepthTarget
    {
        get => _cameraDepthTarget;
        set => _cameraDepthTarget = value;
    }

    /// <summary>Camera transition accumulator. The view's
    /// <c>AdvanceTransition</c> reads this each frame.</summary>
    public float CameraTransitionAccumulator
    {
        get => _cameraTransitionAccumulator;
        set => _cameraTransitionAccumulator = value;
    }

    /// <summary>Current zoom level. The view's <c>NormalizePosition</c>
    /// and <c>ResetZoom</c> read this.</summary>
    public float ZoomLevel
    {
        get => _zoomLevel;
        set => _zoomLevel = value;
    }

    /// <summary>Cached neutral position. The view's
    /// <c>NormalizePosition</c> helper reads this after subtracting
    /// <c>GlobalPosition</c> from <c>Position</c>.</summary>
    public Vector2 NeutralPosition
    {
        get => _neutralPosition;
        set => _neutralPosition = value;
    }

    /// <summary>Vertical pan direction: <c>-1</c> up, <c>0</c> none,
    /// <c>1</c> down. The view's <c>BeginVerticalCameraPan</c> writes
    /// this; <c>ContinueVerticalCameraPan</c> reads it.</summary>
    public int VerticalPanDirection
    {
        get => _verticalPanDirection;
        set => _verticalPanDirection = value;
    }

    /// <summary>Vertical pan hold seconds. The view's
    /// <c>VerticalPanRepeatSeconds</c> and
    /// <c>VerticalPanTransitionMultiplier</c> helpers read this.</summary>
    public float VerticalPanHoldSeconds
    {
        get => _verticalPanHoldSeconds;
        set => _verticalPanHoldSeconds = value;
    }

    /// <summary>Vertical pan repeat accumulator. The view's
    /// <c>ContinueVerticalCameraPan</c> reads this each frame.</summary>
    public float VerticalPanRepeatAccumulator
    {
        get => _verticalPanRepeatAccumulator;
        set => _verticalPanRepeatAccumulator = value;
    }

    /// <summary>Pending building entry. The view's
    /// <c>BeginBuildingEntry</c> writes this; <c>AdvanceBuildingEntry</c>
    /// reads it.</summary>
    public BuildingId? PendingBuildingEntry
    {
        get => _pendingBuildingEntry;
        set => _pendingBuildingEntry = value;
    }

    /// <summary>Building-entry pivot local. The view reads this to
    /// compute the new zoom around the clicked building.</summary>
    public Vector2 BuildingEntryPivotLocal
    {
        get => _buildingEntryPivotLocal;
        set => _buildingEntryPivotLocal = value;
    }

    /// <summary>Building-entry start zoom. The view's
    /// <c>AdvanceBuildingEntry</c> reads this to compute the
    /// step's zoom level.</summary>
    public float BuildingEntryStartZoom
    {
        get => _buildingEntryStartZoom;
        set => _buildingEntryStartZoom = value;
    }

    /// <summary>Building-entry step counter. The view's
    /// <c>AdvanceBuildingEntry</c> reads and increments this.</summary>
    public int BuildingEntryStep
    {
        get => _buildingEntryStep;
        set => _buildingEntryStep = value;
    }

    /// <summary>Building-entry accumulator. The view's
    /// <c>AdvanceBuildingEntry</c> reads this.</summary>
    public float BuildingEntryAccumulator
    {
        get => _buildingEntryAccumulator;
        set => _buildingEntryAccumulator = value;
    }

    /// <summary>Test surface for the view's <c>CameraLateralForVisualRegression</c>.</summary>
    public float CameraLateralForVisualRegression => _freeCameraLateral;

    /// <summary>Computed camera lateral. The view's
    /// <c>CameraLateral</c> property reads this; the camera
    /// transition helpers read it too.</summary>
    public float CameraLateral
    {
        get
        {
            if (_cameraFollowsHero)
            {
                return _freeCameraLateral;
            }
            return _freeCameraLateral;
        }
    }

    /// <summary>Mutation helper for the view's per-frame transition.
    /// The view's <c>AdvanceTransition</c> takes <c>ref</c> parameters;
    /// because the source fields are properties on this controller, the
    /// view reads them into locals, calls <c>AdvanceTransition</c>, and
    /// writes the result back through this single setter.</summary>
    public void UpdateCameraTransition(float depthAnchor, float? depthTarget, float accumulator)
    {
        _cameraDepthAnchor = depthAnchor;
        _cameraDepthTarget = depthTarget;
        _cameraTransitionAccumulator = accumulator;
    }
}
