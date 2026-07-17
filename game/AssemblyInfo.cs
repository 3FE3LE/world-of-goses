using System.Runtime.CompilerServices;

/// <summary>
/// Exposes internal types to the <c>WorldofGoses.Tests</c> project so the
/// domain can be unit-tested without weakening the encapsulation
/// boundary at the public API level.
/// </summary>
[assembly: InternalsVisibleTo("WorldofGoses.Tests")]
