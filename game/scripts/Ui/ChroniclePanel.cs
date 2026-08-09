#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Presentation;

namespace WorldofGoses.Ui;

/// <summary>
/// The single Chronicle presentation used by the macro HUD. Compact mode shows
/// the newest meaningful events; expanded mode keeps the complete causal log,
/// offline summary and actionable production blockers in the same right rail.
/// </summary>
[GlobalClass]
public partial class ChroniclePanel : VBoxContainer
{
    private const int CompactRows = 4;
    private const int MaximumRows = 80;
    private const int IconSize = 16;

    private readonly List<Control> _focusables = new();
    private IReadOnlyList<WorldEvent> _events = Array.Empty<WorldEvent>();
    private CityWorldController? _controller;
    private string _offlineSummary = string.Empty;
    private bool _expanded;

    public event Action<bool>? ExpandedChanged;
    public event Action? FocusablesChanged;
    public event Action? ScrollToNewestRequested;
    public event Action? ScrollToStartRequested;

    public IReadOnlyList<Control> Focusables => _focusables;
    public IconButton ToggleButton { get; private set; } = null!;

    public ChroniclePanel()
    {
        Name = "ChroniclePanel";
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ShrinkBegin;
        MouseFilter = MouseFilterEnum.Pass;
        AddThemeConstantOverride("separation", Tokens.SpacingTight);
    }

    public bool Expanded
    {
        get => _expanded;
        set
        {
            if (_expanded == value) return;
            _expanded = value;
            Rebuild();
            ExpandedChanged?.Invoke(_expanded);
        }
    }

    public void SetController(CityWorldController controller) => _controller = controller;

    public void RefreshLive(IReadOnlyList<WorldEvent> events)
    {
        _events = events;
        Rebuild();
    }

    public void ShowOfflineReport(OfflineProgressionReport report)
    {
        if (!report.HadProgression) return;
        _events = report.Events;
        _offlineSummary = UiText.Format(
            "ui.chronicle.welcome",
            SimulationTimeText.FormatDurationLocalized(report.TicksApplied));
        _expanded = true;
        Rebuild();
        ExpandedChanged?.Invoke(true);
        ScrollToStartRequested?.Invoke();
    }

    public void ClearOfflineSummary()
    {
        if (string.IsNullOrEmpty(_offlineSummary)) return;
        _offlineSummary = string.Empty;
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        _focusables.Clear();

        IReadOnlyList<WorldEvent> meaningful =
            ChronicleEventProjection.MeaningfulEvents(_events);
        IReadOnlyList<ChronicleEventProjection.Item> compacted =
            ChronicleEventProjection.Compact(meaningful);

        AddChild(new HudSectionHeader(
            UiText.Get("ui.expedition_rail.activity"),
            compacted.Count.ToString()));

        if (_expanded && !string.IsNullOrWhiteSpace(_offlineSummary))
        {
            AddChild(Caption(_offlineSummary, LineageThemeRegistry.IconAccent));
        }

        if (_expanded)
        {
            BuildDecisions(meaningful);
        }

        int maximum = _expanded ? MaximumRows : CompactRows;
        int start = Math.Max(0, compacted.Count - maximum);
        if (compacted.Count == 0)
        {
            AddChild(Caption(UiText.Get("ui.expedition_rail.no_activity")));
        }
        else
        {
            for (int i = start; i < compacted.Count; i++)
            {
                AddChild(new ChronicleEventRow(compacted[i]));
            }
        }

        ToggleButton = new IconButton
        {
            IconPath = _expanded ? IconPaths.ChevronUp : IconPaths.Expand,
            ButtonText = _expanded
                ? UiText.Get("ui.chronicle.collapse")
                : UiText.Get("ui.expedition_rail.more"),
            ShowLabel = true,
            ThemeTypeVariation = "HudButton",
            TooltipText = _expanded
                ? UiText.Get("ui.chronicle.collapse_tooltip")
                : UiText.Get("ui.expedition_rail.more_tooltip"),
            FocusMode = FocusModeEnum.All,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        ToggleButton.Pressed += ToggleExpanded;
        AddChild(ToggleButton);
        _focusables.Add(ToggleButton);
        FocusablesChanged?.Invoke();
    }

    private void ToggleExpanded()
    {
        Expanded = !Expanded;
        ToggleButton.GrabFocus();
        if (_expanded) ScrollToNewestRequested?.Invoke();
    }

    private void BuildDecisions(IReadOnlyList<WorldEvent> events)
    {
        List<DecisionNeeded> decisions = GroupDecisionsNeeded(events);
        if (decisions.Count == 0) return;

        AddChild(new HudSectionHeader(
            UiText.Get("ui.chronicle.needs_attention"),
            decisions.Count.ToString()));
        foreach (DecisionNeeded decision in decisions)
        {
            string label = $"{decision.Count}× {UiText.Get(decision.SubjectName)}";
            if (decision.TargetBuildingId is not BuildingId buildingId)
            {
                AddChild(Caption(label));
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
            AddChild(button);
            _focusables.Add(button);
        }
        AddChild(new HSeparator
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
