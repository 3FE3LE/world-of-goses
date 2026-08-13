#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses.Testing;

/// <summary>
/// Single entry point for visual regression orchestration. Owns the
/// detection of the harness mode (env var + CLI flag), parses the
/// requested fixture name from <c>--wog-visual-fixture=&lt;name&gt;</c>,
/// runs the matching fixture against the live world, and exposes a
/// typed contract for callers that want to know whether the harness is
/// active.
///
/// <para>Architecture Hardening A10 closes the previous shape, where:
/// <list type="bullet">
///   <item>every fixture scene probed <c>WOG_VISUAL_CAPTURE</c> directly
///         via <c>System.Environment.GetEnvironmentVariable</c>;</item>
///   <item>every fixture method lived on <c>CityWorldController</c>
///         with names ending in <c>ForFixture</c> or
///         <c>ForVisualRegression</c>;</item>
///   <item>every fixture mutator on the domain (<c>SeedProgressForFixture</c>,
///         <c>ConcludeFirstNightForFixtures</c>, etc.) was <c>public</c>
///         so a screenshot could author the state it wanted.</item>
/// </list></para>
///
/// <para>After A10:
/// <list type="bullet">
///   <item>Domain mutators are <c>internal</c> and only callable
///         through the harness or the test assembly, enforced by an
///         architecture guard.</item>
///   <item>Fixture orchestration lives here, not in
///         <c>CityPrototype</c> or <c>CityWorldController</c>.</item>
///   <item>Runtime scenes that want to know whether the harness is
///         active read <see cref="IsActive"/> — they do not probe the
///         environment themselves.</item>
/// </list></para>
///
/// <para>The harness is intentionally not an autoload. <see cref="Activate"/>
/// runs once from <c>CityPrototype._Ready</c>; everything else asks
/// <see cref="IsActive"/> after that. Keeping the lifecycle tied to
/// <c>CityPrototype</c> matches the codebase's "no premature singletons"
/// rule.</para>
/// </summary>
public static class VisualRegressionHarness
{
    private const string EnvironmentVariable = "WOG_VISUAL_CAPTURE";
    private const string CommandLineFlag = "--wog-visual-capture";
    private const string FixtureCommandLinePrefix = "--wog-visual-fixture=";
    private const string CapturePathCommandLinePrefix = "--wog-visual-capture-out=";
    private const string CaptureSizeCommandLinePrefix = "--wog-visual-capture-size=";

    private static bool _isActive;
    private static string? _fixtureName;
    private static string? _longTerrariumCapturePath;
    private static string? _viewportCapturePath;
    private static Vector2I? _captureSize;

    /// <summary>
    /// True once <see cref="Activate"/> has run and confirmed the
    /// harness mode is on. Always <c>false</c> in normal play: the
    /// harness only flips this bit when the env var or CLI flag is
    /// present.
    /// </summary>
    public static bool IsActive => _isActive;

    /// <summary>
    /// The fixture name requested via <c>--wog-visual-fixture=</c>, or
    /// <c>null</c> when the harness was activated without a fixture
    /// (caller falls back to the default state).
    /// </summary>
    public static string? RequestedFixture => _fixtureName;

    /// <summary>
    /// Path the harness uses to write a long terrarium probe, when
    /// the caller asked for one. <c>null</c> when no probe was
    /// requested.
    /// </summary>
    public static string? LongTerrariumCapturePath => _longTerrariumCapturePath;

    /// <summary>
    /// Where the golden frame should be written, from
    /// <c>--wog-visual-capture-out=</c>. <c>null</c> when the caller did not
    /// ask for a viewport capture.
    /// </summary>
    public static string? ViewportCapturePath => _viewportCapturePath;

    /// <summary>
    /// Attaches the engine-side golden-frame writer to
    /// <paramref name="host"/> when one was requested. No-op in normal play
    /// and when no output path was given.
    ///
    /// <para>The capture is taken from the engine's own viewport rather than
    /// from the desktop, so an unrelated foreground window cannot end up in
    /// a golden frame — see <see cref="ViewportCaptureService"/>.</para>
    /// </summary>
    public static void AttachViewportCapture(Node host)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (!_isActive || _viewportCapturePath is null) return;
        ApplyCaptureWindowSize();
        host.AddChild(new ViewportCaptureService(_viewportCapturePath));
    }

    /// <summary>
    /// Pins the window to the size the matrix asked for, in windowed mode.
    /// </summary>
    /// <remarks>
    /// The golden frame is the viewport, and the viewport is the window: with
    /// <c>stretch/mode = canvas_items</c> and <c>aspect = expand</c>, a
    /// larger window renders more world rather than the same world larger, so
    /// the window size is part of what the frame means. The project ships
    /// fullscreen borderless, and a fullscreen window ignores
    /// <c>--resolution</c> outright — it takes the desktop's size. On a
    /// 2560x1440 desktop every capture therefore rendered 2560x1440 while the
    /// matrix asked for 1280x720, and the run failed at the size assertion
    /// instead of producing evidence. Asking the display server directly is
    /// the only place the answer cannot be overridden by a project setting.
    /// Dev-only: nothing reaches here unless a capture was requested.
    /// </remarks>
    private static void ApplyCaptureWindowSize()
    {
        if (_captureSize is not { } size) return;
        if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Windowed)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        }
        if (DisplayServer.WindowGetSize() != size) DisplayServer.WindowSetSize(size);
    }

    /// <summary>
    /// Reads the environment and command line, decides whether the
    /// harness is active, and returns the requested fixture name.
    /// Idempotent: a second call returns the same result without
    /// re-parsing the arguments.
    /// </summary>
    public static bool Activate()
    {
        if (_isActive) return true;
        if (!IsEnvironmentOn() && !HasCommandLineFlag()) return false;

        _isActive = true;
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith(FixtureCommandLinePrefix, StringComparison.Ordinal))
            {
                _fixtureName = argument[FixtureCommandLinePrefix.Length..];
            }
            else if (argument.StartsWith("WOG_LONG_TERRARIUM_CAPTURE=", StringComparison.Ordinal))
            {
                _longTerrariumCapturePath = argument["WOG_LONG_TERRARIUM_CAPTURE=".Length..];
            }
            else if (argument.StartsWith(CapturePathCommandLinePrefix, StringComparison.Ordinal))
            {
                _viewportCapturePath = argument[CapturePathCommandLinePrefix.Length..];
            }
            else if (argument.StartsWith(CaptureSizeCommandLinePrefix, StringComparison.Ordinal))
            {
                _captureSize = ParseCaptureSize(
                    argument[CaptureSizeCommandLinePrefix.Length..]);
            }
        }

        // Long terrarium probe is opt-in via its own flag and does
        // not require a fixture name.
        if (_longTerrariumCapturePath is not null)
        {
            return true;
        }

        return true;
    }

    /// <summary>
    /// Reads a <c>WxH</c> slug. Returns <c>null</c> for anything that is not
    /// two positive integers, so a malformed flag leaves the window alone
    /// rather than resizing it to nonsense the frame would then be judged on.
    /// </summary>
    private static Vector2I? ParseCaptureSize(string slug)
    {
        string[] parts = slug.Split('x', StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return null;
        if (!int.TryParse(parts[0], out int width) || width <= 0) return null;
        if (!int.TryParse(parts[1], out int height) || height <= 0) return null;
        return new Vector2I(width, height);
    }

    /// <summary>
    /// Convenience for callers that want to read <c>WOG_VISUAL_CAPTURE</c>
    /// through the harness without poking the environment themselves.
    /// Used by <c>MacroStreetLiveView</c>, <c>PanelHeader</c>,
    /// <c>LocaleManager</c> and similar runtime classes that have a
    /// dev-only branch gated on capture mode.
    /// </summary>
    public static bool EnvironmentFlagEnabled()
    {
        return System.Environment.GetEnvironmentVariable(EnvironmentVariable) == "1";
    }

    private static bool IsEnvironmentOn() =>
        System.Environment.GetEnvironmentVariable(EnvironmentVariable) == "1";

    private static bool HasCommandLineFlag()
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (string.Equals(argument, CommandLineFlag, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns the parsed fixture name and dispatch metadata. A10
    /// leaves the per-fixture composition steps on
    /// <c>CityPrototype</c> (they touch the scene tree, which is the
    /// prototype's responsibility). The next slice moves them into
    /// <see cref="VisualFixtureCatalog"/>; the harness already parses
    /// the name and exposes the typed result so callers can switch on
    /// it.
    /// </summary>
    public static VisualFixtureResult Resolve()
    {
        if (!_isActive) return VisualFixtureResult.Skipped();
        if (_longTerrariumCapturePath is not null)
        {
            return VisualFixtureResult.LongTerrarium();
        }
        if (_fixtureName is null) return VisualFixtureResult.Skipped();
        return new VisualFixtureResult(
            Applied: false,
            Kind: ClassifyFixtureName(_fixtureName),
            FixtureName: _fixtureName);
    }

    private static VisualFixtureKind ClassifyFixtureName(string name)
    {
        if (name.StartsWith("macro", StringComparison.Ordinal))
        {
            return VisualFixtureKind.MacroComposition;
        }
        if (name.StartsWith("first-night", StringComparison.Ordinal)
            || name.StartsWith("firstnight", StringComparison.Ordinal))
        {
            return VisualFixtureKind.FirstNight;
        }
        if (name.StartsWith("hero", StringComparison.Ordinal))
        {
            return VisualFixtureKind.HeroProfile;
        }
        if (name.StartsWith("building", StringComparison.Ordinal))
        {
            return VisualFixtureKind.BuildingDetail;
        }
        if (name.StartsWith("expedition", StringComparison.Ordinal)
            || name.StartsWith("rail", StringComparison.Ordinal))
        {
            return VisualFixtureKind.ExpeditionRail;
        }
        return VisualFixtureKind.Other;
    }
}

/// <summary>
/// Outcome of <see cref="VisualRegressionHarness.Apply"/>. The
/// caller (typically <c>CityPrototype._Ready</c>) inspects this to
/// decide whether to keep the harness state alive for follow-up
/// captures or move on to normal play.
/// </summary>
public readonly record struct VisualFixtureResult(
    bool Applied,
    VisualFixtureKind Kind,
    string? FixtureName)
{
    public static VisualFixtureResult Skipped() =>
        new(false, VisualFixtureKind.None, null);

    public static VisualFixtureResult LongTerrarium() =>
        new(true, VisualFixtureKind.LongTerrarium, null);
}

/// <summary>
/// Coarse category for the fixture that ran. Used by the harness to
/// dispatch follow-up work without forcing the caller to inspect
/// strings.
/// </summary>
public enum VisualFixtureKind
{
    /// <summary>Harness inactive or no fixture requested.</summary>
    None,

    /// <summary>Long terrarium probe for headless boot tests.</summary>
    LongTerrarium,

    /// <summary>Authored macro composition screenshot.</summary>
    MacroComposition,

    /// <summary>First-night authored sequence screenshot.</summary>
    FirstNight,

    /// <summary>Hero profile screenshot.</summary>
    HeroProfile,

    /// <summary>Building detail screenshot.</summary>
    BuildingDetail,

    /// <summary>Expedition rail screenshot.</summary>
    ExpeditionRail,

    /// <summary>Generic catch-all for fixtures that do not fit a
    /// named composition.</summary>
    Other,
}
