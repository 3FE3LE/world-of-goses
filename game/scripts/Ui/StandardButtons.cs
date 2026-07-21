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
/// The factory returns <see cref="IconButton"/> instances so the
/// buttons carry the project's typography (Jersey 10 via the
/// <c>ButtonText</c> theme variation defined in
/// <c>res://assets/ui/default_theme.tres</c>) and the engine's
/// default popup respects the Pixelify base
/// <c>Label</c> entry also defined there.
/// </summary>
public static class StandardButtons
{
    /// <summary>
    /// Builds the canonical "Back to city" button — arrow icon,
    /// Jersey 10 typography, "Return to the city view" tooltip. Used
    /// by the building detail view and the hero profile view.
    /// </summary>
    public static IconButton BackToCityButton()
    {
        var button = NewBase("Back to city");
        button.IconPath = IconPaths.ArrowLeft;
        button.CustomMinimumSize = new Vector2(160, 44);
        button.TooltipText = "Return to the city view";
        return button;
    }

    /// <summary>
    /// Builds the canonical "View hero" button — user icon, Jersey
    /// 10 typography, "Open the hero profile" tooltip. Currently used
    /// by the construction panel footer; the macro-view shortcut uses
    /// an .tscn-instanced variant with the same properties.
    /// </summary>
    public static IconButton ViewHeroButton()
    {
        var button = NewBase("View hero");
        button.IconPath = IconPaths.User;
        button.CustomMinimumSize = new Vector2(160, 44);
        button.TooltipText = "Open the hero profile";
        return button;
    }

    private static IconButton NewBase(string label)
    {
        return new IconButton
        {
            Label = label,
            ThemeTypeVariation = "ButtonText",
            FocusMode = Control.FocusModeEnum.All,
        };
    }
}
