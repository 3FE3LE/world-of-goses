#nullable enable
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>Central, read-only first surface for city-wide policies.</summary>
[GlobalClass]
public partial class PoliciesPanel : Control
{
    private static readonly Vector2 PreferredSize = new(560f, 440f);

    [Export] public NodePath ControllerPath { get; set; } = "../../../CityWorldController";
    [Export] public NodePath ModalHostPath { get; set; } = "../ModalHost";

    private CityWorldController _controller = null!;
    private ModalHost _modalHost = null!;
    private Label _scheduleValue = null!;
    private Label _dayStateValue = null!;
    private Label _productionValue = null!;
    private Label _offDutyValue = null!;
    private Label _constructionValue = null!;

    public override void _Ready()
    {
        OverlayLayers.Apply(this, OverlayLayers.Modal);
        _controller = GetNode<CityWorldController>(ControllerPath);
        _modalHost = GetNode<ModalHost>(ModalHostPath);
        Build();
        _controller.WorldTickAdvanced += OnWorldTickAdvanced;
        GetViewport().SizeChanged += ApplyResponsiveBounds;
        Hide();
        CallDeferred(MethodName.ApplyResponsiveBounds);
    }

    public override void _ExitTree()
    {
        if (_controller is not null) _controller.WorldTickAdvanced -= OnWorldTickAdvanced;
        GetViewport().SizeChanged -= ApplyResponsiveBounds;
    }

    public void Open()
    {
        Refresh();
        Show();
        _modalHost.Open(this);
    }

    private void Build()
    {
        var surface = new PanelContainer { ThemeTypeVariation = "HudSurface" };
        surface.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(surface);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", Tokens.SpacingSection);
        margin.AddThemeConstantOverride("margin_top", Tokens.SpacingWide);
        margin.AddThemeConstantOverride("margin_right", Tokens.SpacingSection);
        margin.AddThemeConstantOverride("margin_bottom", Tokens.SpacingSection);
        surface.AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", Tokens.SpacingComfortable);
        margin.AddChild(layout);

        var header = new PanelHeader { Title = UiText.Get("ui.policies.title") };
        header.CloseRequested += () => _modalHost.Close();
        layout.AddChild(header);

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        layout.AddChild(scroll);
        var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", Tokens.SpacingComfortable);
        scroll.AddChild(body);

        _scheduleValue = AddPolicy(body, "ui.policies.workday");
        _dayStateValue = AddPolicy(body, "ui.policies.current_state");
        _productionValue = AddPolicy(body, "ui.policies.production");
        _offDutyValue = AddPolicy(body, "ui.policies.off_duty");
        _constructionValue = AddPolicy(body, "ui.policies.construction");

        var future = new Label
        {
            Text = UiText.Get("ui.policies.future"),
            ThemeTypeVariation = "HudCaption",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        body.AddChild(future);
    }

    private static Label AddPolicy(VBoxContainer layout, string titleKey)
    {
        var title = new Label
        {
            Text = UiText.Get(titleKey),
            ThemeTypeVariation = "HudLabel",
        };
        layout.AddChild(title);
        var value = new Label
        {
            ThemeTypeVariation = "HudBody",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        layout.AddChild(value);
        return value;
    }

    private void Refresh()
    {
        CityPolicySnapshot snapshot = _controller.GetCityPolicySnapshot();
        _scheduleValue.Text = UiText.Format(
            "ui.policies.schedule_value",
            FormatTimeOfDay(snapshot.WorkdayStartTick),
            FormatTimeOfDay(snapshot.WorkdayEndTick));
        _dayStateValue.Text = UiText.Get(snapshot.IsWorkday
            ? "ui.policies.workday_active"
            : "ui.policies.workday_inactive");
        _productionValue.Text = UiText.Format(
            "ui.policies.production_value",
            SimulationTimeText.FormatDurationLocalized(snapshot.ProductionCycleTicks));
        _offDutyValue.Text = UiText.Get("ui.policies.off_duty_value");
        _constructionValue.Text = UiText.Get("ui.policies.construction_value");
    }

    private static string FormatTimeOfDay(int dayTick)
    {
        int totalMinutes = dayTick * 24 * 60 / GameClock.TicksPerInGameDay;
        return $"{totalMinutes / 60:00}:{totalMinutes % 60:00}";
    }

    private void OnWorldTickAdvanced(int _) { if (Visible) Refresh(); }

    private void ApplyResponsiveBounds()
    {
        Vector2 available = GetParentOrNull<Control>()?.Size ?? Vector2.Zero;
        if (available.X < 100f || available.Y < 100f) available = GetViewportRect().Size;
        Vector2 size = new(
            Mathf.Min(PreferredSize.X, Mathf.Max(360f, available.X - 48f)),
            Mathf.Min(PreferredSize.Y, Mathf.Max(320f, available.Y - 48f)));
        SetAnchorsPreset(LayoutPreset.Center);
        OffsetLeft = -Mathf.Round(size.X * 0.5f);
        OffsetTop = -Mathf.Round(size.Y * 0.5f);
        OffsetRight = Mathf.Round(size.X * 0.5f);
        OffsetBottom = Mathf.Round(size.Y * 0.5f);
    }
}
