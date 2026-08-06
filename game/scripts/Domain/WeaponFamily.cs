namespace WorldofGoses.Domain;

public enum WeaponFamily
{
    Mace,
    Orb,
    Sword,
    Daggers,
    Bow,
    Darts,
    Whip,
    Gauntlets,
    Hammer,
    Axe,
    Spear,
    Staff,
}

internal static class WeaponFamilyDisplay
{
    public static string DisplayName(WeaponFamily family) => family switch
    {
        WeaponFamily.Mace => "Mace",
        WeaponFamily.Orb => "Orb",
        WeaponFamily.Sword => "Sword",
        WeaponFamily.Daggers => "Daggers",
        WeaponFamily.Bow => "Bow",
        WeaponFamily.Darts => "Darts",
        WeaponFamily.Whip => "Whip",
        WeaponFamily.Gauntlets => "Gauntlets",
        WeaponFamily.Hammer => "Hammer",
        WeaponFamily.Axe => "Axe",
        WeaponFamily.Spear => "Spear",
        WeaponFamily.Staff => "Staff",
        _ => family.ToString(),
    };
}
