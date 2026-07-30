#nullable enable
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Keeps world-camera pointer gestures from leaking through UI controls that
/// can legitimately own the same gesture. A ScrollContainer may leave a wheel
/// event unhandled when it is already at an edge, so _UnhandledInput alone is
/// not a sufficient boundary between the HUD and the world.
/// </summary>
internal static class UiInputBoundary
{
    internal static bool ShouldWorldCameraHandleWheel(
        bool isWheelEvent,
        bool pointerIsOverScrollableUi) =>
        isWheelEvent && !pointerIsOverScrollableUi;

    internal static bool IsWheelEvent(InputEvent inputEvent) =>
        inputEvent is InputEventMouseButton
        {
            Pressed: true,
            ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown,
        };

    internal static bool IsPointerOverScrollableUi(Viewport viewport)
    {
        for (Node? node = viewport.GuiGetHoveredControl(); node is not null; node = node.GetParent())
        {
            if (node is not ScrollContainer scroll || !scroll.IsVisibleInTree()) continue;
            if (scroll.HorizontalScrollMode != ScrollContainer.ScrollMode.Disabled
                || scroll.VerticalScrollMode != ScrollContainer.ScrollMode.Disabled)
            {
                return true;
            }
        }
        return false;
    }
}
