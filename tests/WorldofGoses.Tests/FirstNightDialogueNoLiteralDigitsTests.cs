using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The first-night catalogue delivers body text from the .po files; the
/// catalogue itself carries the ids only, never the displayed strings.
/// Quantities (the Campfire needs branches and stone, the Bedroll needs
/// branches and fibre) are interpolated at runtime from
/// <c>FoundingSiteRules.InputsFor</c>, so a recipe change cannot leave
/// the night describing a world that no longer exists. This guard
/// asserts the catalogue's promise: nothing under the
/// <c>firstnight.*</c> prefix may contain a literal digit, because
/// every visible number must come from the recipe or be absent.
///
/// <para>
/// It mirrors <see cref="LineageSignatureLocalizationTests"/>'s
/// shape: a minimal .po parser that does not depend on Godot, so the
/// test stays runnable from xUnit and catches the same kind of
/// defect the existing <c>tools/Test-LocalizationCatalog.ps1</c>
/// validator cannot see (computed/composed keys never appear in its
/// regex pass).
/// </para>
/// </summary>
public sealed class FirstNightDialogueNoLiteralDigitsTests
{
    [Fact]
    public void NoFirstNightKey_ContainsALiteralDigit_InEnglishCatalog()
    {
        var offending = new List<string>();
        foreach (KeyValuePair<string, string> entry in ReadFirstNightEntries("en.po"))
        {
            if (ContainsLiteralDigit(entry.Key)) offending.Add($"msgid='{entry.Key}'");
            if (ContainsLiteralDigit(entry.Value)) offending.Add($"msgstr='{entry.Value}' (key={entry.Key})");
        }
        Assert.True(
            offending.Count == 0,
            "Found literal digits in firstnight.* entries of en.po:\n  " + string.Join("\n  ", offending));
    }

    [Fact]
    public void NoFirstNightKey_ContainsALiteralDigit_InSpanishCatalog()
    {
        var offending = new List<string>();
        foreach (KeyValuePair<string, string> entry in ReadFirstNightEntries("es.po"))
        {
            if (ContainsLiteralDigit(entry.Key)) offending.Add($"msgid='{entry.Key}'");
            if (ContainsLiteralDigit(entry.Value)) offending.Add($"msgstr='{entry.Value}' (key={entry.Key})");
        }
        Assert.True(
            offending.Count == 0,
            "Found literal digits in firstnight.* entries of es.po:\n  " + string.Join("\n  ", offending));
    }

    [Fact]
    public void FirstNightKeysHaveNonEmptyTranslations_BothLocales()
    {
        // Defensive: even when digits are absent, an empty msgstr would
        // render the key itself in the UI — the same defect the lineage
        // signature test guards. The first night has at least the strip
        // button labels today, so the catalogue is not empty; if it ever
        // becomes empty, this test still passes (vacuously true).
        foreach (string locale in new[] { "en", "es" })
        {
            foreach (KeyValuePair<string, string> entry in ReadFirstNightEntries($"{locale}.po"))
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(entry.Value),
                    $"{locale}.po has no translation for firstnight key '{entry.Key}'.");
            }
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> ReadFirstNightEntries(string fileName)
    {
        Dictionary<string, string> all = ReadCatalog(
            Path.Combine(FindRepositoryRoot(), "game", "locale", fileName));
        foreach (KeyValuePair<string, string> entry in all)
        {
            if (entry.Key.StartsWith("firstnight.", StringComparison.Ordinal))
            {
                yield return entry;
            }
        }
    }

    private static bool ContainsLiteralDigit(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        // Strip placeholder tokens of the shape {0}, {1}, ... before checking,
        // so a body like "uses {0} branches and {1} stones" is allowed.
        string stripped = Regex.Replace(value, @"\{\d+\}", string.Empty);
        return Regex.IsMatch(stripped, @"\d");
    }

    /// <summary>
    /// Minimal msgid → msgstr reader for the single-line entries these
    /// catalogues use. Mirrors <see cref="LineageSignatureLocalizationTests"/>.
    /// </summary>
    private static Dictionary<string, string> ReadCatalog(string path)
    {
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        string? pendingId = null;
        foreach (string line in File.ReadAllLines(path))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("msgid \"", StringComparison.Ordinal))
            {
                pendingId = Unquote(trimmed, "msgid ");
            }
            else if (trimmed.StartsWith("msgstr \"", StringComparison.Ordinal)
                && pendingId is not null)
            {
                entries[pendingId] = Unquote(trimmed, "msgstr ");
                pendingId = null;
            }
        }
        return entries;
    }

    private static string Unquote(string line, string prefix)
    {
        string value = line[prefix.Length..].Trim();
        return value.Length >= 2 && value[0] == '"' && value[^1] == '"'
            ? value[1..^1]
            : value;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "game", "locale")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the repository root from the test output directory.");
    }
}
