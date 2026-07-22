#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Replaces the one-line offline banner with a chronological panel
/// of <see cref="WorldEvent"/> rows. Each row carries a presentation-owned
/// icon, a one-line summary, and
/// the tick at which it happened relative to the offline window's
/// start.
///
/// The panel is shown when <see cref="OfflineProgressionReport.HadProgression"/>
/// is true and hidden otherwise. The host (typically
/// <see cref="CityMacroView"/>) calls <see cref="ShowReport"/> after
/// it loads the world and detects an offline stretch.
/// </summary>
public partial class OfflineReportPanel : PanelContainer
{
    private const int MaxRows = 80;
    private const int RowSpacing = 4;
    private const int IconSize = 14;
    private const float ExpandedTopOffset = -336f;
    private const float CollapsedTopOffset = -92f;

    private Label _summary = null!;
    private ScrollContainer _scroll = null!;
    private VBoxContainer _list = null!;
    private IconButton _collapseButton = null!;
    private IReadOnlyList<WorldEvent> _currentLiveEvents = System.Array.Empty<WorldEvent>();
    private bool _isExpanded;
    private int _compactedCount;
    private int _lastLiveEventCount = -1;
    private WorldEventId? _lastLiveEventId;
    private bool _followNewestAfterLayout;
    private double _scrollValueAfterLayout;
    private bool _collapseHovered;
    private CityWorldController? _controller;

    /// <summary>
    /// Wires the controller so the "Decisions needed" rows can route
    /// to the matching building detail view when clicked. Call this
    /// once from the host (the macro view) before the panel needs to
    /// resolve subjects.
    /// </summary>
    public void SetController(CityWorldController controller)
    {
        _controller = controller;
    }

    public override void _Ready()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        MouseFilter = MouseFilterEnum.Stop;
        AddThemeStyleboxOverride(
            "panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
        AddThemeConstantOverride("margin_left", 16);
        AddThemeConstantOverride("margin_right", 16);
        AddThemeConstantOverride("margin_top", 12);
        AddThemeConstantOverride("margin_bottom", 12);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(margin);

        var shell = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        shell.AddThemeConstantOverride("separation", 8);
        margin.AddChild(shell);

        // One direct native Button owns the whole header. The previous
        // icon/title/spacer/button composition created several overlapping
        // Control rectangles and proved unreliable for pointer hit-testing.
        _collapseButton = new IconButton
        {
            IconPath = IconPaths.ChevronUp,
            ButtonText = "Chronicle — click to collapse",
            TooltipText = "Click to show only the newest event.",
            ThemeTypeVariation = "ButtonText",
            CustomMinimumSize = new Vector2(0, 40),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            FocusMode = FocusModeEnum.All,
        };
        _collapseButton.Pressed += OnCollapsePressed;
        shell.AddChild(_collapseButton);

        _summary = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = "BodySmall",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _summary.AddThemeColorOverride("font_color", LineageThemeRegistry.IconAccent);
        shell.AddChild(_summary);

        _scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize = new Vector2(0, 220),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _scroll.GuiInput += OnScrollGuiInput;
        shell.AddChild(_scroll);

        _list = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            // Let wheel input bubble to the ScrollContainer instead of
            // stopping on the list's otherwise non-interactive background.
            MouseFilter = MouseFilterEnum.Pass,
        };
        _list.AddThemeConstantOverride("separation", RowSpacing);
        _scroll.AddChild(_list);

        _summary.Text = "The city's recent events will be recorded here.";
    }

    public override void _ExitTree()
    {
        if (_scroll is not null)
        {
            _scroll.GuiInput -= OnScrollGuiInput;
        }
        if (_collapseButton is not null)
        {
            _collapseButton.Pressed -= OnCollapsePressed;
        }
    }

    private void OnCollapsePressed()
    {
        SetExpanded(!_isExpanded);
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!IsVisibleInTree() || _collapseButton is null) return;
        if (inputEvent is not InputEventMouseButton mouseButton
            || mouseButton.ButtonIndex != MouseButton.Left
            || !mouseButton.Pressed)
        {
            return;
        }
        if (!_collapseButton.GetGlobalRect().HasPoint(mouseButton.GlobalPosition)) return;

        OnCollapsePressed();
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (_collapseButton is null || !IsVisibleInTree()) return;
        bool hovered = _collapseButton.GetGlobalRect().HasPoint(GetGlobalMousePosition());
        if (hovered == _collapseHovered) return;
        _collapseHovered = hovered;
        _collapseButton.Modulate = hovered
            ? new Color(0.86f, 0.94f, 1f, 1f)
            : Colors.White;
        _collapseButton.MouseDefaultCursorShape = hovered
            ? CursorShape.PointingHand
            : CursorShape.Arrow;
    }

    private void SetExpanded(bool expanded)
    {
        _isExpanded = expanded;
        _lastLiveEventCount = -1;
        ApplyExpandedState();
        ShowLog(_currentLiveEvents);
    }

    private void ApplyExpandedState()
    {
        OffsetTop = _isExpanded ? ExpandedTopOffset : CollapsedTopOffset;
        _summary.Visible = _isExpanded;
        _scroll.Visible = _isExpanded || _compactedCount > 0;
        _scroll.CustomMinimumSize = new Vector2(0, _isExpanded ? 220 : 36);
        _scroll.VerticalScrollMode = _isExpanded
            ? ScrollContainer.ScrollMode.Auto
            : ScrollContainer.ScrollMode.Disabled;
        _collapseButton.SetIconAndLabel(
            _isExpanded ? IconPaths.ChevronUp : IconPaths.ChevronDown,
            _isExpanded
                ? "Chronicle — click to collapse"
                : $"Chronicle — click to expand ({_compactedCount})");
        _collapseButton.TooltipText = _isExpanded
            ? "Click to show only the newest event."
            : "Click to open the full chronicle.";
    }

    private void OnScrollGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
        {
            return;
        }
        if (mouseButton.ButtonIndex is not MouseButton.WheelUp and not MouseButton.WheelDown)
        {
            return;
        }

        var bar = _scroll.GetVScrollBar();
        if (bar is null) return;

        double direction = mouseButton.ButtonIndex == MouseButton.WheelUp ? -1d : 1d;
        double factor = mouseButton.Factor > 0f ? mouseButton.Factor : 1d;
        bar.Value += direction * 48d * factor;
        _scroll.AcceptEvent();
    }

    /// <summary>
    /// Populates the panel with a fresh offline report. The panel
    /// shows itself; passing a report with no events keeps it hidden.
    /// </summary>
    public void ShowReport(OfflineProgressionReport report)
    {
        ClearRows();

        if (!report.HadProgression || report.Events.Count == 0)
        {
            Hide();
            return;
        }

        _summary.Text = SummariseReport(report);

        // "Decisions needed" — distinct groups of ProductionBlocked
        // events by (subject, cause). Renders before the event list so
        // the player sees what requires attention at a glance. Each
        // row is a clickable button when the subject resolves to a
        // building currently in the world.
        var decisions = GroupDecisionsNeeded(report.Events);
        if (decisions.Count > 0)
        {
            var header = new Label
            {
                Text = $"Decisions needed ({decisions.Count})",
                ThemeTypeVariation = "SectionTitle",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            _list.AddChild(header);
            foreach (var entry in decisions)
            {
                _list.AddChild(BuildDecisionRow(entry));
            }
            _list.AddChild(new HSeparator());
        }

        // Show the most recent N events; older ones would only add
        // noise to the panel.
        IReadOnlyList<EventItem> events = CompactConsecutiveEvents(report.Events);
        int skip = System.Math.Max(0, events.Count - MaxRows);
        for (int i = skip; i < events.Count; i++)
        {
            _list.AddChild(new EventRow(events[i]));
        }

        Show();
        // Defer the scroll-to-bottom until the container has sized.
        CallDeferred(MethodName.ScrollToBottom);
    }

    /// <summary>
    /// Shows the live chronological log. This keeps the same visual
    /// language as the offline report while making the simulation's
    /// event slice visible during play.
    /// </summary>
    public void ShowLog(IReadOnlyList<WorldEvent> events)
    {
        _currentLiveEvents = events;
        WorldEventId? newestId = events.Count > 0 ? events[^1].Id : null;
        if (_lastLiveEventCount == events.Count && _lastLiveEventId == newestId)
        {
            Show();
            return;
        }

        var scrollBar = _scroll.GetVScrollBar();
        bool firstRender = _lastLiveEventCount < 0;
        bool wasFollowingNewest = firstRender
            || scrollBar is null
            || scrollBar.Value >= scrollBar.MaxValue - scrollBar.Page - 1d;
        double previousScrollValue = scrollBar?.Value ?? 0d;

        ClearRows();

        var liveDecisions = GroupDecisionsNeeded(events);
        IReadOnlyList<EventItem> compactedEvents = CompactConsecutiveEvents(events);
        _compactedCount = compactedEvents.Count;
        if (liveDecisions.Count > 0)
        {
            _summary.Text = "Needs attention · newest entry at the bottom";
            foreach (var entry in liveDecisions)
            {
                _list.AddChild(BuildDecisionRow(entry));
            }
            _list.AddChild(new HSeparator());
        }
        else
        {
            _summary.Text = events.Count == 0
                ? "The city's recent events will be recorded here."
                : "Newest entry at the bottom";
        }

        int visibleRows = _isExpanded ? MaxRows : 1;
        int skip = System.Math.Max(0, compactedEvents.Count - visibleRows);
        for (int i = skip; i < compactedEvents.Count; i++)
        {
            _list.AddChild(new EventRow(compactedEvents[i]));
        }

        Show();
        _lastLiveEventCount = events.Count;
        _lastLiveEventId = newestId;
        ApplyExpandedState();
        _followNewestAfterLayout = wasFollowingNewest;
        _scrollValueAfterLayout = previousScrollValue;
        if (_isExpanded)
        {
            CallDeferred(MethodName.ApplyPendingLiveScroll);
        }
    }

    private void ClearRows()
    {
        foreach (var child in _list.GetChildren())
        {
            _list.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void ApplyPendingLiveScroll()
    {
        if (_followNewestAfterLayout)
        {
            ScrollToBottom();
            return;
        }

        var bar = _scroll.GetVScrollBar();
        if (bar is not null)
        {
            bar.Value = System.Math.Min(_scrollValueAfterLayout, bar.MaxValue - bar.Page);
        }
    }

    private void ScrollToBottom()
    {
        if (_scroll is null) return;
        var bar = _scroll.GetVScrollBar();
        if (bar is not null) bar.Value = bar.MaxValue;
    }

    private static string SummariseReport(OfflineProgressionReport report)
    {
        string time = FormatTime(report.SimulatedTime);
        return report.StockAdded > 0
            ? $"Welcome back · {time} simulated · +{report.StockAdded} stock"
            : $"Welcome back · {time} simulated";
    }

    /// <summary>
    /// Groups <see cref="WorldEventKind.ProductionBlocked"/> events
    /// by their subject so the offline report can surface "this many
    /// stoppages from that building" at a glance. Each entry pairs the
    /// display label with the optional building id so the panel can
    /// route the click to the matching detail view.
    /// </summary>
    private System.Collections.Generic.List<DecisionNeeded> GroupDecisionsNeeded(
        IReadOnlyList<WorldEvent> events)
    {
        var grouped = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var evt in events)
        {
            if (evt.Kind != WorldEventKind.ProductionBlocked) continue;
            grouped.TryGetValue(evt.SubjectName, out var count);
            grouped[evt.SubjectName] = count + 1;
        }
        var output = new System.Collections.Generic.List<DecisionNeeded>();
        foreach (var pair in grouped)
        {
            output.Add(new DecisionNeeded($"{pair.Value}× {pair.Key}", ResolveBuildingId(pair.Key)));
        }
        return output;
    }

    /// <summary>
    /// Looks up a building id by display name. Returns null when the
    /// controller has not been wired (the panel is being unit-tested
    /// without a scene) or when no building currently matches the
    /// subject name (e.g. a Forest that was demolished).
    /// </summary>
    private BuildingId? ResolveBuildingId(string subjectName)
    {
        if (_controller is null) return null;
        foreach (var building in _controller.World.Buildings.Values)
        {
            if (building.DisplayName == subjectName) return building.Id;
        }
        return null;
    }

    private void OnDecisionClicked(BuildingId id)
    {
        _controller?.SelectBuilding(id);
    }

    /// <summary>
    /// Builds a single row for a "Decisions needed" entry. When the
    /// subject resolves to a building, the row is a button that opens
    /// the matching detail view; otherwise it falls back to a label
    /// so the player still sees the information.
    /// </summary>
    private Control BuildDecisionRow(DecisionNeeded entry)
    {
        if (entry.TargetBuildingId is { } buildingId)
        {
            var button = new Button
            {
                Text = $"{entry.Label} · open",
                TooltipText = "Open the building that needs attention.",
                ThemeTypeVariation = "ButtonText",
                CustomMinimumSize = new Vector2(0, 28),
                FocusMode = FocusModeEnum.All,
            };
            button.Pressed += () => OnDecisionClicked(buildingId);
            return button;
        }
        return new Label
        {
            Text = entry.Label,
            ThemeTypeVariation = "BodyText",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
    }

    private readonly record struct DecisionNeeded(string Label, BuildingId? TargetBuildingId);

    private static string FormatTime(System.TimeSpan time)
    {
        if (time.TotalDays >= 1) return $"{(int)time.TotalDays}d {time.Hours}h";
        if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
        if (time.TotalMinutes >= 1) return $"{(int)time.TotalMinutes}m {time.Seconds}s";
        return $"{(int)time.TotalSeconds}s";
    }

    public sealed record EventItem(
        WorldEventKind Kind,
        string SubjectName,
        int Amount,
        int FirstTick,
        int LastTick,
        string Summary);

    /// <summary>
    /// Compacts adjacent additive events without modifying the domain log.
    /// A different event kind or subject closes the current chain, so two
    /// production runs separated by another fact remain separate rows.
    /// </summary>
    public static IReadOnlyList<EventItem> CompactConsecutiveEvents(
        IReadOnlyList<WorldEvent> events)
    {
        var compacted = new System.Collections.Generic.List<EventItem>();
        foreach (var evt in events)
        {
            bool additive = evt.Amount > 0
                && evt.Kind is WorldEventKind.StockProduced or WorldEventKind.ProjectProgressed;
            if (additive
                && compacted.Count > 0
                && compacted[^1].Kind == evt.Kind
                && compacted[^1].SubjectName == evt.SubjectName)
            {
                EventItem previous = compacted[^1];
                int amount = previous.Amount + evt.Amount;
                compacted[^1] = previous with
                {
                    Amount = amount,
                    LastTick = evt.Tick,
                    Summary = SummariseCompacted(evt.Kind, evt.SubjectName, amount),
                };
                continue;
            }

            bool repeatedState = evt.Kind is WorldEventKind.StockCapped
                or WorldEventKind.WorkersExhausted
                or WorldEventKind.ProductionBlocked;
            if (repeatedState
                && compacted.Count > 0
                && compacted[^1].Kind == evt.Kind
                && compacted[^1].SubjectName == evt.SubjectName)
            {
                compacted[^1] = compacted[^1] with { LastTick = evt.Tick };
                continue;
            }

            compacted.Add(new EventItem(
                evt.Kind,
                evt.SubjectName,
                evt.Amount,
                evt.Tick,
                evt.Tick,
                evt.Summary));
        }
        return compacted;
    }

    private static string SummariseCompacted(
        WorldEventKind kind,
        string subjectName,
        int amount) => kind switch
    {
        WorldEventKind.StockProduced => $"{subjectName} produced +{amount}",
        WorldEventKind.ProjectProgressed => $"{subjectName} made +{amount} work",
        _ => subjectName,
    };

    /// <summary>
    /// One row of the offline report: tinted icon + summary + tick.
    /// The row is intentionally compact so the player can scan the
    /// full timeline without scrolling; the summary line carries
    /// the human meaning, the icon hints the category, and the tick
    /// anchors the row in time.
    /// </summary>
    private partial class EventRow : HBoxContainer
    {
        public EventRow(EventItem evt)
        {
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeConstantOverride("separation", 8);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            CustomMinimumSize = new Vector2(0, 24);

            var iconCell = new CenterContainer
            {
                CustomMinimumSize = new Vector2(IconSize, 24),
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            AddChild(iconCell);

            var icon = new TextureRect
            {
                StretchMode = TextureRect.StretchModeEnum.Keep,
                CustomMinimumSize = new Vector2(IconSize, IconSize),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                MouseFilter = MouseFilterEnum.Ignore,
                Modulate = LineageThemeRegistry.IconAccent,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            string? iconPath = IconPathFor(evt.Kind);
            if (iconPath is not null)
            {
                icon.Texture = ResourceLoader.Load<Texture2D>(iconPath);
            }
            iconCell.AddChild(icon);

            var label = new Label
            {
                Text = evt.Summary,
                ThemeTypeVariation = "BodySmall",
                VerticalAlignment = VerticalAlignment.Center,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            label.AddThemeColorOverride("font_color", LineageThemeRegistry.IconAccent);
            AddChild(label);

            var tickLabel = new Label
            {
                // A compacted row is dated by its most recent event; this is
                // the moment the player cares about and avoids exposing raw
                // simulation ticks as if they were meaningful UI language.
                Text = SimulationTimeText.Format(evt.LastTick),
                ThemeTypeVariation = "BodySmall",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(60, 0),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            tickLabel.AddThemeColorOverride("font_color", LineageThemeRegistry.IconAccent.Darkened(0.18f));
            AddChild(tickLabel);
        }
    }

    public static string FormatSimulationDate(int tick)
        => SimulationTimeText.Format(tick);

    private static string? IconPathFor(WorldEventKind kind) => kind switch
    {
        WorldEventKind.StockProduced => IconPaths.Coin,
        WorldEventKind.StockCapped => IconPaths.Check,
        WorldEventKind.WorkersExhausted => IconPaths.Warning,
        WorldEventKind.WorkerRecovered => IconPaths.Heart,
        WorldEventKind.DayBegan => IconPaths.Sun,
        WorldEventKind.NightBegan => IconPaths.Moon,
        WorldEventKind.ProjectProgressed => IconPaths.Building,
        WorldEventKind.ProjectPaused => IconPaths.Pause,
        WorldEventKind.ProjectResumed => IconPaths.Play,
        WorldEventKind.ProjectCompleted => IconPaths.Check,
        WorldEventKind.BuildingCreated => IconPaths.House,
        WorldEventKind.WellFedExpired => IconPaths.Clock,
        WorldEventKind.ProductionBlocked => IconPaths.Warning,
        _ => null,
    };
}
