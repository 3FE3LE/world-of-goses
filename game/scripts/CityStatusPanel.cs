#nullable enable
using System;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Top-of-screen status strip. Renders the city's headline state as
/// a horizontal row of icon-plus-label pairs (day/night, mobilisation,
/// per-building summary, free citizens) separated by thin gaps.
///
/// Each pair is built with <see cref="IconChip"/>, a tiny helper that
/// keeps the icon-on-the-left layout consistent across the strip and
/// guarantees integer pixel positions for the pixel-art pipeline.
/// Text styling comes from the project's default theme (BodySmall);
/// icons come from <see cref="IconPaths"/>.
/// </summary>
public partial class CityStatusPanel : PanelContainer
{
    private const int ChipGap = 18;
    /// <summary>Fixed width of the clock chip so the row never shifts when
    /// the day digit count changes (1–3 digits). Sized to fit
    /// "Day 99 · 23:59" at 22 px Jersey 10 plus the icon + gap.</summary>
    private const float ClockChipWidth = 180f;

    private LineageThemeSignals? _themeSignals;
    private HBoxContainer _row = null!;
    private IconChip? _savedChip;
    private long _lastSavedUnixMillis;
    private long _lastEmphasizedSavedUnixMillis;
    private CityWorldController? _controller;

    public override void _Ready()
    {
        EnsureBuilt();
        GetViewport().SizeChanged += OnViewportSizeChanged;
    }

    /// <summary>
    /// Creates the row and wires subscriptions the first time it runs.
    /// Safe to call multiple times — idempotent. Exists so that an
    /// early <see cref="Refresh"/> from a sibling that was instantiated
    /// before us (e.g. <c>CityMacroView</c>) doesn't crash on a null
    /// <c>_row</c>.
    /// </summary>
    private void EnsureBuilt()
    {
        if (_row is not null) return;

        _row = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _row.AddThemeConstantOverride("separation", ChipGap);
        // The status bar surface spans the full width of the GameUiShell
        // VBox; wrap the chip row in a SafeAreaMarginContainer so the
        // chip content stays inside the OS safe area on notched or
        // rounded displays. Wrapping the OUTER panel with a margin
        // container previously rendered a visible grey band and was
        // reverted (TO_DO.md 2026-07-22).
        var safeArea = new SafeAreaMarginContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(safeArea);
        safeArea.AddChild(_row);

        AddThemeStyleboxOverride("panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
        _themeSignals = GetNodeOrNull<LineageThemeSignals>("/root/LineageThemeSignals");
        if (_themeSignals is not null)
        {
            _themeSignals.LineageChanged += OnLineageChanged;
        }
        LineageThemeRegistry.ActiveLineageChanged += OnLineageAccentChanged;
    }

    /// <summary>
    /// Subscribes to the controller's save signal so the indicator
    /// chip stays accurate even when the panel is built before the
    /// controller signals the first save.
    /// </summary>
    public void AttachController(CityWorldController controller)
    {
        _controller = controller;
        controller.WorldSaved += OnWorldSaved;
        ApplySavedChip();
    }

    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= OnViewportSizeChanged;
        if (_controller is not null)
        {
            _controller.WorldSaved -= OnWorldSaved;
        }
        if (_themeSignals is not null) _themeSignals.LineageChanged -= OnLineageChanged;
        LineageThemeRegistry.ActiveLineageChanged -= OnLineageAccentChanged;
    }

    private void OnViewportSizeChanged()
    {
        if (_controller is not null) Refresh(_controller);
    }

    private void OnWorldSaved(long unixMillis)
    {
        _lastSavedUnixMillis = unixMillis;
        ApplySavedChip();
    }

    private void ApplySavedChip()
    {
        if (_row is null) return;
        if (_lastSavedUnixMillis <= 0)
        {
            if (_savedChip is not null)
            {
                _row.RemoveChild(_savedChip);
                _savedChip.QueueFree();
            }
            _savedChip = null;
            return;
        }
        string text = UiText.Format("ui.status.saved", FormatSavedTime(_lastSavedUnixMillis));
        if (_savedChip is null)
        {
            _savedChip = new IconChip(IconPaths.Check, text);
            _row.AddChild(_savedChip);
        }
        else
        {
            _savedChip.UpdateText(text);
        }
        _row.MoveChild(_savedChip, _row.GetChildCount() - 1);
        if (_lastEmphasizedSavedUnixMillis != _lastSavedUnixMillis)
        {
            _lastEmphasizedSavedUnixMillis = _lastSavedUnixMillis;
            UiMotion.Pulse(_savedChip, LineageThemeRegistry.IconAccent);
        }
    }

    private static string FormatSavedTime(long unixMillis)
    {
        var time = DateTimeOffset.FromUnixTimeMilliseconds(unixMillis).ToLocalTime();
        return time.ToString("HH:mm");
    }

    private void OnLineageChanged(string lineage) => AddThemeStyleboxOverride(
        "panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));

    private void OnLineageAccentChanged(string lineage) => ReapplyAccent();

    /// <summary>
    /// Walks every chip currently in the row and re-tints its leading
    /// icon with the active linaje's accent. Called once on _Ready and
    /// again whenever the linaje changes via <c>LineageThemeSignals</c>.
    /// </summary>
    private void ReapplyAccent()
    {
        if (_row is null) return;
        var accent = LineageThemeRegistry.IconAccent;
        foreach (var child in _row.GetChildren())
        {
            if (child is HBoxContainer chip)
            {
                TintTextureRects(chip, accent);
            }
        }
    }

    private static void TintTextureRects(Node root, Color accent)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is TextureRect icon) icon.Modulate = accent;
            TintTextureRects(child, accent);
        }
    }

    public void Refresh(CityWorldController controller)
    {
        EnsureBuilt();
        var snapshot = controller.GetCityStatusSnapshot();
        // The status bar is intentionally bounded: clock, speed controls,
        // resources, and — only when a project exists — a concise project
        // chip. The mobilisation, hero, and empty-state chips moved to
        // their natural surface (BuildingDetailView, EmptyPanel); a
        // building's own StopCause is visible on its detail view and
        // plot tooltip. Upkeep is dormant; the chip that used to
        // advertise it is gone.
        float windowWidth = DisplayServer.WindowGetSize().X;
        bool compact = ShouldUseCompactLayout(windowWidth, snapshot.Projects.Count > 0);
        _row.AddThemeConstantOverride("separation", compact ? 8 : ChipGap);
        foreach (var child in _row.GetChildren())
        {
            _row.RemoveChild(child);
            child.QueueFree();
        }
        _savedChip = null;

        BuildClockChip(snapshot);
        BuildResourcesChip(snapshot);

        // Construction is intentionally singular in the current slice. Keep
        // one concise progress chip instead of allowing future projects to
        // grow the status strip horizontally without bound.
        if (snapshot.Projects.Count > 0)
        {
            BuildProjectChip(snapshot.Projects[0], compact);
        }

        ApplySavedChip();
    }

    internal static bool ShouldUseCompactLayout(float windowWidth, bool hasActiveProject) =>
        windowWidth < 1280f || hasActiveProject;

    private void BuildClockChip(CityStatusSnapshot snapshot)
    {
        int tick = snapshot.CurrentTick;
        bool day = GameClock.IsDaytime(tick);
        string iconPath = day ? IconPaths.Sun : IconPaths.Moon;
        // The day field varies (1–3 digits) and even a monospaced font
        // shifts the chip width when the digit count changes. Wrap the
        // chip in a fixed-width Control with clip_contents so the row
        // never reflows as the simulation advances.
        var chip = new IconChip(iconPath, SimulationTimeText.FormatLocalized(tick), "BuildingName");
        chip.TooltipText = SimulationTimeText.FormatLocalized(tick);
        var wrap = new Control
        {
            CustomMinimumSize = new Vector2(ClockChipWidth, 0),
            ClipContents = true,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        wrap.AddChild(chip);
        _row.AddChild(wrap);
        if (snapshot.HasController)
        {
            // Two independent buttons: PlayPause owns the pause state,
            // SpeedButton owns the speed multiplier. The row separation
            // gives them a visible gap without a wrapping container.
            _row.AddChild(new PlayPauseButton
            {
                ThemeTypeVariation = "ButtonText",
                FocusMode = Control.FocusModeEnum.All,
            });
            _row.AddChild(new SpeedButton
            {
                ThemeTypeVariation = "ButtonText",
                FocusMode = Control.FocusModeEnum.All,
            });
        }
    }

    private void BuildUpkeepChip(CityStatusSnapshot snapshot)
    {
        // Upkeep is dormant. Kept private to avoid leaving the public
        // surface dangling for any external caller; the call site in
        // Refresh() no longer invokes it. Remove this stub entirely
        // when the seam is reactivated.
        _ = snapshot;
    }

    private void BuildFoodChip(CityStatusSnapshot snapshot)
    {
        // Kept for backward compatibility (call sites may still want the
        // standalone chip). The default Refresh() uses BuildResourcesChip
        // to fit on 1280×720.
        int food = snapshot.FoodStock;
        int cap = snapshot.MaxFoodStock;
        if (cap <= 0) return;
        _row.AddChild(new IconChip(IconPaths.Leaf, UiText.Format("ui.status.food_stock", food, cap)));
    }

    private void BuildWoodChip(CityStatusSnapshot snapshot)
    {
        // Kept for backward compatibility. Default Refresh() uses
        // BuildResourcesChip to fit on 1280×720.
        int stock = snapshot.WoodStock;
        int reserve = snapshot.WoodReserve;
        if (stock == 0 && reserve == 0) return;
        _row.AddChild(new IconChip(
            IconPaths.Tree,
            reserve > 0
                ? UiText.Format("ui.status.wood_reserve", stock, reserve)
                : UiText.Format("ui.status.wood_stock", stock)));
    }

    /// <summary>
    /// Combines Food and Wood into a single compact chip when both
    /// categories are active, so the status bar stops overflowing on
    /// 1280×720. The tooltip exposes the full breakdown.
    /// </summary>
    private void BuildResourcesChip(CityStatusSnapshot snapshot)
    {
        bool hasFood = snapshot.MaxFoodStock > 0;
        bool hasWood = snapshot.WoodStock > 0 || snapshot.WoodReserve > 0;
        if (!hasFood && !hasWood) return;

        var breakdown = new System.Text.StringBuilder();
        if (hasFood)
        {
            breakdown.Append(UiText.Format(
                "ui.status.food_stock", snapshot.FoodStock, snapshot.MaxFoodStock));
        }
        if (hasWood)
        {
            if (breakdown.Length > 0) breakdown.Append('\n');
            breakdown.Append(snapshot.WoodReserve > 0
                ? UiText.Format("ui.status.wood_reserve", snapshot.WoodStock, snapshot.WoodReserve)
                : UiText.Format("ui.status.wood_stock", snapshot.WoodStock));
        }

        string headline = hasFood && snapshot.MaxFoodStock > 0
            ? UiText.Format("ui.status.food", snapshot.FoodStock, snapshot.MaxFoodStock)
            : hasWood
                ? UiText.Format("ui.status.wood", snapshot.WoodStock)
                : UiText.Get("Resources");

        var chip = new IconChip(IconPaths.Leaf, headline);
        chip.TooltipText = breakdown.ToString();
        _row.AddChild(chip);
    }

    private void BuildProjectChip(CityStatusSnapshot.ProjectItem project, bool compact)
    {
        var phase = ConstructionRules.PhaseFor(project.Progress, project.RequiredWork);
        string label = compact
            ? UiText.Format("ui.status.build", project.Progress, project.RequiredWork)
            : UiText.Format(
                "ui.status.project",
                UiText.Get(project.DisplayName),
                project.Progress,
                project.RequiredWork,
                UiText.Get(ConstructionRules.Describe(phase)),
                project.AssignedCount,
                project.WorkerCapacity);
        if (!project.Enabled) label += UiText.Get(" · paused");
        var chip = new IconChip(IconPaths.Building, label);
        chip.TooltipText = project.Enabled
            ? UiText.Get("In progress. Click the construction menu for details.")
            : UiText.Get("Paused. Resume from the construction menu.");
        _row.AddChild(chip);
    }

    private static string StopCauseSuffix(CityStatusSnapshot.BuildingItem building) => building.StopCause switch
    {
        ProductionStopCause.Paused => UiText.Get(" · paused"),
        ProductionStopCause.TargetReached => UiText.Get(" · full"),
        ProductionStopCause.WorkersExhausted => UiText.Get(" · exhausted"),
        ProductionStopCause.NoWorkers => UiText.Get(" · no workers"),
        ProductionStopCause.Night => UiText.Get(" · night"),
        ProductionStopCause.MissingInputs => UiText.Get(" · missing inputs"),
        _ => string.Empty,
    };
}

/// <summary>
/// One icon-plus-text pair used in <see cref="CityStatusPanel"/>.
/// Compact helper that keeps the icon-on-the-left layout consistent
/// across the strip; intentionally not a Control so it inlines
/// without its own panel chrome. Icons ship with a white SVG fill
/// and are tinted at construction time with the active linaje's
/// accent; the parent <see cref="CityStatusPanel"/> re-tints every
/// chip when the linaje changes so the entire strip stays coherent.
/// </summary>
public partial class IconChip : HBoxContainer
{
    private const int IconTextGap = 8;
    private const int IconSize = 14;
    private const int ChipHeight = 24;

    private Label _label = null!;

    public IconChip(string iconPath, string text, string labelVariation = "BodySmall")
    {
        MouseFilter = MouseFilterEnum.Pass;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        CustomMinimumSize = new Vector2(0, ChipHeight);
        AddThemeConstantOverride("separation", IconTextGap);
        TooltipText = string.Empty;

        var iconCell = new MarginContainer
        {
            CustomMinimumSize = new Vector2(IconSize, ChipHeight),
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        iconCell.AddThemeConstantOverride("margin_top", 3);
        AddChild(iconCell);

        var icon = new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(iconPath),
            StretchMode = TextureRect.StretchModeEnum.Keep,
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = LineageThemeRegistry.IconAccent,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        iconCell.AddChild(icon);

        _label = new Label
        {
            Text = text,
            // Pass any non-empty variation to override the default
            // BodySmall (Pixelify Sans). The clock uses a Jersey 10
            // variation so the digit widths stay constant across ticks.
            ThemeTypeVariation = string.IsNullOrEmpty(labelVariation) ? "BodySmall" : labelVariation,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(_label);
    }

    /// <summary>
    /// Replaces the chip text without rebuilding the icon. Used by the
    /// "Saved" chip so the timestamp can refresh in place.
    /// </summary>
    public void UpdateText(string text)
    {
        if (_label is not null) _label.Text = text;
    }
}
