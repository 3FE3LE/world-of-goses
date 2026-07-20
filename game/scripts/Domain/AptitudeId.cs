namespace WorldofGoses.Domain;

/// <summary>Stable identifier for a personal aptitude.</summary>
public readonly record struct AptitudeId(string Value)
{
    public static AptitudeId Observation { get; } = new("observation");
    public static AptitudeId Empathy { get; } = new("empathy");
    public static AptitudeId ManualPrecision { get; } = new("manual_precision");
    public static AptitudeId Strength { get; } = new("strength");
    public static AptitudeId Orientation { get; } = new("orientation");
    public static AptitudeId Memory { get; } = new("memory");
    public static AptitudeId Creativity { get; } = new("creativity");
    public static AptitudeId SelfControl { get; } = new("self_control");
    public static AptitudeId RiskTolerance { get; } = new("risk_tolerance");
    public static AptitudeId Adaptability { get; } = new("adaptability");

    public override string ToString() => Value;
}
