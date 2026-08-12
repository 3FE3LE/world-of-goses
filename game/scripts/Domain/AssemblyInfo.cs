using System.Runtime.CompilerServices;

/// <summary>
/// The domain assembly's test seam. Mirrors the one the Godot assembly has
/// carried since before the split: unit tests may reach domain internals so
/// encapsulation does not have to be weakened at the public API level just to
/// be testable.
///
/// <para><strong>The Godot assembly IS granted.</strong> This comment used to
/// claim the opposite — "deliberately NOT granted to the Godot assembly" —
/// while <c>WorldofGoses.Domain.csproj</c> carried an
/// <c>InternalsVisibleTo("World of Goses")</c> assembly attribute the whole
/// time. A10 added the grant so the visual-regression harness could reach
/// <c>CityWorld.ConcludeFirstNightForFixtures</c> and
/// <c>ConstructionProject.SeedProgressForFixture</c> after both were demoted
/// from <c>public</c>; the comment was never updated. A boundary document
/// that describes a stricter boundary than the compiler enforces is worse
/// than none, because reviewers trust it.</para>
///
/// <para>What is actually true: Presentation calls the domain's
/// <em>public</em> API for all gameplay. The internal grant exists for the
/// fixture seams only, and which call sites may use them is pinned by
/// <c>ArchitectureBoundaryTests</c> (<c>Presentation_ConcludesFirstNightOnlyInFixtures</c>,
/// <c>Presentation_DoesNotAccessCityWorldDirectly</c>) rather than by
/// visibility, precisely because visibility can no longer express it.</para>
///
/// <para>
/// Architecture Hardening A6 also grants <c>InternalsVisibleTo</c> to the
/// <c>WorldofGoses.Persistence</c> assembly. The persistence layer lives
/// outside Domain and may not have Domain types reference it, so the only
/// way the mapper can call into domain helpers like
/// <c>CitizenProfile.Restore</c> and
/// <c>ExpeditionCombatSessionFactory.OpeningBaselineFor</c> is by reaching
/// through the internal seam. The seam is still one-directional: Persistence
/// may see Domain internals, but Domain never sees Persistence symbols.
/// </para>
/// </summary>
[assembly: InternalsVisibleTo("WorldofGoses.Tests")]
[assembly: InternalsVisibleTo("WorldofGoses.Persistence")]
