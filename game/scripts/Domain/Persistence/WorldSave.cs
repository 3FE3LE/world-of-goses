#nullable enable
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
    /// changes. Retired versions are rejected before restore so the
    /// controller can start a new onboarding flow without mutation.
    ///
    /// <para>Version history:</para>
    /// <list type="bullet">
    ///   <item><description>v1 — retired founding prototype (5 citizens, 3 buildings seeded).</description></item>
    ///   <item><description>v2 — founding-hero slice with single-target production policy.</description></item>
    ///   <item><description>v3 — recipes + min/max stock range + priority + CauseEventId wiring.</description></item>
    ///   <item><description>v4 — explicit gender identity stored on CitizenProfile.</description></item>
    ///   <item><description>v5 — bounded, selective causal-event history.</description></item>
    ///   <item><description>v6 — durable resource reservations and building input stock.</description></item>
    ///   <item><description>v7 — stable natural-resource units and citizen resource visits.</description></item>
    ///   <item><description>v8 — persistent parcels and natural-resource patches.</description></item>
    ///   <item><description>v9 — persistent building/project parcel placements.</description></item>
    ///   <item><description>v10 — one bounded natural-resource unit per parcel lot.</description></item>
    ///   <item><description>v11 — city-owned gathered inventory.</description></item>
    ///   <item><description>v12 — cosmetic appearance variant per citizen.</description></item>
    ///   <item><description>v13 — persisted abstract reconnaissance expeditions.</description></item>
    ///   <item><description>v14 — retired the `BuildingKind.Forest` compatibility adapter. Wood lives in `NaturalResourcePatches` and `CityInventory`; the Forest building entity no longer exists.</description></item>
    ///   <item><description>v15 — expeditions carry a real 1-2 citizen team (`ExpeditionSave.MemberCitizenIds`) instead of a single `LeadCitizenId`.</description></item>
    ///   <item><description>v16 — expeditions carry a persisted phase (`ExpeditionSave.Phase`) and a resolved encounter outcome (`ExpeditionSave.EncounterOutcome`).</description></item>
    ///   <item><description>v17 — every expedition member is an explicitly incorporated hero; legacy participants receive the existing hero role during migration.</description></item>
    ///   <item><description>v18 — expedition plans persist a retreat posture and their causal dispatch event; retreat is distinct from pre-travel cancellation.</description></item>
    ///   <item><description>v19 — citizens persist durable wounds and treatment progress independently from stamina; parcels persist an expedition-driven territorial state and expeditions retain their target parcel.</description></item>
    ///   <item><description>v20 — EG-0 opening measurement (`EarlyGameMetrics`). Purely observational: no rule reads it, so migrated cities start with an empty measurement rather than a reconstructed one. A city that predates the instrumentation genuinely has no measured opening, and inventing one would corrupt the very numbers EG-0 exists to gather.</description></item>
    /// </list>
    /// </summary>
    public const int CurrentVersion = 20;

    public int Version { get; set; } = CurrentVersion;

    /// <summary>
    /// Additive tuning revision. Zero identifies snapshots written before
    /// production batching and the first Farm/Quarry storage rebalance.
    /// </summary>
    public int EconomicBalanceVersion { get; set; }

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
    public List<ConstructionProjectSave> Projects { get; set; } = new();
    public List<WorldEventSave> Events { get; set; } = new();
    public List<ResourceReservationSave> ResourceReservations { get; set; } = new();
    public List<ParcelSave> Parcels { get; set; } = new();
    public List<NaturalResourcePatchSave> NaturalResourcePatches { get; set; } = new();
    public List<ParcelPlacementSave> ParcelPlacements { get; set; } = new();
    public Dictionary<string, int> CityInventory { get; set; } = new();

    /// <summary>
    /// EG-0 opening measurement (schema v20). Nullable so a partially written
    /// or hand-edited save restores as an empty measurement rather than
    /// throwing: no rule reads these numbers, so their absence can never make
    /// the city inconsistent. See <see cref="EarlyGameMetricsSave"/>.
    /// </summary>
    public EarlyGameMetricsSave? EarlyGameMetrics { get; set; }
    public List<ExpeditionSave> Expeditions { get; set; } = new();
    public int? PendingProspectSeed { get; set; }
    public string? PendingProspectName { get; set; }
}
