#nullable enable
using Godot;

namespace WorldofGoses;

/// <summary>
/// Speed indicator button. Renders one, two or four small play icons
/// whose count tracks the current speed multiplier (1× = 1 play, 2× =
/// 2 plays, 4× = 4 plays). Clicking the button cycles through the
/// three running speeds (Normal → Fast → Fastest → Normal); it does
/// not touch the paused state — that is the responsibility of
/// <see cref="PlayPauseButton"/>.
///
/// Pairs with <see cref="PlayPauseButton"/> in the status bar: each
/// owns one concern (play/pause vs. speed) so the player can target
/// either independently.
/// </summary>
public partial class SpeedButton : Button
{
    [Export] public NodePath ControllerPath { get; set; } = "/root/CityPrototype/CityWorldController";

    private const int MaxPlayIcons = 4;
    private const int PlayIconSize = 10;

    private CityWorldController? _controller;
    private HBoxContainer _container = null!;
    private readonly TextureRect[] _playIcons = new TextureRect[MaxPlayIcons];
    private CityWorldController.SpeedChoice _currentSpeed = CityWorldController.SpeedChoice.Normal;

    public override void _Ready()
    {
        Text = string.Empty;
        Icon = null;

        // Tight, gluen icon stack — no state icon, no separation,
        // just the play icons directly next to each other.
        CustomMinimumSize = new Vector2(
            MaxPlayIcons * PlayIconSize + 4,
            PlayIconSize + 4);

        _container = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        _container.AddThemeConstantOverride("separation", 0);
        AddChild(_container);

        var playTexture = ResourceLoader.Load<Texture2D>(IconPaths.Play);
        for (int i = 0; i < MaxPlayIcons; i++)
        {
            _playIcons[i] = new TextureRect
            {
                Texture = playTexture,
                StretchMode = TextureRect.StretchModeEnum.Keep,
                CustomMinimumSize = new Vector2(PlayIconSize, PlayIconSize),
                Visible = false,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _container.AddChild(_playIcons[i]);
        }

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
        _currentSpeed = (CityWorldController.SpeedChoice)speedValue;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        // Number of trailing play icons matches the speed multiplier so
        // the visual reads as "faster" through duplication rather than a
        // separate glyph. The paused state is represented by an empty
        // indicator — the play/pause glyph lives in the other button.
        int playCount = _currentSpeed switch
        {
            CityWorldController.SpeedChoice.Paused => 0,
            CityWorldController.SpeedChoice.Normal => 1,
            CityWorldController.SpeedChoice.Fast => 2,
            CityWorldController.SpeedChoice.Fastest => 4,
            _ => 0,
        };
        for (int i = 0; i < MaxPlayIcons; i++)
        {
            _playIcons[i].Visible = i < playCount;
        }

        string tooltip = _currentSpeed switch
        {
            CityWorldController.SpeedChoice.Paused => "Paused. Click to resume at 1×.",
            CityWorldController.SpeedChoice.Normal => "Normal speed (1×). Click to switch to 2×.",
            CityWorldController.SpeedChoice.Fast => "Fast speed (2×). Click to switch to 4×.",
            CityWorldController.SpeedChoice.Fastest => "Fastest speed (4×). Click to switch back to 1×.",
            _ => "Click to change speed.",
        };
        TooltipText = tooltip;
    }

    public override void _Pressed()
    {
        if (_controller is null) return;
        // Cycle within the three running speeds. Paused state is owned
        // by PlayPauseButton, so this button never enters/exits Pause.
        var next = _currentSpeed switch
        {
            CityWorldController.SpeedChoice.Normal => CityWorldController.SpeedChoice.Fast,
            CityWorldController.SpeedChoice.Fast => CityWorldController.SpeedChoice.Fastest,
            CityWorldController.SpeedChoice.Fastest => CityWorldController.SpeedChoice.Normal,
            _ => CityWorldController.SpeedChoice.Normal,
        };
        _controller.SetSimulationSpeed(next);
    }
}
