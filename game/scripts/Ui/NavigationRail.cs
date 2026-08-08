#nullable enable

using System;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// The city's primary navigation: a compact vertical cluster of global actions,
/// anchored top-left over the world.
/// </summary>
/// <remarks>
/// <para>
/// Replaces <c>MacroActions</c>, an edge-to-edge horizontal strip that sat
/// directly under the status bar and cost the city 42 px of full-width height for
/// seven buttons. A vertical cluster sized to its own content reserves a fraction
/// of that, which is what "the city remains visually dominant" asks for.
/// </para>
/// <para>
/// It is deliberately **not** anchored full-height. A rail that spans the viewport
/// would put an opaque column over the left edge of a world the player clicks
/// into — trees, citizens and plots live there. Sized to content, the rail claims
/// only the rectangle it actually draws.
/// </para>
/// <para>
/// The rail owns its own structure and exposes the buttons as typed properties.
/// Before, the 4576-line macro view reached each one through a literal path like
/// <c>"../MacroActions/Actions/ConstructionMenuButton"</c> — seven strings that
/// broke if a button moved. It now holds one path to the rail. Deciding what a
/// button opens stays with the macro view: the rail is chrome, and chrome does not
/// know what a screen is.
/// </para>
/// </remarks>
[GlobalClass]
public partial class NavigationRail : PanelContainer
{
    /// <summary>Name of the container the buttons live in, inside the scene.</summary>
    public static readonly StringName ActionsName = "Actions";

    private BoxContainer? _actions;

    public IconButton HeroButton => RequireButton("HeroAccessButton");
    public IconButton ConstructionButton => RequireButton("ConstructionMenuButton");
    public IconButton MenuButton => RequireButton("GameMenuButton");
    public IconButton ExpeditionButton => RequireButton("ExpeditionMenuButton");
    public IconButton PoliciesButton => RequireButton("PoliciesButton");
    public IconButton CitizensButton => RequireButton("CitizensButton");
    public IconButton CameraButton => RequireButton("CameraModeButton");

    public override void _Ready()
    {
        // HUD chrome: the navigation must read identically at 03:00 and at noon,
        // so it sits above the ambient day/night tint.
        OverlayLayers.Apply(this, OverlayLayers.Hud);
        ApplyLayout();
    }

    /// <summary>
    /// Resolves a button on demand rather than caching it in <c>_Ready</c>.
    /// </summary>
    /// <remarks>
    /// The macro view sits before the rail in <c>CityPrototype.tscn</c>, and Godot
    /// readies siblings in tree order, so the rail's <c>_Ready</c> has not run when
    /// the macro view asks for its buttons. Caching them there returned null and
    /// crashed the boot. Resolving on access makes the rail independent of who is
    /// readied first, which is the property a shared component needs — reordering
    /// the scene would have fixed this one caller and left the trap set for the
    /// next.
    /// </remarks>
    private IconButton RequireButton(string name)
    {
        _actions ??= GetNodeOrNull<BoxContainer>(new NodePath(ActionsName))
            ?? throw new InvalidOperationException(
                $"{nameof(NavigationRail)} requires a {nameof(BoxContainer)} child named {ActionsName}.");

        return _actions.GetNodeOrNull<IconButton>(new NodePath(name))
            ?? throw new InvalidOperationException(
                $"{nameof(NavigationRail)} requires an {nameof(IconButton)} named {name} under {ActionsName}.");
    }

    /// <summary>
    /// Collapses the buttons to their icons and fixes the rail's width.
    /// </summary>
    /// <remarks>
    /// Icon-only, with each button's tooltip carrying the wording. This is only
    /// legible because the three buttons that used to share <c>user.svg</c> — hero,
    /// roster and camera — now have distinct glyphs; before that, collapsing the
    /// labels made three unrelated actions identical. <see cref="IconButton.ShowLabel"/>
    /// stays a supported capability, so restoring the words is one line if
    /// playtesting shows the glyphs are not learnable.
    /// </remarks>
    private void ApplyLayout()
    {
        foreach (var button in new[]
                 {
                     HeroButton, ConstructionButton, MenuButton, ExpeditionButton,
                     PoliciesButton, CitizensButton, CameraButton,
                 })
        {
            button.ShowLabel = false;
            // Without this the icon-only buttons shrink to the glyph and leave the
            // column ragged.
            button.CustomMinimumSize = new Vector2(0, Tokens.ControlHeight);
        }

        CustomMinimumSize = new Vector2(Tokens.RailWidth, 0);

        // Shrink-wrap the content so the rail never covers world the player needs
        // to click. Deferred because the buttons' own minimum sizes are not known
        // until the container has laid out this frame; reading them now yields the
        // pre-layout value and clips the last button off the bottom.
        CallDeferred(MethodName.ShrinkToContent);
    }

    private void ShrinkToContent()
    {
        if (!IsInsideTree()) return;
        Size = GetCombinedMinimumSize();
    }
}
