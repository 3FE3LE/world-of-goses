#nullable enable
using Godot;

namespace WorldofGoses;

/// <summary>
/// Named camera-only input map. Registered once so gameplay code never treats
/// WASD as direct citizen control and can later move these bindings to an
/// editable settings surface without changing the camera implementation.
/// </summary>
public static class CameraInputActions
{
    public const string PanLeft = "camera_pan_left";
    public const string PanRight = "camera_pan_right";
    public const string PanUp = "camera_pan_up";
    public const string PanDown = "camera_pan_down";
    public const string ToggleFollow = "camera_toggle_follow";

    public static void EnsureRegistered()
    {
        Ensure(PanLeft, Key.A, Key.Left);
        Ensure(PanRight, Key.D, Key.Right);
        Ensure(PanUp, Key.W, Key.Up);
        Ensure(PanDown, Key.S, Key.Down);
        Ensure(ToggleFollow, Key.F);
    }

    private static void Ensure(string action, params Key[] keys)
    {
        if (!InputMap.HasAction(action)) InputMap.AddAction(action);
        foreach (Key key in keys)
        {
            var input = new InputEventKey { Keycode = key };
            if (!InputMap.ActionHasEvent(action, input)) InputMap.ActionAddEvent(action, input);
        }
    }
}
