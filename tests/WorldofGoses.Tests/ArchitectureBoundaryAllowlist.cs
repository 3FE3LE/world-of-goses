using System.Collections.Generic;

namespace WorldofGoses.Tests;

/// <summary>
/// Allowlist of files that currently violate one of the architectural
/// boundary rules enforced by <see cref="ArchitectureBoundaryTests"/>.
///
/// Every entry in this file is **legacy debt** that a future Architecture
/// Hardening slice must delete. Adding a new entry requires reviewer
/// sign-off; the comments name the slice that owns its removal.
/// </summary>
public static class ArchitectureBoundaryAllowlist
{
    /// <summary>
    /// Files under Presentation that access <c>CityWorld</c> directly
    /// via <c>controller.World</c>. The aggregate facade must only be
    /// reached through <c>CityWorldController</c>'s command methods
    /// and snapshot projections. A1 closed every production-scene entry
    /// in this list except three that the Plan agent flagged as real
    /// entity-binding requirements or fixture-only seam (renamed to A3
    /// fixtures — see the inline comments).
    /// </summary>
    /// <remarks>
    /// <para><strong>Empty, and that is the finding.</strong> This list
    /// carried three entries — <c>AstralOnboardingView</c>,
    /// <c>CombatDebugPanel</c> and <c>CityPrototype</c> — each with a comment
    /// explaining why it could not be closed yet. A8–A12 closed all three by
    /// routing the reads through the session and the narrow
    /// <c>GetFixtureWorld()</c> seam, but nobody deleted the entries, so the
    /// A0–A12 report still counted three exemptions the code no longer used.
    /// The final exit gate verified every entry against the guard's own
    /// pattern and found zero matches in all three files.</para>
    ///
    /// <para>The property stays (rather than the guard losing its allowlist
    /// parameter) so a future exemption has an obvious, reviewed place to
    /// go. Adding one back means writing down why presentation must hold the
    /// aggregate — which, for gameplay, it must not.</para>
    /// </remarks>
    public static IReadOnlyCollection<string> PresentationDirectWorldAccess { get; } =
        System.Array.Empty<string>();

    /// <summary>
    /// Files under Presentation that import or reference types in
    /// <c>WorldofGoses.Domain.Persistence</c>. The boundary rule
    /// (AGENTS.md §3 / docs/ARCHITECTURE.md §5) keeps persistence types
    /// behind the controller so the snapshot pipeline stays
    /// presentation-owned.
    /// Remove during Architecture Hardening A3.
    /// </summary>
    public static IReadOnlyCollection<string> PresentationPersistenceReference { get; } =
        new[]
        {
            // game/scripts/CityWorldController.cs — the boundary class
            // owns the save/load seam (autosave, slot reset, slot import).
            // The next slice must extract WorldPersistence calls into a
            // dedicated persistence adapter behind the controller. Remove
            // during Architecture Hardening A3.
            "game/scripts/CityWorldController.cs",

            // game/scripts/CityPrototype.cs — the prototype scene drives
            // the dev-only load and restore fixture flow. Once the
            // persistence adapter lands, CityPrototype should call the
            // adapter, not WorldPersistence directly. Remove during
            // Architecture Hardening A3.
            "game/scripts/CityPrototype.cs",

            // game/scripts/Ui/LocaleManager.cs — reads the save-slot path
            // to decide where to write its own settings file. Move into
            // the persistence adapter. Remove during Architecture
            // Hardening A3.
            "game/scripts/Ui/LocaleManager.cs",

            // game/scripts/Prototypes/RealCityStreetPreview.cs — dev-only
            // preview that loads a real save to render the macro layout.
            // Collapse to a controller command once the snapshot pipeline
            // can produce the same preview. Remove during Architecture
            // Hardening A3.
            "game/scripts/Prototypes/RealCityStreetPreview.cs",
        };

    /// <summary>
    /// Files under Presentation whose public API surface exposes
    /// mutable Domain entities as return types. Parameter positions
    /// (e.g. <c>From(CityWorld world)</c> on snapshot factories) are
    /// NOT covered here — they are the documented consumption seam in
    /// <c>docs/ARCHITECTURE.md §5</c>. The replacement contract is
    /// read-only snapshots.
    /// Remove during Architecture Hardening A3.
    /// </summary>
    /// <summary>
    /// The only files allowed to conclude the first night by decree. Both are
    /// visual-regression fixture builders: they fabricate a mid-game city for
    /// a screenshot and must not sit through the opening sequence to do it.
    /// This allowlist is not legacy debt to be paid down — it is the rule
    /// itself, and it should stay exactly this short.
    /// </summary>
    public static IReadOnlyCollection<string> PresentationFirstNightFixtureSeam { get; } =
        new[]
        {
            "game/scripts/CityPrototype.cs",
        };

    public static IReadOnlyCollection<string> PresentationMutableEntityReturn { get; } =
        new[]
        {
            // game/scripts/IconPaths.cs — `public const string Building =
            // Root + "building.svg";` is a constant whose name "Building"
            // would false-match a naive "mutable entity return" regex.
            // The current lookahead (uppercase-or-bracket next token)
            // excludes it; this entry is kept as evidence of the
            // rule's design boundary and as a safety net if a future
            // refactor loosens the lookahead. Remove during
            // Architecture Hardening A3.
            "game/scripts/IconPaths.cs",
        };

    /// <summary>
    /// Files under Presentation that may reach into a domain aggregate
    /// or entity through a public mutator. Architecture Hardening A8
    /// closed the production paths: every gameplay command goes
    /// through <c>CityGameSession</c>, and visual-regression fixtures
    /// reach the world only through the controller's <c>internal</c>
    /// fixture methods. The entries below are the two fixture scenes
    /// that still author their own citizens / buildings by hand and
    /// therefore trip the mutator pattern. Closing them is fixture-seam
    /// work that belongs to a future slice.
    /// </summary>
    public static IReadOnlyCollection<string> PresentationEntityMutator { get; } =
        new[]
        {
            // game/scripts/CityPrototype.cs — dev-only fixture scene
            // that authors named, deterministic citizens and reads
            // inventory directly to compose screenshot scenarios.
            // Gated by the WOG_VISUAL_CAPTURE flag and never runs in
            // production. Closing the seam is fixture-seam extraction
            // that lives behind a future slice.
            "game/scripts/CityPrototype.cs",

            // game/scripts/CombatDebugPanel.cs — debug-only combat
            // trigger that equips the picked party through direct
            // entity mutation before running the deterministic
            // resolver. Closing the seam is fixture-seam extraction
            // that lives behind a future slice.
            "game/scripts/CombatDebugPanel.cs",
        };

    /// <summary>
    /// Files under Presentation that may instantiate a fresh
    /// <see cref="CityWorld"/>. Architecture Hardening A8 made the
    /// <c>CityGameSession</c> the only legitimate owner of the
    /// aggregate; production presentation code never builds a world
    /// of its own. The single entry is the dev-only fixture scene
    /// that authors named, deterministic cities for screenshot
    /// scenarios. Closing it is fixture-seam work that belongs to a
    /// future slice.
    /// </summary>
    public static IReadOnlyCollection<string> PresentationInstantiatesWorld { get; } =
        new[]
        {
            // game/scripts/CityPrototype.cs — dev-only fixture scene
            // that authors named, deterministic cities to compose
            // screenshot scenarios. Gated by the WOG_VISUAL_CAPTURE
            // flag and never runs in production.
            "game/scripts/CityPrototype.cs",

            // game/scripts/Prototypes/RealCityStreetPreview.cs —
            // dev-only preview that loads a real save through
            // WorldPersistence and authors a fresh CityWorld to render
            // the macro layout. Closing the seam is fixture-seam
            // extraction that lives behind a future slice.
            "game/scripts/Prototypes/RealCityStreetPreview.cs",
        };

    /// <summary>
    /// Files under Presentation that still compose their static UI
    /// hierarchy in C#. Architecture Hardening A11 codifies the rule
    /// that production screens whose shape does not depend on
    /// runtime data live in a <c>.tscn</c>; the script owns behaviour,
    /// state binding, and the rows that the snapshot drives.
    ///
    /// <para>Every entry below is one of the four classifications
    /// A11 documents:</para>
    /// <list type="bullet">
    ///   <item><b>A</b> — static authored structure. Currently
    ///         composed in C#; targeted for migration to <c>.tscn</c>
    ///         by follow-up slices (see the GitHub issue tracker).</item>
    ///   <item><b>B</b> — genuinely dynamic collection. Static parts
    ///         are minimal (scroll container + headers); the bulk is
    ///         data-driven rows the snapshot rebuilds each refresh.</item>
    ///   <item><b>D</b> — dev/debug tooling. No migration needed.</item>
    ///   <item><b>E</b> — runtime-only visual object. Lives only while
    ///         a transient state is active; no migration needed.</item>
    /// </list>
    ///
    /// <para>The architecture guard
    /// <see cref="ArchitectureBoundaryTests.ProductionUi_DoesNotComposeStaticHierarchyInCode"/>
    /// scans every other production screen and fails the build if
    /// one adds static structure in C#. New panels default to
    /// <c>.tscn</c>; the script only composes dynamic rows.</para>
    /// </summary>
    public static IReadOnlyCollection<string> ProductionUiStaticStructureInCode { get; } =
        new[]
        {
            // ── A: static authored structure, migrate to .tscn ──
            // Each panel's static hierarchy is identical for every
            // city; the dynamic content is rebuilt on signal. The
            // migration moves the shell to a PackedScene and keeps
            // C# only for the rows that depend on a snapshot.
            "game/scripts/ExpeditionRail.cs",     // rail shell + chronicle accordion
            "game/scripts/HeroProfileView.cs",    // profile columns
            "game/scripts/ConstructionPanel.cs",  // construction modal shell

            // ── B: genuinely dynamic collection ──
            // Static parts are minimal (VBoxContainer + Header);
            // dynamic rows are the bulk. Stay programmatic.
            // Shell migrated to game/scenes/OnboardingView.tscn; what remains
            // is the stage slot, whose whole content is replaced per stage
            // (twelve progress pips, four narrative choices, the naming
            // controls, the founder card) and freed by ClearStage. That is the
            // B shape, so the entry moved here rather than being closed by
            // wrapping two loops in primitives that exist only to satisfy the
            // scanner.
            "game/scripts/AstralOnboardingView.cs",
            "game/scripts/AssignmentPanel.cs",     // 3 scrollable sections + rows
            "game/scripts/ExpeditionPanel.cs",     // team list rebuilt per snapshot
            "game/scripts/BuildingPlot.cs",       // one node per plot, fixed per-plot structure
            "game/scripts/MacroBuildingView.cs",  // one node per building, spawned per snapshot
            "game/scripts/MigrantPanel.cs",       // shell is .tscn; the prospect buttons are per-snapshot

            // ── D: dev/debug tooling ──
            // Combat debug panel only renders under WOG_VISUAL_CAPTURE.
            "game/scripts/CombatDebugPanel.cs",

            // ── D: dev/showcase tooling ──
            // Component showcase scene, not a player-facing screen.
            "game/scripts/LineageShowcase.cs",

            // ── E: runtime-only visual object ──
            // First-night overlay nodes vanish when the night ends;
            // notifier toasts and the founder arrival sequence are
            // transient visuals that compose on demand.
            "game/scripts/FirstNightScene.cs",
            "game/scripts/FirstNightSpeechBubble.cs",
            "game/scripts/TimeOfDayFilter.cs",
            "game/scripts/Notifier.cs",
            "game/scripts/FounderArrivalSequence.cs",
        };
}