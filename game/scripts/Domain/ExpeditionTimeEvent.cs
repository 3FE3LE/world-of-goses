namespace WorldofGoses.Domain;

/// <summary>
/// Why an expedition took longer or less than its estimate.
/// </summary>
/// <remarks>
/// The list is deliberately about <em>time</em> and nothing else. Experience,
/// loot and reputation hang off encounters too, but those systems do not exist
/// yet and guessing their shape here would bake it in. What the summary needs
/// today is distance, time and what moved it.
/// </remarks>
public enum ExpeditionTimeEventKind
{
    /// <summary>A group of enemies, a camp, a village — met and resolved.</summary>
    Encounter = 0,

    /// <summary>Nothing met on a stretch that usually holds something.</summary>
    ClearRoad = 1,

    /// <summary>The party pushed the pace and spent its reserve for it.</summary>
    ForcedMarch = 2,
}

/// <summary>
/// One thing that happened on the road and cost or saved time.
/// </summary>
/// <param name="Kind">What happened.</param>
/// <param name="Ticks">
/// Positive delays the return, negative brings it forward. It is the measured
/// cost of the event, not a roll: an encounter that dragged charges what it
/// actually took.
/// </param>
/// <param name="AtTick">The world tick the event was recorded on.</param>
public sealed record ExpeditionTimeEvent(
    ExpeditionTimeEventKind Kind,
    int Ticks,
    int AtTick);
