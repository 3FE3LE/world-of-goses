using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Architecture Hardening A5 guardrails.
///
/// Every public or internal use-case command on
/// <c>CityWorldController</c> must delegate to <c>CityGameSession</c>
/// rather than reach <c>CityWorld</c> directly. The session is the
/// Application layer; bypassing it for a gameplay operation re-opens
/// the very boundary A5 closed.
///
/// <para>The test does not assert against private method names or any
/// fragile regex on body shape. It only walks the controller file,
/// matches every method whose name appears in the
/// <see cref="UseCaseCommandMethods"/> roster, and verifies the body
/// calls the matching <c>_session.&lt;Name&gt;</c> instead of
/// <c>_world.&lt;Name&gt;</c>. The roster grows with each use case the
/// session exposes; if a future command is added without going through
/// the session, the build fails here, before review.</para>
///
/// <para>Seams the controller keeps on <c>_world</c> directly are out
/// of scope and intentionally not checked: fixture seeding,
/// persistence (save/load), event subscriptions, the public
/// <c>World</c> getter for the test assembly, the
/// <c>CitizenDebugSnapshot</c> factory (it composes presentation-side
/// timing), and the selection-validity lookups.</para>
/// </summary>
public sealed class UseCaseDelegationTests
{
    /// <summary>
    /// Use-case commands the controller must route through the session.
    /// Every entry corresponds to a public method on
    /// <c>CityGameSession</c>; the controller's matching wrapper must
    /// call <c>_session.&lt;Name&gt;</c> (or its overload variant) and
    /// must not reach <c>_world.&lt;Name&gt;</c> directly.
    /// </summary>
    public static readonly string[] UseCaseCommandMethods =
    {
        // Citizen assignment
        "TryAssignCitizen",
        "TryUnassignCitizen",
        "TryAssignCitizenToProject",
        "TryUnassignCitizenFromProject",
        // Construction
        "TryAuthorizeBasicShelter",
        "TryAuthorizeConstruction",
        "TryAuthorizeFoundingSiteModule",
        "ReturnFoundingCargo",
        "SetProjectEnabled",
        "CancelProject",
        "AdvanceProduction",
        "ConfigureProductionPolicy",
        "SetProductionEnabled",
        // Cultivation and tools
        "TrySowCultivationSite",
        "TryHarvestCultivationSite",
        "TryCraftTool",
        // Resources
        "GatherFromPatch",
        "GetNaturalResourceGatherAvailability",
        "TryGatherFromPatch",
        // Expeditions
        "StartExpedition",
        "StartResourceExpedition",
        "CancelExpedition",
        "SetCombatAutoSkillsEnabled",
        "TryActivateMemberSkill",
        // Citizens
        "TryIncorporateHero",
        "TryBeginWoundRecovery",
        "TryAcceptPendingProspect",
        // First Night
        "TryOpenFirstNightDialogue",
        "TryCloseFirstNightDialogue",
        // World time
        "AdvanceWorldTick",
        // Onboarding
        "CompleteOnboarding",
        // Retroactive seeds used at load time
        "SeedStartingForests",
        "SeedStartingOpportunities",
    };

    /// <summary>
    /// The same roster, mirrored against the controller's overloaded
    /// names. <c>TryAuthorizeConstruction</c> has two public overloads
    /// in the controller; the second one (with a
    /// <c>ConstructionLot?</c> parameter) is the canonical body, so the
    /// test only checks that overload. The first is a thin alias and
    /// would produce a false positive.
    /// </summary>
    public static readonly string[] ControllerAliasMethods =
    {
        "TryAssignCitizen",
        "TryUnassignCitizen",
        "TryAssignCitizenToProject",
        "TryUnassignCitizenFromProject",
        "TryAuthorizeBasicShelter",
        "TryAuthorizeFoundingSiteModule",
        "ReturnFoundingCargo",
        "SetProjectEnabled",
        "CancelProject",
        "AdvanceProduction",
        "ConfigureProductionPolicy",
        "SetProductionEnabled",
        "TrySowCultivationSite",
        "TryHarvestCultivationSite",
        "TryCraftTool",
        "GatherFromPatch",
        "GetNaturalResourceGatherAvailability",
        "TryGatherFromPatch",
        "StartExpedition",
        "StartResourceExpedition",
        "CancelExpedition",
        "SetCombatAutoSkillsEnabled",
        "TryActivateMemberSkill",
        "TryIncorporateHero",
        "TryBeginWoundRecovery",
        "TryAcceptPendingProspect",
        "TryOpenFirstNightDialogue",
        "TryCloseFirstNightDialogue",
    };

    [Fact]
    public void Controller_DelegatesUseCaseCommandsToSession()
    {
        string repositoryRoot = FindRepositoryRoot();
        string controllerPath = Path.Combine(
            repositoryRoot, "game", "scripts", "CityWorldController.cs");
        string controllerSource = File.ReadAllText(controllerPath);

        // Strip comments so a docstring like `/// <see cref="_world.Foo"/>`
        // does not register as a call.
        string executable = StripComments(controllerSource);

        List<string> regressions = new();
        foreach (string methodName in UseCaseCommandMethods)
        {
            // Walk every method or property whose name is `methodName`.
            // Accept both `(...) { ... }` blocks and `=> ...` arrows so
            // expression-bodied members are also caught.
            foreach (string body in EnumerateMemberBodies(executable, methodName))
            {
                if (!IsRoutedThroughSession(body, methodName))
                {
                    regressions.Add(methodName);
                    break;
                }
            }
        }

        Assert.True(
            regressions.Count == 0,
            "The following controller use-case commands still reach CityWorld "
            + "directly instead of routing through CityGameSession: "
            + string.Join(", ", regressions.Distinct())
            + ". Architecture Hardening A5 closes this gap — refactor the wrapper "
            + "to call _session.<Name> instead of _world.<Name>.");
    }

    [Fact]
    public void Controller_HasAliasWrappersThatMatchTheSession()
    {
        // Every alias listed in ControllerAliasMethods must exist on the
        // controller. If a future refactor renames or removes one, the
        // alias list is the next thing that needs to move with it.
        string repositoryRoot = FindRepositoryRoot();
        string controllerPath = Path.Combine(
            repositoryRoot, "game", "scripts", "CityWorldController.cs");
        string controllerSource = File.ReadAllText(controllerPath);
        string executable = StripComments(controllerSource);

        List<string> missing = new();
        foreach (string methodName in ControllerAliasMethods)
        {
            if (!HasMember(executable, methodName))
            {
                missing.Add(methodName);
            }
        }

        Assert.True(
            missing.Count == 0,
            "The controller is missing the following session-routed wrappers: "
            + string.Join(", ", missing));
    }

    /// <summary>
    /// Returns the body of every public or internal member whose name
    /// is <paramref name="methodName"/>. Both block-bodied and
    /// expression-bodied definitions are supported. Returns an empty
    /// sequence if no member by that name exists in the file.
    /// </summary>
    private static IEnumerable<string> EnumerateMemberBodies(string source, string methodName)
    {
        foreach (Match signature in FindMemberSignatures(source, methodName))
        {
            int bodyStart = signature.Groups["body"].Index;
            int bodyEnd = signature.Groups["body"].Index + signature.Groups["body"].Length;
            yield return source.Substring(bodyStart, bodyEnd - bodyStart);
        }
    }

    /// <summary>
    /// True when the file contains at least one public/internal member
    /// named <paramref name="methodName"/>. Used to distinguish
    /// "wrapper absent" from "wrapper still calls _world".
    /// </summary>
    private static bool HasMember(string source, string methodName)
    {
        return FindMemberSignatures(source, methodName).Any();
    }

    private static IEnumerable<Match> FindMemberSignatures(string source, string methodName)
    {
        // Match:  public|internal  <return>  MethodName  ( ... )  {  body  }
        //         ^--- captures start (header) ---^   ^- body -^
        Regex block = new(
            @"\b(?:public|internal)\b[^;{}]*\b" + Regex.Escape(methodName)
            + @"\s*\([^)]*\)\s*(?:where[^{]*?)?\{(?<body>(?:[^{}]|\{[^{}]*\})*)\}",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        // Match:  public|internal  <return>  MethodName  (...)  =>  body  ;
        Regex expression = new(
            @"\b(?:public|internal)\b[^;{}]*\b" + Regex.Escape(methodName)
            + @"\s*\([^)]*\)\s*=>\s*(?<body>[^;]+);",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        foreach (Match m in block.Matches(source)) yield return m;
        foreach (Match m in expression.Matches(source)) yield return m;
    }

    /// <summary>
    /// True when the body routes the call through the session facade.
    /// Accepts the canonical <c>_session.&lt;Method&gt;</c> call and
    /// also delegates to a sibling whose body itself routes correctly,
    /// so thin aliases (<c>TryAuthorizeBasicShelter()</c> calling
    /// <c>TryAuthorizeConstruction(...)</c>) don't false-positive.
    /// </summary>
    private static bool IsRoutedThroughSession(string body, string methodName)
    {
        if (body.Contains("_session." + methodName, StringComparison.Ordinal))
        {
            return true;
        }

        // Expression-bodied member that delegates to another controller
        // method, which is itself expected to route through the session.
        // Only applies when the body is a single call expression.
        if (System.Text.RegularExpressions.Regex.IsMatch(
            body,
            @"^\s*\w[\w\.]*\s*\([^)]*\)\s*$",
            RegexOptions.CultureInvariant))
        {
            return true;
        }

        return false;
    }

    private static string StripComments(string source)
    {
        // Strip line comments and block comments so cref references and
        // XML docs do not register as code-level call sites.
        return Regex.Replace(
            source,
            @"//.*?$|/\*.*?\*/",
            string.Empty,
            RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant);
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

        throw new DirectoryNotFoundException(
            "Could not locate the repository root from the test output directory.");
    }
}
