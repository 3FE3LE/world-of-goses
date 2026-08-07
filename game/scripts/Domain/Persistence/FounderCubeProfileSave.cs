namespace WorldofGoses.Domain.Persistence;

/// <summary>Serializable form of the founder's canonical cube profile.</summary>
public sealed class FounderCubeProfileSave
{
    public int Body { get; set; }
    public int Bond { get; set; }
    public int Stability { get; set; }
    public int Impulse { get; set; }
    public int Domain { get; set; }
    public int Reach { get; set; }

    /// <summary>
    /// Nullable bridge for v31 saves, whose on-disk key was
    /// <c>"Mastery"</c>. Schema v32 renames the canonical field to
    /// <c>"Domain"</c>; a v31 save deserialized by the new code keeps the
    /// legacy value here until <see cref="WorldPersistence.MigrateV31ToV32"/>
    /// copies it across and clears the field. New code never writes to it.
    /// </summary>
    [System.Obsolete("Bridge for v31 saves; migrated to Domain by MigrateV31ToV32.")]
    public int? Mastery { get; set; }
}