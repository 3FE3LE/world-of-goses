#nullable enable
using Godot;

namespace WorldofGoses;

/// <summary>Small contextual menu anchored to an in-world resource.</summary>
public partial class ResourceActionMenu : PanelContainer
{
    [Signal]
    public delegate void GatherRequestedEventHandler(
        int forestId,
        int unitId,
        Vector2 targetPosition);

    private Label _reserveLabel = null!;
    private Label _regenerationLabel = null!;
    private Label _availabilityLabel = null!;
    private IconButton _gatherButton = null!;
    private Button _closeButton = null!;
    private int _forestId;
    private int _unitId;
    private Vector2 _targetPosition;

    public override void _Ready()
    {
        _reserveLabel = GetNode<Label>("Margin/Content/ReserveLabel");
        _regenerationLabel = GetNode<Label>("Margin/Content/RegenerationLabel");
        _availabilityLabel = GetNode<Label>("Margin/Content/AvailabilityLabel");
        _gatherButton = GetNode<IconButton>("Margin/Content/GatherButton");
        _closeButton = GetNode<Button>("Margin/Content/CloseButton");
        _gatherButton.SetIconAndLabel(ResourceTree.AxeCursorPath, "Gather");
        _gatherButton.Pressed += OnGatherPressed;
        _closeButton.Pressed += Hide;
        Hide();
    }

    public override void _ExitTree()
    {
        _gatherButton.Pressed -= OnGatherPressed;
        _closeButton.Pressed -= Hide;
    }

    public void Open(
        int forestId,
        int unitId,
        int reserve,
        int ticksUntilRegeneration,
        Vector2 targetPosition,
        Vector2 localAnchor,
        bool canGather,
        string unavailableReason)
    {
        _forestId = forestId;
        _unitId = unitId;
        _targetPosition = targetPosition;
        _reserveLabel.Text = $"{reserve} wood remains";
        _regenerationLabel.Text =
            $"Patch growth at next dawn · {ticksUntilRegeneration} ticks";
        _availabilityLabel.Text = unavailableReason;
        _availabilityLabel.Visible = !canGather;
        _gatherButton.Disabled = !canGather;
        _gatherButton.TooltipText = canGather ? string.Empty : unavailableReason;
        Show();
        ResetSize();
        Vector2 wanted = localAnchor + new Vector2(20, -36);
        Control parent = GetParent<Control>();
        Position = new Vector2(
            Mathf.Clamp(wanted.X, 8, Mathf.Max(8, parent.Size.X - Size.X - 8)),
            Mathf.Clamp(wanted.Y, 8, Mathf.Max(8, parent.Size.Y - Size.Y - 8)));
        if (canGather) _gatherButton.GrabFocus();
        else _closeButton.GrabFocus();
    }

    private void OnGatherPressed()
    {
        Hide();
        EmitSignal(
            SignalName.GatherRequested,
            _forestId,
            _unitId,
            _targetPosition);
    }
}
