#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>Guided, complete creation flow for the world's principal hero.</summary>
public partial class OnboardingView : Control
{
    [Export] public NodePath ControllerPath { get; set; } = "../CityWorldController";

    private const int LastStep = 4;

    private readonly HashSet<AptitudeId> _aptitudes = new();
    private readonly HashSet<ProfessionFamilyId> _professions = new();
    private readonly HashSet<WeaponPreferenceId> _weapons = new();
    private readonly HashSet<PersonalityTraitId> _traits = new();

    private CityWorldController _controller = null!;
    private VBoxContainer _page = null!;
    private Label _stepLabel = null!;
    private Label _errorLabel = null!;
    private Label? _reviewLabel;
    private Button _backButton = null!;
    private Button _nextButton = null!;
    private Button _confirmButton = null!;
    private Control? _initialFocus;
    private int _step;
    private string _heroName = string.Empty;
    private LineageId? _lineage;
    private GenderId? _gender;
    private ElementalAffinityId? _element;
    private CombatStyleId? _combatStyle;
    private PoliticalOrientationId? _politics;
    private SpiritualPostureId? _spirituality;
    private LineageThemeSignals? _themeSignals;

    public override void _Ready()
    {
        _controller = GetNode<CityWorldController>(ControllerPath);
        _controller.HeroCreated += OnHeroCreated;
        BuildShell();
        AddThemeStyleboxOverride("panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
        _themeSignals = GetNodeOrNull<LineageThemeSignals>("/root/LineageThemeSignals");
        if (_themeSignals is not null)
        {
            _themeSignals.LineageChanged += OnLineageChanged;
        }
        Visible = _controller.NeedsOnboarding();
        if (Visible) ShowStep(0);
    }

    public override void _ExitTree()
    {
        if (_controller is not null) _controller.HeroCreated -= OnHeroCreated;
        if (_themeSignals is not null) _themeSignals.LineageChanged -= OnLineageChanged;
    }

    private void OnLineageChanged(string lineage) => AddThemeStyleboxOverride(
        "panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));

    private void BuildShell()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var background = new ColorRect
        {
            Color = new Color("10171f"),
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

        var title = new Label
        {
            Text = UiText.Get("Create your hero"),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.ThemeTypeVariation = "ScreenTitle";
        shell.AddChild(title);

        _stepLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _stepLabel.ThemeTypeVariation = "SectionTitle";
        shell.AddChild(_stepLabel);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        shell.AddChild(scroll);

        _page = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        _page.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_page);

        _errorLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _errorLabel.ThemeTypeVariation = "ErrorText";
        shell.AddChild(_errorLabel);

        var footer = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.End,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        footer.AddThemeConstantOverride("separation", 10);
        shell.AddChild(footer);

        _backButton = StandardButtons.NavigationButton(UiText.Get("Back"));
        _nextButton = StandardButtons.NavigationButton(UiText.Get("Next"));
        _confirmButton = StandardButtons.NavigationButton(UiText.Get("Create the hero"));
        _confirmButton.ThemeTypeVariation = "ButtonPrimary";
        _backButton.Pressed += OnBackPressed;
        _nextButton.Pressed += OnNextPressed;
        _confirmButton.Pressed += OnConfirmPressed;
        footer.AddChild(_backButton);
        footer.AddChild(_nextButton);
        footer.AddChild(_confirmButton);
    }

    private void ShowStep(int step)
    {
        _step = Math.Clamp(step, 0, LastStep);
        _reviewLabel = null;
        _initialFocus = null;
        ClearPage();

        switch (_step)
        {
            case 0:
                BuildIdentityStep();
                break;
            case 1:
                BuildAptitudesStep();
                break;
            case 2:
                BuildCombatStep();
                break;
            case 3:
                BuildTraitsStep();
                break;
            case 4:
                BuildWorldviewStep();
                break;
        }

        _stepLabel.Text = UiText.Format("ui.hero.step", _step + 1, LastStep + 1);
        UpdateNavigation();
        _initialFocus?.GrabFocus();
    }

    private void ClearPage()
    {
        foreach (var child in _page.GetChildren())
        {
            _page.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void BuildIdentityStep()
    {
        AddHeading(
            UiText.Get("Identity"),
            UiText.Get("Your lineage describes a common starting context, not a profession or destiny."));
        var name = new LineEdit
        {
            PlaceholderText = UiText.Get("Hero name"),
            Text = _heroName,
            MaxLength = 32,
            CustomMinimumSize = new Vector2(0, 42),
            FocusMode = FocusModeEnum.All,
            // The global theme defines Pixelify Sans for any LineEdit
            // (default_theme.tres:136-141), but assigning the variation
            // explicitly follows the typography-rule "every visible
            // text node carries an explicit variation in code".
            ThemeTypeVariation = "LineEdit",
        };
        name.TextChanged += value =>
        {
            _heroName = value;
            UpdateNavigation();
        };
        _page.AddChild(name);
        _initialFocus = name;

        // Hero preview anchored to the right of the description so the
        // player sees the sprite they are choosing as line/age options
        // change. Updated by ShowHeroPreview below.
        _heroPreviewSlot = new CenterContainer
        {
            CustomMinimumSize = new Vector2(128, 128),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _page.AddChild(_heroPreviewSlot);
        ShowHeroPreview();

        AddSectionTitle(UiText.Get("Lineage — choose one"));
        Label description = AddDescription(string.Empty);
        AddSingleChoiceGrid(
            ProfileCatalog.Lineages.Select(value =>
                new ProfileOption<LineageId>(value.Id, value.DisplayName, value.Summary)).ToArray(),
            _lineage,
            selected =>
            {
                _lineage = selected;
                var definition = ProfileCatalog.Get(selected);
                description.Text = FormatLineage(definition);
                ShowHeroPreview();
                UpdateNavigation();
            });
        if (_lineage.HasValue) description.Text = FormatLineage(ProfileCatalog.Get(_lineage.Value));

        AddSectionTitle(UiText.Get("Gender — choose one"));
        string genderBodyVariantTooltip = UiText.Get("Body variant used by the imported sprite.");
        AddSingleChoiceGrid(
            new[]
            {
                new ProfileOption<GenderId>(GenderId.Feminine, UiText.Get("Feminine"), genderBodyVariantTooltip),
                new ProfileOption<GenderId>(GenderId.Masculine, UiText.Get("Masculine"), genderBodyVariantTooltip),
            },
            _gender,
            selected =>
            {
                _gender = selected;
                ShowHeroPreview();
                UpdateNavigation();
            });
    }

    private CenterContainer _heroPreviewSlot = null!;

    /// <summary>
    /// Renders the hero sprite for the current lineage + gender pair so
    /// the player has a visual anchor while reading the descriptions.
    /// The slot is kept across re-renders so we can swap the contents
    /// without rebuilding the layout.
    /// </summary>
    private void ShowHeroPreview()
    {
        if (_heroPreviewSlot is null) return;
        foreach (var child in _heroPreviewSlot.GetChildren())
        {
            _heroPreviewSlot.RemoveChild(child);
            child.QueueFree();
        }
        if (!_lineage.HasValue || !_gender.HasValue) return;
        var bodyVariant = CharacterVisualRegistry.ResolveBodyVariant(_gender.Value);
        var scene = CharacterVisualRegistry.LoadScene(_lineage.Value, bodyVariant);
        var sprite = scene.Instantiate<LineageSpritePlayer>();
        _heroPreviewSlot.AddChild(sprite);
    }

    private void BuildAptitudesStep()
    {
        AddHeading(
            UiText.Get("Personal paths"),
            UiText.Get("Individual aptitudes and professional affinities can reinforce or contradict lineage tendencies."));
        AddSectionTitle(UiText.Format("ui.hero.aptitudes_choose", _aptitudes.Count));
        AddMultipleChoiceGrid(ProfileCatalog.Aptitudes, _aptitudes, 3);
        AddSectionTitle(UiText.Format("ui.hero.affinities_choose", _professions.Count));
        AddMultipleChoiceGrid(ProfileCatalog.ProfessionFamilies, _professions, 3);
    }

    private void BuildCombatStep()
    {
        AddHeading(
            UiText.Get("Element and combat"),
            UiText.Get("These choices describe preference. They do not prevent the hero from learning another weapon or role."));
        AddSectionTitle(UiText.Get("Elemental affinity — choose one"));
        AddSingleChoiceGrid(ProfileCatalog.ElementalAffinities, _element, value =>
        {
            _element = value;
            UpdateNavigation();
        });
        AddSectionTitle(UiText.Get("Combat style — choose one"));
        AddSingleChoiceGrid(ProfileCatalog.CombatStyles, _combatStyle, value =>
        {
            _combatStyle = value;
            UpdateNavigation();
        });
        AddSectionTitle(UiText.Format("ui.hero.weapons_choose", _weapons.Count));
        AddMultipleChoiceGrid(ProfileCatalog.WeaponPreferences, _weapons, 2);
    }

    private void BuildTraitsStep()
    {
        AddHeading(
            UiText.Get("Personality"),
            UiText.Get("Traits create tendencies and tensions. None is inherently virtuous or defective."));
        AddSectionTitle(UiText.Format("ui.hero.traits_choose", _traits.Count));
        AddMultipleChoiceGrid(ProfileCatalog.PersonalityTraits, _traits, 3);
    }

    private void BuildWorldviewStep()
    {
        AddHeading(
            UiText.Get("Worldview and review"),
            UiText.Get("Political orientation and spiritual posture are separate, descriptive choices."));
        AddSectionTitle(UiText.Get("Political orientation — choose one"));
        AddSingleChoiceGrid(ProfileCatalog.PoliticalOrientations, _politics, value =>
        {
            _politics = value;
            UpdateNavigation();
        });
        AddSectionTitle(UiText.Get("Spiritual posture — choose one"));
        AddSingleChoiceGrid(ProfileCatalog.SpiritualPostures, _spirituality, value =>
        {
            _spirituality = value;
            UpdateNavigation();
        });
        AddSectionTitle(UiText.Get("Profile review"));
        AddReviewCard();
        _reviewLabel = AddDescription(string.Empty);
        UpdateReview();
    }

    private CenterContainer _reviewSpriteSlot = null!;

    private void AddReviewCard()
    {
        _reviewSpriteSlot = new CenterContainer
        {
            CustomMinimumSize = new Vector2(0, 128),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _page.AddChild(_reviewSpriteSlot);
        UpdateReviewSprite();
    }

    private void UpdateReviewSprite()
    {
        if (_reviewSpriteSlot is null) return;
        foreach (var child in _reviewSpriteSlot.GetChildren())
        {
            _reviewSpriteSlot.RemoveChild(child);
            child.QueueFree();
        }
        if (!_lineage.HasValue || !_gender.HasValue) return;
        var bodyVariant = CharacterVisualRegistry.ResolveBodyVariant(_gender.Value);
        var scene = CharacterVisualRegistry.LoadScene(_lineage.Value, bodyVariant);
        var sprite = scene.Instantiate<LineageSpritePlayer>();
        _reviewSpriteSlot.AddChild(sprite);
    }

    private void AddHeading(string title, string description)
    {
        var heading = new Label { Text = title };
        heading.ThemeTypeVariation = "PanelTitle";
        _page.AddChild(heading);
        AddDescription(description);
    }

    private void AddSectionTitle(string text)
    {
        var label = new Label { Text = text };
        label.ThemeTypeVariation = "SectionTitle";
        _page.AddChild(label);
    }

    private Label AddDescription(string text)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        label.ThemeTypeVariation = "BodyText";
        _page.AddChild(label);
        return label;
    }

    private void AddSingleChoiceGrid<TId>(
        IReadOnlyList<ProfileOption<TId>> options,
        TId? selected,
        Action<TId> onSelected)
        where TId : struct
    {
        var group = new ButtonGroup { AllowUnpress = false };
        var grid = NewChoiceGrid();
        foreach (var option in options)
        {
            var button = StandardButtons.ChoiceButton(UiText.Get(option.DisplayName), UiText.Get(option.Description));
            button.ToggleMode = true;
            button.ButtonGroup = group;
            button.SetPressedNoSignal(selected.HasValue
                && EqualityComparer<TId>.Default.Equals(selected.Value, option.Id));
            TId id = option.Id;
            button.Toggled += pressed =>
            {
                if (pressed) onSelected(id);
            };
            grid.AddChild(button);
            _initialFocus ??= button;
        }
        _page.AddChild(grid);
    }

    private void AddMultipleChoiceGrid<TId>(
        IReadOnlyList<ProfileOption<TId>> options,
        HashSet<TId> selected,
        int maximum)
        where TId : struct
    {
        var grid = NewChoiceGrid();
        foreach (var option in options)
        {
            var button = StandardButtons.ChoiceButton(UiText.Get(option.DisplayName), UiText.Get(option.Description));
            button.ToggleMode = true;
            button.SetPressedNoSignal(selected.Contains(option.Id));
            TId id = option.Id;
            button.Toggled += pressed =>
            {
                if (pressed)
                {
                    if (selected.Count >= maximum)
                    {
                        button.SetPressedNoSignal(false);
                        _errorLabel.Text = UiText.Format("ui.hero.maximum", maximum);
                        return;
                    }
                    selected.Add(id);
                }
                else
                {
                    selected.Remove(id);
                }
                ShowStep(_step);
            };
            grid.AddChild(button);
            _initialFocus ??= button;
        }
        _page.AddChild(grid);
    }

    private GridContainer NewChoiceGrid()
    {
        // On short screens the 2-column grid forces the bottom row to
        // hide the footer. Collapse to a single column when the
        // viewport is unusually short so every option fits.
        var viewport = GetViewportRect().Size;
        return new GridContainer
        {
            Columns = viewport.Y < 720f ? 1 : 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
    }

    private void OnBackPressed()
    {
        if (_step > 0) ShowStep(_step - 1);
    }

    private void OnNextPressed()
    {
        if (IsStepValid(_step)) ShowStep(_step + 1);
    }

    private void OnConfirmPressed()
    {
        if (!TryBuildProfile(out CitizenProfile? profile, out string error))
        {
            _errorLabel.Text = error;
            return;
        }

        var result = _controller.TryCompleteOnboarding(
            new HeroCreationRequest(_heroName, profile!, _gender!.Value));
        if (!result.IsSuccess)
        {
            _errorLabel.Text = UiText.Format("ui.hero.creation_failed", result.Outcome);
            return;
        }
        var signals = GetNodeOrNull<LineageThemeSignals>("/root/LineageThemeSignals");
        if (signals is not null)
        {
            signals.ApplyLineage(profile!.Lineage);
        }
        LineageThemeRegistry.ActiveLineage = LineageThemeRegistry.IdOf(profile!.Lineage);
    }

    private void OnHeroCreated(int citizenId) => Hide();

    private void UpdateNavigation()
    {
        _backButton.Disabled = _step == 0;
        _nextButton.Visible = _step < LastStep;
        _confirmButton.Visible = _step == LastStep;
        _nextButton.Disabled = !IsStepValid(_step);
        _confirmButton.Disabled = !TryBuildProfile(out _, out string error);
        _errorLabel.Text = IsStepValid(_step) || string.IsNullOrEmpty(error) ? string.Empty : error;
        UpdateReview();
    }

    private bool IsStepValid(int step)
    {
        return step switch
        {
            0 => IsNameValid(_heroName) && _lineage.HasValue && _gender.HasValue,
            1 => _aptitudes.Count == 3 && _professions.Count == 3,
            2 => _element.HasValue && _combatStyle.HasValue && _weapons.Count is >= 1 and <= 2,
            3 => _traits.Count == 3,
            4 => _politics.HasValue && _spirituality.HasValue,
            _ => false,
        };
    }

    private bool TryBuildProfile(out CitizenProfile? profile, out string error)
    {
        profile = null;
        error = string.Empty;
        if (!IsNameValid(_heroName))
        {
            error = UiText.Get("Enter a name between 1 and 32 characters.");
            return false;
        }
        if (!_lineage.HasValue || !_gender.HasValue || !_element.HasValue
            || !_combatStyle.HasValue || !_politics.HasValue || !_spirituality.HasValue)
        {
            error = UiText.Get("Complete every single-choice section.");
            return false;
        }

        return CitizenProfile.TryCreate(
            _lineage.Value,
            _gender!.Value,
            _aptitudes,
            _professions,
            _element.Value,
            _combatStyle.Value,
            _weapons,
            _traits,
            _politics.Value,
            _spirituality.Value,
            out profile,
            out error);
    }

    private void UpdateReview()
    {
        if (_reviewLabel is null) return;
        if (!_lineage.HasValue || !_gender.HasValue)
        {
            _reviewLabel.Text = UiText.Get("Complete the earlier steps to review the profile.");
            return;
        }
        UpdateReviewSprite();

        FounderCubeProfile cube = CubeScoring.ComputeCubeVertex(_lineage.Value);

        _reviewLabel.Text =
            UiText.Format("ui.hero.review_name", _heroName.Trim()) + "\n" +
            UiText.Format("ui.hero.review_lineage", ProfileCatalog.Get(_lineage.Value).DisplayName) + "\n" +
            UiText.Format("ui.hero.review_gender", UiText.Get(_gender.Value.ToString())) + "\n" +
            UiText.Format("ui.hero.review_element", DisplayLocalized(_element, ProfileCatalog.DisplayName)) + "\n" +
            $"{UiText.Get("Cuerpo")} {cube.Body} / {cube.Bond} {UiText.Get("Vínculo")}\n" +
            $"{UiText.Get("Estabilidad")} {cube.Stability} / {cube.Impulse} {UiText.Get("Impulso")}\n" +
            $"{UiText.Get("Dominio")} {cube.Mastery} / {cube.Reach} {UiText.Get("Alcance")}\n" +
            UiText.Get(ProfileCatalog.Get(_lineage.Value).Summary);
    }

    private static bool IsNameValid(string value)
    {
        string trimmed = value.Trim();
        if (trimmed.Length is < 1 or > 32) return false;
        return !trimmed.Any(char.IsControl);
    }

    private static string FormatLineage(LineageDefinition lineage) =>
        $"{UiText.Get(lineage.Summary)}\n{UiText.Get(lineage.LearningApproach)}\n\n" +
        UiText.Format("ui.hero.common_marked_paths", JoinLocalized(lineage.MarkedAffinities.Select(ProfileCatalog.DisplayName))) + "\n" +
        UiText.Get("These are starting tendencies only. Any profession remains learnable.");

    private static string DisplayLocalized<TId>(TId? value, Func<TId, string> display)
        where TId : struct => value.HasValue ? UiText.Get(display(value.Value)) : UiText.Get("not selected");

    private static string JoinLocalized(IEnumerable<string> values)
    {
        string[] materialised = values.Select(UiText.Get).ToArray();
        return materialised.Length == 0 ? UiText.Get("none selected") : string.Join(", ", materialised);
    }
}
