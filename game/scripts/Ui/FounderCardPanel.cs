#nullable enable
using System;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Ui;

/// <summary>
/// The closing card of the founder onboarding: the reconstructed form, shown
/// once the player has named it. Replaces the eleven-line string blob the
/// onboarding used to pour into a single centred label.
///
/// <para>
/// Its content is fixed by
/// <c>docs/world-of-goses-design-bible/07_ONBOARDING_AND_FOUNDER.md</c>
/// §"Pantalla final del onboarding": name, body presentation, sprite,
/// lineage, elemental affinity, the three Cube axes and a brief narrative
/// summary — and nothing from that section's "no mostrar" list, which is why
/// the natural weapon families the citizens panel prints are deliberately
/// absent here.
/// </para>
///
/// <para>
/// The panel is <c>ShrinkCenter</c> at a fixed measure. The onboarding's
/// question steps run full-bleed, but a data card read at 1216 px would
/// scatter its label/value pairs across the screen.
/// </para>
/// </summary>
[GlobalClass]
public partial class FounderCardPanel : PanelContainer
{
    private const int CardWidth = 680;

    /// <summary>Shared caption column, so every value starts on one edge.</summary>
    private const int LabelGutter = 132;

    /// <summary>
    /// Renders the card. <paramref name="translate"/> is supplied by the
    /// caller so this widget makes no assumption about whether the active
    /// catalog is reachable through the <c>LocaleManager</c> autoload or the
    /// bare <c>TranslationServer</c> — the onboarding degrades gracefully to
    /// the key literal during headless capture.
    /// </summary>
    public void Render(
        string founderName,
        FounderOnboardingResult result,
        Func<string, string> translate)
    {
        foreach (Node child in GetChildren()) child.QueueFree();

        ThemeTypeVariation = "OverlayPanel";
        CustomMinimumSize = new Vector2(CardWidth, 0);
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;

        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 8);
        AddChild(body);

        LineageDefinition lineage = ProfileCatalog.Get(result.Lineage);

        AddLabel(body, "ScreenTitle", founderName, HorizontalAlignment.Center);
        AddPair(
            body,
            translate("ui.astral.card.lineage"),
            translate(lineage.DisplayName),
            translate(CubeScoring.Signature(result.Lineage)));
        AddLabel(body, "BodyText", translate(lineage.Summary), HorizontalAlignment.Left, wrap: true);

        AddDivider(body);

        AddPair(
            body,
            translate("Afinidad"),
            translate(ProfileCatalog.DisplayName(result.ElementalAffinity)),
            null);
        AddLabel(
            body,
            "BodySmall",
            DescribePhysicalExpression(CubeExpression.Derive(result.CubeProfile), translate),
            HorizontalAlignment.Left,
            wrap: true);

        AddDivider(body);

        AddLabel(
            body,
            "SectionTitle",
            translate("Perfil de encarnación").ToUpperInvariant(),
            HorizontalAlignment.Left);

        var axes = new VBoxContainer();
        axes.AddThemeConstantOverride("separation", 6);
        body.AddChild(axes);
        FounderCubeProfile cube = result.CubeProfile;
        AddAxis(axes, translate("Cuerpo"), cube.Body, translate("Vínculo"), cube.Bond);
        AddAxis(axes, translate("Estabilidad"), cube.Stability, translate("Impulso"), cube.Impulse);
        AddAxis(axes, translate("Dominio"), cube.Domain, translate("Alcance"), cube.Reach);
    }

    /// <summary>
    /// The bodily expression the affinity implies. The onboarding used to
    /// append the two natural weapon families here as well; the bible's final
    /// screen lists "Arma preferida" under "no mostrar", so the card names the
    /// expression only. The citizens panel and the hero profile keep their own
    /// fuller wording — this omission is scoped to the founding card.
    /// </summary>
    private static string DescribePhysicalExpression(
        PhysicalExpression expression,
        Func<string, string> translate)
    {
        return string.Format(
            translate("ui.citizen.physical_expression"),
            translate(ProfileCatalog.DisplayName(expression)));
    }

    private static void AddAxis(Container parent, string left, int leftValue, string right, int rightValue)
    {
        var axis = new CubeAxisBar();
        parent.AddChild(axis);
        axis.Configure(left, leftValue, right, rightValue);
    }

    /// <summary>
    /// A label/value row, optionally trailed by a quieter qualifier such as
    /// the lineage signature. Label and value carry different type tiers so
    /// the value reads as the answer even before the eye parses the row.
    /// </summary>
    private static void AddPair(Container parent, string label, string value, string? qualifier)
    {
        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 10);
        parent.AddChild(row);

        // A fixed gutter for the label keeps the values of successive rows on
        // one vertical edge, which is what makes this read as a data card
        // rather than a run of sentences.
        Label caption = AddLabel(row, "BodySmall", label.ToUpperInvariant(), HorizontalAlignment.Left);
        caption.CustomMinimumSize = new Vector2(LabelGutter, 0);
        caption.VerticalAlignment = VerticalAlignment.Bottom;

        AddLabel(row, "PanelTitle", value.ToUpperInvariant(), HorizontalAlignment.Left);
        if (string.IsNullOrEmpty(qualifier)) return;

        Label signature = AddLabel(row, "BodySmall", $"· {qualifier}", HorizontalAlignment.Right);
        signature.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        signature.VerticalAlignment = VerticalAlignment.Bottom;
    }

    private static void AddDivider(Container parent)
    {
        var divider = new ColorRect
        {
            CustomMinimumSize = new Vector2(0, 2),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Color = LineageThemeRegistry.IconAccent with { A = 0.35f },
            MouseFilter = MouseFilterEnum.Ignore,
        };
        parent.AddChild(divider);
    }

    /// <summary>
    /// Adds a label. <paramref name="wrap"/> defaults to off because these
    /// rows sit inside <see cref="HBoxContainer"/>s: a wrapping label reports
    /// a one-character minimum width, so an expanding sibling takes the whole
    /// row and the caption renders as a vertical column of letters. Only the
    /// standalone prose paragraphs opt in.
    /// </summary>
    private static Label AddLabel(
        Container parent,
        string variation,
        string text,
        HorizontalAlignment alignment,
        bool wrap = false)
    {
        var label = new Label
        {
            Text = text,
            ThemeTypeVariation = variation,
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = wrap
                ? TextServer.AutowrapMode.WordSmart
                : TextServer.AutowrapMode.Off,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        parent.AddChild(label);
        return label;
    }
}
