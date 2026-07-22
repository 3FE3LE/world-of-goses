#nullable enable
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

    public override void _ExitTree()
    {
        if (_themeSignals is not null) _themeSignals.LineageChanged -= OnLineageChanged;
        LineageThemeRegistry.ActiveLineageChanged -= OnLineageAccentChanged;
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

        BuildClockChip(snapshot);
        BuildUpkeepChip(snapshot);
        BuildFoodChip(snapshot);
        BuildWoodChip(snapshot);
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
    }

    private void BuildClockChip(CityStatusSnapshot snapshot)
    {
        int tick = snapshot.CurrentTick;
        bool day = GameClock.IsDaytime(tick);
        string iconPath = day ? IconPaths.Sun : IconPaths.Moon;
        _row.AddChild(new IconChip(iconPath, SimulationTimeText.Format(tick)));
    }

    private void BuildUpkeepChip(CityStatusSnapshot snapshot)
    {
        int rate = snapshot.UpkeepPerTick;
        if (rate <= 0) return;
        _row.AddChild(new IconChip(IconPaths.Coin, $"-{rate} stone/tick (upkeep)"));
    }

    private void BuildFoodChip(CityStatusSnapshot snapshot)
    {
        int food = snapshot.FoodStock;
        int cap = snapshot.MaxFoodStock;
        if (cap <= 0) return;
        _row.AddChild(new IconChip(IconPaths.Leaf, $"Food: {food} / {cap}"));
    }

    private void BuildWoodChip(CityStatusSnapshot snapshot)
    {
        int stock = snapshot.WoodStock;
        int reserve = snapshot.WoodReserve;
        if (stock == 0 && reserve == 0) return;
        _row.AddChild(new IconChip(
            IconPaths.Tree,
            reserve > 0
                ? $"Wood: {stock} gathered · {reserve} in forests"
                : $"Wood: {stock} gathered"));
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
        _row.AddChild(new IconChip(IconPaths.Building, label));
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
        _row.AddChild(new IconChip(
            IconPaths.User,
            $"Free citizens: {snapshot.FreeCitizenNames.Count}"));
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

    public IconChip(string iconPath, string text)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        CustomMinimumSize = new Vector2(0, ChipHeight);
        AddThemeConstantOverride("separation", IconTextGap);

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

        var label = new Label
        {
            Text = text,
            ThemeTypeVariation = "BodySmall",
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(label);
    }
}
