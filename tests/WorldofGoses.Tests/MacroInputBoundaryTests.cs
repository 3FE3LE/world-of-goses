using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Regression tests for the macro-view input boundary. The bug these guard
/// against: a fullscreen ancestor <see cref="Control"/> with the default
/// <c>MouseFilter = Stop</c> silently swallowed hover and click events that
/// should have reached <c>MacroStreetLiveView</c>, so the citizen bubble
/// blinked open on the motion event and <c>ClearWorldStatusHover</c> hid it
/// one frame later, and left-clicks on citizens never reached
/// <c>_UnhandledInput</c>. The symptom that pointed at the cause: the
/// bubble only stayed visible when an external window sat on top of Godot,
/// because the OS stopped delivering mouse events to Godot and the
/// fullscreen Stop Control no longer claimed hover.
///
/// The fix is structural — set <c>mouse_filter = 2</c> on
/// <c>GameUiShell</c> in <c>CityPrototype.tscn</c>. Pure layout containers
/// must not claim input.
/// </summary>
public sealed class MacroInputBoundaryTests
{
    [Fact]
    public void GameUiShell_DoesNotClaimPointerInput()
    {
        string tscnPath = ResolveScenePath();
        string[] lines = File.ReadAllLines(tscnPath);

        int gameUiShellStart = IndexOfNodeHeader(lines, "GameUiShell");
        Assert.True(gameUiShellStart >= 0,
            "Could not locate the GameUiShell node header in CityPrototype.tscn.");

        int nextNodeStart = IndexOfNextNodeHeader(lines, gameUiShellStart + 1);
        int blockEnd = nextNodeStart < 0 ? lines.Length : nextNodeStart;

        bool hasMouseFilter = false;
        for (int i = gameUiShellStart; i < blockEnd; i++)
        {
            Match match = MouseFilterPattern.Match(lines[i]);
            if (!match.Success) continue;
            hasMouseFilter = true;
            string actual = match.Groups[1].Value;
            Assert.True(actual == "2",
                "GameUiShell must use mouse_filter = 2 (Ignore) so it does not "
                + "swallow hover or click events that the macro world needs to "
                + "see. Stop (0) is the default and breaks the citizen bubble "
                + "and click summary path. Found value: " + actual);
        }

        Assert.True(hasMouseFilter,
            "GameUiShell must declare mouse_filter explicitly. Relying on the "
            + "default Stop is what produced the silent input-eating bug.");
    }

    [Fact]
    public void ScreenContent_DoesNotClaimPointerInput()
    {
        string tscnPath = ResolveScenePath();
        string[] lines = File.ReadAllLines(tscnPath);

        int screenContentStart = IndexOfNodeHeader(lines, "ScreenContent");
        Assert.True(screenContentStart >= 0,
            "Could not locate the ScreenContent node header in CityPrototype.tscn.");

        int nextNodeStart = IndexOfNextNodeHeader(lines, screenContentStart + 1);
        int blockEnd = nextNodeStart < 0 ? lines.Length : nextNodeStart;

        for (int i = screenContentStart; i < blockEnd; i++)
        {
            Match match = MouseFilterPattern.Match(lines[i]);
            if (!match.Success) continue;
            string actual = match.Groups[1].Value;
            Assert.True(actual == "2",
                "ScreenContent hosts the macro world. MouseFilter = Stop would "
                + "silently eat hover and clicks over citizens and trees. "
                + "Found value: " + actual);
            return;
        }

        Assert.Fail("ScreenContent must declare mouse_filter explicitly as 2 (Ignore).");
    }

    private static string ResolveScenePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "game", "scenes", "CityPrototype.tscn");
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Could not locate CityPrototype.tscn.");
    }

    private static int IndexOfNodeHeader(string[] lines, string nodeName)
    {
        string prefix = $"[node name=\"{nodeName}\"";
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith(prefix)) return i;
        }
        return -1;
    }

    private static int IndexOfNextNodeHeader(string[] lines, int startIndex)
    {
        for (int i = startIndex; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("[node ")) return i;
        }
        return -1;
    }

    private static readonly Regex MouseFilterPattern = new(
        @"^mouse_filter\s*=\s*(\d+)\s*$",
        RegexOptions.CultureInvariant);
}