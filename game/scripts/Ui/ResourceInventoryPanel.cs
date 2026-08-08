#nullable enable
using System.Collections.Generic;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Collapsible contextual resource inventory. It consumes a snapshot and
/// never queries or mutates the world directly.
/// </summary>
public partial class ResourceInventoryPanel : PanelContainer
{
    private const int RowHeight = 30;
    private readonly VBoxContainer _rows = new();
    private readonly Label _capacity = new();
    private readonly IconButton _toggle = new();
    private bool _expanded;
    private int _resourceCount;
    private ResourceInventoryOwner _owner;

    public ResourceInventoryPanel(bool expandedByDefault = false)
    {
        _expanded = expandedByDefault;
        Name = "ResourceInventory";
        AddThemeStyleboxOverride(
            "panel",
            LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));

        var shell = new VBoxContainer();
        shell.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        AddChild(shell);

        _toggle.ThemeTypeVariation = "ButtonText";
        _toggle.CustomMinimumSize = new Vector2(0, 40);
        _toggle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _toggle.FocusMode = FocusModeEnum.All;
        _toggle.Pressed += ToggleExpanded;
        shell.AddChild(_toggle);

        _capacity.ThemeTypeVariation = "BodySmall";
        _capacity.HorizontalAlignment = HorizontalAlignment.Center;
        _capacity.AddThemeColorOverride("font_color", LineageThemeRegistry.IconAccent);
        shell.AddChild(_capacity);

        _rows.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        shell.AddChild(_rows);
        ApplyExpandedState();
    }

    public void Render(
        IReadOnlyList<ResourceInventoryItem> resources,
        int usedCapacity,
        int totalCapacity,
        ResourceInventoryOwner owner)
    {
        _owner = owner;
        foreach (Node child in _rows.GetChildren())
        {
            _rows.RemoveChild(child);
            child.QueueFree();
        }

        _resourceCount = 0;
        foreach (ResourceInventoryItem item in resources)
        {
            _rows.AddChild(BuildRow(item));
            if (item.TotalAmount > 0) _resourceCount++;
        }
        _capacity.Text = UiText.Format(CapacityKey(owner), usedCapacity, totalCapacity);
        ApplyExpandedState();
    }

    private Control BuildRow(ResourceInventoryItem item)
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, RowHeight),
            MouseFilter = MouseFilterEnum.Pass,
        };
        row.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        row.AddChild(new ResourceIcon(item.Resource));
        row.AddChild(new Label
        {
            Text = UiText.Get(item.Resource.ToString().ToLowerInvariant()),
            ThemeTypeVariation = "BodyText",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        });
        row.AddChild(new Label
        {
            Text = item.TotalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ThemeTypeVariation = "SectionTitle",
            CustomMinimumSize = new Vector2(52, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        });
        if (item.AvailableAmount < item.TotalAmount)
        {
            row.TooltipText = UiText.Format(
                "ui.shelter_storage.reserved",
                item.TotalAmount - item.AvailableAmount,
                item.AvailableAmount);
        }
        return row;
    }

    private void ToggleExpanded()
    {
        _expanded = !_expanded;
        ApplyExpandedState();
    }

    internal void SetExpandedForVisualRegression(bool expanded)
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        _expanded = expanded;
        ApplyExpandedState();
    }

    private void ApplyExpandedState()
    {
        _rows.Visible = _expanded;
        _capacity.Visible = _expanded;
        _toggle.SetIconAndLabel(
            _expanded ? IconPaths.ChevronUp : IconPaths.ChevronDown,
            UiText.Format(ToggleKey(_owner, _expanded), _resourceCount));
        _toggle.TooltipText = UiText.Get(
            _expanded
                ? "ui.resource_inventory.collapse_tooltip"
                : "ui.resource_inventory.expand_tooltip");
    }

    private static string CapacityKey(ResourceInventoryOwner owner) => owner switch
    {
        ResourceInventoryOwner.FounderCargo => "ui.founding_storage.cargo_capacity",
        ResourceInventoryOwner.FoundingCache => "ui.founding_storage.cache_capacity",
        _ => "ui.shelter_storage.capacity",
    };

    private static string ToggleKey(ResourceInventoryOwner owner, bool expanded) =>
        (owner, expanded) switch
        {
            (ResourceInventoryOwner.FounderCargo, true) => "ui.founding_storage.cargo_collapse",
            (ResourceInventoryOwner.FounderCargo, false) => "ui.founding_storage.cargo_expand",
            (ResourceInventoryOwner.FoundingCache, true) => "ui.founding_storage.cache_collapse",
            (ResourceInventoryOwner.FoundingCache, false) => "ui.founding_storage.cache_expand",
            (_, true) => "ui.shelter_storage.collapse",
            _ => "ui.shelter_storage.expand",
        };
}
