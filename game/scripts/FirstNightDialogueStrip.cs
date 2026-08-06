#nullable enable
using Godot;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Non-modal dialogue strip for the authored first night
/// (<c>docs/19_FIRST_NIGHT_AND_FIRE_SPIRIT.md</c>). The strip lives on
/// <see cref="OverlayLayers.Tutorial"/> = 50, anchored to the bottom
/// of the viewport with a fixed height. The outer Control captures
/// clicks so the button works, but the empty area above the strip is
/// free of any Control and lets clicks fall through to the world —
/// the player can keep playing while reading.
///
/// <para>
/// The body text is shown as a translation key resolved by the
/// presentation layer, not the strip itself: this node never touches
/// <c>UiText.Get</c>. The controller passes an already-resolved
/// string in, so the strip stays Godot-free of localisation
/// decisions and can be reasoned about without spinning up a
/// translation context.
/// </para>
///
/// <para>
/// The route is strictly linear; the strip exposes a single
/// <see cref="FollowPressed"/> signal. The controller decides whether
/// to call <c>CityWorld.TryCloseFirstNightDialogue</c> or another
/// advance method based on the current stage.
/// </para>
/// </summary>
public partial class FirstNightDialogueStrip : Control
{
    /// <summary>Strip height in logical pixels. Wide enough for one line of wrapped body text plus a button.</summary>
    private const float StripHeight = 96f;

    /// <summary>Side margin (logical pixels) to keep the strip away from the screen edges.</summary>
    private const float SideMargin = 48f;

    /// <summary>
    /// Emitted when the player confirms the current node. The
    /// controller translates this into <c>TryCloseFirstNightDialogue</c>.
    /// </summary>
    [Signal] public delegate void FollowPressedEventHandler();

    private Label _bodyLabel = null!;
    private Button _actionButton = null!;
    private string _followLabel = "Continue";
    private string _sleepLabel = "Give in to sleep";

    public override void _Ready()
    {
        // Anchored bottom, fixed height. The strip's outer rect is the
        // only place clicks should land during the night — outside it,
        // there is no Control and clicks fall through to the world.
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        OffsetTop = -StripHeight;
        OffsetLeft = SideMargin;
        OffsetRight = -SideMargin;

        // The strip itself captures clicks. This is intentional: the
        // button needs to work, and the player should be able to click
        // anywhere within the strip without it falling through to the
        // world below.
        MouseFilter = MouseFilterEnum.Stop;

        OverlayLayers.Apply(this, OverlayLayers.Tutorial);

        var panel = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Pass,
        };
        panel.AddThemeStyleboxOverride(
            "panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        margin.MouseFilter = MouseFilterEnum.Pass;
        panel.AddChild(margin);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 18);
        row.MouseFilter = MouseFilterEnum.Pass;
        margin.AddChild(row);

        _bodyLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _bodyLabel.ThemeTypeVariation = "BodyText";
        row.AddChild(_bodyLabel);

        _actionButton = new Button
        {
            CustomMinimumSize = new Vector2(160, 0),
            MouseFilter = MouseFilterEnum.Stop,
        };
        _actionButton.ThemeTypeVariation = "ButtonText";
        _actionButton.Pressed += OnActionButtonPressed;
        row.AddChild(_actionButton);

        // Hidden until the controller calls ShowNode. The strip never
        // appears during normal play; only the first night uses it.
        Visible = false;
    }

    /// <summary>
    /// Resolves the displayed body via the caller's already-localised
    /// string and selects the appropriate action label for the stage.
    /// The <paramref name="isSleeping"/> flag switches the button from
    /// "continue" to "give in to sleep"; the controller decides which
    /// stage requires which label so the strip stays stage-agnostic.
    /// </summary>
    public void ShowNode(string resolvedBody, bool isSleeping)
    {
        _bodyLabel.Text = resolvedBody;
        _actionButton.Text = isSleeping ? _sleepLabel : _followLabel;
        Visible = true;
    }

    /// <summary>
    /// Hides the strip. Idempotent: calling it twice has no extra
    /// effect, and the controller may invoke it whenever the night
    /// concludes or the player opens a modal that should not share
    /// the screen with the spirit.
    /// </summary>
    public void Vanish()
    {
        Visible = false;
    }

    /// <summary>
    /// Updates the cached button labels. The controller calls this
    /// once after the locale has loaded so the strip can show the
    /// "follow" / "sleep" texts in the player's language.
    /// </summary>
    public void SetActionLabels(string followLabel, string sleepLabel)
    {
        _followLabel = followLabel;
        _sleepLabel = sleepLabel;
    }

    private void OnActionButtonPressed()
    {
        EmitSignal(SignalName.FollowPressed);
    }
}
