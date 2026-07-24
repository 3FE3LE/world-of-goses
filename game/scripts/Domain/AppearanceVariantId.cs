namespace WorldofGoses.Domain;

/// <summary>Stable cosmetic appearance profile. It never grants profession bonuses or restrictions.</summary>
public readonly record struct AppearanceVariantId(string Value)
{
    public static AppearanceVariantId Standard { get; } = new("standard");
    public static AppearanceVariantId Extraction { get; } = new("extraction");
    public static AppearanceVariantId Construction { get; } = new("construction");
    public static AppearanceVariantId Agriculture { get; } = new("agriculture");
    public static AppearanceVariantId Care { get; } = new("care");
    public static AppearanceVariantId Engineering { get; } = new("engineering");
    public static AppearanceVariantId Exploration { get; } = new("exploration");
    public static AppearanceVariantId Logistics { get; } = new("logistics");
    public static AppearanceVariantId Commerce { get; } = new("commerce");
    public static AppearanceVariantId Research { get; } = new("research");
    public static AppearanceVariantId Social { get; } = new("social");
    public static AppearanceVariantId Security { get; } = new("security");
    public static AppearanceVariantId Arts { get; } = new("arts");

    public override string ToString() => Value;
}
