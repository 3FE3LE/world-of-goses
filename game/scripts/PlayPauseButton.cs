#nullable enable
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Standalone play/pause toggle independent of the speed indicator. The icon
/// shows the action clicking performs while the selected running multiplier is
/// preserved by <see cref="CityWorldController"/>.
/// </summary>
public partial class PlayPauseButton : Button
{
    [Export] public NodePath ControllerPath { get; set; } = "/root/CityPrototype/CityWorldController";

    private const int IconSize = 14;
    private const int ButtonSize = 24;

    private CityWorldController? _controller;
    private TextureRect _icon = null!;

    public override void _Ready()
    {
        Text = string.Empty;
        Icon = null;
        CustomMinimumSize = new Vector2(ButtonSize, ButtonSize);
        ClipContents = true;
        CompactIconButtonStyle.Apply(this);

        _icon = new TextureRect
        {
            StretchMode = TextureRect.StretchModeEnum.Keep,
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        center.AddChild(_icon);

        CityWorldController? controllerNode = GetNodeOrNull<CityWorldController>(ControllerPath);
        if (controllerNode is not null) AttachController(controllerNode);
    }

    public override void _ExitTree()
    {
        if (_controller is not null)
        {
            _controller.SimulationSpeedChanged -= OnSpeedChanged;
        }
    }

    public void AttachController(CityWorldController controller)
    {
        if (ReferenceEquals(_controller, controller)) return;
        if (_controller is not null)
        {
            _controller.SimulationSpeedChanged -= OnSpeedChanged;
        }
        _controller = controller;
        controller.SimulationSpeedChanged += OnSpeedChanged;
        OnSpeedChanged((int)controller.CurrentSpeed);
    }

    private void OnSpeedChanged(int speedValue)
    {
        bool isPaused = speedValue == (int)CityWorldController.SpeedChoice.Paused;
        _icon.Texture = ResourceLoader.Load<Texture2D>(isPaused ? IconPaths.Play : IconPaths.Pause);
        TooltipText = isPaused
            ? UiText.Format("ui.simulation.resume", (int)(_controller?.LastRunningSpeed
                ?? CityWorldController.SpeedChoice.Normal))
            : UiText.Get("ui.simulation.pause");
    }

    public override void _Pressed()
    {
        if (_controller is null) return;
        _controller.ToggleSimulationPause();
    }
}
