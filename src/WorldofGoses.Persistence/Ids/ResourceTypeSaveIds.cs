using System;
using WorldofGoses.Domain;

namespace WorldofGoses.Persistence.Ids;

/// <summary>
/// Stable wire IDs for <see cref="ResourceType"/>.
/// Architecture Hardening A7: these IDs are the contract. Renaming
/// the C# enum value does NOT change the persisted string until the
/// mapper is also updated — and the mapper carries an explicit
/// reminder comment naming the schema version that introduced or
/// last touched the value.
///
/// <para>Wire IDs match the historical <c>Enum.ToString()</c>
/// output of v1..v34. Do not change without a migration that keeps
/// the old ID parseable.</para>
/// </summary>
internal static class ResourceTypeSaveIds
{
    public const string StoneId = "Stone";
    public const string FoodId = "Food";
    public const string IronId = "Iron";
    public const string PotionsId = "Potions";
    public const string WoodId = "Wood";
    public const string BranchesId = "Branches";
    public const string PlantFiberId = "PlantFiber";
    public const string SmallStoneId = "SmallStone";
    public const string WildFoodId = "WildFood";

    public static string ToId(ResourceType value) => value switch
    {
        ResourceType.Stone => StoneId,
        ResourceType.Food => FoodId,
        ResourceType.Iron => IronId,
        ResourceType.Potions => PotionsId,
        ResourceType.Wood => WoodId,
        ResourceType.Branches => BranchesId,
        ResourceType.PlantFiber => PlantFiberId,
        ResourceType.SmallStone => SmallStoneId,
        ResourceType.WildFood => WildFoodId,
        _ => value.ToString(),
    };

    public static bool TryParse(string id, out ResourceType value)
    {
        switch (id)
        {
            case StoneId: value = ResourceType.Stone; return true;
            case FoodId: value = ResourceType.Food; return true;
            case IronId: value = ResourceType.Iron; return true;
            case PotionsId: value = ResourceType.Potions; return true;
            case WoodId: value = ResourceType.Wood; return true;
            case BranchesId: value = ResourceType.Branches; return true;
            case PlantFiberId: value = ResourceType.PlantFiber; return true;
            case SmallStoneId: value = ResourceType.SmallStone; return true;
            case WildFoodId: value = ResourceType.WildFood; return true;
            default:
                value = default!;
                return Enum.TryParse(id, ignoreCase: true, out value);
        }
    }
}
