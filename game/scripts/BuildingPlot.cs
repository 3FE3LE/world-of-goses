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
    private const int PlaceholderInset = 24;
    private static readonly Vector2 PlaceholderSize = new(
        PresentationConstants.MacroPlotSize - PlaceholderInset * 2,
        PresentationConstants.MacroPlotSize - PlaceholderInset * 2);
    [Signal] public delegate void BuildingClickedEventHandler(int buildingId);

    /// <summary>
    /// Optional visual fallback for kinds without art. When
    /// <see cref="BuildingTexturePath"/> is null, <see cref="_Ready"/>
    /// paints the plot with <see cref="Background"/> + a centred
    /// <see cref="Headline"/> + <see cref="Subline"/> pair. Both are
    /// drawn before the hit button so clicks still fire
    /// <see cref="BuildingClicked"/>.
    /// </summary>
    public readonly struct PlaceholderStyle
    {
        public PlaceholderStyle(Color background, string headline, string subline, Color headlineColor)
        {
            Background = background;
            Headline = headline;
            Subline = subline;
            HeadlineColor = headlineColor;
        }
        public Color Background { get; }
        public string Headline { get; }
        public string Subline { get; }
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
    public int ConstructionProgress { get; private set; }
    public int ConstructionRequiredWork { get; private set; }

    private TextureRect _art = null!;
    private ColorRect _placeholder = null!;
    private VBoxContainer _placeholderLabelStack = null!;
    private Label _placeholderLabel = null!;
    private Label _placeholderSubLabel = null!;
    private Label _label = null!;
    private TooltipButton _button = null!;
    private Panel _hitOutline = null!;
    private Label _overlay = null!;
    private ProgressBar _progressBar = null!;
    private PlaceholderStyle _placeholderStyle = DefaultForestStyle();

    private static PlaceholderStyle DefaultForestStyle() =>
        new(
            background: new Color(0.42f, 0.27f, 0.16f),
            headline: "FOREST",
            subline: "Click to gather wood",
            headlineColor: new Color(0.96f, 0.93f, 0.86f));

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(
            PresentationConstants.MacroPlotSize,
            PresentationConstants.MacroPlotSize);

        _art = new TextureRect
        {
            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
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
            Position = new Vector2(PlaceholderInset, PlaceholderInset),
            Size = PlaceholderSize,
            Visible = BuildingTexturePath is null,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(_placeholder);

        _placeholderLabelStack = new VBoxContainer
        {
            Name = "PlaceholderLabels",
            Position = new Vector2(PlaceholderInset, PlaceholderInset),
            Size = PlaceholderSize,
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = BuildingTexturePath is null,
        };
        _placeholderLabelStack.AddThemeConstantOverride("separation", 2);
        AddChild(_placeholderLabelStack);

        _placeholderLabel = new Label
        {
            Text = _placeholderStyle.Headline,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = _placeholderStyle.HeadlineColor,
            ThemeTypeVariation = "SectionTitle",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _placeholderLabelStack.AddChild(_placeholderLabel);

        _placeholderSubLabel = new Label
        {
            Text = _placeholderStyle.Subline,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = _placeholderStyle.HeadlineColor,
            ThemeTypeVariation = "BodySmall",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _placeholderLabelStack.AddChild(_placeholderSubLabel);

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
            Text = ConstructionProgressLabel(ConstructionProgress, ConstructionRequiredWork),
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

        _progressBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            Value = 0,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(
                PresentationConstants.MacroPlotSize - 16, 8),
            Position = new Vector2(8, PresentationConstants.MacroPlotSize - 16),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = IsUnderConstruction,
        };
        AddChild(_progressBar);

        _button = new TooltipButton
        {
            Flat = true,
            TooltipText = BuildingTooltip,
            FocusMode = FocusModeEnum.All,
        };
        _hitOutline = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        var outlineStyle = new StyleBoxFlat
        {
            BgColor = Colors.Transparent,
            BorderColor = LineageThemeRegistry.IconAccent,
        };
        outlineStyle.SetBorderWidthAll(2);
        _hitOutline.AddThemeStyleboxOverride("panel", outlineStyle);
        _hitOutline.Hide();
        AddChild(_hitOutline);
        UpdateInteractionGeometry();
        _button.MouseEntered += OnInteractionEntered;
        _button.MouseExited += _hitOutline.Hide;
        _button.FocusEntered += OnInteractionEntered;
        _button.FocusExited += _hitOutline.Hide;
        _button.Pressed += () =>
        {
            GD.Print($"BuildingPlot {Name}: click BuildingIdValue={BuildingIdValue}");
            EmitSignal(SignalName.BuildingClicked, BuildingIdValue);
        };
        AddChild(_button);

        GD.Print($"BuildingPlot {Name} ready: BuildingIdValue={BuildingIdValue} BuildingNameValue='{BuildingNameValue}' UnderConstruction={IsUnderConstruction} HasArt={BuildingTexturePath is not null}");
    }

    private void OnInteractionEntered()
    {
        _hitOutline.Show();
        Ui.UiMotion.Pulse(_hitOutline, LineageThemeRegistry.IconAccent);
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
    public void Configure(
        string? texturePath,
        string displayName,
        bool underConstruction,
        int progress = 0,
        int requiredWork = 0,
        PlaceholderStyle? placeholder = null,
        bool enabled = true)
    {
        BuildingTexturePath = texturePath;
        BuildingNameValue = displayName;
        IsUnderConstruction = underConstruction;
        ConstructionProgress = progress;
        ConstructionRequiredWork = requiredWork;
        if (placeholder is { } style) _placeholderStyle = style;
        if (_art is null) return;
        ApplyTexture();
        _label.Text = displayName;
        _label.Visible = texturePath is not null;
        _overlay.Visible = underConstruction;
        if (underConstruction && !enabled)
        {
            _overlay.Text = "Paused";
        }
        else
        {
            _overlay.Text = ConstructionProgressLabel(progress, requiredWork);
        }
        _progressBar.Visible = underConstruction;
        _progressBar.MinValue = 0;
        _progressBar.MaxValue = requiredWork > 0 ? requiredWork : 1;
        _progressBar.Value = System.Math.Clamp(progress, 0, _progressBar.MaxValue);
        _placeholder.Visible = texturePath is null;
        _placeholder.Color = _placeholderStyle.Background;
        _placeholderLabelStack.Visible = texturePath is null;
        _placeholderLabel.Text = _placeholderStyle.Headline;
        _placeholderLabel.Modulate = _placeholderStyle.HeadlineColor;
        _placeholderSubLabel.Text = enabled && texturePath is null
            ? _placeholderStyle.Subline
            : "Depleted";
        _placeholderSubLabel.Modulate = _placeholderStyle.HeadlineColor;
        // Disable the plot when not under construction and the building
        // is not gatherable (e.g. forest with no wood). Construction
        // plots stay clickable so the player can open their progress.
        _button.Disabled = !enabled && !underConstruction;
        _button.TooltipText = underConstruction
            ? enabled
                ? $"Under construction — click to open progress ({progress}/{requiredWork})"
                : "Work paused — click to open progress"
            : texturePath is null
                ? enabled
                    ? $"Click to gather wood from {displayName}"
                    : $"{displayName} has no wood available."
                : "Click to enter";
        UpdateInteractionGeometry();
    }

    private void UpdateInteractionGeometry()
    {
        if (_button is null || _hitOutline is null) return;
        Vector2? canvasSize = _art.Texture?.GetSize();
        Rect2 interaction = InteractionRect(canvasSize, BuildingTexturePath is null);
        _button.Position = interaction.Position;
        _button.Size = interaction.Size;
        _hitOutline.Position = interaction.Position;
        _hitOutline.Size = interaction.Size;
        _label.Position = new Vector2(interaction.Position.X, interaction.Position.Y);
        _label.Size = new Vector2(interaction.Size.X, 22);
        _label.HorizontalAlignment = HorizontalAlignment.Center;
    }

    internal static Rect2 InteractionRect(Vector2? canvasSize, bool isPlaceholder = false)
    {
        float plotSize = PresentationConstants.MacroPlotSize;
        // Placeholder plots have no art, so the hitbox tracks the visible
        // placeholder canvas (192 - 2*24) instead of the full plot.
        if (isPlaceholder || canvasSize is null)
        {
            return new Rect2(
                new Vector2(PlaceholderInset, PlaceholderInset),
                PlaceholderSize);
        }

        float width = Mathf.Max(canvasSize.Value.X, 96f);
        float artHeight = Mathf.Max(canvasSize.Value.Y, 96f);
        float x = (plotSize - width) * 0.5f;
        float artTop = (plotSize - artHeight) * 0.5f;
        float labelTop = Mathf.Max(4f, artTop - 24f);
        float artBottom = (plotSize + artHeight) * 0.5f;
        return new Rect2(x, labelTop, width, artBottom - labelTop);
    }

    internal static string ConstructionProgressLabel(int progress, int requiredWork)
    {
        if (requiredWork <= 0) return "Under construction";
        int percent = (int)((long)System.Math.Clamp(progress, 0, requiredWork) * 100 / requiredWork);
        return $"Construction · {percent}%";
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
