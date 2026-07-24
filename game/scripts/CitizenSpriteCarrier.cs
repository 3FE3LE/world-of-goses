#nullable enable
using System;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// One canonical sprite for one citizen. The carrier handles the
/// movement tween (walk to a target position with the speed derived
/// from the sprite's own animation cadence) and forwards animation
/// calls to the sprite.
/// </summary>
public partial class CitizenSpriteCarrier : Node2D
{
    public enum VisualState
    {
        Hidden,
        Home,
        Entering,
        Working,
        Exiting,
        Macro,
        HeroProfile,
    }

    /// <summary>
    /// Effective walking speed produced by the shared 8 px / 12 Hz
    /// presentation cadence.
    /// </summary>
    public const float WalkSpeedPxPerSec =
        PixelMotion.StepPixels / PixelMotion.CadenceSeconds;
    private static readonly Vector2 MacroScale = new(0.25f, 0.25f);

    public CitizenId Id { get; private set; }
    public LineageId Lineage { get; private set; }
    public GenderId Gender { get; private set; }
    public AppearanceVariantId AppearanceVariant { get; private set; }
    public LineageSpritePlayer Sprite { get; private set; } = null!;
    public VisualState State { get; private set; } = VisualState.Hidden;
    private Vector2? _moveTarget;
    private Action? _moveCompleted;
    private float _moveAccumulator;

    /// <summary>
    /// Creates the carrier's sprite from the lineage/gender pair and
    /// parents it to the carrier. The carrier doesn't need a full
    /// Citizen — the visual layer only needs the visual identity.
    /// </summary>
    internal void Initialize(CitizenId id, LineageId lineage, GenderId gender, AppearanceVariantId appearanceVariant)
    {
        Id = id;
        Lineage = lineage;
        Gender = gender;
        AppearanceVariant = appearanceVariant;
        var bodyVariant = CharacterVisualRegistry.ResolveBodyVariant(gender);
        var scene = CharacterVisualRegistry.LoadScene(lineage, appearanceVariant, bodyVariant);
        Sprite = scene.Instantiate<LineageSpritePlayer>();
        Sprite.Position = Vector2.Zero;
        AddChild(Sprite);
        Hide();
    }

    internal void ReinitializeAppearance(AppearanceVariantId appearanceVariant)
    {
        if (appearanceVariant == AppearanceVariant) return;
        AppearanceVariant = appearanceVariant;
        if (Sprite is not null) Sprite.QueueFree();
        var bodyVariant = CharacterVisualRegistry.ResolveBodyVariant(Gender);
        var scene = CharacterVisualRegistry.LoadScene(Lineage, appearanceVariant, bodyVariant);
        Sprite = scene.Instantiate<LineageSpritePlayer>();
        Sprite.Position = Vector2.Zero;
        AddChild(Sprite);
    }

    /// <summary>
    /// Tweens the carrier from its current position to
    /// <paramref name="targetPosition"/>. The sprite plays
    /// <c>walk_&lt;direction&gt;</c> based on the relative direction.
    /// If a previous tween is in progress, it is cancelled and the
    /// new one starts — so re-assigning during an exit simply turns
    /// the carrier around and walks it back to the new target.
    /// </summary>
    public void GoTo(Vector2 targetPosition, Vector2 hintFacing, Action? onComplete = null)
    {
        Vector2 current = Position;
        Vector2 delta = targetPosition - current;
        if (delta.LengthSquared() < 1f)
        {
            onComplete?.Invoke();
            return;
        }

        Vector2 facing = Math.Abs(delta.X) > Math.Abs(delta.Y)
            ? new Vector2(Math.Sign(delta.X), 0)
            : new Vector2(0, Math.Sign(delta.Y));
        if (hintFacing != Vector2.Zero) facing = hintFacing;
        Sprite.PlayWalk(facing);

        _moveTarget = PixelMotion.Snap(targetPosition);
        _moveCompleted = onComplete;
        _moveAccumulator = 0f;
    }

    /// <summary>
    /// Sets the carrier's position immediately without animation.
    /// Used by the macro view's procedural movement and by the slot
    /// when seeding the carrier at the entry border.
    /// </summary>
    public void SetPositionImmediate(Vector2 position)
    {
        CancelMotion();
        Position = PixelMotion.Snap(position);
    }

    public void SetState(VisualState state)
    {
        State = state;
        Scale = ScaleForState(state);
        Visible = state != VisualState.Hidden;
    }

    internal static Vector2 ScaleForState(VisualState state) =>
        state == VisualState.Macro ? MacroScale : Vector2.One;

    /// <summary>
    /// Cancels any in-flight motion and snaps the sprite to the
    /// current position. Used when the carrier's parent context
    /// changes (e.g., hide) so the next show can start from a clean
    /// state.
    /// </summary>
    public void CancelMotion()
    {
        _moveTarget = null;
        _moveCompleted = null;
        _moveAccumulator = 0f;
    }

    public override void _Process(double delta)
    {
        if (_moveTarget is not Vector2 target) return;
        _moveAccumulator += (float)delta;
        while (_moveAccumulator >= PixelMotion.CadenceSeconds && _moveTarget is not null)
        {
            _moveAccumulator -= PixelMotion.CadenceSeconds;
            Position = PixelMotion.StepCardinal(Position, target);
            if (Position != target) continue;

            Action? completed = _moveCompleted;
            CancelMotion();
            completed?.Invoke();
        }
    }

    public void Slash(Vector2 facing) => Sprite.PlaySlash(facing);
    public void Walk(Vector2 facing) => Sprite.PlayWalk(facing);
    public void Idle(Vector2 facing) => Sprite.PlayIdle(facing);

    /// <summary>Combat-ready idle pose in the given facing.</summary>
    public void CombatIdle(Vector2 facing) => Sprite.PlayCombatIdle(facing);

    /// <summary>Faster gait than <see cref="Walk"/>; same tween cadence.</summary>
    public void Run(Vector2 facing) => Sprite.PlayRun(facing);

    /// <summary>One-shot jump; the carrier does not tween while airborne.</summary>
    public void Jump(Vector2 facing) => Sprite.PlayJump(facing);

    /// <summary>Climbing pose for vertical traversal; loopable.</summary>
    public void Climb(Vector2 facing) => Sprite.PlayClimb(facing);

    /// <summary>Resting pose; the carrier is parked until it leaves.</summary>
    public void Sit(Vector2 facing) => Sprite.PlaySit(facing);

    /// <summary>One-shot reaction to damage. Plays once, then resumes idle.</summary>
    public void Hurt(Vector2 facing) => Sprite.PlayHurt(facing);

    /// <summary>Forwarder for the thrusting attack pose.</summary>
    public void Thrust(Vector2 facing) => Sprite.PlayThrust(facing);

    /// <summary>Forwarder for the half-swipe attack pose.</summary>
    public void Halfslash(Vector2 facing) => Sprite.PlayHalfslash(facing);

    /// <summary>Forwarder for the back-handed slash attack pose.</summary>
    public void Backslash(Vector2 facing) => Sprite.PlayBackslash(facing);

    /// <summary>Ranged attack pose with a bow or crossbow.</summary>
    public void Shoot(Vector2 facing) => Sprite.PlayShoot(facing);

    /// <summary>Spellcasting pose; loopable idle for ceremonial casting.</summary>
    public void Spellcast(Vector2 facing) => Sprite.PlaySpellcast(facing);
}
