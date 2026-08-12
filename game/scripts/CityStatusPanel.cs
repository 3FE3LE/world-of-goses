#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Edge-to-edge compact HUD bar: stable brand, world context, authoritative
/// resource availability, population, and a right-edge utility cluster that
/// carries the camera-mode toggle and the menu/pause open button.
/// It consumes <see cref="CityStatusSnapshot"/> only; storage and reservation
/// rules remain in the city domain.
///
/// <para>
/// A3 surface: the brand block, world-context block, resource ticker, and
/// population chip are built <b>once</b> and updated in place via
/// <see cref="ApplySnapshot"/>. A world tick never restructures the row's
/// SceneTree — it only mutates existing labels, icons, tooltips, and
/// visibility. The persistent utility cluster (camera/menu/speed) keeps its
/// typed accessor contract.
/// </para>
/// </summary>
public partial class CityStatusPanel : PanelContainer
{
    internal const float BrandBlockWidth = 190f;
    internal const float WorldContextWidth = 250f;

    /// <summary>
    /// Maximum number of resource chips the ticker shows inline. Resources
    /// beyond this cap are exposed through a single "+N" overflow affordance
    /// whose tooltip lists each hidden resource with its exact amount.
    /// </summary>
    /// <remarks>
    /// The cap is a presentation constant, not a layout measurement: the
    /// ticker's parent row has no fixed width the way the rest of the HUD
    /// does, and trying to derive a cap from <c>GetRect()</c> on a child that
    /// is itself not yet laid out (sibling-order trap) reintroduced the
    /// flicker the old clipped-ticker already suffered. A fixed cap plus an
    /// explicit overflow affordance is also what the brief asked for:
    /// "deliberate overflow behaviour BEFORE the resource catalog grows".
    /// When the catalog gains a tenth resource, the only change is the cap.
    /// </remarks>
    internal const int MaxVisibleResourceChips = 5;

    private HBoxContainer _row = null!;
    private HBoxContainer? _utilityCluster;
    private IconButton? _cameraButton;
    private IconButton? _menuButton;
    private SpeedButton? _speedButton;
    private StatChip? _savedChip;
    private ulong _saveIndicatorGeneration;
    private ulong _emphasizedSaveGeneration;
    private bool _saveIndicatorVisible;
    private CityWorldController? _controller;

    // Persistent block references — created once in EnsureBuiltSurface,
    // mutated in place by ApplySnapshot. No QueueFree per tick.
    private Label _brandLabel = null!;
    private Label _worldContextLabel = null!;
    private TextureRect _worldContextIcon = null!;
    private Control _worldContextBlock = null!;
    private HBoxContainer _resourceTicker = null!;
    private StatChip _populationChip = null!;
    private StatChip? _resourceOverflowChip;
    private readonly Dictionary<ResourceType, StatChip> _resourceChips = new();

    /// <summary>
    /// Typed accessor for the camera-mode toggle. Owned by the right-edge
    /// utility cluster; the macro view's <c>UpdateCameraModeButtonLabel</c>
    /// keeps its icon, tooltip and selected-state theme variation fresh.
    /// </summary>
    public IconButton CameraButton
    {
        get { EnsureUtilityClusterBuilt(); return _cameraButton!; }
    }

    /// <summary>
    /// Typed accessor for the menu/pause open button. The macro view hooks
    /// this into the existing pause-menu toggle.
    /// </summary>
    public IconButton MenuButton
    {
        get { EnsureUtilityClusterBuilt(); return _menuButton!; }
    }

    /// <summary>
    /// Typed accessor for the speed control. Lives in the right-edge
    /// utility cluster so the speed affordance shares the same dock
    /// chrome as Camera and Menu and no longer floats in a separate
    /// bottom-right surface.
    /// </summary>
    public SpeedButton SpeedButton
    {
        get { EnsureUtilityClusterBuilt(); return _speedButton!; }
    }

    public override void _Ready()
    {
        // HUD chrome: stay above the ambient day/night tint so the status
        // strip keeps its authored contrast at every in-game hour.
        OverlayLayers.Apply(this, OverlayLayers.Hud);
        EnsureBuilt();
    }

    /// <summary>
    /// Creates the row, persistent blocks, and wires subscriptions the first
    /// time it runs. Safe to call multiple times — idempotent. Exists so
    /// that an early <see cref="Refresh"/> from a sibling that was
    /// instantiated before us doesn't crash on a null <c>_row</c>.
    /// </summary>
    private void EnsureBuilt()
    {
        if (_row is not null) return;

        _row = new HBoxContainer
        {
            Name = "StatusComposition",
            Alignment = BoxContainer.AlignmentMode.Begin,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _row.AddThemeConstantOverride("separation", Tokens.SpacingComfortable);
        // The status bar surface spans the full width of the GameUiShell
        // VBox; wrap the chip row in a SafeAreaMarginContainer so the
        // chip content stays inside the OS safe area on notched or
        // rounded displays. Wrapping the OUTER panel with a margin
        // container previously rendered a visible grey band and was
        // reverted (TO_DO.md 2026-07-22).
        var safeArea = new SafeAreaMarginContainer
        {
            Name = "SafeArea",
            MinimumTopInset = 0,
            MinimumBottomInset = 0,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(safeArea);
        safeArea.AddChild(_row);

        EnsureUtilityClusterBuilt();
        EnsureSurfaceBuilt();

        LineageThemeRegistry.ActiveLineageChanged += OnLineageAccentChanged;
    }

    /// <summary>
    /// Builds the persistent block structure (brand, world context, resource
    /// ticker, population) exactly once. <see cref="ApplySnapshot"/> mutates
    /// text/icon/visibility on these existing nodes; it never replaces them.
    /// </summary>
    private void EnsureSurfaceBuilt()
    {
        if (_brandLabel is not null) return;

        _brandLabel = new Label
        {
            Name = "BrandBlock",
            Text = "WORLD OF GOSES",
            ThemeTypeVariation = "HudBrand",
            CustomMinimumSize = new Vector2(BrandBlockWidth, Tokens.HudRowHeight),
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };

        _worldContextLabel = new Label
        {
            ThemeTypeVariation = "HudBody",
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _worldContextIcon = new TextureRect
        {
            StretchMode = TextureRect.StretchModeEnum.Keep,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = LineageThemeRegistry.IconAccent,
        };
        var contextRow = new HBoxContainer
        {
            Name = "WorldContextRow",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        contextRow.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        contextRow.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        contextRow.AddChild(_worldContextLabel);
        contextRow.AddChild(_worldContextIcon);
        _worldContextBlock = new Control
        {
            Name = "WorldContext",
            CustomMinimumSize = new Vector2(WorldContextWidth, Tokens.HudRowHeight),
            ClipContents = true,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _worldContextBlock.AddChild(contextRow);

        _resourceTicker = new HBoxContainer
        {
            Name = "ResourceTicker",
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ClipContents = true,
            MouseFilter = MouseFilterEnum.Pass,
        };
        _resourceTicker.AddThemeConstantOverride("separation", Tokens.SpacingBase);

        _populationChip = StatChip.HudIconValue(IconPaths.Users, "0");
        _populationChip.Name = "Population";
        _populationChip.TooltipText = UiText.Get("ui.status.population");

        _row.AddChild(_brandLabel);
        _row.AddChild(_worldContextBlock);
        _row.AddChild(_resourceTicker);
        _row.AddChild(_populationChip);

        // The utility cluster was added first by EnsureUtilityClusterBuilt,
        // so move it back to the rightmost position now that the persistent
        // blocks are in place.
        if (_utilityCluster is not null)
        {
            _row.MoveChild(_utilityCluster, _row.GetChildCount() - 1);
        }
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
        if (_controller is not null)
        {
            _controller.WorldSaved -= OnWorldSaved;
        }
        LineageThemeRegistry.ActiveLineageChanged -= OnLineageAccentChanged;
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
            _savedChip = StatChip.HudIconValue(IconPaths.Check, text);
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

    private void OnLineageAccentChanged(string lineage) => ReapplyAccent();

    /// <summary>
    /// Walks every chip currently in the row and re-tints its leading
    /// icon with the active linaje's accent. Called once on _Ready and
    /// again whenever the active lineage changes.
    /// </summary>
    private void ReapplyAccent()
    {
        if (_row is null) return;
        var accent = LineageThemeRegistry.IconAccent;
        TintTextureRects(_row, accent);
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
        ApplySnapshot(snapshot);
        if (_saveIndicatorVisible) ApplySavedChip();
    }

    /// <summary>
    /// Mutates the existing persistent structure to reflect the latest
    /// snapshot. Replaces the previous rebuild-per-tick SceneTree churn
    /// with in-place label/icon/visibility updates. The
    /// <see cref="_resourceChips"/> dictionary carries a chip per
    /// resource so adding or removing a single resource only touches
    /// one entry; chips not in the snapshot are hidden rather than freed
    /// so the focus chain and tooltips remain stable.
    /// </summary>
    private void ApplySnapshot(CityStatusSnapshot snapshot)
    {
        // World context: text + sun/moon icon flip is the only thing
        // that changes frame-to-frame.
        string time = SimulationTimeText.FormatLocalized(snapshot.CurrentTick);
        string context = string.IsNullOrWhiteSpace(snapshot.LineageName)
            ? time
            : UiText.Format("ui.status.world_context", snapshot.LineageName, time);
        _worldContextLabel.Text = context;
        _worldContextBlock.TooltipText = snapshot.IsLaborTime
            ? context
            : context + "\n" + UiText.Get("ui.status.off_hours_hint");
        string iconPath = GameClock.IsDaytime(snapshot.CurrentTick)
            ? IconPaths.Sun
            : IconPaths.Moon;
        _worldContextIcon.Texture = LoadIconCached(iconPath);
        _worldContextIcon.Modulate = LineageThemeRegistry.IconAccent;

        // Population: a single StatChip, mutated in place.
        string popValue = snapshot.HousingCapacity > 0
            ? $"{snapshot.CitizenCount}/{snapshot.HousingCapacity}"
            : snapshot.CitizenCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _populationChip.UpdateText(popValue);
        _populationChip.TooltipText = snapshot.HousingCapacity > 0
            ? UiText.Format(
                "ui.status.population_with_capacity",
                snapshot.CitizenCount,
                snapshot.HousingCapacity)
            : UiText.Get("ui.status.population");

        // Resource ticker: a fixed cap of slots, hidden/shown rather than
        // added/removed. Newly visible resources are added to
        // _resourceChips; resources that drop out of the snapshot are
        // hidden and remain in the dictionary so a later reappearance
        // is cheap.
        System.Collections.Generic.IReadOnlyList<ResourceInventoryItem> ordered =
            ResourcePriority.Prioritize(snapshot.Resources);
        int visibleCount = System.Math.Min(ordered.Count, MaxVisibleResourceChips);
        var seen = new HashSet<ResourceType>();
        for (int i = 0; i < visibleCount; i++)
        {
            ResourceInventoryItem resource = ordered[i];
            seen.Add(resource.Resource);
            if (!_resourceChips.TryGetValue(resource.Resource, out StatChip? chip))
            {
                chip = StatChip.HudIconValue(
                    resource.Resource,
                    CompactNumber.Format(resource.AvailableAmount));
                chip.Name = $"Resource{resource.Resource}";
                chip.TooltipText = BuildResourceTooltip(resource);
                _resourceTicker.AddChild(chip);
                _resourceChips[resource.Resource] = chip;
            }
            chip.Visible = true;
            chip.UpdateText(CompactNumber.Format(resource.AvailableAmount));
            chip.TooltipText = BuildResourceTooltip(resource);
            _resourceTicker.MoveChild(chip, i);
        }
        // Hide any chip whose resource is no longer in the snapshot.
        foreach (var pair in _resourceChips)
        {
            if (!seen.Contains(pair.Key)) pair.Value.Visible = false;
        }

        // Overflow affordance: created lazily on first need, hidden
        // again when it has nothing to show.
        if (ordered.Count > visibleCount)
        {
            var hidden = new System.Collections.Generic.List<ResourceInventoryItem>(
                ordered.Count - visibleCount);
            for (int i = visibleCount; i < ordered.Count; i++)
            {
                hidden.Add(ordered[i]);
            }
            if (_resourceOverflowChip is null)
            {
                _resourceOverflowChip = StatChip.HudIconValue(
                    IconPaths.Backpack,
                    "+" + hidden.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
                _resourceOverflowChip.Name = "ResourceOverflow";
                _resourceTicker.AddChild(_resourceOverflowChip);
            }
            else
            {
                _resourceOverflowChip.UpdateText(
                    "+" + hidden.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            _resourceOverflowChip.Visible = true;
            _resourceOverflowChip.TooltipText = BuildOverflowTooltip(hidden);
        }
        else if (_resourceOverflowChip is not null)
        {
            _resourceOverflowChip.Visible = false;
        }
    }

    private static Texture2D? LoadIconCached(string path)
    {
        if (IconPathCache.TryGetValue(path, out Texture2D? cached)) return cached;
        Texture2D? loaded = ResourceLoader.Load<Texture2D>(path);
        IconPathCache[path] = loaded;
        return loaded;
    }

    private static readonly System.Collections.Generic.Dictionary<string, Texture2D?> IconPathCache = new();

    /// <summary>
    /// Builds the right-edge utility cluster exactly once. Subsequent calls
    /// are idempotent. The cluster is persistent across <see cref="Refresh"/>
    /// because the macro view caches typed references to its buttons.
    /// </summary>
    private void EnsureUtilityClusterBuilt()
    {
        if (_utilityCluster is not null) return;

        _utilityCluster = new HBoxContainer
        {
            Name = "UtilityCluster",
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _utilityCluster.AddThemeConstantOverride("separation", Tokens.SpacingTight);

        _cameraButton = new IconButton
        {
            Name = "CameraButton",
            ThemeTypeVariation = "HudButton",
            FocusMode = FocusModeEnum.All,
            MouseFilter = MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(Tokens.ControlHeight, Tokens.ControlHeight),
            ShowLabel = false,
        };

        _menuButton = new IconButton
        {
            Name = "MenuButton",
            IconPath = IconPaths.Menu,
            ButtonText = UiText.Get("ui.nav.menu_short"),
            TooltipText = UiText.Get("ui.pause.open"),
            ThemeTypeVariation = "HudButton",
            FocusMode = FocusModeEnum.All,
            MouseFilter = MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(Tokens.ControlHeight, Tokens.ControlHeight),
            ShowLabel = false,
        };

        _speedButton = new SpeedButton
        {
            Name = "SpeedButton",
            ThemeTypeVariation = "HudButton",
            FocusMode = FocusModeEnum.All,
            MouseFilter = MouseFilterEnum.Stop,
        };

        // Camera first (anchor for the cluster), Speed in the middle,
        // Menu at the end where the original utility cluster lived.
        _utilityCluster.AddChild(_cameraButton);
        _utilityCluster.AddChild(_speedButton);
        _utilityCluster.AddChild(_menuButton);
        _row.AddChild(_utilityCluster);
    }

    private static Label BuildBrandBlock() => new()
    {
        Name = "BrandBlock",
        Text = "WORLD OF GOSES",
        ThemeTypeVariation = "HudBrand",
        CustomMinimumSize = new Vector2(BrandBlockWidth, Tokens.HudRowHeight),
        VerticalAlignment = VerticalAlignment.Center,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    private static Control BuildWorldContext(CityStatusSnapshot snapshot)
    {
        string time = SimulationTimeText.FormatLocalized(snapshot.CurrentTick);
        string context = string.IsNullOrWhiteSpace(snapshot.LineageName)
            ? time
            : UiText.Format("ui.status.world_context", snapshot.LineageName, time);
        var wrap = new Control
        {
            Name = "WorldContext",
            CustomMinimumSize = new Vector2(WorldContextWidth, Tokens.HudRowHeight),
            ClipContents = true,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        var contextRow = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        contextRow.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        contextRow.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        contextRow.AddChild(new Label
        {
            Text = context,
            ThemeTypeVariation = "HudBody",
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        });
        contextRow.AddChild(new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(
                GameClock.IsDaytime(snapshot.CurrentTick) ? IconPaths.Sun : IconPaths.Moon),
            StretchMode = TextureRect.StretchModeEnum.Keep,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(Tokens.IconInline, Tokens.IconInline),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = LineageThemeRegistry.IconAccent,
        });
        wrap.TooltipText = snapshot.IsLaborTime
            ? context
            : context + "\n" + UiText.Get("ui.status.off_hours_hint");
        wrap.AddChild(contextRow);
        return wrap;
    }

    private static HBoxContainer BuildResourceTicker(CityStatusSnapshot snapshot)
    {
        var ticker = new HBoxContainer
        {
            Name = "ResourceTicker",
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ClipContents = true,
            MouseFilter = MouseFilterEnum.Pass,
        };
        ticker.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        System.Collections.Generic.IReadOnlyList<ResourceInventoryItem> ordered =
            ResourcePriority.Prioritize(snapshot.Resources);
        int visibleCount = System.Math.Min(ordered.Count, MaxVisibleResourceChips);
        for (int i = 0; i < visibleCount; i++)
        {
            ResourceInventoryItem resource = ordered[i];
            StatChip chip = StatChip.HudIconValue(
                resource.Resource,
                CompactNumber.Format(resource.AvailableAmount));
            chip.Name = $"Resource{resource.Resource}";
            chip.TooltipText = BuildResourceTooltip(resource);
            ticker.AddChild(chip);
        }
        if (ordered.Count > visibleCount)
        {
            var hidden = new System.Collections.Generic.List<ResourceInventoryItem>(ordered.Count - visibleCount);
            for (int i = visibleCount; i < ordered.Count; i++)
            {
                hidden.Add(ordered[i]);
            }
            StatChip overflow = BuildResourceOverflowChip(hidden);
            ticker.AddChild(overflow);
        }
        return ticker;
    }

    private static StatChip BuildResourceOverflowChip(
        System.Collections.Generic.IReadOnlyList<ResourceInventoryItem> hidden)
    {
        StatChip chip = StatChip.HudIconValue(IconPaths.Backpack, "+" + hidden.Count.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        chip.Name = "ResourceOverflow";
        chip.TooltipText = BuildOverflowTooltip(hidden);
        return chip;
    }

    private static string BuildOverflowTooltip(
        System.Collections.Generic.IReadOnlyList<ResourceInventoryItem> hidden)
    {
        var lines = new System.Collections.Generic.List<string>
        {
            UiText.Get("ui.status.resource_overflow_label"),
        };
        foreach (ResourceInventoryItem resource in hidden)
        {
            string name = ResourceTypeLocalizer.Label(resource.Resource);
            string amount = CompactNumber.FormatExact(resource.AvailableAmount);
            lines.Add(UiText.Format("ui.status.resource_overflow_line", name, amount));
        }
        return string.Join("\n", lines);
    }

    private static string BuildResourceTooltip(ResourceInventoryItem resource)
    {
        // Tooltips must show the exact amount even when the chip uses the
        // compact form (1.2K vs. 1,200). A player deciding whether to
        // dispatch an expedition cannot afford to round.
        string amount = CompactNumber.FormatExact(resource.AvailableAmount);
        string total = CompactNumber.FormatExact(resource.TotalAmount);
        var lines = new System.Collections.Generic.List<string>
        {
            ResourceTypeLocalizer.Label(resource.Resource),
            UiText.Format("ui.status.resource_available", amount),
            UiText.Format("ui.status.resource_stored", total),
        };
        int reserved = resource.TotalAmount - resource.AvailableAmount;
        if (reserved > 0)
        {
            lines.Add(UiText.Format("ui.status.resource_reserved", reserved));
        }
        return string.Join("\n", lines);
    }

    private static StatChip BuildPopulation(CityStatusSnapshot snapshot)
    {
        string value = snapshot.HousingCapacity > 0
            ? $"{snapshot.CitizenCount}/{snapshot.HousingCapacity}"
            : snapshot.CitizenCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        StatChip chip = StatChip.HudIconValue(IconPaths.Users, value);
        chip.Name = "Population";
        chip.TooltipText = snapshot.HousingCapacity > 0
            ? UiText.Format(
                "ui.status.population_with_capacity",
                snapshot.CitizenCount,
                snapshot.HousingCapacity)
            : UiText.Format("ui.status.population", snapshot.CitizenCount);
        return chip;
    }

}
