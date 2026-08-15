using System;
using Godot;

namespace WorldofGoses.Prototypes;

/// <summary>Read-only visual specimen for pixel-perfect font regression.</summary>
/// <remarks>
/// <para>
/// The capture waits for the viewport to reach the size the harness asked for
/// instead of firing on a fixed frame number. The project ships fullscreen
/// borderless (<c>display/window/size/mode = 3</c>), so Godot boots at the
/// desktop size and only then honours <c>--windowed --resolution</c>. Shooting
/// at frame four caught the window mid-resize: on a 2560x1440 desktop this
/// fixture wrote a 2560x1440 image into a file named <c>1280x720</c>, and the
/// only thing that noticed was the size assertion in the calling script.
/// </para>
/// <para>
/// If the size never settles the capture still happens, so the failure surfaces
/// as a wrong-sized frame the script rejects rather than as a missing file the
/// script blames on a timeout.
/// </para>
/// </remarks>
public sealed partial class TypographySpecimen : Control
{
    private const string OutputArgumentPrefix = "--wog-typography-output=";
    private const string SizeArgumentPrefix = "--wog-typography-size=";

    /// <summary>Consecutive frames at the requested size before shooting.</summary>
    private const int SettledFramesRequired = 3;

    /// <summary>
    /// Seconds to wait for the window, kept inside the caller's own deadline.
    /// </summary>
    /// <remarks>
    /// Measured in elapsed time rather than in frames. This scene is a handful of
    /// Labels on a ColorRect, so it renders thousands of frames per second: a
    /// frame-count budget generous enough to look like ten seconds expired in a
    /// fraction of one, and the fixture gave up and shot the fullscreen window it
    /// had been launched with.
    /// </remarks>
    private const double GiveUpAfterSeconds = 10.0;

    private Vector2I? _expectedSize;
    private Vector2I _lastLoggedSize = -Vector2I.One;
    private double _elapsed;
    private int _settledFrames;
    private bool _captureStarted;

    public override void _Ready()
    {
        _expectedSize = FindExpectedSize();
        PinWindow();
    }

    /// <summary>
    /// Pins the window to the requested size, in windowed mode.
    /// </summary>
    /// <remarks>
    /// The same thing, for the same reason, as
    /// <c>VisualRegressionHarness.ApplyCaptureWindowSize</c>: the project ships
    /// fullscreen borderless and a fullscreen window ignores <c>--resolution</c>
    /// outright, taking the desktop's size instead. This fixture never did it and
    /// so rendered 2560x1440 on a 2560x1440 desktop no matter what the script
    /// asked for. Asking the display server is the only place a project setting
    /// cannot override the answer.
    /// </remarks>
    private void PinWindow()
    {
        if (_expectedSize is not { } size) return;
        if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Windowed)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        }
        if (DisplayServer.WindowGetSize() != size) DisplayServer.WindowSetSize(size);
    }

    public override void _Process(double delta)
    {
        if (_captureStarted) return;
        _elapsed += delta;

        // DisplayServer.WindowGetSize, not Viewport.GetVisibleRect. Under
        // stretch/mode = canvas_items the visible rect always reports the 1280x720
        // base size no matter how large the framebuffer is, so comparing against it
        // declared the window settled on frame one while the image being saved was
        // still 2560x1440. The window size is the number the saved PNG will have.
        Vector2I windowSize = DisplayServer.WindowGetSize();
        if (windowSize != _lastLoggedSize)
        {
            GD.Print($"[WOG-TYPOGRAPHY-CAPTURE] t={_elapsed:0.00}s: window {windowSize.X}x{windowSize.Y}");
            _lastLoggedSize = windowSize;
        }

        bool atRequestedSize = _expectedSize is null || windowSize == _expectedSize.Value;
        _settledFrames = atRequestedSize ? _settledFrames + 1 : 0;

        if (_settledFrames < SettledFramesRequired && _elapsed < GiveUpAfterSeconds) return;

        _captureStarted = true;
        SetProcess(false);

        string outputPath = FindArgument(OutputArgumentPrefix);
        if (string.IsNullOrWhiteSpace(outputPath)) return;
        _ = CaptureAsync(outputPath);
    }

    private async System.Threading.Tasks.Task CaptureAsync(string outputPath)
    {
        // Let the frame being composed finish, so the grabbed texture is a whole
        // frame rather than whatever the GPU had in flight.
        await ToSignal(RenderingServer.Singleton, RenderingServerInstance.SignalName.FramePostDraw);

        Image image = GetViewport().GetTexture().GetImage();
        Error result = image.SavePng(outputPath);
        GD.Print($"[WOG-TYPOGRAPHY-CAPTURE] {image.GetWidth()}x{image.GetHeight()} -> {outputPath}");
        if (result != Error.Ok)
        {
            GD.PushError($"Typography capture failed with {result}: {outputPath}");
        }
    }

    private static Vector2I? FindExpectedSize()
    {
        string raw = FindArgument(SizeArgumentPrefix);
        if (string.IsNullOrWhiteSpace(raw)) return null;

        string[] parts = raw.Split('x', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], out int width)
            || !int.TryParse(parts[1], out int height))
        {
            GD.PushWarning($"Unreadable typography capture size '{raw}'; shooting unconditionally.");
            return null;
        }
        return new Vector2I(width, height);
    }

    private static string FindArgument(string prefix)
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith(prefix, StringComparison.Ordinal))
            {
                return argument[prefix.Length..];
            }
        }
        return string.Empty;
    }
}
