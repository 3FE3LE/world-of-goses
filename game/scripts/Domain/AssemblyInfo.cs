using System.Runtime.CompilerServices;

/// <summary>
/// The domain assembly's test seam. Mirrors the one the Godot assembly has
/// carried since before the split: unit tests may reach domain internals so
/// encapsulation does not have to be weakened at the public API level just to
/// be testable.
///
/// <para>Deliberately NOT granted to the Godot assembly. Presentation calls
/// the domain's <em>public</em> API and nothing else; anything it genuinely
/// needs is promoted with intent and a doc comment, which is how the six
/// members the assembly split first caught were resolved.</para>
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
