#nullable enable
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

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
/// Three visual states are supported:
/// - **idle / finished**: the building texture is shown at full opacity.
/// - **under construction**: the same texture is shown with a small
///   overlay label so the player can distinguish an in-flight worksite
///   from a finished plot.
/// - **placeholder**: when the <c>BuildingKind</c> has no art yet
///   (Forest, future Smithy, future PotionLab), a brown <c>ColorRect</c>
///   fills the canvas with a large label so the plot remains clickable
///   and visually present in the city. Once art ships, remove the
///   placeholder branch and pass a real texture path again.
///
/// The state is driven by <see cref="IsUnderConstruction"/> and the
/// nullable <see cref="BuildingTexturePath"/>; the
/// <see cref="Configure(string?, string, bool, PlaceholderStyle?)"/>
/// entry point is the preferred way to update it at runtime.
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
    /// Optional visual fallback for kinds without art. When
    /// <see cref="BuildingTexturePath"/> is null, <see cref="_Ready"/>
    /// paints the plot with <see cref="Background"/> + a centred
    /// <see cref="Headline"/> label. Both are drawn before the hit
    /// button so clicks still fire <see cref="BuildingClicked"/>.
    /// </summary>
    public readonly struct PlaceholderStyle
    {
        public PlaceholderStyle(Color background, string headline, Color headlineColor)
        {
            Background = background;
            Headline = headline;
            HeadlineColor = headlineColor;
        }
        public Color Background { get; }
        public string Headline { get; }
        public Color HeadlineColor { get; }
    }

    /// <summary>
    /// Path to the macro plot texture. Defaults to the quarry
    /// placeholder so a freshly placed `BuildingPlot` still renders
    /// something legible; scenes that want a different kind should
    /// set this through the inspector or via
    /// <see cref="BuildingArt.GetTexturePath"/>.
    /// </summary>
    [Export] public string? BuildingTexturePath { get; set; } =
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
    private ColorRect _placeholder = null!;
    private Label _placeholderLabel = null!;
    private Label _label = null!;
    private TooltipButton _button = null!;
    private Label _overlay = null!;
    private PlaceholderStyle _placeholderStyle = DefaultForestStyle();

    private static PlaceholderStyle DefaultForestStyle() =>
        new(
            background: new Color(0.42f, 0.27f, 0.16f),
            headline: "FOREST",
            headlineColor: new Color(0.96f, 0.93f, 0.86f));

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(
            PresentationConstants.MacroPlotSize,
            PresentationConstants.MacroPlotSize);

        _art = new TextureRect
        {
            StretchMode = TextureRect.StretchModeEnum.Keep,
            Size = new Vector2(
                PresentationConstants.MacroPlotSize,
                PresentationConstants.MacroPlotSize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        ApplyTexture();
        AddChild(_art);

        _placeholder = new ColorRect
        {
            Color = _placeholderStyle.Background,
            Size = new Vector2(
                PresentationConstants.MacroPlotSize,
                PresentationConstants.MacroPlotSize),
            Visible = BuildingTexturePath is null,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(_placeholder);

        _placeholderLabel = new Label
        {
            Text = _placeholderStyle.Headline,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Size = new Vector2(
                PresentationConstants.MacroPlotSize,
                PresentationConstants.MacroPlotSize),
            Modulate = _placeholderStyle.HeadlineColor,
            ThemeTypeVariation = "GameTitle",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = BuildingTexturePath is null,
        };
        _placeholderLabel.AddThemeFontSizeOverride("font_size", 36);
        AddChild(_placeholderLabel);

        _label = new Label
        {
            Text = BuildingNameValue,
            Position = new Vector2(8, 8),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ThemeTypeVariation = "SectionTitle",
            Visible = BuildingTexturePath is not null,
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

        _button = new TooltipButton
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

        GD.Print($"BuildingPlot {Name} ready: BuildingIdValue={BuildingIdValue} BuildingNameValue='{BuildingNameValue}' UnderConstruction={IsUnderConstruction} HasArt={BuildingTexturePath is not null}");
    }

    /// <summary>
    /// Runtime configuration entry point used by <c>BuildingPlotStage</c>
    /// when adding plots dynamically. Pass the texture path from
    /// <see cref="BuildingArt.GetTexturePath"/> when art exists; pass
    /// <c>null</c> to render the brown placeholder for kinds without
    /// art. <paramref name="placeholder"/> customises the placeholder's
    /// background, headline and headline colour; pass <c>null</c> to
    /// use the default brown "FOREST" style.
    /// </summary>
    public void Configure(string? texturePath, string displayName, bool underConstruction, PlaceholderStyle? placeholder = null)
    {
        BuildingTexturePath = texturePath;
        BuildingNameValue = displayName;
        IsUnderConstruction = underConstruction;
        if (placeholder is { } style) _placeholderStyle = style;
        if (_art is null) return;
        ApplyTexture();
        _label.Text = displayName;
        _label.Visible = texturePath is not null;
        _overlay.Visible = underConstruction;
        _placeholder.Visible = texturePath is null;
        _placeholder.Color = _placeholderStyle.Background;
        _placeholderLabel.Text = _placeholderStyle.Headline;
        _placeholderLabel.Modulate = _placeholderStyle.HeadlineColor;
        _placeholderLabel.Visible = texturePath is null;
        _button.TooltipText = underConstruction
            ? "Under construction"
            : texturePath is null
                ? $"Click to enter {displayName}"
                : "Click to enter";
    }

    private void ApplyTexture()
    {
        if (BuildingTexturePath is { } path)
        {
            _art.Texture = ResourceLoader.Load<Texture2D>(path);
            _art.Visible = true;
        }
        else
        {
            _art.Texture = null;
            _art.Visible = false;
        }
    }
}
