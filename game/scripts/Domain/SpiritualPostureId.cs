namespace WorldofGoses.Domain;

/// <summary>Stable identifier for a spiritual posture.</summary>
public readonly record struct SpiritualPostureId(string Value)
{
    public static SpiritualPostureId Devout { get; } = new("devout");
    public static SpiritualPostureId Contemplative { get; } = new("contemplative");
    public static SpiritualPostureId Syncretic { get; } = new("syncretic");
    public static SpiritualPostureId Agnostic { get; } = new("agnostic");
    public static SpiritualPostureId Skeptical { get; } = new("skeptical");
    public static SpiritualPostureId Secular { get; } = new("secular");

    public override string ToString() => Value;
}
