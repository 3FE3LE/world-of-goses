#nullable enable
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Detail-view panel for a <see cref="BuildingKind.Forest"/> plot.
/// Shows the remaining reserve and the gathered-and-available stock,
/// with a "Gather wood" button that drains reserve into stock.
/// </summary>
public partial class ForestGatherPanel : PanelContainer
{
    [Signal] public delegate void GatherRequestedEventHandler(int forestId);

    private Label _titleLabel = null!;
    private Label _reserveLabel = null!;
    private Label _stockLabel = null!;
    private Button _gatherButton = null!;
    private Label _helperLabel = null!;
    private LineageThemeSignals? _themeSignals;

    public override void _Ready()
    {
        var root = new VBoxContainer();
        AddChild(root);
        AddThemeStyleboxOverride(
            "panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
        _themeSignals = GetNodeOrNull<LineageThemeSignals>("/root/LineageThemeSignals");
        if (_themeSignals is not null)
        {
            _themeSignals.LineageChanged += OnLineageChanged;
        }

        _titleLabel = new Label
        {
            Text = "Forest",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _titleLabel.ThemeTypeVariation = "PanelTitle";
        root.AddChild(_titleLabel);

        _reserveLabel = new Label
        {
            Text = "Reserve: 0 wood",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _reserveLabel.ThemeTypeVariation = "NumericText";
        root.AddChild(_reserveLabel);

        _stockLabel = new Label
        {
            Text = "Available: 0 wood",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _stockLabel.ThemeTypeVariation = "NumericText";
        root.AddChild(_stockLabel);

        _gatherButton = new Button
        {
            Text = "Gather wood",
            ThemeTypeVariation = "ButtonPrimary",
            CustomMinimumSize = new Vector2(0, 44),
            FocusMode = FocusModeEnum.All,
        };
        _gatherButton.Pressed += () =>
        {
            if (CurrentForestId is { } id)
            {
                EmitSignal(SignalName.GatherRequested, id.Value);
            }
        };
        root.AddChild(_gatherButton);

        _helperLabel = new Label
        {
            Text = "Click to gather 2 wood from this forest.",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(1, 1, 1, 0.7f),
        };
        _helperLabel.ThemeTypeVariation = "BodySmall";
        root.AddChild(_helperLabel);
    }

    public override void _ExitTree()
    {
        if (_themeSignals is not null) _themeSignals.LineageChanged -= OnLineageChanged;
    }

    private void OnLineageChanged(string lineage) => AddThemeStyleboxOverride(
        "panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));

    /// <summary>How much wood one click of "Gather wood" extracts.</summary>
    public const int GatherAmount = 2;

    private BuildingId? CurrentForestId { get; set; }

    public void Refresh(BuildingDetailSnapshot forest)
    {
        if (!forest.IsForest)
        {
            CurrentForestId = null;
            return;
        }
        CurrentForestId = forest.Id;
        _titleLabel.Text = $"Forest · {forest.DisplayName}";
        _reserveLabel.Text = $"Reserve: {forest.WoodReserve} wood";
        _stockLabel.Text = $"Available: {forest.Stock} wood";
        _gatherButton.Disabled = forest.WoodReserve <= 0;
        _helperLabel.Text = forest.WoodReserve > 0
            ? $"Click to gather {GatherAmount} wood from this forest."
            : "This forest is exhausted.";
    }
}
