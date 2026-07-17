namespace WorldofGoses.Domain;

/// <summary>
/// Stable, opaque identifier for a <see cref="Citizen"/>. Value type so it
/// can be used as a dictionary key without allocation.
/// </summary>
public readonly record struct CitizenId(int Value);