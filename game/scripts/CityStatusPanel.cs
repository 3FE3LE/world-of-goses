#nullable enable
using System;
using Godot;
using WorldofGoses.Domain;

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
        AddChild(_row);

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
        string text = $"Saved · {FormatSavedTime(_lastSavedUnixMillis)}";
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
        // Compact threshold matches the project's reference viewport width
        // (1280×720). Below that, chips collapse to a single summary so the
        // row never pushes the shell UI past the viewport width.
        bool compact = GetViewportRect().Size.X < 1280f;
        _row.AddThemeConstantOverride("separation", compact ? 8 : ChipGap);
        foreach (var child in _row.GetChildren())
        {
            _row.RemoveChild(child);
            child.QueueFree();
        }
        _savedChip = null;

        BuildClockChip(snapshot);
        BuildResourcesChip(snapshot);
        if (compact)
        {
            BuildCompactCityChip(snapshot);
        }
        else
        {
            BuildUpkeepChip(snapshot);
            BuildMobilisationChip(snapshot);
        }

        // Construction is intentionally singular in the current slice. Keep
        // one concise progress chip instead of allowing future projects to
        // grow the status strip horizontally without bound.
        if (snapshot.Projects.Count > 0)
        {
            BuildProjectChip(snapshot.Projects[0], compact);
        }

        BuildAttentionChip(snapshot);
        if (!compact) BuildFreeCitizensChip(snapshot);

        if (snapshot.IsEmpty)
        {
            BuildHeroChip(snapshot);
            BuildEmptyStateChip();
        }

        ApplySavedChip();
    }

    private void BuildClockChip(CityStatusSnapshot snapshot)
    {
        int tick = snapshot.CurrentTick;
        bool day = GameClock.IsDaytime(tick);
        string iconPath = day ? IconPaths.Sun : IconPaths.Moon;
        // The day field varies (1–3 digits) and even a monospaced font
        // shifts the chip width when the digit count changes. Wrap the
        // chip in a fixed-width Control with clip_contents so the row
        // never reflows as the simulation advances.
        var chip = new IconChip(iconPath, SimulationTimeText.Format(tick), "BuildingName");
        chip.TooltipText = SimulationTimeText.Format(tick);
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
        int rate = snapshot.UpkeepPerTick;
        if (rate <= 0) return;
        _row.AddChild(new IconChip(IconPaths.Coin, $"-{rate} stone/tick (upkeep)"));
    }

    private void BuildFoodChip(CityStatusSnapshot snapshot)
    {
        // Kept for backward compatibility (call sites may still want the
        // standalone chip). The default Refresh() uses BuildResourcesChip
        // to fit on 1280×720.
        int food = snapshot.FoodStock;
        int cap = snapshot.MaxFoodStock;
        if (cap <= 0) return;
        _row.AddChild(new IconChip(IconPaths.Leaf, $"Food: {food} / {cap}"));
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
                ? $"Wood: {stock} gathered · {reserve} in forests"
                : $"Wood: {stock} gathered"));
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
            breakdown.Append($"Food: {snapshot.FoodStock} / {snapshot.MaxFoodStock}");
        }
        if (hasWood)
        {
            if (breakdown.Length > 0) breakdown.Append('\n');
            breakdown.Append(snapshot.WoodReserve > 0
                ? $"Wood: {snapshot.WoodStock} gathered · {snapshot.WoodReserve} in forests"
                : $"Wood: {snapshot.WoodStock} gathered");
        }

        string headline = hasFood && snapshot.MaxFoodStock > 0
            ? $"Food {snapshot.FoodStock}/{snapshot.MaxFoodStock}"
            : hasWood
                ? $"Wood {snapshot.WoodStock}"
                : "Resources";

        var chip = new IconChip(IconPaths.Leaf, headline);
        chip.TooltipText = breakdown.ToString();
        _row.AddChild(chip);
    }

    private void BuildMobilisationChip(CityStatusSnapshot snapshot)
    {
        _row.AddChild(new IconChip(IconPaths.User,
            $"{snapshot.CitizensAtWork} at work · {snapshot.CitizensAtHome} at home"));
    }

    private void BuildProjectChip(CityStatusSnapshot.ProjectItem project, bool compact)
    {
        var phase = ConstructionRules.PhaseFor(project.Progress, project.RequiredWork);
        string label = compact
            ? $"Build {project.Progress}/{project.RequiredWork}"
            : $"{project.DisplayName} {project.Progress}/{project.RequiredWork} " +
                $"({ConstructionRules.Describe(phase)}) · {project.AssignedCount}/{project.WorkerCapacity}";
        if (!project.Enabled) label += " · paused";
        var chip = new IconChip(IconPaths.Building, label);
        chip.TooltipText = project.Enabled
            ? $"In progress. Click the construction menu for details."
            : "Paused. Resume from the construction menu.";
        _row.AddChild(chip);
    }

    private void BuildCompactCityChip(CityStatusSnapshot snapshot)
    {
        var chip = new IconChip(
            IconPaths.User,
            $"Work {snapshot.CitizensAtWork} · Home {snapshot.CitizensAtHome} · Free {snapshot.FreeCitizenNames.Count}");
        chip.TooltipText = snapshot.UpkeepPerTick > 0
            ? $"Upkeep: {snapshot.UpkeepPerTick} stone/tick"
            : "No current upkeep";
        _row.AddChild(chip);
    }

    private void BuildBuildingChip(CityStatusSnapshot.BuildingItem building)
    {
        string range = building.StorageCapacity > 0
            ? $" ({building.MinStock}-{building.MaxStock})"
            : string.Empty;
        string label = $"{building.DisplayName}: {building.Stock}/{building.StorageCapacity}" +
            $"{range} {building.ResourceUnit} · {building.AssignedCount}/{building.WorkerCapacity} workers" +
            StopCauseSuffix(building);
        _row.AddChild(new IconChip(IconPaths.House, label));
    }

    private void BuildFreeCitizensChip(CityStatusSnapshot snapshot)
    {
        var chip = new IconChip(
            IconPaths.User,
            $"Free citizens: {snapshot.FreeCitizenNames.Count}");
        if (snapshot.FreeCitizenNames.Count > 0)
        {
            chip.TooltipText = "Unassigned: " + string.Join(", ", snapshot.FreeCitizenNames);
        }
        _row.AddChild(chip);
    }

    private void BuildAttentionChip(CityStatusSnapshot snapshot)
    {
        int attentionCount = 0;
        foreach (var building in snapshot.Buildings)
        {
            if (building.StopCause is ProductionStopCause.NoWorkers
                or ProductionStopCause.WorkersExhausted
                or ProductionStopCause.MissingInputs)
            {
                attentionCount++;
            }
        }
        if (attentionCount > 0)
        {
            _row.AddChild(new IconChip(
                IconPaths.Warning,
                $"Needs attention: {attentionCount}"));
        }
    }

    private void BuildHeroChip(CityStatusSnapshot snapshot)
    {
        string heroName = snapshot.HeroName ?? "not established";
        _row.AddChild(new IconChip(IconPaths.User, $"Hero: {heroName}"));
    }

    private void BuildEmptyStateChip()
    {
        _row.AddChild(new IconChip(IconPaths.House, "No buildings yet"));
    }

    private static string StopCauseSuffix(CityStatusSnapshot.BuildingItem building) => building.StopCause switch
    {
        ProductionStopCause.Paused => " · paused",
        ProductionStopCause.TargetReached => " · full",
        ProductionStopCause.WorkersExhausted => " · exhausted",
        ProductionStopCause.NoWorkers => " · no workers",
        ProductionStopCause.Night => " · night",
        ProductionStopCause.MissingInputs => " · missing inputs",
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
