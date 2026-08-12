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

    /// <summary>
    /// Binds the shell authored in <c>game/scenes/HeroProfileView.tscn</c>.
    /// </summary>
    /// <remarks>
    /// The portrait sits on the left at full height with its width computed
    /// rather than left to an expand mode. The art is authored portrait and
    /// the set does not share one aspect ratio — nine are 3:4, seven are 4:5 —
    /// so a fixed frame would letterbox one group or crop the other. Every
    /// proportional mode derives the width from a height that is still
    /// unresolved on the first layout pass, so the portrait reports a zero
    /// minimum width, the row gives it none, and the art loads but is never
    /// drawn. <see cref="UpdateSplashWidth"/> recomputes it on resize, which
    /// also keeps it right across window sizes and both aspect groups.
    ///
    /// <para>The splash uses linear filtering with mipmaps: it is displayed
    /// downscaled from its authored size, where nearest reads far worse. A
    /// deliberate, local exception — in-world pixel art keeps nearest.</para>
    /// </remarks>
    private void BuildShell()
    {
        _splashColumns = GetNode<HBoxContainer>("SafeArea/Shell/Columns");
        _splash = GetNode<TextureRect>("SafeArea/Shell/Columns/Splash");
        _content = GetNode<VBoxContainer>("SafeArea/Shell/Columns/Scroll/ContentMargin/Content");

        GetNode<Label>("SafeArea/Shell/Header/Title").Text =
            UiText.Get("ui.hero_profile.title");
        _backButton = GetNode<Button>("SafeArea/Shell/Header/BackButton");
        _backButton.Pressed += OnBackPressed;

        _splashColumns.Resized += UpdateSplashWidth;
    }

    private void OnBackPressed() => _controller.ReturnToCity();

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

        AddHeading(UiText.Get("Perfil de encarnación"));
        AddBody($"{UiText.Get("Cuerpo")} {hero.CubeProfile.Body} / {hero.CubeProfile.Bond} {UiText.Get("Vínculo")}");
        AddBody($"{UiText.Get("Estabilidad")} {hero.CubeProfile.Stability} / {hero.CubeProfile.Impulse} {UiText.Get("Impulso")}");
        AddBody($"{UiText.Get("Dominio")} {hero.CubeProfile.Domain} / {hero.CubeProfile.Reach} {UiText.Get("Alcance")}");
        AddBody($"{UiText.Get("Firma")} · {UiText.Get(hero.LineageSignature)}");

        AddHeading(UiText.Get("Afinidad"));
        AddBody(UiText.Format("ui.hero_profile.elemental_affinity", UiText.Get(hero.ElementalAffinity)));
        // The physical expression is derived from the affinity, so it belongs on
        // the same card. Onboarding produces both and only the affinity was shown.
        AddBody(UiText.Format(
            "ui.citizen.physical_expression",
            UiText.Get(hero.PhysicalExpression)));
        AddBody(UiText.Format(
            "ui.citizen.natural_weapons",
            UiText.Get(hero.NaturalWeaponFamilies[0]),
            UiText.Get(hero.NaturalWeaponFamilies[1])));
        AddBody(UiText.Get("ui.hero_profile.natural_weapons_hint"));
        AddBody(UiText.Format("ui.hero_profile.gender", GenderIdLocalizer.Label(hero.Gender)));

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
        bar.ThemeTypeVariation = "HudProgress";
        _content.AddChild(bar);
    }

    private void AddHeading(string text)
    {
        var label = new Label { Text = text };
        label.ThemeTypeVariation = "HudHeader";
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
        row.AddThemeConstantOverride("separation", Tokens.SpacingRelaxed);
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
        label.ThemeTypeVariation = "HudBody";
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
        row.AddThemeConstantOverride("separation", Tokens.SpacingRelaxed);
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
        label.ThemeTypeVariation = "HudBody";
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
