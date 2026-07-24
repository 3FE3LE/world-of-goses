#nullable enable
using Godot;

namespace WorldofGoses;

/// <summary>
/// Global ESC menu. Its PackedScene owns layout and chrome; this script owns
/// focus, simulation pause/resume, close paths, and confirmed slot reset.
/// </summary>
public partial class PauseMenu : Control
{
    [Export] public NodePath ControllerPath { get; set; } = "../CityWorldController";
    [Export] public NodePath OpenButtonPath { get; set; } =
        "../GameUiShell/ScreenContent/MacroActions/Actions/GameMenuButton";

    private CityWorldController _controller = null!;
    private PanelContainer _card = null!;
    private ColorRect _scrim = null!;
    private VBoxContainer _mainActions = null!;
    private VBoxContainer _resetConfirmation = null!;
    private IconButton _resumeButton = null!;
    private IconButton _closeButton = null!;
    private IconButton _resetButton = null!;
    private IconButton _softResetButton = null!;
    private IconButton _confirmResetButton = null!;
    private Button _cancelResetButton = null!;
    private IconButton _openButton = null!;
    private CityWorldController.SpeedChoice _speedBeforeOpen;
    private bool _scrimPressStarted;
    private bool _softResetRequested;

    public override void _Ready()
    {
        _controller = GetNode<CityWorldController>(ControllerPath);
        _card = GetNode<PanelContainer>("Center/Card");
        _scrim = GetNode<ColorRect>("Scrim");
        _mainActions = GetNode<VBoxContainer>("Center/Card/Margin/Shell/MainActions");
        _resetConfirmation = GetNode<VBoxContainer>(
            "Center/Card/Margin/Shell/ResetConfirmation");
        _resumeButton = GetNode<IconButton>(
            "Center/Card/Margin/Shell/MainActions/ResumeButton");
        _closeButton = GetNode<IconButton>(
            "Center/Card/Margin/Shell/Heading/CloseButton");
        _resetButton = GetNode<IconButton>(
            "Center/Card/Margin/Shell/MainActions/ResetButton");
        _softResetButton = GetNode<IconButton>(
            "Center/Card/Margin/Shell/MainActions/SoftResetButton");
        _confirmResetButton = GetNode<IconButton>(
            "Center/Card/Margin/Shell/ResetConfirmation/ConfirmResetButton");
        _cancelResetButton = GetNode<Button>(
            "Center/Card/Margin/Shell/ResetConfirmation/CancelResetButton");
        _openButton = GetNode<IconButton>(OpenButtonPath);

        _closeButton.SetIconAndLabel(IconPaths.Close, string.Empty);
        _resumeButton.SetIconAndLabel(IconPaths.Play, "Resume");
        GetNode<IconButton>(
            "Center/Card/Margin/Shell/MainActions/SettingsButton")
            .SetIconAndLabel(IconPaths.Cog, "Settings — coming next");
        _resetButton.SetIconAndLabel(IconPaths.Trash, "Start over");
        _softResetButton.SetIconAndLabel(
            IconPaths.Reload,
            "Restart city — keep founder");
        _confirmResetButton.SetIconAndLabel(
            IconPaths.Reload,
            "Delete city and restart");

        _card.AddThemeStyleboxOverride(
            "panel",
            LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
        _resumeButton.Pressed += Close;
        _closeButton.Pressed += Close;
        _resetButton.Pressed += ShowResetConfirmation;
        _softResetButton.Pressed += ShowSoftResetConfirmation;
        _confirmResetButton.Pressed += ConfirmReset;
        _cancelResetButton.Pressed += HideResetConfirmation;
        _openButton.SetIconAndLabel(IconPaths.Menu, "Menu");
        _openButton.Pressed += Toggle;
        _scrim.GuiInput += OnScrimGuiInput;
        Hide();
    }

    public override void _ExitTree()
    {
        if (_resumeButton is not null) _resumeButton.Pressed -= Close;
        if (_closeButton is not null) _closeButton.Pressed -= Close;
        if (_resetButton is not null) _resetButton.Pressed -= ShowResetConfirmation;
        if (_softResetButton is not null)
        {
            _softResetButton.Pressed -= ShowSoftResetConfirmation;
        }
        if (_confirmResetButton is not null) _confirmResetButton.Pressed -= ConfirmReset;
        if (_cancelResetButton is not null) _cancelResetButton.Pressed -= HideResetConfirmation;
        if (_openButton is not null) _openButton.Pressed -= Toggle;
        if (_scrim is not null) _scrim.GuiInput -= OnScrimGuiInput;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("ui_cancel")) return;

        if (Visible)
        {
            if (_resetConfirmation.Visible) HideResetConfirmation();
            else Close();
        }
        else
        {
            Open();
        }
        GetViewport().SetInputAsHandled();
    }

    internal void ShowForVisualRegression(bool confirmReset)
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        Open();
        if (confirmReset) ShowResetConfirmation();
    }

    private void Open()
    {
        _speedBeforeOpen = _controller.CurrentSpeed;
        _controller.SetSimulationSpeed(CityWorldController.SpeedChoice.Paused);
        HideResetConfirmation();
        Show();
        _resumeButton.GrabFocus();
    }

    public void Toggle()
    {
        if (Visible) Close();
        else Open();
    }

    private void Close()
    {
        if (!Visible) return;
        Hide();
        if (_speedBeforeOpen != CityWorldController.SpeedChoice.Paused)
        {
            _controller.SetSimulationSpeed(_speedBeforeOpen);
        }
    }

    private void ShowResetConfirmation()
    {
        _softResetRequested = false;
        ConfigureResetConfirmation(
            "Start a new city?",
            "This permanently deletes the current local city and returns to hero onboarding.",
            "Delete city and restart");
        ShowConfiguredResetConfirmation();
    }

    private void ShowSoftResetConfirmation()
    {
        _softResetRequested = true;
        ConfigureResetConfirmation(
            "Restart this city?",
            "Buildings, resources, and progress will be cleared. Your founder and onboarding choices will be kept.",
            "Restart and keep founder");
        ShowConfiguredResetConfirmation();
    }

    private void ConfigureResetConfirmation(
        string title,
        string warning,
        string confirmLabel)
    {
        GetNode<Label>(
            "Center/Card/Margin/Shell/ResetConfirmation/Title").Text = title;
        GetNode<Label>(
            "Center/Card/Margin/Shell/ResetConfirmation/Warning").Text = warning;
        _confirmResetButton.SetIconAndLabel(IconPaths.Reload, confirmLabel);
    }

    private void ShowConfiguredResetConfirmation()
    {
        _mainActions.Hide();
        _resetConfirmation.Show();
        _confirmResetButton.GrabFocus();
    }

    private void HideResetConfirmation()
    {
        _resetConfirmation.Hide();
        _mainActions.Show();
        if (Visible) _resumeButton.GrabFocus();
    }

    private void ConfirmReset()
    {
        if (_softResetRequested)
        {
            _controller.ResetCityKeepingFounderAndRestart();
        }
        else
        {
            _controller.ResetPrimarySlotAndRestart();
        }
    }

    private void OnScrimGuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouse
            || mouse.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        if (_card.GetGlobalRect().HasPoint(mouse.GlobalPosition))
        {
            _scrimPressStarted = false;
            return;
        }
        if (mouse.Pressed)
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
