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
    /// Hard ceiling on the chronicle's own body height so a long event
    /// history cannot push the rest of the HUD off-screen. The body
    /// holds every scrollable child (caption, decisions, rows) inside
    /// this single cap, so the chronicle's overall height stays fixed
    /// regardless of how many meaningful events accumulate.
    /// </summary>
    private const int MaxHeight = 360;

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

    public ChroniclePanel()
    {
        Name = "ChroniclePanel";
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        // Start collapsed: just the header counts toward the layout;
        // the rail expedition section is the initial protagonist and
        // gets the full rail column. SizeFlagsVertical flips to
        // ExpandFill only when the chronicle becomes the protagonist,
        // so it never competes with the expedition scroll for height.
        SizeFlagsVertical = SizeFlags.ShrinkBegin;
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
        AddChild(_header);

        // One ScrollContainer wraps everything below the header. The
        // single-scroll design (caption + decisions + rows together)
        // matches CitySummaryPanel's body and bounds the chronicle's
        // own height. The chronicle fills the available rail column
        // when it is the accordion protagonist, so when the player
        // deploys the chronicle the body uses the full height of the
        // rail instead of a fixed MaxHeight strip.
        _body = new ScrollContainer
        {
            Name = BodyScrollName,
            // Match the header's initial collapsed state. Otherwise this
            // 560 px body still occupies the shared rail while its chevron
            // says it is folded, squeezing the expedition list out.
            Visible = false,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            MouseFilter = MouseFilterEnum.Stop,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _body.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        _body.CustomMinimumSize = new Vector2(0, MaxHeight);
        _body.OffsetBottom = MaxHeight;
        _body.GuiInput += OnChronicleScrollGuiInput;
        AddChild(_body);
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
        _body.Visible = expanded;
        // Flip the vertical size flag with the body. ExpandFill when
        // the chronicle is the accordion protagonist so the body
        // owns the full rail column; ShrinkBegin when the body hides
        // so the chronicle collapses to header-only and gives the
        // expedition scroll every remaining pixel.
        SizeFlagsVertical = expanded
            ? SizeFlags.ExpandFill
            : SizeFlags.ShrinkBegin;
        // The body becoming invisible changes the chronicle's minimum
        // size; the parent VBoxContainer needs to re-measure on the
        // next frame or the chronicle keeps its old (expanded) height
        // and the body remains on screen even though Visible = false.
        CallDeferred(MethodName.NotifyChronicleCollapseLayout);
        ExpandedChanged?.Invoke(expanded);
        if (expanded) ScrollToNewest();
        // The decision buttons are reachable only when the body is
        // unfolded; rebuild the focusable list so the rail's focus
        // chain does not contain buttons that are not on screen.
        RebuildFocusables();
    }

    /// <summary>
    /// Deferred hook fired right after the body toggles its visibility.
    /// The chronicle asks its parent for a re-measure so it collapses
    /// to header-only the moment the body becomes invisible, matching
    /// <see cref="CitySummaryPanel"/>'s fold behaviour.
    /// </summary>
    private void NotifyChronicleCollapseLayout()
    {
        if (IsInsideTree()) ResetSize();
    }

    private void RebuildBody()
    {
        foreach (Node child in _body.GetChildren())
        {
            _body.RemoveChild(child);
            child.QueueFree();
        }
        _decisionButtons.Clear();

        IReadOnlyList<WorldEvent> meaningful =
            ChronicleEventProjection.MeaningfulEvents(_events);
        IReadOnlyList<ChronicleEventProjection.Item> compacted =
            ChronicleEventProjection.Compact(meaningful);

        // The chronicle count rides on the header so it stays visible
        // even when the body is folded — same affordance the rail's
        // own header uses.
        _header.Text = $"{UiText.Get("ui.expedition_rail.activity")} · {compacted.Count}";

        // Single VBoxContainer inside the body's scroll. Every
        // scrollable element (caption, decisions, rows) lives in this
        // one VBox so the body's anchor cap holds the chronicle at
        // exactly MaxHeight regardless of how many rows or decisions
        // accumulate.
        var content = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            MouseFilter = MouseFilterEnum.Pass,
        };
        content.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        _body.AddChild(content);

        if (!string.IsNullOrWhiteSpace(_offlineSummary))
        {
            content.AddChild(Caption(_offlineSummary, LineageThemeRegistry.IconAccent));
        }

        BuildDecisions(meaningful, content);

        int start = Math.Max(0, compacted.Count - MaximumRows);
        if (compacted.Count == 0)
        {
            content.AddChild(Caption(UiText.Get("ui.expedition_rail.no_activity")));
        }
        else
        {
            for (int i = start; i < compacted.Count; i++)
            {
                content.AddChild(new ChronicleEventRow(compacted[i]));
            }
        }

        RebuildFocusables();
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
        public ChronicleEventRow(ChronicleEventProjection.Item item)
        {
            MouseFilter = MouseFilterEnum.Ignore;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddThemeConstantOverride("separation", Tokens.SpacingTight);

            var icon = new TextureRect
            {
                Texture = ResourceLoader.Load<Texture2D>(IconPathFor(item.Kind)),
                CustomMinimumSize = new Vector2(IconSize, IconSize),
                StretchMode = TextureRect.StretchModeEnum.KeepCentered,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Ignore,
                Modulate = LineageThemeRegistry.IconAccent,
            };
            AddChild(icon);

            var text = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            text.AddThemeConstantOverride("separation", 0);
            AddChild(text);
            text.AddChild(new Label
            {
                Text = WorldEventTextFormatter.FormatLocalized(
                    item.Kind, item.SubjectName, item.Amount),
                ThemeTypeVariation = "HudCaption",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore,
            });
            text.AddChild(new Label
            {
                Text = SimulationTimeText.FormatLocalized(item.LastTick),
                ThemeTypeVariation = "HudCaption",
                MouseFilter = MouseFilterEnum.Ignore,
                Modulate = new Color(1f, 1f, 1f, 0.72f),
            });
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
