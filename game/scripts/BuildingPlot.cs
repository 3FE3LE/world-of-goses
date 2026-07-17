using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Generic plot for a single building in the macro city view. The
/// plot is parametrized by <see cref="BuildingIdValue"/>, so the
/// same script can back Quarry (1) or Farm (2) without forking.
///
/// Emits <see cref="BuildingClicked"/> with the configured id when
/// the plot is activated. The view responds by routing to whichever
/// detail view handles that building.
///
/// [Export] names use single PascalCase tokens
/// (<see cref="BuildingIdValue"/>, <see cref="BuildingNameValue"/>,
/// <see cref="BuildingTooltip"/>) so each name is unambiguous whether
/// Godot's tscn loader expects PascalCase or snake_case for C# exports.
/// </summary>
public partial class BuildingPlot : Control
{
    [Signal] public delegate void BuildingClickedEventHandler(int buildingId);

    [Export] public string BuildingTexturePath { get; set; } =
        "res://assets/buildings/building_placeholder.png";

    [Export] public int BuildingIdValue { get; set; } = 1;

    [Export] public string BuildingNameValue { get; set; } = "Building";

    [Export] public string BuildingTooltip { get; set; } = "Click to enter";

    private TextureRect _art = null!;
    private Label _label = null!;
    private Button _button = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(
            PresentationConstants.MacroPlotSize,
            PresentationConstants.MacroPlotSize);

        _art = new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(BuildingTexturePath),
            StretchMode = TextureRect.StretchModeEnum.Keep,
            Size = new Vector2(
                PresentationConstants.MacroPlotSize,
                PresentationConstants.MacroPlotSize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(_art);

        _label = new Label
        {
            Text = BuildingNameValue,
            Position = new Vector2(8, 8),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(_label);

        _button = new Button
        {
            Size = new Vector2(PresentationConstants.MacroPlotSize, PresentationConstants.MacroPlotSize),
            Flat = true,
            TooltipText = BuildingTooltip,
        };
        _button.Pressed += () =>
        {
            GD.Print($"BuildingPlot {Name}: click BuildingIdValue={BuildingIdValue}");
            EmitSignal(SignalName.BuildingClicked, BuildingIdValue);
        };
        AddChild(_button);

        GD.Print($"BuildingPlot {Name} ready: BuildingIdValue={BuildingIdValue} BuildingNameValue='{BuildingNameValue}'");
    }
}
