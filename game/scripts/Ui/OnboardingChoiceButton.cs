#nullable enable
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// A selectable narrative option. Used by the founder onboarding for the
/// four choices of every question, for the two body-presentation options
/// inside <see cref="GenderToggle"/>, and for the disabled placeholders of
/// the closing false question, so all three read with the same rhythm.
///
/// <para>
/// Two properties make this a typed widget rather than a configured
/// <see cref="Button"/>. First, the selected state travels on three
/// independent channels — the <c>ButtonPrimary</c> palette, the pressed
/// state of a shared <see cref="ButtonGroup"/>, and a check glyph — because
/// <c>docs/ai/CROSS_DOMAIN_INVARIANTS.md</c> forbids communicating a state
/// by colour alone. Second, the glyph is <em>always</em> assigned and only
/// its alpha moves, so selecting an option never changes the control's
/// minimum width and the label never jogs sideways.
/// </para>
/// </summary>
[GlobalClass]
public partial class OnboardingChoiceButton : Button
{
    /// <summary>
    /// Comfortable height for a single line of <c>ButtonText</c> (Jersey 10
    /// at 20 px inside a 9-slice with 4 px vertical content margins, so
    /// roughly 34 px intrinsic). The onboarding used to force 66 px, which
    /// spent 44 % of its vertical budget on padding.
    /// </summary>
    public const int DefaultHeight = 40;

    private static readonly Color OpaqueGlyph = new(1f, 1f, 1f, 1f);
    private static readonly Color InvisibleGlyph = new(1f, 1f, 1f, 0f);

    private static readonly string[] IconColorSlots =
    {
        "icon_normal_color",
        "icon_hover_color",
        "icon_pressed_color",
        "icon_focus_color",
        "icon_disabled_color",
    };

    private bool _selected;

    public OnboardingChoiceButton()
    {
        Text = string.Empty;
        Icon = ResourceLoader.Load<Texture2D>(IconPaths.Check);
        ExpandIcon = false;
        IconAlignment = HorizontalAlignment.Left;
        Alignment = HorizontalAlignment.Left;
        AutowrapMode = TextServer.AutowrapMode.WordSmart;
        ToggleMode = true;
        ThemeTypeVariation = "ButtonText";
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        CustomMinimumSize = new Vector2(0, DefaultHeight);
        FocusMode = FocusModeEnum.All;
        ApplySelectedVisuals();
    }

    /// <summary>
    /// Whether this option is the answer currently held by the session.
    /// Assigning it never emits <c>toggled</c>: the caller is the source of
    /// truth and a signal here would re-enter the handler that set it.
    /// </summary>
    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            SetPressedNoSignal(value);
            ApplySelectedVisuals();
        }
    }

    private void ApplySelectedVisuals()
    {
        ThemeTypeVariation = _selected ? "ButtonPrimary" : "ButtonText";
        Color tint = _selected ? OpaqueGlyph : InvisibleGlyph;
        foreach (string slot in IconColorSlots) AddThemeColorOverride(slot, tint);
    }
}
