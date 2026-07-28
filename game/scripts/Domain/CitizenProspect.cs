namespace WorldofGoses.Domain;

/// <summary>
/// A person encountered by an expedition who is temporarily hosted by the
/// Town Hall. A prospect is not yet a city citizen and cannot be assigned.
/// </summary>
public sealed record CitizenProspect(int Seed, string Name, CitizenProfile Profile);
