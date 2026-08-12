#nullable enable

using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// The one-pixel rule the compact HUD uses between sections.
/// </summary>
/// <remarks>
/// <para>
/// Trivially thin as a type, and that is the point: every panel that wanted a
/// divider was writing the same three-property initialiser — the
/// <c>HudSeparator</c> variation, <see cref="Control.MouseFilterEnum.Ignore"/>
/// so the rule never eats a click meant for the row under it, and nothing
/// else. Three copies of a literal are three chances for one of them to keep
/// its default mouse filter and quietly become a dead strip inside a
/// scrollable body.
/// </para>
/// <para>
/// A <c>[GlobalClass]</c> node rather than a PackedScene for the reason
/// <see cref="HudSectionHeader"/> gives: these are emitted procedurally by
/// whichever panel is rebuilding a body, never dropped into a scene by hand.
/// </para>
/// </remarks>
[GlobalClass]
public partial class HudSeparator : HSeparator
{
    public HudSeparator()
    {
        ThemeTypeVariation = "HudSeparator";
        MouseFilter = MouseFilterEnum.Ignore;
    }
}
