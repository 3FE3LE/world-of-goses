#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Narrative reconstruction of the founder. Content and scoring live in the
/// domain; this node owns only navigation, focus and presentation.
/// </summary>
public partial class AstralOnboardingView : Control
{
    [Export] public NodePath ControllerPath { get; set; } = "../CityWorldController";
    [Export] public NodePath CityViewPath { get; set; } =
        "../GameUiShell/ScreenContent/CityMacroView";

    private readonly FounderNarrativeSession _session = new();
    private readonly Dictionary<string, Button> _choiceButtons = new();
    private CityWorldController _controller = null!;
    private CityMacroView _cityView = null!;
    private ColorRect _astralVeil = null!;
    private HBoxContainer _fragments = null!;
    private Label _progress = null!;
    private Label _title = null!;
    private Label _narrative = null!;
    private VBoxContainer _choices = null!;
    private Label _consequence = null!;
    private Label _error = null!;
    private Button _back = null!;
    private Button _next = null!;
    private int _step;
    private FounderNarrativeResult? _result;
    private string _founderName = string.Empty;
    private GenderId? _gender;
    private LineEdit? _nameEdit;
    private bool _identityStage;
    private LocaleManager? _localeManager;

    public override void _Ready()
    {
        _controller = GetNode<CityWorldController>(ControllerPath);
        _cityView = GetNode<CityMacroView>(CityViewPath);
        _localeManager = GetNodeOrNull<LocaleManager>("/root/LocaleManager");
        if (_localeManager is not null) _localeManager.LocaleChanged += OnLocaleChanged;
        BuildShell();
        Visible = _controller.NeedsOnboarding();
        if (Visible) RenderQuestion();
    }

    private void BuildShell()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        OverlayLayers.Apply(this, OverlayLayers.Onboarding);

        _astralVeil = new ColorRect
        {
            Color = new Color(0.015f, 0.02f, 0.055f, 1f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        _astralVeil.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_astralVeil);

        var glow = new ColorRect
        {
            Color = new Color(0.15f, 0.08f, 0.28f, 0.16f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        glow.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(glow);

        var safe = new SafeAreaMarginContainer { MinimumInset = 32 };
        safe.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(safe);

        var shell = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        shell.AddThemeConstantOverride("separation", 12);
        safe.AddChild(shell);

        _progress = NewLabel("SectionTitle", HorizontalAlignment.Center);
        shell.AddChild(_progress);
        _fragments = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _fragments.AddThemeConstantOverride("separation", 8);
        shell.AddChild(_fragments);

        _title = NewLabel("ScreenTitle", HorizontalAlignment.Center);
        shell.AddChild(_title);
        _narrative = NewLabel("BodyText", HorizontalAlignment.Center);
        _narrative.CustomMinimumSize = new Vector2(0, 92);
        shell.AddChild(_narrative);

        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        shell.AddChild(scroll);
        _choices = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _choices.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_choices);

        _consequence = NewLabel("BodyText", HorizontalAlignment.Center);
        _consequence.CustomMinimumSize = new Vector2(0, 52);
        shell.AddChild(_consequence);
        _error = NewLabel("ErrorText", HorizontalAlignment.Center);
        shell.AddChild(_error);

        var footer = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.End,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        footer.AddThemeConstantOverride("separation", 10);
        shell.AddChild(footer);
        _back = StandardButtons.NavigationButton(TrKey("ui.onboarding.back"));
        _next = StandardButtons.NavigationButton(TrKey("ui.onboarding.stabilise"));
        _next.ThemeTypeVariation = "ButtonPrimary";
        _back.Pressed += OnBack;
        _next.Pressed += OnNext;
        footer.AddChild(_back);
        footer.AddChild(_next);
    }

    private void RenderQuestion()
    {
        FounderNarrativeQuestion question = FounderNarrativeCatalog.Questions[_step];
        ClearChoices();
        _progress.Text = string.Format(
            TrKey("ui.onboarding.progress"),
            _session.Answers.Count,
            FounderNarrativeCatalog.Questions.Count);
        BuildFragments();
        _title.Text = TrKey(question.Title);
        _narrative.Text = TrKey(question.Text);
        _consequence.Text = string.Empty;
        _error.Text = string.Empty;
        _astralVeil.Color = new Color(0.015f, 0.02f, 0.055f, 1f - question.TerrainReveal);
        _back.Disabled = _step == 0;
        _next.Text = _step == FounderNarrativeCatalog.Questions.Count - 1
            ? TrKey("ui.onboarding.shape_memory")
            : TrKey("ui.onboarding.stabilise");

        _choiceButtons.Clear();
        foreach (FounderNarrativeChoice choice in question.Choices)
        {
            var button = new Button
            {
                Text = TrKey(choice.Text),
                CustomMinimumSize = new Vector2(0, 66),
                Alignment = HorizontalAlignment.Left,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                ThemeTypeVariation = "ButtonText",
            };
            string choiceId = choice.Id;
            button.Pressed += () => SelectChoice(question.Id, choiceId);
            _choices.AddChild(button);
            _choiceButtons[choice.Id] = button;
            FadeIn(button, 0.12 + _choiceButtons.Count * 0.05);
        }
        if (_session.TryGetAnswer(question.Id, out string selected))
        {
            ApplySelectedState(question, selected);
        }
        else
        {
            _next.Disabled = true;
        }
        FadeIn(_title, 0.18);
        FadeIn(_narrative, 0.24);
        (_session.TryGetAnswer(question.Id, out string focusId)
            ? _choiceButtons[focusId]
            : _choiceButtons[question.Choices[0].Id]).GrabFocus();
    }

    private void SelectChoice(string questionId, string choiceId)
    {
        _session.Answer(questionId, choiceId);
        ApplySelectedState(FounderNarrativeCatalog.GetQuestion(questionId), choiceId);
        BuildFragments();
    }

    private void ApplySelectedState(FounderNarrativeQuestion question, string choiceId)
    {
        foreach ((string id, Button button) in _choiceButtons)
        {
            button.ThemeTypeVariation = id == choiceId ? "ButtonPrimary" : "ButtonText";
        }
        FounderNarrativeChoice choice = FindChoice(question, choiceId);
        _consequence.Text = TrKey(choice.ImmediateConsequence);
        FadeIn(_consequence, 0.18);
        _next.Disabled = false;
    }

    private void OnBack()
    {
        if (_identityStage)
        {
            ReturnToLastQuestion();
            return;
        }
        if (_step <= 0) return;
        _step--;
        RenderQuestion();
    }

    /// <summary>
    /// Resolves a translation key via the active <see cref="LocaleManager"/>
    /// when one is registered, or returns the key literal otherwise
    /// (graceful degradation during capture mode and headless boot
    /// before the autoload has finished its own <c>_Ready</c>).
    /// </summary>
    private string TrKey(string key) =>
        string.IsNullOrEmpty(key)
            ? string.Empty
            : GetNodeOrNull<LocaleManager>("/root/LocaleManager")?.Translate(key) ?? key;

    private void OnLocaleChanged(string locale)
    {
        _ = locale;
        _back.Text = TrKey("ui.onboarding.back");
        if (_identityStage) RenderIdentity();
        else RenderQuestion();
    }

    public override void _ExitTree()
    {
        if (_localeManager is not null) _localeManager.LocaleChanged -= OnLocaleChanged;
    }

    private void OnNext()
    {
        if (_identityStage)
        {
            OnConfirmIdentity();
            return;
        }
        FounderNarrativeQuestion current = FounderNarrativeCatalog.Questions[_step];
        if (!_session.TryGetAnswer(current.Id, out _)) return;
        if (_step < FounderNarrativeCatalog.Questions.Count - 1)
        {
            _step++;
            RenderQuestion();
            return;
        }
        _result = FounderNarrativeScorer.Calculate(_session);
        RenderIdentity();
    }

    private void RenderIdentity()
    {
        _identityStage = true;
        ClearChoices();
        _astralVeil.Color = new Color(0.015f, 0.02f, 0.055f, 0.25f);
        _progress.Text = UiText.Get("FORMA RECONSTRUIDA");
        _title.Text = UiText.Get("El nombre que atravesará contigo");
        _narrative.Text = UiText.Get("ui.astral.identity.body");
        _consequence.Text = DescribeResult(_result!);
        _back.Disabled = false;
        _next.Text = UiText.Get("Conservar este nombre");

        _nameEdit = new LineEdit
        {
            PlaceholderText = UiText.Get("Nombre del fundador"),
            MaxLength = 32,
            Text = _founderName,
            CustomMinimumSize = new Vector2(0, 44),
            ThemeTypeVariation = "LineEdit",
        };
        _nameEdit.TextChanged += value =>
        {
            _founderName = value;
            ValidateIdentity();
        };
        _choices.AddChild(_nameEdit);

        var genderRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        genderRow.AddThemeConstantOverride("separation", 12);
        _choices.AddChild(genderRow);
        foreach ((GenderId id, string text) in new[]
        {
            (GenderId.Feminine, UiText.Get("Feminine")),
            (GenderId.Masculine, UiText.Get("Masculine")),
        })
        {
            var button = StandardButtons.ChoiceButton(
                text,
                UiText.Get("Afecta la presentación del sprite, no el resultado narrativo."));
            button.Pressed += () =>
            {
                _gender = id;
                RenderIdentity();
            };
            button.ThemeTypeVariation = _gender == id ? "ButtonPrimary" : "ButtonText";
            genderRow.AddChild(button);
        }
        AddResultSprite();
        ValidateIdentity();
        FadeIn(_narrative, 0.25);
        _nameEdit.GrabFocus();
    }

    private void ReturnToLastQuestion()
    {
        _identityStage = false;
        _step = FounderNarrativeCatalog.Questions.Count - 1;
        RenderQuestion();
    }

    private void ValidateIdentity()
    {
        _next.Disabled = !IsFounderNameValid(_founderName) || !_gender.HasValue;
        _error.Text = _next.Disabled
            ? UiText.Get("El nombre debe tener entre 1 y 32 caracteres y no contener controles.")
            : string.Empty;
    }

    private void OnConfirmIdentity()
    {
        if (_result is null || !_gender.HasValue || !IsFounderNameValid(_founderName)) return;
        FounderNarrativeResult final = FounderNarrativeScorer.WithGender(_result, _gender.Value);
        HeroCreationResult creation = _controller.TryCompleteOnboarding(
            new HeroCreationRequest(_founderName.Trim(), final.Profile, _gender.Value));
        if (!creation.IsSuccess)
        {
            _error.Text = UiText.Format("ui.astral.creation_failed", creation.Outcome);
            return;
        }
        _result = final;
        RenderFalseQuestion();
    }

    private void RenderFalseQuestion()
    {
        ClearChoices();
        _back.Hide();
        _next.Hide();
        _progress.Text = string.Empty;
        _title.Text = UiText.Get("La forma está lista.");
        _narrative.Text = UiText.Get("ui.astral.false_question.body");
        _consequence.Text = string.Empty;
        foreach (string beginning in new[]
        {
            "Conservaré…", "Buscaré…", "Intentaré comprender…", "Comenzaré de nuevo…",
        })
        {
            var incomplete = StandardButtons.ChoiceButton(UiText.Get(beginning), string.Empty);
            incomplete.Disabled = true;
            _choices.AddChild(incomplete);
            FadeIn(incomplete, 0.35 + _choices.GetChildCount() * 0.18);
        }
        Tween interruption = CreateTween();
        interruption.TweenInterval(1.15);
        interruption.TweenCallback(Callable.From(InterruptFalseQuestion));
    }

    private void InterruptFalseQuestion()
    {
        _title.Text = UiText.Get("Ah.");
        _narrative.Text = UiText.Get("Ya llegamos.");
        ClearChoices();
        FadeIn(_title, 0.08);
        FadeIn(_narrative, 0.08);
        Tween cut = CreateTween();
        cut.TweenInterval(0.45);
        cut.TweenCallback(Callable.From(BeginArrival));
    }

    private void BeginArrival()
    {
        if (_result is null || !_gender.HasValue) return;
        _cityView.PrepareFounderArrival();
        var arrival = new FounderArrivalSequence
        {
            Name = nameof(FounderArrivalSequence),
        };
        GetParent().AddChild(arrival);
        arrival.Completed += OnArrivalCompleted;
        arrival.Begin(
            _controller.World.Hero!,
            _cityView.GetFoundingArrivalGlobalPosition());
        Hide();
    }

    private void OnArrivalCompleted()
    {
        QueueFree();
        _cityView.CompleteFounderArrival();
    }

    private void AddResultSprite()
    {
        if (_result is null || !_gender.HasValue) return;
        CharacterBodyVariant body = CharacterVisualRegistry.ResolveBodyVariant(_gender.Value);
        LineageSpritePlayer sprite =
            CharacterVisualRegistry.LoadScene(_result.Lineage, body)
                .Instantiate<LineageSpritePlayer>();
        var preview = new Control
        {
            CustomMinimumSize = new Vector2(0, 140),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        preview.AddChild(sprite);
        preview.Resized += () =>
            sprite.Position = new Vector2(preview.Size.X * 0.5f, 76);
        _choices.AddChild(preview);
        Callable.From(() =>
            sprite.Position = new Vector2(preview.Size.X * 0.5f, 76)).CallDeferred();
    }

    private void BuildFragments()
    {
        foreach (Node child in _fragments.GetChildren()) child.QueueFree();
        int stabilised = _session.Answers.Count;
        if (!_identityStage)
        {
            _progress.Text = string.Format(
                TrKey("ui.onboarding.progress"),
                stabilised,
                FounderNarrativeCatalog.Questions.Count);
        }
        for (int index = 0; index < 12; index++)
        {
            _fragments.AddChild(new ColorRect
            {
                Color = index < stabilised
                    ? new Color("e3c35b")
                    : new Color(0.35f, 0.37f, 0.48f, 0.45f),
                CustomMinimumSize = new Vector2(14, 6),
                MouseFilter = MouseFilterEnum.Ignore,
            });
        }
    }

    private void ClearChoices()
    {
        foreach (Node child in _choices.GetChildren()) child.QueueFree();
        _choiceButtons.Clear();
    }

    private static FounderNarrativeChoice FindChoice(
        FounderNarrativeQuestion question,
        string id)
    {
        foreach (FounderNarrativeChoice choice in question.Choices)
        {
            if (choice.Id == id) return choice;
        }
        throw new InvalidOperationException($"Unknown choice '{id}'.");
    }

    private static string DescribeResult(FounderNarrativeResult result) =>
        $"{ProfileCatalog.Get(result.Lineage).DisplayName}\n" +
        $"{UiText.Get(ProfileCatalog.Get(result.Lineage).Summary)}\n\n" +
        UiText.Format("ui.astral.result_aptitudes", JoinLocalized(result.Aptitudes, ProfileCatalog.DisplayName)) + "\n" +
        UiText.Format("ui.astral.result_traits", JoinLocalized(result.Traits, ProfileCatalog.DisplayName)) + "\n" +
        UiText.Format("ui.astral.result_affinities", JoinLocalized(result.ProfessionalAffinities, ProfileCatalog.DisplayName)) + "\n" +
        UiText.Format("ui.astral.result_element", UiText.Get(ProfileCatalog.DisplayName(result.Element))) + "\n" +
        UiText.Format("ui.astral.result_combat", UiText.Get(ProfileCatalog.DisplayName(result.CombatStyle)));

    private static string JoinLocalized<T>(IReadOnlyList<T> values, Func<T, string> name)
    {
        var names = new string[values.Count];
        for (int index = 0; index < values.Count; index++) names[index] = UiText.Get(name(values[index]));
        return string.Join(", ", names);
    }

    private static Label NewLabel(string variation, HorizontalAlignment alignment) =>
        new()
        {
            ThemeTypeVariation = variation,
            HorizontalAlignment = alignment,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };

    private void FadeIn(CanvasItem item, double duration)
    {
        item.Modulate = new Color(1f, 1f, 1f, 0f);
        CreateTween().TweenProperty(item, "modulate:a", 1f, duration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }

    public static bool IsFounderNameValid(string? value)
    {
        string name = value?.Trim() ?? string.Empty;
        if (name.Length is < 1 or > 32) return false;
        foreach (char character in name)
        {
            if (char.IsControl(character)) return false;
        }
        return true;
    }

    public void ShowForVisualRegression(int step)
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        int questionStep = Math.Clamp(
            step,
            0,
            FounderNarrativeCatalog.Questions.Count);
        for (int index = 0; index < questionStep; index++)
        {
            FounderNarrativeQuestion question = FounderNarrativeCatalog.Questions[index];
            _session.Answer(question.Id, question.Choices[0].Id);
        }
        Show();
        if (questionStep < FounderNarrativeCatalog.Questions.Count)
        {
            _step = questionStep;
            RenderQuestion();
            return;
        }
        _result = FounderNarrativeScorer.Calculate(_session);
        _gender = GenderId.Feminine;
        _founderName = "Aster";
        RenderIdentity();
    }
}
