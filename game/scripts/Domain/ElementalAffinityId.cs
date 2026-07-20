namespace WorldofGoses.Domain;

/// <summary>Stable identifier for an elemental affinity.</summary>
public readonly record struct ElementalAffinityId(string Value)
{
    public static ElementalAffinityId Water { get; } = new("water");
    public static ElementalAffinityId Fire { get; } = new("fire");
    public static ElementalAffinityId Earth { get; } = new("earth");
    public static ElementalAffinityId Air { get; } = new("air");
    public static ElementalAffinityId Aether { get; } = new("aether");
    public static ElementalAffinityId None { get; } = new("none");

    public override string ToString() => Value;
}
