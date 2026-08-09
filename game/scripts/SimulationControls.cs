#nullable enable

using System;
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Compact bottom-right surface that owns the existing play/pause and speed
/// controls without owning simulation rules. Camera mode lives on the
/// top-bar utility cluster (see <see cref="CityStatusPanel.CameraButton"/>).
/// </summary>
[GlobalClass]
public partial class SimulationControls : PanelContainer
{
    [Export] public NodePath ControllerPath { get; set; } = "../../../CityWorldController";

    private PlayPauseButton? _playPauseButton;
    private SpeedButton? _speedButton;

    public PlayPauseButton PlayPauseButton
    {
        get { EnsureBuilt(); return _playPauseButton!; }
    }

    public SpeedButton SpeedButton
    {
        get { EnsureBuilt(); return _speedButton!; }
    }

    public override void _Ready()
    {
        OverlayLayers.Apply(this, OverlayLayers.Hud);
        ThemeTypeVariation = "HudDock";
        MouseFilter = MouseFilterEnum.Stop;
        EnsureBuilt();
    }

    private void EnsureBuilt()
    {
        if (_playPauseButton is not null) return;

        var actions = new HBoxContainer
        {
            Name = "Actions",
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = MouseFilterEnum.Stop,
        };
        actions.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        AddChild(actions);

        _playPauseButton = new PlayPauseButton
        {
            Name = "PlayPauseButton",
            ThemeTypeVariation = "HudButton",
            FocusMode = FocusModeEnum.All,
        };
        _speedButton = new SpeedButton
        {
            Name = "SpeedButton",
            ThemeTypeVariation = "HudButton",
            FocusMode = FocusModeEnum.All,
        };
        actions.AddChild(_playPauseButton);
        actions.AddChild(_speedButton);

        WireHorizontalFocus(new Control[] { _playPauseButton, _speedButton });
    }

    private static void WireHorizontalFocus(Control[] controls)
    {
        for (int i = 0; i < controls.Length; i++)
        {
            Control current = controls[i];
            Control previous = controls[(i + controls.Length - 1) % controls.Length];
            Control next = controls[(i + 1) % controls.Length];
            current.FocusNeighborLeft = current.GetPathTo(previous);
            current.FocusNeighborRight = current.GetPathTo(next);
            current.FocusPrevious = current.GetPathTo(previous);
            current.FocusNext = current.GetPathTo(next);
        }
    }
}
