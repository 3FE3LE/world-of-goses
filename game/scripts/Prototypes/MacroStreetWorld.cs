#nullable enable
using Godot;
using WorldofGoses;

using WorldofGoses.Ui;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Prototype-only macro city depth navigation: a handful of placeholder
/// "calles" (depth rows), each with a couple of placeholder buildings,
/// projected through <see cref="StreetDepthProjection"/>. Vertical movement
/// steps discretely between adjacent streets with a brief quantized
/// transition (never a continuous scroll); horizontal movement is
/// cadence-quantized and confined to the current street. Not the real city
/// — see
/// docs/presentation/visual-language.md,
/// "Ciudad macro (perspectiva por calles)".
/// </summary>
public partial class MacroStreetWorld : Node2D
{
    private const int StreetCount = 5;
    private const float LateralHalfWidthPx = 460f;
    private const float CenterX = 320f;
    private const float BaseY = 380f;
    private const float BuildingSize = 64f;
    private const float RoadHeightPx = 24f;
    private const float AvatarSize = 24f;

    // Street-change transitions and lateral steps both advance on this same
    // cadence (design bible §08, "Pixel-motion grammar") — no continuous
    // tweening, only a handful of discrete frames per transition.
    private const int TransitionSteps = 5;
    private const float DepthStepSize = 1f / TransitionSteps;

    // More columns than a first pass needs, deliberately: this is the test
    // bed for "what happens when the city grows wide and the avatar stands
    // near an edge column", not just the centered two-column case.
    private static readonly float[] BuildingLateralOffsets =
        { -400f, -240f, -80f, 80f, 240f, 400f };
    private static readonly Color[] StreetColors =
    {
        new("#7a6a4f"), new("#6d6148"), new("#605841"), new("#534f3a"), new("#464633"),
    };
    private static readonly Color AvatarColor = new("#d9a24e");

    private int _currentStreet;
    private float _depthAnchor;
    private float? _depthTarget;
    private float _lateralPosition;
    private float _lateralAccumulator;
    private float _transitionAccumulator;

    public override void _Ready() => QueueRedraw();

    public override void _PhysicsProcess(double delta)
    {
        AdvanceLateralMovement(delta);
        AdvanceStreetTransition(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent) return;
        if (keyEvent.Keycode is Key.Up or Key.W) StepStreet(1);
        else if (keyEvent.Keycode is Key.Down or Key.S) StepStreet(-1);
    }

    /// <summary>Requests a discrete transition to the adjacent street; ignored mid-transition.</summary>
    private void StepStreet(int direction)
    {
        if (_depthTarget.HasValue) return;
        int nextStreet = Mathf.Clamp(_currentStreet + direction, 0, StreetCount - 1);
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
            -LateralHalfWidthPx,
            LateralHalfWidthPx);
        if (next == _lateralPosition) return;
        _lateralPosition = next;
        QueueRedraw();
    }

    private static float ReadLateralDirection()
    {
        if (Input.IsActionPressed(UiInputActions.Left)) return -1f;
        if (Input.IsActionPressed(UiInputActions.Right)) return 1f;
        return 0f;
    }

    public override void _Draw()
    {
        for (int street = StreetCount - 1; street >= 0; street--)
        {
            if (!StreetDepthProjection.IsVisibleDepth(street - _depthAnchor)) continue;
            DrawStreetRow(street);
        }
        DrawAvatar();
    }

    /// <summary>
    /// Every lateral offset projected here is relative to the avatar's own
    /// <see cref="_lateralPosition"/>, not a world-fixed center — the
    /// vanishing point is wherever the viewer currently stands, so whichever
    /// column the avatar is nearest to reads as vertical ("|"), and columns
    /// converge toward the avatar's screen position as depth increases
    /// (never toward a fixed world coordinate regardless of where the
    /// player is). See <see cref="DrawAvatar"/>, which is the same
    /// projection with a relative offset of exactly zero.
    /// </summary>
    private void DrawStreetRow(int street)
    {
        float depth = street - _depthAnchor;
        Color color = StreetColors[street % StreetColors.Length];
        (Vector2 roadPosition, Vector2 roadScale) =
            StreetDepthProjection.Project(depth, -_lateralPosition, CenterX, BaseY);
        var roadSize = new Vector2(2f * LateralHalfWidthPx * roadScale.X, RoadHeightPx * roadScale.Y);
        DrawRect(new Rect2(roadPosition - roadSize * 0.5f, roadSize), color);

        foreach (float lateralOffset in BuildingLateralOffsets)
        {
            float relativeOffset = lateralOffset - _lateralPosition;
            (Vector2 position, Vector2 scale) =
                StreetDepthProjection.Project(depth, relativeOffset, CenterX, BaseY);
            var size = new Vector2(BuildingSize * scale.X, BuildingSize * scale.Y);
            DrawRect(new Rect2(position - size * 0.5f, size), color.Lightened(0.18f));
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
