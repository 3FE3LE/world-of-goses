#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

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
            Text = "Hero profile",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        title.ThemeTypeVariation = "ScreenTitle";
        header.AddChild(title);

        _backButton = StandardButtons.BackToCityButton();
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

        HeroProfileSnapshot? hero = _controller.GetHeroProfileSnapshot();
        if (hero is null)
        {
            AddBody("No hero has been created yet.");
            return;
        }

        AddHeroSprite(hero);
        AddHeroName($"{hero.Name} · {hero.LineageName}");
        AddBody("Role: Hero");
        AddBody(hero.LineageSummary);
        AddBody(hero.LearningApproach);
        AddBody(
            "Lineage describes common starting paths. It does not block any profession, " +
            "set a permanent ceiling, or grant automatic production.");

        AddHeading("Personal aptitudes");
        AddBody(Join(hero.Aptitudes));

        AddHeading("Professional affinities");
        AddBody(Join(hero.ProfessionalAffinities));
        AddBody(
            $"Common {hero.LineageName} paths: " + Join(hero.MarkedAffinities));

        AddHeading("Element and combat");
        AddBody($"Elemental affinity: {hero.ElementalAffinity}");
        AddBody($"Combat style: {hero.CombatStyle}");
        AddBody($"Weapon preferences: {Join(hero.WeaponPreferences)}");
        AddBody($"Gender: {hero.Gender}");

        AddHeading("Personality and worldview");
        AddBody($"Traits: {Join(hero.PersonalityTraits)}");
        AddBody($"Political orientation: {hero.PoliticalOrientation}");
        AddBody($"Spiritual posture: {hero.SpiritualPosture}");

        AddHeading("Current condition");
        AddIconBody(IconPaths.Heart, $"Stamina: {hero.CurrentStamina}/{hero.MaxStamina}");
        AddIconBody(
            hero.IsAtHome ? IconPaths.House : IconPaths.Building,
            hero.IsAtHome ? "At home" : "At work");
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
        var bodyVariant = CharacterVisualRegistry.ResolveBodyVariant(hero.Gender);
        var scene = CharacterVisualRegistry.LoadScene(hero.Lineage, bodyVariant);
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
