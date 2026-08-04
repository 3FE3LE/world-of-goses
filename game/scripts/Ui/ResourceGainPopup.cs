#nullable enable
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Ui;

/// <summary>
/// Short-lived, quantized icon + amount feedback anchored to the world object
/// that owns founding storage. It contains no resource mutation or storage
/// selection rules.
/// </summary>
public partial class ResourceGainPopup : Node2D
{
    private const int MotionSteps = 12;
    private const int PixelsPerStep = 2;
    private int _step;
    private Timer _motionTimer = null!;
    private Node2D? _followTarget;
    private Vector2 _followOffset;
    private Vector2 _lastFollowBase;

    public static void ShowGain(
        Node parent,
        ResourceType resource,
        int amount,
        Vector2 position)
    {
        if (amount <= 0) return;
        var popup = new ResourceGainPopup
        {
            Position = PixelMotion.Snap(position),
            ZIndex = 120,
        };
        parent.AddChild(popup);
        popup.Build(resource, amount);
    }

    public static void ShowGainFollowing(
        Node2D parent,
        Node2D target,
        ResourceType resource,
        int amount,
        Vector2 offset)
    {
        if (amount <= 0 || !IsInstanceValid(target)) return;
        var popup = new ResourceGainPopup
        {
            _followTarget = target,
            _followOffset = offset,
            ZIndex = 120,
        };
        parent.AddChild(popup);
        popup._lastFollowBase = popup.ResolveFollowBase();
        popup.Position = FollowedPosition(popup._lastFollowBase, offset, 0);
        popup.Build(resource, amount);
    }

    internal static Vector2 FollowedPosition(
        Vector2 targetPosition,
        Vector2 offset,
        int motionStep) => PixelMotion.Snap(
            targetPosition + offset + Vector2.Up * PixelsPerStep * motionStep);

    private void Build(ResourceType resource, int amount)
    {
        var content = new HBoxContainer
        {
            Position = new Vector2(-28, -20),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        content.AddThemeConstantOverride("separation", 4);
        AddChild(content);

        var icon = new ResourceIcon(resource)
        {
            CustomMinimumSize = new Vector2(20, 20),
        };
        content.AddChild(icon);
        var amountLabel = new Label
        {
            Text = $"+{amount}",
            ThemeTypeVariation = "SectionTitle",
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        amountLabel.AddThemeColorOverride("font_color", new Color("#f0e5c8"));
        amountLabel.AddThemeColorOverride("font_shadow_color", new Color(0.08f, 0.06f, 0.04f, 1f));
        amountLabel.AddThemeConstantOverride("shadow_offset_x", 2);
        amountLabel.AddThemeConstantOverride("shadow_offset_y", 2);
        content.AddChild(amountLabel);

        _motionTimer = new Timer
        {
            OneShot = false,
            WaitTime = PixelMotion.CadenceSeconds,
        };
        _motionTimer.Timeout += AdvanceMotion;
        AddChild(_motionTimer);
        _motionTimer.Start();
    }

    private void AdvanceMotion()
    {
        _step++;
        if (IsInstanceValid(_followTarget))
        {
            _lastFollowBase = ResolveFollowBase();
            Position = FollowedPosition(_lastFollowBase, _followOffset, _step);
        }
        else if (_followTarget is not null)
        {
            Position = FollowedPosition(_lastFollowBase, _followOffset, _step);
        }
        else
        {
            Position = PixelMotion.Snap(Position + Vector2.Up * PixelsPerStep);
        }
        if (_step >= MotionSteps - 3)
        {
            float alpha = Mathf.Clamp((MotionSteps - _step) / 3f, 0f, 1f);
            Modulate = new Color(1f, 1f, 1f, alpha);
        }
        if (_step < MotionSteps) return;
        _motionTimer.Stop();
        QueueFree();
    }

    private Vector2 ResolveFollowBase()
    {
        if (!IsInstanceValid(_followTarget)) return _lastFollowBase;
        return GetParent() is Node2D parent
            ? parent.ToLocal(_followTarget!.GlobalPosition)
            : _followTarget!.GlobalPosition;
    }
}
