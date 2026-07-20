namespace WorldofGoses.Domain;

/// <summary>Stable identifier for a preferred weapon family.</summary>
public readonly record struct WeaponPreferenceId(string Value)
{
    public static WeaponPreferenceId Polearm { get; } = new("polearm");
    public static WeaponPreferenceId Heavy { get; } = new("heavy");
    public static WeaponPreferenceId Blade { get; } = new("blade");
    public static WeaponPreferenceId Ranged { get; } = new("ranged");
    public static WeaponPreferenceId Shield { get; } = new("shield");
    public static WeaponPreferenceId Unarmed { get; } = new("unarmed");

    public override string ToString() => Value;
}
