namespace WorldofGoses.Domain;

/// <summary>
/// A historical or active recognition attached to a citizen. A
/// citizen may hold any number of roles at any time; re-granting
/// the same role refreshes its granted tick.
/// </summary>
public sealed record Role(RoleId Id, int GrantedAtTick);
