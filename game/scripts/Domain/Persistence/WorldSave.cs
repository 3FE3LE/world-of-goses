using System.Collections.Generic;

namespace WorldofGoses.Domain.Persistence;

/// <summary>
/// Plain-data transfer object describing a serializable snapshot of
/// the domain state. Kept deliberately separate from
/// <see cref="CityWorld"/> so the persistence layer can evolve
/// without forcing domain refactors.
///
/// All fields are public properties with getters and setters so
/// that <c>System.Text.Json</c> can populate them during
/// deserialization. The shape is intentionally explicit (no records
/// / init-only properties) to make version migrations tractable
/// later: adding a property to <see cref="WorldSave"/> is
/// non-breaking for older saves that omit it.
/// </summary>
public sealed class WorldSave
{
    /// <summary>
    /// Current save schema version. Bumped on backwards-incompatible
    /// changes. The loader reads this and would migrate older
    /// versions up if it recognised any.
    /// </summary>
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    /// <summary>
    /// UTC timestamp of the moment the save was written, in Unix
    /// milliseconds. 0 means "no timestamp recorded" — the loader
    /// treats that as zero elapsed so legacy saves never
    /// accidentally apply years of catch-up.
    /// </summary>
    public long LastSeenAtUnixMillis { get; set; }

    public int CurrentTick { get; set; }
    public List<BuildingSave> Buildings { get; set; } = new();
    public List<CitizenSave> Citizens { get; set; } = new();
}
