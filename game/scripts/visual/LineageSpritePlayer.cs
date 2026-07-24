#nullable enable
using Godot;

namespace WorldofGoses;

/// <summary>
/// Directional animation player for one lineage/gender character.
/// Every animation declared in
/// <c>art/world-of-goses-lpc-lineages-reproducible-v2/source/recipes/build.json</c>
/// is reachable through a strongly typed <see cref="Play*"/> helper; the
/// underlying string name follows the <c>{animation}_{down|left|up|right}</c>
/// convention produced by the lineage sprite generator.
///
/// <para>The player does not own the animation cadence or movement —
/// that lives in <see cref="CitizenSpriteCarrier"/>. This class only
/// resolves which animation corresponds to a normalised direction and
/// forwards the call to <see cref="AnimatedSprite2D.Play"/>.</para>
/// </summary>
public partial class LineageSpritePlayer : AnimatedSprite2D
{
    private Vector2 _facing = Vector2.Down;

    public void PlayIdle(Vector2 direction) => PlayDirectional(AnimationState.Idle, direction);
    public void PlayCombatIdle(Vector2 direction) => PlayDirectional(AnimationState.CombatIdle, direction);
    public void PlayWalk(Vector2 direction) => PlayDirectional(AnimationState.Walk, direction);
    public void PlayRun(Vector2 direction) => PlayDirectional(AnimationState.Run, direction);
    public void PlayJump(Vector2 direction) => PlayDirectional(AnimationState.Jump, direction);
    public void PlayClimb(Vector2 direction) => PlayDirectional(AnimationState.Climb, direction);
    public void PlaySit(Vector2 direction) => PlayDirectional(AnimationState.Sit, direction);
    public void PlayHurt(Vector2 direction) => PlayDirectional(AnimationState.Hurt, direction);
    public void PlaySlash(Vector2 direction) => PlayDirectional(AnimationState.Slash, direction);
    public void PlayThrust(Vector2 direction) => PlayDirectional(AnimationState.Thrust, direction);
    public void PlayHalfslash(Vector2 direction) => PlayDirectional(AnimationState.Halfslash, direction);
    public void PlayBackslash(Vector2 direction) => PlayDirectional(AnimationState.Backslash, direction);
    public void PlayShoot(Vector2 direction) => PlayDirectional(AnimationState.Shoot, direction);
    public void PlaySpellcast(Vector2 direction) => PlayDirectional(AnimationState.Spellcast, direction);

    /// <summary>
    /// Resumes the idle animation in the last known facing direction.
    /// Useful when a one-shot animation finishes and the carrier should
    /// settle without re-asserting the direction.
    /// </summary>
    public void ResumeIdle() => Play(AnimationName(AnimationState.Idle, _facing));

    /// <summary>
    /// Plays an arbitrary directional animation by its base name (the
    /// same string declared in <c>build.json</c>). The sprite must have
    /// the matching <c>{name}_{down|left|up|right}</c> animation
    /// registered in its <see cref="SpriteFrames"/>.
    /// </summary>
    public void PlayDirectional(string animationName, Vector2 direction)
    {
        _facing = NormalizeDirection(direction, _facing);
        Play($"{animationName}_{ToSuffix(_facing)}");
    }

    private void PlayDirectional(AnimationState state, Vector2 direction)
    {
        _facing = NormalizeDirection(direction, _facing);
        Play(AnimationName(state, _facing));
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

    private static string AnimationName(AnimationState state, Vector2 direction)
    {
        bool horizontal = Mathf.Abs(direction.X) > Mathf.Abs(direction.Y);
        bool negative = horizontal ? direction.X < 0 : direction.Y < 0;
        string suffix = horizontal
            ? (negative ? "left" : "right")
            : (negative ? "up" : "down");
        return $"{AnimationPrefix(state)}_{suffix}";
    }

    private static string ToSuffix(Vector2 direction)
    {
        if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
        {
            return direction.X < 0 ? "left" : "right";
        }

        return direction.Y < 0 ? "up" : "down";
    }

    private static string AnimationPrefix(AnimationState state) => state switch
    {
        AnimationState.Idle => "idle",
        AnimationState.CombatIdle => "combat_idle",
        AnimationState.Walk => "walk",
        AnimationState.Run => "run",
        AnimationState.Jump => "jump",
        AnimationState.Climb => "climb",
        AnimationState.Sit => "sit",
        AnimationState.Hurt => "hurt",
        AnimationState.Slash => "slash",
        AnimationState.Thrust => "thrust",
        AnimationState.Halfslash => "halfslash",
        AnimationState.Backslash => "backslash",
        AnimationState.Shoot => "shoot",
        AnimationState.Spellcast => "spellcast",
        _ => "idle",
    };

    private enum AnimationState
    {
        Idle,
        CombatIdle,
        Walk,
        Run,
        Jump,
        Climb,
        Sit,
        Hurt,
        Slash,
        Thrust,
        Halfslash,
        Backslash,
        Shoot,
        Spellcast,
    }
}