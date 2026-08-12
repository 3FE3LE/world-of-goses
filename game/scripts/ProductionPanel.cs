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
    private bool _refreshing;

    /// <summary>
    /// A11: the shape is authored under <c>ProductionPanel/Root</c> in
    /// <c>CityPrototype.tscn</c>. Every string is still written here, because
    /// a literal in a <c>.tscn</c> is one no locale switch can reach; the
    /// numbers come from <see cref="MinStockFloor"/>/<see cref="MaxStockCeiling"/>
    /// so the policy bounds have one owner rather than one per file.
    /// </summary>
    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("Root/Title");
        _titleLabel.Text = UiText.Get("Production");

        _stockLabel = GetNode<Label>("Root/StockRow/Stock");

        _enabledToggle = GetNode<IconButton>("Root/StockRow/EnabledToggle");
        _enabledToggle.SetIconAndLabel(IconPaths.Pause, UiText.Get("Pause"));
        _enabledToggle.TooltipText = UiText.Get("Pause production");
        _enabledToggle.Toggled += OnPolicyToggle;

        _stockBar = GetNode<ProgressBar>("Root/StockBar");
        _rateLabel = GetNode<Label>("Root/Rate");

        _inputsLabel = GetNode<Label>("Root/Inputs");
        _inputsLabel.Text = UiText.Get("Inputs due: none");

        _statusLabel = GetNode<Label>("Root/Status");
        GetNode<Label>("Root/PolicyHeader").Text = UiText.Get("Reactive policy");

        // The caption is resolved here rather than inside the helper so the
        // key stays a literal at the call site. The localisation validator
        // scans for a quoted key argument and cannot see one arriving as a
        // variable, which silently drops it from the catalogue's coverage —
        // moving these two into the helper cost exactly that, 324 runtime
        // keys down to 322, without failing anything.
        _minStockBox = BindPolicyColumn("Min", UiText.Get("Min"));
        _maxStockBox = BindPolicyColumn("Max", UiText.Get("Max"));
        _minStockBox.ValueChanged += OnMinStockChanged;
        _maxStockBox.ValueChanged += OnMaxStockChanged;

        _policyErrorLabel = GetNode<Label>("Root/PolicyError");
    }

    private SpinBox BindPolicyColumn(string column, string caption)
    {
        GetNode<Label>($"Root/PolicyRow/{column}Column/Caption").Text = caption;
        SpinBox box = GetNode<SpinBox>($"Root/PolicyRow/{column}Column/Box");
        box.MinValue = MinStockFloor;
        box.MaxValue = MaxStockCeiling;
        return box;
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
                    SimulationTimeText.FormatDurationLocalized(
                        snapshot.ProductionCycleTicks))
                : UiText.Format(
                    "ui.production.rate",
                    snapshot.ProductionRate,
                    UiText.Get(snapshot.ResourceUnit),
                    SimulationTimeText.FormatDurationLocalized(
                        snapshot.ProductionCycleTicks));

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
            parts.Add($"{input.Amount} {ResourceTypeLocalizer.Label(input.Resource)}");
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
