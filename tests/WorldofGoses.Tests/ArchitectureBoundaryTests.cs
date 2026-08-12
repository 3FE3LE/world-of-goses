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
    /// persistence namespace. Architecture Hardening A6 moved persistence
    /// out of Domain into its own assembly (<c>WorldofGoses.Persistence</c>);
    /// the rule tracks both the legacy and the new namespace so an unmigrated
    /// import (the worst kind of debt) still trips the test.
    /// </summary>
    private static readonly Regex PersistenceReferencePattern = new(
        @"\bWorldofGoses\.Domain\.Persistence\b|using\s+WorldofGoses\.Domain\.Persistence\b|\bWorldofGoses\.Persistence\b|using\s+WorldofGoses\.Persistence\b|\bWorldPersistence\b|\bWorldSave\b",
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
    /// <summary>
    /// Neither engine-free assembly may gain a Godot reference. This is the
    /// belt to the compiler's braces: both are plain
    /// <c>Microsoft.NET.Sdk</c> projects, so `using Godot` in either is
    /// already a build error — but only for as long as nobody "fixes" a
    /// future compile error by adding the package back. This test names that
    /// temptation so it fails loudly instead of silently reopening the
    /// boundary the whole split exists to create.
    /// </summary>
    [Theory]
    [InlineData("WorldofGoses.Domain")]
    [InlineData("WorldofGoses.Application")]
    [InlineData("WorldofGoses.Persistence")]
    public void EngineFreeProject_DoesNotReferenceGodot(string projectName)
    {
        string projectFile = Path.Combine(
            FindRepositoryRoot(), "src", projectName, $"{projectName}.csproj");

        Assert.True(File.Exists(projectFile), $"Project not found at '{projectFile}'.");

        // XML comments are stripped first: the project file explains at length
        // why it does not reference GodotSharp, and prose about the rule must
        // not read as a breach of it.
        string project = Regex.Replace(
            File.ReadAllText(projectFile),
            @"<!--.*?-->",
            string.Empty,
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        Assert.DoesNotContain("Godot.NET.Sdk", project, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GodotSharp", project, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Microsoft.NET.Sdk", project, StringComparison.Ordinal);
    }

    /// <summary>
    /// Architecture Hardening A6 closes the persistence leak: Domain
    /// must not depend on Persistence. If a future refactor adds a
    /// <c>ProjectReference</c> from the domain assembly back into the
    /// persistence layer, the build fails here, before review.
    ///
    /// <para>The only allowed dependency direction between the two is
    /// <c>Persistence → Domain</c>. Application, by spec, also stays
    /// out of Persistence in this slice, and that rule is enforced by
    /// a sibling test below.</para>
    /// </summary>
    [Theory]
    [InlineData("WorldofGoses.Domain")]
    [InlineData("WorldofGoses.Application")]
    public void Layer_DoesNotReferencePersistenceAssembly(string projectName)
    {
        string projectFile = Path.Combine(
            FindRepositoryRoot(), "src", projectName, $"{projectName}.csproj");

        Assert.True(File.Exists(projectFile), $"Project not found at '{projectFile}'.");

        // XML comments are stripped first so prose explanations of the
        // rule don't register as breaches of it.
        string project = Regex.Replace(
            File.ReadAllText(projectFile),
            @"<!--.*?-->",
            string.Empty,
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        Assert.DoesNotContain(
            "WorldofGoses.Persistence.csproj",
            project,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Architecture Hardening A8: the Domain layer must not depend on
    /// the Application layer. Application sits above Domain in the
    /// dependency stack (use cases read domain state, not the other
    /// way around); Domain does not import types from Application. A
    /// future refactor that crosses that direction fails the build.
    /// </summary>
    [Fact]
    public void Domain_DoesNotReferenceApplicationAssembly()
    {
        string projectFile = Path.Combine(
            FindRepositoryRoot(),
            "src", "WorldofGoses.Domain", "WorldofGoses.Domain.csproj");

        Assert.True(File.Exists(projectFile), $"Project not found at '{projectFile}'.");

        string project = Regex.Replace(
            File.ReadAllText(projectFile),
            @"<!--.*?-->",
            string.Empty,
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        Assert.DoesNotContain(
            "WorldofGoses.Application.csproj",
            project,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Architecture Hardening A9: the night scene reaches the macro
    /// view through typed C# calls and a typed signal. It must not
    /// probe method names via <c>HasMethod</c> and dispatch through
    /// <c>Node.Call</c>; that seam was the dynamic-dispatch regression
    /// A9 closed. The allowlist is empty by design — there is no
    /// legitimate call site for this pattern in the night anymore.
    /// </summary>
    private static readonly Regex NightDynamicDispatchPattern = new(
        @"\bHasMethod\s*\(|\bnode\.Call\s*\(|\.HasMethod\s*\(|\.Call\s*\(\s*""[A-Za-z]",
        RegexOptions.CultureInvariant);

    [Fact]
    public void FirstNightScene_DoesNotUseDynamicDispatch()
    {
        string repositoryRoot = FindRepositoryRoot();
        string firstNightPath = Path.Combine(
            repositoryRoot, "game", "scripts", "FirstNightScene.cs");
        Assert.True(
            File.Exists(firstNightPath),
            $"FirstNightScene not found at '{firstNightPath}'.");

        string source = File.ReadAllText(firstNightPath);
        string executable = CommentStripper.Replace(source, string.Empty);

        Assert.False(
            NightDynamicDispatchPattern.IsMatch(executable),
            "FirstNightScene still uses HasMethod or Node.Call for "
            + "dynamic dispatch. Architecture Hardening A9 requires the "
            + "night to reach the macro view through typed C# methods and "
            + "the WorldDialogueAnchorsChanged signal.");
    }

    /// <summary>
    /// Architecture Hardening A9: the night scene must subscribe to
    /// <c>WorldDialogueAnchorsChanged</c> on the macro view and refresh
    /// its cached anchors through typed method calls — not poll the
    /// macro view in <c>_Process</c>. The handler presence is the
    /// contract; the polling absence is the regression guard.
    /// </summary>
    [Fact]
    public void FirstNightScene_SubscribesToTypedAnchorSignal()
    {
        string repositoryRoot = FindRepositoryRoot();
        string firstNightPath = Path.Combine(
            repositoryRoot, "game", "scripts", "FirstNightScene.cs");
        Assert.True(
            File.Exists(firstNightPath),
            $"FirstNightScene not found at '{firstNightPath}'.");

        string source = File.ReadAllText(firstNightPath);
        string executable = CommentStripper.Replace(source, string.Empty);

        Assert.Contains(
            "WorldDialogueAnchorsChanged",
            executable,
            StringComparison.Ordinal);
        Assert.Contains(
            "GetFoundingArrivalGlobalPosition",
            executable,
            StringComparison.Ordinal);
        Assert.Contains(
            "GetBuildingGlobalPosition",
            executable,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Architecture Hardening A9: the macro view must raise the typed
    /// <c>WorldDialogueAnchorsChanged</c> signal when the camera or
    /// projection changes. The previous design polled every frame;
    /// the new design emits only on real change so the night scene
    /// can refresh cached anchors from a typed event.
    /// </summary>
    [Fact]
    public void MacroStreetLiveView_ExposesTypedAnchorSignal()
    {
        string repositoryRoot = FindRepositoryRoot();
        string macroPath = Path.Combine(
            repositoryRoot,
            "game", "scripts", "Prototypes", "MacroStreetLiveView.cs");
        Assert.True(
            File.Exists(macroPath),
            $"MacroStreetLiveView not found at '{macroPath}'.");

        string source = File.ReadAllText(macroPath);
        string executable = CommentStripper.Replace(source, string.Empty);

        Assert.Contains(
            "[Signal]",
            executable,
            StringComparison.Ordinal);
        Assert.Contains(
            "WorldDialogueAnchorsChangedEventHandler",
            executable,
            StringComparison.Ordinal);
        Assert.Contains(
            "EmitSignal(SignalName.WorldDialogueAnchorsChanged",
            executable,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Architecture Hardening A10: the visual-regression harness lives
    /// under <c>game/scripts/Testing/</c>. Production runtime classes
    /// do not own fixture orchestration; they ask the harness whether
    /// capture mode is active and let the harness drive the
    /// dispatch.
    /// </summary>
    [Fact]
    public void VisualRegressionHarness_LivesUnderTestingNamespace()
    {
        string repositoryRoot = FindRepositoryRoot();
        string harnessPath = Path.Combine(
            repositoryRoot, "game", "scripts", "Testing", "VisualRegressionHarness.cs");
        string catalogPath = Path.Combine(
            repositoryRoot, "game", "scripts", "Testing", "VisualFixtureCatalog.cs");

        Assert.True(
            File.Exists(harnessPath),
            $"VisualRegressionHarness not found at '{harnessPath}'.");
        Assert.True(
            File.Exists(catalogPath),
            $"VisualFixtureCatalog not found at '{catalogPath}'.");
    }

    /// <summary>
    /// Architecture Hardening A10: the Domain does not expose fixture
    /// seams as <c>public</c> members anymore. The two methods that
    /// used to be <c>public</c> so a screenshot could author the state
    /// it wanted are now <c>internal</c>; production scenes cannot
    /// grow a new screenshot path through them. Any future seam that
    /// has to remain visible across the assembly boundary must go in
    /// <see cref="ArchitectureBoundaryAllowlist.DomainFixtureSeamAllowlist"/>.
    /// </summary>
    private static readonly Regex PublicDomainFixtureSeamPattern = new(
        @"\bpublic\s+(?:void|[A-Z]\w*)\s+\w*[Ff]or[Ff]ixture(?:s)?\s*\(",
        RegexOptions.CultureInvariant);

    [Fact]
    public void Domain_DoesNotExposeFixtureSeamsAsPublic()
    {
        string repositoryRoot = FindRepositoryRoot();
        string domainPath = Path.Combine(repositoryRoot, "game", "scripts", "Domain");

        foreach (string file in Directory.EnumerateFiles(
            domainPath, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(file);
            string executable = CommentStripper.Replace(source, string.Empty);

            Assert.False(
                PublicDomainFixtureSeamPattern.IsMatch(executable),
                $"Domain source '{file}' exposes a public *ForFixture method. "
                + "Architecture Hardening A10 closed this seam; only the test "
                + "assembly and the visual-regression harness reach it.");
        }
    }

    /// <summary>
    /// Architecture Hardening A10: CityWorldController does not grow
    /// new public methods to enable screenshots. The visual-regression
    /// entry points that used to live as <c>public</c> members
    /// (<c>DrainAllForestsForVisualRegression</c>,
    /// <c>AdvanceWorldTickForVisualRegression</c>) are gone; the
    /// fixture seam is <c>internal</c> and only the harness calls
    /// it. A future regression that adds another
    /// <c>ForVisualRegression</c> public method fails this test.
    /// </summary>
    private static readonly Regex ControllerPublicVisualRegressionPattern = new(
        @"\bpublic\s+(?:void|[A-Z]\w*)\s+\w*[Ff]or[Vv]isual[Rr]egression\s*\(",
        RegexOptions.CultureInvariant);

    [Fact]
    public void CityWorldController_DoesNotGrowPublicVisualRegressionMethods()
    {
        string repositoryRoot = FindRepositoryRoot();
        string controllerPath = Path.Combine(
            repositoryRoot, "game", "scripts", "CityWorldController.cs");
        Assert.True(
            File.Exists(controllerPath),
            $"Controller not found at '{controllerPath}'.");

        string source = File.ReadAllText(controllerPath);
        string executable = CommentStripper.Replace(source, string.Empty);

        Assert.False(
            ControllerPublicVisualRegressionPattern.IsMatch(executable),
            "CityWorldController still exposes a public *ForVisualRegression "
            + "method. Architecture Hardening A10 closes that seam: the "
            + "harness reaches the same operations through internal methods "
            + "gated on VisualRegressionHarness.IsActive.");
    }

    /// <summary>
    /// Architecture Hardening A11: production UI panels must not
    /// compose their static hierarchy in C#. A panel whose shape,
    /// anchors and container layout do not depend on runtime data is a
    /// <c>.tscn</c>; runtime C# owns behaviour, state binding, and
    /// the rows that the snapshot drives. The allowlist is the
    /// canonical list of panels that A11 documented as "B" (genuinely
    /// dynamic), "D" (dev tooling) or "E" (runtime-only visual
    /// object); every other production screen lives in
    /// <see cref="ArchitectureBoundaryAllowlist.ProductionUiMigratedToTscn"/>
    /// once migrated. New panels default to <c>.tscn</c> and use the
    /// C# only for dynamic rows.
    /// </summary>
    private static readonly Regex PublicVisualRegressionMethodPattern = new(
        @"\bpublic\s+(?:void|[A-Z]\w*)\s+\w*[Ff]or[Vv]isual[Rr]egression\s*\(",
        RegexOptions.CultureInvariant);

    [Fact]
    public void Production_DoesNotExposePublicVisualRegressionMethods()
    {
        // A10 closed the controller seam; A12 closes the seam on every
        // other public surface. Visual-regression entry points must be
        // `internal` and gated on VisualRegressionHarness.IsActive so
        // production scenes cannot grow a new screenshot path through
        // them.
        string repositoryRoot = FindRepositoryRoot();
        string scriptsPath = Path.Combine(repositoryRoot, "game", "scripts");
        List<string> offending = new();

        foreach (string file in Directory.EnumerateFiles(scriptsPath, "*.cs", SearchOption.AllDirectories))
        {
            // Domain/Application/Testing are not production UI.
            if (file.Replace('\\', '/').Contains("/Domain/", StringComparison.Ordinal)) continue;
            if (file.Replace('\\', '/').Contains("/Application/", StringComparison.Ordinal)) continue;
            if (file.Replace('\\', '/').Contains("/Testing/", StringComparison.Ordinal)) continue;

            string source = File.ReadAllText(file);
            string executable = CommentStripper.Replace(source, string.Empty);
            string relativePath = ToRepositoryRelative(repositoryRoot, file);

            foreach (Match match in PublicVisualRegressionMethodPattern.Matches(executable))
            {
                offending.Add(
                    $"{relativePath}: '{match.Value}' exposes a public "
                    + "*ForVisualRegression method. A10/A12 closed this seam: "
                    + "the entry point must be `internal` and gated on "
                    + "VisualRegressionHarness.IsActive.");
            }
        }

        Assert.Empty(offending);
    }
    private static readonly Regex ProductionUiStaticStructurePattern = new(
        @"\bnew\s+(?:Panel|Label|Button|Container|HBox|VBox|Margin|TextureRect|PanelContainer|Separator|HSeparator|VSeparator|GridContainer|TabBar|TabContainer|Tab|ScrollContainer|CenterContainer|PanelContainer|MarginContainer|HSplitContainer|VSplitContainer)\s*\(",
        RegexOptions.CultureInvariant);

    [Fact]
    public void ProductionUi_DoesNotComposeStaticHierarchyInCode()
    {
        // Production screens under game/scripts/ (outside the Ui/ folder of
        // reusable primitives) must not compose their static hierarchy in
        // C#. A future screen that reaches for `new VBoxContainer { ... }`
        // for its top-level layout fails this test.
        IReadOnlyList<string> offending = ScanProductionUiForViolations(
            ProductionUiStaticStructurePattern,
            ArchitectureBoundaryAllowlist.ProductionUiStaticStructureInCode,
            nameof(ArchitectureBoundaryAllowlist.ProductionUiStaticStructureInCode));

        Assert.Empty(offending);
    }

    private static IReadOnlyList<string> ScanProductionUiForViolations(
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
            // Domain, Application, Persistence are engine-free layers; not UI.
            if (file.Replace('\\', '/').Contains("/Domain/", StringComparison.Ordinal)) continue;
            if (file.Replace('\\', '/').Contains("/Application/", StringComparison.Ordinal)) continue;
            if (file.Replace('\\', '/').Contains("/Testing/", StringComparison.Ordinal)) continue;

            // Ui/ primitives intentionally compose controls — they ARE
            // the reusable building blocks (Buttons, Panels, Chips).
            if (file.Replace('\\', '/').Contains("/Ui/", StringComparison.Ordinal)) continue;

            // Prototypes/ are reference compositions, not production UI.
            if (file.Replace('\\', '/').Contains("/Prototypes/", StringComparison.Ordinal)) continue;

            string source = File.ReadAllText(file);
            string executable = CommentStripper.Replace(source, string.Empty);
            string relativePath = ToRepositoryRelative(repositoryRoot, file);

            foreach (Match match in pattern.Matches(executable))
            {
                if (allowlistSet.Contains(relativePath))
                {
                    continue;
                }
                offending.Add(
                    $"{relativePath}: '{match.Value}' builds a static UI "
                    + $"hierarchy in C#. Architecture Hardening A11 routes "
                    + $"this through a .tscn; the script owns behaviour and "
                    + $"dynamic rows only. See {allowlistPropertyName} for the "
                    + $"panels A11 classified as B/D/E and the migration order.");
            }
        }

        return offending;
    }

    /// <summary>
    /// Architecture Hardening A8: Presentation never instantiates
    /// <see cref="CityWorld"/> directly. The aggregate is owned by
    /// <see cref="WorldofGoses.CityGameSession"/>, and a view that
    /// built its own <c>CityWorld</c> would create a parallel world
    /// outside the session's reach. The narrow fixture seam reaches
    /// the session's owned world through
    /// <c>controller.GetFixtureWorld()</c>; building a fresh aggregate
    /// from Presentation is the regression this guard catches.
    /// </summary>
    private static readonly Regex PresentationInstantiatesWorldPattern = new(
        @"\bnew\s+CityWorld\s*\(",
        RegexOptions.CultureInvariant);

    [Fact]
    public void Presentation_DoesNotInstantiateCityWorld()
    {
        // The visual-regression fixture seam
        // (<c>CityPrototype</c>) authors fresh <c>CityWorld</c>
        // aggregates to compose screenshot scenarios. Production
        // presentation code never builds a world of its own — the
        // session owns the only one. The allowlist is one entry by
        // design.
        IReadOnlyList<string> offending = ScanPresentationForViolations(
            PresentationInstantiatesWorldPattern,
            ArchitectureBoundaryAllowlist.PresentationInstantiatesWorld,
            nameof(ArchitectureBoundaryAllowlist.PresentationInstantiatesWorld));

        Assert.Empty(offending);
    }

    /// <summary>
    /// Architecture Hardening A8: Presentation never calls a public
    /// mutator on a domain aggregate or entity. Production views read
    /// through snapshot projections or use-case commands on the
    /// session; the only allowed direct entity mutation is the visual
    /// regression fixture seam, gated by the controller's
    /// <c>internal</c> fixture methods. A view that called
    /// <c>citizen.SustainWound(...)</c> directly would be a regression
    /// even though the method compiles cleanly.
    /// </summary>
    private static readonly Regex PresentationEntityMutatorPattern = new(
        @"\b_?controller\??\.World\b"
        + @"|\b(?:[Cc]itizen|[Bb]uilding|[Cc]ity[Ww]orld|[Ee]xpedition|[Cc]onstructionProject|[Cc]ultivationSite)"
        + @"\.[A-Z][A-Za-z0-9_]*\s*\(",
        RegexOptions.CultureInvariant);

    [Fact]
    public void Presentation_DoesNotMutateAggregatesOrEntities()
    {
        // The visual-regression fixture seam reaches the session's
        // owned world through narrow controller commands. Any other
        // presentation code that mutates an aggregate or entity
        // directly is a regression; no allowlist entries are expected.
        // The controller itself is excluded — its fixture commands are
        // the documented seam. This test scans scene code only.
        IReadOnlyList<string> offending = ScanPresentationScenesForViolations(
            PresentationEntityMutatorPattern,
            ArchitectureBoundaryAllowlist.PresentationEntityMutator,
            nameof(ArchitectureBoundaryAllowlist.PresentationEntityMutator));

        Assert.Empty(offending);
    }

    /// <summary>
    /// Architecture Hardening A8: <see cref="WorldofGoses.CityWorldController"/>
    /// no longer carries a <c>_world</c> field. A future refactor that
    /// re-introduces one — bypassing the session seam — would break the
    /// ownership rule that the session owns the aggregate. The guard
    /// scans the controller source for the field declaration so any
    /// regression fails the build before review.
    /// </summary>
    [Fact]
    public void CityWorldController_DoesNotHoldACityWorldField()
    {
        string controllerPath = Path.Combine(
            FindRepositoryRoot(),
            "game", "scripts", "CityWorldController.cs");
        Assert.True(
            File.Exists(controllerPath),
            $"Controller not found at '{controllerPath}'.");

        string source = File.ReadAllText(controllerPath);
        string executable = CommentStripper.Replace(source, string.Empty);

        Assert.False(
            Regex.IsMatch(
                executable,
                @"\b(?:private|protected|internal|public)\s+readonly\s+CityWorld\s+\w+",
                RegexOptions.CultureInvariant),
            "CityWorldController still holds a CityWorld field; "
            + "Architecture Hardening A8 moved ownership to CityGameSession.");
        Assert.False(
            Regex.IsMatch(
                executable,
                @"\b(?:private|protected|internal)\s+CityWorld\s+_?world\b",
                RegexOptions.CultureInvariant),
            "CityWorldController still has a CityWorld instance field named '_world'; "
            + "Architecture Hardening A8 moved ownership to CityGameSession.");
    }

    /// <summary>
    /// Architecture Hardening A8: <see cref="WorldofGoses.CityWorldController"/>
    /// no longer exposes a public or internal <c>World</c> getter on
    /// itself. The session owns the aggregate and exposes the narrow
    /// <c>internal CityWorld GetFixtureWorld()</c> reach for the
    /// visual-regression fixture seam, but presentation never reads
    /// the controller for a <c>World</c> reference for gameplay. The
    /// session's own <c>internal CityWorld World</c> is exempt because
    /// it is the documented fixture seam.
    /// </summary>
    private static readonly Regex ControllerWorldGetterPattern = new(
        @"\b(?:public|internal|private)\s+(?:readonly\s+)?CityWorld\s+World\s*[{=>]",
        RegexOptions.CultureInvariant);

    [Fact]
    public void CityWorldController_DoesNotExposeWorldGetter()
    {
        string controllerPath = Path.Combine(
            FindRepositoryRoot(),
            "game", "scripts", "CityWorldController.cs");
        Assert.True(
            File.Exists(controllerPath),
            $"Controller not found at '{controllerPath}'.");

        string source = File.ReadAllText(controllerPath);
        string executable = CommentStripper.Replace(source, string.Empty);

        // Allow `internal CityWorld GetFixtureWorld()` — the narrow
        // fixture seam documented by A8 — but never a plain `World`
        // property. The lookahead `{=>` covers both the `{ get {` and
        // `=> _session.World;` shapes.
        Assert.False(
            ControllerWorldGetterPattern.IsMatch(executable),
            "CityWorldController still exposes a World getter (the legacy "
            + "'internal CityWorld World' from before A8). The session owns "
            + "the aggregate; presentation reaches it through the narrow "
            + "GetFixtureWorld() fixture seam, not through a controller property.");
    }

    /// <summary>
    /// Only the visual-regression fixture builders may end the first night by
    /// decree. The sequence is the game's opening; skipping it in real play
    /// would start a city in a state the player never lived through.
    ///
    /// <para>This rule used to be expressed as <c>internal</c> on
    /// <c>CityWorld.ConcludeFirstNightForFixtures</c>. That stopped meaning
    /// anything when the domain became its own assembly and the fixture
    /// builders — the legitimate callers — ended up outside it, so the rule
    /// moved here, where the call sites can actually be pinned.</para>
    /// </summary>
    [Fact]
    public void Presentation_ConcludesFirstNightOnlyInFixtures()
    {
        IReadOnlyList<string> offending = ScanPresentationForViolations(
            new Regex(@"\bConcludeFirstNightForFixtures\s*\(", RegexOptions.CultureInvariant),
            ArchitectureBoundaryAllowlist.PresentationFirstNightFixtureSeam,
            nameof(ArchitectureBoundaryAllowlist.PresentationFirstNightFixtureSeam));

        Assert.Empty(offending);
    }

    /// <summary>
    /// A translation lookup whose msgid is produced by calling
    /// <c>ToString()</c> on a value. Architecture Hardening A12 introduced
    /// <see cref="WorldofGoses.Ui.ResourceTypeLocalizer"/> to stop this, but
    /// left the rule to a convention; the final exit gate found two live
    /// call sites still doing it, one of which
    /// (<c>UiText.Get(supplyResource.Value.ToString())</c>) asked
    /// <c>en.po</c> for a msgid that does not exist and rendered the raw
    /// enum name into the dispatch error.
    ///
    /// <para>The defect class is not "a resource label is wrong". It is
    /// that a C# rename silently changes a PO key, so the failure appears
    /// in shipped UI in one language and nowhere in the test suite. The
    /// replacement contract is an explicit, exhaustive mapper per value
    /// family (<c>ResourceTypeLocalizer</c>, <c>GenderIdLocalizer</c>).</para>
    ///
    /// <para>Scoped to <c>UiText.Get</c>/<c>UiText.Format</c> so ordinary
    /// <c>ToString()</c> use — debug text, path building, numeric
    /// formatting — is untouched.</para>
    /// </summary>
    private static readonly Regex TranslationKeyFromValueNamePattern = new(
        @"\bUiText\.(?:Get|Format)\(\s*[A-Za-z_][A-Za-z0-9_.?]*\.ToString\(\)",
        RegexOptions.CultureInvariant);

    [Fact]
    public void Ui_DoesNotDeriveTranslationKeysFromValueNames()
    {
        IReadOnlyList<string> offending = ScanPresentationForViolations(
            TranslationKeyFromValueNamePattern,
            Array.Empty<string>(),
            "(no allowlist — route the value through an explicit localizer)");

        Assert.Empty(offending);
    }

    /// <summary>
    /// A canonical UI input action named by its raw engine string rather
    /// than through <see cref="WorldofGoses.Ui.UiInputActions"/>.
    ///
    /// <para>A12 created the constants and the doc comment on
    /// <c>UiInputActions</c> already promised this guard by name; the guard
    /// itself was never written, and production code kept accumulating
    /// literal <c>"ui_cancel"</c>. One misspelling in a string literal is a
    /// silently dead keybinding — the engine has no idea the action was
    /// meant to exist.</para>
    ///
    /// <para>Deliberately scoped to the canonical <c>ui_*</c> family that
    /// <c>UiInputActions</c> owns. Gameplay actions belonging to other input
    /// systems (camera pan, macro navigation) are a different contract and
    /// are not banned here.</para>
    /// </summary>
    private static readonly Regex HardcodedUiInputActionPattern = new(
        @"""ui_(?:cancel|accept|left|right|up|down|text_completion|text_newline)""",
        RegexOptions.CultureInvariant);

    [Fact]
    public void Ui_DoesNotHardcodeInputActionStrings()
    {
        string repositoryRoot = FindRepositoryRoot();
        string definitionPath =
            Path.Combine(repositoryRoot, "game", "scripts", "Ui", "UiInputActions.cs")
                .Replace('\\', '/');

        IReadOnlyList<string> offending = ScanPresentationForViolations(
            HardcodedUiInputActionPattern,
            // The definition itself is the one place the literals belong;
            // that is not an allowlisted violation, it is the contract.
            new[] { ToRepositoryRelative(repositoryRoot, definitionPath) },
            nameof(WorldofGoses.Ui.UiInputActions));

        Assert.Empty(offending);
    }

    private static IReadOnlyList<string> ScanPresentationForViolations(
        Regex pattern,
        IReadOnlyCollection<string> allowlist,
        string allowlistPropertyName)
    {
        return ScanPresentationFiles(pattern, allowlist, allowlistPropertyName, includeController: true);
    }

    /// <summary>
    /// Same scan as <see cref="ScanPresentationForViolations"/> but
    /// skips <c>CityWorldController.cs</c>. Architecture Hardening A8
    /// moved entity-mutation freedom into the controller's
    /// <c>internal</c> fixture methods; the controller is the
    /// documented seam, not a regression. Scene code outside the
    /// controller still has to go through the session.
    /// </summary>
    private static IReadOnlyList<string> ScanPresentationScenesForViolations(
        Regex pattern,
        IReadOnlyCollection<string> allowlist,
        string allowlistPropertyName)
    {
        return ScanPresentationFiles(pattern, allowlist, allowlistPropertyName, includeController: false);
    }

    private static IReadOnlyList<string> ScanPresentationFiles(
        Regex pattern,
        IReadOnlyCollection<string> allowlist,
        string allowlistPropertyName,
        bool includeController)
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

            // Application is its own assembly too; its files compile
            // through the WorldofGoses.Application project rather than
            // the Godot presentation project. Boundary tests for the
            // application assembly live on the layer rules above.
            if (file.Replace('\\', '/').Contains("/Application/", StringComparison.Ordinal))
            {
                continue;
            }

            if (!includeController
                && file.Replace('\\', '/').EndsWith("/CityWorldController.cs", StringComparison.Ordinal))
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