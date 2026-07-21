#nullable enable
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Bottom panel of the building detail view. Shows the building's
/// stock, capacity, current production rate, and the reactive
/// production policy (min stock / max stock / priority). Reads the
/// city's pending-input view so the player can see what materials
/// the operating building is missing right now.
/// </summary>
public partial class ProductionPanel : PanelContainer
{
    [Signal] public delegate void PolicyChangeRequestedEventHandler(bool enabled, int minStock, int maxStock, int priority);

    private Label _titleLabel = null!;
    private Label _stockLabel = null!;
    private Label _rateLabel = null!;
    private Label _inputsLabel = null!;
    private ProgressBar _stockBar = null!;
    private CheckButton _enabledToggle = null!;
    private SpinBox _minStockInput = null!;
    private SpinBox _maxStockInput = null!;
    private SpinBox _priorityInput = null!;
    private Label _policyStateLabel = null!;
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

        _stockLabel = new Label { Text = "Stock: 0 / 0" };
        _stockLabel.ThemeTypeVariation = "NumericText";
        root.AddChild(_stockLabel);

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

        root.AddChild(new HSeparator());
        var policyHeader = new Label { Text = "Production policy" };
        policyHeader.ThemeTypeVariation = "SectionTitle";
        root.AddChild(policyHeader);

        _enabledToggle = new CheckButton { Text = "Authorize production", ButtonPressed = true };
        _enabledToggle.ThemeTypeVariation = "BodyText";
        _enabledToggle.Toggled += OnPolicyToggle;
        root.AddChild(_enabledToggle);

        var minRow = new HBoxContainer();
        var minLabel = new Label { Text = "Resume at stock:" };
        minLabel.ThemeTypeVariation = "BodyText";
        minRow.AddChild(minLabel);
        _minStockInput = new SpinBox
        {
            MinValue = 0,
            Step = 1,
            Rounded = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _minStockInput.ValueChanged += _ => OnPolicyInputChanged();
        minRow.AddChild(_minStockInput);
        root.AddChild(minRow);

        var maxRow = new HBoxContainer();
        var maxLabel = new Label { Text = "Stop at stock:" };
        maxLabel.ThemeTypeVariation = "BodyText";
        maxRow.AddChild(maxLabel);
        _maxStockInput = new SpinBox
        {
            MinValue = 0,
            Step = 1,
            Rounded = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _maxStockInput.ValueChanged += _ => OnPolicyInputChanged();
        maxRow.AddChild(_maxStockInput);
        root.AddChild(maxRow);

        var priorityRow = new HBoxContainer();
        var priorityLabel = new Label { Text = "Priority (future auto-assignment):" };
        priorityLabel.ThemeTypeVariation = "BodyText";
        priorityRow.AddChild(priorityLabel);
        _priorityInput = new SpinBox
        {
            MinValue = 0,
            Step = 1,
            Rounded = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _priorityInput.ValueChanged += _ => OnPolicyInputChanged();
        priorityRow.AddChild(_priorityInput);
        root.AddChild(priorityRow);

        _policyStateLabel = new Label();
        _policyStateLabel.ThemeTypeVariation = "BodySmall";
        root.AddChild(_policyStateLabel);
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
        _stockLabel.Text = $"{snapshot.ResourceLabel}: {snapshot.Stock} / {snapshot.StorageCapacity}";

        _stockBar.MinValue = 0;
        _stockBar.MaxValue = snapshot.StorageCapacity == 0 ? 1 : snapshot.StorageCapacity;
        _stockBar.Value = snapshot.Stock;

        _rateLabel.Text = $"Rate: {snapshot.ProductionRate} {snapshot.ResourceUnit} / tick ({snapshot.AssignedCount} workers)";
        _inputsLabel.Text = DescribeInputsDue(snapshot);

        _refreshing = true;
        _enabledToggle.ButtonPressed = snapshot.ProductionEnabled;
        _minStockInput.MaxValue = snapshot.StorageCapacity;
        _maxStockInput.MaxValue = snapshot.StorageCapacity;
        _priorityInput.MinValue = 0;
        _minStockInput.Value = snapshot.MinStock;
        _maxStockInput.Value = snapshot.MaxStock;
        _priorityInput.Value = snapshot.Priority;
        _refreshing = false;

        _policyStateLabel.Text = DescribePolicyState(snapshot);
    }

    private void OnPolicyToggle(bool pressed)
    {
        if (_refreshing) return;
        EmitSignal(SignalName.PolicyChangeRequested,
            pressed,
            (int)_minStockInput.Value,
            (int)_maxStockInput.Value,
            (int)_priorityInput.Value);
    }

    private void OnPolicyInputChanged()
    {
        if (_refreshing) return;
        EmitSignal(SignalName.PolicyChangeRequested,
            _enabledToggle.ButtonPressed,
            (int)_minStockInput.Value,
            (int)_maxStockInput.Value,
            (int)_priorityInput.Value);
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
        if (snapshot.StopCause == ProductionStopCause.Night) return "Workers resting (night)";
        if (!snapshot.ProductionEnabled) return "Paused by player policy";
        if (snapshot.StopCause == ProductionStopCause.MissingInputs)
            return $"Waiting: missing inputs ({string.Join(", ", snapshot.PendingInputs)})";
        if (snapshot.AssignedCount == 0) return "Blocked: no assigned workers";
        if (snapshot.StopCause == ProductionStopCause.WorkersExhausted)
            return "Blocked: workers exhausted";
        if (snapshot.Stock >= snapshot.MaxStock) return $"Full at {snapshot.MaxStock} {snapshot.ResourceUnit}";
        return $"Authorized between {snapshot.MinStock} and {snapshot.MaxStock} {snapshot.ResourceUnit}";
    }
}
