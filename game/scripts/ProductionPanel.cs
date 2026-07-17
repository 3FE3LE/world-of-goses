using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Bottom panel of the building detail view. Shows the building's
/// stock, capacity, and current production rate, and offers a manual
/// button that advances the production by one tick.
/// </summary>
public partial class ProductionPanel : PanelContainer
{
    [Signal] public delegate void AdvanceRequestedEventHandler();

    private Label _titleLabel = null!;
    private Label _stockLabel = null!;
    private Label _rateLabel = null!;
    private Label _helperLabel = null!;
    private ProgressBar _stockBar = null!;
    private Button _advanceButton = null!;

    public override void _Ready()
    {
        var root = new VBoxContainer();
        AddChild(root);

        _titleLabel = new Label
        {
            Text = "Production",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        root.AddChild(_titleLabel);

        _stockLabel = new Label { Text = "Stock: 0 / 0" };
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
        root.AddChild(_rateLabel);

        _advanceButton = new Button { Text = "Advance production" };
        _advanceButton.Pressed += () => EmitSignal(SignalName.AdvanceRequested);
        root.AddChild(_advanceButton);

        _helperLabel = new Label
        {
            Text = "Click 'Advance production' to add.",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(1, 1, 1, 0.7f),
        };
        root.AddChild(_helperLabel);
    }

    public void Refresh(Building building, CityWorldController controller)
    {
        _titleLabel.Text = $"{building.DisplayName} — {building.ResourceLabel}";
        _stockLabel.Text = $"{building.ResourceLabel}: {building.Stock} / {building.StorageCapacity}";

        _stockBar.MinValue = 0;
        _stockBar.MaxValue = building.StorageCapacity == 0 ? 1 : building.StorageCapacity;
        _stockBar.Value = building.Stock;

        int rate = controller.CurrentProductionRate(building.Id);
        _rateLabel.Text = $"Rate: {rate} {building.ResourceUnit} / tick ({building.AssignedCount} workers)";
        _helperLabel.Text = $"Click 'Advance production' to add {building.ResourceUnit}.";
    }
}
