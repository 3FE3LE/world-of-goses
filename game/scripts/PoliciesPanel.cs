#nullable enable
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Central, read-only first surface for city-wide policies.
///
/// <para>Architecture Hardening A11: the static hierarchy —
/// <c>Surface</c> → <c>Margin</c> → <c>Layout</c> → <c>Header</c> +
/// <c>Scroll</c> → <c>Body</c> with its five title/value pairs — is authored
/// in <c>game/scenes/Components/PoliciesPanel.tscn</c>, which
/// <c>CityPrototype.tscn</c> instances. The script resolves those nodes,
/// fills them from <see cref="CityPolicySnapshot"/>, and owns the modal
/// open/close and the responsive bounds.</para>
/// </summary>
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

    /// <summary>
    /// Binds the authored nodes and writes the text the catalogue owns. The
    /// scene carries the shape and the theme variations; every string still
    /// comes from <see cref="UiText"/>, because a literal in a <c>.tscn</c>
    /// is a string no locale switch can reach.
    /// </summary>
    private void Build()
    {
        const string body = "Surface/Margin/Layout/Scroll/Body";
        var header = GetNode<PanelHeader>("Surface/Margin/Layout/Header");
        header.SetTitle(UiText.Get("ui.policies.title"));
        header.CloseRequested += OnCloseRequested;

        _scheduleValue = BindPolicy(body, "Workday", "ui.policies.workday");
        _dayStateValue = BindPolicy(body, "CurrentState", "ui.policies.current_state");
        _productionValue = BindPolicy(body, "Production", "ui.policies.production");
        _offDutyValue = BindPolicy(body, "OffDuty", "ui.policies.off_duty");
        _constructionValue = BindPolicy(body, "Construction", "ui.policies.construction");

        GetNode<Label>($"{body}/Future").Text = UiText.Get("ui.policies.future");
    }

    private Label BindPolicy(string bodyPath, string rowName, string titleKey)
    {
        GetNode<Label>($"{bodyPath}/{rowName}Title").Text = UiText.Get(titleKey);
        return GetNode<Label>($"{bodyPath}/{rowName}Value");
    }

    private void OnCloseRequested() => _modalHost.Close();

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
