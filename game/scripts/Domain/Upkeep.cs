namespace WorldofGoses.Domain;

/// <summary>
/// Passive city upkeep. The city consumes stone at a rate that
/// scales with its population. The placeholder is intentionally
/// "abstract city upkeep" — a future slice will replace the
/// consumption with building-driven demand (Smithy producing
/// tools, depot maintenance, etc.). The seam is the single
/// <see cref="StonePerTick"/> entry point; callers go through
/// <see cref="CityWorld.ApplyUpkeep"/>.
///
/// <para>
/// The rate is provisional tuning; see
/// <c>docs/PRODUCT_DIRECTION.md §5</c>.
/// </para>
/// </summary>
public static class Upkeep
{
    /// <summary>
    /// Stone consumed per tick. Defaults to one stone per five
    /// citizens (rounded up), with a floor of 1. So a city of
    /// five drains one stone per tick; a city of ten drains two;
    /// a city of one still drains one (no zero-demand state).
    /// </summary>
    public static int StonePerTick(int citizenCount)
    {
        if (citizenCount <= 0) return 0;
        int scaled = (citizenCount + 4) / 5; // ceil(citizenCount / 5)
        return scaled < 1 ? 1 : scaled;
    }

    /// <summary>
    /// Food consumed once per in-game day, for every resident, whether or
    /// not they worked or lost stamina that day — the "mouths to feed"
    /// pressure recommended by <c>docs/FIRST_PLAYABLE_LOOP_AUDIT.md</c> §17.
    /// Unlike <see cref="StonePerTick"/> this has no artificial floor: an
    /// empty city has no demand, because the demand exists to make adding
    /// residents (recruitment, migrants) carry a real ongoing cost, not to
    /// simulate abstract city maintenance. See
    /// <see cref="CityWorld.ApplyResidentFoodRation"/> for the call site.
    /// </summary>
    public static int FoodPerResidentPerDay(int citizenCount) =>
        citizenCount <= 0 ? 0 : citizenCount;
}
