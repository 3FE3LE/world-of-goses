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
    public static IReadOnlyCollection<string> PresentationDirectWorldAccess { get; } =
        new[]
        {
            // game/scripts/AstralOnboardingView.cs — hands the live
            // founder <c>Citizen</c> to <c>FounderArrivalSequence.Begin</c>,
            // which reads profile.Gender, profile.Lineage, and
            // appearanceVariant. The arrival sequence is documented as
            // transient placeholder art; A1 keeps it on the allowlist so
            // the animation shape is not coupled to a new snapshot. Move
            // during Architecture Hardening A3.
            "game/scripts/AstralOnboardingView.cs",

            // game/scripts/CombatDebugPanel.cs — passes the live
            // <c>CityWorld</c> to <c>CombatExpeditionService.Run(party, plan)</c>
            // and writes back through <c>service.ApplyResult(party, result)</c>.
            // The write-back requirement is real; A1 keeps the panel on the
            // allowlist. Move during Architecture Hardening A3.
            "game/scripts/CombatDebugPanel.cs",

            // game/scripts/CityPrototype.cs — dev-only fixture scene that
            // builds deterministic worlds for visual regression and offline
            // tests. A1 added <c>internal</c> fixture commands on the
            // controller (<c>SeedFixtureWorld</c>, <c>RegisterFixtureCitizen</c>,
            // <c>RecordFixtureLogEvent</c>, <c>DepositToFixtureInventory</c>,
            // <c>TryConsumeFixtureResource</c>, <c>GetFixtureHero</c>, …)
            // and migrated the bulk of the setup, but a handful of fixture
            // functions still capture <c>controller.World</c> to a local
            // and operate on it for stage-bound setup. Closing the rest is a
            // fixture-seam extraction that belongs to A3; until then, this
            // file is gated by the dev command-line flags and never runs in
            // production.
            "game/scripts/CityPrototype.cs",
        };

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
}