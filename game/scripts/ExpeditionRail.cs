#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>Persistent right-side summary of active expeditions and recent history.</summary>
[GlobalClass]
public partial class ExpeditionRail : PanelContainer
{
    public const int PanelWidth = 236;

    [Export] public NodePath ControllerPath { get; set; } = new("../../../CityWorldController");
    [Export] public NodePath ExpeditionPanelPath { get; set; } = new("../ExpeditionPanel");

    private CityWorldController _controller = null!;
    private ExpeditionPanel _expeditionPanel = null!;
    private LocaleManager _localeManager = null!;
    private VBoxContainer _content = null!;
    private VBoxContainer _expeditionContent = null!;
    private ChroniclePanel _chronicle = null!;
    private ScrollContainer _scroll = null!;
    private readonly List<Control> _focusables = new();
    private ExpeditionRailSnapshot _snapshot = null!;
    private int _pendingFocusIndex = -1;
    private bool _rebuilding;
    private bool _visualRegressionFixtureActive;
    public IconButton? FirstDetailsButton { get; private set; }
    public IconButton? FirstCancelButton { get; private set; }
    public IconButton MoreButton => _chronicle.ToggleButton;
    public bool ChronicleExpanded => _chronicle.Expanded;
    public ExpeditionId? FirstExpeditionId { get; private set; }

    public override void _Ready()
    {
        OverlayLayers.Apply(this, OverlayLayers.Hud);
        MouseFilter = MouseFilterEnum.Stop;
        _controller = GetNode<CityWorldController>(ControllerPath);
        _expeditionPanel = GetNode<ExpeditionPanel>(ExpeditionPanelPath);
        _localeManager = GetNode<LocaleManager>("/root/LocaleManager");

        _scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            MouseFilter = MouseFilterEnum.Stop,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _scroll.GuiInput += OnScrollGuiInput;
        AddChild(_scroll);

        _content = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            MouseFilter = MouseFilterEnum.Pass,
        };
        _content.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        _scroll.AddChild(_content);

        _expeditionContent = new VBoxContainer
        {
            Name = "ExpeditionSummary",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Pass,
        };
        _expeditionContent.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        _content.AddChild(_expeditionContent);

        _chronicle = new ChroniclePanel();
        _chronicle.SetController(_controller);
        _chronicle.ExpandedChanged += OnChronicleExpanded;
        _chronicle.FocusablesChanged += OnChronicleFocusablesChanged;
        _chronicle.ScrollToNewestRequested += RequestScrollToNewest;
        _chronicle.ScrollToStartRequested += RequestScrollToStart;
        _content.AddChild(_chronicle);

        _controller.WorldTickAdvanced += OnWorldTickAdvanced;
        _controller.ExpeditionStateChanged += OnExpeditionStateChanged;
        _controller.BuildingStateChanged += OnStateChanged;
        _controller.ProjectStateChanged += OnStateChanged;
        _controller.NaturalResourceStateChanged += OnStateChanged;
        _controller.CultivationSiteStateChanged += OnStateChanged;
        _controller.FirstNightStageChanged += OnStateChanged;
        _controller.HeroCreated += OnStateChanged;
        _controller.CitizensChanged += OnCitizensChanged;
        _localeManager.LocaleChanged += OnLocaleChanged;
        Refresh();
        if (_controller.LastOfflineReport is { HadProgression: true } report)
        {
            ShowOfflineReport(report);
        }
    }

    public override void _ExitTree()
    {
        if (_controller is not null)
        {
            _controller.WorldTickAdvanced -= OnWorldTickAdvanced;
            _controller.ExpeditionStateChanged -= OnExpeditionStateChanged;
            _controller.BuildingStateChanged -= OnStateChanged;
            _controller.ProjectStateChanged -= OnStateChanged;
            _controller.NaturalResourceStateChanged -= OnStateChanged;
            _controller.CultivationSiteStateChanged -= OnStateChanged;
            _controller.FirstNightStageChanged -= OnStateChanged;
            _controller.HeroCreated -= OnStateChanged;
            _controller.CitizensChanged -= OnCitizensChanged;
        }
        if (_scroll is not null) _scroll.GuiInput -= OnScrollGuiInput;
        if (_localeManager is not null) _localeManager.LocaleChanged -= OnLocaleChanged;
        if (_chronicle is not null)
        {
            _chronicle.ExpandedChanged -= OnChronicleExpanded;
            _chronicle.FocusablesChanged -= OnChronicleFocusablesChanged;
            _chronicle.ScrollToNewestRequested -= RequestScrollToNewest;
            _chronicle.ScrollToStartRequested -= RequestScrollToStart;
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!IsVisibleInTree()
            || inputEvent is not InputEventMouseButton mouse
            || !mouse.Pressed
            || mouse.ButtonIndex is not MouseButton.WheelUp and not MouseButton.WheelDown
            || !GetGlobalRect().HasPoint(mouse.GlobalPosition))
        {
            return;
        }
        ScrollBy(mouse);
        GetViewport().SetInputAsHandled();
    }

    public void Refresh()
    {
        _snapshot = _controller.GetExpeditionRailSnapshot();
        Rebuild();
    }

    public void ShowOfflineReport(OfflineProgressionReport report)
    {
        _chronicle.ShowOfflineReport(report);
        Show();
    }

    internal void ShowVisualRegressionReport(OfflineProgressionReport report)
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        _visualRegressionFixtureActive = true;
        ShowOfflineReport(report);
    }

    private void OnWorldTickAdvanced(int _) => RequestRefresh();
    private void OnExpeditionStateChanged(int _) => RequestRefresh();
    private void OnStateChanged(int _) => RequestRefresh();
    private void OnLocaleChanged(string _) => RequestRefresh();
    private void OnCitizensChanged() => RequestRefresh();

    private bool _refreshQueued;

    private void RequestRefresh()
    {
        if (_visualRegressionFixtureActive) return;
        if (_refreshQueued) return;
        _refreshQueued = true;
        CallDeferred(MethodName.ApplyQueuedRefresh);
    }

    private void ApplyQueuedRefresh()
    {
        _refreshQueued = false;
        if (_visualRegressionFixtureActive) return;
        Refresh();
    }

    private void Rebuild()
    {
        _rebuilding = true;
        Control? focusOwner = GetViewport().GuiGetFocusOwner();
        int focusedIndex = focusOwner is null ? -1 : _focusables.IndexOf(focusOwner);
        if (focusedIndex >= 0) _pendingFocusIndex = focusedIndex;
        foreach (Node child in _expeditionContent.GetChildren())
        {
            _expeditionContent.RemoveChild(child);
            child.QueueFree();
        }
        FirstDetailsButton = null;
        FirstCancelButton = null;
        FirstExpeditionId = null;

        _expeditionContent.AddChild(new HudSectionHeader(UiText.Get("ui.expedition_rail.title")));
        _expeditionContent.AddChild(new HudSectionHeader(
            UiText.Get("ui.expedition_rail.active"),
            _snapshot.ActiveExpeditions.Count.ToString()));

        if (_snapshot.ActiveExpeditions.Count == 0)
        {
            _expeditionContent.AddChild(Caption(UiText.Get("ui.expedition_rail.none_active")));
        }
        else
        {
            foreach (ExpeditionRailSnapshot.Item item in _snapshot.ActiveExpeditions)
            {
                var card = new ExpeditionCompactCard(item, _snapshot.CurrentTick);
                card.DetailsRequested += OpenDetails;
                card.CancelRequested += CancelExpedition;
                _expeditionContent.AddChild(card);
                FirstDetailsButton ??= card.DetailsButton;
                FirstExpeditionId ??= item.Id;
                if (card.CancelButton is not null)
                {
                    FirstCancelButton ??= card.CancelButton;
                }
            }
        }

        _expeditionContent.AddChild(new HSeparator
        {
            ThemeTypeVariation = "HudSeparator",
            MouseFilter = MouseFilterEnum.Ignore,
        });
        _chronicle.RefreshLive(_snapshot.Events);
        _expeditionContent.Visible = !_chronicle.Expanded;
        _rebuilding = false;
        RebuildFocusables();
    }

    public void GrabDefaultFocus() => (FirstDetailsButton ?? MoreButton).GrabFocus();

    private static Label Caption(string text) => new()
    {
        Text = text,
        ThemeTypeVariation = "HudCaption",
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    private void OpenDetails(ExpeditionId id) => _expeditionPanel.Open(id);

    private void CancelExpedition(ExpeditionId id)
    {
        _controller.CancelExpedition(id);
        Refresh();
    }

    private void OnChronicleExpanded(bool expanded)
    {
        _expeditionContent.Visible = !expanded;
        RebuildFocusables();
        if (expanded) RequestScrollToNewest();
    }

    private void OnChronicleFocusablesChanged()
    {
        if (!_rebuilding) RebuildFocusables();
    }

    private void RebuildFocusables()
    {
        _focusables.Clear();
        if (!_chronicle.Expanded)
        {
            if (FirstDetailsButton is not null) _focusables.Add(FirstDetailsButton);
            if (FirstCancelButton is not null) _focusables.Add(FirstCancelButton);
        }
        foreach (Control control in _chronicle.Focusables) _focusables.Add(control);
        CallDeferred(MethodName.WireFocus);
        if (_pendingFocusIndex >= 0) CallDeferred(MethodName.RestorePendingFocus);
    }

    private void RequestScrollToNewest() => CallDeferred(MethodName.ScrollToNewest);

    private void RequestScrollToStart() => CallDeferred(MethodName.ScrollToStart);

    private void ScrollToNewest()
    {
        VScrollBar bar = _scroll.GetVScrollBar();
        bar.Value = bar.MaxValue;
    }

    private void ScrollToStart() => _scroll.GetVScrollBar().Value = 0d;

    private void WireFocus()
    {
        if (_focusables.Count == 0) return;
        for (int i = 0; i < _focusables.Count; i++)
        {
            Control current = _focusables[i];
            Control previous = _focusables[(i - 1 + _focusables.Count) % _focusables.Count];
            Control next = _focusables[(i + 1) % _focusables.Count];
            current.FocusNeighborTop = previous.GetPath();
            current.FocusNeighborBottom = next.GetPath();
            current.FocusPrevious = previous.GetPath();
            current.FocusNext = next.GetPath();
        }
    }

    private void RestorePendingFocus()
    {
        if (_pendingFocusIndex < 0 || _focusables.Count == 0) return;
        _focusables[Math.Min(_pendingFocusIndex, _focusables.Count - 1)].GrabFocus();
        _pendingFocusIndex = -1;
    }

    private void OnScrollGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton mouse || !mouse.Pressed
            || mouse.ButtonIndex is not MouseButton.WheelUp and not MouseButton.WheelDown)
        {
            return;
        }
        ScrollBy(mouse);
        _scroll.AcceptEvent();
    }

    private void ScrollBy(InputEventMouseButton mouse)
    {
        VScrollBar bar = _scroll.GetVScrollBar();
        double direction = mouse.ButtonIndex == MouseButton.WheelUp ? -1d : 1d;
        bar.Value += direction * 40d * (mouse.Factor > 0f ? mouse.Factor : 1f);
    }
}
