#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;

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

        var title = new Label
        {
            Text = "Create your hero",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.ThemeTypeVariation = "ScreenTitle";
        title.AddThemeFontSizeOverride("font_size", 36);
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
        _errorLabel.ThemeTypeVariation = "BodySmall";
        _errorLabel.AddThemeColorOverride("font_color", new Color("ef8f7a"));
        shell.AddChild(_errorLabel);

        var footer = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.End,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        footer.AddThemeConstantOverride("separation", 10);
        shell.AddChild(footer);

        _backButton = NewNavigationButton("Back");
        _nextButton = NewNavigationButton("Next");
        _confirmButton = NewNavigationButton("Found the city");
        _confirmButton.ThemeTypeVariation = "ButtonPrimary";
        _backButton.Pressed += OnBackPressed;
        _nextButton.Pressed += OnNextPressed;
        _confirmButton.Pressed += OnConfirmPressed;
        footer.AddChild(_backButton);
        footer.AddChild(_nextButton);
        footer.AddChild(_confirmButton);
    }

    private static Button NewNavigationButton(string text) => new()
    {
        Text = text,
        ThemeTypeVariation = "ButtonText",
        CustomMinimumSize = new Vector2(150, 44),
        FocusMode = FocusModeEnum.All,
    };

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

        _stepLabel.Text = $"Step {_step + 1} of {LastStep + 1}";
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
        AddHeading("Identity", "Your lineage describes a common starting context, not a profession or destiny.");
        var name = new LineEdit
        {
            PlaceholderText = "Hero name",
            Text = _heroName,
            MaxLength = 32,
            CustomMinimumSize = new Vector2(0, 42),
            FocusMode = FocusModeEnum.All,
        };
        name.TextChanged += value =>
        {
            _heroName = value;
            UpdateNavigation();
        };
        _page.AddChild(name);
        _initialFocus = name;

        AddSectionTitle("Lineage — choose one");
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
                UpdateNavigation();
            });
        if (_lineage.HasValue) description.Text = FormatLineage(ProfileCatalog.Get(_lineage.Value));
    }

    private void BuildAptitudesStep()
    {
        AddHeading(
            "Personal paths",
            "Individual aptitudes and professional affinities can reinforce or contradict lineage tendencies.");
        AddSectionTitle($"Personal aptitudes — choose exactly 3 ({_aptitudes.Count}/3)");
        AddMultipleChoiceGrid(ProfileCatalog.Aptitudes, _aptitudes, 3);
        AddSectionTitle($"Professional affinities — choose exactly 3 ({_professions.Count}/3)");
        AddMultipleChoiceGrid(ProfileCatalog.ProfessionFamilies, _professions, 3);
    }

    private void BuildCombatStep()
    {
        AddHeading(
            "Element and combat",
            "These choices describe preference. They do not prevent the hero from learning another weapon or role.");
        AddSectionTitle("Elemental affinity — choose one");
        AddSingleChoiceGrid(ProfileCatalog.ElementalAffinities, _element, value =>
        {
            _element = value;
            UpdateNavigation();
        });
        AddSectionTitle("Combat style — choose one");
        AddSingleChoiceGrid(ProfileCatalog.CombatStyles, _combatStyle, value =>
        {
            _combatStyle = value;
            UpdateNavigation();
        });
        AddSectionTitle($"Weapon preferences — choose 1 or 2 ({_weapons.Count}/2)");
        AddMultipleChoiceGrid(ProfileCatalog.WeaponPreferences, _weapons, 2);
    }

    private void BuildTraitsStep()
    {
        AddHeading(
            "Personality",
            "Traits create tendencies and tensions. None is inherently virtuous or defective.");
        AddSectionTitle($"Traits — choose exactly 3 ({_traits.Count}/3)");
        AddMultipleChoiceGrid(ProfileCatalog.PersonalityTraits, _traits, 3);
    }

    private void BuildWorldviewStep()
    {
        AddHeading(
            "Worldview and review",
            "Political orientation and spiritual posture are separate, descriptive choices.");
        AddSectionTitle("Political orientation — choose one");
        AddSingleChoiceGrid(ProfileCatalog.PoliticalOrientations, _politics, value =>
        {
            _politics = value;
            UpdateNavigation();
        });
        AddSectionTitle("Spiritual posture — choose one");
        AddSingleChoiceGrid(ProfileCatalog.SpiritualPostures, _spirituality, value =>
        {
            _spirituality = value;
            UpdateNavigation();
        });
        AddSectionTitle("Profile review");
        _reviewLabel = AddDescription(string.Empty);
        UpdateReview();
    }

    private void AddHeading(string title, string description)
    {
        var heading = new Label { Text = title };
        heading.ThemeTypeVariation = "PanelTitle";
        heading.AddThemeFontSizeOverride("font_size", 26);
        _page.AddChild(heading);
        AddDescription(description);
    }

    private void AddSectionTitle(string text)
    {
        var label = new Label { Text = text };
        label.ThemeTypeVariation = "SectionTitle";
        label.AddThemeFontSizeOverride("font_size", 22);
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
            var button = NewChoiceButton(option.DisplayName, option.Description);
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
            var button = NewChoiceButton(option.DisplayName, option.Description);
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
                        _errorLabel.Text = $"You can select at most {maximum}.";
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

    private static GridContainer NewChoiceGrid() => new()
    {
        Columns = 2,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
    };

    private static Button NewChoiceButton(string text, string tooltip) => new()
    {
        Text = text,
        TooltipText = tooltip,
        ThemeTypeVariation = "ButtonText",
        CustomMinimumSize = new Vector2(230, 44),
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
        FocusMode = FocusModeEnum.All,
    };

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
            new HeroCreationRequest(_heroName, profile!));
        if (!result.IsSuccess)
        {
            _errorLabel.Text = $"The hero could not be created ({result.Outcome}).";
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
            0 => IsNameValid(_heroName) && _lineage.HasValue,
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
            error = "Enter a name between 1 and 32 characters.";
            return false;
        }
        if (!_lineage.HasValue || !_element.HasValue || !_combatStyle.HasValue
            || !_politics.HasValue || !_spirituality.HasValue)
        {
            error = "Complete every single-choice section.";
            return false;
        }

        return CitizenProfile.TryCreate(
            _lineage.Value,
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
        if (!_lineage.HasValue)
        {
            _reviewLabel.Text = "Complete the earlier steps to review the profile.";
            return;
        }

        _reviewLabel.Text =
            $"Name: {_heroName.Trim()}\n" +
            $"Lineage: {ProfileCatalog.Get(_lineage.Value).DisplayName}\n" +
            $"Aptitudes: {Join(_aptitudes.Select(ProfileCatalog.DisplayName))}\n" +
            $"Professional affinities: {Join(_professions.Select(ProfileCatalog.DisplayName))}\n" +
            $"Element: {Display(_element, ProfileCatalog.DisplayName)}\n" +
            $"Combat: {Display(_combatStyle, ProfileCatalog.DisplayName)} · {Join(_weapons.Select(ProfileCatalog.DisplayName))}\n" +
            $"Traits: {Join(_traits.Select(ProfileCatalog.DisplayName))}\n" +
            $"Politics: {Display(_politics, ProfileCatalog.DisplayName)}\n" +
            $"Spirituality: {Display(_spirituality, ProfileCatalog.DisplayName)}";
    }

    private static bool IsNameValid(string value)
    {
        string trimmed = value.Trim();
        if (trimmed.Length is < 1 or > 32) return false;
        return !trimmed.Any(char.IsControl);
    }

    private static string FormatLineage(LineageDefinition lineage) =>
        $"{lineage.Summary}\n{lineage.LearningApproach}\n\n" +
        $"Common marked paths: {Join(lineage.MarkedAffinities.Select(ProfileCatalog.DisplayName))}.\n" +
        "These are starting tendencies only. Any profession remains learnable.";

    private static string Display<TId>(TId? value, Func<TId, string> display)
        where TId : struct => value.HasValue ? display(value.Value) : "not selected";

    private static string Join(IEnumerable<string> values)
    {
        string[] materialised = values.ToArray();
        return materialised.Length == 0 ? "none selected" : string.Join(", ", materialised);
    }
}
