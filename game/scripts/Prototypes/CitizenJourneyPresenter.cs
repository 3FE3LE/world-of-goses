#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Owns the per-citizen journey state of the macro street view (A4).
/// The founder's hero state, the per-citizen journey dictionary, the
/// navmesh planner, and the ambient route state all live here. The
/// view exposes the founder's carrier through the presenter; the
/// presenter paces against the domain window via the same
/// <c>PacedRouteSteps</c> / <c>ReconstructRouteProgress</c> helpers A2
/// introduced, never against the render cadence. The presenter has no
/// arrival authority — the calculate arrival tick is the domain's; the
/// presenter only renders the route to that tick.
/// </summary>
internal sealed class CitizenJourneyPresenter
{
    /// <summary>Per-citizen journey state. Lifted from the view's
    /// nested type to a top-level type so tests reach it without
    /// pulling the view into the test process.</summary>
    public sealed class JourneyState
    {
        public CitizenJourneyPresenter? Presenter;
        public CitizenId CitizenId;
        public CitizenSpriteCarrier? Carrier;
        public int Street;
        public float Lateral;
        public float DepthAnchor;
        public float? DepthTarget;
        public float TransitionAccumulator;
        public List<StreetRoutePlanner.Waypoint>? Route;
        public int RouteIndex;
        public BuildingId? Destination;
        public bool ReturningHome;
        public bool Walking;
        public bool IsAmbient;
        public int NextAmbientDecisionTick;
        public int? PacingStartTick;
        public int TotalSteps;
        public int StepsApplied;

        public JourneyState(
            CitizenJourneyPresenter presenter,
            CitizenId citizenId,
            CitizenSpriteCarrier carrier,
            int street,
            float lateral)
        {
            Presenter = presenter;
            CitizenId = citizenId;
            Carrier = carrier;
            Street = street;
            Lateral = lateral;
        }

        /// <summary>Compact constructor for the ambient-route helper
        /// that doesn't have a carrier yet. The view passes the
        /// carrier once <c>CitizenSpriteBank</c> creates it.</summary>
        public JourneyState(
            CitizenJourneyPresenter presenter,
            CitizenId citizenId,
            int street,
            float lateral)
        {
            Presenter = presenter;
            CitizenId = citizenId;
            Street = street;
            Lateral = lateral;
        }
    }

    private readonly Dictionary<int, JourneyState> _citizenJourneys = new();
    private CitizenSpriteCarrier? _heroCarrier;
    private int _heroStreet;
    private float _heroLateral;
    private float _depthAnchor;
    private float? _depthTarget;
    private float _motionAccumulator;
    private float _transitionAccumulator;
    private bool _heroWalking;
    private bool _heroPositionInitialized;
    private List<StreetRoutePlanner.Waypoint>? _route;
    private int _routeIndex;
    private (int ForestId, int UnitId)? _pendingGather;
    private BuildingId? _pendingAssignment;
    private bool _pendingReturnHome;
    private bool _heroIsGatheringOutsideHome;
    private bool _heroAmbientRoute;
    private int _heroNextAmbientDecisionTick;
    private int? _routePacingStartTick;
    private int _routeTotalSteps;
    private int _routeStepsApplied;
    private BuildingId? _lastKnownAssignment;
    private CitizenLocation? _lastKnownHeroLocation;

    private StreetNavigationServerPlanner? _navmeshPlanner;

    /// <summary>Per-citizen journeys dictionary. The renderer reads
    /// through this to project the visual carriers; the view's
    /// <c>UpdateCitizenHitRects</c> reads hits from it. Exposed as
    /// the underlying <c>Dictionary</c> so the presenter can also
    /// own write paths (start / stop / ambient reroll).</summary>
    public Dictionary<int, JourneyState> Journeys => _citizenJourneys;

    /// <summary>Hero carrier. The view's <c>UpdateHeroVisual</c>
    /// reads through this; the <c>CitizenSpriteBank</c> mounts the
    /// carrier through this reference.</summary>
    public CitizenSpriteCarrier? HeroCarrier
    {
        get => _heroCarrier;
        set => _heroCarrier = value;
    }

    /// <summary>Hero street. The view's <c>_Process</c> cadence loop
    /// queries this to drive the founder's tile index for terrain
    /// wear.</summary>
    public int HeroStreet
    {
        get => _heroStreet;
        set => _heroStreet = value;
    }

    /// <summary>Hero lateral. The view's <c>_Process</c> cadence loop
    /// queries this to drive the founder's tile index for terrain
    /// wear.</summary>
    public float HeroLateral
    {
        get => _heroLateral;
        set => _heroLateral = value;
    }

    /// <summary>Hero position initialization flag. The view's
    /// <c>EnsureHeroCarrier</c> hook flips this once.</summary>
    public bool HeroPositionInitialized
    {
        get => _heroPositionInitialized;
        set => _heroPositionInitialized = value;
    }

    /// <summary>Depth anchor for the hero carrier visual. The view
    /// reads through this for the carrier's <c>ZIndex</c>.</summary>
    public float DepthAnchor
    {
        get => _depthAnchor;
        set => _depthAnchor = value;
    }

    /// <summary>Depth target for the hero carrier visual. The view
    /// reads through this when the carrier is in transit.</summary>
    public float? DepthTarget
    {
        get => _depthTarget;
        set => _depthTarget = value;
    }

    /// <summary>Motion accumulator for the 12 Hz cadence. The view's
    /// <c>_Process</c> loop adds delta to this and consumes it in
    /// cadence steps.</summary>
    public float MotionAccumulator
    {
        get => _motionAccumulator;
        set => _motionAccumulator = value;
    }

    /// <summary>Transition accumulator for the founder's smoothed
    /// depth. The view's <c>AdvanceTransition</c> reads this.</summary>
    public float TransitionAccumulator
    {
        get => _transitionAccumulator;
        set => _transitionAccumulator = value;
    }

    /// <summary>Walker flag for the hero. The view's <c>UpdateHeroVisual</c>
    /// checks this to decide whether to keep the carrier mounted.</summary>
    public bool HeroWalking
    {
        get => _heroWalking;
        set => _heroWalking = value;
    }

    /// <summary>Current planned route. The view's <c>MotionTick</c>
    /// reads and consumes this when the founder reaches a waypoint.</summary>
    public List<StreetRoutePlanner.Waypoint>? Route
    {
        get => _route;
        set => _route = value;
    }

    /// <summary>Current route index. The view's <c>MotionTick</c>
    /// advances this.</summary>
    public int RouteIndex
    {
        get => _routeIndex;
        set => _routeIndex = value;
    }

    /// <summary>Pending gather target. The view's <c>OpenGatherMenu</c>
    /// reads this to fire the gather popup.</summary>
    public (int ForestId, int UnitId)? PendingGather
    {
        get => _pendingGather;
        set => _pendingGather = value;
    }

    /// <summary>Pending assignment target. The view's <c>EnsureHeroCarrier</c>
    /// hooks into this when the assignment changes.</summary>
    public BuildingId? PendingAssignment
    {
        get => _pendingAssignment;
        set => _pendingAssignment = value;
    }

    /// <summary>Pending return-home flag. The view's <c>EnsureHeroCarrier</c>
    /// hooks into this when the founder returns home.</summary>
    public bool PendingReturnHome
    {
        get => _pendingReturnHome;
        set => _pendingReturnHome = value;
    }

    /// <summary>Hero-is-currently-gathering-outside-home flag. The view
    /// reads this to keep the carrier visible during gather travel.</summary>
    public bool HeroIsGatheringOutsideHome
    {
        get => _heroIsGatheringOutsideHome;
        set => _heroIsGatheringOutsideHome = value;
    }

    /// <summary>Hero is on an ambient wander route. The view's
    /// <c>TryStartHeroAmbientRoute</c> queries this when the hero is
    /// idle.</summary>
    public bool HeroAmbientRoute
    {
        get => _heroAmbientRoute;
        set => _heroAmbientRoute = value;
    }

    /// <summary>Next ambient-route decision tick. The view's
    /// <c>TryStartHeroAmbientRoute</c> uses this to decide whether to
    /// reroll the ambient route.</summary>
    public int HeroNextAmbientDecisionTick
    {
        get => _heroNextAmbientDecisionTick;
        set => _heroNextAmbientDecisionTick = value;
    }

    /// <summary>Pacing start tick for the founder's planned route.
    /// A2's <c>PacedRouteSteps</c> reads this to compute the
    /// current step. Never set by the renderer — only the domain
    /// journey supplies the start tick.</summary>
    public int? RoutePacingStartTick
    {
        get => _routePacingStartTick;
        set => _routePacingStartTick = value;
    }

    /// <summary>Total steps for the founder's planned route. The
    /// <c>PacedRouteSteps</c> helper reads this.</summary>
    public int RouteTotalSteps
    {
        get => _routeTotalSteps;
        set => _routeTotalSteps = value;
    }

    /// <summary>Steps applied so far for the founder's planned route.
    /// The <c>PacedRouteSteps</c> helper reads this.</summary>
    public int RouteStepsApplied
    {
        get => _routeStepsApplied;
        set => _routeStepsApplied = value;
    }

    /// <summary>Last known assignment building id. The view's
    /// <c>EnsureHeroCarrier</c> reads this to detect changes.</summary>
    public BuildingId? LastKnownAssignment
    {
        get => _lastKnownAssignment;
        set => _lastKnownAssignment = value;
    }

    /// <summary>Last known hero location. The view's <c>EnsureHeroCarrier</c>
    /// reads this to detect transit changes.</summary>
    public CitizenLocation? LastKnownHeroLocation
    {
        get => _lastKnownHeroLocation;
        set => _lastKnownHeroLocation = value;
    }

    /// <summary>Navmesh planner. Created in the view's <c>_Ready</c>;
    /// disposed in <c>_ExitTree</c>. The presenter only owns the
    /// reference; the planner is built by the view because the planner
    /// is a Godot resource.</summary>
    public StreetNavigationServerPlanner? NavmeshPlanner
    {
        get => _navmeshPlanner;
        set => _navmeshPlanner = value;
    }

    /// <summary>Disposes the navmesh planner. The view calls this
    /// from its <c>_ExitTree</c> pass.</summary>
    public void DisposeNavmeshPlanner() => _navmeshPlanner?.Dispose();

    /// <summary>Looks up or creates the per-citizen journey entry.
    /// Same name as the view's helper so the migration is a clean
    /// forwarder.</summary>
    public JourneyState GetOrCreateJourney(CitizenId citizenId, out bool created)
    {
        if (_citizenJourneys.TryGetValue(citizenId.Value, out JourneyState? existing))
        {
            created = false;
            return existing;
        }
        var journey = new JourneyState(this, citizenId, null!, 0, 0f);
        _citizenJourneys[citizenId.Value] = journey;
        created = true;
        return journey;
    }

    /// <summary>Mutation helper for the founder's per-frame transition.
    /// The view's <c>AdvanceTransition</c> takes <c>ref</c> parameters;
    /// because the source fields are properties on this presenter, the
    /// view reads them into locals, calls <c>AdvanceTransition</c>, and
    /// writes the result back through this single setter.</summary>
    public void UpdateFounderTransition(float depthAnchor, float? depthTarget, float accumulator)
    {
        _depthAnchor = depthAnchor;
        _depthTarget = depthTarget;
        _transitionAccumulator = accumulator;
    }
}
