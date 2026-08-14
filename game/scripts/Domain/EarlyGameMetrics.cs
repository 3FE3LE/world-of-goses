#nullable enable
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// EG-0 measurement. Accumulates the five quantities
/// the opening measurement contract asks for
/// before the EG-A0 balance numbers may be approved or revised: time to the
/// first shelter, resources collected and spent, idle time, the Food horizon,
/// and the opportunity cost of an expedition.
///
/// <para>This type observes; it never decides. Nothing here feeds back into a
/// rule, a cost, or an availability check, so instrumenting a run cannot
/// change what that run measures.</para>
///
/// <para><b>Why every counter is event- or boundary-driven.</b>
/// <see cref="WorldTimeAdvance"/> collapses quiescent stretches into a single
/// arithmetic batch, so a per-tick counter would silently under-count exactly
/// the idle periods this measurement exists to find. Every quantity here is
/// therefore recorded either when a domain event happens or when the world
/// crosses the dawn boundary — the same transitions live play and offline
/// catch-up both go through, which is what makes the two agree.</para>
///
/// <para>Idle time is deliberately reported in <b>citizen-days sampled at
/// dawn</b> rather than exact idle ticks. A batched world cannot honestly
/// produce the latter, and a number that looks more precise than its source
/// would invite conclusions the data cannot support.</para>
/// </summary>
public sealed class EarlyGameMetrics
{
    private readonly Dictionary<ResourceType, int> _gathered = new();
    private readonly Dictionary<ResourceType, int> _consumed = new();

    /// <summary>Tick at which the city's first shelter finished. Null until it
    /// does. Only the first is recorded; later shelters do not overwrite it.</summary>
    public int? FirstShelterCompletedAtTick { get; private set; }

    /// <summary>Tick of the first expedition ever dispatched. Null until one is.</summary>
    public int? FirstExpeditionDispatchedAtTick { get; private set; }

    /// <summary>How many expeditions have been dispatched in total.</summary>
    public int ExpeditionsDispatched { get; private set; }

    /// <summary>Citizen-ticks spent away on expeditions — the direct
    /// opportunity cost of a sortie, since an absent citizen is an exclusive
    /// commitment the city cannot use for anything else.</summary>
    public int ExpeditionAbsenceTicks { get; private set; }

    /// <summary>How many dawn boundaries have been sampled. The denominator
    /// for every "-days" figure below.</summary>
    public int DawnSamples { get; private set; }

    /// <summary>Citizen-days observed with no commitment at dawn.</summary>
    public int IdleCitizenDays { get; private set; }

    /// <summary>Citizen-days observed at dawn in total, idle or not. The
    /// denominator that turns <see cref="IdleCitizenDays"/> into a ratio.</summary>
    public int ObservedCitizenDays { get; private set; }

    /// <summary>Lowest Food horizon ever sampled, in tenths of a day of
    /// rations. Tenths rather than days because the interesting range for a
    /// lone founder is under three days, where whole days would round the
    /// entire measurement away. Null before the first sample.</summary>
    public int? MinFoodHorizonTenths { get; private set; }

    /// <summary>Food horizon at the dawn following the first shelter, in the
    /// same tenths. This is the number EG-A0 predicts should sit near five
    /// Food for a lone founder.</summary>
    public int? FoodHorizonTenthsAtFirstShelter { get; private set; }

    /// <summary>Cumulative units of each resource that entered the city.</summary>
    public IReadOnlyDictionary<ResourceType, int> Gathered => _gathered;

    /// <summary>Cumulative units of each resource the city spent.</summary>
    public IReadOnlyDictionary<ResourceType, int> Consumed => _consumed;

    public void RecordGathered(ResourceType resource, int amount)
    {
        if (amount <= 0) return;
        _gathered.TryGetValue(resource, out int current);
        _gathered[resource] = current + amount;
    }

    public void RecordConsumed(ResourceType resource, int amount)
    {
        if (amount <= 0) return;
        _consumed.TryGetValue(resource, out int current);
        _consumed[resource] = current + amount;
    }

    /// <summary>
    /// Records the first shelter completion. Idempotent past the first call:
    /// "time to first shelter" is a property of the opening, so a second
    /// shelter built on day 20 must not overwrite it.
    /// </summary>
    public void RecordFirstShelterCompleted(int tick)
    {
        FirstShelterCompletedAtTick ??= tick;
    }

    public void RecordExpeditionDispatched(int tick)
    {
        FirstExpeditionDispatchedAtTick ??= tick;
        ExpeditionsDispatched++;
    }

    /// <summary>
    /// Adds the absence a returning expedition cost the city, counted per
    /// member because a two-person sortie removes twice the labour a
    /// one-person sortie does.
    /// </summary>
    public void RecordExpeditionAbsence(int ticksAway, int memberCount)
    {
        if (ticksAway <= 0 || memberCount <= 0) return;
        ExpeditionAbsenceTicks += ticksAway * memberCount;
    }

    /// <summary>
    /// Samples the once-per-day quantities at the dawn boundary, where the
    /// ration is charged and the population is stable for the day.
    /// </summary>
    /// <param name="foodStock">Food available to the city right now.</param>
    /// <param name="residentCount">Mouths the ration will feed.</param>
    /// <param name="idleCitizenCount">Residents holding no commitment.</param>
    public void SampleDawn(int foodStock, int residentCount, int idleCitizenCount)
    {
        DawnSamples++;
        ObservedCitizenDays += residentCount;
        IdleCitizenDays += idleCitizenCount;

        // A city with no residents has no ration to outlast, so it has no
        // horizon — recording zero there would report a starving city.
        if (residentCount <= 0) return;

        int horizonTenths = foodStock * 10 / residentCount;

        // The first dawn once a shelter exists is the snapshot EG-A0 predicts
        // against; every later dawn leaves it alone.
        if (FoodHorizonTenthsAtFirstShelter is null
            && FirstShelterCompletedAtTick is not null)
        {
            FoodHorizonTenthsAtFirstShelter = horizonTenths;
        }

        if (MinFoodHorizonTenths is null || horizonTenths < MinFoodHorizonTenths)
        {
            MinFoodHorizonTenths = horizonTenths;
        }
    }

    /// <summary>
    /// Rehydrates a persisted measurement. Used only by the persistence layer;
    /// gameplay never rewrites accumulated history.
    /// </summary>
    public void Restore(
        int? firstShelterCompletedAtTick,
        int? firstExpeditionDispatchedAtTick,
        int expeditionsDispatched,
        int expeditionAbsenceTicks,
        int dawnSamples,
        int idleCitizenDays,
        int observedCitizenDays,
        int? minFoodHorizonTenths,
        int? foodHorizonTenthsAtFirstShelter,
        IReadOnlyDictionary<ResourceType, int>? gathered,
        IReadOnlyDictionary<ResourceType, int>? consumed)
    {
        FirstShelterCompletedAtTick = firstShelterCompletedAtTick;
        FirstExpeditionDispatchedAtTick = firstExpeditionDispatchedAtTick;
        ExpeditionsDispatched = expeditionsDispatched;
        ExpeditionAbsenceTicks = expeditionAbsenceTicks;
        DawnSamples = dawnSamples;
        IdleCitizenDays = idleCitizenDays;
        ObservedCitizenDays = observedCitizenDays;
        MinFoodHorizonTenths = minFoodHorizonTenths;
        FoodHorizonTenthsAtFirstShelter = foodHorizonTenthsAtFirstShelter;

        _gathered.Clear();
        if (gathered is not null)
        {
            foreach (KeyValuePair<ResourceType, int> entry in gathered)
            {
                _gathered[entry.Key] = entry.Value;
            }
        }

        _consumed.Clear();
        if (consumed is null) return;
        foreach (KeyValuePair<ResourceType, int> entry in consumed)
        {
            _consumed[entry.Key] = entry.Value;
        }
    }
}
