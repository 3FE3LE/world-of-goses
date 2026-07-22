#nullable enable
using Godot;

namespace WorldofGoses;

/// <summary>
/// Standalone play/pause toggle independent of the speed indicator.
/// When the simulation is paused the icon shows the pause glyph; when
/// it is running it shows the play glyph. Clicking the button swaps
/// between the two states (does not cycle through speed multipliers —
/// that is the responsibility of <see cref="SpeedButton"/>).
///
/// The button has no Text/Icon attributes of its own; the glyph is
/// rendered by a custom TextureRect so the click area can be sized
/// independently of the theme's button padding.
/// </summary>
public partial class PlayPauseButton : Button
{
    [Export] public NodePath ControllerPath { get; set; } = "/root/CityPrototype/CityWorldController";

    private const int IconSize = 14;

    private CityWorldController? _controller;
    private TextureRect _icon = null!;
    private bool _wasPaused;

    public override void _Ready()
    {
        Text = string.Empty;
        Icon = null;
        CustomMinimumSize = new Vector2(IconSize + 8, IconSize + 8);

        _icon = new TextureRect
        {
            StretchMode = TextureRect.StretchModeEnum.Keep,
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_icon);

        var controllerNode = GetNodeOrNull<CityWorldController>(ControllerPath);
        if (controllerNode is not null)
        {
            AttachController(controllerNode);
        }
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
        _controller = controller;
        controller.SimulationSpeedChanged += OnSpeedChanged;
        OnSpeedChanged((int)controller.CurrentSpeed);
    }

    private void OnSpeedChanged(int speedValue)
    {
        _wasPaused = speedValue == (int)CityWorldController.SpeedChoice.Paused;
        _icon.Texture = ResourceLoader.Load<Texture2D>(
            _wasPaused ? IconPaths.Pause : IconPaths.Play);
        TooltipText = _wasPaused
            ? "Simulation paused. Click to resume at 1×."
            : "Click to pause the simulation.";
    }

    public override void _Pressed()
    {
        if (_controller is null) return;
        _controller.SetSimulationSpeed(_wasPaused
            ? CityWorldController.SpeedChoice.Normal
            : CityWorldController.SpeedChoice.Paused);
    }
}
