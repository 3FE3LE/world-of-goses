using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Applies the OS-reported display safe area as direct <c>Offset*</c>
/// deltas on a Control. Use this on top-level controls that the scene
/// anchors to the edges of the viewport (HUD status bar, macro action
/// strip). It avoids wrapping the node in a <c>MarginContainer</c>,
/// which previously rendered as a visible grey band on
/// <c>CityStatusPanel</c> and <c>MacroActions</c> (see <c>TO_DO.md</c>
/// 2026-07-22 entry, reverted in commit <c>d0fd51d</c>).
///
/// The helper writes four <c>Offset*</c> values that **shrink** the
/// node's inner rect so the content stays inside the safe area on every
/// edge. Pass the <paramref name="minimumInsetPx"/> for the desktop case
/// where the OS reports no inset at all.
/// </summary>
public static class SafeArea
{
    public static void ApplyOffsets(Control node, int minimumInsetPx = 8)
    {
        if (node is null) return;
        Vector2I window = DisplayServer.WindowGetSize();
        if (window.X <= 0 || window.Y <= 0) return;
        Rect2I safe = DisplayServer.GetDisplaySafeArea();
        Vector2 viewport = node.GetViewportRect().Size;
        if (viewport.X <= 0 || viewport.Y <= 0) return;

        float scaleX = viewport.X / window.X;
        float scaleY = viewport.Y / window.Y;
        int left = Mathf.Max(minimumInsetPx, Mathf.RoundToInt(safe.Position.X * scaleX));
        int top = Mathf.Max(minimumInsetPx, Mathf.RoundToInt(safe.Position.Y * scaleY));
        int right = Mathf.Max(minimumInsetPx, Mathf.RoundToInt((window.X - safe.End.X) * scaleX));
        int bottom = Mathf.Max(minimumInsetPx, Mathf.RoundToInt((window.Y - safe.End.Y) * scaleY));

        // Control.Offset* are deltas from the anchored edge. Positive
        // values shrink the node; negative values grow it. We always
        // want at least the minimum inset, so all four edges shrink.
        node.OffsetLeft = left;
        node.OffsetTop = top;
        node.OffsetRight = -right;
        node.OffsetBottom = -bottom;
    }
}
