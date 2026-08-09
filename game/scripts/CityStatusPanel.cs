#nullable enable
using System;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Edge-to-edge compact HUD bar: stable brand, world context, authoritative
/// resource availability and population.
/// It consumes <see cref="CityStatusSnapshot"/> only; storage and reservation
/// rules remain in the city domain.
/// </summary>
public partial class CityStatusPanel : PanelContainer
{
    internal const float BrandBlockWidth = 190f;
    internal const float WorldContextWidth = 250f;

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
            MinimumTopInset = 0,
            MinimumBottomInset = 0,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(safeArea);
        safeArea.AddChild(_row);

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
        foreach (var child in _row.GetChildren())
        {
            _row.RemoveChild(child);
            child.QueueFree();
        }
        _savedChip = null;

        _row.AddChild(BuildBrandBlock());
        _row.AddChild(BuildWorldContext(snapshot));
        _row.AddChild(BuildResourceTicker(snapshot));
        _row.AddChild(BuildPopulation(snapshot));
        if (_saveIndicatorVisible) ApplySavedChip();
        ReapplyAccent();
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
        foreach (ResourceInventoryItem resource in snapshot.Resources)
        {
            StatChip chip = StatChip.HudIconValue(
                resource.Resource,
                resource.AvailableAmount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            chip.Name = $"Resource{resource.Resource}";
            chip.TooltipText = BuildResourceTooltip(resource);
            ticker.AddChild(chip);
        }
        return ticker;
    }

    private static string BuildResourceTooltip(ResourceInventoryItem resource)
    {
        string amount = resource.AvailableAmount.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        string total = resource.TotalAmount.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        var lines = new System.Collections.Generic.List<string>
        {
            UiText.Get(resource.Resource.ToString().ToLowerInvariant()),
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
