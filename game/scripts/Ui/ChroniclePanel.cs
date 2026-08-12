#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Presentation;

namespace WorldofGoses.Ui;

/// <summary>
/// The single Chronicle presentation used by the macro HUD. The body is
/// a single bounded scroll container that holds the offline summary,
/// actionable production blockers and the event rows together — the
/// same shape <see cref="CitySummaryPanel"/> uses for its body. The
/// collapsible header governs the body so the player can fold the
/// chronicle exactly the way the city summary folds: header only,
/// nothing else. The rail can also force the body closed while the
/// rail itself is folded, so the rail's collapsed state stays slim.
/// </summary>
[GlobalClass]
public partial class ChroniclePanel : VBoxContainer
{
    private const int MaximumRows = 80;
    private const int IconSize = 16;

    /// <summary>
    /// Locked width matching <c>ExpeditionRail.PanelWidth</c>. Without
    /// this minimum, the chronicle's combined-minimum width falls to
    /// the chevron+title strip the moment the body hides and never
    /// restores when it comes back — <see cref="CitySummaryPanel"/>
    /// sidesteps the same trap by pinning the panel's minimum width.
    /// </summary>
    private const int MinWidth = 236;

    private const string BodyScrollName = "ChronicleBodyScroll";

    private readonly List<Control> _focusables = new();
    private readonly List<Control> _decisionButtons = new();
    private IReadOnlyList<WorldEvent> _events = Array.Empty<WorldEvent>();
    private CityWorldController? _controller;
    private string _offlineSummary = string.Empty;

    private CollapsiblePanelHeader _header = null!;
    private ScrollContainer _body = null!;

    public event Action<bool>? ExpandedChanged;
    public event Action? FocusablesChanged;

    public IReadOnlyList<Control> Focusables => _focusables;
    public CollapsiblePanelHeader Header => _header;

    /// <summary>
    /// The scrollable body, exposed because it no longer lives under this
    /// node: <see cref="ExpeditionRail"/> registers it with an
    /// <see cref="AccordionHost"/> so it shares one stretch of column with
    /// the expedition list instead of competing with it for height. This
    /// panel still builds and fills the body; it just does not parent it.
    /// </summary>
    public ScrollContainer Body => _body;

    public ChroniclePanel()
    {
        Name = "ChroniclePanel";
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        // The locked width keeps the chronicle from collapsing to the
        // chevron+title strip when the body hides. Without it the
        // combined-minimum shrinks to header-only and the layout never
        // gives the chronicle its full width back.
        CustomMinimumSize = new Vector2(MinWidth, 0);
        MouseFilter = MouseFilterEnum.Pass;
        AddThemeConstantOverride("separation", Tokens.SpacingTight);

        // The collapsible header is the affordance the player uses to
        // fold the chronicle: matches CitySummaryPanel exactly, so a
        // player who learns one section header learns the other.
        // Starts collapsed so the expedition rail is the initial
        // protagonist — accordion rule: only one is visible at a time.
        _header = new CollapsiblePanelHeader(
            UiText.Get("ui.expedition_rail.activity"),
            expanded: false);
        _header.ExpandedChanged += OnHeaderExpandedChanged;

        // One ScrollContainer wraps everything below the header. The
        // single-scroll design (caption + decisions + rows together)
        // matches CitySummaryPanel's body.
        //
        // Neither the header nor the body is added here. The rail mounts
        // the header above the shared AccordionHost and registers the body
        // inside it, so both headers stay on screen while only one body
        // does. The height comes from the host's rect; there is no fixed
        // strip and no vertical size flag to flip, because nothing competes
        // for the column any more.
        _body = new ScrollContainer
        {
            Name = BodyScrollName,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            MouseFilter = MouseFilterEnum.Stop,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _body.GuiInput += OnChronicleScrollGuiInput;
    }

    /// <summary>
    /// Whether the chronicle body is unfolded. Driven by the header's
    /// collapsible toggle — collapsed = header only, expanded = header
    /// plus rows, decisions and offline summary. The rail can also
    /// assign this directly so the chronicle folds in lockstep with
    /// the rail's own collapse.
    /// </summary>
    public bool Expanded
    {
        get => _header.Expanded;
        set => _header.Expanded = value;
    }

    public void SetController(CityWorldController controller) => _controller = controller;

    public void RefreshLive(IReadOnlyList<WorldEvent> events)
    {
        _events = events;
        RebuildBody();
    }

    public void ShowOfflineReport(OfflineProgressionReport report)
    {
        if (!report.HadProgression) return;
        _events = report.Events;
        _offlineSummary = UiText.Format(
            "ui.chronicle.welcome",
            SimulationTimeText.FormatDurationLocalized(report.TicksApplied));
        _header.Expanded = true;
        RebuildBody();
        ScrollToStart();
    }

    public void ClearOfflineSummary()
    {
        if (string.IsNullOrEmpty(_offlineSummary)) return;
        _offlineSummary = string.Empty;
        RebuildBody();
    }

    /// <summary>
    /// Scrolls the chronicle's own bounded body scroll to the newest
    /// entry, if there is one. The rail asks for this whenever the
    /// chronicle opens so the player lands on the latest event
    /// instead of an arbitrary offset.
    /// </summary>
    public void ScrollToNewest()
    {
        VScrollBar bar = _body.GetVScrollBar();
        bar.Value = bar.MaxValue;
    }

    /// <summary>Scrolls the chronicle's bounded body to the top entry.</summary>
    public void ScrollToStart()
    {
        _body.GetVScrollBar().SetValueNoSignal(0d);
    }

    private void OnHeaderExpandedChanged(bool expanded)
    {
        // Visibility is no longer this panel's business. The body lives in
        // the rail's AccordionHost, which shows exactly one body; this panel
        // only reports the intent and keeps its own contents coherent.
        ExpandedChanged?.Invoke(expanded);
        if (expanded) ScrollToNewest();
        // The decision buttons are reachable only when the body is
        // unfolded; rebuild the focusable list so the rail's focus
        // chain does not contain buttons that are not on screen.
        RebuildFocusables();
    }

    private VBoxContainer? _contentContainer;
    private Label? _caption;
    private Label? _noActivityLabel;
    private VBoxContainer? _decisionsContainer;
    private HSeparator? _decisionsSeparator;
    private readonly List<ChronicleEventRow> _rowPool = new();

    private void RebuildBody()
    {
        if (_contentContainer is null) BuildBodyOnce();
        VBoxContainer content = _contentContainer!;
        _decisionButtons.Clear();

        IReadOnlyList<WorldEvent> meaningful =
            ChronicleEventProjection.MeaningfulEvents(_events);
        IReadOnlyList<ChronicleEventProjection.Item> compacted =
            ChronicleEventProjection.Compact(meaningful);

        // The chronicle count rides on the header so it stays visible
        // even when the body is folded — same affordance the rail's
        // own header uses.
        _header.Text = $"{UiText.Get("ui.expedition_rail.activity")} · {compacted.Count}";

        // Offline summary caption: hidden when there is no summary,
        // updated in place when there is. Same node lives across rebuilds.
        if (!string.IsNullOrWhiteSpace(_offlineSummary))
        {
            _caption!.Text = _offlineSummary;
            _caption.Visible = true;
        }
        else
        {
            _caption!.Visible = false;
        }

        // Decision section: rebuild is cheap (it already diffs by identity
        // via its own state) and rare (production-blocked events only).
        // Recreate the section per refresh; focus chain reacts via
        // RebuildFocusables below.
        if (_decisionsContainer is not null) _decisionsContainer.QueueFree();
        _decisionsContainer = null;
        _decisionsSeparator = null;
        RebuildDecisions(meaningful, content);

        int start = Math.Max(0, compacted.Count - MaximumRows);
        int visibleCount = compacted.Count - start;
        // Grow the persistent row pool to the count we need.
        while (_rowPool.Count < visibleCount)
        {
            var row = new ChronicleEventRow();
            content.AddChild(row);
            _rowPool.Add(row);
        }
        // Apply in place. Surplus rows are hidden so they don't take
        // layout space — the next tick can re-show them without an
        // allocation.
        for (int i = 0; i < _rowPool.Count; i++)
        {
            ChronicleEventRow row = _rowPool[i];
            if (i < visibleCount)
            {
                row.Apply(compacted[start + i]);
                row.Visible = true;
            }
            else
            {
                row.Visible = false;
            }
        }

        if (compacted.Count == 0)
        {
            _noActivityLabel!.Visible = true;
        }
        else
        {
            _noActivityLabel!.Visible = false;
        }

        RebuildFocusables();
    }

    private void BuildBodyOnce()
    {
        // Single VBoxContainer inside the body's scroll. Every scrollable
        // element (caption, decisions, rows) lives in this one VBox, so the
        // body scrolls as a whole however many rows or decisions accumulate.
        _contentContainer = new VBoxContainer
        {
            Name = "ChronicleContent",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            MouseFilter = MouseFilterEnum.Pass,
        };
        _contentContainer.AddThemeConstantOverride("separation", Tokens.SpacingTight);

        // Same gutter the city summary needs, for the same reason: once the
        // log is long enough to scroll, the bar is drawn over the right edge
        // of the content. Here it costs a wrap point rather than a digit —
        // the entries autowrap, so without it the last characters of a full
        // line sit under the bar. See Tokens.ScrollGutter.
        var gutter = new MarginContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            MouseFilter = MouseFilterEnum.Pass,
        };
        gutter.AddThemeConstantOverride("margin_right", Tokens.ScrollGutter);
        gutter.AddChild(_contentContainer);
        _body.AddChild(gutter);

        _caption = new Label
        {
            Name = "Caption",
            ThemeTypeVariation = "HudCaption",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
        };
        _contentContainer.AddChild(_caption);

        _noActivityLabel = new Label
        {
            Name = "NoActivity",
            Text = UiText.Get("ui.expedition_rail.no_activity"),
            ThemeTypeVariation = "HudCaption",
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
        };
        _contentContainer.AddChild(_noActivityLabel);
    }

    private void RebuildDecisions(
        IReadOnlyList<WorldEvent> meaningful, VBoxContainer content)
    {
        var decisionsContainer = new VBoxContainer
        {
            Name = "Decisions",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Pass,
        };
        decisionsContainer.AddThemeConstantOverride("separation", Tokens.SpacingTight);

        List<DecisionNeeded> decisions = GroupDecisionsNeeded(meaningful);
        if (decisions.Count == 0) return;

        decisionsContainer.AddChild(new HudSectionHeader(
            UiText.Get("ui.chronicle.needs_attention"),
            decisions.Count.ToString()));
        foreach (DecisionNeeded decision in decisions)
        {
            string label = $"{decision.Count}× {UiText.Get(decision.SubjectName)}";
            if (decision.TargetBuildingId is not BuildingId buildingId)
            {
                decisionsContainer.AddChild(Caption(label));
                continue;
            }

            var button = new IconButton
            {
                IconPath = IconPaths.Warning,
                ButtonText = UiText.Format("ui.chronicle.open", label),
                ShowLabel = true,
                TooltipText = UiText.Get("ui.chronicle.open_tooltip"),
                ThemeTypeVariation = "HudButton",
                FocusMode = FocusModeEnum.All,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            button.Pressed += () => _controller?.SelectBuilding(buildingId);
            decisionsContainer.AddChild(button);
            _decisionButtons.Add(button);
        }
        decisionsContainer.AddChild(new HSeparator
        {
            ThemeTypeVariation = "HudSeparator",
            MouseFilter = MouseFilterEnum.Ignore,
        });
        content.AddChild(decisionsContainer);
        _decisionsContainer = decisionsContainer;
    }

    private void RebuildFocusables()
    {
        _focusables.Clear();
        if (_header is null) return;
        if (_header.Expanded)
        {
            foreach (Control button in _decisionButtons)
            {
                _focusables.Add(button);
            }
        }
        FocusablesChanged?.Invoke();
    }

    private void OnChronicleScrollGuiInput(InputEvent inputEvent)
    {
        // The chronicle's own scroll container is the only wheel
        // target inside the chronicle — accept the event so the rail's
        // outer _Input handler doesn't also scroll the expedition
        // section when the player wheels over the chronicle rows,
        // then drive the VScrollBar ourselves because Godot's
        // ScrollContainer does not auto-scroll on wheel input.
        if (inputEvent is not InputEventMouseButton mouse || !mouse.Pressed
            || mouse.ButtonIndex is not MouseButton.WheelUp and not MouseButton.WheelDown)
        {
            return;
        }
        ScrollBy(mouse);
        AcceptEvent();
    }

    private void ScrollBy(InputEventMouseButton mouse)
    {
        VScrollBar bar = _body.GetVScrollBar();
        double direction = mouse.ButtonIndex == MouseButton.WheelUp ? -1d : 1d;
        bar.Value += direction * 40d * (mouse.Factor > 0f ? mouse.Factor : 1f);
    }

    private void BuildDecisions(IReadOnlyList<WorldEvent> events, VBoxContainer content)
    {
        List<DecisionNeeded> decisions = GroupDecisionsNeeded(events);
        if (decisions.Count == 0) return;

        content.AddChild(new HudSectionHeader(
            UiText.Get("ui.chronicle.needs_attention"),
            decisions.Count.ToString()));
        foreach (DecisionNeeded decision in decisions)
        {
            string label = $"{decision.Count}× {UiText.Get(decision.SubjectName)}";
            if (decision.TargetBuildingId is not BuildingId buildingId)
            {
                content.AddChild(Caption(label));
                continue;
            }

            var button = new IconButton
            {
                IconPath = IconPaths.Warning,
                ButtonText = UiText.Format("ui.chronicle.open", label),
                ShowLabel = true,
                TooltipText = UiText.Get("ui.chronicle.open_tooltip"),
                ThemeTypeVariation = "HudButton",
                FocusMode = FocusModeEnum.All,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            button.Pressed += () => _controller?.SelectBuilding(buildingId);
            content.AddChild(button);
            _decisionButtons.Add(button);
        }
        content.AddChild(new HSeparator
        {
            ThemeTypeVariation = "HudSeparator",
            MouseFilter = MouseFilterEnum.Ignore,
        });
    }

    private List<DecisionNeeded> GroupDecisionsNeeded(IReadOnlyList<WorldEvent> events)
    {
        var groups = new Dictionary<DecisionIdentity, DecisionAggregate>();
        foreach (WorldEvent evt in events)
        {
            if (evt.Kind != WorldEventKind.ProductionBlocked) continue;
            var identity = new DecisionIdentity(evt.Subject.Kind, evt.Subject.EntityId);
            groups.TryGetValue(identity, out DecisionAggregate aggregate);
            groups[identity] = new DecisionAggregate(
                aggregate.Count + 1,
                evt.Subject.DisplayName);
        }

        var output = new List<DecisionNeeded>(groups.Count);
        foreach ((DecisionIdentity identity, DecisionAggregate aggregate) in groups)
        {
            BuildingId? target = identity.Kind == WorldEventSubjectKind.Building
                && identity.EntityId is int entityId
                    ? new BuildingId(entityId)
                    : null;
            output.Add(new DecisionNeeded(
                aggregate.DisplayName,
                aggregate.Count,
                target));
        }
        return output;
    }

    private static Label Caption(string text, Color? color = null)
    {
        var label = new Label
        {
            Text = text,
            ThemeTypeVariation = "HudCaption",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        if (color is Color value) label.AddThemeColorOverride("font_color", value);
        return label;
    }

    private readonly record struct DecisionNeeded(
        string SubjectName,
        int Count,
        BuildingId? TargetBuildingId);

    private readonly record struct DecisionIdentity(
        WorldEventSubjectKind Kind,
        int? EntityId);

    private readonly record struct DecisionAggregate(int Count, string DisplayName);

    private sealed partial class ChronicleEventRow : HBoxContainer
    {
        private readonly TextureRect _icon;
        private readonly Label _body;
        private readonly Label _timestamp;

        /// <summary>
        /// Process-wide texture cache for the chronicle's per-row icon.
        /// The 27-event-kind enum maps to a closed set of icon paths that
        /// never changes; the cache keeps the per-row rebuild allocation
        /// at zero after the first tick.
        /// </summary>
        private static readonly Dictionary<string, Texture2D?> IconPathCache = new();

        private static Texture2D? LoadIconCached(string path)
        {
            if (IconPathCache.TryGetValue(path, out Texture2D? cached)) return cached;
            Texture2D? loaded = ResourceLoader.Load<Texture2D>(path);
            IconPathCache[path] = loaded;
            return loaded;
        }

        public ChronicleEventRow()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddThemeConstantOverride("separation", Tokens.SpacingTight);

            _icon = new TextureRect
            {
                CustomMinimumSize = new Vector2(IconSize, IconSize),
                StretchMode = TextureRect.StretchModeEnum.KeepCentered,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Ignore,
                Modulate = LineageThemeRegistry.IconAccent,
            };
            AddChild(_icon);

            var text = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            text.AddThemeConstantOverride("separation", 0);
            AddChild(text);
            _body = new Label
            {
                ThemeTypeVariation = "HudCaption",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            text.AddChild(_body);
            _timestamp = new Label
            {
                ThemeTypeVariation = "HudCaption",
                MouseFilter = MouseFilterEnum.Ignore,
                Modulate = new Color(1f, 1f, 1f, 0.72f),
            };
            text.AddChild(_timestamp);
        }

        /// <summary>
        /// Mutates the persistent row in place. Called from
        /// <see cref="ChroniclePanel.RebuildBody"/>'s diff so a row
        /// that survives a refresh keeps its node identity and never
        /// pays the SceneTree-rebuild cost.
        /// </summary>
        public void Apply(ChronicleEventProjection.Item item)
        {
            _icon.Texture = LoadIconCached(IconPathFor(item.Kind));
            _body.Text = WorldEventTextFormatter.FormatLocalized(
                item.Kind, item.SubjectName, item.Amount);
            _timestamp.Text = SimulationTimeText.FormatLocalized(item.LastTick);
        }

        private static string IconPathFor(WorldEventKind kind) => kind switch
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
            WorldEventKind.MigrantArrived => IconPaths.Users,
            WorldEventKind.FoodRationShortfall => IconPaths.Warning,
            WorldEventKind.ExpeditionDispatched => IconPaths.Backpack,
            WorldEventKind.ExpeditionReturned => IconPaths.Backpack,
            WorldEventKind.ExpeditionFailed => IconPaths.Warning,
            WorldEventKind.ExpeditionCancelled => IconPaths.Close,
            WorldEventKind.ExpeditionRetreated => IconPaths.ArrowLeft,
            WorldEventKind.ExpeditionEncounterResolved => IconPaths.Shield,
            WorldEventKind.WoundSustained => IconPaths.Warning,
            WorldEventKind.WoundRecoveryStarted => IconPaths.Heart,
            WorldEventKind.WoundRecoveryCompleted => IconPaths.Heart,
            WorldEventKind.TerritoryAdvanced => IconPaths.Expand,
            WorldEventKind.CropReady => IconPaths.Leaf,
            WorldEventKind.CropHarvested => IconPaths.Leaf,
            _ => IconPaths.Info,
        };
    }
}
