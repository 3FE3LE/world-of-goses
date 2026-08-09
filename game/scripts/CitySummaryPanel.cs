#nullable enable

using System.Globalization;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Persistent city-at-a-glance surface. It renders the same immutable status
/// projection as the top bar and owns only ephemeral collapse state.
/// </summary>
[GlobalClass]
public partial class CitySummaryPanel : PanelContainer
{
    public const int PanelWidth = 240;
    public const int ExpandedBodyHeight = 536;

    [Export] public NodePath ControllerPath { get; set; } = "../../../CityWorldController";

    private CityWorldController _controller = null!;
    private CollapsiblePanelHeader _header = null!;
    private ScrollContainer _body = null!;
    private VBoxContainer _content = null!;

    public override void _Ready()
    {
        OverlayLayers.Apply(this, OverlayLayers.Hud);
        ThemeTypeVariation = "HudSurface";
        CustomMinimumSize = new Vector2(PanelWidth, 0);
        MouseFilter = MouseFilterEnum.Stop;

        BuildSurface();
        _controller = GetNode<CityWorldController>(ControllerPath);
        _controller.WorldTickAdvanced += OnStateChanged;
        _controller.BuildingStateChanged += OnStateChanged;
        _controller.ProjectStateChanged += OnStateChanged;
        _controller.NaturalResourceStateChanged += OnStateChanged;
        _controller.CultivationSiteStateChanged += OnStateChanged;
        _controller.HeroCreated += OnStateChanged;
        LineageThemeRegistry.ActiveLineageChanged += OnLineageChanged;
        Refresh(_controller.GetCityStatusSnapshot());
    }

    public override void _ExitTree()
    {
        if (_controller is not null)
        {
            _controller.WorldTickAdvanced -= OnStateChanged;
            _controller.BuildingStateChanged -= OnStateChanged;
            _controller.ProjectStateChanged -= OnStateChanged;
            _controller.NaturalResourceStateChanged -= OnStateChanged;
            _controller.CultivationSiteStateChanged -= OnStateChanged;
            _controller.HeroCreated -= OnStateChanged;
        }
        LineageThemeRegistry.ActiveLineageChanged -= OnLineageChanged;
    }

    private void BuildSurface()
    {
        var layout = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Pass,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        layout.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        AddChild(layout);

        _header = new CollapsiblePanelHeader(UiText.Get("ui.city_summary.city"));
        _header.ExpandedChanged += expanded => _body.Visible = expanded;
        layout.AddChild(_header);

        _body = new ScrollContainer
        {
            Name = "SummaryBody",
            CustomMinimumSize = new Vector2(0, ExpandedBodyHeight),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            MouseFilter = MouseFilterEnum.Stop,
        };
        layout.AddChild(_body);

        _content = new VBoxContainer
        {
            Name = "SummaryContent",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Pass,
        };
        _content.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        _body.AddChild(_content);
    }

    public void Refresh(CityStatusSnapshot snapshot)
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

        if (snapshot.HousingCapacity > 0)
        {
            AddSeparator();
            _content.AddChild(new HudSectionHeader(UiText.Get("ui.city_summary.status")));
            _content.AddChild(new HudMetricRow(
                UiText.Get("ui.city_summary.housing"),
                $"{snapshot.CitizenCount}/{snapshot.HousingCapacity}"));
            _content.AddChild(new HudProgressBar(
                (double)snapshot.CitizenCount / snapshot.HousingCapacity));
        }

        AddSeparator();
        _content.AddChild(new HudSectionHeader(
            UiText.Get("ui.city_summary.resources"),
            snapshot.Resources.Count.ToString(CultureInfo.InvariantCulture)));
        if (snapshot.Resources.Count == 0)
        {
            _content.AddChild(EmptyLabel("ui.city_summary.no_resources"));
        }
        else
        {
            foreach (ResourceInventoryItem resource in snapshot.Resources)
            {
                var row = new HudResourceRow(
                    resource.Resource,
                    UiText.Get(resource.Resource.ToString().ToLowerInvariant()),
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
            _content.AddChild(EmptyLabel("ui.city_summary.no_construction"));
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

    private void BuildIdentity(CityStatusSnapshot snapshot, string lineage)
    {
        var identity = new HBoxContainer
        {
            Name = "CityIdentity",
            MouseFilter = MouseFilterEnum.Pass,
        };
        identity.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        _content.AddChild(identity);
        identity.AddChild(new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(IconPaths.Building),
            StretchMode = TextureRect.StretchModeEnum.Keep,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = LineageThemeRegistry.IconAccent,
            TooltipText = UiText.Format("ui.city_summary.founding_lineage", lineage),
        });
        var labels = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        labels.AddThemeConstantOverride("separation", 0);
        identity.AddChild(labels);
        labels.AddChild(new Label
        {
            Text = lineage,
            ThemeTypeVariation = "HudHeader",
            MouseFilter = MouseFilterEnum.Ignore,
        });
        string population = snapshot.HousingCapacity > 0
            ? UiText.Format(
                "ui.status.population_with_capacity",
                snapshot.CitizenCount,
                snapshot.HousingCapacity)
            : UiText.Format("ui.status.population", snapshot.CitizenCount);
        labels.AddChild(new Label
        {
            Text = population,
            ThemeTypeVariation = "HudCaption",
            MouseFilter = MouseFilterEnum.Ignore,
        });
    }

    private void AddSeparator() => _content.AddChild(new HSeparator
    {
        ThemeTypeVariation = "HudSeparator",
        MouseFilter = MouseFilterEnum.Ignore,
    });

    private static Label EmptyLabel(string key) => new()
    {
        Text = UiText.Get(key),
        ThemeTypeVariation = "HudCaption",
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    private static string ResourceTooltip(ResourceInventoryItem resource)
    {
        int reserved = resource.TotalAmount - resource.AvailableAmount;
        string tooltip = UiText.Get(resource.Resource.ToString().ToLowerInvariant())
            + "\n" + UiText.Format("ui.status.resource_available", resource.AvailableAmount)
            + "\n" + UiText.Format("ui.status.resource_stored", resource.TotalAmount);
        return reserved > 0
            ? tooltip + "\n" + UiText.Format("ui.status.resource_reserved", reserved)
            : tooltip;
    }

    private void OnStateChanged(int _) => Refresh(_controller.GetCityStatusSnapshot());

    private void OnLineageChanged(string _) => Refresh(_controller.GetCityStatusSnapshot());

    private static void ReapplyAccent(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is TextureRect icon) icon.Modulate = LineageThemeRegistry.IconAccent;
            ReapplyAccent(child);
        }
    }
}
