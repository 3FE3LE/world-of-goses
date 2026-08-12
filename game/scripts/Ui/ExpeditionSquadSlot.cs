#nullable enable

using System;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Read-only presentation slot for one future expedition vanguard position.
/// It accepts prepared display values and owns no Citizen or Expedition rules.
/// </summary>
[GlobalClass]
public partial class ExpeditionSquadSlot : PanelContainer
{
    public enum SlotState
    {
        Active,
        Empty,
        Locked,
    }

    [Signal]
    public delegate void SelectedEventHandler(int slotNumber);

    private const string UiAcceptAction = UiInputActions.Accept;

    private Label _slotNumberLabel = null!;
    private TextureRect _portrait = null!;
    private Label _nameLabel = null!;
    private Label _stateLabel = null!;
    private Label _lockIndicator = null!;
    private Label _hpLabel = null!;
    private ProgressBar _hpProgress = null!;
    private Label _secondaryLabel = null!;
    private ProgressBar _secondaryProgress = null!;
    private Label _criticalState = null!;
    private int _slotNumber = 1;
    private SlotState _state = SlotState.Empty;
    private Texture2D? _portraitTexture;
    private string _shortName = string.Empty;
    private double? _hpRatio;
    private string _secondaryName = string.Empty;
    private double _secondaryRatio;
    private string _criticalText = string.Empty;

    public SlotState State => _state;
    public int SlotNumber => _slotNumber;

    public override void _Ready()
    {
        _slotNumberLabel = GetNode<Label>("Content/Layout/SlotNumber");
        _portrait = GetNode<TextureRect>("Content/Layout/Body/Portrait");
        _nameLabel = GetNode<Label>("Content/Layout/Body/Facts/Name");
        _stateLabel = GetNode<Label>("Content/Layout/Body/Facts/State");
        _lockIndicator = GetNode<Label>("Content/Layout/Body/Facts/LockIndicator");
        _hpLabel = GetNode<Label>("Content/Layout/Body/Facts/HpLabel");
        _hpProgress = GetNode<ProgressBar>("Content/Layout/Body/Facts/HpProgress");
        _secondaryLabel = GetNode<Label>("Content/Layout/Body/Facts/SecondaryLabel");
        _secondaryProgress = GetNode<ProgressBar>("Content/Layout/Body/Facts/SecondaryProgress");
        _criticalState = GetNode<Label>("Content/Layout/CriticalState");

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
        if (!HasFocus()) return;
        DrawRect(
            new Rect2(Vector2.Zero, Size),
            GetThemeColor("font_focus_color", "HudButton"),
            filled: false,
            width: 2,
            antialiased: false);
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        bool accepted = inputEvent.IsActionPressed(UiAcceptAction)
            || inputEvent is InputEventMouseButton
            {
                ButtonIndex: MouseButton.Left,
                Pressed: true,
            };
        if (!accepted || _state != SlotState.Active) return;

        EmitSignal(SignalName.Selected, _slotNumber);
        AcceptEvent();
    }

    public void Configure(
        int slotNumber,
        SlotState state,
        Texture2D? portrait = null,
        string? shortName = null,
        double? hpRatio = null,
        string? secondaryName = null,
        double secondaryRatio = 0,
        string? criticalState = null)
    {
        if (slotNumber is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotNumber), slotNumber, "Vanguard slots are numbered 1 through 4.");
        }

        _slotNumber = slotNumber;
        _state = state;
        _portraitTexture = portrait;
        _shortName = shortName ?? string.Empty;
        _hpRatio = hpRatio.HasValue ? Mathf.Clamp(hpRatio.Value, 0, 1) : null;
        _secondaryName = secondaryName ?? string.Empty;
        _secondaryRatio = Mathf.Clamp(secondaryRatio, 0, 1);
        _criticalText = criticalState ?? string.Empty;
        if (IsNodeReady()) ApplyConfiguration();
    }

    private void ApplyConfiguration()
    {
        bool active = _state == SlotState.Active;
        bool locked = _state == SlotState.Locked;
        bool hasSecondary = active && !string.IsNullOrWhiteSpace(_secondaryName);

        _slotNumberLabel.Text = _slotNumber.ToString();
        _portrait.Texture = _portraitTexture;
        _portrait.Modulate = LineageThemeRegistry.IconAccent;
        _portrait.Visible = active && _portraitTexture is not null;
        _nameLabel.Text = _shortName;
        _nameLabel.Visible = active && !string.IsNullOrWhiteSpace(_shortName);
        _stateLabel.Text = StateText(_state);
        _lockIndicator.Visible = locked;

        bool hasHealth = active && _hpRatio.HasValue;
        int hpPercent = Mathf.RoundToInt((_hpRatio ?? 0) * 100);
        _hpLabel.Text = UiText.Format("ui.expedition_live.squad.hp", hpPercent);
        _hpLabel.Visible = hasHealth;
        _hpProgress.Value = _hpRatio ?? 0;
        _hpProgress.Visible = hasHealth;

        int secondaryPercent = Mathf.RoundToInt(_secondaryRatio * 100);
        _secondaryLabel.Text = UiText.Format(
            "ui.expedition_live.squad.secondary",
            _secondaryName,
            secondaryPercent);
        _secondaryLabel.Visible = hasSecondary;
        _secondaryProgress.Value = _secondaryRatio;
        _secondaryProgress.Visible = hasSecondary;

        _criticalState.Text = string.IsNullOrWhiteSpace(_criticalText)
            ? string.Empty
            : $"[!] {_criticalText}";
        _criticalState.Visible = active && !string.IsNullOrWhiteSpace(_criticalText);
        QueueRedraw();
    }

    private void OnFocusChanged() => QueueRedraw();

    private static string StateText(SlotState state) => UiText.Get(state switch
    {
        SlotState.Active => "ui.expedition_live.squad.active",
        SlotState.Locked => "ui.expedition_live.squad.locked",
        _ => "ui.expedition_live.squad.empty",
    });
}
