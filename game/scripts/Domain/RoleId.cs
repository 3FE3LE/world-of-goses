namespace WorldofGoses.Domain;

/// <summary>
/// Stable, opaque identifier for a <see cref="Role"/>. Roles are
/// composed onto a citizen (e.g. Miner, Hero, Farmer) and may be
/// granted, revoked, or checked via the citizen's API.
/// </summary>
public readonly record struct RoleId(string Value)
{
    public static RoleId Miner { get; } = new("miner");
    public static RoleId Hero { get; } = new("hero");

    public override string ToString() => Value;
}
