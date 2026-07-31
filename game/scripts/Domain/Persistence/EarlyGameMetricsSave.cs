#nullable enable
using System.Collections.Generic;

namespace WorldofGoses.Domain.Persistence;

/// <summary>
/// Serializable form of <see cref="EarlyGameMetrics"/> — the EG-0 measurement
/// of a city's opening.
///
/// <para>Schema v20. A migrated city starts with an empty measurement rather
/// than a reconstructed one: it genuinely has no measured opening, and
/// back-filling plausible numbers would corrupt exactly the dataset EG-0
/// exists to gather. An empty measurement is distinguishable from a real one
/// by <see cref="DawnSamples"/> being zero.</para>
///
/// <para>Nothing in the domain reads these numbers, which is what lets absence
/// restore as empty instead of as an error. The moment one of them starts
/// feeding a rule, that no longer holds and the field needs real validation.</para>
/// </summary>
public sealed class EarlyGameMetricsSave
{
    public int? FirstShelterCompletedAtTick { get; set; }
    public int? FirstExpeditionDispatchedAtTick { get; set; }
    public int ExpeditionsDispatched { get; set; }
    public int ExpeditionAbsenceTicks { get; set; }
    public int DawnSamples { get; set; }
    public int IdleCitizenDays { get; set; }
    public int ObservedCitizenDays { get; set; }
    public int? MinFoodHorizonTenths { get; set; }
    public int? FoodHorizonTenthsAtFirstShelter { get; set; }

    /// <summary>Cumulative units gathered, keyed by <see cref="ResourceType"/> name.</summary>
    public Dictionary<string, int> Gathered { get; set; } = new();

    /// <summary>Cumulative units spent, keyed by <see cref="ResourceType"/> name.</summary>
    public Dictionary<string, int> Consumed { get; set; } = new();
}
