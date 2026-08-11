using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Structural architectural boundary tests for the A0 hardening slice.
///
/// Each <see cref="Fact"/> enforces one rule from
/// <c>docs/ARCHITECTURE.md</c>. Where existing code still violates a
/// rule, the corresponding property on
/// <see cref="ArchitectureBoundaryAllowlist"/> exempts the legacy debt
/// and the comment names the slice that must remove the entry.
///
/// The tests are independent from <see cref="DomainBoundaryTests"/> so
/// the A0 boundary work can move at its own pace and a future migration
/// can collapse the two files if desired.
/// </summary>
public sealed class ArchitectureBoundaryTests
{
    /// <summary>
    /// Pattern that strips the same comment shapes
    /// <see cref="DomainBoundaryTests"/> already strips: line comments
    /// (including XML doc <c>///</c>) and block comments. The pattern is
    /// reused so the two tests stay aligned if it is ever extended.
    /// </summary>
    private static readonly Regex CommentStripper = new(
        @"//.*?$|/\*.*?\*/",
        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    /// <summary>
    /// Presentation files that reach into <c>CityWorld</c> directly via
    /// <c>controller.World</c>, <c>_controller.World</c>, or their
    /// null-conditional form (<c>_controller?.World</c>). The negative
    /// lookahead excludes signal names like <c>WorldSaved</c> and
    /// <c>WorldTickAdvanced</c> (an uppercase letter immediately
    /// follows <c>World</c>).
    /// </summary>
    private static readonly Regex DirectWorldAccessPattern = new(
        @"\b(?:_?controller|Controller)\??\.World(?![A-Z])",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Presentation files that call one of the A0-era wrapper methods
    /// the A1 slice removed. Catches any new view path attempting to
    /// resurrect the entity-returning API by going through the
    /// controller. Wrapped method names appear bare after the
    /// <c>controller.</c> qualifier — only the named methods below
    /// count as policy violations. The lookahead <c>\s*\(</c>
    /// restricts the match to actual call sites (method invocation).
    /// </summary>
    private static readonly Regex DirectEntityAccessorPattern = new(
        @"\b_?controller\??\.(HeroOrNull|Citizens|CitizensByPriority|GetBuilding|PrimaryBuilding|PrimaryBuildingOrNull|AvailableCitizens|AvailableCitizensByPriority|GetProject|Projects|GetCitizen)\s*\(",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Presentation files that try to tell the domain a citizen has arrived.
    /// A2 (<c>DEC-0023</c>) made world time the only authority that ends a
    /// journey; a view that could confirm an arrival could also withhold one,
    /// which is what let a stalled animation stall production. The allowlist
    /// is empty and must stay empty.
    /// </summary>
    private static readonly Regex ArrivalConfirmationPattern = new(
        @"\bConfirmCitizenArrived\w*",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Presentation files that import or reference any type under the
    /// <c>WorldofGoses.Domain.Persistence</c> namespace.
    /// </summary>
    private static readonly Regex PersistenceReferencePattern = new(
        @"\bWorldofGoses\.Domain\.Persistence\b|using\s+WorldofGoses\.Domain\.Persistence\b|\bWorldPersistence\b|\bWorldSave\b",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Presentation public method or property return type matches one
    /// of the mutable domain entity classes. The optional
    /// <c>?</c> handles nullable annotations (<c>Citizen?</c>) and
    /// the lookahead restricts the match to a true return-type
    /// position (followed by an uppercase method/property name, an
    /// opening parenthesis, or a closing angle bracket that closes
    /// the generic instantiation). A bare identifier like
    /// <c>CityWorld world</c> in a parameter position (lowercase
    /// next token) does not match.
    /// </summary>
    private static readonly Regex PublicMutableReturnPattern = new(
        @"\bpublic\b[^\n;{}]*\b(CityWorld|Citizen|Building|Expedition|ConstructionProject|CultivationSite|ConstructionLot)\b\??(?=\s*[A-Z>(])",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Domain files that hardcode an asset extension. Catches any
    /// <c>.png</c>, <c>.tres</c>, <c>.tscn</c> or <c>.import</c> path
    /// literal that the legacy <see cref="DomainBoundaryTests"/> rule
    /// (<c>res://</c>) does not. The allowlist for this rule is
    /// intentionally empty today: the Domain tree currently has zero
    /// hardcoded asset paths and the rule is therefore globally
    /// enforceable.
    /// </summary>
    private static readonly Regex HardcodedAssetExtensionPattern = new(
        @"""[^""\n]*\.(png|tres|tscn|import)\b",
        RegexOptions.CultureInvariant);

    [Fact]
    public void Presentation_DoesNotAccessCityWorldDirectly()
    {
        IReadOnlyList<string> offending = ScanPresentationForViolations(
            DirectWorldAccessPattern,
            ArchitectureBoundaryAllowlist.PresentationDirectWorldAccess,
            nameof(ArchitectureBoundaryAllowlist.PresentationDirectWorldAccess));

        Assert.Empty(offending);
    }

    [Fact]
    public void Presentation_DoesNotCallRemovedEntityAccessorWrappers()
    {
        // The A1 slice removed these wrappers from
        // <c>CityWorldController</c>'s public API. Any new view path
        // that resurrects one is a boundary regression — fail the
        // build before it ships.
        IReadOnlyList<string> offending = ScanPresentationForViolations(
            DirectEntityAccessorPattern,
            System.Array.Empty<string>(),
            "(removed wrappers)");

        Assert.Empty(offending);
    }

    [Fact]
    public void Presentation_DoesNotConfirmCitizenArrival()
    {
        // The domain completes a journey when the world clock reaches its
        // arrival tick. Presentation draws that journey; if it could also
        // authorise the ending, an animation that never ran would be able to
        // hold a citizen in transit forever — the defect A2 removed.
        IReadOnlyList<string> offending = ScanPresentationForViolations(
            ArrivalConfirmationPattern,
            System.Array.Empty<string>(),
            "(arrival confirmation is domain-only)");

        Assert.Empty(offending);
    }

    [Fact]
    public void Presentation_DoesNotReferenceDomainPersistence()
    {
        IReadOnlyList<string> offending = ScanPresentationForViolations(
            PersistenceReferencePattern,
            ArchitectureBoundaryAllowlist.PresentationPersistenceReference,
            nameof(ArchitectureBoundaryAllowlist.PresentationPersistenceReference));

        Assert.Empty(offending);
    }

    [Fact]
    public void Presentation_DoesNotExposeMutableDomainEntities()
    {
        IReadOnlyList<string> offending = ScanPresentationForViolations(
            PublicMutableReturnPattern,
            ArchitectureBoundaryAllowlist.PresentationMutableEntityReturn,
            nameof(ArchitectureBoundaryAllowlist.PresentationMutableEntityReturn));

        Assert.Empty(offending);
    }

    [Fact]
    public void Domain_HasNoHardcodedAssetPaths()
    {
        string repositoryRoot = FindRepositoryRoot();
        string domainPath = Path.Combine(repositoryRoot, "game", "scripts", "Domain");
        List<string> offending = new();

        foreach (string file in Directory.EnumerateFiles(domainPath, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(file);
            string executableSource = CommentStripper.Replace(source, string.Empty);
            MatchCollection matches = HardcodedAssetExtensionPattern.Matches(executableSource);
            if (matches.Count == 0)
            {
                continue;
            }

            string relativePath = ToRepositoryRelative(repositoryRoot, file);
            // Allowlist is empty for this rule; any match is a regression.
            offending.Add($"{relativePath}: {matches.Count} hardcoded asset extension(s)");
        }

        Assert.Empty(offending);
    }

    /// <summary>
    /// Shared scan over every Presentation C# file (everything under
    /// <c>game/scripts/</c> outside <c>Domain/</c>). Strips comments,
    /// applies <paramref name="pattern"/>, and reports each offending
    /// file unless it sits in <paramref name="allowlist"/>. The error
    /// message names the allowlist property the next maintainer would
    /// need to extend — and the slice that owns the cleanup.
    /// </summary>
    private static IReadOnlyList<string> ScanPresentationForViolations(
        Regex pattern,
        IReadOnlyCollection<string> allowlist,
        string allowlistPropertyName)
    {
        string repositoryRoot = FindRepositoryRoot();
        string scriptsPath = Path.Combine(repositoryRoot, "game", "scripts");
        HashSet<string> allowlistSet = new(allowlist, StringComparer.Ordinal);
        List<string> offending = new();

        foreach (string file in Directory.EnumerateFiles(scriptsPath, "*.cs", SearchOption.AllDirectories))
        {
            // Domain is its own boundary (DomainBoundaryTests covers it).
            // Persistence and Combat are sub-namespaces of Domain.
            if (file.Replace('\\', '/').Contains("/Domain/", StringComparison.Ordinal))
            {
                continue;
            }

            string source = File.ReadAllText(file);
            string executableSource = CommentStripper.Replace(source, string.Empty);
            string relativePath = ToRepositoryRelative(repositoryRoot, file);

            foreach (Match match in pattern.Matches(executableSource))
            {
                if (allowlistSet.Contains(relativePath))
                {
                    continue;
                }

                offending.Add(
                    $"{relativePath}: '{match.Value}' is not in {allowlistPropertyName}; " +
                    "add the file with a '// Remove during Architecture Hardening <slice>.' comment, " +
                    "or refactor the caller.");
            }
        }

        return offending;
    }

    /// <summary>
    /// Walks upward from the test binary's base directory until it
    /// finds the repository root. Mirrors
    /// <see cref="DomainBoundaryTests.FindRepositoryRoot"/> so the two
    /// tests stay self-contained and a future cleanup can promote the
    /// helper to <c>TestHelpers</c>.
    /// </summary>
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

        throw new DirectoryNotFoundException(
            "Could not locate the repository root from the test output directory.");
    }

    private static string ToRepositoryRelative(string repositoryRoot, string file)
    {
        string normalizedRoot = repositoryRoot.Replace('\\', '/');
        string normalizedFile = file.Replace('\\', '/');
        return normalizedFile.StartsWith(normalizedRoot, StringComparison.Ordinal)
            ? normalizedFile[(normalizedRoot.Length + 1)..]
            : normalizedFile;
    }
}