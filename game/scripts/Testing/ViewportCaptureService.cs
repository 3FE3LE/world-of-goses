#nullable enable
using System;
using System.IO;
using Godot;

namespace WorldofGoses.Testing;

/// <summary>
/// Saves golden frames from the engine's own viewport.
///
/// <para>The visual matrix used to produce its goldens with
/// <c>Graphics.CopyFromScreen</c> — a copy of whatever desktop pixels
/// happened to sit at the Godot window's screen coordinates. That is not a
/// screenshot of the game; it is a screenshot of a rectangle. The harness
/// raised the window and asked for foreground, but <c>SetForegroundWindow</c>
/// fails silently when the caller does not already own the foreground, and
/// an always-on-top window from another application sits above a topmost
/// window just fine. The failure mode was not theoretical: a run captured a
/// Chrome/Google Meet window and filed it as a valid golden frame.</para>
///
/// <para>This service removes the desktop from the golden path entirely. The
/// pixels come from <c>GetViewport().GetTexture().GetImage()</c>, so the
/// artifact provably belongs to the launched Godot process and to the
/// fixture it was launched with. No other window on the machine can
/// contribute a pixel, and occlusion, focus theft, scaling and monitor
/// layout stop being able to corrupt a capture.</para>
///
/// <para>Protocol with <c>tools/Capture-VisualMatrix.ps1</c>, chosen so the
/// script keeps control of <em>when</em> to shoot (after the fixture has
/// composed and any pointer input has been delivered) while the engine keeps
/// control of <em>what</em> is shot:</para>
/// <list type="number">
///   <item>The script launches Godot with
///         <c>--wog-visual-capture-out=&lt;png path&gt;</c>.</item>
///   <item>When it is ready, the script creates <c>&lt;png path&gt;.request</c>.</item>
///   <item>This node sees the request, saves the viewport image, and writes
///         <c>&lt;png path&gt;.done</c> (or <c>.failed</c> with the reason).</item>
///   <item>The script waits for one of those two files. There is no
///         fallback to a desktop grab: a capture that cannot be taken from
///         the viewport is a failed capture, not a differently-sourced one.</item>
/// </list>
///
/// <para>Dev-only. Nothing constructs this node unless
/// <see cref="VisualRegressionHarness.IsActive"/> is true, which is false in
/// normal play.</para>
/// </summary>
internal sealed partial class ViewportCaptureService : Node
{
    private readonly string _outputPath;
    private bool _captureStarted;

    internal ViewportCaptureService(string outputPath)
    {
        _outputPath = outputPath;
        Name = nameof(ViewportCaptureService);
    }

    private string RequestPath => _outputPath + ".request";
    private string DonePath => _outputPath + ".done";
    private string FailedPath => _outputPath + ".failed";

    public override void _Ready()
    {
        // Clear any stale handshake files from a previous run in the same
        // output directory. Without this, a leftover .done would let the
        // script accept the *previous* fixture's frame as this one's.
        foreach (string path in new[] { RequestPath, DonePath, FailedPath })
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                GD.PushWarning($"Could not clear stale capture handshake '{path}': {ex.Message}");
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_captureStarted) return;
        if (!File.Exists(RequestPath)) return;
        _captureStarted = true;
        _ = CaptureAsync();
    }

    private async System.Threading.Tasks.Task CaptureAsync()
    {
        try
        {
            // Wait for the frame currently being composed to finish drawing,
            // so the grabbed texture is a complete frame rather than whatever
            // the GPU had mid-flight.
            await ToSignal(RenderingServer.Singleton, RenderingServerInstance.SignalName.FramePostDraw);

            Viewport viewport = GetViewport();
            Image? image = viewport.GetTexture()?.GetImage();
            if (image is null)
            {
                WriteFailure("viewport produced no image");
                return;
            }

            Error error = image.SavePng(_outputPath);
            if (error != Error.Ok)
            {
                WriteFailure($"SavePng returned {error}");
                return;
            }

            // The .done file carries the dimensions the engine actually
            // rendered, so the script can assert the frame is the resolution
            // it asked for instead of trusting the window manager.
            File.WriteAllText(DonePath, $"{image.GetWidth()}x{image.GetHeight()}");
        }
        catch (Exception ex)
        {
            WriteFailure(ex.Message);
        }
    }

    private void WriteFailure(string reason)
    {
        GD.PushError($"Viewport capture failed: {reason}");
        try
        {
            File.WriteAllText(FailedPath, reason);
        }
        catch (Exception ex)
        {
            GD.PushError($"Could not write the capture failure marker: {ex.Message}");
        }
    }
}
