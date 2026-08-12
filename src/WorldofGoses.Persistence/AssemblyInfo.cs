using System.Runtime.CompilerServices;

/// <summary>
/// The persistence assembly's test seam. Mirrors the one Domain and
/// Presentation have carried since the engine-free assemblies
/// split: xUnit may reach persistence internals so encapsulation
/// does not have to be weakened at the public API level just to be
/// testable.
///
/// <para>
/// Architecture Hardening A6 grants this visibility. The migration
/// path used by A0-A4 — capturing/restoring through internal
/// <c>CaptureEarlyGameMetrics</c> / <c>RestoreEarlyGameMetrics</c>
/// helpers — would otherwise leak into public surface area or
/// duplicate the EG-0 measurement translation logic in every test.
/// </para>
/// </summary>
[assembly: InternalsVisibleTo("WorldofGoses.Tests")]
