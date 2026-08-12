using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Structural guard for the A11 panel migration (GitHub #9). Every panel whose
/// static hierarchy moved into a <c>.tscn</c> now depends on two facts a
/// compiler cannot see: that the parent scene actually <em>instances</em> the
/// component scene, and that every node path the script resolves exists in it.
///
/// <para>
/// This is the exact failure that reverted the first attempt. A component
/// scene was authored while <c>CityPrototype.tscn</c> still declared a bare
/// <c>PanelContainer</c> with the script attached, so at runtime the panel had
/// no children, <c>GetNode("Layout/Header")</c> threw, and
/// <c>Test-GodotBoot.ps1</c> failed with "Node not found". Nothing in the
/// build or the test suite said a word: the C# compiled, and the scene parsed.
/// </para>
///
/// <para>
/// A boot test catches it too, but only after launching the engine, and only
/// for the panels a boot happens to reach. This reads the two text files and
/// says which node is missing.
/// </para>
/// </summary>
public sealed class PanelSceneMigrationTests
{
    /// <summary>
    /// Panels migrated out of the allowlist so far: the script, the component
    /// scene that owns its hierarchy, and the node in the parent scene that
    /// must instance it. Add a row here as each migration lands — a panel
    /// leaving <c>ProductionUiStaticStructureInCode</c> without one is a
    /// migration nothing is holding in place.
    /// </summary>
    public static IEnumerable<object[]> MigratedPanels =>
    [
        ["game/scripts/CitySummaryPanel.cs", "game/scenes/Components/CitySummaryPanel.tscn", "CitySummaryPanel"],
        ["game/scripts/PoliciesPanel.cs", "game/scenes/Components/PoliciesPanel.tscn", "PoliciesPanel"],
        ["game/scripts/ExpeditionLiveView.cs", "game/scenes/expeditions/ExpeditionLiveView.tscn", "ExpeditionLiveView"],
    ];

    /// <summary>
    /// Panels whose hierarchy is authored inside <c>CityPrototype.tscn</c>
    /// itself rather than in a component scene of their own. They are just as
    /// migrated — the shape is in a <c>.tscn</c> and the script only binds —
    /// but there is no separate scene for
    /// <see cref="ParentSceneInstancesTheComponentScene"/> to check, so what
    /// holds them is that every path they resolve exists in the parent scene.
    /// </summary>
    public static IEnumerable<object[]> PanelsAuthoredInTheParentScene =>
    [
        ["game/scripts/BuildingDetailView.cs", "BuildingDetailView"],
        ["game/scripts/ProductionPanel.cs", "ProductionPanel"],
    ];

    [Theory]
    [MemberData(nameof(PanelsAuthoredInTheParentScene))]
    public void ScriptResolvesOnlyNodesTheParentSceneDeclares(string scriptPath, string nodeName)
    {
        string root = TestHelpers.FindRepositoryRoot();
        string source = File.ReadAllText(
            Path.Combine(root, scriptPath.Replace('/', Path.DirectorySeparatorChar)));
        IReadOnlySet<string> authored = NodePaths(
            Path.Combine(root, "game", "scenes", "CityPrototype.tscn"));

        // Paths are relative to the panel's own node inside the parent scene.
        string prefix = authored.Single(path =>
            path.EndsWith($"/{nodeName}", StringComparison.Ordinal)
            || path == nodeName);

        var missing = new List<string>();
        foreach (string path in ResolvedPaths(source))
        {
            if (!authored.Contains($"{prefix}/{path}")) missing.Add(path);
        }

        Assert.True(
            missing.Count == 0,
            $"{scriptPath} resolves {string.Join(", ", missing)} under {prefix}, "
            + "which CityPrototype.tscn does not declare.");
    }

    /// <summary>
    /// Relative node paths a script resolves, from both
    /// <c>GetNode&lt;T&gt;("…")</c> and the <c>const string</c> path fields
    /// panels use when the same subtree is addressed from several methods.
    /// Absolute paths and paths climbing out of the panel are skipped.
    /// </summary>
    private static IEnumerable<string> ResolvedPaths(string source)
    {
        foreach (Match match in GetNodePattern.Matches(source))
        {
            string path = match.Groups["path"].Value;
            if (path.StartsWith('/') || path.StartsWith("..", StringComparison.Ordinal)) continue;
            yield return path;
        }
        foreach (Match match in PathConstantPattern.Matches(source))
        {
            yield return match.Groups["path"].Value;
        }
    }

    private static readonly Regex PathConstantPattern = new(
        @"private const string \w*Path = ""(?<path>[^""/][^""]*)""",
        RegexOptions.CultureInvariant);

    [Theory]
    [MemberData(nameof(MigratedPanels))]
    public void ParentSceneInstancesTheComponentScene(
        string scriptPath,
        string scenePath,
        string nodeName)
    {
        _ = scriptPath;
        string root = TestHelpers.FindRepositoryRoot();
        string[] parent = File.ReadAllLines(
            Path.Combine(root, "game", "scenes", "CityPrototype.tscn"));

        string resourcePath = "res://" + scenePath["game/".Length..];
        string? extResource = parent.FirstOrDefault(line =>
            line.StartsWith("[ext_resource ", StringComparison.Ordinal)
            && line.Contains("type=\"PackedScene\"", StringComparison.Ordinal)
            && line.Contains($"path=\"{resourcePath}\"", StringComparison.Ordinal));
        Assert.True(
            extResource is not null,
            $"CityPrototype.tscn must declare {resourcePath} as a PackedScene ext_resource. "
            + "Authoring the component scene without referencing it leaves the panel empty at runtime.");

        Match id = Regex.Match(extResource!, @"id=""(?<id>[^""]+)""");
        Assert.True(id.Success, $"The ext_resource line for {resourcePath} has no id.");

        string? header = parent.FirstOrDefault(line =>
            line.StartsWith($"[node name=\"{nodeName}\"", StringComparison.Ordinal));
        Assert.True(header is not null, $"CityPrototype.tscn has no {nodeName} node.");
        Assert.Contains(
            $"instance=ExtResource(\"{id.Groups["id"].Value}\")",
            header!,
            StringComparison.Ordinal);
        Assert.DoesNotContain("type=\"PanelContainer\"", header!, StringComparison.Ordinal);
    }

    /// <summary>
    /// Every <c>GetNode&lt;T&gt;("relative/path")</c> in the migrated script
    /// must name a node the component scene actually declares. Absolute paths
    /// (<c>/root/…</c>) and paths that climb out of the panel (<c>../…</c>)
    /// address the wider tree and are out of this scene's reach, so they are
    /// skipped rather than mis-reported.
    /// </summary>
    [Theory]
    [MemberData(nameof(MigratedPanels))]
    public void ScriptResolvesOnlyNodesTheComponentSceneDeclares(
        string scriptPath,
        string scenePath,
        string nodeName)
    {
        _ = nodeName;
        string root = TestHelpers.FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(root, scriptPath.Replace('/', Path.DirectorySeparatorChar)));
        IReadOnlySet<string> authored = NodePaths(
            Path.Combine(root, scenePath.Replace('/', Path.DirectorySeparatorChar)));

        var missing = new List<string>();
        foreach (Match match in GetNodePattern.Matches(source))
        {
            string path = match.Groups["path"].Value;
            if (path.StartsWith('/') || path.StartsWith("..", StringComparison.Ordinal)) continue;
            if (!authored.Contains(path)) missing.Add(path);
        }

        Assert.True(
            missing.Count == 0,
            $"{scriptPath} resolves {string.Join(", ", missing)}, which {scenePath} does not declare. "
            + $"Authored nodes: {string.Join(", ", authored.OrderBy(item => item, StringComparer.Ordinal))}.");
    }

    private static readonly Regex GetNodePattern = new(
        @"GetNode(?:OrNull)?<[^>]+>\(\s*""(?<path>[^""]+)""\s*\)",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Every node path a scene declares, relative to its root. The root itself
    /// carries no <c>parent</c> attribute; a child names its parent as "." or
    /// as a path already relative to the root.
    /// </summary>
    private static IReadOnlySet<string> NodePaths(string scenePath)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(scenePath))
        {
            if (!line.StartsWith("[node ", StringComparison.Ordinal)) continue;
            Match name = Regex.Match(line, @"name=""(?<name>[^""]+)""");
            if (!name.Success) continue;
            Match parent = Regex.Match(line, @"parent=""(?<parent>[^""]*)""");
            if (!parent.Success) continue; // the scene root
            string parentPath = parent.Groups["parent"].Value;
            paths.Add(parentPath is "." or ""
                ? name.Groups["name"].Value
                : $"{parentPath}/{name.Groups["name"].Value}");
        }
        return paths;
    }
}
