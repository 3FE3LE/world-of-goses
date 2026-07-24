#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// First-time tutorial overlay for the macro city view. Shows a
/// sequence of three short tips the first time the player lands on
/// the macro view after hero creation. The overlay is self-contained
/// and remembers whether it has been dismissed so it never reshows
/// during the same session and during subsequent loads.
/// </summary>
public partial class TutorialOverlay : Control
{
    private const float PreferredCardWidth = 360f;
    private const float HorizontalSafeMargin = 48f;

    [Export] public NodePath ControllerPath { get; set; } = "../CityWorldController";

    private static readonly IReadOnlyList<TutorialStep> Steps = new[]
    {
        new TutorialStep(
            "This is the status bar.",
            "It tracks the time of day, wood, food, active workers, and the most recent project. Hover any chip for details.",
            IconPaths.Calendar),
        new TutorialStep(
            "Your hero walks the field.",
            "Right now the world is empty. Hover the hero to pause the walk, or click to open the profile.",
            IconPaths.User),
        new TutorialStep(
            "Build your first shelter.",
            "Open the Construction menu (top-right). You will need 1 wood — gather it from the Forest plots first.",
            IconPaths.House),
    };

    private CityWorldController _controller = null!;
    private PanelContainer _card = null!;
    private ScrollContainer _bodyScroll = null!;
    private Label _titleLabel = null!;
    private Label _bodyLabel = null!;
    private IconButton _nextButton = null!;
    private int _stepIndex;
    private bool _dismissed;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        MouseFilter = Control.MouseFilterEnum.Stop;
        Visible = false;

        _controller = GetNode<CityWorldController>(ControllerPath);
        _controller.HeroCreated += OnHeroCreated;

        AddThemeStyleboxOverride("panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
        BuildShell();
        GetViewport().SizeChanged += ApplyResponsiveCardWidth;
        ApplyResponsiveCardWidth();
    }

    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= ApplyResponsiveCardWidth;
        if (_controller is not null) _controller.HeroCreated -= OnHeroCreated;
    }

    private void BuildShell()
    {
        var scrim = new ColorRect
        {
            Color = new Color(0.04f, 0.05f, 0.08f, 0.55f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        scrim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(scrim);

        var align = new CenterContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        align.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(align);

        _card = new PanelContainer
        {
            CustomMinimumSize = new Vector2(360, 0),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _card.AddThemeStyleboxOverride(
            "panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
        align.AddChild(_card);

        var padding = new MarginContainer();
        padding.AddThemeConstantOverride("margin_left", 24);
        padding.AddThemeConstantOverride("margin_right", 24);
        padding.AddThemeConstantOverride("margin_top", 20);
        padding.AddThemeConstantOverride("margin_bottom", 20);
        _card.AddChild(padding);

        var shell = new VBoxContainer();
        shell.AddThemeConstantOverride("separation", 12);
        padding.AddChild(shell);

        _titleLabel = new Label
        {
            ThemeTypeVariation = "ScreenTitle",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        shell.AddChild(_titleLabel);

        // Body lives in a ScrollContainer so translated or stretched copy
        // does not push the footer out of the viewport. Title and footer
        // stay fixed.
        _bodyScroll = new ScrollContainer
        {
            Name = "BodyScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            CustomMinimumSize = new Vector2(0, 96),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _bodyScroll.AddThemeStyleboxOverride(
            "panel",
            new StyleBoxFlat { BgColor = new Color(0.09f, 0.13f, 0.16f, 0.92f) });
        shell.AddChild(_bodyScroll);

        _bodyLabel = new Label
        {
            ThemeTypeVariation = "BodyText",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        _bodyScroll.AddChild(_bodyLabel);

        var footer = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.End,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        footer.AddThemeConstantOverride("separation", 8);
        shell.AddChild(footer);

        var skip = new Button
        {
            Text = "Skip",
            ThemeTypeVariation = "ButtonText",
            FocusMode = Control.FocusModeEnum.All,
        };
        skip.Pressed += () => Dismiss();
        footer.AddChild(skip);

        _nextButton = StandardButtons.IconAction(
            IconPaths.ArrowLeft, "Next", variation: "ButtonText");
        _nextButton.Text = "Next";
        // Swap the arrow direction so the label reads naturally.
        _nextButton.SetIconAndLabel(IconPaths.Check, "Next");
        _nextButton.Pressed += OnNextPressed;
        footer.AddChild(_nextButton);
    }

    private void OnHeroCreated(int _)
    {
        if (_dismissed) return;
        ShowFromFirstStep();
    }

    internal void ShowForVisualRegression(int stepIndex = 0)
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        _stepIndex = System.Math.Clamp(stepIndex, 0, Steps.Count - 1);
        ApplyStep();
        Notifier.SetOverlaySuppressed(true);
        Visible = true;
        _nextButton.GrabFocus();
    }

    private void ShowFromFirstStep()
    {
        _stepIndex = 0;
        ApplyStep();
        Notifier.SetOverlaySuppressed(true);
        Visible = true;
        _nextButton.GrabFocus();
    }

    private void OnNextPressed()
    {
        _stepIndex++;
        if (_stepIndex >= Steps.Count)
        {
            Dismiss();
            return;
        }
        ApplyStep();
    }

    private void ApplyStep()
    {
        var step = Steps[_stepIndex];
        _titleLabel.Text = $"Tip {_stepIndex + 1} of {Steps.Count} — {step.Title}";
        _bodyLabel.Text = step.Body;
        bool isLast = _stepIndex == Steps.Count - 1;
        _nextButton.SetIconAndLabel(
            IconPaths.Check,
            isLast ? "Got it" : "Next");
    }

    private void Dismiss()
    {
        _dismissed = true;
        Visible = false;
        Notifier.SetOverlaySuppressed(false);
    }

    private void ApplyResponsiveCardWidth()
    {
        if (_card is null) return;
        float availableWidth = Mathf.Max(0f, GetViewportRect().Size.X - HorizontalSafeMargin);
        _card.CustomMinimumSize = new Vector2(Mathf.Min(PreferredCardWidth, availableWidth), 0f);
    }

    private readonly record struct TutorialStep(string Title, string Body, string IconPath);
}
