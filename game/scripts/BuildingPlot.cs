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
/// Two visual states are supported:
/// - **idle**: the finished building texture is shown at full opacity.
/// - **under construction**: the same texture is shown with a small
///   overlay label so the player can distinguish an in-flight worksite
///   from a finished plot.
///
/// The state is driven by <see cref="IsUnderConstruction"/>; the
/// <see cref="Configure(string, string, bool)"/> entry point is the
/// preferred way to update it at runtime.
///
/// [Export] names use single PascalCase tokens
/// (<see cref="BuildingIdValue"/>, <see cref="BuildingNameValue"/>,
/// <see cref="BuildingTexturePath"/>) so each name is unambiguous
/// whether Godot's tscn loader expects PascalCase or snake_case for
/// C# exports.
/// </summary>
public partial class BuildingPlot : Control
{
    [Signal] public delegate void BuildingClickedEventHandler(int buildingId);

    /// <summary>
    /// Path to the macro plot texture. Defaults to the quarry
    /// placeholder so a freshly placed `BuildingPlot` still renders
    /// something legible; scenes that want a different kind should
    /// set this through the inspector or via
    /// <see cref="BuildingArt.GetTexturePath"/>.
    /// </summary>
    [Export] public string BuildingTexturePath { get; set; } =
        BuildingArt.AssetsRoot + "quarry_idle.png";

    [Export] public int BuildingIdValue { get; set; } = 1;

    [Export] public string BuildingNameValue { get; set; } = "Building";

    [Export] public string BuildingTooltip { get; set; } = "Click to enter";

    /// <summary>
    /// When true, the plot renders its "under construction" overlay so
    /// the player can distinguish an in-flight worksite from a finished
    /// building. Default false so existing inspector-placed plots
    /// continue to render normally.
    /// </summary>
    [Export] public bool IsUnderConstruction { get; set; }

    private TextureRect _art = null!;
    private Label _label = null!;
    private Button _button = null!;
    private Label _overlay = null!;

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
            ThemeTypeVariation = "SectionTitle",
        };
        AddChild(_label);

        _overlay = new Label
        {
            Text = "Under construction",
            HorizontalAlignment = HorizontalAlignment.Center,
            Size = new Vector2(
                PresentationConstants.MacroPlotSize,
                20),
            Position = new Vector2(0, PresentationConstants.MacroPlotSize - 28),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ThemeTypeVariation = "ButtonWarning",
            Visible = IsUnderConstruction,
        };
        AddChild(_overlay);

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

        GD.Print($"BuildingPlot {Name} ready: BuildingIdValue={BuildingIdValue} BuildingNameValue='{BuildingNameValue}' UnderConstruction={IsUnderConstruction}");
    }

    /// <summary>
    /// Runtime configuration entry point used by <c>BuildingPlotStage</c>
    /// when adding plots dynamically. Pass the texture path from
    /// <see cref="BuildingArt.GetTexturePath"/>; the caller should not
    /// pass <c>null</c> (the stage filters kinds without art before
    /// creating the plot).
    /// </summary>
    public void Configure(string texturePath, string displayName, bool underConstruction)
    {
        BuildingTexturePath = texturePath;
        BuildingNameValue = displayName;
        IsUnderConstruction = underConstruction;
        if (_art is not null)
        {
            _art.Texture = ResourceLoader.Load<Texture2D>(texturePath);
            _label.Text = displayName;
            _overlay.Visible = underConstruction;
            _button.TooltipText = underConstruction
                ? "Under construction"
                : "Click to enter";
        }
    }
}