#nullable enable
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Global ESC menu. Its PackedScene owns layout and chrome; this script owns
/// focus, simulation pause/resume, close paths, and confirmed slot reset.
/// </summary>
public partial class PauseMenu : Control
{
    [Export] public NodePath ControllerPath { get; set; } = "../CityWorldController";
    [Export] public NodePath OpenButtonPath { get; set; } =
        "../GameUiShell/CityStatusPanel/SafeArea/StatusComposition/UtilityCluster/MenuButton";

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
    private IconButton _languageButton = null!;
    private IconButton _openButton = null!;
    private LocaleManager? _localeManager;
    private bool _scrimPressStarted;
    private bool _softResetRequested;

    public override void _Ready()
    {
        OverlayLayers.Apply(this, OverlayLayers.PauseAndNotifier);

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
        _languageButton = GetNode<IconButton>(
            "Center/Card/Margin/Shell/MainActions/LanguageButton");
        _confirmResetButton = GetNode<IconButton>(
            "Center/Card/Margin/Shell/ResetConfirmation/ConfirmResetButton");
        _cancelResetButton = GetNode<Button>(
            "Center/Card/Margin/Shell/ResetConfirmation/CancelResetButton");
        _openButton = GetNode<IconButton>(OpenButtonPath);
        _localeManager = GetNodeOrNull<LocaleManager>("/root/LocaleManager");

        _closeButton.SetIconAndLabel(IconPaths.Close, string.Empty);
        _resumeButton.SetIconAndLabel(IconPaths.Play, string.Empty);
        _languageButton.Pressed += OnLanguageButtonPressed;
        if (_localeManager is not null) _localeManager.LocaleChanged += OnLocaleChanged;

        _resumeButton.Pressed += Close;
        _closeButton.Pressed += Close;
        _resetButton.Pressed += ShowResetConfirmation;
        _softResetButton.Pressed += ShowSoftResetConfirmation;
        _confirmResetButton.Pressed += ConfirmReset;
        _cancelResetButton.Pressed += HideResetConfirmation;
        RefreshLocalizedText();
        _openButton.Pressed += Toggle;
        _scrim.GuiInput += OnScrimGuiInput;
        Hide();
    }

    private void OnLanguageButtonPressed()
    {
        LocaleManager? locale = GetNodeOrNull<LocaleManager>("/root/LocaleManager");
        if (locale is null) return;
        locale.ToggleLocale();
    }

    private string GetLanguageLabel()
    {
        string localeName = _localeManager?.CurrentLocale == "es"
            ? T("ui.common.language.spanish")
            : T("ui.common.language.english");
        return string.Format(T("ui.pause.language"), localeName);
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
        if (_languageButton is not null) _languageButton.Pressed -= OnLanguageButtonPressed;
        if (_openButton is not null) _openButton.Pressed -= Toggle;
        if (_scrim is not null) _scrim.GuiInput -= OnScrimGuiInput;
        if (_localeManager is not null) _localeManager.LocaleChanged -= OnLocaleChanged;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // ESC only closes the pause menu; it never opens it. The menu
        // has its own button (see <see cref="_openButton"/>) so opening
        // it via ESC would compete with the iterative back behaviour
        // added in CityPrototype, which closes the topmost modal first,
        // then returns to the macro view from a hero profile or
        // building detail. Opening the pause menu from a single ESC
        // press would skip those steps.
        if (!@event.IsActionPressed("ui_cancel")) return;
        if (!Visible) return;
        if (_resetConfirmation.Visible) HideResetConfirmation();
        else Close();
        GetViewport().SetInputAsHandled();
    }

    internal void ShowForVisualRegression(bool confirmReset)
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        Open();
        if (confirmReset) ShowResetConfirmation();
    }

    /// <summary>Opens the pause menu. Also called by the headless
    /// <c>language-selector</c> fixture to capture the language
    /// switcher without going through the open button.</summary>
    public void Open()
    {
        // Opening the menu no longer freezes the simulation. The city runs
        // while the game is closed; stopping it because a menu is on screen
        // contradicted that, and it made the menu a way to buy time. The
        // world keeps ticking behind the scrim.
        HideResetConfirmation();
        Show();
        _resumeButton.GrabFocus();
    }

    public void Toggle()
    {
        if (Visible) Close();
        else Open();
    }

    /// <summary>Closes the pause menu.</summary>
    public void Close()
    {
        if (!Visible) return;
        Hide();
    }

    private void ShowResetConfirmation()
    {
        _softResetRequested = false;
        ConfigureResetConfirmation(
            T("ui.pause.reset.title"),
            T("ui.pause.reset.warning"),
            T("ui.pause.reset.confirm"));
        ShowConfiguredResetConfirmation();
    }

    private void ShowSoftResetConfirmation()
    {
        _softResetRequested = true;
        ConfigureResetConfirmation(
            T("ui.pause.soft_reset.title"),
            T("ui.pause.soft_reset.warning"),
            T("ui.pause.soft_reset.confirm"));
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

    private void OnLocaleChanged(string locale)
    {
        _ = locale;
        RefreshLocalizedText();
    }

    private void RefreshLocalizedText()
    {
        GetNode<Label>("Center/Card/Margin/Shell/Heading/Title").Text = T("ui.pause.title");
        _resumeButton.SetIconAndLabel(IconPaths.Play, T("ui.pause.resume"));
        GetNode<IconButton>("Center/Card/Margin/Shell/MainActions/SettingsButton")
            .SetIconAndLabel(IconPaths.Cog, T("ui.pause.settings_pending"));
        _languageButton.SetIconAndLabel(IconPaths.Cog, GetLanguageLabel());
        _resetButton.SetIconAndLabel(IconPaths.Trash, T("ui.pause.reset.action"));
        _softResetButton.SetIconAndLabel(IconPaths.Reload, T("ui.pause.soft_reset.action"));
        _openButton.SetIconAndLabel(IconPaths.Menu, T("ui.pause.open"));
        _cancelResetButton.Text = T("ui.pause.cancel_reset");
        _languageButton.TooltipText = T("ui.common.language.tooltip");

        if (_resetConfirmation.Visible)
        {
            if (_softResetRequested) ShowSoftResetConfirmation();
            else ShowResetConfirmation();
        }
    }

    private string T(string key) => _localeManager?.Translate(key) ?? key;

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
