namespace WorldofGoses.Domain;

/// <summary>
/// A person encountered by an expedition who is temporarily hosted by the
/// Town Hall. A prospect is not yet a city citizen and cannot be assigned.
/// </summary>
/// <param name="ArrivalTick">
/// The tick this prospect was hosted on. Part of their identity, not just a
/// timestamp: it is one of the three inputs <see cref="MigrantGenerator"/> draws
/// them from, so a save that loses it cannot regenerate the same person. It is
/// also the tick their prior working history is seeded from, which is why
/// accepting a prospect two days later must not produce a different mason.
/// </param>
public sealed record CitizenProspect(
    int Seed,
    string Name,
    CitizenProfile Profile,
    int ArrivalTick = 0);
