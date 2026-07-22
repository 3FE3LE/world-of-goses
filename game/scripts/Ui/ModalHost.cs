#nullable enable
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Reusable modal host. Owns a single full-screen scrim and the close
/// routes the stabilisation slice required (X button inside the content,
/// the <c>ui_cancel</c> action = ESC by default, click on the scrim).
///
/// Important: <see cref="Open(Control)"/> does NOT re-parent the
/// supplied content into the host's tree. Callers keep full ownership
/// of their content's tree location — the macro view, for instance,
/// keeps the <see cref="WorldofGoses.ConstructionPanel"/> as a child of
/// its <c>Center</c> container, where the rest of the layout expects
/// it. The host only flips the content's <see cref="CanvasItem.Visible"/>
/// so the scrim (sibling, lower z) and the panel (z_index 21 inside the
/// macro view) layer correctly.
///
/// The macro view replaces the ad-hoc <c>ConstructionModalScrim</c> +
/// <c>_constructionMenuOpen</c> toggle with this single host. Future
/// modals (alerts, expedition summaries) follow the same pattern:
/// instantiate <see cref="ModalHost"/>, call <see cref="Open"/> on it
/// with any <see cref="Control"/> that already lives in the scene.
/// </summary>
[GlobalClass]
public partial class ModalHost : Control
{
    /// <summary>Emitted when <see cref="Open"/> finishes binding content.</summary>
    [Signal] public delegate void OpenedEventHandler();

    /// <summary>Emitted when the player dismisses the modal via any route.</summary>
    [Signal] public delegate void ClosedEventHandler();

    /// <summary>
    /// Defaults to the same dim warm tone as the legacy
    /// <c>ConstructionModalScrim</c>: <c>Color(0.055, 0.047, 0.039, 0.72)</c>.
    /// </summary>
    public Color ScrimColor { get; set; } = new(0.055f, 0.047f, 0.039f, 0.72f);

    private ColorRect _scrim = null!;
    private Control? _content;
    private bool _scrimPressStarted;

    /// <summary>True while a content control is bound. Used by sibling layouts.</summary>
    public bool IsOpen => _content is not null && Visible;

    /// <summary>
    /// Currently hosted content, or <c>null</c> when closed. Useful
    /// for siblings that need to dim or fade background widgets.
    /// </summary>
    public Control? Content => _content;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        _scrim = new ColorRect
        {
            Name = "Scrim",
            Color = ScrimColor,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _scrim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_scrim);

        _scrim.GuiInput += OnScrimGuiInput;
    }

    /// <summary>
    /// Attaches <paramref name="content"/> as the modal body. The
    /// content's parent is NOT changed — the host only flips the
    /// content's <see cref="CanvasItem.Visible"/> and the host's own
    /// visibility. Calling <see cref="Open"/> with the same content
    /// while the modal is already open is a no-op.
    /// </summary>
    public void Open(Control content)
    {
        if (_content == content && IsOpen) return;
        _content = content;
        _content.MouseFilter = MouseFilterEnum.Stop;
        _scrimPressStarted = false;
        _content.Visible = true;
        Visible = true;
        EmitSignal(SignalName.Opened);
    }

    /// <summary>
    /// Dismisses the modal. Idempotent — calling <see cref="Close"/>
    /// when nothing is open is a safe no-op. Does not free the content.
    /// </summary>
    public void Close()
    {
        if (!IsOpen && _content is null && !Visible) return;
        if (_content is not null)
        {
            _content.Visible = false;
            _content = null;
        }
        Visible = false;
        _scrimPressStarted = false;
        EmitSignal(SignalName.Closed);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsOpen) return;
        if (@event.IsActionPressed("ui_cancel"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Clicks on the scrim close the modal. The scrim covers the
    /// whole viewport, so this is equivalent to "click outside the
    /// content". A click landing on the content passes through
    /// <see cref="Control.MouseFilterEnum.Stop"/> on the content
    /// itself and never reaches the scrim's <see cref="Control._GuiInput"/>.
    /// </summary>
    private void OnScrimGuiInput(InputEvent @event)
    {
        if (!IsOpen || @event is not InputEventMouseButton mb || mb.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        bool overContent = _content is not null && _content.GetGlobalRect().HasPoint(mb.Position);
        if (overContent)
        {
            _scrimPressStarted = false;
            return;
        }

        if (mb.Pressed)
        {
            _scrimPressStarted = true;
            AcceptEvent();
            return;
        }

        if (!_scrimPressStarted) return;
        _scrimPressStarted = false;
        Close();
        AcceptEvent();
    }
}
