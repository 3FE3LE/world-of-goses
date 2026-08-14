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

    [Export] public NodePath ControllerPath { get; set; } = new("../../../../CityWorldController");
    [Export] public NodePath ExpeditionPanelPath { get; set; } = new("../../Center/ExpeditionPanel");

    private CityWorldController _controller = null!;
    private ExpeditionPanel _expeditionPanel = null!;
    private LocaleManager _localeManager = null!;
    private CollapsiblePanelHeader _header = null!;
    private VBoxContainer _layout = null!;
    private VBoxContainer _expeditionSection = null!;
    private ScrollContainer _scroll = null!;
    private AccordionHost _bodyHost = null!;
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

    /// <summary>
    /// Global rect of the expedition body, for fixtures that must prove the
    /// card list actually occupies space.
    /// </summary>
    /// <remarks>
    /// Visibility is not enough and never was. The defect this rail was
    /// restructured to remove left the cards with <c>Visible == true</c> while
    /// their host was squeezed to a zero-height rect — alive, laid out
    /// nowhere, drawn not at all. A fixture asserting
    /// <c>IsVisibleInTree()</c> passes straight through that state, which is
    /// why the accordion round trip reported green for a rail the player
    /// could see was empty. Height is the only witness that distinguishes
    /// them.
    /// </remarks>
    internal Rect2 ExpeditionBodyRectForVisualRegression => _scroll.GetGlobalRect();
    public bool ChronicleExpanded => _chronicle.Expanded;
    public ExpeditionId? FirstExpeditionId { get; private set; }

    /// <summary>
    /// Whether the expedition section is the one on screen. Read from the
    /// accordion host rather than from the header, because the host is the
    /// authority and the header is a projection of it — see
    /// <see cref="ShowSection"/>.
    /// </summary>
    public bool Expanded => _bodyHost is null || _bodyHost.IsShowing(_scroll);

    /// <summary>Whether every section is folded and the rail is just its headers.</summary>
    public bool AllSectionsCollapsed => _bodyHost is not null && _bodyHost.CurrentBody is null;

    public override void _Ready()
    {
        OverlayLayers.Apply(this, OverlayLayers.Hud);
        MouseFilter = MouseFilterEnum.Stop;
        // The rail sits inside a SidePanelHost that spans the shared vertical
        // envelope, and its own vertical size flag decides how much of it to
        // take: its headers when everything is folded (GitHub #15's "libere
        // mapa al colapsar"), the whole envelope when a section is open, with
        // the open body scrolling inside it (GitHub #17). No body carries a
        // fixed height any more — that made the content the accidental
        // authority on the outer size, so this rail's two headers and the
        // summary's one produced different outer heights from the same number.
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
        //
        // That whole shape is identical for every city, so it is
        // authored in ExpeditionRail.tscn (GitHub #9). What stays here
        // is what a scene cannot state: which bodies the accordion
        // swaps between, and the fact that the chronicle's two halves
        // are built by ChroniclePanel and adopted from it.
        _layout = GetNode<VBoxContainer>("Layout");
        _header = GetNode<CollapsiblePanelHeader>("Layout/Header");
        _expeditionSection = GetNode<VBoxContainer>("Layout/ExpeditionQuickActions");
        _bodyHost = GetNode<AccordionHost>("Layout/BodyHost");
        _scroll = GetNode<ScrollContainer>("Layout/BodyHost/ExpeditionScroll");
        _content = GetNode<VBoxContainer>("Layout/BodyHost/ExpeditionScroll/Content");
        _expeditionContent = GetNode<VBoxContainer>(
            "Layout/BodyHost/ExpeditionScroll/Content/ExpeditionSummary");
        _chronicle = GetNode<ChroniclePanel>("Layout/Chronicle");

        _header.ExpandedChanged += OnHeaderExpandedChanged;
        _chronicle.SetController(_controller);
        _chronicle.ExpandedChanged += OnChronicleExpanded;
        _chronicle.FocusablesChanged += OnChronicleFocusablesChanged;
        _scroll.GuiInput += OnScrollGuiInput;

        // The chronicle's header is adopted into the body host and then
        // moved to the front of it. Both headers stay on screen at all
        // times while only the bodies beneath them swap, so the header
        // has to precede whichever body is the current protagonist —
        // and a node reparented at runtime lands at the end. The scene
        // cannot author this child because ChroniclePanel builds it.
        _bodyHost.AddChild(_chronicle.Header);
        _bodyHost.MoveChild(_chronicle.Header, 0);

        // One host, one ExpandFill, one visible body. Registering both
        // bodies here is what removes the old two-claimant negotiation: the
        // expedition list and the chronicle can no longer starve each other,
        // because only one of them is ever measured.
        _bodyHost.Register(_scroll);
        _bodyHost.Register(_chronicle.Body);
        _bodyHost.CurrentBodyChanged += OnBodyHostChanged;
        // The expedition list is the opening protagonist. Going through
        // ShowSection rather than the host directly is what makes the two
        // headers agree with it from the first frame.
        ShowSection(_scroll);

        // ChroniclePanel keeps owning the chronicle's rows, projection and
        // offline report, but it does not parent either of its own parts.
        // It sits in the scene hidden and empty, purely so its lifetime is
        // managed with the rail's; nothing renders through it.

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
        // The chronicle body shares the body host with the expedition
        // scroll and owns its own bounded rows scroll. Letting the rail
        // intercept the wheel here would scroll the wrong surface — the
        // expedition cards instead of the chronicle the pointer is over.
        // Test against the body, not the panel: the panel is an empty,
        // hidden logic owner and has no meaningful rect.
        if (_chronicle is not null
            && _chronicle.Body.Visible
            && _chronicle.Body.GetGlobalRect().HasPoint(mouse.GlobalPosition))
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
        if (!WorldofGoses.Testing.VisualRegressionHarness.IsActive) return;
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
            _expeditionContent.AddChild(
                new HudListCaption(UiText.Get("ui.expedition_rail.none_active")));
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
    /// The one place that decides which section is open, and therefore the
    /// only authority on the question (GitHub #15).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rail used to keep three: the rail header's <c>Expanded</c>, the
    /// chronicle header's, and <see cref="AccordionHost.CurrentBody"/> — three
    /// values for one fact, each written from a different handler. They could
    /// disagree, and one transition made them disagree by design: closing the
    /// chronicle while the expedition body was already folded forced
    /// <c>_header.Expanded = true</c>, so clicking a section's own header to
    /// close it reopened the other one. Closing expeditions left both folded;
    /// closing the chronicle did not. Same gesture, two grammars.
    /// </para>
    /// <para>
    /// Now the host holds the state and both headers are told what they are.
    /// Zero or one section open, a second click on the open header closes it
    /// and opens nothing, and a new section added to the rail joins the same
    /// rule by registering a body — no pairwise toggle to write.
    /// </para>
    /// </remarks>
    private void ShowSection(Control? body)
    {
        _bodyHost.ShowOnly(body);

        // Assigning a header's Expanded raises ExpandedChanged, which lands
        // back here. The guard makes that re-entry a no-op instead of a
        // second, conflicting decision.
        _syncingSections = true;
        _header.Expanded = _bodyHost.IsShowing(_scroll);
        _chronicle.Expanded = _bodyHost.IsShowing(_chronicle.Body);
        _syncingSections = false;

        // The quick-action button belongs to the expedition section and
        // follows it, so a folded rail really is just its headers.
        _expeditionSection.Visible = _bodyHost.IsShowing(_scroll);
        // Zero bodies open means the rail shrinks to its header stack; one
        // open means it claims the shared envelope and that body scrolls
        // inside it. The container tree resolves the split — nothing here
        // subtracts header heights from a maximum.
        SizeFlagsVertical = SidePanelHost.PanelSizing(body is not null);
        RebuildFocusables();
        if (_bodyHost.IsShowing(_chronicle.Body)) _chronicle.ScrollToNewest();
    }

    private bool _syncingSections;

    /// <summary>
    /// Either header toggling means the same thing: open me, or — if I was
    /// the one already open — close everything.
    /// </summary>
    private void OnHeaderExpandedChanged(bool expanded)
    {
        if (_syncingSections) return;
        ShowSection(expanded ? _scroll : null);
    }

    private void OnChronicleExpanded(bool expanded)
    {
        if (_syncingSections) return;
        ShowSection(expanded ? _chronicle.Body : null);
    }

    /// <summary>
    /// The host swapped bodies, so the focus chain must be rebuilt: the
    /// hidden body's controls are off screen and must not stay reachable.
    /// </summary>
    private void OnBodyHostChanged() => RebuildFocusables();

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
        // Rerouted through the shared FocusRing helper, which uses
        // relative GetPathTo() paths. The previous implementation
        // resolved absolute paths via GetPath() — fine until the rail
        // was reparented, then the targets silently turned into
        // broken focus jumps. Close #52.
        FocusRing.WireVertical(_focusables);
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
