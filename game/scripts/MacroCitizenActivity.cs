#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Renders the world's citizen population on the macro city view.
///
/// <para>
/// The canonical hero carrier is always mounted in this view. Other citizens
/// use compact markers until their own contextual representation is required.
/// </summary>
public partial class MacroCitizenActivity : Node2D
{
    private const float RouteObstaclePadding = 12f;
    private const float HeroHitboxPx = 32f;
    private const float StatusIconSize = 16f;
    private const int StatusRowSeparation = 6;

    /// <summary>
    /// Emitted when the player clicks the hero sprite on the macro
    /// view. The host routes the click to the hero profile detail.
    /// </summary>
    [Signal] public delegate void HeroClickedEventHandler();

    private CitizenSpriteCarrier? _heroCarrier;
    private Control? _heroMarker;
    private readonly List<Vector2> _travelRoute = new();
    private CanvasItem? _travellingNode;
    private Action? _travelArrived;
    private int _travelWaypointIndex;
    private float _travelAccumulator;
    private bool _heroHovered;
    private bool _travelling;

    /// <summary>
    /// (Re)builds the macro population dots or places the walking
    /// hero sprite when the city is empty. Positions are deterministic
    /// so the visual is identical across reloads.
    /// </summary>
    /// <param name="citizens">Citizens projected for the macro view.</param>
    /// <param name="buildingCount">Total buildings (zero while the
    /// world is empty).</param>
    /// <param name="projectCount">Total in-flight construction projects
    /// (zero while the world is empty).</param>
    public void Populate(
        IReadOnlyList<CityMacroSnapshot.CitizenItem> citizens,
        int buildingCount = 0,
        int projectCount = 0,
        Vector2? heroAnchorGlobal = null)
    {
        // Construction and production can emit state changes on every world
        // tick. Those refresh the macro view while a gather route is active.
        // Rebuilding here would free the moving marker and cancel the route
        // before its arrival callback can persist the visit.
        if (!ShouldRebuildForRefresh(_travelling)) return;

        if (_heroHovered)
        {
            _heroHovered = false;
            Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
        }
        foreach (var child in GetChildren())
        {
            if (child == _heroCarrier) continue;
            RemoveChild(child);
            child.QueueFree();
        }
        if (_heroCarrier?.State == CitizenSpriteCarrier.VisualState.Macro)
        {
            _heroCarrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
        }
        _heroCarrier = null;
        _heroMarker = null;
        ResetTravel();

        var parentSize = ((Control)GetParent()).Size;
        if (parentSize == Vector2.Zero)
        {
            parentSize = new Vector2(
                PresentationConstants.CanvasWidth,
                PresentationConstants.CanvasHeight);
        }

        _ = buildingCount;
        _ = projectCount;
        if (citizens.Count > 0 && _hero is not null)
        {
            Vector2 heroPosition = heroAnchorGlobal.HasValue
                ? PixelMotion.Snap(ToLocal(heroAnchorGlobal.Value))
                : PixelMotion.Snap(new Vector2(
                    parentSize.X * 0.5f,
                    parentSize.Y * 0.6f));
            PlaceIdleHero(_hero, heroPosition);
        }

        for (int i = 1; i < citizens.Count; i++)
        {
            int denominator = Mathf.Max(citizens.Count, 1);
            float angle = Mathf.Pi * (0.15f + 0.7f * (i / (float)denominator));
            float radius = 220f;
            float cx = parentSize.X * 0.5f;
            float cy = parentSize.Y * 0.85f;
            float x = cx + Mathf.Cos(angle) * radius;
            float y = cy - Mathf.Sin(angle) * radius;

            var marker = new Control
            {
                Position = new Vector2(x, y),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            AddChild(marker);

            var dot = new ColorRect
            {
                Color = new Color("c8b88a"),
                Size = new Vector2(
                    PresentationConstants.MacroCitizenSize,
                    PresentationConstants.MacroCitizenSize),
                Position = Vector2.Zero,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            dot.AddToGroup(PresentationConstants.GroupMacroCitizenDot);
            marker.AddChild(dot);
            if (!citizens[i].IsAvailable) continue;

            // An unassigned citizen is physically AtHome in the domain. On
            // the macro view, keep that state visible by naming its existing
            // population marker instead of letting it become an anonymous dot.
            string statusIcon = CitizenStatusIcon(citizens[i]);
            var row = new HBoxContainer
            {
                Position = new Vector2(
                    -48f,
                    PresentationConstants.MacroCitizenSize + 3f),
                Size = new Vector2(96f, 22f),
                Alignment = BoxContainer.AlignmentMode.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            row.AddThemeConstantOverride("separation", StatusRowSeparation);
            marker.AddChild(row);

            var icon = new TextureRect
            {
                Texture = ResourceLoader.Load<Texture2D>(statusIcon),
                StretchMode = TextureRect.StretchModeEnum.Scale,
                CustomMinimumSize = new Vector2(StatusIconSize, StatusIconSize),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Modulate = LineageThemeRegistry.IconAccent,
            };
            row.AddChild(icon);

            var nameLabel = new Label
            {
                Text = citizens[i].Name,
                ThemeTypeVariation = "BodySmall",
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            row.AddChild(nameLabel);
        }
    }

    internal static bool ShouldRebuildForRefresh(bool isTravelling) =>
        !isTravelling;

    public void SetHeroAnchor(Vector2 globalAnchor)
    {
        if (_travelling
            || _heroCarrier is null
            || _heroCarrier.State != CitizenSpriteCarrier.VisualState.Macro)
        {
            return;
        }
        _heroCarrier.SetPositionImmediate(
            PixelMotion.Snap(ToLocal(globalAnchor)));
        _heroCarrier.Idle(Vector2.Down);
    }

    /// <summary>
    /// Maps a citizen's state to the icon shown next to their name on
    /// the macro view. The hero walking on the empty field ignores
    /// this helper (the sprite carries the affordance).
    /// </summary>
    private static string CitizenStatusIcon(CityMacroSnapshot.CitizenItem item)
    {
        if (item.CurrentStamina <= 0)
        {
            return IconPaths.Warning;
        }
        return item.Location switch
        {
            CitizenLocation.AtHome => IconPaths.House,
            CitizenLocation.AtWork => IconPaths.Building,
            _ => IconPaths.User,
        };
    }

    /// <summary>
    /// Hero reference, set by <see cref="CityMacroView"/> alongside
    /// the population count so this node can resolve the lineage +
    /// gender scene for the empty-field sprite. Null when no hero
    /// exists yet (population mode applies).
    /// </summary>
    public CityMacroSnapshot.HeroVisual? Hero
    {
        get => _hero;
        set
        {
            _hero = value;
        }
    }
    private CityMacroSnapshot.HeroVisual? _hero;

    private void PlaceIdleHero(
        CityMacroSnapshot.HeroVisual hero,
        Vector2 position)
    {
        _heroCarrier = CitizenSpriteBank.Instance.GetOrCreate(hero.Id, hero.Lineage, hero.Gender, hero.Appearance);
        CitizenSpriteBank.Instance.Mount(_heroCarrier, this);
        _heroCarrier.SetPositionImmediate(position);
        _heroCarrier.SetState(CitizenSpriteCarrier.VisualState.Macro);
        _heroCarrier.Idle(Vector2.Down);
    }

    public override void _Process(double delta)
    {
        if (_travelling)
        {
            AdvancePixelTravel((float)delta);
            return;
        }
        if (_heroCarrier is null
            || _heroCarrier.State != CitizenSpriteCarrier.VisualState.Macro) return;

        UpdateHoverState();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_heroCarrier is null || _heroCarrier.State != CitizenSpriteCarrier.VisualState.Macro) return;
        if (@event is InputEventMouseButton mb
            && mb.ButtonIndex == MouseButton.Left
            && mb.Pressed
            && _heroHovered)
        {
            EmitSignal(SignalName.HeroClicked);
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Tracks whether the cursor is currently over the hero sprite and
    /// toggles the global cursor shape to communicate that the sprite
    /// is interactive. The hitbox is the same 128×128 box the LPC
    /// sprite uses, so the hint matches the visible art.
    /// </summary>
    private void UpdateHoverState()
    {
        if (_heroCarrier is null) return;
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector2 pos = _heroCarrier.GlobalPosition;
        float half = HeroHitboxPx * 0.5f;
        bool nowHovered = mouse.X >= pos.X - half
            && mouse.X <= pos.X + half
            && mouse.Y >= pos.Y - half
            && mouse.Y <= pos.Y + half;
        if (nowHovered == _heroHovered) return;
        _heroHovered = nowHovered;
        Input.SetDefaultCursorShape(nowHovered
            ? Input.CursorShape.PointingHand
            : Input.CursorShape.Arrow);
    }

    /// <summary>
    /// Moves the current macro representation of the hero to an in-world
    /// resource. The caller performs the domain action only after arrival.
    /// </summary>
    public void TravelHeroTo(
        Vector2 globalTarget,
        IReadOnlyList<Rect2> occupiedGlobalRects,
        Action arrived)
    {
        ArgumentNullException.ThrowIfNull(arrived);
        Vector2 target = PixelMotion.Snap(ToLocal(globalTarget) + new Vector2(0, 20));
        Node2D? movingNode = _heroCarrier;
        Vector2 start;
        if (movingNode is not null)
        {
            start = PixelMotion.Snap(movingNode.Position);
        }
        else if (_heroMarker is not null)
        {
            start = PixelMotion.Snap(_heroMarker.Position);
        }
        else
        {
            arrived();
            return;
        }

        var localObstacles = new List<Rect2>(occupiedGlobalRects.Count);
        foreach (Rect2 globalRect in occupiedGlobalRects)
        {
            localObstacles.Add(new Rect2(
                ToLocal(globalRect.Position),
                globalRect.Size).Grow(RouteObstaclePadding));
        }

        ResetTravel();
        _travelling = true;
        _travellingNode = movingNode is not null
            ? movingNode
            : _heroMarker;
        _travelArrived = arrived;
        _travelRoute.AddRange(PlanCardinalRoute(start, target, localObstacles));
        FaceNextTravelWaypoint();
    }

    private void AdvancePixelTravel(float delta)
    {
        if (_travellingNode is null || _travelWaypointIndex >= _travelRoute.Count)
        {
            CompleteTravel();
            return;
        }

        _travelAccumulator += delta;
        while (_travelAccumulator >= PixelMotion.CadenceSeconds && _travelling)
        {
            _travelAccumulator -= PixelMotion.CadenceSeconds;
            Vector2 target = _travelRoute[_travelWaypointIndex];
            Vector2 next = PixelMotion.StepCardinal(
                GetTravelPosition(_travellingNode),
                target);
            SetTravelPosition(_travellingNode, next);
            if (GetTravelPosition(_travellingNode) != target) continue;

            _travelWaypointIndex++;
            if (_travelWaypointIndex >= _travelRoute.Count) CompleteTravel();
            else FaceNextTravelWaypoint();
        }
    }

    private void FaceNextTravelWaypoint()
    {
        if (_heroCarrier is null
            || _travellingNode is null
            || _travelWaypointIndex >= _travelRoute.Count) return;
        Vector2 delta = _travelRoute[_travelWaypointIndex] - GetTravelPosition(_travellingNode);
        _heroCarrier.Walk(Mathf.Abs(delta.X) >= Mathf.Abs(delta.Y)
            ? new Vector2(Mathf.Sign(delta.X), 0)
            : new Vector2(0, Mathf.Sign(delta.Y)));
    }

    private void CompleteTravel()
    {
        Action? arrived = _travelArrived;
        ResetTravel();
        _heroCarrier?.Idle(Vector2.Down);
        arrived?.Invoke();
    }

    private void ResetTravel()
    {
        _travelling = false;
        _travellingNode = null;
        _travelArrived = null;
        _travelRoute.Clear();
        _travelWaypointIndex = 0;
        _travelAccumulator = 0f;
    }

    internal static IReadOnlyList<Vector2> PlanCardinalRoute(
        Vector2 start,
        Vector2 target,
        IReadOnlyList<Rect2> obstacles)
    {
        start = PixelMotion.Snap(start);
        target = PixelMotion.Snap(target);
        var candidates = new List<List<Vector2>>
        {
            new() { new Vector2(target.X, start.Y), target },
            new() { new Vector2(start.X, target.Y), target },
        };

        foreach (Rect2 obstacle in obstacles)
        {
            float above = Mathf.Floor(obstacle.Position.Y - 1f);
            float below = Mathf.Ceil(obstacle.End.Y + 1f);
            float left = Mathf.Floor(obstacle.Position.X - 1f);
            float right = Mathf.Ceil(obstacle.End.X + 1f);
            candidates.Add(new List<Vector2>
            {
                new(start.X, above), new(target.X, above), target,
            });
            candidates.Add(new List<Vector2>
            {
                new(start.X, below), new(target.X, below), target,
            });
            candidates.Add(new List<Vector2>
            {
                new(left, start.Y), new(left, target.Y), target,
            });
            candidates.Add(new List<Vector2>
            {
                new(right, start.Y), new(right, target.Y), target,
            });
        }

        List<Vector2>? best = null;
        float bestDistance = float.MaxValue;
        foreach (List<Vector2> candidate in candidates)
        {
            Vector2 from = start;
            float distance = 0f;
            bool blocked = false;
            foreach (Vector2 waypoint in candidate)
            {
                if (SegmentCrossesAny(from, waypoint, obstacles))
                {
                    blocked = true;
                    break;
                }
                distance += from.DistanceTo(waypoint);
                from = waypoint;
            }
            if (blocked || distance >= bestDistance) continue;
            best = candidate;
            bestDistance = distance;
        }

        return best ?? new List<Vector2> { target };
    }

    private static bool SegmentCrossesAny(
        Vector2 from,
        Vector2 to,
        IReadOnlyList<Rect2> obstacles)
    {
        foreach (Rect2 obstacle in obstacles)
        {
            bool horizontal = Mathf.IsEqualApprox(from.Y, to.Y)
                && from.Y > obstacle.Position.Y
                && from.Y < obstacle.End.Y
                && Mathf.Max(from.X, to.X) > obstacle.Position.X
                && Mathf.Min(from.X, to.X) < obstacle.End.X;
            bool vertical = Mathf.IsEqualApprox(from.X, to.X)
                && from.X > obstacle.Position.X
                && from.X < obstacle.End.X
                && Mathf.Max(from.Y, to.Y) > obstacle.Position.Y
                && Mathf.Min(from.Y, to.Y) < obstacle.End.Y;
            if (horizontal || vertical) return true;
        }
        return false;
    }

    private static Vector2 GetTravelPosition(CanvasItem item) => item switch
    {
        Node2D node => node.Position,
        Control control => control.Position,
        _ => Vector2.Zero,
    };

    private static void SetTravelPosition(CanvasItem item, Vector2 position)
    {
        switch (item)
        {
            case Node2D node:
                node.Position = position;
                break;
            case Control control:
                control.Position = position;
                break;
        }
    }
}
