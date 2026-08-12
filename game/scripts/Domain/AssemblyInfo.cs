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
/// </summary>
[assembly: InternalsVisibleTo("WorldofGoses.Tests")]
