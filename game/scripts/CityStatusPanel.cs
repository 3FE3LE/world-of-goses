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
/// Each pair is built with <see cref="StatChip"/>, a tiny helper that
/// keeps the icon-on-the-left layout consistent across the strip and
/// guarantees integer pixel positions for the pixel-art pipeline.
/// Text styling comes from the project's default theme (BodySmall);
/// icons come from <see cref="IconPaths"/>.
/// </summary>
public partial class CityStatusPanel : PanelContainer
{
    private const int ChipGap = Tokens.SpacingLoose;
    private const float StatusHorizontalPadding = 8f;
    // Vertical breathing room comes from the global 8 px safe-area rule.
    // Keep the ornamental resource itself at zero so the two layers do not
    // accumulate padding.
    private const float StatusVerticalPadding = 0f;
    /// <summary>Fixed width of the clock chip so the row never shifts when
    /// the day digit count changes (1–3 digits). Sized to fit
    /// "Day 99 · 23:59" at 22 px Jersey 10 plus the icon + gap.</summary>
    private const float ClockChipWidth = 180f;

    private LineageThemeSignals? _themeSignals;
    private HBoxContainer _row = null!;
    private StatChip? _savedChip;
    private ulong _saveIndicatorGeneration;
    private ulong _emphasizedSaveGeneration;
    private bool _saveIndicatorVisible;
    private CityWorldController? _controller;

    public override void _Ready()
    {
        // HUD chrome: stay above the ambient day/night tint so the status
        // strip keeps its authored contrast at every in-game hour.
        OverlayLayers.Apply(this, OverlayLayers.Hud);
        EnsureBuilt();
        GetViewport().SizeChanged += OnViewportSizeChanged;
    }

    /// <summary>
    /// Creates the row and wires subscriptions the first time it runs.
    /// Safe to call multiple times — idempotent. Exists so that an
    /// early <see cref="Refresh"/> from a sibling that was instantiated
    /// before us doesn't crash on a null
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

        ApplyLineageStatusStyle();
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
        _ = unixMillis;
        ulong generation = ++_saveIndicatorGeneration;
        _saveIndicatorVisible = true;
        ApplySavedChip();
        GetTree().CreateTimer(2.25).Timeout += () =>
        {
            if (!IsInstanceValid(this) || generation != _saveIndicatorGeneration) return;
            RemoveSavedChip();
        };
    }

    private void ApplySavedChip()
    {
        if (_row is null) return;
        if (!_saveIndicatorVisible) return;
        string text = UiText.Get("ui.status.saved_short");
        if (_savedChip is null)
        {
            _savedChip = new StatChip(IconPaths.Check, text);
            _row.AddChild(_savedChip);
        }
        else
        {
            _savedChip.UpdateText(text);
        }
        _row.MoveChild(_savedChip, _row.GetChildCount() - 1);
        if (_emphasizedSaveGeneration != _saveIndicatorGeneration)
        {
            _emphasizedSaveGeneration = _saveIndicatorGeneration;
            UiMotion.Pulse(_savedChip, LineageThemeRegistry.IconAccent);
        }
    }

    private void RemoveSavedChip()
    {
        if (_savedChip is null) return;
        if (_savedChip.GetParent() == _row) _row.RemoveChild(_savedChip);
        _savedChip.QueueFree();
        _savedChip = null;
        _saveIndicatorVisible = false;
    }

    private void OnLineageChanged(string lineage)
    {
        ApplyLineageStatusStyle();
        ReapplyAccent();
    }

    /// <summary>
    /// Keeps the active lineage's ornamental panel without inheriting the
    /// generous card padding intended for large views. The resource is
    /// duplicated so compacting the HUD never mutates panels elsewhere.
    /// </summary>
    private void ApplyLineageStatusStyle()
    {
        var style = (StyleBox)LineageThemeRegistry
            .GetStyleBox(LineageThemeRegistry.ComponentPanel)
            .Duplicate();
        style.ContentMarginLeft = StatusHorizontalPadding;
        style.ContentMarginTop = StatusVerticalPadding;
        style.ContentMarginRight = StatusHorizontalPadding;
        style.ContentMarginBottom = StatusVerticalPadding;
        AddThemeStyleboxOverride("panel", style);
    }

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
		// The status bar is intentionally bounded: clock, speed control,
		// and — only when a project exists — a concise project
        // chip. The mobilisation, hero, and empty-state chips moved to
        // their natural surface (BuildingDetailView, EmptyPanel); a
        // building's own StopCause is visible on its detail view and
        // plot tooltip. Upkeep is dormant; the chip that used to
        // advertise it is gone.
        float windowWidth = DisplayServer.WindowGetSize().X;
        bool compact = ShouldUseCompactLayout(windowWidth, snapshot.Projects.Count > 0);
        _row.AddThemeConstantOverride("separation", compact ? Tokens.SpacingBase : ChipGap);
        foreach (var child in _row.GetChildren())
        {
            _row.RemoveChild(child);
            child.QueueFree();
        }
        _savedChip = null;

		BuildClockChip(snapshot);
		BuildOffHoursChip(snapshot);

        // Construction is intentionally singular in the current slice. Keep
        // one concise progress chip instead of allowing future projects to
        // grow the status strip horizontally without bound.
        if (snapshot.Projects.Count > 0)
        {
            BuildProjectChip(snapshot.Projects[0], compact);
        }

        if (_saveIndicatorVisible) ApplySavedChip();
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
        var chip = new StatChip(iconPath, SimulationTimeText.FormatLocalized(tick), "BuildingName");
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
            // Speed only. The play/pause control is gone: the world advances
            // while the game is closed, so a button that freezes it was
            // arguing with the premise. A player who wants the city to settle
            // slows it down instead of stopping it.
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

    /// <summary>
    /// Surfaces the configured workday window so the player knows at
    /// a glance whether production, construction and expedition
    /// mobilisation can run. The chip only appears outside the
    /// configured 08:00–16:00 window (the day/night clock already
    /// rotates the icon for the full daily cycle, but the chip is
    /// the explicit "work paused" cue the player asked for during
    /// the 2026-07-30 playtest).
    ///
    /// It reads the snapshot's labour flag rather than the raw clock: the
    /// founding-camp bypass keeps work running at any hour until the first
    /// Basic Shelter exists, so a clock-only test announced "work paused"
    /// for the entire opening while the founder was in fact building.
    /// </summary>
    private void BuildOffHoursChip(CityStatusSnapshot snapshot)
    {
        if (snapshot.IsLaborTime) return;
        var chip = new StatChip(IconPaths.Moon, UiText.Get("ui.status.off_hours"));
        chip.TooltipText = UiText.Get("ui.status.off_hours_hint");
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
        // A worksite that is not advancing must say so here. The strip is the
        // one surface always on screen, and "Obra 0/180" with no reason reads as
        // a broken game rather than a blocked one.
        label += StopCauseSuffix(project);
        var chip = new StatChip(IconPaths.Building, label);
        chip.TooltipText = StopCauseHint(project);
        _row.AddChild(chip);
    }

    private static string StopCauseSuffix(CityStatusSnapshot.ProjectItem project)
    {
        if (!project.Enabled) return UiText.Get(" · paused");
        return project.StopCause switch
        {
            ConstructionStopCause.Paused => UiText.Get(" · paused"),
            ConstructionStopCause.NoWorkers => UiText.Get(" · no workers"),
            ConstructionStopCause.WorkersExhausted => UiText.Get(" · exhausted"),
            ConstructionStopCause.WorkersInTransit => UiText.Get(" · travelling"),
            ConstructionStopCause.MissingMaterials => UiText.Get(" · missing inputs"),
            ConstructionStopCause.Night => UiText.Get(" · night"),
            ConstructionStopCause.AwaitingModule => UiText.Get(" · awaiting module"),
            ConstructionStopCause.NoHero => UiText.Get(" · no hero"),
            _ => string.Empty,
        };
    }

    private static string StopCauseHint(CityStatusSnapshot.ProjectItem project)
    {
        if (!project.Enabled) return UiText.Get("Paused. Resume from the construction menu.");
        return project.StopCause switch
        {
            ConstructionStopCause.NoWorkers =>
                UiText.Get("ui.status.build_hint_no_workers"),
            ConstructionStopCause.WorkersExhausted =>
                UiText.Get("ui.status.build_hint_exhausted"),
            ConstructionStopCause.WorkersInTransit =>
                UiText.Get("ui.status.build_hint_travelling"),
            ConstructionStopCause.MissingMaterials =>
                UiText.Get("ui.status.build_hint_materials"),
            ConstructionStopCause.Night =>
                UiText.Get("ui.status.build_hint_night"),
            ConstructionStopCause.AwaitingModule =>
                UiText.Get("ui.status.build_hint_module"),
            _ => UiText.Get("In progress. Click the construction menu for details."),
        };
    }

    private static string StopCauseSuffix(CityStatusSnapshot.BuildingItem building) => building.StopCause switch
    {
        ProductionStopCause.Paused => UiText.Get(" · paused"),
        ProductionStopCause.TargetReached => UiText.Get(" · full"),
        ProductionStopCause.WorkersExhausted => UiText.Get(" · exhausted"),
        ProductionStopCause.NoWorkers => UiText.Get(" · no workers"),
        ProductionStopCause.Night => UiText.Get(" · night"),
        ProductionStopCause.MissingInputs => UiText.Get(" · missing inputs"),
        ProductionStopCause.WorkersInTransit => UiText.Get(" · travelling"),
        ProductionStopCause.WorkersRecovering => UiText.Get(" · recovering"),
        ProductionStopCause.WorkersBlockedNoFood => UiText.Get(" · no food"),
        _ => string.Empty,
    };
}

