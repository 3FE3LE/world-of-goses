#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>Read-only presentation of every persisted hero profile choice.</summary>
public partial class HeroProfileView : Control
{
    [Export] public NodePath ControllerPath { get; set; } = "../CityWorldController";

    private CityWorldController _controller = null!;
    private VBoxContainer _content = null!;
    private Button _backButton = null!;
    private LineageSpritePlayer? _heroSprite;

    public override void _Ready()
    {
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

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_right", 28);
        margin.AddThemeConstantOverride("margin_top", 24);
        margin.AddThemeConstantOverride("margin_bottom", 24);
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
            Text = "Hero profile",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        title.ThemeTypeVariation = "ScreenTitle";
        title.AddThemeFontSizeOverride("font_size", 36);
        header.AddChild(title);

        _backButton = new Button
        {
            Text = "Back to city",
            ThemeTypeVariation = "ButtonText",
            CustomMinimumSize = new Vector2(150, 44),
            FocusMode = FocusModeEnum.All,
        };
        _backButton.Pressed += () => _controller.ReturnToCity();
        header.AddChild(_backButton);

        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        shell.AddChild(scroll);

        _content = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        _content.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_content);
    }

    private void Render()
    {
        foreach (var child in _content.GetChildren())
        {
            _content.RemoveChild(child);
            child.QueueFree();
        }

        // Drop the cached sprite so a re-render after a lineage or
        // gender change loads the new scene.
        if (_heroSprite is not null)
        {
            _heroSprite.QueueFree();
            _heroSprite = null;
        }

        Citizen? hero = _controller.HeroOrNull();
        if (hero is null)
        {
            AddBody("No hero has been created yet.");
            return;
        }

        CitizenProfile profile = hero.Profile;
        LineageDefinition lineage = ProfileCatalog.Get(profile.Lineage);
        AddHeroSprite(hero);
        AddHeroName($"{hero.Name} · {lineage.DisplayName}");
        AddBody("Role: Hero");
        AddBody(lineage.Summary);
        AddBody(lineage.LearningApproach);
        AddBody(
            "Lineage describes common starting paths. It does not block any profession, " +
            "set a permanent ceiling, or grant automatic production.");

        AddHeading("Personal aptitudes");
        AddBody(Join(profile.Aptitudes.Select(ProfileCatalog.DisplayName)));

        AddHeading("Professional affinities");
        AddBody(Join(profile.ProfessionalAffinities.Select(ProfileCatalog.DisplayName)));
        AddBody(
            $"Common {lineage.DisplayName} paths: " +
            Join(lineage.MarkedAffinities.Select(ProfileCatalog.DisplayName)));

        AddHeading("Element and combat");
        AddBody($"Elemental affinity: {ProfileCatalog.DisplayName(profile.ElementalAffinity)}");
        AddBody($"Combat style: {ProfileCatalog.DisplayName(profile.CombatStyle)}");
        AddBody($"Weapon preferences: {Join(profile.WeaponPreferences.Select(ProfileCatalog.DisplayName))}");
        AddBody($"Gender: {profile.Gender}");

        AddHeading("Personality and worldview");
        AddBody($"Traits: {Join(profile.PersonalityTraits.Select(ProfileCatalog.DisplayName))}");
        AddBody($"Political orientation: {ProfileCatalog.DisplayName(profile.PoliticalOrientation)}");
        AddBody($"Spiritual posture: {ProfileCatalog.DisplayName(profile.SpiritualPosture)}");

        AddHeading("Current condition");
        AddIconBody(IconPaths.Heart, $"Stamina: {hero.CurrentStamina}/{hero.MaxStamina}");
        AddIconBody(
            hero.CurrentLocation == CitizenLocation.AtHome ? IconPaths.House : IconPaths.Building,
            hero.CurrentLocation == CitizenLocation.AtHome ? "At home" : "At work");
    }

    private void AddHeading(string text)
    {
        var label = new Label { Text = text };
        label.ThemeTypeVariation = "PanelTitle";
        label.AddThemeFontSizeOverride("font_size", 26);
        _content.AddChild(label);
    }

    /// <summary>
    /// Centers the hero sprite above the title block. The sprite is the
    /// imported LPC scene for the hero's lineage + gender combination,
    /// resolved through <see cref="CharacterVisualRegistry"/>, played in
    /// its idle animation so the page communicates the hero is alive
    /// without forcing the player to read the body text first.
    /// </summary>
    private void AddHeroSprite(Citizen hero)
    {
        var bodyVariant = CharacterVisualRegistry.ResolveBodyVariant(hero.Profile.Gender);
        var scene = CharacterVisualRegistry.LoadScene(hero.Profile.Lineage, bodyVariant);
        _heroSprite = scene.Instantiate<LineageSpritePlayer>();
        _heroSprite.Position = new Vector2(0, 0);
        var centered = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        centered.AddChild(_heroSprite);
        _content.AddChild(centered);
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
        label.AddThemeFontSizeOverride("font_size", 36);
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
            _backButton.GrabFocus();
        }
        else
        {
            Hide();
        }
    }

    private void OnHeroCreated(int citizenId) => Render();

    private static string Join(IEnumerable<string> values) => string.Join(", ", values);
}
