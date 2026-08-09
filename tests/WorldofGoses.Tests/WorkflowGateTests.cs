using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Tests for the deterministic workflow gates introduced by the
/// agent-workflow refactor. See
/// <c>tools/Get-VerificationPlan.ps1</c> for the canonical
/// PowerShell implementation; this class mirrors its classification
/// rules so they can be exercised in a CI-friendly xUnit run.
///
/// Drift between the script and this test is intentional evidence of
/// a gate change — both files must move together.
/// </summary>
public sealed class WorkflowGateTests
{
    // ---------------------------------------------------------------------
    // Path classification (mirrors Get-VerificationPlan.ps1 §2).
    // ---------------------------------------------------------------------

    /// <summary>
    /// PowerShell -like semantics on forward slashes: `*` matches any
    /// run of characters except `/`; `**` matches any run including `/`.
    /// </summary>
    private static bool LikeMatch(string path, string pattern)
    {
        // Convert PowerShell glob to a regex anchored at start and end.
        var rx = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*\\*", "::DOUBLESTAR::")
            .Replace("\\*", "[^/]*")
            .Replace("::DOUBLESTAR::", ".*") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(
            path.Replace('\\', '/'), rx,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static bool PathMatches(string path, params string[] patterns)
    {
        return patterns.Any(p => LikeMatch(path, p));
    }

    private static bool AnyPathMatches(IEnumerable<string> paths, params string[] patterns)
    {
        return paths.Any(p => PathMatches(p, patterns));
    }

    // ---------------------------------------------------------------------
    // Risk + Mode + Review derivation (mirrors Get-VerificationPlan.ps1 §3-§6).
    // ---------------------------------------------------------------------

    public enum Risk { Low, Medium, High }
    public enum Mode { Surgical, Feature, Release }
    public enum ReviewDepth { None, PresentationReview, DomainReview, SystemReview }

    public sealed record PlanResult(
        Risk Risk,
        Mode Mode,
        ReviewDepth Review,
        bool RequiresAgentValidation,
        bool RequiresLocalizationValidation,
        bool RequiresVisualCapture,
        bool RequiresFullSnapshot,
        IReadOnlyList<string> Reasons);

    public static PlanResult Classify(IEnumerable<string> changedPaths)
    {
        var paths = changedPaths.ToList();
        var reasons = new List<string>();

        var saveSchema = AnyPathMatches(paths,
            "game/scripts/Domain/**/WorldSave.cs",
            "game/scripts/Domain/*Save.cs",
            "game/scripts/Domain/**/*Save.cs");

        var persistence = AnyPathMatches(paths,
            "game/scripts/Domain/**/WorldPersistence.cs",
            "game/scripts/Domain/**/WorldPersistence/**",
            "game/scripts/Domain/**/WorldMigration*");

        var dependency = AnyPathMatches(paths,
            "game/*.csproj",
            "game/*.sln",
            "game/project.godot");

        var domainLayer = AnyPathMatches(paths, "game/scripts/Domain/");

        var architecture = AnyPathMatches(paths,
            "docs/ARCHITECTURE.md",
            "docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE.md");

        var subtreesHit = new List<string>();
        foreach (var sub in new[] { "Citizen", "City", "Expedition" })
        {
            if (AnyPathMatches(paths, $"game/scripts/Domain/{sub}/**"))
            {
                subtreesHit.Add(sub);
            }
        }

        // Risk derivation.
        Risk? risk = null;
        if (saveSchema)
        {
            risk = Risk.High;
            reasons.Add("save schema / migration touched");
        }
        if (persistence && risk != Risk.High)
        {
            risk = Risk.High;
            reasons.Add("persistence code touched");
        }
        if (dependency && risk != Risk.High)
        {
            risk = Risk.High;
            reasons.Add("project / dependency files touched");
        }
        if (architecture && risk != Risk.High)
        {
            risk = Risk.High;
            reasons.Add("architecture / boundary doc touched");
        }
        if (subtreesHit.Count >= 2)
        {
            risk = Risk.High;
            reasons.Add($"multiple Domain subtrees touched: {string.Join(", ", subtreesHit)}");
        }
        else if (subtreesHit.Count == 1 && risk != Risk.High)
        {
            risk = Risk.Medium;
            reasons.Add($"single Domain subtree touched ({subtreesHit[0]})");
        }
        else if (domainLayer && subtreesHit.Count == 0 && risk != Risk.High)
        {
            risk = Risk.High;
            reasons.Add("root-level Domain file touched");
        }
        if (risk == null)
        {
            if (AnyPathMatches(paths, "game/scripts/Ui/**", "game/scenes/**", "game/scripts/visual/**", "art/**"))
            {
                risk = Risk.Medium;
                reasons.Add("UI / asset surface touched");
            }
            else if (AnyPathMatches(paths, "tests/**"))
            {
                risk = Risk.Low;
                reasons.Add("test-only change");
            }
            else if (AnyPathMatches(paths, "*.po", "*.pot", "game/locale/**"))
            {
                risk = Risk.Medium;
                reasons.Add("localization touched");
            }
            else if (AnyPathMatches(paths,
                         ".agents/**", ".claude/**", ".codex/**",
                         "AGENTS.md", "CLAUDE.md",
                         "docs/ai/**", "scripts/**", "tools/**",
                         "Install-GodotDotNetSkills.ps1"))
            {
                risk = Risk.Medium;
                reasons.Add("agent / tooling layer touched");
            }
            else if (AnyPathMatches(paths, "docs/**"))
            {
                risk = Risk.Low;
                reasons.Add("docs-only change");
            }
            else
            {
                risk = Risk.Low;
                reasons.Add("default to LOW");
            }
        }

        // Mode derivation.
        Mode mode;
        switch (risk)
        {
            case Risk.High:
                mode = Mode.Release;
                break;
            case Risk.Medium:
                var singleFileUi = paths.Count <= 1 && AnyPathMatches(paths, "game/scripts/Ui/**", "game/scenes/**", "game/scripts/visual/**");
                mode = singleFileUi ? Mode.Surgical : Mode.Feature;
                break;
            default:
                mode = Mode.Surgical;
                break;
        }

        // Escalation: schema / dependency always RELEASE.
        if ((saveSchema || dependency) && mode != Mode.Release)
        {
            mode = Mode.Release;
            reasons.Add("escalated: schema/dependency always RELEASE");
        }

        // Review derivation.
        ReviewDepth review = mode switch
        {
            Mode.Surgical => ReviewDepth.None,
            Mode.Feature when AnyPathMatches(paths, "game/scripts/Ui/**", "game/scenes/**", "game/scripts/visual/**")
                            && !domainLayer && !saveSchema
                => ReviewDepth.PresentationReview,
            Mode.Feature => ReviewDepth.DomainReview,
            Mode.Release => ReviewDepth.SystemReview,
            _ => ReviewDepth.None,
        };

        // Validator requirements.
        var requiresAgentValidation = AnyPathMatches(paths,
            ".agents/**", ".claude/**", ".codex/**",
            "AGENTS.md", "CLAUDE.md",
            "docs/ai/**", "scripts/**", "tools/**",
            "Install-GodotDotNetSkills.ps1");

        var requiresLocalizationValidation = AnyPathMatches(paths,
            "*.po", "*.pot", "game/locale/**");

        var requiresVisualCapture = AnyPathMatches(paths,
            "game/scripts/Ui/**", "game/scenes/**", "game/scripts/visual/**", "art/**");

        var requiresFullSnapshot = mode == Mode.Release;

        return new PlanResult(
            risk.Value, mode, review,
            requiresAgentValidation,
            requiresLocalizationValidation,
            requiresVisualCapture,
            requiresFullSnapshot,
            reasons);
    }

    // ---------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------

    [Fact]
    public void UiOnlyDiff_DoesNotRequireAgentValidation()
    {
        var plan = Classify(new[] { "game/scripts/Ui/HudResourceRow.cs" });
        Assert.False(plan.RequiresAgentValidation);
    }

    [Fact]
    public void LocaleDiff_RequiresLocalizationValidation()
    {
        var plan = Classify(new[] { "game/locale/en.po", "game/locale/es.po" });
        Assert.True(plan.RequiresLocalizationValidation);
    }

    [Fact]
    public void AgentDiff_RequiresAgentValidation()
    {
        var plan = Classify(new[]
        {
            ".agents/skills/presentation-experience/SKILL.md",
            "docs/ai/CONTEXT_MAP.md",
        });
        Assert.True(plan.RequiresAgentValidation);
        Assert.Equal(Risk.Medium, plan.Risk);
    }

    [Fact]
    public void DomainPersistenceDiff_EscalatesToHigh()
    {
        var plan = Classify(new[] { "game/scripts/Domain/Persistence/WorldPersistence.cs" });
        Assert.Equal(Risk.High, plan.Risk);
        Assert.Equal(Mode.Release, plan.Mode);
    }

    [Fact]
    public void SaveSchemaDiff_ChoosesRelease()
    {
        var plan = Classify(new[] { "game/scripts/Domain/Persistence/WorldSave.cs" });
        Assert.Equal(Risk.High, plan.Risk);
        Assert.Equal(Mode.Release, plan.Mode);
        Assert.Equal(ReviewDepth.SystemReview, plan.Review);
    }

    [Fact]
    public void SmallUiDiff_ChoosesSurgical()
    {
        var plan = Classify(new[] { "game/scripts/Ui/HudResourceRow.cs" });
        Assert.Equal(Mode.Surgical, plan.Mode);
        Assert.Equal(ReviewDepth.None, plan.Review);
        Assert.False(plan.RequiresFullSnapshot);
    }

    [Fact]
    public void BroadHudDiff_ChoosesFeature()
    {
        var plan = Classify(new[]
        {
            "game/scripts/Ui/HudResourceRow.cs",
            "game/scripts/Ui/PrimaryNavDock.cs",
            "game/scripts/Ui/ChroniclePanel.cs",
            "game/scenes/CityPrototype.tscn",
        });
        Assert.Equal(Risk.Medium, plan.Risk);
        Assert.Equal(Mode.Feature, plan.Mode);
        Assert.Equal(ReviewDepth.PresentationReview, plan.Review);
    }

    [Fact]
    public void NoChanges_ReturnsLowRisk()
    {
        var plan = Classify(Array.Empty<string>());
        Assert.Equal(Risk.Low, plan.Risk);
        Assert.Equal(Mode.Surgical, plan.Mode);
    }

    [Fact]
    public void ArchitectureDoc_EscalatesToHigh()
    {
        var plan = Classify(new[] { "docs/ARCHITECTURE.md" });
        Assert.Equal(Risk.High, plan.Risk);
        Assert.Equal(Mode.Release, plan.Mode);
    }

    [Fact]
    public void DependencyFile_EscalatesToHigh()
    {
        var plan = Classify(new[] { "game/World of Goses.csproj" });
        Assert.Equal(Risk.High, plan.Risk);
        Assert.Equal(Mode.Release, plan.Mode);
    }

    [Fact]
    public void TestOnlyChange_IsSurgical()
    {
        var plan = Classify(new[] { "tests/WorldofGoses.Tests/HudCompositionTests.cs" });
        Assert.Equal(Risk.Low, plan.Risk);
        Assert.Equal(Mode.Surgical, plan.Mode);
        Assert.False(plan.RequiresVisualCapture);
    }

    [Fact]
    public void MultipleDomainSubtrees_EscalateToHigh()
    {
        var plan = Classify(new[]
        {
            "game/scripts/Domain/Citizen/Citizen.cs",
            "game/scripts/Domain/City/Building.cs",
        });
        Assert.Equal(Risk.High, plan.Risk);
        Assert.Equal(Mode.Release, plan.Mode);
    }

    [Fact]
    public void SingleDomainSubtree_IsFeatureNotRelease()
    {
        var plan = Classify(new[] { "game/scripts/Domain/Citizen/Citizen.cs" });
        Assert.Equal(Risk.Medium, plan.Risk);
        Assert.Equal(Mode.Feature, plan.Mode);
    }

    [Fact]
    public void NewUiDiff_TriggersVisualCapture()
    {
        var plan = Classify(new[]
        {
            "game/scripts/Ui/HudResourceRow.cs",
            "game/scripts/Ui/PrimaryNavDock.cs",
            "game/scripts/Ui/ChroniclePanel.cs",
        });
        Assert.True(plan.RequiresVisualCapture);
        Assert.Equal(Mode.Feature, plan.Mode);
    }
}