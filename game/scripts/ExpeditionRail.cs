#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Presentation;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>Persistent right-side summary of active expeditions and recent history.</summary>
[GlobalClass]
public partial class ExpeditionRail : PanelContainer
{
    public const int PanelWidth = 236;

    [Export] public NodePath ControllerPath { get; set; } = new("../../../CityWorldController");
    [Export] public NodePath ExpeditionPanelPath { get; set; } = new("../Center/ExpeditionPanel");

    private CityWorldController _controller = null!;
    private ExpeditionPanel _expeditionPanel = null!;
    private LocaleManager _localeManager = null!;
    private CollapsiblePanelHeader _header = null!;
    private VBoxContainer _layout = null!;
    private VBoxContainer _expeditionSection = null!;
    private ScrollContainer _scroll = null!;
    private VBoxContainer _content = null!;
    private VBoxContainer _expeditionContent = null!;
    private ChroniclePanel _chronicle = null!;
    private readonly List<Control> _focusables = new();
    private ExpeditionRailSnapshot _snapshot = null!;
    private int _pendingFocusIndex = -1;
    private bool _rebuilding;
    private bool _visualRegressionFixtureActive;
    private IconButton? _quickViewButton;
    private ExpeditionId? _quickViewExpeditionId;
    public IconButton? FirstDetailsButton { get; private set; }
    public IconButton? FirstViewButton { get; private set; }
    public IconButton? FirstCancelButton { get; private set; }
    public Button MoreButton => _chronicle.Header;
    internal Button HeaderForVisualRegression => _header;
    public bool ChronicleExpanded => _chronicle.Expanded;
    public ExpeditionId? FirstExpeditionId { get; private set; }

    /// <summary>
    /// Whether the rail body is unfolded. Folding the rail also folds
    /// the chronicle — its body disappears too, so the rail falls back
    /// to a slim resume (rail header + chronicle header, two chevron
    /// toggles, no rows). The chronicle's own collapse lives on its
    /// own header, independent of this flag, so a fully-opened rail can
    /// still have its chronicle folded like the city summary.
    /// </summary>
    public bool Expanded => _header is null || _header.Expanded;

    public override void _Ready()
    {
        OverlayLayers.Apply(this, OverlayLayers.Hud);
        MouseFilter = MouseFilterEnum.Stop;
        // ExpandFill vertical so the rail always claims the full
        // vertical space between the status bar and the dock. Without
        // it the PanelContainer collapses to the combined-minimum of
        // its children (ShrinkBegin default) — when the chronicle body
        // hides, that minimum shrinks to header-only and the rail
        // panel itself drops to 80 px, hiding the expedition scroll
        // inside an empty rail.
        SizeFlagsVertical = SizeFlags.ExpandFill;
        _controller = GetNode<CityWorldController>(ControllerPath);
        _expeditionPanel = GetNode<ExpeditionPanel>(ExpeditionPanelPath);
        _localeManager = GetNode<LocaleManager>("/root/LocaleManager");

        // The rail body wraps the expedition section and the chronicle
        // under a single rail-level header. Both the expedition cards
        // and the chronicle's rows live below it; the chronicle has
        // its own collapsible header so a player can fold it the same
        // way they fold the city summary, while the rail-level header
        // folds the whole body together — including the chronicle's
        // body, so a folded rail is just two stacked headers.
        var layout = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Pass,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        layout.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        _layout = layout;
        AddChild(_layout);

        _header = new CollapsiblePanelHeader(UiText.Get("ui.expedition_rail.title"));
        _layout.AddChild(_header);

        _expeditionSection = new VBoxContainer
        {
            Name = "ExpeditionSection",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Pass,
        };
        _expeditionSection.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        _layout.AddChild(_expeditionSection);
        _header.ExpandedChanged += OnHeaderExpandedChanged;

        _scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            MouseFilter = MouseFilterEnum.Stop,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _scroll.GuiInput += OnScrollGuiInput;
        _layout.AddChild(_scroll);

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

        // Chronicle sits as a direct child of the layout, below the
        // rail-level scroll, so its own bounded rows scroll stays
        // independent of the expedition section's overflow. Its own
        // collapsible header governs the body exactly the way
        // CitySummaryPanel governs its body — the player can fold the
        // chronicle even when the rail is open.
        _chronicle = new ChroniclePanel();
        _chronicle.SetController(_controller);
        _chronicle.ExpandedChanged += OnChronicleExpanded;
        _chronicle.FocusablesChanged += OnChronicleFocusablesChanged;
        _layout.AddChild(_chronicle);

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
        // The chronicle sits below the rail-level scroll and owns its
        // own bounded rows scroll. Letting the rail intercept the wheel
        // here would scroll the wrong surface — the expedition cards
        // instead of the chronicle the pointer is actually over.
        if (_chronicle is not null
            && _chronicle.Visible
            && _chronicle.GetGlobalRect().HasPoint(mouse.GlobalPosition))
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

    /// <summary>
    /// Forces the rail collapsed or expanded for visual-regression
    /// fixtures. Not part of the public contract; the player toggles
    /// the rail through the header itself.
    /// </summary>
    internal void SetExpandedForVisualRegression(bool expanded)
    {
        if (_header is null) return;
        _header.Expanded = expanded;
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
        if (_quickViewButton is not null)
        {
            _quickViewButton.Pressed -= OnQuickViewPressed;
            if (_quickViewButton.GetParent() == _expeditionSection)
            {
                _expeditionSection.RemoveChild(_quickViewButton);
            }
            _quickViewButton.QueueFree();
            _quickViewButton = null;
            _quickViewExpeditionId = null;
        }
        FirstDetailsButton = null;
        FirstViewButton = null;
        FirstCancelButton = null;
        FirstExpeditionId = null;

        // Header trailing count: only active expeditions. The chronicle
        // has its own count badge in its own collapsible header — the
        // rail toggler must never report chronicle events under its
        // own label, otherwise the two badges bleed into each other.
        int headerCount = _snapshot.ActiveExpeditions.Count;
        _header.Text = UiText.Format(
            "ui.expedition_rail.header",
            headerCount.ToString());

        _expeditionContent.AddChild(new HudSectionHeader(
            UiText.Get("ui.expedition_rail.active"),
            _snapshot.ActiveExpeditions.Count.ToString()));

        if (_snapshot.ActiveExpeditions.Count == 0)
        {
            _expeditionContent.AddChild(Caption(UiText.Get("ui.expedition_rail.none_active")));
        }
        else
        {
            ExpeditionRailSnapshot.Item first = _snapshot.ActiveExpeditions[0];
            _quickViewExpeditionId = first.Id;
            _quickViewButton = new IconButton
            {
                IconPath = IconPaths.Expand,
                ButtonText = UiText.Get("ui.expedition_rail.view"),
                ShowLabel = true,
                ThemeTypeVariation = "HudButtonSelected",
                TooltipText = UiText.Get("ui.expedition_rail.view_tooltip"),
                FocusMode = FocusModeEnum.All,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _quickViewButton.Pressed += OnQuickViewPressed;
            _expeditionSection.AddChild(_quickViewButton);
            FirstViewButton = _quickViewButton;

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

        _chronicle.RefreshLive(_snapshot.Events);
        _rebuilding = false;
        RebuildFocusables();
    }

    public void GrabDefaultFocus() =>
        (FirstViewButton ?? FirstDetailsButton ?? MoreButton).GrabFocus();

    private static Label Caption(string text) => new()
    {
        Text = text,
        ThemeTypeVariation = "HudCaption",
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    private void OpenDetails(ExpeditionId id) => _expeditionPanel.Open(id);

    private void OpenLiveView(ExpeditionId id) => _controller.SelectExpeditionLive(id);

    private void OnQuickViewPressed()
    {
        if (_quickViewExpeditionId is ExpeditionId id) OpenLiveView(id);
    }

    private void CancelExpedition(ExpeditionId id)
    {
        _controller.CancelExpedition(id);
        Refresh();
    }

    /// <summary>
    /// The rail-level header governs the expedition section: clicking
    /// it expands the expedition scroll. The accordion rule keeps the
    /// rail and the chronicle mutually exclusive — when the expedition
    /// becomes the protagonist, the chronicle body folds out and the
    /// two surfaces never compete for the same column at the same
    /// time.
    /// </summary>
    private void OnHeaderExpandedChanged(bool expanded)
    {
        if (expanded)
        {
            // Accordion: when expedition expands, chronicle folds.
            _chronicle.Expanded = false;
        }
        _expeditionSection.Visible = expanded;
        _scroll.Visible = expanded;
        // The Container caches its children's minimum sizes; the next
        // frame's QueueSort re-measures so the body actually collapses
        // to header-only instead of staying at its expanded rect.
        if (IsInsideTree()) CallDeferred(MethodName.RequestRailRelayout);
        RebuildFocusables();
    }

    /// <summary>
    /// The chronicle header is the other half of the accordion. When
    /// it expands, the expedition section folds out so the chronicle
    /// takes the whole rail column — no overlap, no fighting for
    /// pixels, no need to re-distribute a vertical layout that was
    /// trying to fit both at once.
    /// </summary>
    private void OnChronicleExpanded(bool expanded)
    {
        if (expanded)
        {
            // Accordion: when chronicle expands, expedition folds.
            // Also collapse the rail header so its chevron matches
            // the now-hidden scroll.
            _expeditionSection.Visible = false;
            _scroll.Visible = false;
            _header.Expanded = false;
        }
        else if (!_header.Expanded)
        {
            // Symmetric accordion transition: closing Chronicle restores the
            // expedition section instead of leaving both protagonists folded.
            _header.Expanded = true;
        }
        RebuildFocusables();
        if (expanded) _chronicle.ScrollToNewest();
        if (IsInsideTree()) CallDeferred(MethodName.RequestRailRelayout);
    }

    private void RequestRailRelayout()
    {
        if (_layout is null || !IsInsideTree()) return;
        // Invalidate cached minimum sizes so the layout does not hand
        // children back their pre-toggle rect. QueueSort then re-sorts
        // and ResetSize forces each Container to re-ask its parent for
        // a rect based on the new minimum.
        _layout.UpdateMinimumSize();
        if (_chronicle is not null) _chronicle.UpdateMinimumSize();
        _layout.QueueSort();
        if (_chronicle is not null) _chronicle.QueueSort();
        ResetSize();
        if (_scroll is not null) _scroll.ResetSize();
        if (_chronicle is not null) _chronicle.ResetSize();
    }

    private void OnChronicleFocusablesChanged()
    {
        if (!_rebuilding) RebuildFocusables();
    }

    private void RebuildFocusables()
    {
        _focusables.Clear();
        if (Expanded && FirstViewButton is not null) _focusables.Add(FirstViewButton);
        if (Expanded && FirstDetailsButton is not null) _focusables.Add(FirstDetailsButton);
        if (Expanded && FirstCancelButton is not null) _focusables.Add(FirstCancelButton);
        foreach (Control control in _chronicle.Focusables) _focusables.Add(control);
        CallDeferred(MethodName.WireFocus);
        if (_pendingFocusIndex >= 0) CallDeferred(MethodName.RestorePendingFocus);
    }

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
