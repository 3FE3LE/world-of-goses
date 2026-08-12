using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The A12 resource-i18n contract, closed by the final exit gate
/// (GitHub #4).
///
/// <para><see cref="ResourceTypeLocalizer"/> was introduced to stop
/// Presentation deriving PO keys from <c>ResourceType</c> member names,
/// but it kept a <c>_ =&gt; resource.ToString().ToLowerInvariant()</c>
/// fallback arm, which reinstated the coupling for exactly the case that
/// matters: a resource nobody remembered to map. The fallback compiled,
/// ran, and shipped an untranslated enum name into the UI.</para>
///
/// <para>These tests are the replacement. They enumerate the domain enum
/// rather than a hand-written list, so a new <c>ResourceType</c> fails
/// here the moment it is declared — before it can reach a player — and
/// they check the key against the real catalogs on disk, so a mapping
/// that points at a msgid nobody wrote fails too.</para>
/// </summary>
public sealed class ResourceTypeLocalizationContractTests
{
    private static readonly string[] Catalogs = { "en", "es" };

    [Fact]
    public void EveryResourceType_HasAnExplicitLocalizationKey()
    {
        var unmapped = new List<ResourceType>();
        foreach (ResourceType resource in Enum.GetValues<ResourceType>())
        {
            try
            {
                string key = ResourceTypeLocalizer.Key(resource);
                Assert.False(
                    string.IsNullOrWhiteSpace(key),
                    $"{resource} maps to a blank localisation key.");
            }
            catch (ArgumentOutOfRangeException)
            {
                unmapped.Add(resource);
            }
        }

        Assert.True(
            unmapped.Count == 0,
            "ResourceTypeLocalizer.Key has no arm for: "
                + string.Join(", ", unmapped)
                + ". Add the arm and the msgid to every game/locale catalog.");
    }

    [Fact]
    public void ResourceLocalizationKeys_AreNotDerivedFromEnumMemberNames()
    {
        // The keys are allowed to *coincide* with the lowercased member
        // name — "stone" is a perfectly good msgid. What must not exist is
        // a value whose key is produced BY the member name, because that is
        // the coupling that turns a C# rename into a silent translation
        // regression. The proof is that every key is reachable from the
        // explicit switch: if the switch is exhaustive (asserted above),
        // no code path can derive one.
        //
        // This assertion pins the complementary half: the mapping is
        // injective, so two resources can never quietly collapse onto one
        // label the way a naive derivation would if the enum ever grew a
        // member differing only by case.
        var byKey = new Dictionary<string, ResourceType>(StringComparer.Ordinal);
        foreach (ResourceType resource in Enum.GetValues<ResourceType>())
        {
            string key = ResourceTypeLocalizer.Key(resource);
            Assert.False(
                byKey.TryGetValue(key, out ResourceType collision),
                $"{resource} and {collision} share the localisation key '{key}'.");
            byKey[key] = resource;
        }
    }

    [Fact]
    public void EveryResourceLocalizationKey_ExistsInEveryCatalog()
    {
        string root = FindRepositoryRoot();
        foreach (string locale in Catalogs)
        {
            Dictionary<string, string> catalog =
                ReadCatalog(Path.Combine(root, "game", "locale", $"{locale}.po"));

            foreach (ResourceType resource in Enum.GetValues<ResourceType>())
            {
                string key = ResourceTypeLocalizer.Key(resource);
                Assert.True(
                    catalog.ContainsKey(key),
                    $"{locale}.po has no msgid '{key}' for ResourceType.{resource}.");
                Assert.False(
                    string.IsNullOrWhiteSpace(catalog[key]),
                    $"{locale}.po leaves '{key}' (ResourceType.{resource}) untranslated.");
            }
        }
    }

    [Fact]
    public void EveryGenderId_HasAnExplicitLocalizationKeyPresentInEveryCatalog()
    {
        // GenderIdLocalizer is the sibling contract: the hero profile used
        // to call UiText.Get(hero.Gender.ToString()), which worked only
        // because the msgids happened to match the C# member names.
        string root = FindRepositoryRoot();
        foreach (string locale in Catalogs)
        {
            Dictionary<string, string> catalog =
                ReadCatalog(Path.Combine(root, "game", "locale", $"{locale}.po"));

            foreach (GenderId gender in Enum.GetValues<GenderId>())
            {
                string key = GenderIdLocalizer.Key(gender);
                Assert.True(
                    catalog.ContainsKey(key),
                    $"{locale}.po has no msgid '{key}' for GenderId.{gender}.");
                Assert.False(
                    string.IsNullOrWhiteSpace(catalog[key]),
                    $"{locale}.po leaves '{key}' (GenderId.{gender}) untranslated.");
            }
        }
    }

    /// <summary>
    /// Minimal msgid -&gt; msgstr reader for the single-line entries these
    /// catalogs use. Mirrors <see cref="LineageSignatureLocalizationTests"/>;
    /// good enough to assert presence and translation of a known key.
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
