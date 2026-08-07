#nullable enable
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Base for the project's standard action buttons.
///
/// <para>
/// These exist so a view never has to decide what an action looks like. Before
/// them, screens hand-rolled <c>new Button { … }</c> and each one picked its
/// own theme variation, minimum size and focus policy — which is how the same
/// action ended up with three different silhouettes across the game, and how a
/// re-skin turned into an audit of thirty call sites. A view now names the
/// <em>role</em> of the action and the standard supplies the appearance.
/// </para>
///
/// <para>
/// Subclasses set only <see cref="Control.ThemeTypeVariation"/>. Everything
/// visual lives in <c>game/assets/ui/default_theme.tres</c>; nothing here
/// coins a colour, a stylebox or a font, because
/// <c>docs/UI_PATTERNS.md</c> §7 reserves that to the theme registry.
/// </para>
/// </summary>
public abstract partial class ActionButton : Button
{
    /// <summary>
    /// Shared control height. Matches <see cref="OnboardingChoiceButton"/> so
    /// a row of actions lines up with a column of choices, and stays clear of
    /// the ~34 px intrinsic height of a single <c>Jersey 10</c> line inside the
    /// 9-slice's 4 px vertical content margins. Defined by
    /// <see cref="Tokens.ControlHeight"/> so the button standard and the spacing
    /// scale cannot disagree about what an action's height is.
    /// </summary>
    public const int DefaultHeight = Tokens.ControlHeight;

    protected ActionButton(string variation, int minimumWidth)
    {
        ThemeTypeVariation = variation;
        CustomMinimumSize = new Vector2(minimumWidth, DefaultHeight);
        FocusMode = FocusModeEnum.All;
    }
}

/// <summary>
/// The action a screen wants the player to take. One per surface: a second
/// primary makes both read as secondary.
/// </summary>
[GlobalClass]
public partial class PrimaryActionButton : ActionButton
{
    public PrimaryActionButton() : base("ButtonPrimary", 150) { }
}

/// <summary>
/// Every other affirmative action — navigation, toggles, "close", "back".
/// This is the default; reach for it unless the action is the screen's point
/// or is destructive.
/// </summary>
[GlobalClass]
public partial class SecondaryActionButton : ActionButton
{
    public SecondaryActionButton() : base("ButtonText", 150) { }
}

/// <summary>
/// Irreversible actions — permanent reset, cancelling work already paid for.
/// Red is load-bearing here, not decoration, so this variation keeps its own
/// 9-slice while the rest of the UI moved to slate.
/// </summary>
[GlobalClass]
public partial class DangerActionButton : ActionButton
{
    public DangerActionButton() : base("ButtonWarning", 150) { }
}
