#nullable enable
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Right-hand panel of the building detail view. Surfaces the
/// building's headline production state — current stock, capacity,
/// rate, pending inputs, and the simple on/off "authorize
/// production" toggle. The reactive <c>MinStock</c>/<c>MaxStock</c>/
/// <c>Priority</c> triplet lives in the domain but is not surfaced:
/// when a future slice re-introduces advanced policy control it can
/// extend <see cref="CityWorld.ConfigureProductionPolicy"/> and bind
/// to <see cref="CityWorldController.ConfigureProductionPolicy"/>
/// without changing this panel's contract.
/// </summary>
public partial class ProductionPanel : PanelContainer
{
    [Signal] public delegate void PolicyChangeRequestedEventHandler(bool enabled);

    private Label _titleLabel = null!;
    private Label _stockLabel = null!;
    private Label _rateLabel = null!;
    private ProgressBar _stockBar = null!;
    private IconButton _enabledToggle = null!;
    private Label _inputsLabel = null!;
    private Label _statusLabel = null!;
    private bool _refreshing;
    private LineageThemeSignals? _themeSignals;

    public override void _Ready()
    {
        var root = new VBoxContainer();
        AddChild(root);
        AddThemeStyleboxOverride("panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
        _themeSignals = GetNodeOrNull<LineageThemeSignals>("/root/LineageThemeSignals");
        if (_themeSignals is not null)
        {
            _themeSignals.LineageChanged += OnLineageChanged;
        }

        _titleLabel = new Label
        {
            Text = "Production",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _titleLabel.ThemeTypeVariation = "PanelTitle";
        root.AddChild(_titleLabel);

        var stockRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        stockRow.AddThemeConstantOverride("separation", 8);
        root.AddChild(stockRow);

        _stockLabel = new Label { Text = "Stock: 0 / 0" };
        _stockLabel.ThemeTypeVariation = "BodyText";
        stockRow.AddChild(_stockLabel);

        _enabledToggle = new IconButton
        {
            IconPath = IconPaths.Pause,
            Label = string.Empty,
            CustomMinimumSize = new Vector2(40, 40),
            FocusMode = Control.FocusModeEnum.All,
            ToggleMode = true,
        };
        _enabledToggle.Toggled += OnPolicyToggle;
        stockRow.AddChild(_enabledToggle);

        _stockBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            Value = 0,
            CustomMinimumSize = new Vector2(0, 12),
        };
        root.AddChild(_stockBar);

        _rateLabel = new Label { Text = "Rate: 0 / tick" };
        _rateLabel.ThemeTypeVariation = "NumericText";
        root.AddChild(_rateLabel);

        _inputsLabel = new Label { Text = "Inputs due: none" };
        _inputsLabel.ThemeTypeVariation = "BodySmall";
        root.AddChild(_inputsLabel);

        _statusLabel = new Label();
        _statusLabel.ThemeTypeVariation = "BodySmall";
        root.AddChild(_statusLabel);
    }

    public override void _ExitTree()
    {
        if (_themeSignals is not null) _themeSignals.LineageChanged -= OnLineageChanged;
    }

    private void OnLineageChanged(string lineage) => AddThemeStyleboxOverride(
        "panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));

    public void Refresh(BuildingDetailSnapshot snapshot)
    {
        _titleLabel.Text = $"{snapshot.DisplayName} — {snapshot.ResourceLabel}";
        _stockLabel.Text = snapshot.IsForest
            ? $"Wood: {snapshot.Stock} / {snapshot.StorageCapacity} (reserve {snapshot.WoodReserve})"
            : $"{snapshot.ResourceLabel}: {snapshot.Stock} / {snapshot.StorageCapacity}";

        _stockBar.MinValue = 0;
        _stockBar.MaxValue = snapshot.StorageCapacity == 0 ? 1 : snapshot.StorageCapacity;
        _stockBar.Value = snapshot.Stock;

        _rateLabel.Text = snapshot.StorageCapacity == 0
            ? "Resting site — no production"
            : snapshot.IsForest
                ? $"Foraging rate: {snapshot.ProductionRate} {snapshot.ResourceUnit} / tick ({snapshot.AssignedCount} workers)"
                : $"Rate: {snapshot.ProductionRate} {snapshot.ResourceUnit} / tick";

        _inputsLabel.Text = DescribeInputsDue(snapshot);
        _statusLabel.Text = DescribePolicyState(snapshot);

        _refreshing = true;
        _enabledToggle.SetPressedNoSignal(snapshot.ProductionEnabled);
        _enabledToggle.SetIconAndLabel(
            snapshot.ProductionEnabled ? IconPaths.Pause : IconPaths.Play,
            string.Empty);
        _enabledToggle.TooltipText = snapshot.ProductionEnabled
            ? "Pause production"
            : "Resume production";
        _refreshing = false;
    }

    private void OnPolicyToggle(bool pressed)
    {
        if (_refreshing) return;
        EmitSignal(SignalName.PolicyChangeRequested, pressed);
    }

    private static string DescribeInputsDue(BuildingDetailSnapshot snapshot)
    {
        if (snapshot.PendingInputs.Count == 0)
        {
            return "Inputs due: none";
        }
        var parts = new System.Collections.Generic.List<string>();
        foreach (var input in snapshot.PendingInputs)
        {
            parts.Add($"{input.Amount} {input.Resource.ToString().ToLowerInvariant()}");
        }
        return $"Inputs due: {string.Join(" + ", parts)}";
    }

    private static string DescribePolicyState(BuildingDetailSnapshot snapshot)
    {
        if (snapshot.StorageCapacity == 0)
        {
            return "Workers rest here between shifts.";
        }
        if (!snapshot.ProductionEnabled) return "Production paused by the player";
        return snapshot.StopCause switch
        {
            ProductionStopCause.Night => "Resting during the night",
            ProductionStopCause.NoWorkers => "Waiting for contributors",
            ProductionStopCause.WorkersExhausted => "Contributors exhausted",
            ProductionStopCause.TargetReached => $"Storage full ({snapshot.Stock} / {snapshot.StorageCapacity})",
            ProductionStopCause.MissingInputs => "Waiting for inputs",
            _ => "Authorised",
        };
    }
}
