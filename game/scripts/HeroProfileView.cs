#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;
using WorldofGoses.Visual;

namespace WorldofGoses;

/// <summary>Read-only presentation of every persisted hero profile choice.</summary>
public partial class HeroProfileView : Control
{
    [Export] public NodePath ControllerPath { get; set; } = "../../../CityWorldController";

    private CityWorldController _controller = null!;
    private VBoxContainer _content = null!;
    private Button _backButton = null!;
    private TextureRect _splash = null!;
    private Control _splashColumns = null!;
    private CitizenSpriteCarrier? _heroCarrier;
    private CenterContainer? _heroAnchor;

    public override void _Ready()
    {
        // HUD chrome: the hero profile replaces the map entirely, so the
        // map's ambient tint must not wash over it.
        OverlayLayers.Apply(this, OverlayLayers.Hud);
        _controller = GetNode<CityWorldController>(ControllerPath);
        _controller.SelectionChanged += OnSelectionChanged;
        _controller.HeroCreated += OnHeroCreated;
        BuildShell();
        Hide();
    }

    public override void _ExitTree()
    {
        if (_controller is null) return;
        _controller.SelectionChanged -= OnSelectionChanged;
        _controller.HeroCreated -= OnHeroCreated;
    }

    private void BuildShell()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var background = new ColorRect
        {
            Color = new Color("17202a"),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var margin = new SafeAreaMarginContainer { MinimumInset = 24 };
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(margin);

        var shell = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        shell.AddThemeConstantOverride("separation", 12);
        margin.AddChild(shell);

        var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        shell.AddChild(header);

        var title = new Label
        {
            Text = UiText.Get("ui.hero_profile.title"),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        title.ThemeTypeVariation = "ScreenTitle";
        header.AddChild(title);

        _backButton = StandardButtons.BackToCityButton();
        _backButton.Pressed += () => _controller.ReturnToCity();
        header.AddChild(_backButton);

        // Portrait on the left at full height, text column on the right. The
        // art is authored portrait, so anchoring it by height and letting its
        // width follow shows every lineage whole — the set does not share one
        // aspect ratio (nine are 3:4, seven are 4:5), and a fixed-size frame
        // would letterbox one group or crop the other.
        var columns = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        columns.AddThemeConstantOverride("separation", 20);
        shell.AddChild(columns);

        _splash = new TextureRect
        {
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
        };
        // The width is computed rather than left to an expand mode. Every
        // proportional mode derives it from a height that is still unresolved
        // on the first layout pass, so the portrait reports a zero minimum
        // width, the HBox gives it none, and the art loads but is never drawn.
        // Recomputing on resize also keeps it correct across window sizes and
        // across the two aspect groups in the set.
        _splashColumns = columns;
        columns.Resized += UpdateSplashWidth;
        // The illustration is displayed downscaled from its authored size, so
        // linear filtering with mipmaps reads far better than the nearest
        // filter the sprite pipeline uses. This is a deliberate, local
        // exception for splash illustrations; in-world pixel art keeps
        // nearest.
        _splash.TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        columns.AddChild(_splash);

        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        scroll.AddThemeStyleboxOverride(
            "panel",
            new StyleBoxFlat { BgColor = new Color("17202a") });
        columns.AddChild(scroll);

        var contentMargin = new MarginContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        contentMargin.AddThemeConstantOverride("margin_left", 8);
        contentMargin.AddThemeConstantOverride("margin_right", 20);
        contentMargin.AddThemeConstantOverride("margin_top", 8);
        contentMargin.AddThemeConstantOverride("margin_bottom", 8);
        scroll.AddChild(contentMargin);

        _content = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        _content.AddThemeConstantOverride("separation", 10);
        contentMargin.AddChild(_content);
    }

    /// <summary>
    /// Sizes the portrait column to the art's own proportion at the full
    /// height available. Anchoring by height and deriving the width is what
    /// lets both aspect groups in the splash set (3:4 and 4:5) render whole
    /// without letterboxing one or cropping the other.
    /// </summary>
    private void UpdateSplashWidth()
    {
        if (_splash?.Texture is not Texture2D texture)
        {
            if (_splash is not null) _splash.CustomMinimumSize = Vector2.Zero;
            return;
        }
        int textureHeight = texture.GetHeight();
        if (textureHeight <= 0) return;

        float availableHeight = _splashColumns.Size.Y;
        // Before the first layout pass the container has no size yet; fall
        // back to the canvas height so the very first frame is already right.
        if (availableHeight <= 0) availableHeight = LineageSplashRegistry.SplashLogicalHeight;

        float aspect = (float)texture.GetWidth() / textureHeight;
        _splash.CustomMinimumSize = new Vector2(availableHeight * aspect, 0);
    }

    private void Render()
    {
        foreach (var child in _content.GetChildren())
        {
            _content.RemoveChild(child);
            child.QueueFree();
        }

        HideHeroCarrier();

        HeroProfileSnapshot? hero = _controller.GetHeroProfileSnapshot();
        if (hero is null)
        {
            AddBody(UiText.Get("ui.hero_profile.no_hero"));
            return;
        }

        // The splash and the small animated sprite say the same thing, so only
        // one is shown. The sprite stays as the fallback: a missing splash is
        // an asset problem, and the profile must still identify its subject
        // visually rather than degrading to a wall of text.
        Texture2D? splash = LineageSplashRegistry.Load(hero.Lineage, hero.Gender);
        _splash.Texture = splash;
        _splash.Visible = splash is not null;
        UpdateSplashWidth();
        if (splash is null) AddHeroSprite(hero);

        AddHeroName($"{hero.Name} · {hero.LineageName}");
        AddBody(UiText.Get("ui.hero_profile.role"));
        AddBody(UiText.Get(hero.LineageSummary));
        AddBody(UiText.Get(hero.LearningApproach));
        AddBody(UiText.Get("ui.hero_profile.lineage_disclaimer"));

        AddHeading(UiText.Get("ui.hero_profile.aptitudes_heading"));
        AddBody(JoinLocalized(hero.Aptitudes));

        AddHeading(UiText.Get("ui.hero_profile.affinities_heading"));
        AddBody(JoinLocalized(hero.ProfessionalAffinities));
        AddBody(UiText.Format("ui.hero_profile.common_paths", hero.LineageName, JoinLocalized(hero.MarkedAffinities)));

        AddHeading(UiText.Get("ui.hero_profile.combat_heading"));
        AddBody(UiText.Format("ui.hero_profile.elemental_affinity", UiText.Get(hero.ElementalAffinity)));
        AddBody(UiText.Format("ui.hero_profile.combat_style", UiText.Get(hero.CombatStyle)));
        AddBody(UiText.Format("ui.hero_profile.weapon_preferences", JoinLocalized(hero.WeaponPreferences)));
        AddBody(UiText.Format("ui.hero_profile.gender", UiText.Get(hero.Gender.ToString())));

        AddHeading(UiText.Get("ui.hero_profile.personality_heading"));
        AddBody(UiText.Format("ui.hero_profile.traits", JoinLocalized(hero.PersonalityTraits)));
        AddBody(UiText.Format("ui.hero_profile.political_orientation", UiText.Get(hero.PoliticalOrientation)));
        AddBody(UiText.Format("ui.hero_profile.spiritual_posture", UiText.Get(hero.SpiritualPosture)));

        AddHeading(UiText.Get("ui.hero_profile.condition_heading"));
        AddStaminaBar(hero.CurrentStamina, hero.EffectiveMaxStamina);
        AddIconBody(IconPaths.Heart, UiText.Format(
            "ui.hero_profile.stamina_effective",
            hero.CurrentStamina,
            hero.EffectiveMaxStamina,
            hero.MaxStamina));
        if (hero.WoundSeverity is WoundSeverity woundSeverity)
        {
            AddIconBody(
                IconPaths.Heart,
                UiText.Format(
                    hero.IsReceivingWoundTreatment
                        ? "ui.hero_profile.wound_treatment"
                        : "ui.hero_profile.wound",
                    UiText.Get(woundSeverity == WoundSeverity.Severe
                        ? "ui.wound.severe"
                        : "ui.wound.moderate"),
                    SimulationTimeText.FormatDurationLocalized(
                        hero.WoundRecoveryTicksRemaining)));
        }
        AddIconBody(
            hero.IsAtHome ? IconPaths.House : IconPaths.Building,
            UiText.Get(hero.IsAtHome ? "ui.hero_profile.at_home" : "ui.hero_profile.at_work"));
        AddIconBody(IconPaths.Sun, UiText.Format("ui.hero_profile.elemental_affinity", UiText.Get(hero.ElementalAffinity)));
    }

    private void AddStaminaBar(int current, int max)
    {
        var bar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = max,
            Value = current,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 14),
        };
        _content.AddChild(bar);
    }

    private void AddHeading(string text)
    {
        var label = new Label { Text = text };
        label.ThemeTypeVariation = "PanelTitle";
        _content.AddChild(label);
    }

    /// <summary>
    /// Centers the hero sprite above the title block. The sprite is the
    /// imported LPC scene for the hero's lineage + gender combination,
    /// resolved through <see cref="CharacterVisualRegistry"/>, played in
    /// its idle animation so the page communicates the hero is alive
    /// without forcing the player to read the body text first.
    /// </summary>
    private void AddHeroSprite(HeroProfileSnapshot hero)
    {
        _heroCarrier = CitizenSpriteBank.Instance.GetOrCreate(hero.Id, hero.Lineage, hero.Gender, hero.Appearance);
        _heroAnchor = new CenterContainer
        {
            CustomMinimumSize = new Vector2(0, PresentationConstants.DetailedCitizenHeight),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _content.AddChild(_heroAnchor);
        CitizenSpriteBank.Instance.Mount(_heroCarrier, _heroAnchor);
        CallDeferred(MethodName.PositionHeroCarrier);
    }

    private void PositionHeroCarrier()
    {
        if (_heroCarrier is null || _heroAnchor is null || !IsVisibleInTree()) return;
        Vector2 position = new(
            _heroAnchor.Size.X * 0.5f,
            PresentationConstants.DetailedCitizenHeight * 0.5f);
        _heroCarrier.SetPositionImmediate(position);
        _heroCarrier.SetState(CitizenSpriteCarrier.VisualState.HeroProfile);
        _heroCarrier.Idle(Vector2.Down);
    }

    private void HideHeroCarrier()
    {
        if (_heroCarrier?.State == CitizenSpriteCarrier.VisualState.HeroProfile)
        {
            _heroCarrier.SetState(CitizenSpriteCarrier.VisualState.Hidden);
        }
        _heroCarrier = null;
        _heroAnchor = null;
    }

    /// <summary>
    /// The hero's name is the page's primary subject and follows the
    /// bible's <c>ScreenTitle</c> tier (Geist Pixel, 36 px) rather than
    /// the panel-heading tier used by section labels below it. Preceded
    /// by a <c>user</c> icon so the page topic reads at a glance even
    /// when the text is partially scrolled. The 10-px gap keeps the
    /// glyph and the label distinct. The icon is tinted with the
    /// active linaje's accent.
    /// </summary>
    private void AddHeroName(string text)
    {
        var row = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        row.AddThemeConstantOverride("separation", 10);
        _content.AddChild(row);

        var icon = new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(IconPaths.User),
            StretchMode = TextureRect.StretchModeEnum.Keep,
            CustomMinimumSize = new Vector2(32, 32),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = LineageThemeRegistry.IconAccent,
        };
        row.AddChild(icon);

        var label = new Label { Text = text };
        label.ThemeTypeVariation = "ScreenTitle";
        label.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(label);
    }

    private void AddBody(string text)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        label.ThemeTypeVariation = "BodyText";
        _content.AddChild(label);
    }

    /// <summary>
    /// Body row with a leading icon. Used for status lines where the
    /// icon conveys the category at a glance (stamina = heart,
    /// location = house/building). The 10-px gap keeps the glyph
    /// distinct from the text. The icon is tinted with the active
    /// linaje's accent.
    /// </summary>
    private void AddIconBody(string iconPath, string text)
    {
        var row = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddThemeConstantOverride("separation", 10);
        _content.AddChild(row);

        var icon = new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(iconPath),
            StretchMode = TextureRect.StretchModeEnum.Keep,
            CustomMinimumSize = new Vector2(20, 20),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = LineageThemeRegistry.IconAccent,
        };
        row.AddChild(icon);

        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.ThemeTypeVariation = "BodyText";
        row.AddChild(label);
    }

    private void OnSelectionChanged(int selectionState)
    {
        if ((CityWorldController.Selection)selectionState == CityWorldController.Selection.HeroProfile)
        {
            Render();
            Show();
            Modulate = new Color(1f, 1f, 1f, 0f);
            var tween = CreateTween();
            tween.TweenProperty(this, "modulate:a", 1f, 0.2);
            _backButton.GrabFocus();
        }
        else
        {
            HideHeroCarrier();
            Hide();
        }
    }

    private void OnHeroCreated(int citizenId) => Render();

    private static string JoinLocalized(IEnumerable<string> values) =>
        string.Join(", ", values.Select(UiText.Get));
}
