#nullable enable
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Simulation-speed control. One three-state speedometer glyph says how fast
/// the world is running; clicking cycles Normal → Fast → Fastest → Normal. The
/// world always runs; the player can only speed it up.
/// </summary>
/// <remarks>
/// <para>
/// Lives in the <see cref="CityStatusPanel"/> utility cluster beside Camera
/// and Menu, and is an <see cref="IconButton"/> for the same reason they are:
/// Godot's native <see cref="Button"/> renderer owns the icon, centres it in
/// the button rect, and makes it participate in minimum-size calculation.
/// </para>
/// <para>
/// It used to stack one, two or four copies of <c>IconPaths.Play</c> in an
/// <c>HBoxContainer</c> of 8 px <see cref="TextureRect"/> cells inside a
/// 36×24 button, while its neighbours were 40×40 (GitHub #16). Two things were
/// wrong and they compounded. The metric did not match the cluster, so the
/// control sat at a different height from the buttons it lines up with. And
/// <see cref="TextureRect.StretchModeEnum.Keep"/> draws a source at its
/// natural size regardless of the rect it is given — the icon catalogue is
/// 24 px, as <see cref="Tokens.IconInline"/> documents — so an 8 px cell never
/// produced an 8 px glyph. It produced a 24 px glyph in an 8 px box: four of
/// them overflowed a 36 px button, <c>ClipContents</c> cut the overflow, and
/// the visible group's optical centre had nothing to do with the rect the
/// layout believed it was centring.
/// </para>
/// <para>
/// The fix is not to re-rasterise the Pixelarticons set at 8 px, which
/// <see cref="Tokens"/> already rules out for destroying the pixel grid. It is
/// one glyph per state, each of which fits a 24 px cell natively.
/// </para>
/// </remarks>
public partial class SpeedButton : IconButton
{
    [Export] public NodePath ControllerPath { get; set; } = "/root/CityPrototype/CityWorldController";

    private CityWorldController? _controller;
    private CityWorldController.SpeedChoice _currentSpeed = CityWorldController.SpeedChoice.Normal;

    public override void _Ready()
    {
        // Icon-only, and exactly the cell Camera and Menu claim, so the three
        // share one interactive height rather than three opinions about it.
        ShowLabel = false;
        ButtonText = string.Empty;
        CustomMinimumSize = new Vector2(Tokens.ControlHeight, Tokens.ControlHeight);
        CompactIconButtonStyle.Apply(this);
        base._Ready();

        var controllerNode = GetNodeOrNull<CityWorldController>(ControllerPath);
        if (controllerNode is not null)
        {
            AttachController(controllerNode);
        }
        else
        {
            // No controller yet: still render a truthful default rather than
            // an empty cell that would collapse the cluster's spacing.
            UpdateDisplay();
        }
    }

    public override void _ExitTree()
    {
        if (_controller is not null)
        {
            _controller.SimulationSpeedChanged -= OnSpeedChanged;
        }
        base._ExitTree();
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

    /// <summary>
    /// The three glyphs are one family read against each other — the control
    /// cycles 1× → 2× → 4× and there is no slower-than-normal speed for the
    /// first one to be confused with.
    /// </summary>
    internal static string IconFor(CityWorldController.SpeedChoice speed) => speed switch
    {
        CityWorldController.SpeedChoice.Normal => IconPaths.SpeedSlow,
        CityWorldController.SpeedChoice.Fast => IconPaths.SpeedMedium,
        CityWorldController.SpeedChoice.Fastest => IconPaths.SpeedFast,
        _ => IconPaths.SpeedSlow,
    };

    private static string TooltipFor(CityWorldController.SpeedChoice speed) => speed switch
    {
        CityWorldController.SpeedChoice.Normal => UiText.Get("ui.speed.normal"),
        CityWorldController.SpeedChoice.Fast => UiText.Get("ui.speed.fast"),
        CityWorldController.SpeedChoice.Fastest => UiText.Get("ui.speed.fastest"),
        _ => UiText.Get("ui.speed.change"),
    };

    private void UpdateDisplay()
    {
        SetIconAndLabel(IconFor(_currentSpeed), string.Empty);
        TooltipText = TooltipFor(_currentSpeed);
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
