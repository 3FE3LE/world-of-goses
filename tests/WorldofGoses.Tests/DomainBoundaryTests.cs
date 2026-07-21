using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class DomainBoundaryTests
{
    [Fact]
    public void DomainSources_DoNotReferenceGodotOrResourcePaths()
    {
        string repositoryRoot = FindRepositoryRoot();
        string domainPath = Path.Combine(repositoryRoot, "game", "scripts", "Domain");

        foreach (string file in Directory.EnumerateFiles(domainPath, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(file);
            string executableSource = Regex.Replace(
                source,
                @"//.*?$|/\*.*?\*/",
                string.Empty,
                RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant);
            Assert.False(
                Regex.IsMatch(executableSource, @"\bGodot\b", RegexOptions.CultureInvariant),
                $"Domain source '{file}' references the Godot API.");
            Assert.DoesNotContain("res://", source, StringComparison.Ordinal);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, "game", "scripts", "Domain")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the test output directory.");
    }
}
