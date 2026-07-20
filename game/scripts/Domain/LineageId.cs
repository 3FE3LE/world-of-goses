namespace WorldofGoses.Domain;

/// <summary>Stable identifier for one of the eight canonical lineages.</summary>
public readonly record struct LineageId(string Value)
{
    public static LineageId Ardhen { get; } = new("ardhen");
    public static LineageId Eirune { get; } = new("eirune");
    public static LineageId Kovari { get; } = new("kovari");
    public static LineageId Myrven { get; } = new("myrven");
    public static LineageId Vaelun { get; } = new("vaelun");
    public static LineageId Orveth { get; } = new("orveth");
    public static LineageId Caelith { get; } = new("caelith");
    public static LineageId Theryn { get; } = new("theryn");

    public override string ToString() => Value;
}
