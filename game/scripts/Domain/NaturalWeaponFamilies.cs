using System;

namespace WorldofGoses.Domain;

public static class NaturalWeaponFamilies
{
    public static (WeaponFamily First, WeaponFamily Second) For(PhysicalExpression expression) => expression switch
    {
        PhysicalExpression.Stunning => (WeaponFamily.Mace, WeaponFamily.Orb),
        PhysicalExpression.Bleeding => (WeaponFamily.Sword, WeaponFamily.Daggers),
        PhysicalExpression.Poisoning => (WeaponFamily.Bow, WeaponFamily.Darts),
        PhysicalExpression.Paralysis => (WeaponFamily.Whip, WeaponFamily.Gauntlets),
        PhysicalExpression.Fracture => (WeaponFamily.Hammer, WeaponFamily.Axe),
        PhysicalExpression.Knockdown => (WeaponFamily.Spear, WeaponFamily.Staff),
        _ => throw new ArgumentOutOfRangeException(nameof(expression), expression, null),
    };

    public static bool Contains(PhysicalExpression expression, WeaponFamily family)
    {
        (WeaponFamily first, WeaponFamily second) = For(expression);
        return family == first || family == second;
    }
}
