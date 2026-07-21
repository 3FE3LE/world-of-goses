#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Minimal showcase scene used during development to inspect the
/// eight lineage themes. Switches the active lineage through the
/// <see cref="LineageThemeRegistry"/> and applies the resolved
/// <see cref="StyleBox"/> to its own PanelContainer.
/// </summary>
public partial class LineageShowcase : PanelContainer
{
    [Export] public NodePath LabelPath { get; set; } = "Margin/Content/LineageLabel";
    [Export] public NodePath BarPath { get; set; } = "Margin/Content/Bar";
    [Export] public NodePath GridPath { get; set; } = "Margin/Content/Components/Grid";

    private static readonly string[] ComponentIds =
    {
        LineageThemeRegistry.ComponentPanel,
        LineageThemeRegistry.ComponentPanelInset,
        LineageThemeRegistry.ComponentButton,
        LineageThemeRegistry.ComponentButtonPrimary,
        LineageThemeRegistry.ComponentButtonSecondary,
        LineageThemeRegistry.ComponentTooltip,
        LineageThemeRegistry.ComponentModal,
        LineageThemeRegistry.ComponentStatusBar,
        LineageThemeRegistry.ComponentSidebar,
        LineageThemeRegistry.ComponentTab,
        LineageThemeRegistry.ComponentResourceChip,
        LineageThemeRegistry.ComponentProgressBar,
        LineageThemeRegistry.ComponentPortraitFrame,
        LineageThemeRegistry.ComponentIconContainer,
        LineageThemeRegistry.ComponentSelectionFrame,
        LineageThemeRegistry.ComponentDivider,
    };

    private Label _label = null!;
    private HBoxContainer _bar = null!;
    private GridContainer _grid = null!;
    private readonly List<(TooltipPanelContainer Panel, string Component)> _surfaces = new();
    private LineageThemeSignals? _signals;

    public override void _Ready()
    {
        _label = GetNode<Label>(LabelPath);
        _bar = GetNode<HBoxContainer>(BarPath);
        _grid = GetNode<GridContainer>(GridPath);
        if (LineageThemeRegistry.ActiveLineage == LineageThemeRegistry.SystemDefaultLineage)
        {
            LineageThemeRegistry.ActiveLineage = "ardhen";
        }
        BuildComponentGrid();
        foreach (string lineage in LineageThemeRegistry.AvailableLineages.OrderBy(value => value))
        {
            // Loading each exact resource makes opening this scene an integration check
            // for every exported bundle, independent of the active save slot.
            _ = LineageThemeRegistry.GetStyleBox(lineage, LineageThemeRegistry.ComponentPanel);
            var button = new Button
            {
                Text = lineage,
                ThemeTypeVariation = "ButtonText",
                FocusMode = FocusModeEnum.All,
            };
            string captured = lineage;
            button.Pressed += () => LineageThemeRegistry.ActiveLineage = captured;
            _bar.AddChild(button);
        }
        _signals = GetNodeOrNull<LineageThemeSignals>("/root/LineageThemeSignals");
        if (_signals is not null)
        {
            _signals.LineageChanged += OnLineageChanged;
        }
        UpdateLabel(LineageThemeRegistry.ActiveLineage);
    }

    public override void _ExitTree()
    {
        if (_signals is not null)
        {
            _signals.LineageChanged -= OnLineageChanged;
        }
    }

    private void OnLineageChanged(string lineage) => UpdateLabel(lineage);

    private void UpdateLabel(string lineage)
    {
        AddThemeStyleboxOverride("panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
        _label.Text = $"Lineage theme: {lineage}";
        foreach (var surface in _surfaces)
        {
            surface.Panel.AddThemeStyleboxOverride(
                "panel", LineageThemeRegistry.GetStyleBox(surface.Component));
        }
    }

    private void BuildComponentGrid()
    {
        foreach (string component in ComponentIds)
        {
            var surface = new TooltipPanelContainer
            {
                CustomMinimumSize = new Vector2(220, 72),
                TooltipText = component == LineageThemeRegistry.ComponentPanel
                    ? "Exact lineage component"
                    : $"Fallback: {component} → panel",
            };
            var label = new Label
            {
                Text = component,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ThemeTypeVariation = "BodySmall",
            };
            surface.AddChild(label);
            _grid.AddChild(surface);
            _surfaces.Add((surface, component));
        }
    }
}
