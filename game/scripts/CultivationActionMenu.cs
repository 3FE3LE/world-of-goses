#nullable enable
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>Single contextual action for the bounded EG-3 crop lifecycle.</summary>
public partial class CultivationActionMenu : IconButton
{
    [Signal]
    public delegate void CultivationRequestedEventHandler(int siteId);

    private int _siteId;

    public override void _Ready()
    {
        base._Ready();
        OverlayLayers.Apply(this, OverlayLayers.ContextMenu);
        Pressed += OnPressed;
        Hide();
    }

    public override void _ExitTree()
    {
        Pressed -= OnPressed;
    }

    public void Open(
        int siteId,
        CultivationPlotState state,
        Vector2 localAnchor,
        bool canAct,
        string tooltip)
    {
        _siteId = siteId;
        SetIconAndLabel(
            state == CultivationPlotState.Ready ? IconPaths.Plus : IconPaths.Leaf,
            string.Empty);
        Disabled = !canAct;
        TooltipText = tooltip;
        Show();
        ResetSize();
        Vector2 wanted = localAnchor + new Vector2(20, -36);
        Control parent = GetParent<Control>();
        Position = new Vector2(
            Mathf.Clamp(wanted.X, 8, Mathf.Max(8, parent.Size.X - Size.X - 8)),
            Mathf.Clamp(wanted.Y, 8, Mathf.Max(8, parent.Size.Y - Size.Y - 8)));
        if (canAct) GrabFocus();
    }

    private void OnPressed()
    {
        Hide();
        EmitSignal(SignalName.CultivationRequested, _siteId);
    }
}
