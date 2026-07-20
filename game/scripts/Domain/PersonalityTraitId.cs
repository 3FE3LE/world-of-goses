namespace WorldofGoses.Domain;

/// <summary>Stable identifier for a personality trait.</summary>
public readonly record struct PersonalityTraitId(string Value)
{
    public static PersonalityTraitId Patient { get; } = new("patient");
    public static PersonalityTraitId Protective { get; } = new("protective");
    public static PersonalityTraitId Reflective { get; } = new("reflective");
    public static PersonalityTraitId Curious { get; } = new("curious");
    public static PersonalityTraitId Disciplined { get; } = new("disciplined");
    public static PersonalityTraitId Cooperative { get; } = new("cooperative");
    public static PersonalityTraitId Ambitious { get; } = new("ambitious");
    public static PersonalityTraitId Cautious { get; } = new("cautious");
    public static PersonalityTraitId Bold { get; } = new("bold");
    public static PersonalityTraitId Reserved { get; } = new("reserved");
    public static PersonalityTraitId Compassionate { get; } = new("compassionate");
    public static PersonalityTraitId Pragmatic { get; } = new("pragmatic");
    public static PersonalityTraitId Independent { get; } = new("independent");
    public static PersonalityTraitId Diplomatic { get; } = new("diplomatic");
    public static PersonalityTraitId Tenacious { get; } = new("tenacious");
    public static PersonalityTraitId Restless { get; } = new("restless");

    public override string ToString() => Value;
}
