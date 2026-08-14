namespace WorldofGoses.Domain;

/// <summary>
/// Passive city upkeep. No passive upkeep runs today: the placeholder
/// "abstract city upkeep" was retired because it drained Quarry stone
/// for no playable reason. The building-driven demand layer (Smithy
/// tools, depot maintenance, etc.) lands with EG-5C (#32); its
/// activation seam is <see cref="CityWorld.ApplyUpkeep"/>, which
/// currently throws if invoked.
///
/// <para>
/// Until then, only <see cref="FoodPerResidentPerDay"/> carries
/// consumption; see <see cref="CityWorld.ApplyResidentFoodRation"/>
/// for the call site.
/// </para>
/// </summary>
public static class Upkeep
{
    /// <summary>
    /// Food consumed once per in-game day, for every resident, whether or
    /// not they worked or lost stamina that day — the "mouths to feed"
    /// pressure recommended by <c>docs/world/vision-and-pillars.md</c>.
    /// Unlike the retired <c>StonePerTick</c> placeholder this has no
    /// artificial floor: an empty city has no demand, because the demand
    /// exists to make adding residents (recruitment, migrants) carry a
    /// real ongoing cost, not to simulate abstract city maintenance. See
    /// <see cref="CityWorld.ApplyResidentFoodRation"/> for the call site.
    /// </summary>
    public static int FoodPerResidentPerDay(int citizenCount) =>
        citizenCount <= 0 ? 0 : citizenCount;
}
