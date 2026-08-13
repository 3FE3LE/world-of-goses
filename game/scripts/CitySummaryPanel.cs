#nullable enable

using System.Collections.Generic;
using System.Globalization;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Persistent city-at-a-glance surface. Renders the same immutable status
/// projection as the top bar and owns only ephemeral collapse state.
///
/// <para>Architecture Hardening A11: the static hierarchy —
/// <c>Layout</c> → <c>Header</c> → <c>SummaryBody</c> → <c>Gutter</c> →
/// <c>SummaryContent</c> — is authored in
/// <c>game/scenes/Components/CitySummaryPanel.tscn</c>, which
/// <c>CityPrototype.tscn</c> instances at its HUD slot. The script resolves
/// those five nodes, subscribes to controller signals, and composes only the
/// snapshot-driven rows in <see cref="ApplySnapshot"/>.</para>
///
/// <para>The scene has to be <em>instanced</em> by the parent, not merely
/// authored beside it: an earlier attempt added the component scene while
/// <c>CityPrototype.tscn</c> still declared a bare <c>PanelContainer</c> with
/// this script attached, so at runtime the panel had no children and
/// <c>GetNode("Layout/Header")</c> took the boot down. The
/// <c>instance=ExtResource</c> line in the parent scene is what makes the
/// hierarchy exist.</para>
/// </summary>
[GlobalClass]
public partial class CitySummaryPanel : PanelContainer
{
    public const int PanelWidth = 240;

    [Export] public NodePath ControllerPath { get; set; } = "../../../../CityWorldController";

    private CityWorldController _controller = null!;
    private LocaleManager? _localeManager;
    private CollapsiblePanelHeader _header = null!;
    private ScrollContainer _body = null!;
    private VBoxContainer _content = null!;

    // Survival-critical → construction-relevant → remaining. Lives in
    // `ResourcePriority` so the city summary and the top-bar ticker
    // agree on the priority: a resource that appears second in the ticker
    // also appears second in the summary, not fourth.

    public override void _Ready()
    {
        OverlayLayers.Apply(this, OverlayLayers.Hud);
        MouseFilter = MouseFilterEnum.Stop;
        _header = GetNode<CollapsiblePanelHeader>("Layout/Header");
        _body = GetNode<ScrollContainer>("Layout/SummaryBody");
        _content = GetNode<VBoxContainer>("Layout/SummaryBody/Gutter/SummaryContent");
        _header.ExpandedChanged += OnHeaderExpandedChanged;
        OnHeaderExpandedChanged(_header.Expanded);
        _controller = GetNode<CityWorldController>(ControllerPath);
        _controller.WorldTickAdvanced += OnStateChanged;
        _controller.BuildingStateChanged += OnStateChanged;
        _controller.ProjectStateChanged += OnStateChanged;
        _controller.NaturalResourceStateChanged += OnStateChanged;
        _controller.CultivationSiteStateChanged += OnStateChanged;
        _controller.HeroCreated += OnStateChanged;
        _localeManager = GetNodeOrNull<LocaleManager>("/root/LocaleManager");
        if (_localeManager is not null) _localeManager.LocaleChanged += OnLocaleChanged;
        LineageThemeRegistry.ActiveLineageChanged += OnLineageChanged;
        Refresh(_controller.GetCityStatusSnapshot());
    }

    /// <summary>
    /// Folding the section hides its body and lets the panel shrink to its
    /// header; unfolding gives it the side-panel envelope and the body takes
    /// whatever is left under the header, scrolling if the content exceeds it
    /// (GitHub #17). No height is computed here — the container tree resolves
    /// it, which is what stops the number of headers from deciding the outer
    /// size.
    ///
    /// <para>The scene keeps the <c>Gutter</c> — the margin the vertical
    /// scrollbar draws over — inside the scrolled content rather than
    /// insetting <c>SummaryBody</c> itself, because the bar is positioned
    /// against the viewport's right edge and would follow any margin put
    /// there. See <see cref="Tokens.ScrollGutter"/>.</para>
    /// </summary>
    private void OnHeaderExpandedChanged(bool expanded)
    {
        _body.Visible = expanded;
        SizeFlagsVertical = SidePanelHost.PanelSizing(expanded);
    }

    public override void _ExitTree()
    {
        if (_header is not null) _header.ExpandedChanged -= OnHeaderExpandedChanged;
        if (_controller is not null)
        {
            _controller.WorldTickAdvanced -= OnStateChanged;
            _controller.BuildingStateChanged -= OnStateChanged;
            _controller.ProjectStateChanged -= OnStateChanged;
            _controller.NaturalResourceStateChanged -= OnStateChanged;
            _controller.CultivationSiteStateChanged -= OnStateChanged;
            _controller.HeroCreated -= OnStateChanged;
        }
        if (_localeManager is not null) _localeManager.LocaleChanged -= OnLocaleChanged;
        LineageThemeRegistry.ActiveLineageChanged -= OnLineageChanged;
    }

    public void Refresh(CityStatusSnapshot snapshot)
    {
        // Coalesce N signals per frame into a single ApplySnapshot. The
        // full SceneTree rebuild that runs below is expensive enough to
        // require this guard — without it, a single tick that raises
        // WorldTickAdvanced + BuildingStateChanged + ProjectStateChanged
        // produced three rebuilds in a row.
        if (_refreshQueued) return;
        _refreshQueued = true;
        _pendingSnapshot = snapshot;
        CallDeferred(MethodName.ApplyQueuedRefresh);
    }

    private bool _refreshQueued;
    private CityStatusSnapshot? _pendingSnapshot;

    private void ApplyQueuedRefresh()
    {
        _refreshQueued = false;
        if (_pendingSnapshot is { } snapshot)
        {
            _pendingSnapshot = null;
            ApplySnapshot(snapshot);
        }
    }

    /// <summary>
    /// Rebuilds the summary body against the latest snapshot. This is the
    /// only place where the SceneTree is restructured; deferred callers
    /// coalesce so a frame with multiple signals produces one rebuild.
    /// </summary>
    private void ApplySnapshot(CityStatusSnapshot snapshot)
    {
        foreach (Node child in _content.GetChildren())
        {
            _content.RemoveChild(child);
            child.QueueFree();
        }

        string lineage = string.IsNullOrWhiteSpace(snapshot.LineageName)
            ? UiText.Get("ui.city_summary.unknown_lineage")
            : UiText.Get(snapshot.LineageName);
        _header.Text = UiText.Format("ui.city_summary.header", lineage);
        BuildIdentity(snapshot, lineage);

        AddSeparator();
        _content.AddChild(new HudSectionHeader(UiText.Get("ui.city_summary.status")));
        BuildStatusSection(snapshot);

        AddSeparator();
        _content.AddChild(new HudSectionHeader(
            UiText.Get("ui.city_summary.resources"),
            snapshot.Resources.Count.ToString(CultureInfo.InvariantCulture)));
        if (snapshot.Resources.Count == 0)
        {
            _content.AddChild(new HudEmptyState("ui.city_summary.no_resources"));
        }
        else
        {
            foreach (ResourceInventoryItem resource in SequenceResources(snapshot))
            {
                // No authoritative production rate exists; the delta column
                // stays hidden by passing an empty string.
                var row = new HudResourceRow(
                    resource.Resource,
                    ResourceTypeLocalizer.Label(resource.Resource),
                    resource.AvailableAmount.ToString(CultureInfo.InvariantCulture));
                row.TooltipText = ResourceTooltip(resource);
                _content.AddChild(row);
            }
        }

        AddSeparator();
        _content.AddChild(new HudSectionHeader(
            UiText.Get("ui.city_summary.construction"),
            snapshot.Projects.Count.ToString(CultureInfo.InvariantCulture)));
        if (snapshot.Projects.Count == 0)
        {
            _content.AddChild(new HudEmptyState("ui.city_summary.no_construction"));
        }
        else
        {
            foreach (CityStatusSnapshot.ProjectItem project in snapshot.Projects)
            {
                _content.AddChild(new ConstructionQueueItem(project));
            }
        }

        ReapplyAccent(_content);
    }

    /// <summary>
    /// Composes the truthful STATUS section from authoritative snapshot
    /// fields. Each metric answers a different question; warnings only
    /// appear where domain meaning already defines the threshold
    /// (food exhaustion, harvest missing that threshold, housing at
    /// capacity).
    /// </summary>
    private void BuildStatusSection(CityStatusSnapshot snapshot)
    {
        // 1. Food horizon — warning if less than one day of rations remains.
        var foodRow = new HudMetricRow(
            UiText.Get("ui.city_summary.status_food_horizon"),
            UiText.Format(
                "ui.city_summary.food_horizon_format", snapshot.FoodHorizonDays),
            FoodCriticalGlyph(snapshot));
        if (snapshot.FoodHorizonDays < 1)
        {
            foodRow.TooltipText = UiText.Get("ui.city_summary.tooltip_food_critical");
        }
        _content.AddChild(foodRow);

        // 2. Citizens currently contributing at a worksite.
        _content.AddChild(new HudMetricRow(
            UiText.Get("ui.city_summary.status_citizens_work"),
            snapshot.CitizensAtWork.ToString(CultureInfo.InvariantCulture)));

        // 3. Citizens currently at home (resting / available).
        _content.AddChild(new HudMetricRow(
            UiText.Get("ui.city_summary.status_citizens_home"),
            snapshot.CitizensAtHome.ToString(CultureInfo.InvariantCulture)));

        // 4. Time until the first crop harvest — warning if it lands
        // after the food runs out.
        var harvestRow = new HudMetricRow(
            UiText.Get("ui.city_summary.status_next_harvest"),
            FormatHarvest(snapshot),
            HarvestLateGlyph(snapshot));
        if (HarvestIsLate(snapshot))
        {
            harvestRow.TooltipText = UiText.Get("ui.city_summary.tooltip_harvest_late");
        }
        _content.AddChild(harvestRow);

        // 5. Whether the workday rules currently apply.
        _content.AddChild(new HudMetricRow(
            UiText.Get("ui.city_summary.status_labor"),
            UiText.Get(snapshot.IsLaborTime
                ? "ui.city_summary.labor_active"
                : "ui.city_summary.labor_paused")));

        // Housing bar at the end of the section — a single visual cue for
        // capacity, complementary to the text rows above. Empty cities
        // (HousingCapacity == 0) skip it entirely.
        if (snapshot.HousingCapacity > 0)
        {
            _content.AddChild(new HudProgressBar(
                (double)snapshot.CitizenCount / snapshot.HousingCapacity));
        }
    }

    private static string FoodCriticalGlyph(CityStatusSnapshot snapshot) =>
        snapshot.FoodHorizonDays < 1 ? IconPaths.Warning : "";

    private static string HarvestLateGlyph(CityStatusSnapshot snapshot) =>
        HarvestIsLate(snapshot) ? IconPaths.Warning : "";

    private static bool HarvestIsLate(CityStatusSnapshot snapshot)
    {
        if (snapshot.TicksUntilFirstHarvest is not int ticks) return false;
        int foodRunsOutAt = snapshot.FoodHorizonDays * GameClock.TicksPerInGameDay;
        return ticks > foodRunsOutAt;
    }

    private static string FormatHarvest(CityStatusSnapshot snapshot)
    {
        if (snapshot.TicksUntilFirstHarvest is not int ticks)
        {
            return UiText.Get("ui.city_summary.no_next_harvest");
        }
        int days = ticks / GameClock.TicksPerInGameDay;
        int hours = ticks * 24 / GameClock.TicksPerInGameDay % 24;
        return UiText.Format("ui.city_summary.harvest_format", days, hours);
    }

    private static IEnumerable<ResourceInventoryItem> SequenceResources(CityStatusSnapshot snapshot)
    {
        return ResourcePriority.Prioritize(snapshot.Resources);
    }

    private void BuildIdentity(CityStatusSnapshot snapshot, string lineage)
    {
        string population = snapshot.HousingCapacity > 0
            ? UiText.Format(
                "ui.status.population_with_capacity",
                snapshot.CitizenCount,
                snapshot.HousingCapacity)
            : UiText.Format("ui.status.population", snapshot.CitizenCount);
        _content.AddChild(new HudIdentityRow(
            IconPaths.Building,
            lineage,
            population,
            UiText.Format("ui.city_summary.founding_lineage", lineage))
        {
            Name = "CityIdentity",
        });
    }

    private void AddSeparator() => _content.AddChild(new HudSeparator());

    private static string ResourceTooltip(ResourceInventoryItem resource)
    {
        int reserved = resource.TotalAmount - resource.AvailableAmount;
        string tooltip = ResourceTypeLocalizer.Label(resource.Resource)
            + "\n" + UiText.Format("ui.status.resource_available", resource.AvailableAmount)
            + "\n" + UiText.Format("ui.status.resource_stored", resource.TotalAmount);
        return reserved > 0
            ? tooltip + "\n" + UiText.Format("ui.status.resource_reserved", reserved)
            : tooltip;
    }

    private void OnStateChanged(int _) => Refresh(_controller.GetCityStatusSnapshot());

    private void OnLineageChanged(string _) => Refresh(_controller.GetCityStatusSnapshot());

    private void OnLocaleChanged(string _) => Refresh(_controller.GetCityStatusSnapshot());

    private static void ReapplyAccent(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is TextureRect icon) icon.Modulate = LineageThemeRegistry.IconAccent;
            ReapplyAccent(child);
        }
    }
}
