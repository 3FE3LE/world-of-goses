#nullable enable

using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// The transparent column a macro side panel lives in. It spans the shared
/// vertical envelope — top inset to bottom inset inside <c>ScreenContent</c> —
/// and the panel inside it takes as much of that as it currently needs.
/// </summary>
/// <remarks>
/// <para>
/// It exists so the two side panels agree on where they may reach without
/// either of them knowing how tall it is (GitHub #17). The previous contract
/// was a fixed 536 px on each <em>body</em>, which made the content the
/// accidental authority on the outer envelope: the right rail has two
/// persistent headers and the left panel one, so the same body height gave
/// them different outer heights (608 px against 544 px), and adding a third
/// section to the rail would have meant re-deriving the constant to keep it
/// inside the viewport.
/// </para>
/// <para>
/// Being a <see cref="Container"/> is the whole mechanism. Anchors govern a
/// child of a plain <see cref="Control"/>, so an anchored panel always fills
/// its rect and a collapsed one keeps an opaque column over the map — the
/// defect #15 removed. Inside a container the child's own vertical size flag
/// decides: <see cref="Control.SizeFlags.ShrinkBegin"/> collapses it to the
/// natural height of its headers, <see cref="Control.SizeFlags.ExpandFill"/>
/// gives it the envelope and lets its scrolling body absorb the remainder.
/// No panel computes <c>envelope - headers × headerHeight</c>; the container
/// tree resolves it.
/// </para>
/// <para>
/// The host ignores the pointer. It is a region, not a surface: the clicks
/// belong to the panel inside it, and the map shows through everywhere the
/// panel is not.
/// </para>
/// </remarks>
[GlobalClass]
public partial class SidePanelHost : VBoxContainer
{
    /// <summary>
    /// Distance from the top and bottom edges of <c>ScreenContent</c> that
    /// both side panels leave clear. Authored as offsets on each host in
    /// <c>CityPrototype.tscn</c>; named here so the two are comparable and a
    /// third side panel has one number to adopt rather than one to guess.
    /// </summary>
    public const int Inset = Tokens.SpacingBase;

    public SidePanelHost()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>
    /// The vertical size flag a panel in this host should carry for the given
    /// state. Collapsed panels shrink to their headers; an expanded one takes
    /// the envelope so its body can scroll inside it rather than growing the
    /// panel past the viewport.
    /// </summary>
    public static SizeFlags PanelSizing(bool expanded) =>
        expanded ? SizeFlags.ExpandFill : SizeFlags.ShrinkBegin;
}
