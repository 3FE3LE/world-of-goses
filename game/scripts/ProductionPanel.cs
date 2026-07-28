#nullable enable
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

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
    [Signal] public delegate void PolicyConfigureRequestedEventHandler(int minStock, int maxStock);

    private const int MinStockFloor = 0;
    private const int MaxStockCeiling = 999;

    private Label _titleLabel = null!;
    private Label _stockLabel = null!;
    private Label _rateLabel = null!;
    private ProgressBar _stockBar = null!;
    private IconButton _enabledToggle = null!;
    private Label _inputsLabel = null!;
    private Label _statusLabel = null!;
    private SpinBox _minStockBox = null!;
    private SpinBox _maxStockBox = null!;
    private Label _policyErrorLabel = null!;
    private LineageThemeSignals? _themeSignals;
    private bool _refreshing;

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
            Text = UiText.Get("Production"),
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

        _stockLabel = new Label { Text = string.Empty };
        _stockLabel.ThemeTypeVariation = "BodyText";
        stockRow.AddChild(_stockLabel);

        _enabledToggle = StandardButtons.IconAction(
            IconPaths.Pause,
            UiText.Get("Pause"),
            tooltip: UiText.Get("Pause production"));
        _enabledToggle.ToggleMode = true;
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

        _rateLabel = new Label { Text = string.Empty };
        _rateLabel.ThemeTypeVariation = "NumericText";
        root.AddChild(_rateLabel);

        _inputsLabel = new Label { Text = UiText.Get("Inputs due: none") };
        _inputsLabel.ThemeTypeVariation = "BodySmall";
        root.AddChild(_inputsLabel);

        _statusLabel = new Label();
        _statusLabel.ThemeTypeVariation = "BodySmall";
        root.AddChild(_statusLabel);

        var policySeparator = new HSeparator();
        root.AddChild(policySeparator);

        var policyHeader = new Label
        {
            Text = UiText.Get("Reactive policy"),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        policyHeader.ThemeTypeVariation = "PanelTitle";
        root.AddChild(policyHeader);

        var policyRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        policyRow.AddThemeConstantOverride("separation", 12);
        root.AddChild(policyRow);

        _minStockBox = BuildPolicyBox("Min");
        _maxStockBox = BuildPolicyBox("Max");
        policyRow.AddChild(BuildPolicyColumn(UiText.Get("Min"), _minStockBox));
        policyRow.AddChild(BuildPolicyColumn(UiText.Get("Max"), _maxStockBox));

        _minStockBox.ValueChanged += OnMinStockChanged;
        _maxStockBox.ValueChanged += OnMaxStockChanged;

        _policyErrorLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _policyErrorLabel.ThemeTypeVariation = "ErrorText";
        root.AddChild(_policyErrorLabel);
    }

    private static VBoxContainer BuildPolicyColumn(string caption, SpinBox box)
    {
        var column = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        column.AddThemeConstantOverride("separation", 4);
        var label = new Label
        {
            Text = caption,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        label.ThemeTypeVariation = "BodySmall";
        column.AddChild(label);
        column.AddChild(box);
        return column;
    }

    private static SpinBox BuildPolicyBox(string _)
    {
        return new SpinBox
        {
            MinValue = MinStockFloor,
            MaxValue = MaxStockCeiling,
            Step = 1,
            Rounded = true,
            CustomMinimumSize = new Vector2(96, 0),
            FocusMode = Control.FocusModeEnum.All,
            ThemeTypeVariation = "LineEdit",
        };
    }

    private void OnMinStockChanged(double value)
    {
        if (_refreshing) return;
        if (!ValidatePolicy((int)value, (int)_maxStockBox.Value, out string error))
        {
            _policyErrorLabel.Text = error;
            return;
        }
        _policyErrorLabel.Text = string.Empty;
        EmitSignal(SignalName.PolicyConfigureRequested, (int)value, (int)_maxStockBox.Value);
    }

    private void OnMaxStockChanged(double value)
    {
        if (_refreshing) return;
        if (!ValidatePolicy((int)_minStockBox.Value, (int)value, out string error))
        {
            _policyErrorLabel.Text = error;
            return;
        }
        _policyErrorLabel.Text = string.Empty;
        EmitSignal(SignalName.PolicyConfigureRequested, (int)_minStockBox.Value, (int)value);
    }

    private static bool ValidatePolicy(int min, int max, out string error)
    {
        if (min < MinStockFloor)
        {
            error = UiText.Format("ui.production.min_floor", MinStockFloor);
            return false;
        }
        if (max > MaxStockCeiling)
        {
            error = UiText.Format("ui.production.max_ceiling", MaxStockCeiling);
            return false;
        }
        if (min > max)
        {
            error = UiText.Get("Min must be less than or equal to Max.");
            return false;
        }
        error = string.Empty;
        return true;
    }

    public override void _ExitTree()
    {
        if (_themeSignals is not null) _themeSignals.LineageChanged -= OnLineageChanged;
    }

    private void OnLineageChanged(string lineage) => AddThemeStyleboxOverride(
        "panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));

    public void Refresh(BuildingDetailSnapshot snapshot)
    {
        // Title stays the static "Production" set in _Ready(): the
        // building's own name/resource already shows in this screen's
        // header (BuildingDetailView._title), so repeating it here was
        // pure redundant text.
        _stockLabel.Text = snapshot.IsForest
            ? UiText.Format(
                "ui.production.wood_stock",
                snapshot.Stock,
                snapshot.StorageCapacity,
                snapshot.WoodReserve)
            : UiText.Format(
                "ui.production.stock",
                UiText.Get(snapshot.ResourceLabel),
                snapshot.Stock,
                snapshot.StorageCapacity);

        _stockBar.MinValue = 0;
        _stockBar.MaxValue = snapshot.StorageCapacity == 0 ? 1 : snapshot.StorageCapacity;
        _stockBar.Value = snapshot.Stock;

        _rateLabel.Text = snapshot.StorageCapacity == 0
            ? UiText.Get("Resting site — no production")
            : snapshot.IsForest
                ? UiText.Format(
                    "ui.production.foraging_rate",
                    snapshot.ProductionRate,
                    UiText.Get(snapshot.ResourceUnit),
                    snapshot.AssignedCount,
                    snapshot.ProductionCycleTicks)
                : UiText.Format(
                    "ui.production.rate",
                    snapshot.ProductionRate,
                    UiText.Get(snapshot.ResourceUnit),
                    snapshot.ProductionCycleTicks);

        _inputsLabel.Text = DescribeInputsDue(snapshot);
        _statusLabel.Text = DescribePolicyState(snapshot);

        _refreshing = true;
        _enabledToggle.SetPressedNoSignal(snapshot.ProductionEnabled);
        _enabledToggle.SetIconAndLabel(
            snapshot.ProductionEnabled ? IconPaths.Pause : IconPaths.Play,
            UiText.Get(snapshot.ProductionEnabled ? "Pause" : "Resume"));
        _enabledToggle.TooltipText = snapshot.ProductionEnabled
            ? UiText.Get("Pause production")
            : UiText.Get("Resume production");

        // Reactive policy controls are hidden for non-productive
        // buildings (Home) and updated without firing the change
        // signals so Refresh() never produces a feedback loop.
        bool showPolicy = snapshot.StorageCapacity > 0;
        _minStockBox.Visible = showPolicy;
        _maxStockBox.Visible = showPolicy;
        _policyErrorLabel.Visible = showPolicy;
        if (showPolicy)
        {
            _minStockBox.SetValueNoSignal(snapshot.MinStock);
            _maxStockBox.SetValueNoSignal(snapshot.MaxStock);
            _policyErrorLabel.Text = string.Empty;
            _minStockBox.MaxValue = snapshot.StorageCapacity;
            _maxStockBox.MaxValue = snapshot.StorageCapacity;
        }
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
            return UiText.Get("Inputs due: none");
        }
        var parts = new System.Collections.Generic.List<string>();
        foreach (var input in snapshot.PendingInputs)
        {
            parts.Add($"{input.Amount} {input.Resource.ToString().ToLowerInvariant()}");
        }
        return UiText.Format("ui.production.inputs_due", string.Join(" + ", parts));
    }

    private static string DescribePolicyState(BuildingDetailSnapshot snapshot)
    {
        if (snapshot.StorageCapacity == 0)
        {
            return UiText.Get("Workers rest here between shifts.");
        }
        if (!snapshot.ProductionEnabled) return UiText.Get("Production paused by the player");
        return snapshot.StopCause switch
        {
            ProductionStopCause.Night => UiText.Get("Resting during the night"),
            ProductionStopCause.NoWorkers => UiText.Get("Waiting for contributors"),
            ProductionStopCause.WorkersExhausted => UiText.Get("Contributors exhausted"),
            ProductionStopCause.TargetReached => UiText.Format(
                "ui.production.storage_full", snapshot.Stock, snapshot.StorageCapacity),
            ProductionStopCause.MissingInputs => UiText.Get("Waiting for inputs"),
            ProductionStopCause.WorkersInTransit => UiText.Get("Worker travelling to the building"),
            ProductionStopCause.WorkersRecovering => UiText.Get("Workers are recovering before resuming this order"),
            ProductionStopCause.WorkersBlockedNoFood => UiText.Get("Workers cannot resume because no food is available"),
            _ => UiText.Get("Authorised"),
        };
    }
}
