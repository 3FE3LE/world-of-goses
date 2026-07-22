#nullable enable
using Godot;

namespace WorldofGoses.Ui;

[GlobalClass]
public partial class AssignmentRow : HBoxContainer
{
    [Signal] public delegate void ActionRequestedEventHandler(int itemId);

    private Label _nameLabel = null!;
    private TooltipButton _actionButton = null!;
    private int _itemId;
    private string _displayName = string.Empty;
    private string _actionLabel = string.Empty;
    private string _tooltip = string.Empty;
    private bool _disabled;

    public Button ActionButton => _actionButton;

    public override void _Ready()
    {
        _nameLabel = GetNode<Label>("Name");
        _actionButton = GetNode<TooltipButton>("ActionButton");
        _actionButton.Pressed += OnPressed;
        ApplyConfiguration();
    }

    public override void _ExitTree()
    {
        if (_actionButton is not null) _actionButton.Pressed -= OnPressed;
    }

    public void Configure(int itemId, string displayName, string actionLabel, string tooltip, bool disabled = false)
    {
        _itemId = itemId;
        _displayName = displayName;
        _actionLabel = actionLabel;
        _tooltip = tooltip;
        _disabled = disabled;
        if (IsNodeReady()) ApplyConfiguration();
    }

    private void ApplyConfiguration()
    {
        _nameLabel.Text = _displayName;
        _actionButton.Text = _actionLabel;
        _actionButton.TooltipText = _tooltip;
        _actionButton.Disabled = _disabled;
        bool removesAssignment = _actionLabel == "Remove";
        _actionButton.ThemeTypeVariation = removesAssignment
            ? "ButtonWarning"
            : "ButtonPrimary";
        _actionButton.Icon = ResourceLoader.Load<Texture2D>(
            removesAssignment ? IconPaths.Minus : IconPaths.Plus);
    }

    private void OnPressed() => EmitSignal(SignalName.ActionRequested, _itemId);
}
