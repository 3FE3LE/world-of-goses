#nullable enable
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Static factory for the buttons that several screens reach for
/// ("back to city", "view hero", etc.). Routing every consumer
/// through these helpers keeps the icon set, theme variation,
/// minimum size, focus mode, and tooltip text identical across the
/// project so the player never sees a divergent definition of the
/// same action (a quirk we hit when the hero profile created a plain
/// <see cref="Button"/> while the building detail view used an
/// <see cref="IconButton"/> with an arrow glyph).
///
/// Canonical icon actions are PackedScene instances; lightweight text
/// and choice actions are constructed here with the same sizing and
/// focus policy. All carry the project's typography (Jersey 10 via the
/// <c>ButtonText</c> theme variation defined in
/// <c>res://assets/ui/default_theme.tres</c>) and the engine's
/// default popup respects the Jersey 10 base
/// <c>Label</c> entry also defined there.
/// </summary>
public static class StandardButtons
{
    private static readonly PackedScene BackToCityScene =
        GD.Load<PackedScene>("res://scenes/Components/BackToCityButton.tscn");
    private static readonly PackedScene ViewHeroScene =
        GD.Load<PackedScene>("res://scenes/Components/ViewHeroButton.tscn");

    /// <summary>
    /// Builds the canonical "Back to city" button — arrow icon,
    /// Jersey 10 typography, "Return to the city view" tooltip. Used
    /// by the building detail view and the hero profile view.
    /// </summary>
    public static Button BackToCityButton()
    {
        return BackToCityScene.Instantiate<Button>();
    }

    /// <summary>
    /// Builds the canonical "View hero" button — user icon, Jersey
    /// 10 typography, "Open the hero profile" tooltip. Currently used
    /// by the construction panel footer and empty macro state; the
    /// persistent macro shortcut uses an aligned behavioural subclass.
    /// </summary>
    public static IconButton ViewHeroButton()
    {
        IconButton button = ViewHeroScene.Instantiate<IconButton>();
        // Reassert the canonical content after PackedScene instantiation. The
        // native Button text can be present while IconButton.ButtonText is
        // still its C# default; _Ready() would then apply that empty value and
        // render a blank action in dynamically built footers.
        button.SetIconAndLabel(IconPaths.User, "View hero");
        return button;
    }

    public static IconButton IconAction(
        string iconPath,
        string label,
        string variation = "ButtonText",
        string tooltip = "") => new()
    {
        IconPath = iconPath,
        ButtonText = label,
        TooltipText = tooltip,
        ThemeTypeVariation = variation,
        CustomMinimumSize = new Vector2(160, 44),
        FocusMode = Control.FocusModeEnum.All,
    };

    public static Button TextAction(string label, string tooltip = "") => new()
    {
        Text = label,
        TooltipText = tooltip,
        ThemeTypeVariation = "ButtonText",
        FocusMode = Control.FocusModeEnum.All,
    };

    public static Button NavigationButton(string label) => new()
    {
        Text = label,
        ThemeTypeVariation = "ButtonText",
        CustomMinimumSize = new Vector2(150, 44),
        FocusMode = Control.FocusModeEnum.All,
    };

    public static TooltipButton ChoiceButton(string label, string tooltip) => new()
    {
        Text = label,
        TooltipText = tooltip,
        ThemeTypeVariation = "ButtonText",
        CustomMinimumSize = new Vector2(230, 44),
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        FocusMode = Control.FocusModeEnum.All,
    };
}
