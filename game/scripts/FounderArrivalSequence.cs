#nullable enable
using System;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Presentation;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Presentation-only bridge from the astral reconstruction to the existing
/// macro board. Placeholder flash/ring/card nodes are intentionally isolated
/// so final art can replace them without changing founder creation.
/// </summary>
public partial class FounderArrivalSequence : Control
{
    [Signal] public delegate void CompletedEventHandler();

    [Export(PropertyHint.Range, "0.1,3.0,0.05")]
    public double LandedHoldSeconds { get; set; } = 0.9;

    private ColorRect _flash = null!;
    private Line2D _impactRing = null!;
    private LineageSpritePlayer _sprite = null!;
    private PanelContainer _card = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        OverlayLayers.Apply(this, OverlayLayers.FounderArrival);
        MouseFilter = MouseFilterEnum.Stop;
        _flash = new ColorRect
        {
            Color = new Color(0.95f, 0.86f, 0.56f, 0f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _flash.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_flash);
    }

    public void Begin(Citizen founder, Vector2 impactPosition)
    {
        CharacterBodyVariant body =
            CharacterVisualRegistry.ResolveBodyVariant(founder.Profile.Gender);
        _sprite = CharacterVisualRegistry.LoadScene(
                founder.Profile.Lineage,
                founder.AppearanceVariant,
                body)
            .Instantiate<LineageSpritePlayer>();
        _sprite.Position = new Vector2(
            Mathf.Round(impactPosition.X),
            -PresentationConstants.DetailedCitizenHeight);
        AddChild(_sprite);
        _sprite.PlayFall(Vector2.Down);

        BuildImpactRing(impactPosition);
        Tween fall = CreateTween();
        fall.TweenProperty(
                _sprite,
                "position",
                new Vector2(
                    Mathf.Round(impactPosition.X),
                    Mathf.Round(impactPosition.Y)),
                0.82)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        fall.TweenCallback(Callable.From(() => Impact(founder, impactPosition)));
    }

    private void BuildImpactRing(Vector2 position)
    {
        _impactRing = new Line2D
        {
            Width = 3f,
            DefaultColor = new Color(0.9f, 0.75f, 0.35f, 0f),
            Closed = true,
        };
        const int points = 16;
        for (int index = 0; index < points; index++)
        {
            float angle = Mathf.Tau * index / points;
            _impactRing.AddPoint(position + Vector2.FromAngle(angle) * 12f);
        }
        AddChild(_impactRing);
    }

    private void Impact(Citizen founder, Vector2 impactPosition)
    {
        _sprite.HoldLanded(Vector2.Down);
        _flash.Color = new Color(0.95f, 0.86f, 0.56f, 0.72f);
        _impactRing.DefaultColor = new Color(0.9f, 0.75f, 0.35f, 0.9f);
        Tween impact = CreateTween().SetParallel(true);
        impact.TweenProperty(_flash, "color:a", 0f, 0.22);
        impact.TweenProperty(_impactRing, "scale", new Vector2(4f, 4f), 0.32)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        impact.TweenProperty(_impactRing, "modulate:a", 0f, 0.32);

        Tween pause = CreateTween();
        pause.TweenInterval(0.42);
        pause.TweenCallback(Callable.From(() => ShowFounderCard(founder, impactPosition)));

        Tween recovery = CreateTween();
        recovery.TweenInterval(LandedHoldSeconds);
        recovery.TweenCallback(Callable.From(() => _sprite.ResumeIdle()));
    }

    private void ShowFounderCard(Citizen founder, Vector2 impactPosition)
    {
        _card = new PanelContainer
        {
            CustomMinimumSize = new Vector2(640, 220),
            Position = new Vector2(
                Mathf.Max(24f, (Size.X - 640f) * 0.5f),
                Mathf.Max(24f, impactPosition.Y - 250f)),
            Modulate = new Color(1f, 1f, 1f, 0f),
            Scale = new Vector2(0.94f, 0.94f),
            PivotOffset = new Vector2(320, 110),
        };
        _card.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.08f, 0.12f, 0.98f),
            BorderColor = new Color(0.88f, 0.7f, 0.25f),
            BorderWidthLeft = 4,
            BorderWidthTop = 4,
            BorderWidthRight = 4,
            BorderWidthBottom = 4,
        });
        AddChild(_card);
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_top", 22);
        margin.AddThemeConstantOverride("margin_right", 28);
        margin.AddThemeConstantOverride("margin_bottom", 22);
        _card.AddChild(margin);
        var content = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        content.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        margin.AddChild(content);
        content.AddChild(LabelFor(founder.Name.ToUpperInvariant(), "ScreenTitle"));
        content.AddChild(LabelFor(
            $"{ProfileCatalog.Get(founder.Profile.Lineage).DisplayName} · Recién mortal",
            "SectionTitle"));
        content.AddChild(LabelFor(
            "Primera caída. Técnicamente exitosa.",
            "BodyText"));
        // DEC-0013 requires this card to state what onboarding actually produced:
        // affinity, the physical expression derived from it, and the cube axes.
        // It previously showed only the name and lineage.
        content.AddChild(LabelFor(
            CitizenNatureText.FormatCompactLocalized(
                founder.CubeProfile,
                founder.Profile.Lineage,
                founder.CombatNature),
            "BodySmall"));

        Tween reveal = CreateTween().SetParallel(true);
        reveal.TweenProperty(_card, "modulate:a", 1f, 0.18);
        reveal.TweenProperty(_card, "scale", Vector2.One, 0.24)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        Tween hold = CreateTween();
        hold.TweenInterval(2.0);
        hold.TweenProperty(_card, "modulate:a", 0f, 0.22);
        hold.TweenCallback(Callable.From(Finish));
    }

    private static Label LabelFor(string text, string variation) =>
        new()
        {
            Text = text,
            ThemeTypeVariation = variation,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };

    private void Finish()
    {
        EmitSignal(SignalName.Completed);
        QueueFree();
    }
}
