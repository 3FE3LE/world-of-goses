#nullable enable
using Godot;
using WorldofGoses.Ui;

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
    private const int PlayIconSize = 8;
    private const int ButtonWidth = 44;
    private const int ButtonHeight = 24;

    private CityWorldController? _controller;
    private HBoxContainer _container = null!;
    private readonly TextureRect[] _playIcons = new TextureRect[MaxPlayIcons];
    private CityWorldController.SpeedChoice _currentSpeed = CityWorldController.SpeedChoice.Normal;
    private CityWorldController.SpeedChoice _lastRunningSpeed = CityWorldController.SpeedChoice.Normal;

    public override void _Ready()
    {
        Text = string.Empty;
        Icon = null;

        // Tight, gluen icon stack — no state icon, no separation,
        // just the play icons directly next to each other.
        CustomMinimumSize = new Vector2(ButtonWidth, ButtonHeight);
        ClipContents = true;
        CompactIconButtonStyle.Apply(this);

        _container = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
            AnchorsPreset = (int)LayoutPreset.FullRect,
        };
        _container.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _container.AddThemeConstantOverride("separation", 0);
        AddChild(_container);

        var playTexture = ResourceLoader.Load<Texture2D>(IconPaths.Play);
        for (int i = 0; i < MaxPlayIcons; i++)
        {
            _playIcons[i] = new TextureRect
            {
                Texture = playTexture,
                StretchMode = TextureRect.StretchModeEnum.Keep,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
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
        _lastRunningSpeed = controller.LastRunningSpeed;
        controller.SimulationSpeedChanged += OnSpeedChanged;
        OnSpeedChanged((int)controller.CurrentSpeed);
    }

    private void OnSpeedChanged(int speedValue)
    {
        _currentSpeed = (CityWorldController.SpeedChoice)speedValue;
        if (_currentSpeed != CityWorldController.SpeedChoice.Paused)
        {
            _lastRunningSpeed = _currentSpeed;
        }
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        // Number of trailing play icons matches the speed multiplier so
        // the visual reads as "faster" through duplication rather than a
        // separate glyph. While paused, retain the chosen rate but disable
        // this button; PlayPauseButton owns resume.
        CityWorldController.SpeedChoice displayedSpeed = _currentSpeed == CityWorldController.SpeedChoice.Paused
            ? _lastRunningSpeed
            : _currentSpeed;
        int playCount = displayedSpeed switch
        {
            CityWorldController.SpeedChoice.Normal => 1,
            CityWorldController.SpeedChoice.Fast => 2,
            CityWorldController.SpeedChoice.Fastest => 4,
            _ => 0,
        };
        for (int i = 0; i < MaxPlayIcons; i++)
        {
            _playIcons[i].Visible = i < playCount;
        }
        Disabled = _currentSpeed == CityWorldController.SpeedChoice.Paused;

        string tooltip = _currentSpeed switch
        {
            CityWorldController.SpeedChoice.Paused =>
                UiText.Format("ui.speed.paused", (int)_lastRunningSpeed),
            CityWorldController.SpeedChoice.Normal => UiText.Get("ui.speed.normal"),
            CityWorldController.SpeedChoice.Fast => UiText.Get("ui.speed.fast"),
            CityWorldController.SpeedChoice.Fastest => UiText.Get("ui.speed.fastest"),
            _ => UiText.Get("ui.speed.change"),
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
