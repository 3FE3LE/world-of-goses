using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Single source of truth for HUD-internal focus rings. Three
/// surfaces (<see cref="PrimaryNavDock"/>, <see cref="ActionDock"/>,
/// <see cref="ExpeditionRail"/>) and two slot strips
/// (<see cref="ExpeditionSquadStrip"/>, <see cref="ExpeditionSkillStrip"/>)
/// used to walk their own <c>FocusNeighbor*</c> properties by hand.
/// They are unified here.
///
/// <para>
/// Two orientation variants:
/// <list type="bullet">
///   <item><b>Horizontal</b> wires <c>FocusNeighborLeft/Right</c> +
///   <c>FocusPrevious/Next</c>; suited for top bars, action docks,
///   slot strips.</item>
///   <item><b>Vertical</b> wires <c>FocusNeighborTop/Bottom</c> +
///   <c>FocusPrevious/Next</c>; suited for the expedition rail
///   which is laid out top-to-bottom and is read by gamepad
///   D-pad up/down.</item>
/// </list>
/// Both forms use <see cref="Control.GetPathTo"/> so the path is
/// relative to the wired control. <see cref="ExpeditionRail"/>'s
/// previous implementation used absolute
/// <see cref="Node.GetPath"/>, which broke when the rail was
/// reparented; the bug class is closed by routing through here.
/// </para>
///
/// <para>
/// Inter-surface focus traversal (top bar → rail → dock → modal)
/// is not owned by this helper. Each surface decides a default
/// that yields to its successor when its <c>GrabFocus</c> is
/// called from a known cross-surface command.
/// </para>
/// </summary>
internal static class FocusRing
{
    /// <summary>
    /// Wires a horizontal circular focus cycle across
    /// <paramref name="controls"/>. Paths are computed from each
    /// control's own position
    /// (<see cref="Control.GetPathTo(Control)"/>), not from the
    /// scene root.
    /// </summary>
    public static void WireHorizontal(System.Collections.Generic.IReadOnlyList<Control> controls)
    {
        if (controls is null || controls.Count == 0) return;
        int count = controls.Count;
        for (int i = 0; i < count; i++)
        {
            Control current = controls[i];
            Control previous = controls[(i + count - 1) % count];
            Control next = controls[(i + 1) % count];
            current.FocusNeighborLeft = current.GetPathTo(previous);
            current.FocusNeighborRight = current.GetPathTo(next);
            current.FocusPrevious = current.GetPathTo(previous);
            current.FocusNext = current.GetPathTo(next);
        }
    }

    /// <summary>
    /// Wires a vertical circular focus cycle across
    /// <paramref name="controls"/>. Same relative-path convention
    /// as <see cref="WireHorizontal"/>.
    /// </summary>
    public static void WireVertical(System.Collections.Generic.IReadOnlyList<Control> controls)
    {
        if (controls is null || controls.Count == 0) return;
        int count = controls.Count;
        for (int i = 0; i < count; i++)
        {
            Control current = controls[i];
            Control previous = controls[(i + count - 1) % count];
            Control next = controls[(i + 1) % count];
            current.FocusNeighborTop = current.GetPathTo(previous);
            current.FocusNeighborBottom = current.GetPathTo(next);
            current.FocusPrevious = current.GetPathTo(previous);
            current.FocusNext = current.GetPathTo(next);
        }
    }
}
