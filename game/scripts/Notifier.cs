#nullable enable
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Autoload that surfaces short feedback toasts at the bottom of the
/// screen. Replaces the silent logs that previously dropped
/// <c>AssignmentOutcome</c> / authorization failures on the floor.
/// Auto-hides after a few seconds; subsequent calls cancel the
/// previous timer and replace the message.
/// </summary>
public partial class Notifier : Node
{
    public const string AutoloadName = "Notifier";

    private const float DisplaySeconds = 3f;
    private const string NotifierPath = "/root/Notifier";

    private static readonly Color InfoColor = new(0.92f, 0.94f, 1f);
    private static readonly Color ErrorColor = new(0.95f, 0.45f, 0.45f);

    private PanelContainer _panel = null!;
    private Label _label = null!;
    private Timer _hideTimer = null!;
    private bool _overlaySuppressed;

    public override void _Ready()
    {
        var layer = new CanvasLayer { Layer = 100 };
        AddChild(layer);

        var anchor = new SafeAreaMarginContainer
        {
            MinimumInset = 32,
            MinimumTopInset = 0,
        };
        anchor.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        layer.AddChild(anchor);

        var align = new CenterContainer();
        anchor.AddChild(align);

        _panel = new PanelContainer
        {
            Visible = false,
            CustomMinimumSize = new Vector2(360, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _panel.AddThemeStyleboxOverride(
            "panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
        align.AddChild(_panel);

        var padding = new MarginContainer();
        padding.AddThemeConstantOverride("margin_left", 16);
        padding.AddThemeConstantOverride("margin_right", 16);
        padding.AddThemeConstantOverride("margin_top", 10);
        padding.AddThemeConstantOverride("margin_bottom", 10);
        _panel.AddChild(padding);

        _label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _label.ThemeTypeVariation = "BodyText";
        padding.AddChild(_label);

        _hideTimer = new Timer
        {
            OneShot = true,
            WaitTime = DisplaySeconds,
        };
        _hideTimer.Timeout += OnHideTimerTimeout;
        AddChild(_hideTimer);
    }

    /// <summary>
    /// Shows a transient informational message. Safe to call from any
    /// node; missing the autoload is a no-op so the simulation never
    /// crashes because the UI is offline.
    /// </summary>
    public static void Show(string message)
    {
        if (Resolve() is { } notifier) notifier.ShowInternal(message, InfoColor);
    }

    /// <summary>
    /// Shows a transient error message. Uses the warning colour so the
    /// player can distinguish it from informational feedback at a
    /// glance.
    /// </summary>
    public static void ShowError(string message)
    {
        if (Resolve() is { } notifier) notifier.ShowInternal(message, ErrorColor);
    }

    public static void SetOverlaySuppressed(bool suppressed)
    {
        if (Resolve() is not { } notifier) return;
        notifier._overlaySuppressed = suppressed;
        if (suppressed)
        {
            notifier._hideTimer.Stop();
            notifier._panel.Visible = false;
        }
    }

    private static Notifier? Resolve()
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        return tree?.Root.GetNodeOrNull<Notifier>(NotifierPath);
    }

    private void ShowInternal(string message, Color color)
    {
        if (_overlaySuppressed) return;
        _label.Text = message;
        _label.AddThemeColorOverride("font_color", color);
        _panel.Visible = true;
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    private void OnHideTimerTimeout() => _panel.Visible = false;
}
