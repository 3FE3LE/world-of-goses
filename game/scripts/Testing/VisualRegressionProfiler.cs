#nullable enable
using Godot;

namespace WorldofGoses.Testing;

/// <summary>
/// Dev-only per-frame profiler used by
/// <c>tools/Capture-VisualMatrix.ps1</c>. The harness tails the last
/// <see cref="FrameTimeLogTail"/> samples from the run's log file
/// after warm-up, so the screenshot capture does not have to
/// self-time its own sleep loops (which were blind to any real stall
/// inside the Godot process — see TO_DO.md S-1.7's 2026-07-27
/// audit).
///
/// <para>Architecture Hardening issue #8 lifted this from
/// <c>CityWorldController</c> so the controller does not own
/// capture/profiling code in normal play. The profiler runs only
/// while <see cref="VisualRegressionHarness.IsActive"/> is true;
/// it gates itself on the harness so a stray registration does not
/// bleed into production.</para>
/// </summary>
public sealed partial class VisualRegressionProfiler : Node
{
    /// <summary>
    /// Log tag the harness greps for. The literal is part of the
    /// contract with <c>Capture-VisualMatrix.ps1</c>; changing it
    /// silently breaks the visual regression budget check.
    /// </summary>
    public const string FrameTimeLogTag = "[WOG-FRAME-TIME]";

    /// <summary>
    /// Maximum samples emitted per run. ~5 s at 60 fps; bounds log
    /// growth if the capture window stays open.
    /// </summary>
    private const int FrameTimeSampleCap = 300;

    /// <summary>
    /// How many tail samples the harness parses from the log file.
    /// </summary>
    public const int FrameTimeLogTail = 30;

    private static VisualRegressionProfiler? _instance;

    private int _samplesEmitted;

    /// <summary>
    /// Registers the profiler under the harness name so it survives
    /// the controller's lifecycle. Idempotent: a second call returns
    /// the existing node.
    /// </summary>
    public static VisualRegressionProfiler Attach(Node parent)
    {
        if (_instance is not null) return _instance;
        var profiler = new VisualRegressionProfiler { Name = "VisualRegressionProfiler" };
        parent.AddChild(profiler);
        _instance = profiler;
        return profiler;
    }

    /// <summary>
    /// Detaches the profiler and clears the cached instance so the
    /// next <see cref="Attach"/> call re-creates a fresh one.
    /// </summary>
    public static void Detach()
    {
        if (_instance is null) return;
        _instance.QueueFree();
        _instance = null;
    }

    public override void _Process(double delta)
    {
        _ = delta;
        if (!VisualRegressionHarness.IsActive) return;
        if (_samplesEmitted >= FrameTimeSampleCap) return;
        _samplesEmitted++;

        double processMs = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0;
        // Invariant culture, not the OS locale's own decimal separator —
        // the harness parses this with double.TryParse(InvariantCulture)
        // and a comma-decimal locale (e.g. es-*) silently broke every
        // sample otherwise (found via a real capture run, not by reading
        // the code — see TO_DO.md S-1.7).
        GD.Print($"{FrameTimeLogTag} {processMs.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}");
    }
}
