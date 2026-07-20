#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Replaces the one-line offline banner with a chronological panel
/// of <see cref="WorldEvent"/> rows. Each row carries an icon
/// (from <see cref="WorldEvent.IconPath"/>), a one-line summary, and
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

    private Label _summary = null!;
    private ScrollContainer _scroll = null!;
    private VBoxContainer _list = null!;

    public override void _Ready()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ShrinkBegin;
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
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        shell.AddThemeConstantOverride("separation", 8);
        margin.AddChild(shell);

        var titleRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        titleRow.AddThemeConstantOverride("separation", 8);
        shell.AddChild(titleRow);

        var titleIcon = new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(IconPaths.Calendar),
            CustomMinimumSize = new Vector2(18, 18),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Keep,
            Modulate = LineageThemeRegistry.IconAccent,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        titleRow.AddChild(titleIcon);

        var title = new Label
        {
            Text = "City chronicle",
            ThemeTypeVariation = "PanelTitle",
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        titleRow.AddChild(title);

        _summary = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = "BodySmall",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        shell.AddChild(_summary);

        _scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize = new Vector2(0, 220),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        shell.AddChild(_scroll);

        _list = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        _list.AddThemeConstantOverride("separation", RowSpacing);
        _scroll.AddChild(_list);

        _summary.Text = "The city's recent events will be recorded here.";
    }

    /// <summary>
    /// Populates the panel with a fresh offline report. The panel
    /// shows itself; passing a report with no events keeps it hidden.
    /// </summary>
    public void ShowReport(OfflineProgressionReport report)
    {
        foreach (var child in _list.GetChildren())
        {
            child.QueueFree();
        }

        if (!report.HadProgression || report.Events.Count == 0)
        {
            Hide();
            return;
        }

        _summary.Text = SummariseReport(report);

        // Show the most recent N events; older ones would only add
        // noise to the panel.
        IReadOnlyList<WorldEvent> events = report.Events;
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
        foreach (var child in _list.GetChildren())
        {
            child.QueueFree();
        }

        _summary.Text = events.Count == 0
            ? "The city's recent events will be recorded here."
            : $"{events.Count} events recorded · newest at the bottom";

        int skip = System.Math.Max(0, events.Count - MaxRows);
        for (int i = skip; i < events.Count; i++)
        {
            _list.AddChild(new EventRow(events[i]));
        }

        Show();
        CallDeferred(MethodName.ScrollToBottom);
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

    private static string FormatTime(System.TimeSpan time)
    {
        if (time.TotalDays >= 1) return $"{(int)time.TotalDays}d {time.Hours}h";
        if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
        if (time.TotalMinutes >= 1) return $"{(int)time.TotalMinutes}m {time.Seconds}s";
        return $"{(int)time.TotalSeconds}s";
    }

    /// <summary>
    /// One row of the offline report: tinted icon + summary + tick.
    /// The row is intentionally compact so the player can scan the
    /// full timeline without scrolling; the summary line carries
    /// the human meaning, the icon hints the category, and the tick
    /// anchors the row in time.
    /// </summary>
    private partial class EventRow : HBoxContainer
    {
        public EventRow(WorldEvent evt)
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
            if (evt.IconPath is not null)
            {
                icon.Texture = ResourceLoader.Load<Texture2D>(evt.IconPath);
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
            AddChild(label);

            var tickLabel = new Label
            {
                Text = $"t{evt.Tick}",
                ThemeTypeVariation = "BodySmall",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(60, 0),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            AddChild(tickLabel);
        }
    }
}
