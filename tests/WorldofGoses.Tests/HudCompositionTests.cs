using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Structural guards for the macro HUD's three surfaces, added when navigation
/// moved out of the full-width <c>MacroActions</c> strip on 2026-08-07.
/// </summary>
/// <remarks>
/// <para>
/// These assert the scene, not the pixels, because the properties they protect are
/// the ones a screenshot hides. A rail that silently reverts to
/// <c>mouse_filter = 2</c> still looks correct and quietly passes its clicks
/// through to the world behind it. An inspector that loses <c>grow_vertical = 0</c>
/// still renders until its text wraps to a second line.
/// </para>
/// <para>
/// They also cover surfaces no visual-regression fixture reaches, which is the gap
/// that made this pass hard to verify: <c>AssignmentPanel</c> and
/// <c>ProductionPanel</c> hide themselves for homes and the town hall, so every
/// available fixture renders neither.
/// </para>
/// </remarks>
public sealed class HudCompositionTests
{
    [Theory]
    [InlineData("NavigationRail")]
    [InlineData("ContextInspector")]
    [InlineData("ActionDock")]
    public void HudSurface_IsAuthoredInTheScene(string nodeName)
    {
        string[] lines = ReadScene();

        Assert.True(
            IndexOfNodeHeader(lines, nodeName) >= 0,
            $"{nodeName} must be authored in CityPrototype.tscn. Constructing a shared "
            + "HUD surface at runtime is what kept the selection panel invisible to the "
            + "editor and drove it to reposition itself every frame.");
    }

    [Fact]
    public void NavigationRail_ClaimsItsOwnPointerInput()
    {
        string[] block = NodeBlock("NavigationRail");

        Assert.DoesNotContain(
            block,
            line => MouseFilterPattern.Match(line) is { Success: true } m && m.Groups[1].Value == "2");
    }

    [Fact]
    public void ContextInspector_DoesNotBlockTheWorldBehindIt()
    {
        string[] block = NodeBlock("ContextInspector");

        Assert.Contains(
            block,
            line => MouseFilterPattern.Match(line) is { Success: true } m && m.Groups[1].Value == "2");
    }

    [Fact]
    public void ContextInspector_GrowsUpwardFromItsBottomAnchor()
    {
        string[] block = NodeBlock("ContextInspector");

        Assert.Contains(block, line => line.Trim() == "anchor_top = 1.0");
        Assert.Contains(block, line => line.Trim() == "anchor_bottom = 1.0");
        // GROW_DIRECTION_BEGIN. Without it the panel is pinned bottom but expands
        // downward off-screen as its detail text wraps, which is the failure the
        // per-frame reposition was compensating for.
        Assert.Contains(block, line => line.Trim() == "grow_vertical = 0");
    }

    [Fact]
    public void ActionDock_StartsHidden()
    {
        string[] block = NodeBlock("ActionDock");

        // A contextual tray, not a permanent toolbar: only a mode with an action to
        // offer may reveal it.
        Assert.Contains(block, line => line.Trim() == "visible = false");
    }

    [Fact]
    public void MacroActions_IsGone()
    {
        string[] lines = ReadScene();

        Assert.True(
            IndexOfNodeHeader(lines, "MacroActions") < 0,
            "MacroActions was the full-width strip the navigation rail replaced. "
            + "Reintroducing it costs the city 42 px of viewport height across its "
            + "whole width for seven buttons.");
    }

    private static string[] NodeBlock(string nodeName)
    {
        string[] lines = ReadScene();
        int start = IndexOfNodeHeader(lines, nodeName);
        Assert.True(start >= 0, $"Could not locate the {nodeName} node header in CityPrototype.tscn.");

        int next = IndexOfNextNodeHeader(lines, start + 1);
        int end = next < 0 ? lines.Length : next;
        return lines[start..end];
    }

    private static string[] ReadScene() => File.ReadAllLines(ResolveScenePath());

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
        return Array.FindIndex(lines, line => line.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static int IndexOfNextNodeHeader(string[] lines, int startIndex)
    {
        for (int i = startIndex; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("[node ", StringComparison.Ordinal)) return i;
        }
        return -1;
    }

    private static readonly Regex MouseFilterPattern = new(
        @"^mouse_filter\s*=\s*(\d+)\s*$",
        RegexOptions.CultureInvariant);
}
