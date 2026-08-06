using System;
using System.Collections.Generic;
using System.IO;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The lineage signature reaches the UI as a *dynamic* key —
/// UiText.Get(CubeScoring.Signature(lineage)) — so
/// tools/Test-LocalizationCatalog.ps1 cannot see it by scanning call sites. It
/// shipped untranslated for exactly that reason: the eight Spanish signature
/// words were passed as msgids that existed in no catalog, so the English build
/// rendered raw Spanish. These assertions close that blind spot.
/// </summary>
public sealed class LineageSignatureLocalizationTests
{
    [Fact]
    public void EverySignature_HasAnEntryInEveryCatalog()
    {
        string root = FindRepositoryRoot();
        foreach (string locale in new[] { "en", "es" })
        {
            HashSet<string> ids = ReadMsgIds(
                Path.Combine(root, "game", "locale", $"{locale}.po"));
            foreach (LineageId lineage in AllLineages())
            {
                string signature = CubeScoring.Signature(lineage);
                Assert.Contains(signature, ids);
            }
        }
    }

    [Fact]
    public void EnglishCatalog_TranslatesEverySignature()
    {
        Dictionary<string, string> english = ReadCatalog(
            Path.Combine(FindRepositoryRoot(), "game", "locale", "en.po"));

        foreach (LineageId lineage in AllLineages())
        {
            string signature = CubeScoring.Signature(lineage);
            Assert.True(
                english.TryGetValue(signature, out string? translated),
                $"en.po has no entry for the '{signature}' signature.");
            Assert.False(
                string.IsNullOrWhiteSpace(translated),
                $"en.po leaves the '{signature}' signature untranslated.");
            // A signature that translates to itself would render as raw Spanish
            // in the English build, which is the defect this guards.
            Assert.NotEqual(signature, translated);
        }
    }

    private static IEnumerable<LineageId> AllLineages() => new[]
    {
        LineageId.Ardhen,
        LineageId.Eirune,
        LineageId.Kovari,
        LineageId.Myrven,
        LineageId.Vaelun,
        LineageId.Orveth,
        LineageId.Caelith,
        LineageId.Theryn,
    };

    private static HashSet<string> ReadMsgIds(string path) =>
        new(ReadCatalog(path).Keys, StringComparer.Ordinal);

    /// <summary>
    /// Minimal msgid -> msgstr reader for the single-line entries these catalogs
    /// use. Good enough to assert presence and translation of a known key.
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
