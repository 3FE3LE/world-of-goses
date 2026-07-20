namespace WorldofGoses.Domain;

/// <summary>Stable identifier for a preferred approach to combat.</summary>
public readonly record struct CombatStyleId(string Value)
{
    public static CombatStyleId DefensiveSupport { get; } = new("defensive_support");
    public static CombatStyleId TerritorialControl { get; } = new("territorial_control");
    public static CombatStyleId Mobility { get; } = new("mobility");
    public static CombatStyleId Precision { get; } = new("precision");
    public static CombatStyleId DirectAssault { get; } = new("direct_assault");

    public override string ToString() => Value;
}
