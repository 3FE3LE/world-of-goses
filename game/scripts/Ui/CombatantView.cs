#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;

namespace WorldofGoses.Ui;

/// <summary>Replaceable visual shell for one domain combatant.</summary>
public partial class CombatantView : Control
{
    private const float InterpolationSeconds = 0.16f;
    private Label _feedback = null!;
    private CombatParticipantState? _snapshot;
    private CombatSide _side;
    private int _visualIndex;
    private CombatSpatialActivity _displayActivity;
    private Tween? _movementTween;
    private Tween? _feedbackTween;
    private Tween? _recoilTween;
    private LineageSpritePlayer? _sprite;

    /// <summary>
    /// Where the domain says this combatant is, in screen pixels.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="Control.Position"/> because the drawn position
    /// is this plus a transient hit reaction. Writing both into Position
    /// directly would make the two tweens fight, and whichever wrote last would
    /// decide where a combatant stands.
    /// </remarks>
    private Vector2 _domainPosition;

    /// <summary>Transient shove, in screen pixels. Always decays to zero.</summary>
    private float _recoilX;

    /// <summary>
    /// Feet line inside the 64x96 shell. Both the drawn placeholder and the
    /// LPC sprite stand on it, so the two read at the same scale side by side.
    /// </summary>
    private const int FootY = 82;

    /// <summary>
    /// Width of the shell, as declared in <c>CombatantView.tscn</c>.
    /// </summary>
    /// <remarks>
    /// A constant and not <c>Size.X</c>: the sprite is attached the frame the
    /// view is instantiated, before layout has run, and <c>Size.X</c> still
    /// reads zero there — which put every character half a shell to the left.
    /// </remarks>
    private const int ShellWidth = 64;

    /// <summary>
    /// Gives this combatant its citizen's own character sprite.
    /// </summary>
    /// <remarks>
    /// Only party members have one: <c>CombatParticipantState.CitizenId</c> is
    /// null for everything out of <c>EnemyCatalog</c>, and the four enemy
    /// archetypes have no art at all yet. Those keep the drawn placeholder, so
    /// this change cannot regress them — and the contrast is the point.
    /// </remarks>
    public void UseCharacterSprite(
        LineageId lineage, GenderId gender, AppearanceVariantId appearance)
    {
        if (_sprite is not null) return;

        CharacterBodyVariant bodyVariant = CharacterVisualRegistry.ResolveBodyVariant(gender);
        _sprite = CharacterVisualRegistry
            .LoadScene(lineage, appearance, bodyVariant)
            .Instantiate<LineageSpritePlayer>();
        // The node position is the character's feet, not its centre. Every
        // lineage scene declares `centered = true` with `offset = (0, -62)`,
        // which is the project's existing convention for standing a 128 px cell
        // on a ground point — the body occupies roughly 28x46 px low in that
        // cell, and the rest is room for a weapon swing. Subtracting the 62
        // here as well, which is what a reading of the pixels alone suggests,
        // hangs the character a whole body above its own health bar.
        _sprite.Position = new Vector2(ShellWidth * 0.5f, FootY);
        _sprite.ZIndex = -1;
        AddChild(_sprite);
        MoveChild(_sprite, 0);
        QueueRedraw();
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        _feedback = GetNode<Label>("Feedback");
        QueueRedraw();
    }

    public void ApplySnapshot(
        CombatParticipantState snapshot,
        CombatSide side,
        int visualIndex,
        Vector2I targetPosition,
        bool animate,
        IReadOnlyList<CombatLogEntry> events)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(events);
        _snapshot = snapshot;
        _side = side;
        _visualIndex = visualIndex;
        _displayActivity = DisplayActivity(snapshot, events);
        ZIndex = 10 + visualIndex;
        MovePresentationTo(targetPosition, animate);
        ApplyFeedback(events);
        PlayActivity();
        QueueRedraw();
    }

    /// <summary>
    /// Drives the character sprite from the activity the domain already
    /// resolved, never from anything the view decides for itself.
    /// </summary>
    /// <remarks>
    /// <see cref="DisplayActivity"/> reads the snapshot and this step's events
    /// and returns one <see cref="CombatSpatialActivity"/>; this only chooses a
    /// clip for it. That direction of flow is the rule the whole combat surface
    /// runs on — presentation projects domain state and never writes back — and
    /// it is what will let an animated encounter and an unobserved one resolve
    /// identically.
    /// </remarks>
    private void PlayActivity()
    {
        if (_sprite is null || _snapshot is null) return;

        Vector2 facing = _snapshot.Facing == CombatFacing.Right ? Vector2.Right : Vector2.Left;
        switch (_displayActivity)
        {
            case CombatSpatialActivity.Defeated:
            case CombatSpatialActivity.Knockback:
            case CombatSpatialActivity.Hit:
                _sprite.PlayHurt(facing);
                break;
            case CombatSpatialActivity.ActiveSkill:
                _sprite.PlaySpellcast(facing);
                break;
            case CombatSpatialActivity.BasicAttack:
                _sprite.PlaySlash(facing);
                break;
            case CombatSpatialActivity.Approaching:
                _sprite.PlayWalk(facing);
                break;
            default:
                _sprite.PlayCombatIdle(facing);
                break;
        }
    }

    public override void _Draw()
    {
        if (_snapshot is null) return;
        Color body = _side == CombatSide.Party
            ? LineageThemeRegistry.IconAccent
            : GetThemeColor("font_color", "HudButtonDanger");
        Color outline = GetThemeColor("border_locked", "OctagonalSkillSlot");
        Color healthBack = GetThemeColor("fill_empty", "OctagonalSkillSlot");
        Color health = GetThemeColor("fill_ready", "OctagonalSkillSlot");

        int bodyHeight = _snapshot.Stature switch
        {
            CombatStature.Small => 38,
            CombatStature.Tall => 52,
            CombatStature.Large => 58,
            _ => 44,
        };
        int footY = FootY;
        bool facesRight = _snapshot.Facing == CombatFacing.Right;
        if (_snapshot.Defeated)
        {
            DrawRect(new Rect2I(12, footY - 10, 40, 10), body.Darkened(0.35f));
        }
        else if (_sprite is null)
        {
            // The drawn placeholder: a figure assembled from six rectangles.
            // It stays for combatants with no art — today every enemy — rather
            // than being deleted, so an unarted combatant is visibly a
            // placeholder instead of being invisible.
            int top = footY - bodyHeight;
            DrawRect(new Rect2I(25, top, 14, 14), body);
            DrawRect(new Rect2I(21, top + 16, 22, bodyHeight - 20), body);
            DrawRect(new Rect2I(17, footY - 4, 12, 4), body);
            DrawRect(new Rect2I(35, footY - 4, 12, 4), body);
            int armX = facesRight ? 43 : 13;
            DrawRect(new Rect2I(armX, top + 20, 8, 5), body);
        }
        else
        {
            // A contact shadow under the sprite. Without it a character on a
            // parallax backdrop floats, because nothing else ties it to the
            // ground line the domain positions it on.
            DrawRect(new Rect2I(20, footY - 4, 24, 4), body.Darkened(0.55f));
        }

        DrawRect(new Rect2I(7, 4, 50, 7), healthBack);
        int healthWidth = Mathf.RoundToInt((float)Math.Clamp(
            _snapshot.MaxHealth <= 0 ? 0 : _snapshot.CurrentHealth / _snapshot.MaxHealth,
            0,
            1) * 48);
        DrawRect(new Rect2I(8, 5, healthWidth, 5), health);
        DrawRect(new Rect2I(7, 4, 50, 7), outline, filled: false, width: 1);

        DrawActivityCue(_displayActivity, body, outline, facesRight);
    }

    private void MovePresentationTo(Vector2I targetPosition, bool animate)
    {
        _movementTween?.Kill();
        if (!animate || _domainPosition == targetPosition)
        {
            _domainPosition = targetPosition;
            ApplyPosition();
            return;
        }

        Vector2 start = _domainPosition;
        _movementTween = CreateTween();
        _movementTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Linear);
        _movementTween.TweenMethod(
            Callable.From<float>(progress =>
            {
                _domainPosition = InterpolatedPixelPosition(start, targetPosition, progress);
                ApplyPosition();
            }),
            0f,
            1f,
            InterpolationSeconds);
    }

    /// <summary>
    /// Composes the one drawn position out of the two things that decide it.
    /// </summary>
    private void ApplyPosition() =>
        Position = new Vector2I(
            Mathf.RoundToInt(_domainPosition.X + _recoilX),
            Mathf.RoundToInt(_domainPosition.Y));

    /// <summary>
    /// Throws this combatant back from a blow and settles it again.
    /// </summary>
    /// <remarks>
    /// Purely presentational. The recoil decays to exactly zero, so the figure
    /// always ends on the position the domain gave it — an encounter watched and
    /// an encounter resolved offline put everyone in the same place.
    /// <para>
    /// The magnitude arrives already computed, from <see cref="HitReaction"/>.
    /// This view knows where it is and nothing about where anyone else is, and a
    /// shove needs both; the stage holds every placement, but the stage is a
    /// <c>Control</c> and cannot be run in a test.
    /// </para>
    /// </remarks>
    public void ReactToHit(double shovePixels)
    {
        if (Math.Abs(shovePixels) < 0.5) return;
        StartRecoil((float)shovePixels);
    }

    private void StartRecoil(float shovePixels)
    {
        _recoilTween?.Kill();
        _recoilX = shovePixels;
        ApplyPosition();

        _recoilTween = CreateTween();
        _recoilTween.TweenMethod(
            Callable.From<float>(elapsed =>
            {
                _recoilX = (float)HitReaction.Remaining(shovePixels, elapsed);
                ApplyPosition();
            }),
            0f,
            (float)HitReaction.DecaySeconds,
            (float)HitReaction.DecaySeconds);
    }

    internal static Vector2I InterpolatedPixelPosition(
        Vector2 start,
        Vector2 target,
        float progress) => new(
            Mathf.RoundToInt(Mathf.Lerp(start.X, target.X, Math.Clamp(progress, 0, 1))),
            Mathf.RoundToInt(Mathf.Lerp(start.Y, target.Y, Math.Clamp(progress, 0, 1))));

    private void ApplyFeedback(IReadOnlyList<CombatLogEntry> events)
    {
        CombatLogEntry? damage = null;
        foreach (CombatLogEntry entry in events)
        {
            if (entry.TargetId == _snapshot?.Id && entry.Resolution is not null) damage = entry;
        }

        if (damage?.Resolution is { } resolution)
            ShowFeedback($"-{Mathf.RoundToInt(resolution.FinalResult)}");
    }

    private static CombatSpatialActivity DisplayActivity(
        CombatParticipantState snapshot,
        IReadOnlyList<CombatLogEntry> events)
    {
        if (snapshot.Defeated) return CombatSpatialActivity.Defeated;
        foreach (CombatLogEntry entry in events)
        {
            if (entry.TargetId == snapshot.Id && entry.Kind == CombatLogKind.KnockbackApplied)
                return CombatSpatialActivity.Knockback;
        }
        foreach (CombatLogEntry entry in events)
        {
            if (entry.ActorId == snapshot.Id && entry.Kind == CombatLogKind.TechniqueResolved)
                return CombatSpatialActivity.ActiveSkill;
        }
        foreach (CombatLogEntry entry in events)
        {
            if (entry.ActorId == snapshot.Id && entry.Kind == CombatLogKind.BasicAttackResolved)
                return CombatSpatialActivity.BasicAttack;
        }
        foreach (CombatLogEntry entry in events)
        {
            if (entry.TargetId == snapshot.Id && entry.Resolution is not null)
                return CombatSpatialActivity.Hit;
        }
        return snapshot.Activity;
    }

    private void ShowFeedback(string text)
    {
        _feedbackTween?.Kill();
        _feedback.Text = text;
        _feedback.Position = new Vector2(0, 12);
        _feedback.Modulate = Colors.White;
        _feedback.Show();
        _feedbackTween = CreateTween();
        _feedbackTween.SetParallel();
        _feedbackTween.TweenMethod(
            Callable.From<float>(value => _feedback.Position = new Vector2(0, Mathf.RoundToInt(value))),
            12f,
            2f,
            0.36f);
        _feedbackTween.TweenProperty(_feedback, "modulate:a", 0f, 0.36f);
        _feedbackTween.Chain().TweenCallback(Callable.From(_feedback.Hide));
    }

    private void DrawActivityCue(
        CombatSpatialActivity activity,
        Color body,
        Color outline,
        bool facesRight)
    {
        int direction = facesRight ? 1 : -1;
        int front = facesRight ? 54 : 10;
        switch (activity)
        {
            case CombatSpatialActivity.Approaching:
                DrawLine(new Vector2I(18, 88), new Vector2I(28, 88), outline, 2, false);
                DrawLine(new Vector2I(36, 91), new Vector2I(46, 91), outline, 2, false);
                break;
            case CombatSpatialActivity.BasicAttack:
                DrawLine(new Vector2I(front, 48), new Vector2I(front + direction * 8, 44), body, 2, false);
                break;
            case CombatSpatialActivity.ActiveSkill:
                DrawArc(new Vector2I(32, 54), 27, 0, Mathf.Tau, 8, outline, 2, false);
                break;
            case CombatSpatialActivity.Hit:
                DrawLine(new Vector2I(14, 30), new Vector2I(20, 36), outline, 2, false);
                DrawLine(new Vector2I(20, 30), new Vector2I(14, 36), outline, 2, false);
                break;
            case CombatSpatialActivity.Knockback:
                DrawLine(new Vector2I(5, 42), new Vector2I(13, 42), outline, 2, false);
                DrawLine(new Vector2I(3, 48), new Vector2I(13, 48), outline, 2, false);
                break;
        }
    }
}
