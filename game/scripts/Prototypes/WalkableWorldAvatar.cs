#nullable enable
using Godot;
using WorldofGoses;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Prototype-only walkable avatar validating cardinal, cadence-quantized
/// movement (reusing <see cref="PixelMotion"/>, the project's existing
/// "pixel-motion grammar") against the level-edge collision built by
/// <see cref="ElevationTestLayout"/>. This is not the real hero/citizen —
/// see
/// docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md,
/// "Cámara y mundo caminable".
/// </summary>
public partial class WalkableWorldAvatar : CharacterBody2D
{
    /// <summary>Group <see cref="WalkableWorldCamera"/> scans for a click-to-select target.</summary>
    public const string SelectableGroup = "selectable";

    private const float BodySizePx = 16f;
    private static readonly Color BodyColor = new("#d9a24e");

    private float _accumulator;

    public override void _Ready()
    {
        AddToGroup(SelectableGroup);
        AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(BodySizePx, BodySizePx) },
        });
        Position = PixelMotion.Snap(Position);
    }

    public override void _Draw()
    {
        DrawRect(
            new Rect2(
                new Vector2(-BodySizePx * 0.5f, -BodySizePx * 0.5f),
                new Vector2(BodySizePx, BodySizePx)),
            BodyColor);
    }

    public override void _PhysicsProcess(double delta)
    {
        _accumulator += (float)delta;
        while (_accumulator >= PixelMotion.CadenceSeconds)
        {
            _accumulator -= PixelMotion.CadenceSeconds;
            TryStep();
        }
    }

    private void TryStep()
    {
        Vector2 direction = ReadDirection();
        if (direction == Vector2.Zero) return;
        Vector2 motion = direction * PixelMotion.StepPixels;
        KinematicCollision2D? collision = MoveAndCollide(motion, testOnly: true);
        if (collision is not null) return; // blocked by a level-edge wall
        Position = PixelMotion.Snap(Position + motion);
        QueueRedraw();
    }

    private static Vector2 ReadDirection()
    {
        if (Input.IsActionPressed("ui_left")) return Vector2.Left;
        if (Input.IsActionPressed("ui_right")) return Vector2.Right;
        if (Input.IsActionPressed("ui_up")) return Vector2.Up;
        if (Input.IsActionPressed("ui_down")) return Vector2.Down;
        return Vector2.Zero;
    }
}
