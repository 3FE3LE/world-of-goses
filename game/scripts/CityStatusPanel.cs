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

    private LineageThemeSignals? _themeSignals;
    private HBoxContainer _row = null!;
    private IconChip? _savedChip;
    private long _lastSavedUnixMillis;
    private CityWorldController? _controller;

    public override void _Ready()
    {
        EnsureBuilt();
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
        controller.SimulationSpeedChanged += OnSimulationSpeedChanged;
        ApplySavedChip();
    }

    public override void _ExitTree()
    {
        if (_controller is not null)
        {
            _controller.WorldSaved -= OnWorldSaved;
            _controller.SimulationSpeedChanged -= OnSimulationSpeedChanged;
        }
        if (_themeSignals is not null) _themeSignals.LineageChanged -= OnLineageChanged;
        LineageThemeRegistry.ActiveLineageChanged -= OnLineageAccentChanged;
    }

    private void OnSimulationSpeedChanged(int speedChoice)
    {
        // Refresh so the chip highlights the new active speed.
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
            _savedChip?.QueueFree();
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
        foreach (var child in _row.GetChildren())
        {
            child.QueueFree();
        }
        _savedChip = null;

        BuildClockChip(snapshot);
        BuildUpkeepChip(snapshot);
        BuildResourcesChip(snapshot);
        BuildMobilisationChip(snapshot);

        // Construction is intentionally singular in the current slice. Keep
        // one concise progress chip instead of allowing future projects to
        // grow the status strip horizontally without bound.
        if (snapshot.Projects.Count > 0)
        {
            BuildProjectChip(snapshot.Projects[0]);
        }

        BuildAttentionChip(snapshot);
        BuildFreeCitizensChip(snapshot);

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
        var chip = new IconChip(iconPath, SimulationTimeText.Format(tick));
        chip.TooltipText = SimulationTimeText.Format(tick);
        _row.AddChild(chip);
        if (snapshot.HasController)
        {
            _row.AddChild(BuildSpeedControl((CityWorldController.SpeedChoice)snapshot.CurrentSpeed));
        }
    }

    /// <summary>
    /// Compact horizontal group of speed toggles (Pause / 1× / 2× / 4×).
    /// The active speed is highlighted; clicking any button routes
    /// through the controller so the simulation reacts immediately.
    /// </summary>
    private HBoxContainer BuildSpeedControl(CityWorldController.SpeedChoice current)
    {
        var group = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Pass,
        };
        group.AddThemeConstantOverride("separation", 2);
        AddSpeedButton(group, CityWorldController.SpeedChoice.Paused, "Pause", IconPaths.Pause, current);
        AddSpeedButton(group, CityWorldController.SpeedChoice.Normal, "1×", IconPaths.Play, current);
        AddSpeedButton(group, CityWorldController.SpeedChoice.Fast, "2×", IconPaths.ChevronUp, current);
        AddSpeedButton(group, CityWorldController.SpeedChoice.Fastest, "4×", IconPaths.Expand, current);
        return group;
    }

    private void AddSpeedButton(
        HBoxContainer parent,
        CityWorldController.SpeedChoice choice,
        string label,
        string iconPath,
        CityWorldController.SpeedChoice current)
    {
        var button = new Button
        {
            Text = label,
            TooltipText = choice switch
            {
                CityWorldController.SpeedChoice.Paused => "Pause the simulation.",
                CityWorldController.SpeedChoice.Normal => "Normal speed (1 tick per second).",
                CityWorldController.SpeedChoice.Fast => "Fast (2 ticks per second).",
                CityWorldController.SpeedChoice.Fastest => "Fastest (4 ticks per second).",
                _ => label,
            },
            ThemeTypeVariation = choice == current ? "ButtonPrimary" : "ButtonText",
            CustomMinimumSize = new Vector2(40, 28),
            FocusMode = Control.FocusModeEnum.All,
        };
        if (current == choice)
        {
            button.Disabled = true;
        }
        button.Pressed += () => _controller?.SetSimulationSpeed(choice);
        parent.AddChild(button);
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

    private void BuildProjectChip(CityStatusSnapshot.ProjectItem project)
    {
        var phase = ConstructionRules.PhaseFor(project.Progress, project.RequiredWork);
        string label = $"{project.DisplayName} {project.Progress}/{project.RequiredWork} " +
            $"({ConstructionRules.Describe(phase)}) · {project.AssignedCount}/{project.WorkerCapacity}";
        if (!project.Enabled) label += " · paused";
        var chip = new IconChip(IconPaths.Building, label);
        chip.TooltipText = project.Enabled
            ? $"In progress. Click the construction menu for details."
            : "Paused. Resume from the construction menu.";
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

    public IconChip(string iconPath, string text)
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
            ThemeTypeVariation = "BodySmall",
            VerticalAlignment = VerticalAlignment.Center,
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
