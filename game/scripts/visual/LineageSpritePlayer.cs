using Godot;

namespace WorldofGoses;

public partial class LineageSpritePlayer : AnimatedSprite2D
{
    public static readonly StringName IdleDown = "idle_down";
    public static readonly StringName IdleLeft = "idle_left";
    public static readonly StringName IdleUp = "idle_up";
    public static readonly StringName IdleRight = "idle_right";
    public static readonly StringName WalkDown = "walk_down";
    public static readonly StringName WalkLeft = "walk_left";
    public static readonly StringName WalkUp = "walk_up";
    public static readonly StringName WalkRight = "walk_right";
    public static readonly StringName SlashDown = "slash_down";
    public static readonly StringName SlashLeft = "slash_left";
    public static readonly StringName SlashUp = "slash_up";
    public static readonly StringName SlashRight = "slash_right";

    private Vector2 _facing = Vector2.Down;

    public void PlayIdle(Vector2 direction)
    {
        _facing = NormalizeDirection(direction, _facing);
        Play(AnimationFor(AnimationState.Idle, _facing));
    }

    public void PlayWalk(Vector2 direction)
    {
        _facing = NormalizeDirection(direction, _facing);
        Play(AnimationFor(AnimationState.Walk, _facing));
    }

    public void PlaySlash(Vector2 direction)
    {
        _facing = NormalizeDirection(direction, _facing);
        Play(AnimationFor(AnimationState.Slash, _facing));
    }

    public void ResumeIdle()
    {
        Play(AnimationFor(AnimationState.Idle, _facing));
    }

    private static Vector2 NormalizeDirection(Vector2 direction, Vector2 fallback)
    {
        if (direction.IsZeroApprox())
        {
            return fallback;
        }

        return Mathf.Abs(direction.X) > Mathf.Abs(direction.Y)
            ? new Vector2(Mathf.Sign(direction.X), 0)
            : new Vector2(0, Mathf.Sign(direction.Y));
    }

    private static StringName AnimationFor(AnimationState state, Vector2 direction)
    {
        if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
        {
            return (state, direction.X < 0) switch
            {
                (AnimationState.Idle, true) => IdleLeft,
                (AnimationState.Idle, false) => IdleRight,
                (AnimationState.Walk, true) => WalkLeft,
                (AnimationState.Walk, false) => WalkRight,
                (AnimationState.Slash, true) => SlashLeft,
                _ => SlashRight,
            };
        }

        return (state, direction.Y < 0) switch
        {
            (AnimationState.Idle, true) => IdleUp,
            (AnimationState.Idle, false) => IdleDown,
            (AnimationState.Walk, true) => WalkUp,
            (AnimationState.Walk, false) => WalkDown,
            (AnimationState.Slash, true) => SlashUp,
            _ => SlashDown,
        };
    }

    private enum AnimationState
    {
        Idle,
        Walk,
        Slash,
    }
}
