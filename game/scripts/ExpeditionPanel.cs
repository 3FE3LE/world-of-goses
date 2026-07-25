#nullable enable
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Minimal abstract reconnaissance panel: dispatches a deterministic
/// expedition using the founding hero and consumes one Wood supply.
/// The panel reuses <see cref="ModalHost"/> for scrim/close semantics
/// and is the canonical UI surface for the v13 expedition slice.
/// </summary>
[GlobalClass]
public partial class ExpeditionPanel : Control
{
    private static readonly Vector2 PreferredSize = new(520, 320);
    private const float ViewportInset = 32f;

    [Export] public NodePath ControllerPath { get; set; } = "../../../../CityWorldController";
    [Export] public NodePath ModalHostPath { get; set; } = "../ModalHost";
    [Export] public NodePath StatusLabelPath { get; set; } = "Surface/Margin/Layout/StatusLabel";
    [Export] public NodePath DispatchButtonPath { get; set; } = "Surface/Margin/Layout/DispatchButton";
    [Export] public NodePath CancelButtonPath { get; set; } = "Surface/Margin/Layout/CancelButton";
    [Export] public NodePath CloseButtonPath { get; set; } = "Surface/Margin/Layout/CloseButton";

    private CityWorldController _controller = null!;
    private ModalHost _modalHost = null!;
    private Label _statusLabel = null!;
    private Button _dispatchButton = null!;
    private Button _cancelButton = null!;
    private Button _closeButton = null!;

    public override void _Ready()
    {
        _controller = GetNode<CityWorldController>(ControllerPath);
        _modalHost = GetNode<ModalHost>(ModalHostPath);
        _statusLabel = GetNode<Label>(StatusLabelPath);
        _dispatchButton = GetNode<Button>(DispatchButtonPath);
        _cancelButton = GetNode<Button>(CancelButtonPath);
        _closeButton = GetNode<Button>(CloseButtonPath);
        if (_modalHost is null)
        {
            _modalHost = GetNode<ModalHost>(ModalHostPath);
        }

        _dispatchButton.Pressed += OnDispatchPressed;
        _cancelButton.Pressed += OnCancelPressed;
        _closeButton.Pressed += OnClosePressed;
        _controller.ExpeditionStateChanged += OnExpeditionStateChanged;
        _controller.BuildingStateChanged += _ => Refresh();
        _controller.WorldTickAdvanced += _ => Refresh();
        GetViewport().SizeChanged += ApplyResponsiveBounds;

        Hide();
        CallDeferred(MethodName.ApplyResponsiveBounds);
    }

    public override void _ExitTree()
    {
        if (_controller is not null)
        {
            _controller.ExpeditionStateChanged -= OnExpeditionStateChanged;
        }
        GetViewport().SizeChanged -= ApplyResponsiveBounds;
    }

    public void Open()
    {
        Show();
        _modalHost.Open(this);
        Refresh();
        FocusCurrentAction();
    }

    public void Close()
    {
        _modalHost.Close();
    }

    private void ApplyResponsiveBounds()
    {
        Vector2 parentSize = GetParentOrNull<Control>()?.Size ?? GetViewportRect().Size;
        Vector2 size = new(
            Mathf.Max(320f, Mathf.Min(PreferredSize.X, parentSize.X - ViewportInset * 2f)),
            Mathf.Max(240f, Mathf.Min(PreferredSize.Y, parentSize.Y - ViewportInset * 2f)));
        CustomMinimumSize = Vector2.Zero;
        SetAnchorsPreset(LayoutPreset.Center);
        OffsetLeft = -Mathf.Round(size.X * 0.5f);
        OffsetTop = -Mathf.Round(size.Y * 0.5f);
        OffsetRight = Mathf.Round(size.X * 0.5f);
        OffsetBottom = Mathf.Round(size.Y * 0.5f);
    }

    private void OnDispatchPressed()
    {
        if (_controller.World.Hero is null) return;
        var request = ExpeditionRequest.Reconnaissance(_controller.World.Hero.Id);
        ExpeditionStartResult result = _controller.StartExpedition(request);
        if (!result.IsSuccess)
        {
            Notifier.ShowError($"Could not dispatch: {result.Outcome}");
        }
        Refresh();
    }

    private void OnCancelPressed()
    {
        ExpeditionId? active = null;
        foreach (Expedition expedition in _controller.World.Expeditions.Values)
        {
            if (expedition.Status == ExpeditionStatus.Active)
            {
                active = expedition.Id;
                break;
            }
        }
        if (active.HasValue)
        {
            _controller.CancelExpedition(active.Value);
        }
        Refresh();
    }

    private void OnClosePressed() => Close();

    private void OnExpeditionStateChanged(int _) => Refresh();

    private void Refresh()
    {
        Expedition? active = null;
        foreach (Expedition expedition in _controller.World.Expeditions.Values)
        {
            if (expedition.Status == ExpeditionStatus.Active)
            {
                active = expedition;
                break;
            }
        }
        bool heroAvailable = _controller.World.Hero is not null
            && !_controller.World.Hero.CurrentAssignment.HasValue;
        bool canDispatch = active is null && heroAvailable;
        _dispatchButton.Disabled = !canDispatch;
        _cancelButton.Visible = active is not null;
        _statusLabel.Text = active is null
            ? "Dispatch a reconnaissance: consumes 1 Wood, returns with 1 Stone after 4 in-game days."
            : $"{active.DisplayName} departed {SimulationTimeText.Format(active.StartTick)} "
                + $"and returns {SimulationTimeText.Format(active.EndTick)}.";
    }

    private void FocusCurrentAction()
    {
        Button target = _cancelButton.Visible && !_cancelButton.Disabled
            ? _cancelButton
            : !_dispatchButton.Disabled
                ? _dispatchButton
                : _closeButton;
        target.GrabFocus();
    }

    private static StyleBoxFlat CreateReadingSurface() =>
        new()
        {
            BgColor = new Color(0.09f, 0.13f, 0.16f, 0.98f),
            BorderColor = new Color(0.78f, 0.64f, 0.32f, 1f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        };
}
