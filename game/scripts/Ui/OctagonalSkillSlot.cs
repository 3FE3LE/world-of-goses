#nullable enable

using System;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Presentation-only Active Skill slot with a real eight-sided silhouette.
/// The eight hidden TraitSide nodes reserve future attachment points without
/// assigning gameplay meaning to any side.
/// </summary>
[GlobalClass]
public partial class OctagonalSkillSlot : Control
{
    public const int SlotWidth = 104;
    public const int SlotHeight = 164;
    private const int BorderInset = 4;
    private const int CornerCut = 20;

    public enum SlotState
    {
        Empty,
        Locked,
        Ready,
        Cooldown,
        Disabled,
    }

    [Signal]
    public delegate void ActivatedEventHandler(int slotNumber);

    private const string UiAcceptAction = "ui_accept";
    private static readonly Vector2[] Octagon =
    {
        new(BorderInset + CornerCut, BorderInset),
        new(SlotWidth - BorderInset - CornerCut, BorderInset),
        new(SlotWidth - BorderInset, BorderInset + CornerCut),
        new(SlotWidth - BorderInset, SlotHeight - BorderInset - CornerCut),
        new(SlotWidth - BorderInset - CornerCut, SlotHeight - BorderInset),
        new(BorderInset + CornerCut, SlotHeight - BorderInset),
        new(BorderInset, SlotHeight - BorderInset - CornerCut),
        new(BorderInset, BorderInset + CornerCut),
    };

    private static readonly Vector2[] ClosedOctagon =
    {
        Octagon[0], Octagon[1], Octagon[2], Octagon[3], Octagon[4],
        Octagon[5], Octagon[6], Octagon[7], Octagon[0],
    };

    private Label _inputNumber = null!;
    private TextureRect _skillIcon = null!;
    private Label _lockIndicator = null!;
    private Label _stateLabel = null!;
    private ProgressBar _cooldownProgress = null!;
    private Label _cooldownLabel = null!;
    private SlotState _state = SlotState.Empty;
    private int _slotNumber = 1;
    private Texture2D? _icon;
    private double _cooldownRemaining;
    private double _cooldownDuration;

    public SlotState State => _state;
    public int SlotNumber => _slotNumber;

    public override void _Ready()
    {
        _inputNumber = GetNode<Label>("InputNumber");
        _skillIcon = GetNode<TextureRect>("SkillIcon");
        _lockIndicator = GetNode<Label>("LockIndicator");
        _stateLabel = GetNode<Label>("StateLabel");
        _cooldownProgress = GetNode<ProgressBar>("CooldownProgress");
        _cooldownLabel = GetNode<Label>("CooldownLabel");

        FocusEntered += OnFocusChanged;
        FocusExited += OnFocusChanged;
        ApplyConfiguration();
    }

    public override void _ExitTree()
    {
        FocusEntered -= OnFocusChanged;
        FocusExited -= OnFocusChanged;
    }

    public override void _Draw()
    {
        Color fill = GetThemeColor(FillColorName(_state));
        Color border = GetThemeColor(BorderColorName(_state));
        DrawColoredPolygon(Octagon, fill);
        DrawPolyline(ClosedOctagon, border, HasFocus() ? 3f : 1f, antialiased: false);
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        bool accepted = inputEvent.IsActionPressed(UiAcceptAction)
            || inputEvent is InputEventMouseButton
            {
                ButtonIndex: MouseButton.Left,
                Pressed: true,
            };
        if (!accepted || _state != SlotState.Ready) return;

        EmitSignal(SignalName.Activated, _slotNumber);
        AcceptEvent();
    }

    public void Configure(
        int slotNumber,
        SlotState state,
        Texture2D? icon = null,
        double cooldownRemaining = 0,
        double cooldownDuration = 0)
    {
        if (slotNumber is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotNumber), slotNumber, "Active Skill slots are numbered 1 through 4.");
        }

        _slotNumber = slotNumber;
        _state = state;
        _icon = icon;
        _cooldownRemaining = Math.Max(0, cooldownRemaining);
        _cooldownDuration = Math.Max(0, cooldownDuration);
        if (IsNodeReady()) ApplyConfiguration();
    }

    public void SetCooldown(double remaining, double duration)
    {
        _cooldownRemaining = Math.Max(0, remaining);
        _cooldownDuration = Math.Max(0, duration);
        _state = _cooldownRemaining > 0 ? SlotState.Cooldown : SlotState.Ready;
        if (IsNodeReady()) ApplyConfiguration();
    }

    private void ApplyConfiguration()
    {
        _inputNumber.Text = _slotNumber.ToString();
        _skillIcon.Texture = _icon;
        _skillIcon.Modulate = LineageThemeRegistry.IconAccent;
        _skillIcon.Visible = _icon is not null
            && _state is SlotState.Ready or SlotState.Cooldown or SlotState.Disabled;

        bool locked = _state == SlotState.Locked;
        bool cooldown = _state == SlotState.Cooldown;
        _lockIndicator.Visible = locked;
        _stateLabel.Text = StateText(_state);
        _stateLabel.Visible = !cooldown;
        _cooldownProgress.Visible = cooldown;
        _cooldownLabel.Visible = cooldown;

        double ratio = _cooldownDuration <= 0
            ? 0
            : Mathf.Clamp(_cooldownRemaining / _cooldownDuration, 0, 1);
        _cooldownProgress.Value = ratio;
        _cooldownLabel.Text = UiText.Format(
            "ui.expedition_live.skill.cooldown",
            _cooldownRemaining);
        QueueRedraw();
    }

    private void OnFocusChanged() => QueueRedraw();

    private static string StateText(SlotState state) => UiText.Get(state switch
    {
        SlotState.Empty => "ui.expedition_live.skill.empty",
        SlotState.Locked => "ui.expedition_live.skill.locked",
        SlotState.Ready => "ui.expedition_live.skill.ready",
        SlotState.Disabled => "ui.expedition_live.skill.disabled",
        _ => "ui.expedition_live.skill.cooldown_label",
    });

    private static string FillColorName(SlotState state) => state switch
    {
        SlotState.Ready => "fill_ready",
        SlotState.Cooldown => "fill_cooldown",
        SlotState.Disabled => "fill_disabled",
        SlotState.Locked => "fill_locked",
        _ => "fill_empty",
    };

    private static string BorderColorName(SlotState state) => state switch
    {
        SlotState.Ready => "border_ready",
        SlotState.Locked => "border_locked",
        SlotState.Disabled => "border_disabled",
        _ => "border_default",
    };
}
