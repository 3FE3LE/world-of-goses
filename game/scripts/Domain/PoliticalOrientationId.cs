namespace WorldofGoses.Domain;

/// <summary>Stable identifier for a political orientation.</summary>
public readonly record struct PoliticalOrientationId(string Value)
{
    public static PoliticalOrientationId Communitarian { get; } = new("communitarian");
    public static PoliticalOrientationId Autonomist { get; } = new("autonomist");
    public static PoliticalOrientationId Institutional { get; } = new("institutional");
    public static PoliticalOrientationId Traditionalist { get; } = new("traditionalist");
    public static PoliticalOrientationId Reformist { get; } = new("reformist");
    public static PoliticalOrientationId Mercantile { get; } = new("mercantile");
    public static PoliticalOrientationId Ecological { get; } = new("ecological");
    public static PoliticalOrientationId SecurityOriented { get; } = new("security_oriented");

    public override string ToString() => Value;
}
