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
    /// Walking speed in pixels per second. Calibrated to the sprite's
    /// walk animation: 9 frames at 9 fps = 1 cycle per second, and
    /// the LPC cell is 128×128, so the natural "one sprite-width per
    /// second" reads as 128 px/s.
    /// </summary>
    public const float WalkSpeedPxPerSec = 128f;

    public CitizenId Id { get; private set; }
    public LineageId Lineage { get; private set; }
    public GenderId Gender { get; private set; }
    public LineageSpritePlayer Sprite { get; private set; } = null!;
    public VisualState State { get; private set; } = VisualState.Hidden;
    private Tween? _moveTween;

    /// <summary>
    /// Creates the carrier's sprite from the lineage/gender pair and
    /// parents it to the carrier. The carrier doesn't need a full
    /// Citizen — the visual layer only needs the visual identity.
    /// </summary>
    public void Initialize(CitizenId id, LineageId lineage, GenderId gender)
    {
        Id = id;
        Lineage = lineage;
        Gender = gender;
        var bodyVariant = CharacterVisualRegistry.ResolveBodyVariant(gender);
        var scene = CharacterVisualRegistry.LoadScene(lineage, bodyVariant);
        Sprite = scene.Instantiate<LineageSpritePlayer>();
        Sprite.Position = Vector2.Zero;
        AddChild(Sprite);
        Hide();
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

        _moveTween?.Kill();
        float distance = current.DistanceTo(targetPosition);
        float duration = distance / WalkSpeedPxPerSec;
        _moveTween = CreateTween();
        _moveTween.TweenProperty(this, "position", targetPosition, duration);
        _moveTween.TweenCallback(Callable.From(() => onComplete?.Invoke()));
    }

    /// <summary>
    /// Sets the carrier's position immediately without animation.
    /// Used by the macro view's procedural movement and by the slot
    /// when seeding the carrier at the entry border.
    /// </summary>
    public void SetPositionImmediate(Vector2 position)
    {
        _moveTween?.Kill();
        Position = position;
    }

    public void SetState(VisualState state)
    {
        State = state;
        Visible = state != VisualState.Hidden;
    }

    /// <summary>
    /// Cancels any in-flight motion and snaps the sprite to the
    /// current position. Used when the carrier's parent context
    /// changes (e.g., hide) so the next show can start from a clean
    /// state.
    /// </summary>
    public void CancelMotion()
    {
        _moveTween?.Kill();
        _moveTween = null;
    }

    public void Slash(Vector2 facing) => Sprite.PlaySlash(facing);
    public void Walk(Vector2 facing) => Sprite.PlayWalk(facing);
    public void Idle(Vector2 facing) => Sprite.PlayIdle(facing);
}
