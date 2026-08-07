#nullable enable
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// The fire spirit's voice, spoken from where the spirit is standing.
///
/// <para>
/// Replaces the bottom band (<c>FirstNightDialogueStrip</c>, removed) as the
/// night's surface. The band was decided in <c>DEC-0014</c> §3 and
/// superseded by <c>DEC-0016</c>: it was never seen running until the
/// NodePath that fed the whole sequence was fixed, and once visible it read
/// as a detached caption bar — the words had no speaker, and the player's
/// only verb was "Continuar" at the far edge of the screen. A balloon over
/// the spirit puts the voice on the character and keeps the player's eyes in
/// the world they are being taught about.
/// </para>
///
/// <para>
/// The whole balloon is the confirm affordance: clicking anywhere on it
/// advances. That removes the button the band needed and, with it, the
/// "continuar y continuar" rhythm of two competing rectangles.
/// </para>
/// </summary>
[GlobalClass]
public partial class FirstNightSpeechBubble : Control
{
    /// <summary>Width of the reading column, in logical pixels.</summary>
    private const int BubbleWidth = 380;

    /// <summary>Gap between the tail's point and the speaker's anchor.</summary>
    private const int SpeakerGap = 18;

    private const int TailWidth = 14;
    private const int TailHeight = 10;

    /// <summary>Narration reads a shade back from the surface, like a caption.</summary>
    private static readonly Color NarrationTint = new(0.86f, 0.84f, 0.80f, 1f);

    /// <summary>Emitted when the player confirms the current line.</summary>
    [Signal]
    public delegate void ConfirmedEventHandler();

    private PanelContainer _panel = null!;
    private Label _body = null!;
    private Polygon2D _tail = null!;
    private Label _hint = null!;
    private bool _hasSpeaker = true;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = OverlayLayers.Tutorial;
        // The balloon sizes to its content and is placed by hand, so it must
        // not be laid out by a parent container.
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        CustomMinimumSize = new Vector2(BubbleWidth, 0);

        // Pass, not the PanelContainer default of Stop: the whole balloon is
        // the confirm affordance, and a Stop-filtered child consumes the click
        // before this Control's _GuiInput ever runs — silently, and invisibly
        // to code review.
        _panel = new PanelContainer
        {
            ThemeTypeVariation = "OverlayPanel",
            MouseFilter = MouseFilterEnum.Pass,
        };
        _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        _panel.AddChild(column);

        _body = new Label
        {
            ThemeTypeVariation = "DialogText",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Left,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        column.AddChild(_body);

        _hint = new Label
        {
            ThemeTypeVariation = "BodySmall",
            HorizontalAlignment = HorizontalAlignment.Right,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        column.AddChild(_hint);

        // The tail is authored geometry rather than an asset: none of the
        // three Kenney packs ships a tailed balloon frame, and the only
        // balloon art in them is a 16x16 cursor glyph, which cannot be
        // 9-sliced to an arbitrary reading width.
        _tail = new Polygon2D
        {
            Polygon = new[]
            {
                new Vector2(-TailWidth * 0.5f, 0f),
                new Vector2(TailWidth * 0.5f, 0f),
                new Vector2(0f, TailHeight),
            },
            Color = new Color(0.035f, 0.043f, 0.064f, 0.985f),
        };
        AddChild(_tail);

        Visible = false;
    }

    /// <summary>
    /// Shows an already-localised line. <paramref name="confirmHint"/> is the
    /// quiet affordance label ("Continue", "Give in to sleep") that tells the
    /// player the balloon itself is clickable.
    /// </summary>
    public void Speak(string body, string confirmHint, bool hasSpeaker = true)
    {
        _body.Text = body;
        _hint.Text = confirmHint;
        // No tail when nobody is speaking. The night opens on narration —
        // the founder's landing mark, the spirit noticing it — at a stage
        // where FirstNightRules.SpiritIsPresent is still false, so a tail
        // would point at empty ground.
        _hasSpeaker = hasSpeaker;
        _tail.Visible = hasSpeaker;

        // Narration is the world's voice, so it is centred and quieter;
        // speech is someone talking to you, so it is ranged left and carries
        // the dialogue tier. Without the distinction a tail-less balloon still
        // read as a character speaking, just one whose tail had gone missing.
        _body.ThemeTypeVariation = hasSpeaker ? "DialogText" : "BodyText";
        _body.HorizontalAlignment = hasSpeaker
            ? HorizontalAlignment.Left
            : HorizontalAlignment.Center;
        _body.Modulate = hasSpeaker ? Colors.White : NarrationTint;

        Visible = true;
        UiMotion.FadeIn(this);
    }

    public void Vanish() => Visible = false;

    /// <summary>
    /// Places the balloon above the speaker, clamped so it never leaves the
    /// viewport when the spirit stands near an edge. The tail keeps pointing
    /// at the speaker even when the body has been pushed sideways.
    /// </summary>
    public void FollowSpeaker(Vector2 speakerPosition)
    {
        Vector2 size = _panel.GetCombinedMinimumSize();
        size.X = Mathf.Max(size.X, BubbleWidth);
        Size = size;

        Rect2 viewport = GetViewportRect();
        float left = Mathf.Clamp(
            speakerPosition.X - size.X * 0.5f,
            8f,
            Mathf.Max(8f, viewport.Size.X - size.X - 8f));

        // Above the speaker by default, below when there is no room. Clamping
        // to the top edge instead would drop the balloon *onto* the speaker
        // and hide the very thing it points at — the spirit vanishing behind
        // its own dialogue.
        float above = speakerPosition.Y - SpeakerGap - TailHeight - size.Y;
        bool placeBelow = above < 8f;
        float top = placeBelow ? speakerPosition.Y + SpeakerGap + TailHeight : above;
        top = Mathf.Min(top, Mathf.Max(8f, viewport.Size.Y - size.Y - 8f));

        Position = new Vector2(Mathf.Round(left), Mathf.Round(top));

        if (!_hasSpeaker) return;
        float tailX = Mathf.Clamp(speakerPosition.X - left, TailWidth, Mathf.Max(TailWidth, size.X - TailWidth));
        _tail.Position = new Vector2(Mathf.Round(tailX), Mathf.Round(placeBelow ? 0f : size.Y));
        // Flip the tail so it always points back at the speaker.
        _tail.Scale = new Vector2(1f, placeBelow ? -1f : 1f);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
        AcceptEvent();
        EmitSignal(SignalName.Confirmed);
    }
}
