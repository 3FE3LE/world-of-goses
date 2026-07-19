namespace WorldofGoses.Domain;

/// <summary>
/// Reason why a building produced zero this tick (or would have, had
/// it tried). Set by the world tick after each production attempt;
/// the presentation layer reads it to explain state to the player.
///
/// <para>
/// The values are deliberately stable integers so they can be
/// persisted later if a "return report" needs them. The order is
/// also stable: <see cref="Authorized"/> is the "no stoppage" sentinel
/// and every other value is a distinct cause.
/// </para>
///
/// <para>
/// Future causes (e.g. <c>NoFood</c>, <c>ToolShortage</c>) plug in as
/// new enum members — call sites should switch on the explicit names
/// and treat unknown values as the most defensive case.
/// </para>
/// </summary>
public enum ProductionStopCause
{
    Authorized = 0,
    Paused = 1,
    NoWorkers = 2,
    TargetReached = 3,
    WorkersExhausted = 4,
    Night = 5,
}
