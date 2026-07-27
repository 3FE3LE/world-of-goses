#nullable enable
using System;
using Godot;
using WorldofGoses;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Prototype <see cref="Camera2D"/> validating the two documented modes for
/// the future walkable world: free pan/zoom always available, and an
/// explicit follow-selected-target toggle independent from selection
/// itself. See
/// docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md
/// ("Cámara y mundo caminable") and
/// docs/world-of-goses-design-bible/04_CITIZENS_PROFESSIONS_AND_HEROES.md
/// ("Cámara-sigue").
/// </summary>
public partial class WalkableWorldCamera : Camera2D
{
    // Expressed as intuitive magnification ("2x closer"), not raw
    // Camera2D.Zoom: Godot's Zoom is inverted (values > 1 show MORE world,
    // i.e. zoom out). ApplyZoomIndex() converts.
    private static readonly float[] MagnificationSteps = { 1f, 1.5f, 2f };
    private const float SelectionRadiusPx = 24f;

    [Signal] public delegate void FollowModeChangedEventHandler(bool isFollowing);
    [Signal] public delegate void TargetSelectedEventHandler(Node2D target);

    private int _zoomIndex;
    private bool _panning;
    private Vector2 _panStartMouse;
    private Vector2 _panStartPosition;
    private Vector2? _panTarget;
    private float _panAccumulator;
    private Node2D? _followTarget;
    private bool _following;

    public override void _Ready() => ApplyZoomIndex();

    public override void _Process(double delta)
    {
        if (_following && _followTarget is not null && IsInstanceValid(_followTarget))
        {
            GlobalPosition = PixelMotion.Snap(_followTarget.GlobalPosition);
            return;
        }
        // Pixel-motion grammar (design bible §08): world-camera pan follows
        // the same discrete 12 Hz / 8 px cadence as character locomotion —
        // it must not track the mouse 1:1 every frame.
        if (_panTarget is not { } target || GlobalPosition == target) return;
        _panAccumulator += (float)delta;
        while (_panAccumulator >= PixelMotion.CadenceSeconds && GlobalPosition != target)
        {
            _panAccumulator -= PixelMotion.CadenceSeconds;
            GlobalPosition = PixelMotion.StepCardinal(GlobalPosition, target);
        }
    }

    /// <summary>
    /// Selecting a target is independent from following it: this only
    /// records which target a later <see cref="ToggleFollow"/> would track.
    /// </summary>
    public void SetFollowTarget(Node2D? target) => _followTarget = target;

    public void ToggleFollow()
    {
        if (_followTarget is null)
        {
            SetFollowing(false);
            return;
        }
        SetFollowing(!_following);
    }

    private void SetFollowing(bool value)
    {
        if (_following == value) return;
        _following = value;
        EmitSignal(SignalName.FollowModeChanged, _following);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventKey { Keycode: Key.F, Pressed: true }:
                ToggleFollow();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }:
                TrySelectAt(GetGlobalMousePosition());
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right } rightClick:
                if (rightClick.Pressed)
                {
                    SetFollowing(false); // manual pan always releases follow
                    _panning = true;
                    _panStartMouse = rightClick.Position;
                    _panStartPosition = GlobalPosition;
                    _panTarget = GlobalPosition;
                }
                else
                {
                    _panning = false;
                }
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true }:
                StepZoom(1);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true }:
                StepZoom(-1);
                break;
            case InputEventMouseMotion mouseMotion when _panning:
                Vector2 dragDelta = mouseMotion.Position - _panStartMouse;
                // Screen-space drag must scale by the current zoom's
                // world-per-pixel ratio, or pan speed would silently change
                // with zoom level. This only updates the target the camera
                // steps toward each cadence tick (see _Process) — not the
                // camera's actual position, so the drag itself reads as
                // chunky/quantized rather than a smooth 1:1 follow.
                _panTarget = PixelMotion.Snap(_panStartPosition - dragDelta * Zoom);
                break;
        }
    }

    private void TrySelectAt(Vector2 globalMouse)
    {
        foreach (Node node in GetTree().GetNodesInGroup(WalkableWorldAvatar.SelectableGroup))
        {
            if (node is not Node2D node2D) continue;
            if (node2D.GlobalPosition.DistanceTo(globalMouse) > SelectionRadiusPx) continue;
            SetFollowTarget(node2D);
            EmitSignal(SignalName.TargetSelected, node2D);
            return;
        }
    }

    private void StepZoom(int direction)
    {
        _zoomIndex = Math.Clamp(_zoomIndex + direction, 0, MagnificationSteps.Length - 1);
        ApplyZoomIndex();
    }

    private void ApplyZoomIndex() => Zoom = Vector2.One / MagnificationSteps[_zoomIndex];
}
