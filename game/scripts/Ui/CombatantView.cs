#nullable enable
using System;
using System.Collections.Generic;
using Godot;
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
        QueueRedraw();
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
        int footY = 82;
        bool facesRight = _snapshot.Facing == CombatFacing.Right;
        if (_snapshot.Defeated)
        {
            DrawRect(new Rect2I(12, footY - 10, 40, 10), body.Darkened(0.35f));
        }
        else
        {
            int top = footY - bodyHeight;
            DrawRect(new Rect2I(25, top, 14, 14), body);
            DrawRect(new Rect2I(21, top + 16, 22, bodyHeight - 20), body);
            DrawRect(new Rect2I(17, footY - 4, 12, 4), body);
            DrawRect(new Rect2I(35, footY - 4, 12, 4), body);
            int armX = facesRight ? 43 : 13;
            DrawRect(new Rect2I(armX, top + 20, 8, 5), body);
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
        if (!animate || Position == targetPosition)
        {
            Position = targetPosition;
            return;
        }

        Vector2 start = Position;
        _movementTween = CreateTween();
        _movementTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Linear);
        _movementTween.TweenMethod(
            Callable.From<float>(progress => Position = InterpolatedPixelPosition(
                start,
                targetPosition,
                progress)),
            0f,
            1f,
            InterpolationSeconds);
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
