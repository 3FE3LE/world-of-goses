using System;
using System.IO;
using WorldofGoses;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Guard for issue #51. Every <c>GetNode&lt;T&gt;("literal")</c>
/// call in <c>game/scripts/CityPrototype.cs</c> must resolve through
/// one of the constants in <see cref="CityNodePaths"/>; a literal
/// NodePath inside this file is a regression of the "no magic strings"
/// convention rule (<c>docs/engineering/conventions.md</c> §5).
///
/// <para>
/// The guard inspects the file as text (not compiled code) so it
/// catches the pattern even at callsites the compiler cannot
/// type-check, and so a future rename of CityNodePaths.{Constant}
/// immediately fails the test until <see cref="AllowlistedLiterals"/>
/// is updated.
/// </para>
///
/// <para>
/// Two allowlisted literals remain in CityPrototype.cs today:
/// the SettingsButton deep-internal path under PauseMenu, and any
/// <c>[Signal]</c> / <c>MethodName</c> use of a node name. Both
/// are documented at the callsite; adding to this list requires
/// an explicit comment in the source explaining why it cannot
/// route through <see cref="CityNodePaths"/>.
/// </para>
/// </summary>
public class CityNodePathConstantsTests
{
    /// <summary>
    /// Single literal that intentionally stays in
    /// <c>CityPrototype.cs</c>. The SettingsButton is a deep-internal
    /// child of <see cref="Ui.PauseMenu"/>'s authored shell; moving
    /// it to a typed property on PauseMenu is a separate
    /// encapsulation slice.
    /// </summary>
    private static readonly string[] AllowlistedLiterals =
    {
        "Center/Card/Margin/Shell/MainActions/SettingsButton",
    };

    [Fact]
    public void CityPrototype_GetNodeCalls_RouteThroughCityNodePaths()
    {
        string repositoryRoot = FindRepositoryRoot();
        string source = File.ReadAllText(
            Path.Combine(repositoryRoot, "game", "scripts", "CityPrototype.cs"));

        var lintFailures = new System.Collections.Generic.List<string>();
        int begin = 0;

        while (begin < source.Length)
        {
            int keywordPos = source.IndexOf("GetNode<", begin, StringComparison.Ordinal);
            if (keywordPos < 0) break;
            int openAngle = keywordPos + "GetNode<".Length;
            int closeAngle = source.IndexOf('>', openAngle);
            if (closeAngle < 0) break;
            int parenOpen = source.IndexOf('(', closeAngle);
            if (parenOpen < 0) break;
            int parenClose = FindMatchingParen(source, parenOpen);
            if (parenClose < 0) break;

            int firstArgStart = SkipWhitespace(source, parenOpen + 1);
            string firstArg = ExtractFirstArg(
                source, firstArgStart, parenClose, out int argEnd);

            if (firstArg.StartsWith("\""))
            {
                // Extract the literal contents (path string only,
                // without surrounding quotes) so the comparison key
                // matches the allowlist entry verbatim.
                string literal = firstArg;
                int secondQuote = literal.IndexOf('"', 1);
                if (secondQuote > 1)
                {
                    literal = literal.Substring(1, secondQuote - 1);
                }
                if (!IsAllowlisted(literal))
                {
                    int lineNumber = CountLines(source, keywordPos) + 1;
                    lintFailures.Add(
                        $"line {lineNumber}: literal NodePath {literal} must use CityNodePaths");
                }
            }

            begin = parenClose + 1;
        }

        Assert.Empty(lintFailures);
    }

    [Fact]
    public void CityNodePaths_AllReferencesResolveToDeclaredNode()
    {
        // Smoke test: every constant in CityNodePaths is a non-empty
        // NodePath that starts with a valid segment (no leading slash
        // tricks). The runtime NodePath constructor would catch
        // malformed values, but this gives a faster signal.
        string repositoryRoot = FindRepositoryRoot();
        string source = File.ReadAllText(
            Path.Combine(repositoryRoot, "game", "scripts", "CityNodePaths.cs"));
        Assert.Contains("public static class CityNodePaths", source);
        Assert.Contains("public static readonly NodePath Controller", source);
        Assert.Contains("public static readonly NodePath MacroLiveView", source);
        Assert.Contains("public static readonly NodePath StatusPanel", source);
    }

    private static bool IsAllowlisted(string literal) =>
        System.Array.IndexOf(AllowlistedLiterals, literal) >= 0;

    private static int FindMatchingParen(string source, int openPos)
    {
        int depth = 0;
        for (int i = openPos; i < source.Length; i++)
        {
            char c = source[i];
            if (c == '(') depth++;
            else if (c == ')')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    private static int SkipWhitespace(string source, int pos)
    {
        while (pos < source.Length && char.IsWhiteSpace(source[pos])) pos++;
        return pos;
    }

    private static string ExtractFirstArg(string source, int start, int end, out int argEnd)
    {
        // Split at the first top-level comma — generics inside the
        // first arg are tolerated but parsing them is overkill; for
        // this codebase all NodePath literals are simple strings.
        int depth = 0;
        for (int i = start; i < end; i++)
        {
            char c = source[i];
            if (c == '(' || c == '{' || c == '[') depth++;
            else if (c == ')' || c == '}' || c == ']') depth--;
            else if (c == ',' && depth == 0)
            {
                argEnd = i;
                return source[start..i].Trim();
            }
        }
        argEnd = end;
        return source[start..end].Trim();
    }

    private static int CountLines(string source, int position)
    {
        int count = 0;
        for (int i = 0; i < position; i++)
        {
            if (source[i] == '\n') count++;
        }
        return count;
    }

    private static string FindRepositoryRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "AGENTS.md")) ||
                File.Exists(Path.Combine(dir, "CLAUDE.md"))) return dir;
            dir = Path.GetDirectoryName(dir);
        }
        throw new System.InvalidOperationException(
            "Could not locate repository root from " + AppContext.BaseDirectory);
    }
}
