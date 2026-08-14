using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Guards the repository's documentation rule: <b>canonical documentation does
/// not keep actionable future work. That work belongs to GitHub Issues.</b>
///
/// <para>
/// The canonical implementation is <c>scripts/docs/classify.ps1</c> check 4,
/// which also owns the per-document exemptions in
/// <c>scripts/docs/classification.json</c>. This class mirrors its patterns so
/// the rule runs in the ordinary xUnit pass too — the same arrangement
/// <see cref="WorkflowGateTests"/> uses for the verification planner. Drift
/// between the two files is intentional evidence that the rule changed; both
/// must move together.
/// </para>
/// <para>
/// The patterns are deliberately narrow. They match *operational* shapes — a
/// heading that opens a queue, or a row that assigns work — not the ordinary
/// words "phase" or "pending" inside an explanation. A document may say "the
/// encounter resolves in two phases"; it may not open a section called
/// "Pendientes".
/// </para>
/// </summary>
public sealed class DocumentationBacklogGuardTests
{
    private static readonly Regex BacklogHeading = new(
        @"^\s{0,3}#{1,6}\s*(?:\d+[.)]\s*)?(?:" +
        @"pendientes?|to\s*do|todo|next\s+steps?|next\s+work|" +
        @"pr[oó]ximos?\s+pasos?|siguiente\s+entrega|" +
        @"backlog|roadmap|" +
        @"(?:fases?|phases?)\s+de\s+implementaci[oó]n|implementation\s+phases?|" +
        @"trabajo\s+(?:pendiente|futuro)|future\s+work|" +
        @"work\s+remaining|remaining\s+work" +
        @")\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex[] BacklogRows =
    {
        new(@"^\s*[-*]\s+\[ \]\s+\S", RegexOptions.CultureInvariant),
        new(@"^\s*\|\s*(?:Estado|Status)\s*\|.*\|\s*(?:Prioridad|Priority)\s*\|",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^\s*\*\*(?:Estado|Status)\s*:\*\*\s*(?:Pendiente|Pending|En curso|In progress|Bloqueado|Blocked|Diferido|Deferred)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"^\s*\*\*(?:Pendiente|Pending|Next steps?|Pr[oó]ximos pasos)\s*[:.]?\*\*",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
    };

    /// <summary>
    /// Paths whose job is to describe work that was once pending, plus the
    /// guard's own sources. Kept in sync with <c>classify.ps1</c>.
    /// </summary>
    private static readonly string[] ExemptPrefixes =
    {
        "CHANGELOG.md",
        "docs/history/",
        "docs/session-state/",
        "scripts/docs/",
    };

    /// <summary>
    /// Forms whose empty boxes are the point. Mirrors the
    /// <c>backlog_exempt</c> flag in the classification ledger.
    /// </summary>
    private static readonly string[] ExemptDocuments =
    {
        "docs/ai/FEATURE_HANDOFF_TEMPLATE.md",
    };

    [Fact]
    public void NoCanonicalDocumentKeepsAnActionableBacklog()
    {
        string root = FindRepositoryRoot();
        var offenders = new List<string>();

        foreach (string file in CanonicalDocuments(root))
        {
            string relative = ToRepositoryRelative(root, file);
            string[] lines = File.ReadAllLines(file);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (BacklogHeading.IsMatch(line))
                {
                    offenders.Add($"{relative}:{i + 1}: heading opens a queue -> {line.Trim()}");
                    continue;
                }

                if (BacklogRows.Any(p => p.IsMatch(line)))
                {
                    offenders.Add($"{relative}:{i + 1}: backlog row -> {line.Trim()}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Canonical documentation does not keep actionable future work; that belongs to "
            + "GitHub Issues. Offending lines:" + Environment.NewLine
            + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>
    /// The guard is worthless if its patterns match nothing, so this asserts
    /// they still fire on the shapes the rule bans.
    /// </summary>
    [Fact]
    public void TheGuardStillRecognisesTheShapesItBans()
    {
        Assert.Matches(BacklogHeading, "## Pendientes");
        Assert.Matches(BacklogHeading, "## 4. Próximos pasos");
        Assert.Matches(BacklogHeading, "### Next steps");
        Assert.Matches(BacklogHeading, "## Fases de implementación");
        Assert.Matches(BacklogHeading, "# Roadmap");
        Assert.Matches(BacklogRows[0], "- [ ] wire the audio buses");
        Assert.Matches(BacklogRows[2], "**Estado:** Pendiente");
    }

    /// <summary>
    /// And it is worse than worthless if it fires on ordinary prose, because
    /// the next author's fix is to delete a true sentence.
    /// </summary>
    [Fact]
    public void TheGuardIgnoresOrdinaryProse()
    {
        Assert.DoesNotMatch(BacklogHeading, "## Fases del encuentro");
        Assert.DoesNotMatch(BacklogHeading, "## Construction phases");
        Assert.DoesNotMatch(BacklogHeading, "El tratamiento sigue pendiente de una cama.");
        Assert.DoesNotMatch(BacklogRows[0], "- [x] already covered");
        Assert.DoesNotMatch(BacklogRows[1], "| Estado | Evidencia |");
    }

    private static IEnumerable<string> CanonicalDocuments(string root)
    {
        string docs = Path.Combine(root, "docs");
        var files = new List<string>(Directory.EnumerateFiles(docs, "*.md", SearchOption.AllDirectories));
        files.AddRange(Directory.EnumerateFiles(root, "*.md", SearchOption.TopDirectoryOnly));

        foreach (string file in files)
        {
            string relative = ToRepositoryRelative(root, file);
            if (ExemptPrefixes.Any(p => relative.StartsWith(p, StringComparison.Ordinal))) { continue; }
            if (ExemptDocuments.Contains(relative)) { continue; }
            yield return file;
        }
    }

    /// <summary>
    /// Walks upward from the test binary's base directory until it finds the
    /// repository root. Mirrors the helper in
    /// <see cref="ArchitectureBoundaryTests"/>.
    /// </summary>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, "docs")))
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
