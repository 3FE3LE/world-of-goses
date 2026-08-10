#nullable enable
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Speed indicator button. Renders one, two or four small play icons
/// whose count tracks the current speed multiplier (1× = 1 play, 2× =
/// 2 plays, 4× = 4 plays). Clicking the button cycles through the
/// three running speeds (Normal → Fast → Fastest → Normal). The world
/// always runs; the player can only speed it up.
/// </summary>
/// <remarks>
/// Lives in the CityStatusPanel utility cluster (next to Camera and
/// Menu). Replaces the previous bottom-right <c>SimulationControls</c>
/// surface so the speed control shares the same dock chrome as the
/// other status-bar actions and no longer floats alone.
/// </remarks>
public partial class SpeedButton : Button
{
    [Export] public NodePath ControllerPath { get; set; } = "/root/CityPrototype/CityWorldController";

    private const int MaxPlayIcons = 4;
    private const int PlayIconSize = 8;
    private const int ButtonWidth = 36;
    private const int ButtonHeight = 24;
    private const int IconPadding = 4;

    private CityWorldController? _controller;
    private HBoxContainer _container = null!;
    private readonly TextureRect[] _playIcons = new TextureRect[MaxPlayIcons];
    private CityWorldController.SpeedChoice _currentSpeed = CityWorldController.SpeedChoice.Normal;

    public override void _Ready()
    {
        Text = string.Empty;
        Icon = null;

        // The button's CustomMinimumSize is exactly the size of its
        // content plus symmetric padding. No centred HBoxContainer
        // pushing icons to one side; the HBox fills the cell and
        // centers its children with the theme-default separation.
        CustomMinimumSize = new Vector2(ButtonWidth, ButtonHeight);
        ClipContents = true;
        CompactIconButtonStyle.Apply(this);

        _container = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        _container.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _container.AddThemeConstantOverride("separation", 0);
        // Equal margins on both sides so the play-icon stack sits on
        // the button's geometric centre. The previous version used a
        // centred nested FullRect container that left asymmetric
        // visual padding.
        _container.AddThemeConstantOverride("margin_left", IconPadding);
        _container.AddThemeConstantOverride("margin_right", IconPadding);
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
        _currentSpeed = (CityWorldController.SpeedChoice)speedValue;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        // Number of trailing play icons matches the speed multiplier so
        // the visual reads as "faster" through duplication rather than a
        // separate glyph.
        int playCount = _currentSpeed switch
        {
            CityWorldController.SpeedChoice.Normal => 1,
            CityWorldController.SpeedChoice.Fast => 2,
            CityWorldController.SpeedChoice.Fastest => 4,
            _ => 1,
        };
        for (int i = 0; i < MaxPlayIcons; i++)
        {
            _playIcons[i].Visible = i < playCount;
        }

        string tooltip = _currentSpeed switch
        {
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
        // Cycle within the three running speeds. Pause is no longer a
        // possible state; the world always runs.
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