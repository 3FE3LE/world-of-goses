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
    /// <summary>
    /// The stages the view walks through. Replaces the former
    /// <c>bool _identityStage</c>, which could not express the founder card
    /// once naming and the result card became separate beats.
    /// </summary>
    private enum Stage
    {
        Question,
        Identity,
        FounderCard,
        FalseQuestion,
    }

    /// <summary>Integer magnification of the founder portrait on the naming beat.</summary>
    private const int SpritePortraitScale = 2;

    [Export] public NodePath ControllerPath { get; set; } = "../CityWorldController";
    [Export] public NodePath CityViewPath { get; set; } =
        "../GameUiShell/ScreenContent/MacroStreetLiveView";

    private readonly FounderNarrativeSession _session = new();
    private readonly Dictionary<string, OnboardingChoiceButton> _choiceButtons = new();
    private CityWorldController _controller = null!;
    private Prototypes.MacroStreetLiveView _cityView = null!;
    private ColorRect _astralVeil = null!;
    private HBoxContainer _fragments = null!;
    private Label _progress = null!;
    private Label _title = null!;
    private Label _narrative = null!;
    private VBoxContainer _stageSlot = null!;
    private Label _consequence = null!;
    private Label _error = null!;
    private HBoxContainer _footer = null!;
    private Button _back = null!;
    private Button _next = null!;
    private int _step;
    private Stage _currentStage = Stage.Question;
    private FounderOnboardingResult? _result;
    private string _founderName = string.Empty;
    private GenderId? _gender;
    private LineEdit? _nameEdit;
    private Control? _spriteFrame;
    private LocaleManager? _localeManager;

    public override void _Ready()
    {
        _controller = GetNode<CityWorldController>(ControllerPath);
        _cityView = GetNode<Prototypes.MacroStreetLiveView>(CityViewPath);
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
        shell.AddThemeConstantOverride("separation", Tokens.SpacingComfortable);
        safe.AddChild(shell);

        _progress = NewLabel("SectionTitle", HorizontalAlignment.Center);
        shell.AddChild(_progress);
        _fragments = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _fragments.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        shell.AddChild(_fragments);

        // The two spacers are the structural reason this view no longer
        // overflows. Every other child sizes to its content, so without a
        // vertically expanding sibling a VBoxContainer neither clips nor
        // scrolls — it simply lays the surplus out past the bottom edge and
        // walks the footer off-screen. These absorb the slack, optically
        // centre the block between the fragment strip and the footer, and
        // collapse to zero the moment the content genuinely needs the room.
        // Equal ratios: the header already sits above them, so an even split
        // is what puts the reading block on the optical centre.
        shell.AddChild(NewSpacer(1f));

        _title = NewLabel("ScreenTitle", HorizontalAlignment.Center);
        shell.AddChild(_title);
        _narrative = NewLabel("BodyText", HorizontalAlignment.Center);
        shell.AddChild(_narrative);

        // Slot for the stage-specific content: the four narrative choices,
        // the naming controls, or the founder card.
        _stageSlot = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _stageSlot.AddThemeConstantOverride("separation", 6);
        shell.AddChild(_stageSlot);

        _consequence = NewLabel("BodyText", HorizontalAlignment.Center);
        // One line of headroom, held open for the whole question stage even
        // while empty. The longest immediate consequence in either catalog is
        // 77 characters, which renders on one line at this measure, so the
        // row never changes height once reserved and selecting a choice
        // cannot reflow the block above it.
        _consequence.CustomMinimumSize = new Vector2(0, 26);
        shell.AddChild(_consequence);

        shell.AddChild(NewSpacer(1f));

        _error = NewLabel("ErrorText", HorizontalAlignment.Center);
        // Explicit floor so the reserved row has a deterministic height rather
        // than whatever an empty Label happens to report for the font.
        _error.CustomMinimumSize = new Vector2(0, 21);
        shell.AddChild(_error);

        _footer = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.End,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _footer.AddThemeConstantOverride("separation", Tokens.SpacingRelaxed);
        shell.AddChild(_footer);
        _back = StandardButtons.NavigationButton(TrKey("ui.onboarding.back"));
        _next = StandardButtons.NavigationButton(TrKey("ui.onboarding.stabilise"));
        _next.ThemeTypeVariation = "ButtonPrimary";
        _back.Pressed += OnBack;
        _next.Pressed += OnNext;
        _footer.AddChild(_back);
        _footer.AddChild(_next);
    }

    private void RenderQuestion()
    {
        _currentStage = Stage.Question;
        FounderNarrativeQuestion question = FounderNarrativeCatalog.Questions[_step];
        ClearStage();
        BuildFragments();
        SetText(_title, TrKey(question.Title));
        SetText(_narrative, TrKey(question.Text));
        // Reserved, not hidden: answering must not move the question.
        SetReservedText(_consequence, string.Empty);
        SetText(_error, string.Empty);
        _footer.Show();
        _astralVeil.Color = new Color(0.015f, 0.02f, 0.055f, 1f - question.TerrainReveal);
        _back.Disabled = _step == 0;
        _next.Text = _step == FounderNarrativeCatalog.Questions.Count - 1
            ? TrKey("ui.onboarding.shape_memory")
            : TrKey("ui.onboarding.stabilise");

        _choiceButtons.Clear();
        // One group for the four options, so the set announces as a radio
        // choice and keyboard/gamepad traversal treats it as one control.
        var group = new ButtonGroup();
        foreach (FounderNarrativeChoice choice in question.Choices)
        {
            var button = new OnboardingChoiceButton
            {
                Text = TrKey(choice.Text),
                ButtonGroup = group,
            };
            string choiceId = choice.Id;
            button.Pressed += () => SelectChoice(question.Id, choiceId);
            _stageSlot.AddChild(button);
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
        foreach ((string id, OnboardingChoiceButton button) in _choiceButtons)
        {
            button.Selected = id == choiceId;
        }
        FounderNarrativeChoice choice = FindChoice(question, choiceId);
        SetReservedText(_consequence, TrKey(choice.ImmediateConsequence));
        FadeIn(_consequence, 0.18);
        _next.Disabled = false;
    }

    private void OnBack()
    {
        switch (_currentStage)
        {
            case Stage.Question:
                if (_step <= 0) return;
                _step--;
                RenderQuestion();
                return;
            case Stage.Identity:
                ReturnToLastQuestion();
                return;
            case Stage.FounderCard:
                RenderIdentity();
                return;
            default:
                return;
        }
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
        // A locale switch re-bakes text into every node, so the current stage
        // has to be rebuilt. Carry the caret across so a player mid-way
        // through typing a name does not lose their place.
        int caret = _nameEdit?.CaretColumn ?? 0;
        bool hadFocus = _nameEdit?.HasFocus() ?? false;
        switch (_currentStage)
        {
            case Stage.Identity:
                RenderIdentity();
                break;
            case Stage.FounderCard:
                RenderFounderCard();
                break;
            case Stage.FalseQuestion:
                break;
            default:
                RenderQuestion();
                break;
        }
        if (!hadFocus || _nameEdit is null) return;
        _nameEdit.GrabFocus();
        _nameEdit.CaretColumn = Mathf.Min(caret, _nameEdit.Text.Length);
    }

    public override void _ExitTree()
    {
        if (_localeManager is not null) _localeManager.LocaleChanged -= OnLocaleChanged;
    }

    private void OnNext()
    {
        switch (_currentStage)
        {
            case Stage.Identity:
                if (!IsIdentityComplete()) return;
                RenderFounderCard();
                return;
            case Stage.FounderCard:
                OnConfirmIdentity();
                return;
            case Stage.FalseQuestion:
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

    /// <summary>
    /// Naming beat. The result of the twelve questions is already computed,
    /// but the card that presents it is its own step: this screen asks for one
    /// thing, so it shows the sprite the player is about to inhabit, the name
    /// field, and the body presentation — nothing else.
    /// </summary>
    private void RenderIdentity()
    {
        _currentStage = Stage.Identity;
        ClearStage();
        // First point in the flow where revealing the lineage is legitimate,
        // so the accent that themes the sprite frame and the founder card can
        // be pinned here.
        if (_result is not null)
        {
            LineageThemeRegistry.SetActiveLineage(
                LineageThemeRegistry.IdOf(_result.Lineage));
        }
        _astralVeil.Color = new Color(0.015f, 0.02f, 0.055f, 0.25f);
        BuildFragments();
        SetText(_progress, TrKey("ui.astral.identity.progress"));
        SetText(_title, TrKey("ui.astral.identity.title"));
        SetText(_narrative, TrKey("ui.astral.identity.body"));
        SetText(_consequence, string.Empty);
        _footer.Show();
        _back.Disabled = false;
        _next.Text = TrKey("ui.astral.identity.continue");

        var column = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(360, 0),
        };
        column.AddThemeConstantOverride("separation", 6);
        _stageSlot.AddChild(column);

        _spriteFrame = new Control
        {
            CustomMinimumSize = new Vector2(
                PresentationConstants.DetailedCitizenHeight,
                PresentationConstants.DetailedCitizenHeight),
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
        };
        column.AddChild(_spriteFrame);
        // Subscribed once per stage build. The old code re-entered this whole
        // method on every gender press and stacked a new handler each time.
        Control frame = _spriteFrame;
        frame.Resized += () => PositionSprite(frame);

        _nameEdit = new LineEdit
        {
            PlaceholderText = TrKey("ui.astral.identity.name_placeholder"),
            MaxLength = 32,
            Text = _founderName,
            CustomMinimumSize = new Vector2(360, 40),
            ThemeTypeVariation = "LineEdit",
        };
        _nameEdit.TextChanged += value =>
        {
            _founderName = value;
            ValidateIdentity();
        };
        column.AddChild(_nameEdit);

        var toggle = new GenderToggle();
        column.AddChild(toggle);
        toggle.Configure(
            id => TrKey(id == GenderId.Feminine
                ? "ui.astral.identity.body_feminine"
                : "ui.astral.identity.body_masculine"),
            TrKey("ui.astral.identity.body_hint"));
        toggle.Selected = _gender;
        toggle.GenderChanged += OnGenderChanged;

        RefreshResultSprite();
        ValidateIdentity();
        FadeIn(_narrative, 0.25);
        _nameEdit.GrabFocus();
    }

    /// <summary>
    /// Swaps the previewed body without rebuilding the stage. Rebuilding is
    /// what used to destroy the caret and steal focus back to the name field
    /// on every press.
    /// </summary>
    private void OnGenderChanged(int gender)
    {
        _gender = (GenderId)gender;
        RefreshResultSprite();
        ValidateIdentity();
    }

    /// <summary>
    /// The closing card: the reconstructed form as the bible specifies it,
    /// on its own screen so it is read rather than skimmed past the name
    /// field. Confirming here is what actually creates the founder.
    /// </summary>
    private void RenderFounderCard()
    {
        if (_result is null) return;
        _currentStage = Stage.FounderCard;
        ClearStage();
        BuildFragments();
        SetText(_progress, TrKey("ui.astral.identity.progress"));
        SetText(_title, string.Empty);
        SetText(_narrative, string.Empty);
        SetText(_consequence, string.Empty);
        SetText(_error, string.Empty);
        _footer.Show();
        _back.Disabled = false;
        _next.Disabled = false;
        _next.Text = TrKey("ui.astral.identity.confirm");

        var card = new FounderCardPanel();
        _stageSlot.AddChild(card);
        card.Render(_founderName.Trim(), _result, TrKey);
        FadeIn(card, 0.28);
        _next.GrabFocus();
    }

    private void ReturnToLastQuestion()
    {
        _step = FounderNarrativeCatalog.Questions.Count - 1;
        RenderQuestion();
    }

    private bool IsIdentityComplete() =>
        IsFounderNameValid(_founderName) && _gender.HasValue;

    private void ValidateIdentity()
    {
        _next.Disabled = !IsIdentityComplete();
        // Reserved: this message clears the instant the name becomes valid,
        // and the player is typing directly above it.
        SetReservedText(
            _error,
            _next.Disabled ? TrKey("ui.astral.identity.name_invalid") : string.Empty);
    }

    private void OnConfirmIdentity()
    {
        if (_result is null || !_gender.HasValue || !IsFounderNameValid(_founderName)) return;
        FounderOnboardingResult final = _result;
        CitizenProfile profile = CitizenProfile.CreateFounder(final, _gender.Value);
        // Treat a repeated UI activation as idempotent. The first activation
        // may already have created and saved the founder before a queued
        // second button/input event arrives; never present that successful
        // creation as an "AlreadyExists" failure or attempt to overwrite it.
        if (_controller.HasHero())
        {
            _result = final;
            RenderFalseQuestion();
            return;
        }
        _next.Disabled = true;
        HeroCreationResult creation = _controller.TryCompleteOnboarding(
            new HeroCreationRequest(_founderName.Trim(), profile, _gender.Value, final));
        if (!creation.IsSuccess)
        {
            SetText(_error, UiText.Format("ui.astral.creation_failed", creation.Outcome));
            _next.Disabled = false;
            return;
        }
        _result = final;
        RenderFalseQuestion();
    }

    private void RenderFalseQuestion()
    {
        _currentStage = Stage.FalseQuestion;
        ClearStage();
        // Hiding the row rather than the two buttons collapses its separation
        // as well, so the beat does not leave a gap where the actions were.
        _footer.Hide();
        SetText(_progress, string.Empty);
        SetText(_title, TrKey("La forma está lista."));
        SetText(_narrative, TrKey("ui.astral.false_question.body"));
        SetText(_consequence, string.Empty);
        SetText(_error, string.Empty);
        foreach (string beginning in new[]
        {
            "Conservaré…", "Buscaré…", "Intentaré comprender…", "Comenzaré de nuevo…",
        })
        {
            var incomplete = new OnboardingChoiceButton
            {
                Text = TrKey(beginning),
                Disabled = true,
            };
            _stageSlot.AddChild(incomplete);
            FadeIn(incomplete, 0.35 + _stageSlot.GetChildCount() * 0.18);
        }
        Tween interruption = CreateTween();
        interruption.TweenInterval(1.15);
        interruption.TweenCallback(Callable.From(InterruptFalseQuestion));
    }

    private void InterruptFalseQuestion()
    {
        SetText(_title, TrKey("Ah."));
        SetText(_narrative, TrKey("Ya llegamos."));
        ClearStage();
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
            _controller.TryGetHeroForFounderArrival()!,
            _cityView.GetFoundingArrivalGlobalPosition());
        Hide();
    }

    private void OnArrivalCompleted()
    {
        QueueFree();
        _cityView.CompleteFounderArrival();
    }

    /// <summary>
    /// Mounts the sprite for the current lineage and body presentation into
    /// the existing frame, replacing whatever was there. The frame itself
    /// survives, so its <c>Resized</c> subscription is not duplicated.
    /// </summary>
    private void RefreshResultSprite()
    {
        if (_spriteFrame is null || _result is null || !_gender.HasValue) return;
        foreach (Node child in _spriteFrame.GetChildren()) child.QueueFree();

        CharacterBodyVariant body = CharacterVisualRegistry.ResolveBodyVariant(_gender.Value);
        LineageSpritePlayer sprite =
            CharacterVisualRegistry.LoadScene(_result.Lineage, body)
                .Instantiate<LineageSpritePlayer>();
        // The founder is the subject of this screen, not an inhabitant seen
        // in passing, so the portrait runs at twice the detail scale used
        // elsewhere. Integer only — nearest-neighbour pixel art must never be
        // resampled at a fractional factor.
        sprite.Scale = new Vector2(SpritePortraitScale, SpritePortraitScale);
        _spriteFrame.AddChild(sprite);
        Control frame = _spriteFrame;
        Callable.From(() => PositionSprite(frame)).CallDeferred();
    }

    /// <summary>
    /// Stands the sprite on the floor of its frame, horizontally centred.
    /// <c>LineageSpritePlayer</c> is an <c>AnimatedSprite2D</c>, so it takes
    /// no part in the container layout and has to be placed by hand.
    /// </summary>
    private static void PositionSprite(Control frame)
    {
        if (!GodotObject.IsInstanceValid(frame)) return;
        foreach (Node child in frame.GetChildren())
        {
            if (child is not Node2D sprite) continue;
            sprite.Position = new Vector2(
                Mathf.Round(frame.Size.X * 0.5f),
                Mathf.Round(frame.Size.Y * 0.85f));
        }
    }

    private void BuildFragments()
    {
        foreach (Node child in _fragments.GetChildren()) child.QueueFree();
        int stabilised = _session.Answers.Count;
        if (_currentStage == Stage.Question)
        {
            SetText(
                _progress,
                string.Format(
                    TrKey("ui.onboarding.progress"),
                    stabilised,
                    FounderNarrativeCatalog.Questions.Count));
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

    private void ClearStage()
    {
        foreach (Node child in _stageSlot.GetChildren()) child.QueueFree();
        _choiceButtons.Clear();
        _nameEdit = null;
        _spriteFrame = null;
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

    /// <summary>
    /// Assigns a label's text and hides it when there is none. An empty
    /// <see cref="Label"/> still claims its minimum height <em>and</em> its
    /// separation inside a <see cref="BoxContainer"/>; an invisible child
    /// claims neither. Every row of this screen is optional on some stage,
    /// so blanking instead of hiding is what pushed the footer off-screen.
    /// </summary>
    private static void SetText(Label label, string text)
    {
        label.Text = text;
        label.Visible = !string.IsNullOrEmpty(text);
    }

    /// <summary>
    /// Assigns a label's text while holding its row open. Used for the two
    /// rows that appear and disappear <em>in response to the player acting on
    /// the same screen</em> — the immediate consequence of a choice, and the
    /// name validation message. Hiding those would be correct for space but
    /// wrong for feel: the row's arrival would resize the column and shift
    /// everything the player is currently reading or aiming at. The stage that
    /// owns the row pays for it up front; other stages still hide it outright
    /// through <see cref="SetText"/>.
    /// </summary>
    private static void SetReservedText(Label label, string text)
    {
        label.Text = text;
        label.Visible = true;
    }

    private static Control NewSpacer(float ratio) =>
        new()
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = ratio,
            MouseFilter = MouseFilterEnum.Ignore,
        };

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

    /// <summary>
    /// A12 closes the seam: <c>internal</c> and gated on the
    /// visual-regression harness. Production scenes never call this.
    /// </summary>
    internal void ShowForVisualRegression(int step)
    {
        if (!WorldofGoses.Testing.VisualRegressionHarness.IsActive) return;
        int questionStep = Math.Clamp(
            step,
            0,
            FounderNarrativeCatalog.Questions.Count + 1);
        for (int index = 0;
             index < Math.Min(questionStep, FounderNarrativeCatalog.Questions.Count);
             index++)
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
        // One past the last question is the naming beat; two past it is the
        // founder card, which cannot be reached from a save and therefore
        // needs its own entry point.
        if (questionStep > FounderNarrativeCatalog.Questions.Count) RenderFounderCard();
    }
}
