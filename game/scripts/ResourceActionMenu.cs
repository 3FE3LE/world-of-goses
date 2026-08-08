#nullable enable
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Bare icon-only action button anchored to an in-world resource — no
/// panel, frame, or text, matching the reference minimalist interaction
/// style (a single button with the corresponding action icon, nothing
/// else). Right-click triggered; left-click instead selects the resource
/// and shows its details in <see cref="Ui.ContextInspector"/> — see
/// <see cref="Prototypes.MacroStreetLiveView"/>.
/// </summary>
public partial class ResourceActionMenu : IconButton
{
    [Signal]
    public delegate void GatherRequestedEventHandler(
        int forestId,
        int unitId,
        Vector2 targetPosition);

    private int _forestId;
    private int _unitId;
    private Vector2 _targetPosition;

    public override void _Ready()
    {
        base._Ready();
        OverlayLayers.Apply(this, OverlayLayers.ContextMenu);
        SetIconAndLabel(ResourceTree.AxeCursorPath, string.Empty);
        Pressed += OnGatherPressed;
        Hide();
    }

    public override void _ExitTree()
    {
        Pressed -= OnGatherPressed;
    }

    public void Open(
        int forestId,
        int unitId,
        ResourceType resourceType,
        Vector2 targetPosition,
        Vector2 localAnchor,
        bool canGather,
        string unavailableReason)
    {
        _forestId = forestId;
        _unitId = unitId;
        _targetPosition = targetPosition;
        SetIconAndLabel(
            resourceType == ResourceType.Wood ? ResourceTree.AxeCursorPath : IconPaths.Plus,
            string.Empty);
        Disabled = !canGather;
        TooltipText = canGather
            ? UiText.Format(
                "ui.gather.resource_action",
                UiText.Get(resourceType.ToString().ToLowerInvariant()))
            : unavailableReason;
        Show();
        ResetSize();
        Vector2 wanted = localAnchor + new Vector2(20, -36);
        Control parent = GetParent<Control>();
        Position = new Vector2(
            Mathf.Clamp(wanted.X, 8, Mathf.Max(8, parent.Size.X - Size.X - 8)),
            Mathf.Clamp(wanted.Y, 8, Mathf.Max(8, parent.Size.Y - Size.Y - 8)));
        if (canGather) GrabFocus();
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
